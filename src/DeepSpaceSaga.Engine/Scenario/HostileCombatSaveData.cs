using System.Text.Json.Serialization;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>Physical AI decision cursor and captured per-actor motion/encounter state.</summary>
public sealed record HostileCombatSaveData(
    [property: JsonRequired] int Version,
    [property: JsonRequired] long NextDecisionMotionTimeMs,
    [property: JsonRequired] IReadOnlyList<HostileCombatActorSaveData> Actors)
{
    public const int CurrentVersion = 1;
}

public sealed record HostileCombatActorSaveData(
    [property: JsonRequired] string ObjectId,
    [property: JsonRequired] HostileCombatSnapshot State);
