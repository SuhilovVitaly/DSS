using System.Collections.Immutable;

namespace DeepSpaceSaga.Engine.TacticalBattle.Content;

internal sealed record TacticalRules(string Version, string Description, int Substeps,
    double TurnAtMaxSpeedFraction, DeployScatter DeployScatter, HitRules Hit, RearRules Rear,
    DisabledRules Disabled, SystemRules Systems, int SeriesTurnCap);
internal sealed record DeployScatter(double Xy, double CourseDeg);
internal sealed record HitRules(double Base, double MinChance, double MaxChance, double RangeBonus,
    double AnglePenalty, double RearBonus, double RearBonusArcDeg);
internal sealed record RearRules(double ArcHalfDeg);
internal sealed record DisabledRules(double HullFraction);
internal sealed record SystemRules(int MinHullLoss, double EngineDamagedFactor, ImmutableArray<SystemEffect> Table);
internal sealed record SystemEffect(string Effect, int Weight);

internal sealed record BattleFrame(string Id, string Name, int Hull, double FrameMassKg = 0,
    double ReferenceMassKg = 0, double MinSpeed = 0, double MaxSpeedCap = 0, double TurnCapDeg = 0,
    int EngineSlots = 0, int ShieldSlots = 1, int? Maneuver = null);
internal sealed record BattleWeapon(string Id, string Name, int Tier, double ArcDeg, double Range,
    int DamageMin, int DamageMax, int Piercing, int HitBonus, double RearKillChance, double MassKg,
    double? RangeBonus = null);
internal sealed record BattleEngine(string Id, string Name, double SpeedBonus, double SpeedChange,
    double TurnDeg, double MassKg);
internal sealed record BattleShield(string Id, string Name, int Tier, int Defense, double MassKg);
internal sealed record BattleArmor(string Id, string Name, int Tier, int Armor, double MassKg);

// IsRange must survive equal endpoints: [50,50] still consumes a random draw.
internal readonly record struct PilotSkill(int Minimum, int Maximum, bool IsRange)
{
    public static PilotSkill Fixed(int value) => new(value, value, false);
    public static PilotSkill Range(int minimum, int maximum) => new(minimum, maximum, true);
}
internal sealed record BattlePilot(string Name, PilotSkill Accuracy, PilotSkill Maneuver);
internal sealed record BattleLayout(string Name, string Class, string Armor, string? Weapon,
    string? Shield, ImmutableArray<string> Engines, int? Hull = null, int? ShieldCount = null);
internal sealed record BattleGoal(string PlayerSide, double WinMaxOneLoss, double WinNoLoss,
    double Tolerance, ImmutableArray<long> Seeds);
internal sealed record BattleScenario(string Name, string Description, ImmutableArray<BattleSide> Sides,
    BattleGoal? Goal = null);
internal sealed record BattleSide(string Id, string Tactic, BattleCarrierStart Carrier,
    ImmutableArray<BattleDroneStart> Drones);
internal sealed record BattleCarrierStart(string Id, string Layout, double X, double Y, double Course);
internal sealed record BattleDroneStart(string Id, string Pilot, string Layout, double X, double Y,
    double Course, double Speed);

internal sealed record BattleAi(OrderedBattleCatalog<BattleTactic> Tactics,
    OrderedBattleCatalog<BattleRole> Roles, PursuitSettings Pursuit, StrikerSettings Striker,
    InterceptorSettings Interceptor, CommanderSettings Commander, LeashSettings Leash,
    TailSettings Tail, FighterSettings Fighter);
internal sealed record BattleTactic(string Name, bool Adaptive = false, int Striker = 0,
    int Interceptor = 0, int Fighter = 0);
internal sealed record BattleRole(string Name);
internal sealed record PursuitSettings(double TailOffset, double AimErrorScale, double SpeedFactor,
    bool SlowForTurn, double AimAccuracyRef, int SlowForTurnSteps);
internal sealed record StrikerSettings(double AttackSpeed, double BreakDistance, double ExtendDistance,
    bool TakesTail);
internal sealed record InterceptorSettings(double PatrolRadius, double PatrolOrbitRadius,
    double PatrolOrbitStepDeg, double PatrolSpeed, double ScreenDistance, double ScreenHold,
    double EngageFactor, double OrbitTurnFraction, double FlankOffset);
internal sealed record CommanderSettings(double ThreatRadius, double ThreatTurns, double ThreatHeadingDeg,
    int MaxInterceptors, int ExtraInterceptors, int MinRoleTurns, double StrikeWhenEnemyCarrierBelow,
    string StartTactic);
internal sealed record LeashSettings(double Radius);
internal sealed record TailSettings(double Chance, double RangeFactor, double ConeFactor,
    int SpeedSteps, int TurnSteps);
internal sealed record FighterSettings(int BreakAfterTurns, int ExtendTurns);
