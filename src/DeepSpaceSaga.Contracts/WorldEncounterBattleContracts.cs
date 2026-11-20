using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

public sealed record WorldBattleBinding(string BattleId, string EncounterId, string GroupId);
public sealed record WorldEncounterSnapshot(WorldBattleBinding Binding, string Phase, bool CanContinue,
    string? UnavailableReason = null);
public sealed record BeginEncounterBattleCommand(string CommandId, long ExpectedRevision, string EncounterId)
    : DemoCampaignCommand(CommandId, ExpectedRevision)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool RequireBattleResults { get; init; }
}

public enum BattleResultsStage { AwaitingOpen, Open, Acknowledged }

/// <summary>A confirmed terminal battle outcome, independent of loot eligibility.</summary>
public sealed record BattleResultsSnapshot(string BattleId, BattleResultsStage Stage, string PlayerSide,
    TacticalOutcomeSnapshot Outcome, bool CanLoot, bool? PlayerCarrierDestroyed)
{
    public int? ConsumedTorpedoes { get; init; }
    public BattleResultsCapacitySnapshot? Capacity { get; init; }
}

public sealed record BattleResultsCapacitySnapshot(long FreeCargoKg, int FreePrisonerCabins);

public static class WorldEncounterPhases
{
    public const string Captured = "captured";
    public const string PendingBattle = "pending_battle";
    public const string Finished = "finished";
}
