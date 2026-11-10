using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Rng;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>Detached version-two station candidates. Never publishes a world or consumes live RNG.</summary>
internal static class StationGeographyV2
{
    private const int FormationColumns = 3;
    private const int FormationRows = 4;
    private const int AnchorRow = 1;
    private const double RadialRowSpacing = 0.875;
    private const double PositionJitter = 0.012;

    private static double FormationSpacing(ClusterGenerationConfig config, double units) =>
        Math.Min(config.NeighbourMaxDays * 0.965, config.DiameterMaxDays / 3.7) * units;

    internal static double LaneHalfWidth(ClusterGenerationConfig config, double units) =>
        ((FormationRows - 1 - AnchorRow) * RadialRowSpacing + PositionJitter) * FormationSpacing(config, units);

    internal static int ClusterCount(ClusterGenerationConfig config, ulong seed) =>
        SolarSystemGenerator.GeographyRng(seed, "cluster-count", 0).NextInt(config.MinClusters, config.MaxClusters + 1);

    internal static ClusterGenerationResult CompleteBootstrap(ScenarioFile original, ClusterGenerationResult candidate,
        SolarSystemGenerationConfig config, GameDataRegistry registry, ulong seed, double formationPadding = 0)
    {
        var player = candidate.World.GameState.SpaceObjects.Single(o => o.ObjectId == original.GameState.PlayerShipObjectId);
        double targetRadius = double.Hypot(player.PositionX, player.PositionY);
        var world = ScenarioGroupPlacement.BindV2Start(original, candidate, targetRadius);
        var state = world.GameState;
        var objects = state.SpaceObjects.ToDictionary(o => o.ObjectId, StringComparer.Ordinal);
        double units = candidate.Map.Geography!.ReferenceSpeedKmS * 86400 / 300 * 10;
        foreach (var cluster in candidate.Map.Clusters)
            ValidateDistances(cluster.StationIds.Select(id => objects[id]).ToArray(), config.Clusters!, units);
        double resourcePadding = Math.Max(Math.Max(200, formationPadding), (state.StationResourceFields?.Rules.OuterRadiusKm ?? 0) * 10);
        var lanes = candidate.Map.Clusters.Select(c =>
        {
            var radii = c.StationIds.Select(id => objects[id].Orbit!.SemiMajorAxis).ToArray();
            double inner = radii.Min(), outer = radii.Max();
            var belt = state.SolarSystem!.Belts.Single(b => b.Id == c.BeltId);
            if (inner - resourcePadding < belt.InnerRadius || outer + resourcePadding > belt.OuterRadius)
                throw new ScenarioException("geography/v2: corrected start exceeds the human belt resource envelope.");
            return (Inner: inner, Outer: outer);
        }).OrderBy(l => l.Inner).ToArray();
        for (int i = 1; i < lanes.Length; i++)
            if (lanes[i].Inner - lanes[i - 1].Outer < config.Clusters!.MinimumStationDays * units + 2 * resourcePadding)
                throw new ScenarioException("geography/v2: corrected start violates reserved radial lanes.");
        player = objects[state.PlayerShipObjectId];
        double actual = double.Hypot(player.PositionX, player.PositionY);
        double tolerance = 1e-6 + actual * 1e-10;
        if (actual + tolerance < config.StartMinDays * units || actual - tolerance > config.StartMaxDays * units)
            throw new ScenarioException("geography/v2: actual player start outside configured solar distance.");
        if ((original.GameState.TradingMapGeneration ?? original.GameState.TradingMap?.Rules) is { } request)
        {
            var graph = TradingGraphGenerator.Generate(request, seed, registry);
            var geometry = TradingMapRandom.DrawIndex(seed, "TradingMap.Geometry", 4);
            var risks = graph.Rules.RiskProfiles.ToDictionary(r => r.RiskProfileId, StringComparer.Ordinal);
            var edges = graph.Template.Links.Select(link =>
            {
                var a = objects[link.FromStationObjectId]; var b = objects[link.ToStationObjectId];
                double distance = double.Hypot(a.PositionX - b.PositionX, a.PositionY - b.PositionY) / 10;
                double milliseconds = distance / (graph.Rules.ReferenceSpeedMps / 1000) * 1000 * 300;
                if (!double.IsFinite(milliseconds) || milliseconds <= 0 || milliseconds >= long.MaxValue)
                    throw new ScenarioException("geography/v2: trading estimate exceeds supported range.");
                long estimate = checked((long)Math.Ceiling(milliseconds));
                var risk = risks[link.RiskProfileId];
                return new TradingMapEdgeData(a.ObjectId, b.ObjectId, distance, estimate,
                    estimate <= graph.Rules.ShortMaxGameTimeMs ? "Short" : estimate <= graph.Rules.MediumMaxGameTimeMs ? "Medium" : "Long",
                    risk.FuelMultiplierPermille, risk.RiskProfileId);
            }).OrderBy(e => e.FromStationObjectId, StringComparer.Ordinal).ThenBy(e => e.ToStationObjectId, StringComparer.Ordinal).ToArray();
            state = state with
            {
                TradingMapGeneration = null,
                TradingMap = new(1, graph.Template.TemplateId, geometry.Value, graph.Rules, edges, graph.CargoFlows,
                    [.. new[] { graph.TopologyStream, geometry.State }.OrderBy(s => s.Name, StringComparer.Ordinal)])
            };
        }
        world = world with { GameState = state };
        SolarSystemGeneration.ValidateWorld(state);
        return BuildGraph(candidate with { World = world }, config.Clusters!, registry);
    }

    internal static ClusterGenerationResult BuildGraph(ClusterGenerationResult candidate, ClusterGenerationConfig config, GameDataRegistry registry)
    {
        var map = candidate.Map;
        var memberById = map.Stations.ToDictionary(s => s.ObjectId, StringComparer.Ordinal);
        var indices = map.Clusters.Select((c, i) => (c.Id, i)).ToDictionary(c => c.Id, c => c.i, StringComparer.Ordinal);
        double units = map.Geography!.ReferenceSpeedKmS * 86400 / 300 * 10;
        var stations = candidate.World.GameState.SpaceObjects.Where(o => memberById.ContainsKey(o.ObjectId)).ToArray();
        var links = StationClusterGenerator.BuildLinks(stations, registry, (a, b) =>
        {
            string ca = memberById[a.ObjectId].ClusterId, cb = memberById[b.ObjectId].ClusterId;
            if (ca != cb) return Math.Abs(indices[ca] - indices[cb]) == 1;
            double distance = double.Hypot(a.PositionX - b.PositionX, a.PositionY - b.PositionY);
            double tolerance = 1e-6 + distance * 1e-10;
            return distance + tolerance >= config.NeighbourMinDays * units && distance - tolerance <= config.NeighbourMaxDays * units;
        });
        var result = candidate with { Map = map with { Links = links } };
        ValidateGraph(result.Map, registry);
        return result;
    }

    internal static void ValidateGraph(StationClusterMapSnapshot map, GameDataRegistry registry)
    {
        foreach (var station in map.Stations)
        {
            var outbound = map.Links.Where(l => l.FromStationId == station.ObjectId).ToArray();
            var profile = registry.StationMarketProfiles.GetDefinition(registry.StationMarketProfiles.GetIndex(station.MarketProfileId));
            int minimumDirections = map.Clusters.Length > 1 ? 2 : 1;
            if (outbound.Length < minimumDirections || !profile.SupplyItemTypeIds.IsEmpty && outbound.All(l => l.ItemTypeIds.IsEmpty))
                throw new ScenarioException($"geography/v2: station {station.ObjectId} has no eligible outbound consumer/return direction.");
        }
        foreach (bool reverse in new[] { false, true })
        {
            var seen = new HashSet<string>(StringComparer.Ordinal) { map.Stations[0].ObjectId };
            var queue = new Queue<string>(); queue.Enqueue(map.Stations[0].ObjectId);
            while (queue.TryDequeue(out string? id))
                foreach (var link in map.Links.Where(l => (reverse ? l.ToStationId : l.FromStationId) == id))
                {
                    string next = reverse ? link.FromStationId : link.ToStationId;
                    if (seen.Add(next)) queue.Enqueue(next);
                }
            if (seen.Count != map.Stations.Length) throw new ScenarioException("geography/v2: trading graph has no outbound/return path.");
        }
        int pairs = map.Links.Select(l => string.CompareOrdinal(l.FromStationId, l.ToStationId) < 0
            ? (l.FromStationId, l.ToStationId) : (l.ToStationId, l.FromStationId)).Distinct().Count();
        if (pairs - map.Stations.Length + 1 < 2) throw new ScenarioException("geography/v2: fewer than two independent trading cycles.");
    }

    internal static ClusterGenerationResult Expand(ClusterGenerationResult first, ClusterGenerationConfig config,
        GameDataRegistry registry, ulong seed, int attempt, double formationPadding = 0)
    {
        int count = ClusterCount(config, seed);
        if (count == 1) return first;
        var state = first.World.GameState;
        var system = state.SolarSystem!;
        var belt = system.Belts.Single(b => b.Id == first.Map.Clusters[0].BeltId);
        var player = state.SpaceObjects.Single(o => o.ObjectId == state.PlayerShipObjectId);
        var home = first.Map.Clusters[0].StationIds.Select(id => state.SpaceObjects.Single(o => o.ObjectId == id)).ToArray();
        double units = first.Map.Geography!.ReferenceSpeedKmS * 86400 / 300 * 10;
        double radius = double.Hypot(player.PositionX, player.PositionY);
        double padding = Math.Max(Math.Max(200, formationPadding), (state.StationResourceFields?.Rules.OuterRadiusKm ?? 0) * 10);
        double envelope = LaneHalfWidth(config, units) + padding;
        double laneSpacing = config.MinimumStationDays * units * 1.25 + 2 * envelope;
        var lanes = Enumerable.Range(1, count - 1).Select(i => i % 2 == 1 ? (i + 1) / 2 : -i / 2)
            .OrderByDescending(i => i > 0).ThenBy(i => Math.Abs(i)).Select(i => radius + i * laneSpacing).ToArray();
        if (new[] { radius }.Concat(lanes).Any(r => r - envelope < belt.InnerRadius || r + envelope > belt.OuterRadius))
            throw new ScenarioException("geography/v2: human belt cannot fit separated radial lanes and resource envelopes.");
        double centerX = home.Average(o => o.PositionX), centerY = home.Average(o => o.PositionY);
        double centerRadius = double.Hypot(centerX, centerY);
        double phase = Math.Atan2(centerX, -centerY);
        double spacing = (config.InterclusterMinDays + config.InterclusterMaxDays) / 2 * units;
        var clusters = first.Map.Clusters.ToBuilder();
        var members = first.Map.Stations.ToBuilder();
        var links = first.Map.Links.ToBuilder();
        var objects = state.SpaceObjects.ToList();
        var ids = objects.Select(o => o.ObjectId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (int index = 1; index < count; index++)
        {
            string id = $"CLUSTER-{index + 1}";
            double r = lanes[index - 1];
            var isolated = first.World with
            {
                GameState = state with
                {
                    TradingMap = null, TradingMapGeneration = null, ClusterMap = null, StationResourceFields = null,
                    SpaceObjects = [.. state.SpaceObjects.Where(o => o.ObjectType != SpaceObjectType.Station).Select(o => o.ObjectId == player.ObjectId
                        ? o with { PositionX = r * Math.Sin(phase), PositionY = -r * Math.Cos(phase), IsDocked = false, DockedStationObjectId = null } : o)]
                }
            };
            ulong groupSeed = RngStreamSeedDerivation.DeriveStreamSeed(seed, $"station-clusters/v2/{id}");
            var group = Place(isolated, config with { MinClusters = 1, MaxClusters = 1 }, registry, groupSeed, attempt);
            var stations = group.World.GameState.SpaceObjects.Where(o => group.Map.Clusters[0].StationIds.Contains(o.ObjectId)).ToArray();
            double groupX = stations.Average(s => s.PositionX), groupY = stations.Average(s => s.PositionY);
            double groupRadius = double.Hypot(groupX, groupY);
            double centerPhase = Math.Atan2(groupX, -groupY);
            // Compute the chord between actual district centers on their different radii.
            double radialGap = groupRadius - centerRadius;
            double phaseStep = 2 * Math.Asin(Math.Sqrt((spacing * spacing - radialGap * radialGap) / (4 * centerRadius * groupRadius)));
            if (!double.IsFinite(phaseStep)) throw new ScenarioException("geography/v2: cluster centers cannot fit the human belt.");
            phase += phaseStep;
            centerRadius = groupRadius;
            stations = stations.Select(s =>
            {
                double p = (s.Orbit!.InitialPhase + s.Orbit.PhaseOffsetDegrees) * Math.PI / 180 + phase - centerPhase;
                double degrees = (p * 180 / Math.PI % 360 + 360) % 360;
                double stationRadius = s.Orbit.SemiMajorAxis;
                return s with
                {
                    ObjectId = s.ObjectId.Replace("CLUSTER-1", id, StringComparison.Ordinal), Name = $"District {index + 1} {s.Name}",
                    PositionX = stationRadius * Math.Sin(p), PositionY = -stationRadius * Math.Cos(p),
                    Orbit = s.Orbit with { InitialPhase = (int)degrees, PhaseOffsetDegrees = degrees - (int)degrees }
                };
            }).ToArray();
            foreach (var station in stations)
                if (!ids.Add(station.ObjectId)) throw new ScenarioException($"geography/v2: generated ID collision {station.ObjectId}.");
            string specialization = stations.GroupBy(s => s.MarketProfileId).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).First().Key!;
            clusters.Add(new(id, $"District {index + 1}", belt.Id, [.. stations.Select(s => s.ObjectId)], specialization));
            members.AddRange(stations.Select(s => new ClusterStationData(s.ObjectId, id, s.MarketProfileId!)));
            links.AddRange(group.Map.Links.Select(l => l with
            {
                Id = l.Id.Replace("CLUSTER-1", id, StringComparison.Ordinal),
                FromStationId = l.FromStationId.Replace("CLUSTER-1", id, StringComparison.Ordinal), ToStationId = l.ToStationId.Replace("CLUSTER-1", id, StringComparison.Ordinal)
            }));
            objects.AddRange(stations);
        }
        var centers = clusters.Select(c =>
        {
            var stations = objects.Where(o => c.StationIds.Contains(o.ObjectId)).ToArray();
            return (X: stations.Average(o => o.PositionX), Y: stations.Average(o => o.PositionY));
        }).ToArray();
        for (int i = 0; i < centers.Length; i++)
        {
            double nearest = centers.Where((_, j) => j != i).Min(c => double.Hypot(c.X - centers[i].X, c.Y - centers[i].Y)) / units;
            if (nearest < config.InterclusterMinDays || nearest > config.InterclusterMaxDays)
                throw new ScenarioException("geography/v2: initial cluster-center neighbour outside configured days.");
        }
        var world = first.World with
        {
            GameState = state with
            {
                SpaceObjects = [.. objects.OrderBy(o => o.ObjectId, StringComparer.Ordinal)],
                SolarSystem = system with { Orbits = [.. objects.Where(o => o.Orbit is not null).OrderBy(o => o.ObjectId, StringComparer.Ordinal).Select(o => new OrbitMapData(o.ObjectId, o.Orbit!))] }
            }
        };
        SolarSystemGeneration.ValidateWorld(world.GameState);
        return new(world, first.Map with { Clusters = clusters.ToImmutable(), Stations = members.ToImmutable(), Links = links.ToImmutable() });
    }

    internal static ClusterGenerationResult Place(ScenarioFile source, ClusterGenerationConfig config,
        GameDataRegistry registry, ulong seed, int attempt)
    {
        StationClusterGenerator.ValidateConfig(config);
        var state = source.GameState;
        var system = state.SolarSystem ?? throw new ScenarioException("geography/v2: solar candidate required.");
        if (system.GeneratorVersion != 2 || !double.IsFinite(config.MinimumStationDays) ||
            config.MinimumStationDays < 1 || config.NeighbourMinDays < config.MinimumStationDays)
            throw new ScenarioException("geography/v2: inconsistent version or minimum distance.");
        foreach (string profile in StationClusterGenerator.Profiles)
            if (!registry.StationMarketProfiles.Contains(profile))
                throw new ScenarioException($"geography/v2: missing profile {profile}.");
        var player = state.SpaceObjects.Single(o => o.ObjectId == state.PlayerShipObjectId);
        double speed = SolarSystemGenerator.OperationalMaxSpeedKmS(player, registry) ??
            throw new ScenarioException("geography/v2: no operational engine Vmax.");
        if (!double.IsFinite(speed) || speed <= 0) throw new ScenarioException("geography/v2: invalid reference speed.");
        double unitsPerDay = speed * 86400 / 300 * 10;
        double radius = double.Hypot(player.PositionX, player.PositionY);
        var belt = system.Belts.SingleOrDefault(b => radius >= b.InnerRadius && radius <= b.OuterRadius) ??
            throw new ScenarioException("geography/v2: player is outside the human belt.");
        var stations = MaterializeStationIdentities(source, registry, seed).ToList();
        var rng = SolarSystemGenerator.GeographyRng(seed, "roles", attempt);
        int count = rng.NextInt(config.MinStations, config.MaxStations + 1);
        if (stations.Count > count) throw new ScenarioException("geography/v2: source stations exceed quota.");
        var ids = state.SpaceObjects.Select(o => o.ObjectId).Concat(stations.Select(s => s.ObjectId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = StationClusterGenerator.Profiles.Where(p => stations.All(s => s.MarketProfileId != p)).ToArray();
        if (missing.Length > count - stations.Count) throw new ScenarioException("geography/v2: required roles exceed quota.");
        int generated = count - stations.Count;
        for (int i = 0; i < generated; i++)
        {
            string id = $"CLUSTER-1-STATION-{i + 1}";
            if (!ids.Add(id)) throw new ScenarioException($"geography/v2: ID collision {id}.");
            string profile = i < missing.Length ? missing[i] : StationClusterGenerator.Profiles[rng.NextInt(0, StationClusterGenerator.Profiles.Length)];
            stations.Add(new(id, "Station", "Permanent", $"Home {i + 1}", 0, 0, 0, 0,
                "Stationary", null, null, null, IsKnown: true, StationSize: "Medium", MarketProfileId: profile,
                PortFeeCreditsPerDay: stations.FirstOrDefault()?.PortFeeCreditsPerDay ?? 100));
        }
        stations.Sort((a, b) => StringComparer.Ordinal.Compare(a.ObjectId, b.ObjectId));
        string? anchorId = state.TradingMap?.Rules.StartStationObjectId ?? state.TradingMapGeneration?.StartStationObjectId ??
            player.DockedStationObjectId ?? stations.OrderBy(s => StationClusterGenerator.Distance(player, s))
                .ThenBy(s => s.ObjectId, StringComparer.Ordinal).FirstOrDefault()?.ObjectId;
        var anchor = stations.FirstOrDefault(s => s.ObjectId == anchorId);
        double phase = anchor is null || anchor.PositionX == 0 && anchor.PositionY == 0
            ? Math.Atan2(player.PositionX, -player.PositionY) : Math.Atan2(anchor.PositionX, -anchor.PositionY);
        int anchorIndex = Math.Max(0, stations.FindIndex(s => s.ObjectId == anchorId));
        double spacing = FormationSpacing(config, unitsPerDay);
        var orientation = SolarSystemGenerator.GeographyRng(seed, "orientation", attempt);
        phase += (orientation.NextDouble() - 0.5) * spacing / radius * 0.1;
        var reference = system.Orbits.OrderBy(o => Math.Abs(o.Elements.SemiMajorAxis - radius)).FirstOrDefault()?.Elements ??
            throw new ScenarioException("geography/v2: orbital reference missing.");
        long period;
        try { period = checked((long)Math.Round(reference.OrbitalPeriodMs * (radius / reference.SemiMajorAxis))); }
        catch (OverflowException) { throw new ScenarioException("geography/v2: orbital period overflow."); }
        if (period <= 0) throw new ScenarioException("geography/v2: nonpositive orbital period.");
        var scatter = SolarSystemGenerator.GeographyRng(seed, "station-scatter", attempt);
        const int anchorSlot = AnchorRow * FormationColumns + FormationColumns / 2;
        // Keep opposite corners at every supported station count so the diameter stays stable.
        var slots = Enumerable.Range(0, FormationRows * FormationColumns)
            .Where(slot => slot != anchorSlot && (count == 12 || slot != 2) && (count > 10 || slot != 9)).ToArray();
        for (int i = slots.Length - 1; i > 0; i--)
        {
            int j = scatter.NextInt(0, i + 1);
            (slots[i], slots[j]) = (slots[j], slots[i]);
        }
        int nextSlot = 0;
        for (int i = 0; i < stations.Count; i++)
        {
            bool isAnchor = i == anchorIndex;
            int slot = isAnchor ? anchorSlot : slots[nextSlot++];
            int row = slot / FormationColumns;
            double radialOffset = (row - AnchorRow) * RadialRowSpacing;
            // Stagger adjacent rows so diagonal neighbours are reachable too.
            double tangentOffset = slot % FormationColumns - FormationColumns / 2 - (row % 2 == 0 ? 0.5 : 0);
            double stationRadius = isAnchor ? radius : radius + (radialOffset + (scatter.NextDouble() * 2 - 1) * PositionJitter) * spacing;
            double tangent = isAnchor ? 0 : (tangentOffset + (scatter.NextDouble() * 2 - 1) * PositionJitter) * spacing;
            double angle = phase + Math.Asin(tangent / stationRadius);
            if (stationRadius <= 0 || !double.IsFinite(angle))
                throw new ScenarioException("geography/v2: formation cannot fit around the selected radius.");
            double degrees = (angle * 180 / Math.PI % 360 + 360) % 360;
            var orbit = new OrbitalElements(stationRadius, stationRadius, period, (int)degrees, degrees - (int)degrees,
                state.MotionTimeMs, "clockwise");
            stations[i] = stations[i] with
            {
                PositionX = stationRadius * Math.Sin(angle), PositionY = -stationRadius * Math.Cos(angle),
                MovementType = "Orbital", Orbit = orbit, SpeedMps = 0, WorldOffsetX = 0, WorldOffsetY = 0
            };
        }
        ValidateDistances(stations, config, unitsPerDay);
        var byId = stations.ToDictionary(s => s.ObjectId, StringComparer.Ordinal);
        var existingIds = state.SpaceObjects.Select(o => o.ObjectId).ToHashSet(StringComparer.Ordinal);
        var objects = state.SpaceObjects.Select(o => byId.GetValueOrDefault(o.ObjectId, o))
            .Concat(stations.Where(s => !existingIds.Contains(s.ObjectId))).OrderBy(o => o.ObjectId, StringComparer.Ordinal).ToArray();
        var links = StationClusterGenerator.BuildLinks(stations, registry, (a, b) =>
            StationClusterGenerator.Distance(a, b) / unitsPerDay >= config.NeighbourMinDays - 1e-10 &&
            StationClusterGenerator.Distance(a, b) / unitsPerDay <= config.NeighbourMaxDays + 1e-10);
        var map = new StationClusterMapSnapshot(2, "CLUSTER-1",
            [new("CLUSTER-1", "Home", belt.Id, [.. stations.Select(s => s.ObjectId)], "balanced")],
            [.. stations.Select(s => new ClusterStationData(s.ObjectId, "CLUSTER-1", s.MarketProfileId!))], links,
            Geography: new(speed, config.MinimumStationDays, config.NeighbourMinDays, config.NeighbourMaxDays));
        var world = source with
        {
            GameState = state with
            {
                SpaceObjects = objects,
                SolarSystem = system with { Orbits = [.. objects.Where(o => o.Orbit is not null).Select(o => new OrbitMapData(o.ObjectId, o.Orbit!))] }
            }
        };
        SolarSystemGeneration.ValidateWorld(world.GameState);
        return new(world, map);
    }

    internal static IReadOnlyList<SpaceObjectData> MaterializeStationIdentities(ScenarioFile source, GameDataRegistry registry, ulong seed)
    {
        var stations = source.GameState.SpaceObjects.Where(o => o.ObjectType == SpaceObjectType.Station)
            .OrderBy(o => o.ObjectId, StringComparer.Ordinal).ToDictionary(o => o.ObjectId, StringComparer.Ordinal);
        var request = source.GameState.TradingMapGeneration;
        if (request is not null)
        {
            var graph = TradingGraphGenerator.Generate(request, seed, registry);
            foreach (var rule in graph.Rules.Stations)
            {
                if (source.GameState.SpaceObjects.Any(o => string.Equals(o.ObjectId, rule.ObjectId, StringComparison.OrdinalIgnoreCase) &&
                    o.ObjectType != SpaceObjectType.Station))
                    throw new ScenarioException($"geography/v2: station ID collides with non-station {rule.ObjectId}.");
                if (!stations.ContainsKey(rule.ObjectId))
                    stations.Add(rule.ObjectId, new(rule.ObjectId, "Station", "Permanent", rule.Name, 0, 0, 0, 0,
                        "Stationary", null, null, null, IsKnown: true, StationSize: rule.StationSize, MarketProfileId: rule.MarketProfileId));
                else if (stations[rule.ObjectId].MarketProfileId is null)
                    stations[rule.ObjectId] = stations[rule.ObjectId] with { MarketProfileId = rule.MarketProfileId };
            }
        }
        return stations.Values.OrderBy(s => s.ObjectId, StringComparer.Ordinal).Select((s, i) =>
            s with { MarketProfileId = s.MarketProfileId ?? StationClusterGenerator.Profiles[i % StationClusterGenerator.Profiles.Length] }).ToArray();
    }

    private static void ValidateDistances(IReadOnlyList<SpaceObjectData> stations, ClusterGenerationConfig config, double unitsPerDay)
    {
        double diameter = 0;
        foreach (var station in stations)
        {
            double nearest = double.PositiveInfinity;
            foreach (var other in stations)
            {
                if (station.ObjectId == other.ObjectId) continue;
                double distance = StationClusterGenerator.Distance(station, other);
                double tolerance = 1e-6 + distance * 1e-10;
                if (!double.IsFinite(distance) || distance + tolerance < config.MinimumStationDays * unitsPerDay)
                    throw new ScenarioException("geography/v2: pair violates minimum station distance.");
                nearest = Math.Min(nearest, distance); diameter = Math.Max(diameter, distance);
            }
            if (nearest + 1e-6 < config.NeighbourMinDays * unitsPerDay || nearest - 1e-6 > config.NeighbourMaxDays * unitsPerDay)
                throw new ScenarioException("geography/v2: nearest neighbour outside configured days.");
        }
        if (diameter + 1e-6 < config.DiameterMinDays * unitsPerDay || diameter - 1e-6 > config.DiameterMaxDays * unitsPerDay)
            throw new ScenarioException("geography/v2: diameter outside configured days.");
    }
}
