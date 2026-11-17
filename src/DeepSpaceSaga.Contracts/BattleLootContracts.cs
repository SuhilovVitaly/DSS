using System.Collections.Immutable;

namespace DeepSpaceSaga.Contracts;

public interface ILootCampaignCommand { string BattleId { get; } }
public sealed record JettisonCargoCommand(string CommandId, long ExpectedRevision, string BattleId, string ItemTypeId, long Quantity)
    : DemoCampaignCommand(CommandId, ExpectedRevision), ILootCampaignCommand;
public sealed record CollectLootConsumablesCommand(string CommandId, long ExpectedRevision, string BattleId,
    string ObjectId, string ItemTypeId, long Quantity) : DemoCampaignCommand(CommandId, ExpectedRevision), ILootCampaignCommand;
public sealed record AttemptScrapCommand(string CommandId, long ExpectedRevision, string BattleId, string ObjectId)
    : DemoCampaignCommand(CommandId, ExpectedRevision), ILootCampaignCommand;
public sealed record AttemptModuleSalvageCommand(string CommandId, long ExpectedRevision, string BattleId,
    string ObjectId, string InstanceId) : DemoCampaignCommand(CommandId, ExpectedRevision), ILootCampaignCommand;
public sealed record FinishBattleLootCommand(string CommandId, long ExpectedRevision, string BattleId, bool ConfirmDiscard = false)
    : DemoCampaignCommand(CommandId, ExpectedRevision), ILootCampaignCommand;

public sealed record LootSessionSnapshot(string BattleId, string Phase, ImmutableArray<LootObjectSnapshot> Objects, bool HasUnclaimed)
{
    public ImmutableArray<LootCargoSnapshot> Cargo { get; init; } = [];
    public long FreeCargoKg { get; init; }
}
public sealed record LootCargoSnapshot(string ItemTypeId, long Quantity, long UnitMassKg, long QuantityStep);
public sealed record LootObjectSnapshot(string ObjectId, string Kind, ImmutableArray<LootConsumableSnapshot> Consumables,
    ScrapLootSnapshot? Scrap, ImmutableArray<ModuleLootSnapshot> Modules, LootPilotSnapshot? Pilot,
    double X = 0, double Y = 0, double Direction = 0);
public sealed record LootConsumableSnapshot(string ItemTypeId, long Remaining, bool CanCollect, string? Reason, long UnitMassKg = 0);
public sealed record ScrapLootSnapshot(string ItemTypeId, long Quantity, int ChanceBasisPoints, string State, bool CanAttempt, string? Reason, long RequiredMassKg = 0);
public sealed record ModuleLootSnapshot(string InstanceId, string Category, string CatalogId, string ItemTypeId,
    string DisplayName, string ConditionLabel, long EstimatedValueTokens, int ChanceBasisPoints, string State, bool CanAttempt, string? Reason, long RequiredMassKg = 0);
public sealed record LootPilotSnapshot(string SourcePilotId, string DisplayName, string? PortraitAssetId, bool Alive,
    string? CaptureReason, bool Captured = false, CrewGender Gender = CrewGender.Female);

public static class LootPhases
{
    public const string Open = "Open";
    public const string Closed = "Closed";
}
public static class LootAttemptStates
{
    public const string NotAttempted = "NotAttempted";
    public const string Failed = "Failed";
    public const string Collected = "Collected";
    public const string Discarded = "Discarded";
}
public static class BattleLootReasons
{
    public const string NotCargoItem = "loot_not_cargo_item";
    public const string NotEnoughQuantity = "loot_not_enough_quantity";
    public const string WrongBattle = "loot_wrong_battle";
    public const string NotVictory = "loot_not_victory";
    public const string LootClosed = "loot_closed";
    public const string InvalidTarget = "loot_invalid_target";
    public const string AlreadyAttempted = "loot_already_attempted";
    public const string CargoFull = "loot_cargo_full";
    public const string ConfirmDiscardRequired = "loot_confirm_discard_required";
    public const string InvalidQuantity = "loot_invalid_quantity";
}
