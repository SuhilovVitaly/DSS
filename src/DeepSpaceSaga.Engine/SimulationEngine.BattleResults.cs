using System.Text.Json;
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

    private BattleResultsSnapshot? BuildBattleResultsProjection()
    {
        if (_frozenBattleResults is not { } frozen) return null;
        if (!BattleResultsPending || !frozen.Results.CanLoot && frozen.Map.SourceShipId is null) return frozen.Results;
        var ship = _objects.FirstOrDefault(obj => obj.InitialMotion.ObjectId == PlayerShipObjectId && !obj.IsDestroyed);
        return ship is null ? frozen.Results : frozen.Results with
        {
            Capacity = new(PlayerFreeHoldKg(ship), GetAvailablePrisonerCabins(ship, _demoCampaign))
        };
    }

    private BattleResultsData? CaptureFrozenBattleResults() => BattleResultsPending && _frozenBattleResults is { } frozen
        ? new(frozen.Results.BattleId, frozen.Results.Stage, frozen.Results.CanLoot ? null : frozen.Map, frozen.Results.ConsumedTorpedoes)
        : null;

    private static FrozenBattleResults? ValidateFrozenBattleResults(DemoCampaignData campaign, GameStateData state, SimulationSpeed speed)
    {
        if (campaign.BattleResults is not { } saved) return null;
        var loot = campaign.LootSession is { Phase: LootPhases.Open } open ? open : null;
        bool canLoot = saved.Map is null;
        var map = saved.Map ?? loot?.Map;
        if (map is null || string.IsNullOrWhiteSpace(saved.BattleId) || map.BattleId != saved.BattleId ||
            saved.Stage is not (BattleResultsStage.AwaitingOpen or BattleResultsStage.Open) || speed != SimulationSpeed.Speed0 ||
            canLoot != (loot is not null) || campaign.WorldEncounter is { Phase: not WorldEncounterPhases.Finished } ||
            campaign.PendingSensorReport is not null || state.DialogueState?.ActiveDialogue is not null ||
            map.Phase != TacticalBattlePhase.Over || map.PlayerSide is not ("A" or "B") ||
            map.Outcome is not { } outcome || !Enum.IsDefined(outcome.Kind) || outcome.Statistics is null ||
            outcome.Turns < 0 || outcome.Reasons.IsDefault || outcome.Losses.IsDefault ||
            outcome.Losses.Any(loss => loss is null || loss.Count < 0 || loss.Side is not ("A" or "B")) ||
            map.Units.IsDefaultOrEmpty || map.Units.Any(unit => unit is null || string.IsNullOrWhiteSpace(unit.Id) ||
                unit.Side is not ("A" or "B") || unit.Hull < 0 || unit.Hull > unit.MaxHull ||
                !Enum.IsDefined(unit.Kind) || !Enum.IsDefined(unit.State)) ||
            map.Units.Select(unit => unit.Id).Distinct(StringComparer.Ordinal).Count() != map.Units.Length)
            throw new ScenarioException("Invalid frozen battle results.");

        if (canLoot)
            map = map with { SourceShipId = state.PlayerShipObjectId };
        if (map.SourceShipId is { } shipId)
        {
            var ship = state.SpaceObjects.FirstOrDefault(obj => obj.ObjectId == shipId);
            var carrier = map.Units.FirstOrDefault(unit => unit.Side == map.PlayerSide && unit.Kind == TacticalUnitKind.Carrier);
            if (shipId != state.PlayerShipObjectId || ship?.CarrierState?.LastSettlement is not { } settlement ||
                settlement.BattleId != saved.BattleId || carrier is null || settlement.ShipDestroyed != (carrier.Hull == 0) ||
                settlement.ConsumedTorpedoes != saved.ConsumedTorpedoes)
                throw new ScenarioException("Frozen results do not match the committed sortie.");
        }
        else if (canLoot || saved.ConsumedTorpedoes is not null)
            throw new ScenarioException("Frozen results have no owning carrier.");

        var encounter = campaign.EncounterResults.FirstOrDefault(result => result.Binding.BattleId == saved.BattleId);
        if (encounter is not null && (encounter.PlayerSide != map.PlayerSide || encounter.PlayerWon != canLoot ||
            JsonSerializer.Serialize(encounter.Outcome) != JsonSerializer.Serialize(outcome) ||
            JsonSerializer.Serialize(encounter.Units) != JsonSerializer.Serialize(map.Units)))
            throw new ScenarioException("Frozen results do not match the encounter history.");

        return CreateFrozenResults(map, canLoot, saved.Stage, saved.ConsumedTorpedoes);
    }

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
        {
            int? torpedoes = PlayerShipObjectId is { } shipId && _carrierSettlement.TryGetValue(shipId, out var receipt) &&
                receipt.BattleId == legacy.BattleId ? receipt.ConsumedTorpedoes : null;
            frozen = CreateFrozenResults(legacy with { SourceShipId = PlayerShipObjectId }, canLoot: true, BattleResultsStage.AwaitingOpen, torpedoes);
        }
        if (frozen?.Results.BattleId != command.BattleId || frozen is null)
        { reason = BattleLootReasons.WrongBattle; return; }
        if (frozen.Results.Stage != BattleResultsStage.AwaitingOpen)
        { reason = DemoCampaignReasons.AlreadyApplied; return; }
        commit = () => _frozenBattleResults = frozen with { Results = frozen.Results with { Stage = BattleResultsStage.Open } };
    }

    private void PrepareAcknowledgeBattleResults(AcknowledgeBattleResultsCommand command, ref DemoCampaignData candidate,
        ref string? reason, ref Action? commit, bool deferRelease)
    {
        if (_frozenBattleResults is not { } frozen || frozen.Results.BattleId != command.BattleId)
        { reason = BattleLootReasons.WrongBattle; return; }
        if (frozen.Results.Stage != BattleResultsStage.Open)
        { reason = BattleLootReasons.LootClosed; return; }
        if (frozen.Results.CanLoot)
        {
            PrepareFinishBattleLoot(new(command.CommandId, command.ExpectedRevision, command.BattleId, command.ConfirmDiscard),
                ref candidate, ref reason, ref commit);
            if (reason is not null) return;
        }
        commit = () =>
        {
            _frozenBattleResults = frozen with { Results = frozen.Results with { Stage = BattleResultsStage.Acknowledged } };
            _encounterStoragePending = deferRelease;
            SetTacticalBattleCommandBlock(deferRelease);
        };
    }
}
