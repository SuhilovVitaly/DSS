using System.Text.Json.Serialization;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Scenario;

public sealed record SolarSystemGenerationConfig(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("generatorVersion")] int GeneratorVersion,
    [property: JsonPropertyName("maxPlacementAttempts")] int MaxPlacementAttempts,
    [property: JsonPropertyName("minPlanets")] int MinPlanets,
    [property: JsonPropertyName("maxPlanets")] int MaxPlanets,
    [property: JsonPropertyName("minBelts")] int MinBelts,
    [property: JsonPropertyName("maxBelts")] int MaxBelts,
    [property: JsonPropertyName("startMinDays")] double StartMinDays,
    [property: JsonPropertyName("startMaxDays")] double StartMaxDays,
    [property: JsonPropertyName("orbitSpeedFraction")] double OrbitSpeedFraction,
    [property: JsonPropertyName("beltWidthFraction")] double BeltWidthFraction,
    [property: JsonPropertyName("orbitClearanceWorld")] double OrbitClearanceWorld,
    [property: JsonPropertyName("asteroidsPerBelt")] int AsteroidsPerBelt,
    [property: JsonPropertyName("decorationSamplesPerBelt")] int DecorationSamplesPerBelt,
    [property: JsonPropertyName("enabledScenarios")] IReadOnlyList<string> EnabledScenarios);

public static class SolarSystemGeneration
{
    public static SolarSystemGenerationConfig ValidateConfig(SolarSystemGenerationConfig c)
    {
        if (c.SchemaVersion != 1 || c.GeneratorVersion != 1)
            throw new ContentException("solarSystem: unsupported schemaVersion/generatorVersion.");
        if (c.MaxPlacementAttempts <= 0 || c.MinPlanets < 3 || c.MaxPlanets > 7 || c.MaxPlanets < c.MinPlanets ||
            c.MinBelts < 2 || c.MaxBelts > 5 || c.MaxBelts < c.MinBelts ||
            !double.IsFinite(c.StartMinDays) || !double.IsFinite(c.StartMaxDays) ||
            c.StartMinDays < 50 || c.StartMaxDays > 75 || c.StartMaxDays < c.StartMinDays ||
            !double.IsFinite(c.OrbitSpeedFraction) || c.OrbitSpeedFraction <= 0 || c.OrbitSpeedFraction >= 1 ||
            !double.IsFinite(c.BeltWidthFraction) || c.BeltWidthFraction <= 0 || c.BeltWidthFraction >= 1 ||
            !double.IsFinite(c.OrbitClearanceWorld) || c.OrbitClearanceWorld < 0 ||
            c.AsteroidsPerBelt < 0 || c.DecorationSamplesPerBelt < 0 ||
            c.EnabledScenarios is null || c.EnabledScenarios.Any(string.IsNullOrWhiteSpace) ||
            c.EnabledScenarios.Distinct(StringComparer.OrdinalIgnoreCase).Count() != c.EnabledScenarios.Count)
            throw new ContentException("solarSystem: invalid generation ranges, dimensions or enabledScenarios.");
        return c;
    }

    public static void ValidateOrbit(OrbitalElements o, string path)
    {
        if (!double.IsFinite(o.SemiMajorAxis) || !double.IsFinite(o.SemiMinorAxis) ||
            o.SemiMinorAxis <= 0 || o.SemiMajorAxis < o.SemiMinorAxis || o.OrbitalPeriodMs <= 0 ||
            o.EpochSimulationTimeMs < 0 || o.InitialPhase is < 0 or > 359 ||
            !double.IsFinite(o.PhaseOffsetDegrees) || o.PhaseOffsetDegrees is < 0 or >= 1 ||
            o.OrbitDirection is not ("clockwise" or "counterclockwise"))
            throw new ScenarioException($"{path}: invalid orbital elements.");
    }

    internal static void ValidateWorld(GameStateData state)
    {
        foreach (var obj in state.SpaceObjects)
        {
            if (!double.IsFinite(obj.WorldOffsetX) || !double.IsFinite(obj.WorldOffsetY))
                throw new ScenarioException($"{obj.ObjectId}: invalid worldOffset.");
            if (obj.Orbit is { } orbit) ValidateOrbit(orbit, $"{obj.ObjectId}.orbit");
        }
        if (state.SolarSystem is not { } map) return;
        if (map.GeneratorVersion != 1 || !double.IsFinite(map.SystemRadius) || map.SystemRadius <= 0 ||
            map.Belts.IsDefault || map.Planets.IsDefault || map.Orbits.IsDefault ||
            state.MasterSeed != map.Seed)
            throw new ScenarioException("solarSystem: invalid version, seed, radius or arrays.");
        if (map.Planets.Length is < 3 or > 7 || map.Belts.Length is < 2 or > 5 ||
            state.SpaceObjects.Count(o => string.Equals(o.ObjectType, "Sun", StringComparison.OrdinalIgnoreCase)) != 1 ||
            state.SpaceObjects.Count(o => string.Equals(o.ObjectType, "Planet", StringComparison.OrdinalIgnoreCase)) != map.Planets.Length)
            throw new ScenarioException("solarSystem: invalid celestial counts.");
        var sun = state.SpaceObjects.Single(o => string.Equals(o.ObjectType, "Sun", StringComparison.OrdinalIgnoreCase));
        if (sun.Orbit is not null || sun.PositionX != 0 || sun.PositionY != 0 || sun.SpeedMps != 0)
            throw new ScenarioException("solarSystem: Sun must remain at the origin.");
        if (state.SpaceObjects.Any(o => o.WorldOffsetX != 0 || o.WorldOffsetY != 0 ||
            string.Equals(o.ObjectType, "Station", StringComparison.OrdinalIgnoreCase) && o.Orbit is null))
            throw new ScenarioException("solarSystem: invalid offset or missing station orbit.");
        var objects = state.SpaceObjects.ToDictionary(o => o.ObjectId, StringComparer.OrdinalIgnoreCase);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var belt in map.Belts)
        {
            if (belt is null || string.IsNullOrWhiteSpace(belt.Id) || !ids.Add(belt.Id) || objects.ContainsKey(belt.Id) ||
                !double.IsFinite(belt.InnerRadius) || !double.IsFinite(belt.OuterRadius) ||
                belt.DecorationSamples < 0 || belt.InnerRadius <= 0 || belt.OuterRadius <= belt.InnerRadius || belt.OuterRadius > map.SystemRadius)
                throw new ScenarioException("solarSystem.belts: invalid id or radii.");
        }
        var corridors = map.Belts.Select(b => (Inner: b.InnerRadius, Outer: b.OuterRadius)).ToList();
        ids.Clear();
        foreach (var planet in map.Planets)
        {
            if (planet is null || string.IsNullOrWhiteSpace(planet.ObjectId) || !ids.Add(planet.ObjectId) ||
                planet.Kind is not ("Rocky" or "Icy" or "Gas") ||
                !double.IsFinite(planet.VisualRadius) || planet.VisualRadius <= 0 ||
                !objects.TryGetValue(planet.ObjectId, out var obj) ||
                !string.Equals(obj.ObjectType, "Planet", StringComparison.OrdinalIgnoreCase))
                throw new ScenarioException("solarSystem.planets: invalid kind, radius or object reference.");
            if (obj.Orbit is not { } orbit || obj.WorldOffsetX != 0 || obj.WorldOffsetY != 0 ||
                orbit.SemiMinorAxis <= planet.VisualRadius || orbit.SemiMajorAxis + planet.VisualRadius > map.SystemRadius)
                throw new ScenarioException("solarSystem.planets: missing orbit or invalid radial extent.");
            corridors.Add((orbit.SemiMinorAxis - planet.VisualRadius, orbit.SemiMajorAxis + planet.VisualRadius));
        }
        var ordered = corridors.OrderBy(c => c.Inner).ToArray();
        for (int i = 1; i < ordered.Length; i++)
            if (ordered[i].Inner < ordered[i - 1].Outer)
                throw new ScenarioException("solarSystem: overlapping radial corridors.");
        ids.Clear();
        foreach (var row in map.Orbits)
        {
            if (row is null || string.IsNullOrWhiteSpace(row.ObjectId) || !ids.Add(row.ObjectId) || row.Elements is null ||
                !objects.TryGetValue(row.ObjectId, out var obj) || obj.Orbit != row.Elements)
                throw new ScenarioException("solarSystem.orbits: invalid or inconsistent object reference.");
            ValidateOrbit(row.Elements, $"solarSystem.orbits.{row.ObjectId}");
            if (row.Elements.SemiMajorAxis > map.SystemRadius)
                throw new ScenarioException("solarSystem.orbits: orbit exceeds systemRadius.");
        }
        if (state.SpaceObjects.Any(o => o.Orbit is not null && !ids.Contains(o.ObjectId)))
            throw new ScenarioException("solarSystem.orbits: missing orbital object.");
    }
}
