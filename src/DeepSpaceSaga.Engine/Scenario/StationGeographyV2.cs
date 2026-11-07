using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>Detached version-two station candidates. Never publishes a world or consumes live RNG.</summary>
internal static class StationGeographyV2
{
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
        double diameterDays = (config.DiameterMinDays + config.DiameterMaxDays) / 2;
        double ratio = diameterDays * unitsPerDay / (2 * radius);
        if (!double.IsFinite(ratio) || ratio <= 0 || ratio >= 1)
            throw new ScenarioException("geography/v2: diameter cannot fit a circular formation.");
        double step = 2 * Math.Asin(ratio) / (count - 1);
        var orientation = SolarSystemGenerator.GeographyRng(seed, "orientation", attempt);
        phase += (orientation.NextDouble() - 0.5) * step * 0.1;
        var reference = system.Orbits.OrderBy(o => Math.Abs(o.Elements.SemiMajorAxis - radius)).FirstOrDefault()?.Elements ??
            throw new ScenarioException("geography/v2: orbital reference missing.");
        long period;
        try { period = checked((long)Math.Round(reference.OrbitalPeriodMs * (radius / reference.SemiMajorAxis))); }
        catch (OverflowException) { throw new ScenarioException("geography/v2: orbital period overflow."); }
        if (period <= 0) throw new ScenarioException("geography/v2: nonpositive orbital period.");
        for (int i = 0; i < stations.Count; i++)
        {
            double angle = phase + (i - anchorIndex) * step;
            double degrees = (angle * 180 / Math.PI % 360 + 360) % 360;
            var orbit = new OrbitalElements(radius, radius, period, (int)degrees, degrees - (int)degrees,
                state.MotionTimeMs, "clockwise");
            stations[i] = stations[i] with
            {
                PositionX = radius * Math.Sin(angle), PositionY = -radius * Math.Cos(angle),
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
