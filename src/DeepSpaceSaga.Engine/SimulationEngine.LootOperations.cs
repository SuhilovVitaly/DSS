using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private sealed record PendingLootOperation(string Id, LootRowKey Row, long DueAtMs, int DurationMs, int Order);
    private readonly Dictionary<LootRowKey, PendingLootOperation> _lootOperations = [];
    internal Func<long> LootClockMilliseconds { get; set; } = () => Environment.TickCount64;

    private BattleLootData? GetTimedLoot(string battleId) =>
        _frozenBattleResults is { Results: { Stage: BattleResultsStage.Open, CanLoot: true } results } &&
        results.BattleId == battleId && _demoCampaign.LootSession is { Phase: LootPhases.Open } loot && loot.BattleId == battleId
            ? loot : null;

    private static IEnumerable<(LootRowKey Row, bool Available)> EnumerateLootRows(BattleLootData loot)
    {
        foreach (var source in loot.Sources)
        {
            foreach (var item in source.Consumables)
                yield return (new(source.ObjectId, LootRowKind.Consumable, item.ItemTypeId), !item.Destroyed && item.Remaining > 0);
            if (source.Scrap is { } scrap)
                yield return (new(source.ObjectId, LootRowKind.Scrap, scrap.ItemTypeId), scrap.State == LootAttemptStates.NotAttempted);
            foreach (var item in source.Modules)
                yield return (new(source.ObjectId, LootRowKind.Module, item.InstanceId), !item.Destroyed && item.State == LootAttemptStates.NotAttempted);
        }
    }

    private static int LootDuration(LootRowKind kind) => kind switch
    {
        LootRowKind.Consumable => 3000,
        LootRowKind.Module => 5000,
        LootRowKind.Scrap => 10000,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private void PrepareStartLootOperations(StartLootOperationsCommand command, ref string? reason, ref Action? commit)
    {
        if (GetTimedLoot(command.BattleId) is not { } loot) { reason = BattleLootReasons.WrongBattle; return; }
        if (command.Row is { } selected && _lootOperations.ContainsKey(selected))
        { reason = BattleLootReasons.OperationsRunning; return; }
        long start = LootClockMilliseconds();
        var operations = EnumerateLootRows(loot).Select((entry, index) => (entry.Row, entry.Available, Order: index))
            .Where(entry => entry.Available && (command.Row is null || entry.Row == command.Row) && !_lootOperations.ContainsKey(entry.Row))
            .Select(entry =>
            {
                int duration = LootDuration(entry.Row.Kind);
                return new PendingLootOperation(command.CommandId + "/" + entry.Order, entry.Row, checked(start + duration), duration, entry.Order);
            }).ToArray();
        if (operations.Length == 0) { reason = BattleLootReasons.InvalidTarget; return; }
        commit = () =>
        {
            foreach (var operation in operations) _lootOperations.Add(operation.Row, operation);
        };
    }

    private void PrepareAdvanceLootOperations(AdvanceLootOperationsCommand command, ref DemoCampaignData candidate,
        ref string? reason, ref Action? commit)
    {
        if (GetTimedLoot(command.BattleId) is not { } loot) { reason = BattleLootReasons.WrongBattle; return; }
        long now = LootClockMilliseconds();
        var operation = _lootOperations.Values.Where(op => op.DueAtMs <= now).MinBy(op => (op.DueAtMs, op.Order));
        if (operation is null) { reason = BattleLootReasons.OperationNotDue; return; }
        var row = operation.Row;
        DemoCampaignCommand action = row.Kind switch
        {
            LootRowKind.Consumable => new CollectLootConsumablesCommand(operation.Id, command.ExpectedRevision, command.BattleId,
                row.ObjectId, row.EntryId, loot.Sources.Single(source => source.ObjectId == row.ObjectId)
                    .Consumables.Single(item => item.ItemTypeId == row.EntryId).Remaining),
            LootRowKind.Scrap => new AttemptScrapCommand(operation.Id, command.ExpectedRevision, command.BattleId, row.ObjectId),
            LootRowKind.Module => new AttemptModuleSalvageCommand(operation.Id, command.ExpectedRevision, command.BattleId, row.ObjectId, row.EntryId),
            _ => throw new ArgumentOutOfRangeException(nameof(command))
        };
        Action? grant = null;
        PrepareBattleLootCommand(action, ref candidate, ref reason, ref grant, completingOperation: true);
        if (reason == BattleLootReasons.CargoFull) reason = null;
        if (reason is not null) return;
        commit = () =>
        {
            grant?.Invoke();
            _lootOperations.Remove(row);
        };
    }

    private ImmutableArray<LootOperationSnapshot> ProjectLootOperations()
    {
        if (_lootOperations.Count == 0) return [];
        long now = LootClockMilliseconds();
        return _lootOperations.Values.OrderBy(op => op.DueAtMs).ThenBy(op => op.Order)
            .Select(op => new LootOperationSnapshot(op.Id, op.Row, op.DurationMs, (int)Math.Clamp(op.DueAtMs - now, 0, op.DurationMs)))
            .ToImmutableArray();
    }
}
