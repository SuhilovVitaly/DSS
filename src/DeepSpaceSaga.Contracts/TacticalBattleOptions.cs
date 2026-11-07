using System.Collections.Immutable;

namespace DeepSpaceSaga.Contracts;

public sealed record TacticalContentIssue(string File, string Field, string Message);
public sealed record TacticalBattleAvailability(bool Available, TacticalContentIssue? Issue = null);
public readonly record struct TacticalSkillRange(int Minimum, int Maximum, bool IsRandom);
public sealed record TacticalBattleGoal(string PlayerSide, double WinMaxOneLoss, double WinNoLoss, double Tolerance, ImmutableArray<long> Seeds);
public sealed record TacticalDroneSetupOption(string Id, string Side, string FrameId, string FrameName, string PilotId, string PilotName,
    TacticalSkillRange Accuracy, TacticalSkillRange Maneuver, int Hull, string WeaponId, string ArmorId, string? ShieldId, int ShieldCount, int ShieldSlots);
public sealed record TacticalScenarioOption(string Id, string Name, string Description, string PlayerSide, string OwnTacticId,
    string EnemyTacticId, TacticalBattleGoal? Goal, ImmutableArray<TacticalDroneSetupOption> Drones);
public sealed record TacticalWeaponOption(string Id, string Name, int Tier, double ArcDegrees, double Range, int DamageMin,
    int DamageMax, int Piercing, int HitBonus, double? RangeBonus, double RearKillChance, double Mass);
public sealed record TacticalArmorOption(string Id, string Name, int Tier, int Armor, double Mass);
public sealed record TacticalShieldOption(string Id, string Name, int Tier, int Defense, double Mass);
public sealed record TacticalTacticOption(string Id, string Name, bool Adaptive);
public sealed record TacticalBattleOptions(TacticalBattleAvailability Availability, ImmutableArray<TacticalScenarioOption> Scenarios,
    ImmutableArray<TacticalWeaponOption> Weapons, ImmutableArray<TacticalArmorOption> Armor,
    ImmutableArray<TacticalShieldOption> Shields, ImmutableArray<TacticalTacticOption> Tactics, string? Version = null);
