using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

/// <summary>One catalog overlay value. Field and profile names are case-sensitive file keys.</summary>
public sealed record DebugTuningValue(
    string ModuleTypeId,
    string Field,
    double Value,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ProfileId = null);

/// <summary>Atomic player-command patch; validation belongs to the engine.</summary>
public sealed record DebugTuningPatch(
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<DebugTuningValue>))]
    ImmutableArray<DebugTuningValue> Values);

/// <summary>Confirmed catalog overlay and immutable original catalog defaults.</summary>
public sealed record DebugTuningSnapshot(
    long Revision,
    ImmutableArray<DebugTuningValue> Values,
    ImmutableArray<DebugTuningValue> Defaults);

public static class DebugTuningCommandTypes
{
    public const string Apply = "debug.tuning.apply";
    public const string Category = "debug";
}

public static class DebugTuningReasonCodes
{
    public const string UnknownParameter = "debug_unknown_parameter";
    public const string ValueOutOfRange = "debug_value_out_of_range";
    public const string SessionNotRunning = "debug_session_not_running";
}
