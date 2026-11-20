using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>Pending results only. A null map references the same battle's existing open loot ledger.</summary>
public sealed record BattleResultsData(string BattleId, BattleResultsStage Stage, TacticalBattleSnapshot? Map, int? ConsumedTorpedoes);
