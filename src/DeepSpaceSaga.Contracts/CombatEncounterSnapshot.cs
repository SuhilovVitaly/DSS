using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

/// <summary>One encounter's state; independent of the session's terminal outcome.</summary>
public enum CombatEncounterState { Idle, Active, Won, Lost }

public static class CombatEncounterCommandTypes
{
    public const string AcknowledgeResults = "combat.acknowledgeResults";
}

/// <summary>
/// Authoritative encounter counters. Launches, interceptions and evades concern the player;
/// enemy launches count hostile torpedoes. Hits require positive damage, one per blast and
/// ship. Damage totals expose only actual losses of the player's armor and structure.
/// </summary>
public sealed record CombatEncounterStats(
    long TorpedoesLaunched = 0,
    long CountermissilesLaunched = 0,
    long EnemyTorpedoesLaunched = 0,
    long TorpedoesIntercepted = 0,
    long PlayerHitsReceived = 0,
    long EnemyHitsDealt = 0,
    long PlayerEvasions = 0,
    long EnemiesDestroyed = 0,
    long ArmorDamageTaken = 0,
    long StructureDamageTaken = 0);

/// <summary>
/// Engine-owned encounter state. Deadlines are absolute physical milliseconds.
/// DurationSeconds is elapsed physical time supplied by the Engine, excluding pauses;
/// nominal real duration at x1 is four times this value. This DTO runs no state transitions.
/// </summary>
public sealed record CombatEncounterSnapshot(
    CombatEncounterState State,
    long StartedAtMotionMs,
    long? EndedAtMotionMs,
    CombatEncounterStats Stats,
    double DurationSeconds = 0);

/// <summary>Aggregate public enemy status, with no module list or numeric enemy damage.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record EnemyShipStatusSnapshot(bool Damaged, long? ImmobilizedUntilMotionMs, bool Destroyed);
