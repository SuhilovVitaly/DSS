using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

public enum TacticalBattlePhase { Move = 0, Fire = 1, Over = 2 }
public enum TacticalUnitKind { Drone = 0, Carrier = 1 }
public enum TacticalUnitState { Air = 0, Disabled = 1, Destroyed = 2, Alive = 3 }
public enum TacticalBattleOutcomeCode { A = 0, B = 1, Draw = 2, Timeout = 3 }
public enum TacticalOutcomeReasonCode { CarrierDestroyed = 0, NoDrones = 1, Timeout = 2, NoCombatCapability = 3 }
public enum TacticalBattleLogKind
{
    Started = 0, RoleChanged = 1, FighterBreak = 2, NoTargets = 3, ShotMissed = 4, ShotHit = 5,
    RearKill = 6, Disabled = 7, Destroyed = 8, WeaponJammed = 9, EngineDamaged = 10, Outcome = 11, Exited = 12,
    DamageApplied = 13, FireSkipped = 14, DecoyLaunched = 15, ShotAbsorbed = 16, Moved = 17
}

public enum TacticalFireSkipReason
{
    None = 0, NoTarget = 1, Withheld = 2, ShooterDestroyed = 3, ShooterDisabled = 4,
    WeaponJammed = 5, TargetUnavailable = 6, BattleEnded = 7, Evading = 8, InDust = 9,
    NoAmmo = 10
}
public sealed record TacticalFireQueueEntry(int Index, string ShooterId, int Initiative);
public sealed record TacticalFireStepSnapshot(int Index, string ShooterId, string? TargetId, TacticalFireSkipReason SkipReason,
    TacticalShotSnapshot? Shot, ImmutableArray<TacticalUnitSnapshot> UnitsAfter,
    ImmutableArray<TacticalBattleLogEvent> Events, TacticalOutcomeSnapshot? Outcome);

public readonly record struct TacticalPoseSnapshot(double X, double Y, double Course);
public sealed record TacticalSideTactic(string Side, string TacticId);
public sealed record TacticalMovementLimits(double Mass, double MassFactor, double MinSpeed, double MaxSpeed, double Change, double Turn);
public enum TacticalSlotKind { Weapon = 0, Armor = 1, Shield = 2, Engine = 3, Module = 4 }
public enum TacticalSlotState { Ok = 0, Jammed = 1, Damaged = 2 }
public sealed record TacticalSlotSnapshot(TacticalSlotKind Kind, int Index, string? ModuleId, string? ModuleName, TacticalSlotState State, double Factor = 1);
public sealed record TacticalEngineSnapshot(string Id, string Name, double Factor, double Mass, double SpeedBonus, double SpeedChange, double Turn);
public sealed record TacticalBuildSnapshot(string FrameName, string PilotId, string PilotName, string WeaponId, string WeaponName,
    int DamageMin, int DamageMax, int Piercing, int HitBonus, double Range, double ArcDegrees, double? RangeBonus, double RearKillChance,
    string ArmorId, string ArmorName, string? ShieldId, string? ShieldName, int ShieldDefense, int ShieldCount, int ShieldSlots,
    double FrameMass, double ReferenceMass, double WeaponMass, double ArmorMass, double ShieldMass)
{
    public int EngineSlots { get; init; }
    public int ModuleSlots { get; init; }
    public string? DecoyId { get; init; }
    public string? DecoyName { get; init; }
    public int DecoyMaxCharges { get; init; }
    /// <summary>Pilot gender from the shared crew record (same CrewId as <see cref="PilotId"/>).</summary>
    public CrewGender? PilotGender { get; init; }
    public string? PilotPortraitAssetId { get; init; }
    public CrewRole? PilotRole { get; init; }
}
public sealed record TacticalUnitSnapshot(string Id, string Side, TacticalUnitKind Kind, string FrameId,
    TacticalPoseSnapshot Pose, double Speed, int Hull, int MaxHull, int Armor, int MaxArmor, TacticalUnitState State,
    string? RoleId, bool Jammed, ImmutableArray<TacticalEngineSnapshot> Engines, TacticalMovementLimits? Limits,
    double PreviousTurn, TacticalBuildSnapshot? Build, int Accuracy, int Maneuver, string? RoleName = null)
{
    public ImmutableArray<TacticalSlotSnapshot> Slots { get; init; } = [];
    public bool InDust { get; init; }
    public int? Initiative { get; init; }
    public int? Consciousness { get; init; }
    public int? DecoyCharges { get; init; }
    /// <summary>Owned drone id when this unit represents a persistent wing drone (else null).</summary>
    public string? SourceDroneId { get; init; }
    /// <summary>World ship id this unit originates from (carrier or drone).</summary>
    public string? SourceShipId { get; init; }
    /// <summary>Specific carrier identity for every drone, including members of one enemy side.</summary>
    public string? OriginCarrierId { get; init; }
    /// <summary>Remaining torpedoes. Null for legacy payloads = unknown, not infinite; 0 is an empty magazine.</summary>
    public int? AmmoCurrent { get; init; }
    public int? AmmoCapacity { get; init; }
}
public sealed record TacticalDroneOrder(string DroneId, double Speed, double Turn);
public sealed record TacticalMovementPath(int Turn, string DroneId, ImmutableArray<TacticalPoseSnapshot> Points);
/// <summary>Additive terms are percentage points. Clamp and decoy are signed adjustments; absorption is a probability.</summary>
public sealed record TacticalHitFactors(double Base, double Accuracy, double Maneuver, double Weapon, double Range,
    double Angle, double Rear, double Shield, double Evasion, double Clamp, double Decoy, double Absorption);

public sealed record TacticalTargetCandidateSnapshot(string TargetId, double Distance, double Angle, bool Rear, double RearKillChance, double Chance)
{
    public double Absorption { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TacticalHitFactors? Factors { get; init; }
}
public sealed record TacticalTargetGroupSnapshot(string DroneId, ImmutableArray<TacticalTargetCandidateSnapshot> List, string? DefaultTargetId);
public sealed record TacticalIncomingShotSnapshot(string ShooterId, string TargetId, double Chance, double Distance, double Angle, bool Rear, double RearKillChance)
{
    public double Absorption { get; init; }
    public double? ChanceIfEvading { get; init; }
    public double? ChanceIfDecoy { get; init; }
}
/// <summary>Killed means this shot destroyed the target at its step; Kill is this shot's rear-kill roll.</summary>
public sealed record TacticalShotSnapshot(int Turn, string ShooterId, string TargetId, TacticalPoseSnapshot From,
    TacticalPoseSnapshot To, double Chance, bool Hit, int Damage, bool Rear, bool Kill, bool Killed)
{
    /// <summary>This impact transitions the target to destroyed.</summary>
    public double Absorption { get; init; }
    public bool Absorbed { get; init; }
    public bool LethalImpact { get; init; }
    public bool TargetEvading { get; init; }
    public bool Decoyed { get; init; }
    public double? ChanceWithoutDecoy { get; init; }
    /// <summary>Chance the HitRoll was compared with (decoy applied, absorption not); Chance is the effective chance.
    /// Null only in snapshots written before this field existed.</summary>
    public double? ConditionalChance { get; init; }
    /// <summary>The zero-to-one accuracy roll. An absorbed shot never reaches this check.</summary>
    public double? HitRoll { get; init; }
    public double? AbsorptionRoll { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TacticalHitFactors? Factors { get; init; }
}
public sealed record TacticalHitRulesSnapshot(double Base, double MinChance, double MaxChance, double RangeBonus,
    double AnglePenalty, double RearBonus, double RearBonusArc);
public enum TacticalAnomalyKindCode { Heat = 0, Dust = 1 }
public sealed record TacticalClumpSnapshot(double X, double Y, double Amplitude, double SigmaX, double SigmaY, double Rotation);
public sealed record TacticalAnomalySnapshot(string Id, TacticalAnomalyKindCode Kind, ImmutableArray<TacticalClumpSnapshot> Clumps);
public sealed record TacticalAnomalyRulesSnapshot(double ZoneEdge, double Background, int AbsorbSamples);
public sealed record TacticalDecoyRulesSnapshot(double AiThreatChance);
public sealed record TacticalEvasionRulesSnapshot(double HitPenalty, int Cost, int MinConsciousness, int RecoveryPerTurn, int MaxConsciousness);
public sealed record TacticalForecastRules(TacticalHitRulesSnapshot Hit, double TurnAtMaxSpeedFraction, double RearArc, int Substeps)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? DisabledHullFraction { get; init; }
    public TacticalEvasionRulesSnapshot? Evasion { get; init; }
    public TacticalDecoyRulesSnapshot? Decoy { get; init; }
    public TacticalAnomalyRulesSnapshot? Anomalies { get; init; }
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
public sealed record TacticalLogShot(double Chance, bool Rear)
{
    public bool Decoyed { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TacticalShotSnapshot? ResolvedShot { get; init; }
}
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
    public ImmutableArray<TacticalAnomalySnapshot> Anomalies { get; init; } = [];
    public ImmutableArray<string> DecoysLaunched { get; init; } = [];
    public ImmutableArray<TacticalFireQueueEntry> FireQueue { get; init; } = [];
    public ImmutableArray<TacticalFireStepSnapshot> FireSteps { get; init; } = [];
    /// <summary>World ship id this owned sortie launched from (null for diagnostic/legacy battles).</summary>
    public string? SourceShipId { get; init; }
}
