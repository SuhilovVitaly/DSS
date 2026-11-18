using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

/// <summary>
/// EP-0034 US-0003: the single terminal transition of an active transport obligation besides delivery —
/// Cancelled (player command), Expired (calendar deadline passed) or Lost (carrier destroyed). Rules:
/// <list type="bullet">
/// <item>A-01: arrival at or before the deadline wins; expiry happens only when calendar time is past the deadline and
/// is recorded at <c>deadline + 1</c>, so the outcome does not depend on how time is split. Pause does not move it.</item>
/// <item>A-02: cancellation/expiry cost a content permille of the promised payout (default 25%), rounded up. Available
/// Tokens are taken, the rest is contract debt repaid from later contract payouts; debt never blocks peaceful work.</item>
/// <item>Passengers of a failed contract stay aboard until the next confirmed docking (any station), then leave without a
/// payout; they are never removed in open space. A lost carrier keeps its people with the wreck, without a penalty.</item>
/// </list>
/// Transitions bump the campaign revision and commit sequence, which drives the autosave. Cargo obligations (US-0004)
/// reuse the same failure record with an empty passenger list. Dispatch is explicit from
/// <c>SimulationEngine.DemoCampaign.cs</c>; the shared single-implementation partial hooks stay free.
/// </summary>
public sealed partial class SimulationEngine
{
    private const int DefaultTransportPenaltyPermille = 250;

    private int CancelPenaltyPermille => _demoDefinitions.Jobs?.Penalties?.CancelPermille ?? DefaultTransportPenaltyPermille;
    private int ExpiryPenaltyPermille => _demoDefinitions.Jobs?.Penalties?.ExpiryPermille ?? DefaultTransportPenaltyPermille;

    /// <summary>Transport orders that can fail or be cancelled (bounty belongs to EP-0036).</summary>
    internal static bool IsTransportJob(string? type) => StationJobTypes.CarriesPassengers(type) || type == StationJobTypes.Cargo;

    /// <summary>Penalty = ceil(payout x permille / 1000) whole Tokens.</summary>
    internal static long TransportPenalty(long payout, int permille) => CeilDiv(checked(payout * permille), 1000);

    /// <summary>Station of a confirmed docking (dialogue finished, no battle, not mid-docking), or null.</summary>
    private string? ConfirmedDockStation()
    {
        if (CombatOrLootOpen || _dialogue.Active is not null || _voyageState is { Phase: VoyagePhases.Docking })
            return null;
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        return ship is null || ship.IsDestroyed || !ship.IsDocked ? null : ship.DockedStationObjectId;
    }

    /// <summary>Arrival time at a docked station: the arrival voyage completion if it is the latest ledger, else now.</summary>
    private long DockArrivalTime(string station, long gameTimeMs) =>
        Math.Min(gameTimeMs, ArrivalLedger(station)?.Finance.CompletedGameTimeMs ?? gameTimeMs);

    /// <summary>
    /// Prepare a cancellation on the candidate state: the obligation becomes Cancelled and the failure record holds
    /// the penalty split and, when docked, the immediate disembarkation. Returns the record to apply after the commit.
    /// </summary>
    private TransportFailureData? PrepareTransportCancellation(DemoCampaignCommand command, ref DemoCampaignData candidate,
        ref bool handled, ref string? reason)
    {
        if (handled || reason is not null || command is not CancelTransportCommand cancel) return null;
        handled = true;
        if (!StationJobsEnabled) { reason = DemoCampaignReasons.Unavailable; return null; }
        var jobs = candidate.StationJobs ?? EmptyStationJobs;
        var obligation = string.IsNullOrWhiteSpace(cancel.ContractId) ? null
            : jobs.Obligations.FirstOrDefault(o => o.ContractId == cancel.ContractId);
        reason = obligation is null ? TransportFailureReasons.ContractNotFound
            : !IsTransportJob(obligation.Type) ? TransportFailureReasons.NotCancellable
            : obligation.Status != StationJobStatuses.Active ? TransportFailureReasons.ContractNotActive
            : null;
        if (reason is not null) return null;

        long now = StationJobsNow;
        long penalty = TransportPenalty(obligation!.Payout, CancelPenaltyPermille);
        long paid = Math.Min(Math.Max(0, PlayerCredits), penalty);
        var ids = candidate.PassengerContracts.FirstOrDefault(c => c.ContractId == obligation.ContractId)?.PassengerIds ?? [];
        string? station = ids.IsEmpty ? null : ConfirmedDockStation();
        var failure = new TransportFailureData(obligation.ContractId, TransportFailureReasons.Cancelled, now, penalty, paid, 0,
            penalty - paid, TransportFailureReasons.PostingIdFor(obligation.ContractId), ids,
            station is null ? null : now, station);
        candidate = candidate with
        {
            StationJobs = jobs with
            {
                Obligations = jobs.Obligations.SetItem(jobs.Obligations.IndexOf(obligation),
                    obligation with { Status = StationJobStatuses.Cancelled, ClosedAtGameTimeMs = now })
            },
            TransportFailures = candidate.TransportFailures.Add(failure)
        };
        return failure;
    }

    /// <summary>World half of an applied cancellation: money, ledger posting and (when docked) disembarkation.</summary>
    private void ApplyTransportCancellation(TransportFailureData failure)
    {
        PlayerCredits = checked(PlayerCredits - failure.PenaltyPaid);
        if (failure.DisembarkedAtGameTimeMs is not null) RemovePassengersAboard(failure.PassengerIds);
        RecordVoyageAmount(failure.PostingId, VoyageAmountKind.PassengerPenalty, failure.PenaltyCredits);
    }

    private void RemovePassengersAboard(ImmutableArray<string> ids)
    {
        int index = _objects.FindIndex(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (index < 0 || ids.IsDefaultOrEmpty || _objects[index].Passengers.IsDefault) return;
        var leaving = ids.ToHashSet(StringComparer.Ordinal);
        _objects[index] = _objects[index] with { Passengers = _objects[index].Passengers.RemoveAll(p => leaving.Contains(p.PassengerId)) };
    }

    /// <summary>
    /// Idempotent calendar step after delivery: close active transport obligations of a destroyed carrier as Lost,
    /// expire those past their deadline, and let pending passengers leave at a confirmed docking. A closed obligation
    /// is never processed again, so repeated snapshots, saves and loads never charge twice.
    /// </summary>
    private void ResolveTransportFailures(long gameTimeMs)
    {
        if (!StationJobsEnabled || _demoCampaign.StationJobs is not { } jobs) return;
        int shipIndex = _objects.FindIndex(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (shipIndex < 0) return;
        var ship = _objects[shipIndex];
        var obligations = jobs.Obligations;
        var failures = _demoCampaign.TransportFailures;
        long credits = PlayerCredits;
        var postings = new List<(string PostingId, long Credits)>();
        bool changed = false;

        foreach (var obligation in jobs.Obligations.Where(o => o.Status == StationJobStatuses.Active && IsTransportJob(o.Type))
                     .OrderBy(o => o.DeadlineGameTimeMs).ThenBy(o => o.AcceptedAtGameTimeMs).ThenBy(o => o.ContractId, StringComparer.Ordinal))
        {
            string reason;
            long failedAt, penalty;
            if (ship.IsDestroyed)
            {
                reason = TransportFailureReasons.CarrierLost;
                failedAt = Math.Max(gameTimeMs, obligation.AcceptedAtGameTimeMs);
                penalty = 0;
            }
            else
            {
                if (gameTimeMs <= obligation.DeadlineGameTimeMs) continue;
                // A-01: docked at the destination by the deadline and waiting for the docking to be confirmed wins.
                if (ship.IsDocked && ship.DockedStationObjectId == obligation.DestinationStationId &&
                    DockArrivalTime(obligation.DestinationStationId, gameTimeMs) <= obligation.DeadlineGameTimeMs)
                    continue;
                reason = TransportFailureReasons.Expired;
                failedAt = checked(obligation.DeadlineGameTimeMs + 1);
                penalty = TransportPenalty(obligation.Payout, ExpiryPenaltyPermille);
            }
            long paid = Math.Min(Math.Max(0, credits), penalty);
            credits -= paid;
            var ids = _demoCampaign.PassengerContracts.FirstOrDefault(c => c.ContractId == obligation.ContractId)?.PassengerIds ?? [];
            var failure = new TransportFailureData(obligation.ContractId, reason, failedAt, penalty, paid, 0, penalty - paid,
                TransportFailureReasons.PostingIdFor(obligation.ContractId), ids);
            failures = failures.Add(failure);
            obligations = obligations.SetItem(obligations.IndexOf(obligation),
                obligation with { Status = StationJobStatuses.Failed, ClosedAtGameTimeMs = failedAt });
            if (penalty > 0) postings.Add((failure.PostingId, penalty));
            changed = true;
        }

        var leaving = new List<string>();
        if (!ship.IsDestroyed && ConfirmedDockStation() is { } station)
            for (int i = 0; i < failures.Length; i++)
            {
                var failure = failures[i];
                if (!failure.DisembarkPending) continue;
                long left = Math.Max(failure.FailedAtGameTimeMs, DockArrivalTime(station, gameTimeMs));
                failures = failures.SetItem(i, failure with { DisembarkedAtGameTimeMs = left, DisembarkStationId = station });
                leaving.AddRange(failure.PassengerIds);
                changed = true;
            }
        if (!changed) return;

        var candidate = _demoCampaign with
        {
            StationJobs = jobs with { Obligations = obligations },
            TransportFailures = failures,
            Revision = checked(_demoCampaign.Revision + 1),
            CommitSequence = checked(_demoCampaign.CommitSequence + 1)
        };
        DemoCampaignData.Validate(candidate, Math.Max(gameTimeMs, candidate.LastTickGameTimeMs));
        ValidateDemoCampaignSections(candidate, Math.Max(gameTimeMs, candidate.LastTickGameTimeMs));
        // Commit: campaign state, money, people and the voyage penalty postings together.
        _demoCampaign = candidate;
        PlayerCredits = credits;
        RemovePassengersAboard(leaving.ToImmutableArray());
        foreach (var (postingId, amount) in postings)
            RecordVoyageAmount(postingId, VoyageAmountKind.PassengerPenalty, amount);
    }

    /// <summary>
    /// Repay contract debt (oldest failure first) from contract payouts just credited; returns the updated failures and
    /// the amount collected, never more than <paramref name="available"/>.
    /// </summary>
    private static ImmutableArray<TransportFailureData> RepayTransportDebt(ImmutableArray<TransportFailureData> failures,
        long available, out long collected)
    {
        collected = 0;
        if (available <= 0) return failures;
        foreach (var failure in failures.Where(f => f.DebtCredits > 0)
                     .OrderBy(f => f.FailedAtGameTimeMs).ThenBy(f => f.ContractId, StringComparer.Ordinal).ToArray())
        {
            long amount = Math.Min(failure.DebtCredits, available - collected);
            if (amount <= 0) break;
            collected += amount;
            failures = failures.Replace(failure, failure with
            {
                RepaidCredits = checked(failure.RepaidCredits + amount),
                DebtCredits = failure.DebtCredits - amount
            });
        }
        return failures;
    }

    private TransportFailuresSnapshot? BuildTransportFailuresProjection()
    {
        if (!StationJobsEnabled) return null;
        long now = StationJobsNow;
        var jobs = _demoCampaign.StationJobs ?? EmptyStationJobs;
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        string? blocker = CombatOrLootOpen ? DemoCampaignReasons.BattleActive
            : ship is null ? DemoCampaignReasons.Unavailable
            : ship.IsDestroyed ? DemoCampaignReasons.CarrierDestroyed
            : null;
        var failures = _demoCampaign.TransportFailures
            .OrderBy(f => f.FailedAtGameTimeMs).ThenBy(f => f.ContractId, StringComparer.Ordinal)
            .Select(f => new TransportFailureSnapshot(f.ContractId,
                jobs.Obligations.FirstOrDefault(o => o.ContractId == f.ContractId)?.Type ?? "", f.Reason, f.FailedAtGameTimeMs,
                f.PenaltyCredits, f.PenaltyPaid, f.DebtCredits, f.DisembarkPending, f.PostingId, f.PassengerIds,
                f.RepaidCredits, f.DisembarkedAtGameTimeMs, f.DisembarkStationId))
            .ToImmutableArray();
        var terms = jobs.Obligations.Where(o => o.Status == StationJobStatuses.Active && IsTransportJob(o.Type))
            .OrderBy(o => o.AcceptedAtGameTimeMs).ThenBy(o => o.ContractId, StringComparer.Ordinal)
            .Select(o => new TransportCancellationTerms(o.ContractId, TransportPenalty(o.Payout, CancelPenaltyPermille),
                TransportPenalty(o.Payout, ExpiryPenaltyPermille), o.DeadlineGameTimeMs, blocker))
            .ToImmutableArray();
        return new TransportFailuresSnapshot(failures, terms, _demoCampaign.TransportFailures.Sum(f => f.DebtCredits), now);
    }

    /// <summary>
    /// Load cross-check against the candidate world, before the running session is replaced: passengers waiting to
    /// disembark are aboard the player carrier, passengers who left are aboard nowhere.
    /// </summary>
    private static void ValidateTransportFailuresForLoad(DemoCampaignData data, GameStateData state)
    {
        if (data.TransportFailures.IsDefaultOrEmpty) return;
        var player = state.SpaceObjects.FirstOrDefault(o => string.Equals(o.ObjectId, state.PlayerShipObjectId, StringComparison.OrdinalIgnoreCase));
        var playerAboard = (player?.Passengers ?? []).Select(p => p.PassengerId).ToHashSet(StringComparer.Ordinal);
        var anywhere = state.SpaceObjects.SelectMany(o => o.Passengers ?? []).Select(p => p.PassengerId).ToHashSet(StringComparer.Ordinal);
        foreach (var failure in data.TransportFailures)
        {
            if (failure.DisembarkPending && !failure.PassengerIds.All(playerAboard.Contains))
                throw new ScenarioException("Invalid demo campaign transport failures: waiting passengers are not aboard.");
            if (failure.DisembarkedAtGameTimeMs is not null && failure.PassengerIds.Any(anywhere.Contains))
                throw new ScenarioException("Invalid demo campaign transport failures: disembarked passengers are still aboard.");
        }
    }
}
