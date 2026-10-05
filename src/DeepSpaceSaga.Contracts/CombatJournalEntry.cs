namespace DeepSpaceSaga.Contracts;

public enum CombatEventType { Launch, Intercept, Miss, Hit, Destroyed, SelfDestruct, TargetLost }
/// <summary>Immutable combat fact. Time and position are physical world values, never presentation clocks.</summary>
public sealed record CombatJournalEntry(long EventId, double MotionTimeMs, CombatEventType Type,
    string ActorObjectId, string TargetObjectId, string ProjectileObjectId, double X, double Y,
    int? ChanceTenths = null, int? Roll = null, InterceptionRatingBreakdown? RatingBreakdown = null,
    WeaponOperatorSnapshot? TorpedoOperator = null, int? Damage = null, string? Result = null);
