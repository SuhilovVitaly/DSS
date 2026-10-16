using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>One armed automatic reaction, in physical milliseconds (500..1250 = 2..5 seconds at Speed1).</summary>
public sealed record CombatReactionSaveData(
    [property: JsonRequired] string Kind,
    [property: JsonRequired] string OwnerId,
    [property: JsonRequired] string ModuleId,
    [property: JsonRequired] string TargetId,
    [property: JsonRequired] long EligibleAt,
    [property: JsonRequired] double ReadyAt,
    [property: JsonRequired] long DueAt);
