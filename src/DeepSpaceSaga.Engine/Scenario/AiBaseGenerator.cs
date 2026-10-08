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
        GameDataRegistry registry, ulong seed)
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
            .Concat(source.ClusterMap?.Clusters.Select(x => x.Id) ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var bases = ImmutableArray.CreateBuilder<AiBaseMapData>();
        var orbits = map.Orbits.ToBuilder();
        var placement = new SolarSystemGenerator.GeneratorRng(seed, "ai/placement", 0);
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
            AiMap = new(1, bases.ToImmutable())
        };
        SolarSystemGeneration.ValidateWorld(result);
        ValidateWorld(result);
        return result;
    }

    internal static void ValidateWorld(GameStateData state)
    {
        if (state.AiMap is not { } map) return;
        if (map.RulesVersion != 1 || map.Bases.IsDefault)
            throw new ScenarioException("aiMap: unsupported version or missing bases.");
        var objects = state.SpaceObjects.ToDictionary(o => o.ObjectId, StringComparer.OrdinalIgnoreCase);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var b in map.Bases)
        {
            if (b is null || string.IsNullOrWhiteSpace(b.ObjectId) || !ids.Add(b.ObjectId) || b.Owner != "Ai" ||
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
    }
}
