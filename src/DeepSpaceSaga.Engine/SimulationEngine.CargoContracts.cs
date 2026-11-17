using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

/// <summary>
/// EP-0034 US-0004: cargo contracts from sealing to payout. Rules:
/// <list type="bullet">
/// <item>Accepting a cargo offer through the single board command (<see cref="AcceptJobOfferCommand"/>) seals the
/// obligation's real mass into the hold in the same commit. Sealed cargo is not a module cargo stack: ordinary trade
/// cannot sell or spend it, and its mass is subtracted from the hold space available to the player's own goods.</item>
/// <item>A confirmed docking at the destination no later than the calendar deadline frees the hold, credits the payout
/// once (repaying contract debt first, like passenger payouts) and posts it under <c>cargo:{contractId}</c>.</item>
/// <item>A-01 (US-0004): cancelled or expired cargo is taken back without a payout at the next confirmed docking at a
/// human station (immediately when cancelled there); until then it occupies the hold. Cargo of a lost carrier is lost.
/// Penalties, debt and the failure record are the shared US-0003 rules.</item>
/// </list>
/// Tick transitions bump the campaign revision and commit sequence (autosave). Dispatch is explicit from
/// <c>SimulationEngine.DemoCampaign.cs</c>; the shared single-implementation partial hooks stay free.
/// </summary>
public sealed partial class SimulationEngine
{
    /// <summary>Whether the sealed cargo of a cargo obligation is still physically in the hold.</summary>
    private static bool CargoOccupiesHold(JobObligation obligation, DemoCampaignData data)
    {
        if (obligation.Type != StationJobTypes.Cargo) return false;
        if (obligation.Status == StationJobStatuses.Active) return true;
        if (obligation.Status is not (StationJobStatuses.Failed or StationJobStatuses.Cancelled)) return false;
        var failure = data.TransportFailures.FirstOrDefault(f => f.ContractId == obligation.ContractId);
        if (failure?.Reason == TransportFailureReasons.CarrierLost) return false;
        return data.CargoContracts.FirstOrDefault(c => c.ContractId == obligation.ContractId)?.ReturnedAtGameTimeMs is null;
    }

    /// <summary>Hold mass sealed for cargo contracts (active, or failed and not yet returned).</summary>
    private static long ContractCargoReservedKg(DemoCampaignData data) =>
        (data.StationJobs?.Obligations ?? []).Where(o => CargoOccupiesHold(o, data)).Sum(o => o.MassKg);

    /// <summary>Hold space left for the player's own goods: free module space minus sealed contract cargo.</summary>
    private long PlayerFreeHoldKg(SpaceObjectRuntime ship) =>
        Math.Max(0, CarrierFreeHoldKg(ship) - (ship.InitialMotion.ObjectId == PlayerShipObjectId ? ContractCargoReservedKg(_demoCampaign) : 0));

    /// <summary>A confirmed docking at a human station (AI bases do not take contract cargo back).</summary>
    private string? CargoReturnStation() => ConfirmedDockStation() is { } station && !IsAiBase(station) ? station : null;

    /// <summary>
    /// After the board prepared an applied cargo acceptance, attach the sealed-cargo record (durable posting id) to
    /// the same candidate; the reservation follows from the Active obligation.
    /// </summary>
    private void PrepareCargoSealing(DemoCampaignCommand command, ref DemoCampaignData candidate, bool handled, string? reason)
    {
        if (!handled || reason is not null || command is not AcceptJobOfferCommand accept) return;
        var obligation = candidate.StationJobs?.Obligations.FirstOrDefault(o => o.OfferId == accept.OfferId);
        if (obligation is null || obligation.Type != StationJobTypes.Cargo ||
            candidate.CargoContracts.Any(c => c.ContractId == obligation.ContractId))
            return;
        candidate = candidate with
        {
            CargoContracts = candidate.CargoContracts.Add(new CargoContractData(obligation.ContractId,
                CargoContractReasons.PostingIdFor(obligation.ContractId)))
        };
    }

    /// <summary>A cancellation applied while docked at a human station hands the sealed cargo back in the same commit.</summary>
    private void PrepareCargoCancellationReturn(TransportFailureData? cancellation, ref DemoCampaignData candidate)
    {
        if (cancellation is null || candidate.StationJobs is not { } jobs) return;
        var obligation = jobs.Obligations.FirstOrDefault(o => o.ContractId == cancellation.ContractId);
        if (obligation?.Type != StationJobTypes.Cargo || CargoReturnStation() is not { } station) return;
        candidate = candidate with
        {
            CargoContracts = WithCargoRecord(candidate.CargoContracts, obligation.ContractId,
                r => r with { ReturnedAtGameTimeMs = cancellation.FailedAtGameTimeMs, ReturnStationId = station })
        };
    }

    private static ImmutableArray<CargoContractData> WithCargoRecord(ImmutableArray<CargoContractData> records, string contractId,
        Func<CargoContractData, CargoContractData> update)
    {
        for (int i = 0; i < records.Length; i++)
            if (records[i].ContractId == contractId) return records.SetItem(i, update(records[i]));
        // Legacy US-0006 acceptance never created a record: settle it once now.
        return records.Add(update(new CargoContractData(contractId, CargoContractReasons.PostingIdFor(contractId))));
    }

    /// <summary>
    /// Idempotent calendar step: deliver every active cargo contract of the station the carrier is confirmed docked at.
    /// A delivered contract is Completed, so repeated snapshots, saves or loads never pay twice. Arrival later than the
    /// deadline pays nothing and leaves the contract to the failure rules of US-0003.
    /// </summary>
    private void DeliverCargoContracts(long gameTimeMs)
    {
        if (!StationJobsEnabled || _demoCampaign.StationJobs is not { } jobs) return;
        if (ConfirmedDockStation() is not { } station) return;
        bool Deliverable(JobObligation o) => o.Status == StationJobStatuses.Active && o.Type == StationJobTypes.Cargo &&
            o.DestinationStationId == station;
        if (!jobs.Obligations.Any(Deliverable)) return;

        var arrivalLedger = ArrivalLedger(station);
        long arrivalBase = DockArrivalTime(station, gameTimeMs);
        var obligations = jobs.Obligations;
        var records = _demoCampaign.CargoContracts;
        long credits = PlayerCredits;
        var postings = new List<(string PostingId, long Credits)>();
        foreach (var obligation in jobs.Obligations.Where(Deliverable)
                     .OrderBy(o => o.AcceptedAtGameTimeMs).ThenBy(o => o.ContractId, StringComparer.Ordinal))
        {
            long arrival = Math.Max(arrivalBase, obligation.AcceptedAtGameTimeMs);
            if (arrival > obligation.DeadlineGameTimeMs) continue; // overdue: no payout here
            records = WithCargoRecord(records, obligation.ContractId,
                r => r with { DeliveredAtGameTimeMs = arrival, PaidCredits = obligation.Payout });
            credits = checked(credits + obligation.Payout);
            obligations = obligations.SetItem(obligations.IndexOf(obligation),
                obligation with { Status = StationJobStatuses.Completed, ClosedAtGameTimeMs = arrival });
            postings.Add((CargoContractReasons.PostingIdFor(obligation.ContractId), obligation.Payout));
        }
        if (postings.Count == 0) return;

        // EP-0034 US-0003: contract debt is repaid from the payouts just credited, oldest failure first.
        var failures = RepayTransportDebt(_demoCampaign.TransportFailures, postings.Sum(p => p.Credits), out long repaid);
        credits = checked(credits - repaid);
        var candidate = _demoCampaign with
        {
            StationJobs = jobs with { Obligations = obligations },
            CargoContracts = records,
            TransportFailures = failures,
            Revision = checked(_demoCampaign.Revision + 1),
            CommitSequence = checked(_demoCampaign.CommitSequence + 1)
        };
        DemoCampaignData.Validate(candidate, Math.Max(gameTimeMs, candidate.LastTickGameTimeMs));
        ValidateDemoCampaignSections(candidate, Math.Max(gameTimeMs, candidate.LastTickGameTimeMs));
        // Commit: campaign receipt (hold freed), money and the arrival voyage posting together.
        _demoCampaign = candidate;
        PlayerCredits = credits;
        if (arrivalLedger is not null)
            foreach (var (postingId, amount) in postings)
                RecordVoyageAmount(postingId, VoyageAmountKind.PassengerPayout, amount);
    }

    /// <summary>
    /// Idempotent calendar step after the failure rules: a confirmed docking at a human station takes back the sealed
    /// cargo of cancelled or expired contracts, without a payout. A returned record is never processed again.
    /// </summary>
    private void ReturnFailedContractCargo(long gameTimeMs)
    {
        if (!StationJobsEnabled || _demoCampaign.StationJobs is not { } jobs) return;
        if (CargoReturnStation() is not { } station) return;
        var pending = jobs.Obligations
            .Where(o => o.Status != StationJobStatuses.Active && CargoOccupiesHold(o, _demoCampaign))
            .OrderBy(o => o.ClosedAtGameTimeMs).ThenBy(o => o.ContractId, StringComparer.Ordinal).ToArray();
        if (pending.Length == 0) return;

        long arrival = DockArrivalTime(station, gameTimeMs);
        var records = _demoCampaign.CargoContracts;
        foreach (var obligation in pending)
        {
            long failedAt = _demoCampaign.TransportFailures.First(f => f.ContractId == obligation.ContractId).FailedAtGameTimeMs;
            records = WithCargoRecord(records, obligation.ContractId,
                r => r with { ReturnedAtGameTimeMs = Math.Max(failedAt, arrival), ReturnStationId = station });
        }
        var candidate = _demoCampaign with
        {
            CargoContracts = records,
            Revision = checked(_demoCampaign.Revision + 1),
            CommitSequence = checked(_demoCampaign.CommitSequence + 1)
        };
        DemoCampaignData.Validate(candidate, Math.Max(gameTimeMs, candidate.LastTickGameTimeMs));
        ValidateDemoCampaignSections(candidate, Math.Max(gameTimeMs, candidate.LastTickGameTimeMs));
        _demoCampaign = candidate;
    }

    private CargoContractsSnapshot? BuildCargoContractsProjection()
    {
        if (!StationJobsEnabled) return null;
        long now = StationJobsNow;
        var jobs = _demoCampaign.StationJobs ?? EmptyStationJobs;
        var contracts = ImmutableArray.CreateBuilder<CargoContract>();
        foreach (var obligation in jobs.Obligations.Where(o => o.Type == StationJobTypes.Cargo)
                     .OrderBy(o => o.AcceptedAtGameTimeMs).ThenBy(o => o.ContractId, StringComparer.Ordinal))
        {
            var record = _demoCampaign.CargoContracts.FirstOrDefault(c => c.ContractId == obligation.ContractId);
            var failure = _demoCampaign.TransportFailures.FirstOrDefault(f => f.ContractId == obligation.ContractId);
            string state = obligation.Status switch
            {
                StationJobStatuses.Completed => CargoHoldStates.Delivered,
                StationJobStatuses.Active => now <= obligation.DeadlineGameTimeMs ? CargoHoldStates.Aboard : CargoHoldStates.Overdue,
                _ when failure?.Reason == TransportFailureReasons.CarrierLost => CargoHoldStates.Lost,
                _ => record?.ReturnedAtGameTimeMs is null ? CargoHoldStates.ReturnPending : CargoHoldStates.Returned
            };
            contracts.Add(new CargoContract(obligation.ContractId, obligation.OfferId, obligation.ItemTypeId ?? "",
                obligation.MassKg, obligation.Count, obligation.OriginStationId, obligation.DestinationStationId,
                obligation.AcceptedAtGameTimeMs, obligation.DeadlineGameTimeMs, obligation.Payout, obligation.Status,
                record?.PostingId ?? CargoContractReasons.PostingIdFor(obligation.ContractId), state,
                record?.DeliveredAtGameTimeMs, record?.PaidCredits, record?.ReturnedAtGameTimeMs, record?.ReturnStationId));
        }
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        long capacity = 0, playerMass = 0;
        if (ship is not null && !ship.IsDestroyed)
            foreach (var module in ship.Modules)
            {
                var moduleType = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
                if (moduleType.CargoCapacityKg is not > 0 || module.StructurePoints <= 0 || module.Destroyed) continue;
                capacity = checked(capacity + moduleType.CargoCapacityKg.Value);
                playerMass = checked(playerMass + ComputeCargoMassKg(module.Cargo));
            }
        return new CargoContractsSnapshot(contracts.ToImmutable(), now, ContractCargoReservedKg(_demoCampaign), capacity, playerMass);
    }

    /// <summary>Load cross-check before the running session is replaced: every sealed item is a known item type.</summary>
    private void ValidateCargoContractsForLoad(DemoCampaignData data)
    {
        foreach (var obligation in data.StationJobs?.Obligations ?? [])
            if (obligation.Type == StationJobTypes.Cargo && !_registry.ItemTypes.Contains(obligation.ItemTypeId!))
                throw new ScenarioException("Invalid demo campaign cargo contracts: unknown sealed item type.");
    }
}
