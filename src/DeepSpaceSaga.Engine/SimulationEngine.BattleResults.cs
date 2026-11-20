using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private sealed record FrozenBattleResults(TacticalBattleSnapshot Map, BattleResultsSnapshot Results);
    private FrozenBattleResults? _frozenBattleResults;

    private bool BattleResultsPending => _frozenBattleResults is { Results.Stage: not BattleResultsStage.Acknowledged };
    private TacticalBattleSnapshot? FrozenBattleMap => BattleResultsPending ? _frozenBattleResults!.Map
        : LootOpen ? _demoCampaign.LootSession!.Map : null;

    private SortieSettlementSnapshot? FreezeBattleResults()
    {
        var session = _tacticalBattleSession!;
        var map = session.Capture() with { SourceShipId = _tacticalBattleSourceShipId };
        var settlement = BeginPostBattleLoot();
        bool canLoot = settlement is not null;
        if (!canLoot && _tacticalBattleSourceShipId is not null)
        {
            var result = session.Battle.Result!;
            settlement = CommitSortieOutcome(session, MapSortieExitKind(result.Kind, result.Reasons));
        }

        _frozenBattleResults = CreateFrozenResults(map, canLoot, BattleResultsStage.AwaitingOpen, settlement?.ConsumedTorpedoes);
        _tacticalBattleSession = null;
        _tacticalBattleSourceShipId = null;
        _requireBattleResults = false;
        return settlement;
    }

    private static FrozenBattleResults CreateFrozenResults(TacticalBattleSnapshot map, bool canLoot, BattleResultsStage stage,
        int? consumedTorpedoes = null)
    {
        var carrier = map.Units.FirstOrDefault(unit => unit.Kind == TacticalUnitKind.Carrier && unit.Side == map.PlayerSide);
        var results = new BattleResultsSnapshot(map.BattleId, stage, map.PlayerSide, map.Outcome!, canLoot,
            carrier is null ? null : carrier.Hull <= 0)
        {
            ConsumedTorpedoes = consumedTorpedoes
        };
        return new(map, results);
    }

    private bool IsBattleResultsCommandAllowed(DemoCampaignCommand command) =>
        command is OpenBattleResultsCommand or AcknowledgeBattleResultsCommand or SetLootDebugRequest ||
        (_frozenBattleResults is { Results: { Stage: BattleResultsStage.Open, CanLoot: true } } &&
            command is StartLootOperationsCommand or AdvanceLootOperationsCommand or ClaimExtractedLootCommand or ArrestPilotCommand);

    private void PrepareOpenBattleResults(OpenBattleResultsCommand command, ref string? reason, ref Action? commit)
    {
        var frozen = _frozenBattleResults;
        if (frozen?.Results.BattleId != command.BattleId && FrozenBattleMap is { } legacy && LootOpen)
            frozen = CreateFrozenResults(legacy, canLoot: true, BattleResultsStage.AwaitingOpen);
        if (frozen?.Results.BattleId != command.BattleId || frozen is null)
        { reason = BattleLootReasons.WrongBattle; return; }
        if (frozen.Results.Stage != BattleResultsStage.AwaitingOpen)
        { reason = DemoCampaignReasons.AlreadyApplied; return; }
        commit = () => _frozenBattleResults = frozen with { Results = frozen.Results with { Stage = BattleResultsStage.Open } };
    }

    private void PrepareAcknowledgeBattleResults(AcknowledgeBattleResultsCommand command, ref DemoCampaignData candidate,
        ref string? reason, ref Action? commit)
    {
        if (_frozenBattleResults is not { } frozen || frozen.Results.BattleId != command.BattleId)
        { reason = BattleLootReasons.WrongBattle; return; }
        if (frozen.Results.Stage != BattleResultsStage.Open)
        { reason = BattleLootReasons.LootClosed; return; }
        if (_lootOperations.Count != 0)
        { reason = BattleLootReasons.OperationsRunning; return; }
        if (frozen.Results.CanLoot)
        {
            PrepareFinishBattleLoot(new(command.CommandId, command.ExpectedRevision, command.BattleId, command.ConfirmDiscard),
                ref candidate, ref reason, ref commit);
            if (reason is not null) return;
        }
        commit = () =>
        {
            _frozenBattleResults = frozen with { Results = frozen.Results with { Stage = BattleResultsStage.Acknowledged } };
            SetTacticalBattleCommandBlock(false);
        };
    }
}
