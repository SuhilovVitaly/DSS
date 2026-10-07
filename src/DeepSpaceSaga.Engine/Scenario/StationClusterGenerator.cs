using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;

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
            try { return Place(source, config, registry, seed, attempt); }
            catch (ScenarioException ex) { reason = ex.Message; }
        }
        throw new ScenarioException($"clusters/v1 seed={seed} stage=local attempts=128: {reason}");
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
