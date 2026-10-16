using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>Report allocator and range suppression facts; presentation history is never saved.</summary>
public sealed record CombatReportsSaveData(
    [property: JsonPropertyName("version"), JsonRequired] int Version,
    [property: JsonPropertyName("sequence"), JsonRequired] long Sequence,
    [property: JsonPropertyName("reportedRangeTargetIds"), JsonRequired] IReadOnlyList<string> ReportedRangeTargetIds);
