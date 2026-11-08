using System.Collections.Immutable;

namespace DeepSpaceSaga.Contracts;

public enum TacticalBattlePhase { Move = 0, Fire = 1, Over = 2 }
public enum TacticalUnitKind { Drone = 0, Carrier = 1 }
public enum TacticalUnitState { Air = 0, Disabled = 1, Destroyed = 2, Alive = 3 }
public enum TacticalBattleOutcomeCode { A = 0, B = 1, Draw = 2, Timeout = 3 }
public enum TacticalOutcomeReasonCode { CarrierDestroyed = 0, NoDrones = 1, Timeout = 2 }
public enum TacticalBattleLogKind
{
    Started = 0, RoleChanged = 1, FighterBreak = 2, NoTargets = 3, ShotMissed = 4, ShotHit = 5,
    RearKill = 6, Disabled = 7, Destroyed = 8, WeaponJammed = 9, EngineDamaged = 10, Outcome = 11, Exited = 12,
    DamageApplied = 13, FireSkipped = 14
}

public enum TacticalFireSkipReason
{
    None = 0, NoTarget = 1, Withheld = 2, ShooterDestroyed = 3, ShooterDisabled = 4,
    WeaponJammed = 5, TargetUnavailable = 6, BattleEnded = 7, Evading = 8
}
public sealed record TacticalFireQueueEntry(int Index, string ShooterId, int Initiative);
public sealed record TacticalFireStepSnapshot(int Index, string ShooterId, string? TargetId, TacticalFireSkipReason SkipReason,
    TacticalShotSnapshot? Shot, ImmutableArray<TacticalUnitSnapshot> UnitsAfter,
    ImmutableArray<TacticalBattleLogEvent> Events, TacticalOutcomeSnapshot? Outcome);

public readonly record struct TacticalPoseSnapshot(double X, double Y, double Course);
public sealed record TacticalSideTactic(string Side, string TacticId);
public sealed record TacticalMovementLimits(double Mass, double MassFactor, double MinSpeed, double MaxSpeed, double Change, double Turn);
public sealed record TacticalEngineSnapshot(string Id, string Name, double Factor, double Mass, double SpeedBonus, double SpeedChange, double Turn);
public sealed record TacticalBuildSnapshot(string FrameName, string PilotId, string PilotName, string WeaponId, string WeaponName,
    int DamageMin, int DamageMax, int Piercing, int HitBonus, double Range, double ArcDegrees, double? RangeBonus, double RearKillChance,
    string ArmorId, string ArmorName, string? ShieldId, string? ShieldName, int ShieldDefense, int ShieldCount, int ShieldSlots,
    double FrameMass, double ReferenceMass, double WeaponMass, double ArmorMass, double ShieldMass)
{
    public string? DecoyId { get; init; }
    public string? DecoyName { get; init; }
}
public sealed record TacticalUnitSnapshot(string Id, string Side, TacticalUnitKind Kind, string FrameId,
    TacticalPoseSnapshot Pose, double Speed, int Hull, int MaxHull, int Armor, int MaxArmor, TacticalUnitState State,
    string? RoleId, bool Jammed, ImmutableArray<TacticalEngineSnapshot> Engines, TacticalMovementLimits? Limits,
    double PreviousTurn, TacticalBuildSnapshot? Build, int Accuracy, int Maneuver, string? RoleName = null)
{
    public int? Initiative { get; init; }
    public int? Consciousness { get; init; }
    public int? DecoyCharges { get; init; }
}
public sealed record TacticalDroneOrder(string DroneId, double Speed, double Turn);
public sealed record TacticalMovementPath(int Turn, string DroneId, ImmutableArray<TacticalPoseSnapshot> Points);
public sealed record TacticalTargetCandidateSnapshot(string TargetId, double Distance, double Angle, bool Rear, double RearKillChance, double Chance);
public sealed record TacticalTargetGroupSnapshot(string DroneId, ImmutableArray<TacticalTargetCandidateSnapshot> List, string? DefaultTargetId);
public sealed record TacticalIncomingShotSnapshot(string ShooterId, string TargetId, double Chance, double Distance, double Angle, bool Rear, double RearKillChance)
{
    public double? ChanceIfEvading { get; init; }
}
/// <summary>Killed means this shot destroyed the target at its step; Kill is this shot's rear-kill roll.</summary>
public sealed record TacticalShotSnapshot(int Turn, string ShooterId, string TargetId, TacticalPoseSnapshot From,
    TacticalPoseSnapshot To, double Chance, bool Hit, int Damage, bool Rear, bool Kill, bool Killed)
{
    /// <summary>This impact transitions the target to destroyed.</summary>
    public bool LethalImpact { get; init; }
    public bool TargetEvading { get; init; }
}
public sealed record TacticalHitRulesSnapshot(double Base, double MinChance, double MaxChance, double RangeBonus,
    double AnglePenalty, double RearBonus, double RearBonusArc);
public sealed record TacticalEvasionRulesSnapshot(double HitPenalty, int Cost, int MinConsciousness, int RecoveryPerTurn, int MaxConsciousness);
public sealed record TacticalForecastRules(TacticalHitRulesSnapshot Hit, double TurnAtMaxSpeedFraction, double RearArc, int Substeps)
{
    public TacticalEvasionRulesSnapshot? Evasion { get; init; }
}
public sealed record TacticalLossCount(string Side, int Count);
public sealed record TacticalOutcomeReasonSnapshot(TacticalOutcomeReasonCode Kind, string? CarrierId = null, int? Cap = null);
public sealed record TacticalWeaponTotalsSnapshot(string WeaponId, long Shots, long Hits, long RearHits, long RearKills, long Hull);
public sealed record TacticalBattleTotalsSnapshot(long Shots, long Hits, long RearHits, long RearKills, long Systems,
    long Disabled, long Hull, long Armor, long CarrierHull, string? FirstLoss, int RoleChanges,
    ImmutableArray<TacticalWeaponTotalsSnapshot> Weapons);
public sealed record TacticalOutcomeSnapshot(TacticalBattleOutcomeCode Kind, ImmutableArray<TacticalOutcomeReasonSnapshot> Reasons,
    int Turns, ImmutableArray<TacticalLossCount> Losses, TacticalBattleTotalsSnapshot Statistics, long RandomCalls);
/// <summary>Chance is the original zero-to-one probability; Rear describes this actual shot.</summary>
public sealed record TacticalLogShot(double Chance, bool Rear);
public sealed record TacticalLogDamage(int Hull, int Armor, int Hits, int HullRemaining);
public sealed record TacticalLogUnit(TacticalUnitKind Kind, int Hull, int MaxHull, string? EngineName = null, double? EngineFactor = null);
public sealed record TacticalBattleLogEvent(ulong Sequence, int Turn, TacticalBattlePhase Phase, TacticalBattleLogKind Kind,
    string? ActorId = null, string? TargetId = null, string? RoleId = null, int? Amount = null, int? EngineIndex = null,
    bool NoAttackers = false, TacticalOutcomeSnapshot? Outcome = null, TacticalLogShot? Shot = null,
    TacticalLogDamage? Damage = null, TacticalLogUnit? Unit = null, string? RoleName = null)
{
    public TacticalFireSkipReason? SkipReason { get; init; }
    public int? FireStepIndex { get; init; }
}

/// <summary>Captured data only. Suggestions appear in Move; Targets/Incoming appear in Fire. Log is the full retained journal.</summary>
public sealed record TacticalBattleSnapshot(string BattleId, ulong Revision, TacticalBattlePhase Phase, int Turn, string PlayerSide,
    long Seed, string ScenarioId, string ScenarioName, ImmutableArray<TacticalSideTactic> Tactics,
    ImmutableArray<TacticalUnitSnapshot> Units, ImmutableArray<TacticalDroneOrder> Suggestions,
    ImmutableArray<TacticalMovementPath> Paths, ImmutableArray<TacticalTargetGroupSnapshot> Targets,
    ImmutableArray<TacticalIncomingShotSnapshot> Incoming, ImmutableArray<TacticalShotSnapshot> Shots,
    ImmutableArray<TacticalUnitSnapshot> BeforeSalvoUnits, bool FireSkipped, TacticalForecastRules Rules,
    ImmutableArray<TacticalLossCount> Losses, TacticalOutcomeSnapshot? Outcome, ImmutableArray<TacticalBattleLogEvent> Log)
{
    public ImmutableArray<TacticalFireQueueEntry> FireQueue { get; init; } = [];
    public ImmutableArray<TacticalFireStepSnapshot> FireSteps { get; init; } = [];
}
