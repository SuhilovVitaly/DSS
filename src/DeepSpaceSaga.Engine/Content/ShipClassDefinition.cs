using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Engine.Content;

/// <summary>Explicit ship class identity; combat durability belongs to installed modules.</summary>
internal sealed record ShipClassDefinition(
    [property: JsonPropertyName("typeId")] string TypeId) : ITypeDefinition;
