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
    ImmutableArray<AiBaseMapData> Bases);
