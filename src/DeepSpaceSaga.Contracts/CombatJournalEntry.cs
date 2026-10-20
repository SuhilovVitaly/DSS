using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

public enum CombatEventType
{
    Launch, Intercept, Miss, Hit, Destroyed, SelfDestruct, TargetLost, Expired,
    TorpedoHitRoll, ModuleHit, ModuleDestroyed, ShipDestroyed, BurstShotSkipped, LauncherBreakdown,
    TorpedoDetonated, CountermissileDetonated, ShotDown, ManuallyDetonated
}

/// <summary>Public geometric result; no hit chance, RNG roll or hidden module health.</summary>
public enum ProjectileHitResult { Core, Edge, Miss, ShotDown }
/// <summary>Immutable combat fact. Time and position are physical world values, never presentation clocks.</summary>
public sealed record CombatJournalEntry(long EventId, double MotionTimeMs, CombatEventType Type,
    string ActorObjectId, string TargetObjectId, string ProjectileObjectId, double X, double Y,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? ChanceTenths = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Roll = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] InterceptionRatingBreakdown? RatingBreakdown = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] WeaponOperatorSnapshot? TorpedoOperator = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Damage = null,
    string? Result = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? LauncherModuleId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] LaunchMode? LaunchMode = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? Accuracy = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? TargetManeuverability = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] CountermeasureTerminationKind? TerminationKind = null,
    /// <summary>Captured attack and contact-time defense used to calculate the hit chance.</summary>
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? Attack = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? Defense = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? HitModuleId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? ArmorBefore = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? ArmorAfter = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? StructureBefore = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? StructureAfter = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? DestroyReason = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ProjectileKind? ProjectileKind = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ProfileId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? BlastRadiusKm = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ProjectileHitResult? HitResult = null);
