namespace DeepSpaceSaga.Contracts;

public enum CombatEventType
{
    Launch, Intercept, Miss, Hit, Destroyed, SelfDestruct, TargetLost, Expired,
    TorpedoHitRoll, ModuleHit, ModuleDestroyed, ShipDestroyed, BurstShotSkipped, LauncherBreakdown
}
/// <summary>Immutable combat fact. Time and position are physical world values, never presentation clocks.</summary>
public sealed record CombatJournalEntry(long EventId, double MotionTimeMs, CombatEventType Type,
    string ActorObjectId, string TargetObjectId, string ProjectileObjectId, double X, double Y,
    int? ChanceTenths = null, int? Roll = null, InterceptionRatingBreakdown? RatingBreakdown = null,
    WeaponOperatorSnapshot? TorpedoOperator = null, int? Damage = null, string? Result = null,
    string? LauncherModuleId = null, LaunchMode? LaunchMode = null,
    decimal? Accuracy = null, decimal? TargetManeuverability = null,
    CountermeasureTerminationKind? TerminationKind = null,
    /// <summary>Captured attack and contact-time defense used to calculate the hit chance.</summary>
    decimal? Attack = null, decimal? Defense = null,
    string? HitModuleId = null,
    int? ArmorBefore = null, int? ArmorAfter = null,
    int? StructureBefore = null, int? StructureAfter = null,
    string? DestroyReason = null);
