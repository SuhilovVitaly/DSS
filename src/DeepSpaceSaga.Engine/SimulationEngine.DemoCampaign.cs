using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private DemoCampaignData _demoCampaign = new();
    private DemoCampaignDefinitions _demoDefinitions = new(null, null);

    internal void ConfigureDemoCampaign(DemoCampaignDefinitions definitions)
    {
        definitions.ValidateReferences(_registry);
        _demoDefinitions = definitions;
    }

    public DemoCampaignResponse ExecuteDemoCampaign(DemoCampaignCommand command) => ExecuteDemoCampaign(command, out _);

    /// <summary>Reports a new application atomically, so transport never checkpoints a receipt replay.</summary>
    public DemoCampaignResponse ExecuteDemoCampaign(DemoCampaignCommand command, out bool newlyApplied)
    {
        lock (_worldStateLock)
        {
            newlyApplied = false;
            if (command is null || string.IsNullOrWhiteSpace(command.CommandId))
                return new(command?.CommandId ?? "", false, DemoCampaignReasons.InvalidIdentity, _demoCampaign.Revision);

            // Serialization identifies payloads; dispatch remains explicit partial handlers, never reflection.
            var bytes = Encoding.UTF8.GetBytes(command.GetType().FullName + "\n" + JsonSerializer.Serialize(command, command.GetType()));
            string hash = Convert.ToHexString(SHA256.HashData(bytes));
            var previous = _demoCampaign.Receipts.FirstOrDefault(r => r.Response.CommandId == command.CommandId);
            if (previous is not null)
                return previous.PayloadHash == hash ? previous.Response.Rewards is not null ? previous.Response
                    : previous.Response with { Rewards = BuildDemoRewardsProjection(), Revision = _demoCampaign.Revision }
                    : new(command.CommandId, false, DemoCampaignReasons.IdentityConflict, _demoCampaign.Revision);
            if (_demoCampaign.CommittedCommandHashes.TryGetValue(command.CommandId, out string? committedHash))
                return new(command.CommandId, false, committedHash == hash ? DemoCampaignReasons.AlreadyApplied : DemoCampaignReasons.IdentityConflict, _demoCampaign.Revision);

            string? reason = null;
            var candidate = _demoCampaign;
            bool handled = false;
            Action? commit = null;
            if (command.ExpectedRevision != _demoCampaign.Revision) reason = DemoCampaignReasons.StaleRevision;
            else if (_tacticalBattleSession is not null && command is not SetLootDebugRequest) reason = DemoCampaignReasons.BattleActive;
            else if (BattleResultsPending && !IsBattleResultsCommandAllowed(command)) reason = DemoCampaignReasons.BattleActive;
            else if (LootOpen && command is not (ILootCampaignCommand or OpenBattleResultsCommand or AcknowledgeBattleResultsCommand or SetLootDebugRequest))
                reason = DemoCampaignReasons.BattleActive;
            else if (_encounterStoragePending) reason = "encounter_checkpoint_pending";
            else if (HasCapturedEncounter && command is not (BeginEncounterBattleCommand or DismissSensorContactReportRequest or SetFogOfWarRequest or SetLootDebugRequest))
                reason = "encounter_captured";
            else if (command is not (OpenBattleResultsCommand or AcknowledgeBattleResultsCommand) &&
                _objects.Any(o => o.InitialMotion.ObjectId == PlayerShipObjectId && o.IsDestroyed))
                reason = DemoCampaignReasons.CarrierDestroyed;
            else
                PrepareDemoCampaignCommand(command, ref candidate, ref handled, ref reason, ref commit);

            reason ??= handled ? null : DemoCampaignReasons.Unsupported;
            bool applied = handled && reason is null;
            long revision = applied ? checked(_demoCampaign.Revision + 1) : _demoCampaign.Revision;
            long sequence = checked(_demoCampaign.CommitSequence + 1);
            var response = new DemoCampaignResponse(command.CommandId, applied, reason, revision,
                applied ? candidate.Company?.StationRepair : null,
                applied ? candidate.Company?.Supply : null,
                applied ? candidate.Company?.Replacement : null,
                applied ? candidate.Company?.Recruitment : null,
                applied ? candidate.Company?.Upkeep : null);
            candidate = (applied ? candidate : _demoCampaign) with
            {
                Revision = revision,
                CommitSequence = sequence,
                Receipts = (_demoCampaign.Receipts.Length >= 1024 ? _demoCampaign.Receipts.RemoveRange(0, _demoCampaign.Receipts.Length - 1023) : _demoCampaign.Receipts).Add(new(hash, sequence, response))
            };
            if (applied && command is not (RepairQuoteRequest or DecoyQuoteRequest))
                candidate = candidate with { CommittedCommandHashes = candidate.CommittedCommandHashes.Add(command.CommandId, hash) };
            ValidateDemoCampaignSections(candidate, Math.Max(_clock.GameTimeMs, candidate.LastTickGameTimeMs));
            if (applied) commit?.Invoke();
            _demoCampaign = candidate;
            if (applied && command is BeginEncounterBattleCommand { RequireBattleResults: true } &&
                _tacticalBattleSession?.Phase == TacticalBattlePhase.Over)
                FreezeBattleResults();
            response = response with { Rewards = BuildDemoRewardsProjection(), Revision = _demoCampaign.Revision };
            _demoCampaign = _demoCampaign with { Receipts = _demoCampaign.Receipts.SetItem(_demoCampaign.Receipts.Length - 1, new(hash, sequence, response)) };
            newlyApplied = applied;
            return response;
        }
    }

    private void TickDemoCampaign(long gameTimeMs)
    {
        if (LootOpen || BattleResultsPending) return;
        ObservePirateGroups(gameTimeMs);
        PublishCapturedEncounter();
        OpenPendingSensorReport(_processedSimulationTimeMs);
        PublishStationJobs(gameTimeMs); // EP-0034 US-0006: idempotent per calendar bucket, also while paused
        DeliverPassengerContracts(gameTimeMs); // EP-0034 US-0002: idempotent, a Completed contract never pays again
        DeliverCargoContracts(gameTimeMs); // EP-0034 US-0004: idempotent, a Completed contract never pays again
        ResolveTransportFailures(gameTimeMs); // EP-0034 US-0003: expiry/loss/disembarkation, each obligation closes once
        ReturnFailedContractCargo(gameTimeMs); // EP-0034 US-0004: failed cargo is taken back once at a human station
        if (gameTimeMs <= _demoCampaign.LastTickGameTimeMs) return;
        var candidate = _demoCampaign;
        TickDemoCampaignSections(gameTimeMs, ref candidate);
        DemoCampaignData.Validate(candidate, gameTimeMs);
        _demoCampaign = candidate with { LastTickGameTimeMs = gameTimeMs };
    }

    private DemoCampaignSnapshot BuildDemoCampaignProjection() => new(
        _demoCampaign.Revision, _demoCampaign.CommitSequence, BuildDemoCompanyProjection(),
        BuildDemoTransportProjection(), BuildPirateGroupsProjection(), BuildDemoRewardsProjection(),
        _demoCampaign.Progression, _demoCampaign.Story, BuildDemoStartProjection(), BuildPlayerSensorProjection(),
        BuildSensorOperatorProjection(), new(DemoDebugSettings.FogOfWar), _demoCampaign.PendingSensorReport, BuildWorldEncounterProjection(), BuildThreatRoutes(), DemoDebugSettings)
    { BattleResults = BuildBattleResultsProjection() };

    private DemoCampaignData CaptureDemoCampaignState() => _demoCampaign with
    {
        // Rewards are a derived view of the one authoritative ledger, not receipt history.
        Receipts = _demoCampaign.Receipts.Select(r => r with { Response = r.Response with { Rewards = null } }).ToImmutableArray()
    };

    // Central dispatch owns ordering. Feature helpers prepare candidates and deferred commits;
    // unsupported or rejected commands never mutate gameplay state.
    private void PrepareDemoCampaignCommand(DemoCampaignCommand command, ref DemoCampaignData candidate,
        ref bool handled, ref string? reason, ref Action? commit)
    {
        handled = true;
        switch (command)
        {
            case OpenBattleResultsCommand open:
                PrepareOpenBattleResults(open, ref reason, ref commit);
                break;
            case AcknowledgeBattleResultsCommand acknowledge:
                PrepareAcknowledgeBattleResults(acknowledge, ref candidate, ref reason, ref commit);
                break;
            case SetLootDebugRequest debug:
                if (debug.EngineerBonusPp is not (null or 0 or >= 10 and <= 15) ||
                    debug.DestructionRolls is not ("auto" or "keep" or "destroy") || debug.Attempts is not ("auto" or "success" or "fail"))
                    reason = DebugTuningReasonCodes.ValueOutOfRange;
                else commit = () => SetLootDebug(debug);
                break;
            case ArrestPilotCommand arrest:
                PrepareArrestPilot(arrest, ref candidate, ref reason);
                break;
            case FinishBattleLootCommand finish:
                PrepareFinishBattleLoot(finish, ref candidate, ref reason, ref commit);
                break;
            case StartLootOperationsCommand start:
                PrepareStartLootOperations(start, ref reason, ref commit);
                break;
            case AdvanceLootOperationsCommand advance:
                PrepareAdvanceLootOperations(advance, ref candidate, ref reason, ref commit);
                break;
            case ClaimExtractedLootCommand claimLoot:
                PrepareClaimExtractedLoot(claimLoot, ref candidate, ref reason, ref commit);
                break;
            case ILootCampaignCommand:
                PrepareBattleLootCommand(command, ref candidate, ref reason, ref commit);
                break;
            case ClaimBountyCommand claim:
                PrepareBountyPayment(claim, ref candidate, ref reason, ref commit);
                break;
            case AcceptBountyCommand or CancelBountyCommand:
                PrepareTargetBounty(command, ref candidate, ref reason);
                break;
            case SelectDemoRouteCommand route:
                PrepareThreatRoute(route, ref reason, ref commit);
                break;
            case BeginEncounterBattleCommand begin:
                PrepareEncounterBattle(begin, ref candidate, ref reason, ref commit);
                break;
            case DismissSensorContactReportRequest dismiss:
                if (candidate.PendingSensorReport?.BatchId != dismiss.BatchId) reason = "sensor_batch_mismatch";
                else if (_dialogue.Active is { DialogueDefinitionId: not SensorReportDialogueId }) reason = "dialogue_active";
                else
                {
                    candidate = candidate with { PendingSensorReport = null };
                    commit = () =>
                    {
                        if (_dialogue.Active is not null) EndDialogue(_processedSimulationTimeMs, dismiss.CommandId, "dialogue_completed");
                        else _clock.Reset(_processedWorldTimeMs, SimulationSpeed.Speed0, _processedSimulationTimeMs, _clock.Capture().MotionQuarterRemainder);
                    };
                }
                break;
            case SetFogOfWarRequest fog:
                commit = () => ConfigureFogOfWar(fog.Enabled);
                break;
            case PayCrewDebtCommand debt:
                PrepareCrewDebt(debt, ref candidate, ref reason, ref commit);
                break;
            case HirePilotCommand or AssignPilotCommand:
                PrepareRecruitment(command, ref candidate, ref reason, ref commit);
                break;
            case PurchaseDroneCommand or EquipDroneCommand:
                PrepareReplacement(command, ref candidate, ref reason, ref commit);
                break;
            case DecoyQuoteRequest or ResupplyDecoyCommand:
                PrepareSupply(command, ref candidate, ref reason, ref commit);
                break;
            case RepairQuoteRequest or RepairCommand:
                PrepareStationRepair(command, ref candidate, ref reason, ref commit);
                break;
            default:
                handled = false;
                PrepareStationJobsCommand(command, ref candidate, ref handled, ref reason);
                var boarding = PreparePassengerBoarding(command, ref candidate, handled, ref reason);
                var cancellation = PrepareTransportCancellation(command, ref candidate, ref handled, ref reason);
                PrepareCargoSealing(command, ref candidate, handled, reason);
                PrepareCargoCancellationReturn(cancellation, ref candidate);
                if (reason is null && handled)
                    commit = () =>
                    {
                        if (boarding is not null) BoardPassengers(boarding);
                        if (cancellation is not null) ApplyTransportCancellation(cancellation);
                    };
                break;
        }
    }

    private void TickDemoCampaignSections(long gameTimeMs, ref DemoCampaignData candidate) =>
        TickCrewUpkeep(gameTimeMs, ref candidate);

    private void ValidateDemoCampaignSections(DemoCampaignData candidate, long? gameTimeMs = null) =>
        DemoCampaignData.Validate(candidate, gameTimeMs ?? Math.Max(_clock.GameTimeMs, _demoCampaign.LastTickGameTimeMs));
}
