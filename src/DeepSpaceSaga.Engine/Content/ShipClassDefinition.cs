using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Engine.Content;

/// <summary>
/// Explicit ship class identity. Hull durability is now an independent class attribute
/// (HullMax) — living quarters and other modules do not increase hull strength.
/// </summary>
internal sealed record ShipClassDefinition(
    [property: JsonPropertyName("typeId")] string TypeId,
    [property: JsonPropertyName("hullMax")] int? HullMax = null,
    [property: JsonPropertyName("tacticalManeuver")] int? TacticalManeuver = null,
    [property: JsonPropertyName("appearanceId")] string? AppearanceId = null) : ITypeDefinition;
