using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

/// <summary>A known AI station. Ownership is independent of human market profiles.
/// Planetary bases reference a planet; orbital bases carry their own orbit.
/// Offsets use world units (100 m), with the same epoch as the anchor.</summary>
public sealed record AiBaseMapData(
    [property: JsonPropertyName("objectId")] string ObjectId,
    [property: JsonPropertyName("baseType")] string BaseType,
    [property: JsonPropertyName("owner")] string Owner,
    [property: JsonPropertyName("parentObjectId")] string? ParentObjectId,
    [property: JsonPropertyName("orbit")] OrbitalElements? Orbit,
    [property: JsonPropertyName("offsetX")] double OffsetX,
    [property: JsonPropertyName("offsetY")] double OffsetY);

/// <summary>Resolved informational map descriptors. Absent in legacy snapshots.</summary>
public sealed record AiMapEnvironmentSnapshot(
    [property: JsonPropertyName("rulesVersion")] int RulesVersion,
    [property: JsonPropertyName("bases"), JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<AiBaseMapData>))]
    ImmutableArray<AiBaseMapData> Bases,
    [property: JsonPropertyName("territories"), JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<TerritoryMapData>))]
    ImmutableArray<TerritoryMapData> Territories = default,
    [property: JsonPropertyName("fields"), JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<EnvironmentFieldData>))]
    ImmutableArray<EnvironmentFieldData> Fields = default);

/// <summary>Informational circles centered on the referenced base's current pose.
/// Radii are km, never world coordinates; overlapping circles retain both contributors.</summary>
public sealed record TerritoryMapData(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("baseObjectId")] string BaseObjectId,
    [property: JsonPropertyName("defenceRadiusKm")] double DefenceRadiusKm,
    [property: JsonPropertyName("patrolRadiusKm")] double PatrolRadiusKm);

/// <summary>Informational circle or annular sector. Dimensions are world units (100 m),
/// angles clockwise from up; intensity is in [0,1]. AnchorKind is Parent, Orbit or Sun.
/// DecorationSeed affects the pattern only. No gameplay modifiers are carried.</summary>
public sealed record EnvironmentFieldData(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("intensity")] double Intensity,
    [property: JsonPropertyName("anchorKind")] string AnchorKind,
    [property: JsonPropertyName("parentObjectId")] string? ParentObjectId,
    [property: JsonPropertyName("orbit")] OrbitalElements? Orbit,
    [property: JsonPropertyName("offsetX")] double OffsetX,
    [property: JsonPropertyName("offsetY")] double OffsetY,
    [property: JsonPropertyName("innerRadius")] double InnerRadius,
    [property: JsonPropertyName("outerRadius")] double OuterRadius,
    [property: JsonPropertyName("startAngleDegrees")] double StartAngleDegrees,
    [property: JsonPropertyName("sweepDegrees")] double SweepDegrees,
    [property: JsonPropertyName("decorationSeed")] ulong DecorationSeed);
