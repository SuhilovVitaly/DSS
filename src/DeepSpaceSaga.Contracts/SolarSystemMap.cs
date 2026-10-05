using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

/// <summary>Axes in world units; period in calendar milliseconds, epoch in motion milliseconds.
/// Phase is clockwise from up, with an additive fractional offset for compact orbital groups.</summary>
public sealed record OrbitalElements(
    [property: JsonPropertyName("semiMajorAxis")] double SemiMajorAxis,
    [property: JsonPropertyName("semiMinorAxis")] double SemiMinorAxis,
    [property: JsonPropertyName("orbitalPeriodMs")] long OrbitalPeriodMs,
    [property: JsonPropertyName("initialPhase")] int InitialPhase,
    [property: JsonPropertyName("phaseOffsetDegrees")] double PhaseOffsetDegrees,
    [property: JsonPropertyName("epochSimulationTimeMs")] long EpochSimulationTimeMs,
    [property: JsonPropertyName("orbitDirection")] string OrbitDirection);

public sealed record BeltMapData(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("innerRadius")] double InnerRadius,
    [property: JsonPropertyName("outerRadius")] double OuterRadius,
    [property: JsonPropertyName("decorationSeed")] ulong DecorationSeed,
    [property: JsonPropertyName("decorationSamples")] int DecorationSamples = 2048);

/// <summary>Kind is Rocky, Icy or Gas; visual radius is in world units.</summary>
public sealed record PlanetMapData(
    [property: JsonPropertyName("objectId")] string ObjectId,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("visualRadius")] double VisualRadius);

public sealed record OrbitMapData(
    [property: JsonPropertyName("objectId")] string ObjectId,
    [property: JsonPropertyName("elements")] OrbitalElements Elements);

/// <summary>Authoritative generated geography, independent of viewport and rendering.</summary>
public sealed record SolarSystemMapSnapshot(
    [property: JsonPropertyName("generatorVersion")] int GeneratorVersion,
    [property: JsonPropertyName("seed")] ulong Seed,
    [property: JsonPropertyName("systemRadius")] double SystemRadius,
    [property: JsonPropertyName("belts"), JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<BeltMapData>))]
    ImmutableArray<BeltMapData> Belts,
    [property: JsonPropertyName("planets"), JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<PlanetMapData>))]
    ImmutableArray<PlanetMapData> Planets,
    [property: JsonPropertyName("orbits"), JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<OrbitMapData>))]
    ImmutableArray<OrbitMapData> Orbits);
