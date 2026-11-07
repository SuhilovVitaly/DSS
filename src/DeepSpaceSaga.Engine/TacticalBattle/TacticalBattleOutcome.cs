using System.Collections.Immutable;

namespace DeepSpaceSaga.Engine.TacticalBattle;

internal enum TacticalOutcomeKind { A, B, Draw, Timeout }
internal abstract record TacticalOutcomeReason;
internal sealed record TacticalCarrierDestroyed(string CarrierId) : TacticalOutcomeReason;
internal sealed record TacticalNoDrones(string CarrierId) : TacticalOutcomeReason;
internal sealed record TacticalTimeout(int Cap) : TacticalOutcomeReason;
internal sealed record TacticalWeaponTotals(string WeaponId, long Shots, long Hits, long RearHits, long RearKills, long Hull);
internal sealed record TacticalBattleTotals(long Shots, long Hits, long RearHits, long RearKills, long Systems,
    long Disabled, long Hull, long Armor, long CarrierHull, string? FirstLoss, int RoleChanges, ImmutableArray<TacticalWeaponTotals> Weapons);
internal sealed record TacticalBattleResult(string ScenarioId, long Seed, TacticalOutcomeKind Kind,
    ImmutableArray<TacticalOutcomeReason> Reasons, int Turns, ImmutableArray<int> Losses,
    ImmutableArray<int> CarrierHull, TacticalBattleTotals Statistics, long RandomCalls);
