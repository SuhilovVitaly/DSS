using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private sealed record PendingLootOperation(string Id, LootRowKey Row, long DueAtMs, int DurationMs, int Order, long? RequestedQuantity);
    private readonly Dictionary<LootRowKey, PendingLootOperation> _lootOperations = [];
    internal Func<long> LootClockMilliseconds { get; set; } = () => Environment.TickCount64;
    public bool HasActiveLootOperations { get { lock (_worldStateLock) return _lootOperations.Count != 0; } }
    private void ThrowIfLootOperationsActive()
    {
        if (_lootOperations.Count != 0) throw new InvalidOperationException(BattleLootReasons.OperationsRunning);
    }

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
        if (command.Batch is { } batch && (command.Row is not null || !Enum.IsDefined(batch)))
        { reason = BattleLootReasons.InvalidTarget; return; }
        if (command.RequestedQuantity is { } requested && (command.Row is not { Kind: LootRowKind.Consumable } target ||
            requested <= 0 || requested > RemainingConsumables(loot, target)))
        { reason = BattleLootReasons.InvalidQuantity; return; }
        if (command.Row is { } selected && _lootOperations.ContainsKey(selected))
        { reason = BattleLootReasons.OperationsRunning; return; }
        long start = LootClockMilliseconds();
        var operations = EnumerateLootRows(loot).Select((entry, index) => (entry.Row, entry.Available, Order: index))
            .Where(entry => entry.Available && (command.Row is null || entry.Row == command.Row) &&
                !_lootOperations.ContainsKey(entry.Row))
            .Where(entry => command.Batch switch
            {
                LootBatchKind.Components => entry.Row.Kind is LootRowKind.Module or LootRowKind.Consumable,
                LootBatchKind.Scrap => entry.Row.Kind == LootRowKind.Scrap,
                _ => true
            })
            .Select(entry =>
            {
                int duration = LootDuration(entry.Row.Kind);
                return new PendingLootOperation(command.CommandId + "/" + entry.Order, entry.Row, checked(start + duration),
                    duration, entry.Order, command.RequestedQuantity);
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
                row.ObjectId, row.EntryId, Math.Min(operation.RequestedQuantity ?? long.MaxValue, RemainingConsumables(loot, row))),
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

    private static long RemainingConsumables(BattleLootData loot, LootRowKey row) =>
        loot.Sources.FirstOrDefault(source => source.ObjectId == row.ObjectId)?.Consumables
            .FirstOrDefault(item => item.ItemTypeId == row.EntryId && !item.Destroyed)?.Remaining ?? 0;

    private ImmutableArray<LootOperationSnapshot> ProjectLootOperations()
    {
        if (_lootOperations.Count == 0) return [];
        long now = LootClockMilliseconds();
        return _lootOperations.Values.OrderBy(op => op.DueAtMs).ThenBy(op => op.Order)
            .Select(op => new LootOperationSnapshot(op.Id, op.Row, op.DurationMs, (int)Math.Clamp(op.DueAtMs - now, 0, op.DurationMs)))
            .ToImmutableArray();
    }

    private void PrepareClaimExtractedLoot(ClaimExtractedLootCommand command, ref DemoCampaignData candidate,
        ref string? reason, ref Action? commit)
    {
        if (GetTimedLoot(command.BattleId) is not { } loot) { reason = BattleLootReasons.WrongBattle; return; }
        if (command.Row is not { } row) { reason = BattleLootReasons.InvalidTarget; return; }
        var source = loot.Sources.FirstOrDefault(source => source.ObjectId == row.ObjectId);
        if (source is null) { reason = BattleLootReasons.InvalidTarget; return; }
        string itemId;
        long quantity;
        LootSourceData updated;
        if (row.Kind == LootRowKind.Scrap && source.Scrap is { State: LootAttemptStates.ExtractedWaiting } scrap &&
            scrap.ItemTypeId == row.EntryId)
        {
            itemId = scrap.ItemTypeId;
            quantity = scrap.Quantity;
            updated = source with { Scrap = scrap with { State = LootAttemptStates.Collected } };
        }
        else if (row.Kind == LootRowKind.Module && source.Modules.FirstOrDefault(module =>
            module.InstanceId == row.EntryId && module.State == LootAttemptStates.ExtractedWaiting) is { } module)
        {
            itemId = module.ItemTypeId;
            quantity = 1;
            updated = source with { Modules = source.Modules.Replace(module, module with { State = LootAttemptStates.Collected }) };
        }
        else { reason = BattleLootReasons.InvalidTarget; return; }
        if (!TryPrepareLootCargo(itemId, quantity, out commit)) { reason = BattleLootReasons.CargoFull; return; }
        candidate = candidate with { LootSession = loot with { Sources = loot.Sources.Replace(source, updated) } };
    }
}
