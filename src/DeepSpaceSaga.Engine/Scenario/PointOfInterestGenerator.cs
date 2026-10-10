using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Scenario;

internal static class PointOfInterestGenerator
{
    internal static void ValidateConfig(IReadOnlyList<PoiTemplate> templates)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (templates.Count > 128 || templates.Any(t => t is null || string.IsNullOrWhiteSpace(t.Id) || !ids.Add(t.Id) ||
            string.IsNullOrWhiteSpace(t.Name) || string.IsNullOrWhiteSpace(t.Description)))
            throw new ContentException("solarSystem.poiTemplates: expected up to 128 unique IDs with nonblank names/descriptions.");
    }

    internal static GameStateData Generate(GameStateData state, IReadOnlyList<PoiTemplate> templates, ulong seed)
    {
        ValidateConfig(templates);
        var solar = state.SolarSystem ?? throw new ScenarioException("poi/v1: solar system required.");
        var map = state.AiMap ?? new AiMapEnvironmentSnapshot(1, []);
        if (!map.PointsOfInterest.IsDefaultOrEmpty) throw new ScenarioException("poi/v1: already materialized.");
        var resources = (state.StationResourceFields?.Asteroids ?? []).OrderBy(r => r.ObjectId, StringComparer.Ordinal).ToArray();
        var belts = solar.Belts.OrderBy(b => b.Id, StringComparer.Ordinal).ToArray();
        var points = ImmutableArray.CreateBuilder<PointOfInterestData>();
        foreach (var template in templates.OrderBy(t => t.Id, StringComparer.Ordinal))
        {
            var rng = new SolarSystemGenerator.GeneratorRng(seed, "poi/" + template.Id, 0);
            string? parent = null;
            OrbitalElements? orbit = null;
            double x = 0, y = 0;
            if (resources.Length > 0)
            {
                parent = resources[rng.NextInt(0, resources.Length)].ObjectId;
                double angle = rng.NextDouble() * Math.Tau, distance = 25 + rng.NextDouble() * 100;
                x = Math.Sin(angle) * distance; y = -Math.Cos(angle) * distance;
            }
            else
            {
                if (belts.Length == 0 || solar.Orbits.IsDefaultOrEmpty) throw new ScenarioException("poi/v1: belt orbital reference required.");
                var belt = belts[rng.NextInt(0, belts.Length)];
                double radius = belt.InnerRadius + rng.NextDouble() * (belt.OuterRadius - belt.InnerRadius);
                var reference = solar.Orbits.OrderBy(o => o.ObjectId, StringComparer.Ordinal).First().Elements;
                double period = Math.Ceiling(reference.OrbitalPeriodMs * (radius / reference.SemiMajorAxis));
                if (!double.IsFinite(period) || period < 1 || period >= long.MaxValue) throw new ScenarioException("poi/v1: orbital period out of range.");
                orbit = new(radius, radius, (long)period, rng.NextInt(0, 360), rng.NextDouble(), state.MotionTimeMs, "clockwise");
            }
            points.Add(new($"SYS-POI-{points.Count + 1}", template.Name, template.Description, parent, orbit, x, y));
        }
        var result = state with { AiMap = map with { PointsOfInterest = points.ToImmutable() } };
        ValidateWorld(result);
        return result;
    }

    internal static void ValidateWorld(GameStateData state)
    {
        if (state.AiMap is not { } map || map.PointsOfInterest.IsDefaultOrEmpty) return;
        var objects = state.SpaceObjects.ToDictionary(o => o.ObjectId, StringComparer.OrdinalIgnoreCase);
        var ids = objects.Keys.Concat(state.SolarSystem?.Belts.Select(b => b.Id) ?? [])
            .Concat(state.ClusterMap is { } c && !c.Clusters.IsDefault ? c.Clusters.Where(x => x is not null).Select(x => x.Id) : [])
            .Concat(state.ClusterMap is { } links && !links.Links.IsDefault ? links.Links.Where(x => x is not null).Select(x => x.Id) : [])
            .Concat(map.Territories.IsDefault ? [] : map.Territories.Select(t => t.Id))
            .Concat(map.Fields.IsDefault ? [] : map.Fields.Select(f => f.Id)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var p in map.PointsOfInterest)
        {
            if (p is null || string.IsNullOrWhiteSpace(p.ObjectId) || !ids.Add(p.ObjectId) ||
                string.IsNullOrWhiteSpace(p.Name) || string.IsNullOrWhiteSpace(p.Description) ||
                !double.IsFinite(p.OffsetX) || !double.IsFinite(p.OffsetY) ||
                (p.ParentObjectId is null) == (p.Orbit is null))
                throw new ScenarioException("aiMap.pointsOfInterest: invalid identity, text, offsets or one-of anchor.");
            if (p.ParentObjectId is { } parent && !objects.ContainsKey(parent))
                throw new ScenarioException($"aiMap.pointsOfInterest {p.ObjectId}: dangling parent or cycle.");
            if (p.Orbit is { } orbit) SolarSystemGeneration.ValidateOrbit(orbit, p.ObjectId);
        }
    }
}
