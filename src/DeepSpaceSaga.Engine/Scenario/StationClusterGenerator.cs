using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Rng;

namespace DeepSpaceSaga.Engine.Scenario;

internal sealed record ClusterGenerationResult(ScenarioFile World, StationClusterMapSnapshot Map);

internal static class StationClusterGenerator
{
    internal static readonly string[] Profiles = ["market.mining", "market.industrial", "market.hydroponic", "market.transit", "market.scientific-military"];

    internal static void ValidateConfig(ClusterGenerationConfig c)
    {
        if (c.MinClusters < 1 || c.MaxClusters > 5 || c.MaxClusters < c.MinClusters ||
            c.MinStations < 10 || c.MaxStations > 12 || c.MaxStations < c.MinStations ||
            !Range(c.NeighbourMinDays, c.NeighbourMaxDays) || !Range(c.DiameterMinDays, c.DiameterMaxDays) ||
            !Range(c.InterclusterMinDays, c.InterclusterMaxDays) || c.DiameterMinDays <= c.NeighbourMaxDays)
            throw new ContentException("clusters: invalid counts or finite positive travel-day ranges.");
        static bool Range(double min, double max) => double.IsFinite(min) && double.IsFinite(max) && min > 0 && max >= min;
    }

    internal static ClusterGenerationResult Generate(ScenarioFile source, ClusterGenerationConfig config, GameDataRegistry registry, ulong seed)
    {
        ValidateConfig(config);
        string reason = "placement exhausted";
        for (int attempt = 0; attempt < 128; attempt++)
        {
            try { return Expand(Place(source, config, registry, seed, attempt), config, registry, seed); }
            catch (ScenarioException ex) { reason = ex.Message; }
        }
        throw new ScenarioException($"clusters/v1 seed={seed} stage=local attempts=128: {reason}");
    }

    private static ClusterGenerationResult Expand(ClusterGenerationResult first, ClusterGenerationConfig config, GameDataRegistry registry, ulong seed)
    {
        var countRng = new SolarSystemGenerator.GeneratorRng(seed, "clusters/count", 0);
        int count = countRng.NextInt(config.MinClusters, config.MaxClusters + 1);
        if (count == 1) return first;
        var state = first.World.GameState;
        var system = state.SolarSystem!;
        var belt = system.Belts.Single(b => b.Id == first.Map.Clusters[0].BeltId);
        var player = state.SpaceObjects.Single(o => o.ObjectId == state.PlayerShipObjectId);
        var homeStations = state.SpaceObjects.Where(o => first.Map.Clusters[0].StationIds.Contains(o.ObjectId)).ToArray();
        double radius = Math.Sqrt(player.PositionX * player.PositionX + player.PositionY * player.PositionY);
        double phase = Math.Atan2(player.PositionX, -player.PositionY);
        double minHome = homeStations.Min(s => s.Orbit!.SemiMajorAxis), maxHome = homeStations.Max(s => s.Orbit!.SemiMajorAxis);
        double padding = Math.Max(200, (maxHome - minHome) * 0.1);
        // Reserve radial lanes inside the human belt. Different lanes can reach conjunction
        // without merging: each group's entire radial envelope is disjoint.
        var lanes = Enumerable.Range(1, count * 3).Select(i => belt.InnerRadius + (belt.OuterRadius - belt.InnerRadius) * i / (count * 3 + 1.0))
            .Where(r => r < minHome - padding || r > maxHome + padding).OrderBy(r => Math.Abs(r - radius)).Take(count - 1).ToArray();
        if (lanes.Length != count - 1) throw new ScenarioException("clusters: belt cannot fit separate swept radial lanes.");
        var engine = (player.Modules ?? []).Select(m => registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex(m.ModuleTypeId))).First(m => m.MaxSpeedMps is > 0);
        double unitsPerDay = engine.MaxSpeedMps!.Value / 1000.0 * 86400 / 300 * 10;
        double spacing = (config.InterclusterMinDays + config.InterclusterMaxDays) / 2 * unitsPerDay;
        if (spacing >= 2 * lanes.Min()) throw new ScenarioException("clusters: intercluster spacing does not fit belt circumference.");
        var clusters = first.Map.Clusters.ToBuilder();
        var members = first.Map.Stations.ToBuilder();
        var links = first.Map.Links.ToBuilder();
        var objects = state.SpaceObjects.ToList();
        var ids = objects.Select(o => o.ObjectId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (int index = 1; index < count; index++)
        {
            string clusterId = $"CLUSTER-{index + 1}";
            double r = lanes[index - 1];
            double p = phase + index * 2 * Math.Asin(spacing / (2 * radius));
            var isolated = first.World with
            {
                GameState = state with
                {
                    TradingMap = null,
                    SpaceObjects = state.SpaceObjects.Where(o => o.ObjectType != SpaceObjectType.Station).Select(o => o.ObjectId != player.ObjectId ? o : o with
                    { PositionX = r * Math.Sin(p), PositionY = -r * Math.Cos(p), IsDocked = false, DockedStationObjectId = null }).ToArray()
                }
            };
            ulong groupSeed = RngStreamSeedDerivation.DeriveStreamSeed(seed, $"station-clusters/v1/{clusterId}");
            var group = Generate(isolated, config with { MinClusters = 1, MaxClusters = 1 }, registry, groupSeed);
            var groupStations = group.World.GameState.SpaceObjects.Where(o => group.Map.Clusters[0].StationIds.Contains(o.ObjectId))
                .Select(o => o with { ObjectId = o.ObjectId.Replace("CLUSTER-1", clusterId, StringComparison.Ordinal), Name = $"District {index + 1} {o.Name}" }).ToArray();
            foreach (var station in groupStations)
                if (!ids.Add(station.ObjectId)) throw new ScenarioException($"clusters: duplicate generated ID {station.ObjectId}.");
            string specialization = groupStations.GroupBy(s => s.MarketProfileId).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).First().Key!;
            clusters.Add(new(clusterId, $"District {index + 1}", belt.Id, groupStations.Select(s => s.ObjectId).ToImmutableArray(), specialization));
            members.AddRange(groupStations.Select(s => new ClusterStationData(s.ObjectId, clusterId, s.MarketProfileId!)));
            links.AddRange(group.Map.Links.Select(l => l with
            { Id = l.Id.Replace("CLUSTER-1", clusterId, StringComparison.Ordinal), FromStationId = l.FromStationId.Replace("CLUSTER-1", clusterId, StringComparison.Ordinal), ToStationId = l.ToStationId.Replace("CLUSTER-1", clusterId, StringComparison.Ordinal) }));
            objects.AddRange(groupStations);
        }
        // Resolved intercluster cargo candidates carry current endpoints, never cached ETAs.
        for (int index = 1; index < clusters.Count; index++)
        {
            var a = objects.Where(o => clusters[index - 1].StationIds.Contains(o.ObjectId)).ToArray();
            var b = objects.Where(o => clusters[index].StationIds.Contains(o.ObjectId)).ToArray();
            links.AddRange(BuildLinks(a.Concat(b).ToArray(), registry, (x, y) => a.Contains(x) != a.Contains(y)));
        }
        var centers = clusters.Select(c =>
        {
            var stations = objects.Where(o => c.StationIds.Contains(o.ObjectId)).ToArray();
            return (X: stations.Average(o => o.PositionX), Y: stations.Average(o => o.PositionY));
        }).ToArray();
        for (int i = 0; i < centers.Length; i++)
        {
            double nearest = centers.Where((_, j) => j != i).Min(c => Math.Sqrt(Math.Pow(c.X - centers[i].X, 2) + Math.Pow(c.Y - centers[i].Y, 2))) / unitsPerDay;
            if (nearest < config.InterclusterMinDays || nearest > config.InterclusterMaxDays)
                throw new ScenarioException("clusters: initial neighbour distance outside configured days.");
        }
        var result = first.World with
        {
            GameState = state with
            {
                SpaceObjects = objects.OrderBy(o => o.ObjectId, StringComparer.Ordinal).ToArray(),
                SolarSystem = system with
                { Orbits = objects.Where(o => o.Orbit is not null).Select(o => new OrbitMapData(o.ObjectId, o.Orbit!)).ToImmutableArray() }
            }
        };
        SolarSystemGeneration.ValidateWorld(result.GameState);
        return new(result, first.Map with { Clusters = clusters.ToImmutable(), Stations = members.ToImmutable(), Links = links.ToImmutable() });
    }

    private static ClusterGenerationResult Place(ScenarioFile source, ClusterGenerationConfig config, GameDataRegistry registry, ulong seed, int attempt)
    {
        var system = source.GameState.SolarSystem ?? throw new ScenarioException("clusters: materialized solar system required.");
        foreach (string id in Profiles)
            if (!registry.StationMarketProfiles.Contains(id)) throw new ScenarioException($"clusters: missing profile {id}.");
        var original = source.GameState.SpaceObjects.Where(o => o.ObjectType == SpaceObjectType.Station)
            .OrderBy(o => o.ObjectId, StringComparer.Ordinal).ToArray();
        var player = source.GameState.SpaceObjects.Single(o => o.ObjectId == source.GameState.PlayerShipObjectId);
        double radius = Math.Sqrt(player.PositionX * player.PositionX + player.PositionY * player.PositionY);
        var belt = system.Belts.SingleOrDefault(b => b.InnerRadius <= radius && b.OuterRadius >= radius)
            ?? throw new ScenarioException("clusters: start is outside every belt.");
        double vmax = (player.Modules ?? []).Where(m => m.PowerState == "On" && m.OperationalState == "Ready" && m.StructurePoints > 0)
            .Select(m => registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex(m.ModuleTypeId)))
            .First(m => m.MaxSpeedMps is > 0).MaxSpeedMps!.Value / 1000.0;
        double unitsPerDay = vmax * 86400 / SimulationSpeedExtensions.BaseGameSecondsPerRealSecond * 10;
        var rng = new SolarSystemGenerator.GeneratorRng(seed, "clusters/roles", attempt);
        int count = rng.NextInt(config.MinStations, config.MaxStations + 1);
        if (original.Length > count) throw new ScenarioException("clusters: scenario stations exceed quota.");
        var stations = original.Select((s, i) => s with { MarketProfileId = s.MarketProfileId ?? Profiles[i % Profiles.Length] }).ToList();
        var ids = source.GameState.SpaceObjects.Select(o => o.ObjectId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var referenceOrbit = system.Orbits.FirstOrDefault()?.Elements ?? throw new ScenarioException("clusters: no orbital reference.");
        var period = original.FirstOrDefault()?.Orbit?.OrbitalPeriodMs ?? checked((long)Math.Round(referenceOrbit.OrbitalPeriodMs * (radius / referenceOrbit.SemiMajorAxis)));
        double phase = (Math.Atan2(player.PositionX, -player.PositionY) * 180 / Math.PI + 360) % 360;
        // Rigid original objects already share the start group's angular velocity.
        // New stations occupy a short arc; the original group remains untouched.
        int generated = count - stations.Count;
        double diameter = (config.DiameterMinDays + Math.Min(config.DiameterMaxDays - config.DiameterMinDays, config.NeighbourMinDays) * rng.NextDouble()) * unitsPerDay;
        double span = 2 * Math.Asin(diameter / (2 * radius)) * 180 / Math.PI;
        if (!double.IsFinite(span) || generated < Profiles.Length - stations.Select(s => s.MarketProfileId).Distinct().Count())
            throw new ScenarioException("clusters: configuration cannot fit required roles and diameter.");
        var missing = Profiles.Where(p => stations.All(s => s.MarketProfileId != p)).ToList();
        double formationShift = span * (rng.NextDouble() - 0.5) * 0.5;
        for (int i = 0; i < generated; i++)
        {
            string id = $"CLUSTER-1-STATION-{i + 1}";
            if (!ids.Add(id)) throw new ScenarioException($"clusters: generated object ID collision {id}.");
            string profile = i < missing.Count ? missing[i] : Profiles[(i + rng.NextInt(0, Profiles.Length)) % Profiles.Length];
            double offset = generated == 1 ? span / 2 : -span / 2 + span * i / (generated - 1) + formationShift;
            double p = (phase + offset + 360) % 360;
            var orbit = new OrbitalElements(radius, radius, period, (int)p, p - (int)p, source.GameState.MotionTimeMs, "clockwise");
            stations.Add(new(id, "Station", "Permanent", $"Home {i + 1}", radius * Math.Sin(p * Math.PI / 180),
                -radius * Math.Cos(p * Math.PI / 180), 0, 0, "Orbital", null, null, null,
                IsKnown: true, StationSize: "Medium", MarketProfileId: profile, Orbit: orbit));
            double clearance = Math.Max(100, (source.GameState.TradingMap?.Rules.ClearanceKm ?? 0) * 10);
            if (source.GameState.SpaceObjects.Any(o => Distance(o, stations[^1]) < clearance) || stations.Take(stations.Count - 1).Any(o => Distance(o, stations[^1]) < clearance))
                throw new ScenarioException("clusters: generated station violates scenario clearance.");
        }
        var preserved = original.Select(s => s.ObjectId).ToHashSet(StringComparer.Ordinal);
        var links = BuildLinks(stations, registry, (a, b) => preserved.Contains(a.ObjectId) && preserved.Contains(b.ObjectId) ||
            Distance(a, b) / unitsPerDay >= config.NeighbourMinDays - 1e-9 && Distance(a, b) / unitsPerDay <= config.NeighbourMaxDays + 1e-9);
        ValidateGraph(stations, links, registry);
        double actualDiameter = stations.SelectMany(a => stations.Select(b => Distance(a, b))).Max() / unitsPerDay;
        if (actualDiameter < config.DiameterMinDays || actualDiameter > config.DiameterMaxDays)
            throw new ScenarioException("clusters: preserved group violates diameter range.");
        if (stations.Any(s => s.Orbit!.SemiMajorAxis < belt.InnerRadius || s.Orbit.SemiMajorAxis > belt.OuterRadius))
            throw new ScenarioException("clusters: rigid group exceeds belt width.");
        var byId = stations.ToDictionary(s => s.ObjectId, StringComparer.Ordinal);
        var objects = source.GameState.SpaceObjects.Select(s => byId.GetValueOrDefault(s.ObjectId, s)).Concat(stations.Where(s => !preserved.Contains(s.ObjectId)))
            .OrderBy(s => s.ObjectId, StringComparer.Ordinal).ToArray();
        var map = new StationClusterMapSnapshot(1, "CLUSTER-1", [new("CLUSTER-1", "Home", belt.Id, stations.Select(s => s.ObjectId).ToImmutableArray(), "balanced")],
            stations.Select(s => new ClusterStationData(s.ObjectId, "CLUSTER-1", s.MarketProfileId!)).ToImmutableArray(), links);
        var world = source with
        {
            GameState = source.GameState with
            {
                SpaceObjects = objects,
                SolarSystem = system with
                { Orbits = objects.Where(o => o.Orbit is not null).Select(o => new OrbitMapData(o.ObjectId, o.Orbit!)).ToImmutableArray() }
            }
        };
        SolarSystemGeneration.ValidateWorld(world.GameState);
        return new(world, map);
    }

    internal static double Distance(SpaceObjectData a, SpaceObjectData b) => Math.Sqrt(Math.Pow(a.PositionX - b.PositionX, 2) + Math.Pow(a.PositionY - b.PositionY, 2));

    internal static ClusterGenerationResult BindResources(ClusterGenerationResult result)
    {
        var fields = result.World.GameState.StationResourceFields;
        if (fields is null) return result;
        var objects = result.World.GameState.SpaceObjects.ToDictionary(o => o.ObjectId, StringComparer.Ordinal);
        var bindings = ImmutableArray.CreateBuilder<ClusterResourceBinding>();
        foreach (var asteroid in fields.Asteroids)
        {
            var owner = objects[asteroid.StationObjectId];
            var member = result.Map.Stations.Single(s => s.ObjectId == owner.ObjectId);
            var obj = objects[asteroid.ObjectId];
            double radius = Math.Sqrt(obj.PositionX * obj.PositionX + obj.PositionY * obj.PositionY);
            double phase = (Math.Atan2(obj.PositionX, -obj.PositionY) * 180 / Math.PI + 360) % 360;
            var orbit = owner.Orbit! with { SemiMajorAxis = radius, SemiMinorAxis = radius, InitialPhase = (int)phase, PhaseOffsetDegrees = phase - (int)phase };
            objects[obj.ObjectId] = obj with { Orbit = orbit, MovementType = "Orbital" };
            // The existing resource API is an asteroid manifest, with no separate field entity.
            // Use its canonical object ID as the binding ID, preserving one source of composition.
            bindings.Add(new(obj.ObjectId, member.ClusterId, owner.ObjectId, obj.PositionX - owner.PositionX, obj.PositionY - owner.PositionY));
        }
        var world = result.World with
        {
            GameState = result.World.GameState with
            {
                SpaceObjects = objects.Values.OrderBy(o => o.ObjectId, StringComparer.Ordinal).ToArray(),
                StationResourceFields = fields with { Asteroids = fields.Asteroids.Select(a => a with { CompositionKnown = true }).ToArray() },
                SolarSystem = result.World.GameState.SolarSystem! with { Orbits = objects.Values.Where(o => o.Orbit is not null).Select(o => new OrbitMapData(o.ObjectId, o.Orbit!)).ToImmutableArray() }
            }
        };
        SolarSystemGeneration.ValidateWorld(world.GameState);
        return new(world, result.Map with { ResourceBindings = bindings.ToImmutable() });
    }

    internal static ImmutableArray<ClusterTradeLink> BuildLinks(IReadOnlyList<SpaceObjectData> stations, GameDataRegistry registry, Func<SpaceObjectData, SpaceObjectData, bool> eligible)
    {
        var links = ImmutableArray.CreateBuilder<ClusterTradeLink>();
        foreach (var a in stations.OrderBy(s => s.ObjectId, StringComparer.Ordinal))
            foreach (var b in stations.OrderBy(s => s.ObjectId, StringComparer.Ordinal))
            {
                if (a.ObjectId == b.ObjectId || !eligible(a, b)) continue;
                var from = registry.StationMarketProfiles.GetDefinition(registry.StationMarketProfiles.GetIndex(a.MarketProfileId!));
                var to = registry.StationMarketProfiles.GetDefinition(registry.StationMarketProfiles.GetIndex(b.MarketProfileId!));
                var cargo = from.SupplyItemTypeIds.Intersect(to.DemandItemTypeIds, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
                // Transit is a visit/return direction without claiming production.
                if (cargo.IsEmpty && from.SupplyItemTypeIds.Length > 0) continue;
                links.Add(new($"LINK/{a.ObjectId}/{b.ObjectId}", a.ObjectId, b.ObjectId, cargo));
            }
        return links.ToImmutable();
    }

    internal static void ValidateGraph(IReadOnlyList<SpaceObjectData> stations, ImmutableArray<ClusterTradeLink> links, GameDataRegistry registry)
    {
        foreach (var station in stations)
        {
            if (links.Where(l => l.FromStationId == station.ObjectId).Select(l => l.ToStationId).Distinct().Count() < 2)
                throw new ScenarioException($"clusters: {station.ObjectId} has fewer than two trading directions.");
            var visited = new HashSet<string>(StringComparer.Ordinal) { station.ObjectId };
            var queue = new Queue<string>(); queue.Enqueue(station.ObjectId);
            while (queue.TryDequeue(out var id))
                foreach (var link in links.Where(l => l.FromStationId == id))
                    if (visited.Add(link.ToStationId)) queue.Enqueue(link.ToStationId);
            if (visited.Count != stations.Count) throw new ScenarioException("clusters: trade graph is not strongly connected.");
            var profile = registry.StationMarketProfiles.GetDefinition(registry.StationMarketProfiles.GetIndex(station.MarketProfileId!));
            if (profile.SupplyItemTypeIds.Length > 0 && !links.Any(l => l.FromStationId == station.ObjectId && !l.ItemTypeIds.IsEmpty))
                throw new ScenarioException($"clusters: producer {station.ObjectId} has no consumer.");
        }
        int edges = links.Select(l => string.CompareOrdinal(l.FromStationId, l.ToStationId) < 0 ? (l.FromStationId, l.ToStationId) : (l.ToStationId, l.FromStationId)).Distinct().Count();
        if (edges - stations.Count + 1 < 2) throw new ScenarioException("clusters: fewer than two independent trade cycles.");
    }
}
