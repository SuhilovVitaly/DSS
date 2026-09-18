using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>Economic identity of the immutable catalog; names and presentation are excluded.</summary>
public sealed record CatalogCompatibilityData(
    [property: JsonPropertyName("catalogVersion")] int CatalogVersion,
    [property: JsonPropertyName("rulesVersion")] int RulesVersion,
    [property: JsonPropertyName("fingerprint")] string Fingerprint);
