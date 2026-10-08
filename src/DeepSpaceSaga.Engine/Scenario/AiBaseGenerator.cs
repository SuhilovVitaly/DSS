using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Scenario;

internal static class AiBaseGenerator
{
    internal static void ValidateConfig(AiGenerationConfig c)
    {
        if (c.MinBases < 2 || c.MaxBases < c.MinBases || c.MaxBases > 1024 ||
            c.MaxPlacementAttempts is < 1 or > 4096 ||
            !double.IsFinite(c.DefenceRadiusKm) || !double.IsFinite(c.PatrolRadiusKm) ||
            c.DefenceRadiusKm <= 0 || c.PatrolRadiusKm < c.DefenceRadiusKm ||
            !double.IsFinite(c.PatrolRadiusKm * 10))
            throw new ContentException("solarSystem.ai: invalid counts, radii or attempt limit.");
    }

    internal static GameStateData Generate(GameStateData source, SolarSystemGenerationConfig config,
        GameDataRegistry registry, ulong seed, out PlacementValidationResult? diagnostics)
    {
        ValidateConfig(config.Ai!);
        diagnostics = null;
        string reason = "ai_start_network_overlap";
        for (int attempt = 0; attempt < config.Ai!.MaxPlacementAttempts; attempt++)
        {
            var candidate = Place(source, config, registry, seed, attempt);
            var overlap = StartNetworkOverlap(candidate, candidate.MotionTimeMs);
            if (overlap is not null) { reason = overlap; continue; }
            if (candidate.ClusterMap is { } clusters)
            {
                diagnostics = AiTradePlacementValidator.Validate(new(new("placement", "placement"), candidate),
                    clusters, candidate.AiMap!, AiTradePlacementValidator.DefaultHorizon) with
                { Attempts = attempt + 1 };
                if (!diagnostics.IsValid) { reason = string.Join("; ", diagnostics.Violations); continue; }
            }
            return candidate;
        }
        throw new ScenarioException($"ai/v1 seed={seed} attempts={config.Ai.MaxPlacementAttempts}: {reason}.");
    }

    private static GameStateData Place(GameStateData source, SolarSystemGenerationConfig config,
        GameDataRegistry registry, ulong seed, int attempt)
    {
        var c = config.Ai!;
        ValidateConfig(c);
        var map = source.SolarSystem ?? throw new ScenarioException("ai/v1: solar system required.");
        if (source.AiMap is not null) throw new ScenarioException("ai/v1: already materialized.");
        var planets = map.Planets.OrderBy(p => p.ObjectId, StringComparer.Ordinal).ToArray();
        if (planets.Length < 2) throw new ScenarioException("ai/v1: insufficient planets.");
        var rng = new SolarSystemGenerator.GeneratorRng(seed, "ai/counts", 0);
        int count = rng.NextInt(c.MinBases, c.MaxBases + 1);
        int planetary = rng.NextInt(1, Math.Min(planets.Length - 1, count - 1) + 1);
        var player = source.SpaceObjects.Single(o => o.ObjectId == source.PlayerShipObjectId);
        var drive = (player.Modules ?? []).OrderBy(m => m.ModuleId, StringComparer.Ordinal)
            .Where(m => m.PowerState == "On" && m.OperationalState == "Ready" && m.StructurePoints > 0)
            .Select(m => registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex(m.ModuleTypeId)))
            .FirstOrDefault(m => m.MaxSpeedMps is > 0);
        if (drive?.MaxSpeedMps is not > 0) throw new ScenarioException("ai/v1: operational engine required.");
        var objects = source.SpaceObjects.ToList();
        var ids = objects.Select(o => o.ObjectId).Concat(map.Belts.Select(b => b.Id))
            .Concat(source.ClusterMap?.Clusters.Select(x => x.Id) ?? [])
            .Concat(source.ClusterMap?.Links.Select(x => x.Id) ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var bases = ImmutableArray.CreateBuilder<AiBaseMapData>();
        var orbits = map.Orbits.ToBuilder();
        var placement = new SolarSystemGenerator.GeneratorRng(seed, "ai/placement", attempt);
        // Shuffle only the detached planet list; neither prior stages nor their streams change.
        for (int i = planets.Length - 1; i > 0; i--)
        {
            int j = placement.NextInt(0, i + 1);
            (planets[i], planets[j]) = (planets[j], planets[i]);
        }
        for (int i = 0; i < count; i++)
        {
            string id = $"SYS-AI-{i + 1}";
            if (!ids.Add(id)) throw new ScenarioException($"ai/v1 seed={seed} stage=ids: collision {id}.");
            string? parent = i < planetary ? planets[i].ObjectId : null;
            OrbitalElements orbit;
            if (parent is not null)
                orbit = objects.Single(o => o.ObjectId == parent).Orbit ?? throw new ScenarioException("ai/v1: planet orbit missing.");
            else
            {
                double radius = map.SystemRadius * (0.1 + placement.NextDouble() * 0.8);
                orbit = SolarSystemGenerator.CircularOrbit(radius, placement.NextDouble() * 360,
                    drive.MaxSpeedMps.Value / 1000.0, config, source.MotionTimeMs);
            }
            var pose = OrbitalMotionMath.At(new(id, 0, 0, 0, 0), orbit, source.MotionTimeMs);
            bases.Add(new(id, parent is null ? "Orbital" : "Planetary", "Ai", parent,
                parent is null ? orbit : null, 0, 0));
            objects.Add(new(id, "Station", "Permanent", $"База ИИ {i + 1}", pose.X, pose.Y, 0, 0,
                "Orbital", null, null, null, IsKnown: true, Credits: 0, Inventory: [], ProducingModules: [],
                StationCrew: [], Orbit: orbit));
            orbits.Add(new(id, orbit));
        }
        var result = source with
        {
            SpaceObjects = objects.OrderBy(o => o.ObjectId, StringComparer.Ordinal).ToArray(),
            SolarSystem = map with { Orbits = orbits.ToImmutable() },
            AiMap = new(1, bases.ToImmutable(), bases.Select((b, i) =>
                new TerritoryMapData($"SYS-TERRITORY-{i + 1}", b.ObjectId, c.DefenceRadiusKm, c.PatrolRadiusKm)).ToImmutableArray())
        };
        SolarSystemGeneration.ValidateWorld(result);
        ValidateWorld(result);
        return result;
    }

    internal static ObjectMotionSnapshot Pose(SpaceObjectData obj, long time) => obj.Orbit is { } orbit
        ? OrbitalMotionMath.At(new(obj.ObjectId, obj.PositionX, obj.PositionY, 0, 0,
            WorldOffsetX: obj.WorldOffsetX, WorldOffsetY: obj.WorldOffsetY), orbit, time)
        : new(obj.ObjectId, obj.PositionX, obj.PositionY, obj.SpeedMps / 1000, obj.DirectionDegrees);

    internal static double SegmentDistance(double x, double y, ObjectMotionSnapshot a, ObjectMotionSnapshot b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        double lengthSquared = dx * dx + dy * dy;
        double fraction = lengthSquared == 0 ? 0 : Math.Clamp(((x - a.X) * dx + (y - a.Y) * dy) / lengthSquared, 0, 1);
        return Math.Sqrt(Math.Pow(x - a.X - fraction * dx, 2) + Math.Pow(y - a.Y - fraction * dy, 2));
    }

    internal static string? StartNetworkOverlap(GameStateData state, long time)
    {
        if (state.AiMap is not { } map || map.Territories.IsDefaultOrEmpty) return null;
        var objects = state.SpaceObjects.ToDictionary(o => o.ObjectId, StringComparer.OrdinalIgnoreCase);
        var home = state.ClusterMap?.Clusters.First(c => c.Id == state.ClusterMap.StartClusterId);
        var nodes = (home?.StationIds ?? []).Append(state.PlayerShipObjectId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var links = state.ClusterMap?.Links.Where(l => nodes.Contains(l.FromStationId) && nodes.Contains(l.ToStationId)) ?? [];
        foreach (var territory in map.Territories)
        {
            var center = Pose(objects[territory.BaseObjectId], time);
            double radius = territory.PatrolRadiusKm * 10;
            foreach (var id in nodes)
            {
                var p = Pose(objects[id], time);
                if (SegmentDistance(center.X, center.Y, p, p) <= radius)
                    return $"ai_start_network_overlap base={territory.BaseObjectId} node={id}";
            }
            foreach (var link in links)
                if (SegmentDistance(center.X, center.Y, Pose(objects[link.FromStationId], time), Pose(objects[link.ToStationId], time)) <= radius)
                    return $"ai_start_network_overlap base={territory.BaseObjectId} link={link.Id}";
        }
        return null;
    }

    internal static void ValidateWorld(GameStateData state)
    {
        if (state.AiMap is not { } map) return;
        if (map.RulesVersion != 1 || map.Bases.IsDefault)
            throw new ScenarioException("aiMap: unsupported version or missing bases.");
        var objects = state.SpaceObjects.ToDictionary(o => o.ObjectId, StringComparer.OrdinalIgnoreCase);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var descriptorIds = (state.SolarSystem?.Belts.Select(b => b.Id) ?? [])
            .Concat(state.ClusterMap is { } c && !c.Clusters.IsDefault ? c.Clusters.Where(x => x is not null).Select(x => x.Id) : [])
            .Concat(state.ClusterMap is { } l && !l.Links.IsDefault ? l.Links.Where(x => x is not null).Select(x => x.Id) : [])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var b in map.Bases)
        {
            if (b is null || string.IsNullOrWhiteSpace(b.ObjectId) || !ids.Add(b.ObjectId) || descriptorIds.Contains(b.ObjectId) || b.Owner != "Ai" ||
                b.BaseType is not ("Planetary" or "Orbital") || b.OffsetX != 0 || b.OffsetY != 0 ||
                !objects.TryGetValue(b.ObjectId, out var obj) || obj.ObjectType != "Station" ||
                !obj.IsKnown || obj.MarketProfileId is not null || (obj.Inventory?.Count ?? 0) != 0 ||
                (obj.ProducingModules?.Count ?? 0) != 0)
                throw new ScenarioException("aiMap.bases: invalid identity, owner, type or market.");
            OrbitalElements? expected;
            if (b.BaseType == "Planetary")
            {
                if (b.Orbit is not null || b.ParentObjectId is null ||
                    !objects.TryGetValue(b.ParentObjectId, out var parent) || parent.ObjectType != "Planet")
                    throw new ScenarioException("aiMap.bases: invalid planet parent.");
                expected = parent.Orbit;
            }
            else
            {
                if (b.ParentObjectId is not null) throw new ScenarioException("aiMap.bases: orbital base has parent.");
                expected = b.Orbit;
            }
            if (expected is null || obj.Orbit != expected || obj.WorldOffsetX != 0 || obj.WorldOffsetY != 0)
                throw new ScenarioException("aiMap.bases: inconsistent orbital binding.");
            SolarSystemGeneration.ValidateOrbit(expected, b.ObjectId);
        }
        if (!map.Territories.IsDefaultOrEmpty)
        {
            var clusterIds = state.ClusterMap is { } clusters && !clusters.Clusters.IsDefault
                ? clusters.Clusters.Where(c => c is not null).Select(c => c.Id) : [];
            var globalIds = objects.Keys.Concat(state.SolarSystem?.Belts.Select(b => b.Id) ?? [])
                .Concat(clusterIds)
                .Concat(state.ClusterMap is { } links && !links.Links.IsDefault ? links.Links.Where(l => l is not null).Select(l => l.Id) : [])
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var owners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in map.Territories)
                if (t is null || string.IsNullOrWhiteSpace(t.Id) || !globalIds.Add(t.Id) ||
                    string.IsNullOrWhiteSpace(t.BaseObjectId) || !ids.Contains(t.BaseObjectId) || !owners.Add(t.BaseObjectId) ||
                    !double.IsFinite(t.DefenceRadiusKm) || !double.IsFinite(t.PatrolRadiusKm * 10) ||
                    t.DefenceRadiusKm <= 0 || t.PatrolRadiusKm < t.DefenceRadiusKm)
                    throw new ScenarioException("aiMap.territories: invalid identity, base or radii.");
            if (!owners.SetEquals(ids)) throw new ScenarioException("aiMap.territories: incomplete base coverage.");
        }
    }
}
