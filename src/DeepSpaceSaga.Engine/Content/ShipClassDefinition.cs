using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Engine.Content;

/// <summary>Explicit content class identity and maximum hull hit points, independent of sprite/name.</summary>
internal sealed record ShipClassDefinition(
    [property: JsonPropertyName("typeId")] string TypeId,
    [property: JsonPropertyName("hullHitPointsMax")] int HullHitPointsMax) : ITypeDefinition;
