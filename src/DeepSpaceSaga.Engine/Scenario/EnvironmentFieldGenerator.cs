using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Scenario;

internal static class EnvironmentFieldGenerator
{
    internal static void ValidateConfig(EnvironmentGenerationConfig c)
    {
        foreach (var (name, count) in new[] { ("radiationCount", c.RadiationCount), ("dustCount", c.DustCount), ("debrisCount", c.DebrisCount) })
            if (count is < 0 or > 64) throw new ContentException($"solarSystem.environment.{name}: expected 0..64.");
        foreach (var (name, radius) in new[] { ("radiationRadiusKm", c.RadiationRadiusKm), ("dustWidthKm", c.DustWidthKm), ("debrisRadiusKm", c.DebrisRadiusKm) })
            if (radius <= 0 || !double.IsFinite(radius * 10)) throw new ContentException($"solarSystem.environment.{name}: expected positive finite dimension.");
        if (!double.IsFinite(c.Intensity) || c.Intensity is < 0 or > 1)
            throw new ContentException("solarSystem.environment.intensity: expected 0..1.");
    }

    internal static GameStateData Generate(GameStateData state, EnvironmentGenerationConfig config, ulong seed)
    {
        ValidateConfig(config);
        var solar = state.SolarSystem ?? throw new ScenarioException("environment/v1: solar system required.");
        var map = state.AiMap ?? new AiMapEnvironmentSnapshot(1, []);
        if (!map.Fields.IsDefaultOrEmpty) throw new ScenarioException("environment/v1: already materialized.");
        var fields = ImmutableArray.CreateBuilder<EnvironmentFieldData>();
        var planets = state.SpaceObjects.Where(o => o.ObjectType == "Planet").OrderBy(o => o.ObjectId, StringComparer.Ordinal).ToArray();
        var belts = solar.Belts.OrderBy(b => b.Id, StringComparer.Ordinal).ToArray();
        foreach (string kind in new[] { "Radiation", "Dust", "Debris" })
        {
            var rng = new SolarSystemGenerator.GeneratorRng(seed, "environment/" + kind, 0);
            int count = kind == "Radiation" ? config.RadiationCount : kind == "Dust" ? config.DustCount : config.DebrisCount;
            for (int i = 0; i < count; i++)
            {
                string id = $"SYS-FIELD-{kind.ToUpperInvariant()}-{i + 1}";
                double inner = 0, outer = config.RadiationRadiusKm * 10, angle = 0, sweep = 360;
                string anchor = "Sun";
                string? parent = null;
                OrbitalElements? orbit = null;
                if (kind == "Dust")
                {
                    if (belts.Length == 0) throw new ScenarioException("environment/v1: dust requires belt.");
                    var belt = belts[rng.NextInt(0, belts.Length)];
                    double width = Math.Min(config.DustWidthKm * 10, belt.OuterRadius - belt.InnerRadius);
                    inner = belt.InnerRadius + rng.NextDouble() * (belt.OuterRadius - belt.InnerRadius - width);
                    outer = inner + width; angle = rng.NextDouble() * 360; sweep = 30 + rng.NextDouble() * 60;
                }
                else if (kind == "Debris")
                {
                    if (planets.Length == 0) throw new ScenarioException("environment/v1: debris requires orbital reference.");
                    var planet = planets[rng.NextInt(0, planets.Length)];
                    outer = config.DebrisRadiusKm * 10;
                    if (i % 2 == 0) { anchor = "Parent"; parent = planet.ObjectId; }
                    else
                    {
                        anchor = "Orbit";
                        orbit = (planet.Orbit ?? throw new ScenarioException("environment/v1: missing orbit.")) with
                        { InitialPhase = rng.NextInt(0, 360), PhaseOffsetDegrees = rng.NextDouble() };
                    }
                }
                fields.Add(new(id, kind, config.Intensity, anchor, parent, orbit, 0, 0, inner, outer, angle, sweep, rng.Next()));
            }
        }
        var result = state with { AiMap = map with { Fields = fields.OrderBy(f => f.Id, StringComparer.Ordinal).ToImmutableArray() } };
        ValidateWorld(result);
        return result;
    }

    internal static void ValidateWorld(GameStateData state)
    {
        if (state.AiMap is not { } map || map.Fields.IsDefaultOrEmpty) return;
        var objects = state.SpaceObjects.ToDictionary(o => o.ObjectId, StringComparer.OrdinalIgnoreCase);
        var ids = objects.Keys.Concat(state.SolarSystem?.Belts.Select(b => b.Id) ?? [])
            .Concat(state.ClusterMap is { } c && !c.Clusters.IsDefault ? c.Clusters.Where(x => x is not null).Select(x => x.Id) : [])
            .Concat(state.ClusterMap is { } links && !links.Links.IsDefault ? links.Links.Where(x => x is not null).Select(x => x.Id) : [])
            .Concat(map.Territories.IsDefault ? [] : map.Territories.Select(t => t.Id)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var f in map.Fields)
        {
            if (f is null || string.IsNullOrWhiteSpace(f.Id) || !ids.Add(f.Id) ||
                f.Kind is not ("Radiation" or "Dust" or "Debris") || !double.IsFinite(f.Intensity) || f.Intensity is < 0 or > 1 ||
                new[] { f.OffsetX, f.OffsetY, f.InnerRadius, f.OuterRadius, f.StartAngleDegrees, f.SweepDegrees }.Any(v => !double.IsFinite(v)) ||
                f.InnerRadius < 0 || f.OuterRadius <= f.InnerRadius || f.StartAngleDegrees is < 0 or >= 360 || f.SweepDegrees is <= 0 or > 360)
                throw new ScenarioException("aiMap.fields: invalid identity, kind, intensity or geometry.");
            switch (f.AnchorKind)
            {
                case "Parent" when f.Orbit is null && f.ParentObjectId is not null && objects.ContainsKey(f.ParentObjectId): break;
                case "Orbit" when f.ParentObjectId is null && f.Orbit is not null:
                    SolarSystemGeneration.ValidateOrbit(f.Orbit, f.Id); break;
                case "Sun" when f.ParentObjectId is null && f.Orbit is null && f.OffsetX == 0 && f.OffsetY == 0 &&
                    objects.Values.Any(o => o.ObjectType == "Sun"):
                    break;
                default: throw new ScenarioException($"aiMap.fields {f.Id}: invalid one-of anchor, dangling parent or cycle.");
            }
        }
    }
}
