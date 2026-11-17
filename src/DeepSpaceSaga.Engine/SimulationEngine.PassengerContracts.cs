using System.Collections.Immutable;
using System.Globalization;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

/// <summary>
/// EP-0034 US-0002: passenger contracts from boarding to payout. Accepting a passenger offer through the single
/// board command (<see cref="AcceptJobOfferCommand"/>) boards real passengers into free cabins in the same commit;
/// they eat rations like everyone aboard. A confirmed docking at the destination no later than the calendar deadline
/// disembarks exactly those passengers, credits the payout once and posts it to the arrival voyage ledger under the
/// durable posting id. Everything is dispatched explicitly from <c>SimulationEngine.DemoCampaign.cs</c>; the shared
/// single-implementation partial hooks stay free for other epics.
/// </summary>
public sealed partial class SimulationEngine
{
    private const string PassengerIdInfix = ":pax-";

    internal static string PassengerIdFor(string contractId, int ordinal) =>
        string.Create(CultureInfo.InvariantCulture, $"{contractId}{PassengerIdInfix}{ordinal}");

    /// <summary>
    /// After the board prepared an applied passenger acceptance, attach the boarding record (stable passenger ids
    /// and posting id) to the same candidate. Returns the record to board after the commit, or null.
    /// </summary>
    private PassengerContractData? PreparePassengerBoarding(DemoCampaignCommand command, ref DemoCampaignData candidate,
        bool handled, ref string? reason)
    {
        if (!handled || reason is not null || command is not AcceptJobOfferCommand accept) return null;
        var obligation = candidate.StationJobs?.Obligations.FirstOrDefault(o => o.OfferId == accept.OfferId);
        if (obligation is null || !StationJobTypes.CarriesPassengers(obligation.Type) ||
            candidate.PassengerContracts.Any(c => c.ContractId == obligation.ContractId))
            return null;
        var ids = Enumerable.Range(1, obligation.Count).Select(n => PassengerIdFor(obligation.ContractId, n)).ToImmutableArray();
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (ship is null || _objects.Any(o => !o.Passengers.IsDefault && o.Passengers.Any(p => ids.Contains(p.PassengerId))))
        {
            reason = DemoCampaignReasons.Unavailable; // never board over an existing identity
            return null;
        }
        var record = new PassengerContractData(obligation.ContractId, ids, PassengerContractReasons.PostingIdFor(obligation.ContractId));
        candidate = candidate with { PassengerContracts = candidate.PassengerContracts.Add(record) };
        return record;
    }

    /// <summary>World half of an applied acceptance: the passengers take their cabins. Call under the world lock.</summary>
    private void BoardPassengers(PassengerContractData record)
    {
        int index = _objects.FindIndex(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (index < 0) return;
        var ship = _objects[index];
        var aboard = ship.Passengers.IsDefault ? [] : ship.Passengers;
        _objects[index] = ship with
        {
            Passengers = aboard.AddRange(record.PassengerIds.Select(id => new ShipPassengerData(id, id)))
        };
    }

    /// <summary>
    /// Idempotent calendar step: deliver every active passenger contract of the station the carrier is confirmed
    /// docked at. A delivered contract is Completed, so repeated snapshots, saves or loads never pay twice.
    /// Arrival later than the deadline pays nothing and leaves the contract to the failure rules of US-0003.
    /// </summary>
    private void DeliverPassengerContracts(long gameTimeMs)
    {
        if (!StationJobsEnabled || _demoCampaign.StationJobs is not { } jobs) return;
        if (CombatOrLootOpen || _dialogue.Active is not null || _voyageState is { Phase: VoyagePhases.Docking })
            return; // docking is confirmed only after its dialogue has finished
        int shipIndex = _objects.FindIndex(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (shipIndex < 0) return;
        var ship = _objects[shipIndex];
        if (ship.IsDestroyed || !ship.IsDocked || ship.DockedStationObjectId is not { } station) return;
        if (!jobs.Obligations.Any(o => IsDeliverable(o, station))) return;

        var arrivalLedger = ArrivalLedger(station);
        long arrivalBase = Math.Min(gameTimeMs, arrivalLedger?.Finance.CompletedGameTimeMs ?? gameTimeMs);
        var obligations = jobs.Obligations;
        var contracts = _demoCampaign.PassengerContracts;
        var passengers = ship.Passengers.IsDefault ? [] : ship.Passengers;
        long credits = PlayerCredits;
        var postings = new List<(string PostingId, long Credits)>();
        foreach (var obligation in jobs.Obligations.Where(o => IsDeliverable(o, station))
                     .OrderBy(o => o.AcceptedAtGameTimeMs).ThenBy(o => o.ContractId, StringComparer.Ordinal))
        {
            long arrival = Math.Max(arrivalBase, obligation.AcceptedAtGameTimeMs);
            if (arrival > obligation.DeadlineGameTimeMs) continue; // overdue: no payout here
            int recordIndex = -1;
            for (int i = 0; i < contracts.Length; i++)
                if (contracts[i].ContractId == obligation.ContractId) recordIndex = i;
            var record = recordIndex >= 0 ? contracts[recordIndex]
                // Legacy US-0006 acceptance never boarded anyone: nobody to disembark, the obligation still closes once.
                : new PassengerContractData(obligation.ContractId, [], PassengerContractReasons.PostingIdFor(obligation.ContractId));
            var leaving = record.PassengerIds.ToHashSet(StringComparer.Ordinal);
            passengers = passengers.RemoveAll(p => leaving.Contains(p.PassengerId));
            credits = checked(credits + obligation.Payout);
            var delivered = record with { DeliveredAtGameTimeMs = arrival, PaidCredits = obligation.Payout };
            contracts = recordIndex >= 0 ? contracts.SetItem(recordIndex, delivered) : contracts.Add(delivered);
            obligations = obligations.SetItem(obligations.IndexOf(obligation),
                obligation with { Status = StationJobStatuses.Completed, ClosedAtGameTimeMs = arrival });
            postings.Add((record.PostingId, obligation.Payout));
        }
        if (postings.Count == 0) return;

        // EP-0034 US-0003: contract debt is repaid from the payouts just credited, oldest failure first.
        var failures = RepayTransportDebt(_demoCampaign.TransportFailures, postings.Sum(p => p.Credits), out long repaid);
        credits = checked(credits - repaid);
        // EP-0034 US-0005: a payout is a campaign commit like every other transport transition (cargo delivery,
        // failures), so the autosave captures it instead of the pre-payout docking state.
        var candidate = _demoCampaign with
        {
            StationJobs = jobs with { Obligations = obligations }, PassengerContracts = contracts, TransportFailures = failures,
            Revision = checked(_demoCampaign.Revision + 1),
            CommitSequence = checked(_demoCampaign.CommitSequence + 1)
        };
        DemoCampaignData.Validate(candidate, Math.Max(gameTimeMs, candidate.LastTickGameTimeMs));
        ValidateDemoCampaignSections(candidate, Math.Max(gameTimeMs, candidate.LastTickGameTimeMs));
        // Commit: campaign receipt, people, money and the arrival voyage posting together.
        _demoCampaign = candidate;
        _objects[shipIndex] = ship with { Passengers = passengers };
        PlayerCredits = credits;
        if (arrivalLedger is not null)
            foreach (var (postingId, amount) in postings)
                RecordVoyageAmount(postingId, VoyageAmountKind.PassengerPayout, amount);
    }

    private static bool IsDeliverable(JobObligation obligation, string station) =>
        obligation.Status == StationJobStatuses.Active && StationJobTypes.CarriesPassengers(obligation.Type) &&
        obligation.DestinationStationId == station;

    /// <summary>The voyage that brought the carrier to this station, if the latest ledger is that arrival.</summary>
    private VoyageLedgerEntry? ArrivalLedger(string station)
    {
        if (_voyageLedgers.IsDefaultOrEmpty) return null;
        var latest = _voyageLedgers[^1];
        return latest.Finance is { State: VoyageFinanceStates.AwaitingRealization, CompletedGameTimeMs: not null } f &&
            f.DestinationStationObjectId == station ? latest : null;
    }

    /// <summary>Seats promised by legacy active obligations that never boarded anyone (US-0006 saves).</summary>
    private long PassengerSeatsAwaitingBoarding(IEnumerable<JobObligation> active) => active
        .Where(o => StationJobTypes.CarriesPassengers(o.Type) &&
            !_demoCampaign.PassengerContracts.Any(c => c.ContractId == o.ContractId && !c.PassengerIds.IsEmpty))
        .Sum(o => (long)o.Count);

    private PassengerContractsSnapshot? BuildPassengerContractsProjection()
    {
        if (!StationJobsEnabled) return null;
        long now = StationJobsNow;
        var jobs = _demoCampaign.StationJobs ?? EmptyStationJobs;
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        var aboard = ship is null || ship.Passengers.IsDefault
            ? new HashSet<string>(StringComparer.Ordinal)
            : ship.Passengers.Select(p => p.PassengerId).ToHashSet(StringComparer.Ordinal);
        int passengersAboard = 0;
        var contracts = ImmutableArray.CreateBuilder<PassengerContract>();
        foreach (var obligation in jobs.Obligations.Where(o => StationJobTypes.CarriesPassengers(o.Type))
                     .OrderBy(o => o.AcceptedAtGameTimeMs).ThenBy(o => o.ContractId, StringComparer.Ordinal))
        {
            var record = _demoCampaign.PassengerContracts.FirstOrDefault(c => c.ContractId == obligation.ContractId);
            var ids = record?.PassengerIds ?? [];
            passengersAboard += ids.Count(aboard.Contains); // also failed contracts still waiting to disembark (US-0003)
            string state = obligation.Status == StationJobStatuses.Completed && record?.DeliveredAtGameTimeMs is not null
                ? PassengerDeliveryStates.Delivered
                : obligation.Status == StationJobStatuses.Active && now <= obligation.DeadlineGameTimeMs
                    ? PassengerDeliveryStates.Aboard
                    : obligation.Status == StationJobStatuses.Active
                        ? PassengerDeliveryStates.Overdue
                        : PassengerDeliveryStates.Failed; // EP-0034 US-0003: expired, cancelled or lost
            contracts.Add(new PassengerContract(obligation.ContractId, obligation.OfferId, obligation.Type,
                obligation.OriginStationId, obligation.DestinationStationId, obligation.Count, obligation.AcceptedAtGameTimeMs,
                obligation.DeadlineGameTimeMs, obligation.Payout, obligation.Status,
                record?.PostingId ?? PassengerContractReasons.PostingIdFor(obligation.ContractId), ids, state,
                record?.DeliveredAtGameTimeMs, record?.PaidCredits));
        }
        return new PassengerContractsSnapshot(contracts.ToImmutable(), now, passengersAboard);
    }

    /// <summary>
    /// Load cross-check against the candidate world, before the running session is replaced: passengers of an
    /// active contract are aboard the player carrier, delivered passengers are aboard nowhere.
    /// </summary>
    private static void ValidatePassengerContractsForLoad(DemoCampaignData data, GameStateData state)
    {
        if (data.PassengerContracts.IsDefaultOrEmpty) return;
        var player = state.SpaceObjects.FirstOrDefault(o => string.Equals(o.ObjectId, state.PlayerShipObjectId, StringComparison.OrdinalIgnoreCase));
        var playerAboard = (player?.Passengers ?? []).Select(p => p.PassengerId).ToHashSet(StringComparer.Ordinal);
        var anywhere = state.SpaceObjects.SelectMany(o => o.Passengers ?? []).Select(p => p.PassengerId).ToHashSet(StringComparer.Ordinal);
        foreach (var record in data.PassengerContracts)
        {
            var obligation = data.StationJobs!.Obligations.First(o => o.ContractId == record.ContractId);
            if (obligation.Status == StationJobStatuses.Active && !record.PassengerIds.All(playerAboard.Contains))
                throw new ScenarioException("Invalid demo campaign passenger contracts: active passengers are not aboard.");
            if (record.DeliveredAtGameTimeMs is not null && record.PassengerIds.Any(anywhere.Contains))
                throw new ScenarioException("Invalid demo campaign passenger contracts: delivered passengers are still aboard.");
        }
    }
}
