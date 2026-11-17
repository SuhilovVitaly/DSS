using System.Collections.Immutable;
using System.Globalization;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Rng;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

/// <summary>
/// EP-0034 US-0006: station job board and the single obligations journal of the demo campaign.
/// Offers are published per calendar bucket from a named RNG stream keyed by station, bucket and slot, are saved
/// with frozen terms and are never re-rolled by refreshing a screen. Acceptance is an ordinary campaign command
/// (identity, revision, receipt) dispatched here explicitly, so the shared single-implementation partial hooks of
/// <c>SimulationEngine.DemoCampaign.cs</c> stay free for other epics.
/// </summary>
public sealed partial class SimulationEngine
{
    private const string StationJobsRngStream = "station-jobs/v1";
    private static readonly StationJobsStateData EmptyStationJobs = new(-1, [], []);

    /// <summary>Campaign calendar time: the clock, never earlier than the last observed campaign tick.</summary>
    private long StationJobsNow => Math.Max(_clock.GameTimeMs, _demoCampaign.LastTickGameTimeMs);

    private bool StationJobsEnabled =>
        _demoCampaign.CatalogFingerprint is not null && _demoDefinitions is { Cluster: not null, Jobs: not null };

    /// <summary>
    /// Idempotent calendar step: drop expired offers and publish the current bucket once. Runs before the
    /// campaign tick guard so a fresh (paused) New Game already shows its board; repeated calls change nothing.
    /// </summary>
    private void PublishStationJobs(long gameTimeMs)
    {
        if (!StationJobsEnabled) return;
        var jobs = _demoDefinitions.Jobs!;
        var state = _demoCampaign.StationJobs ?? EmptyStationJobs;
        long bucket = gameTimeMs / jobs.Limits.PublicationGameTimeMs;
        var live = state.Offers.RemoveAll(o => o.ExpiresAtGameTimeMs <= gameTimeMs);
        if (bucket <= state.PublishedBucket && live.Length == state.Offers.Length) return;
        if (bucket > state.PublishedBucket)
        {
            live = live.AddRange(GenerateStationJobOffers(bucket, live, firstPublication: _demoCampaign.StationJobs is null));
            state = state with { PublishedBucket = bucket };
        }
        _demoCampaign = _demoCampaign with { StationJobs = state with { Offers = live } };
    }

    private IEnumerable<JobOffer> GenerateStationJobOffers(long bucket, ImmutableArray<JobOffer> live, bool firstPublication)
    {
        var jobs = _demoDefinitions.Jobs!;
        var cluster = _demoDefinitions.Cluster!;
        var map = CurrentVoyageMap();
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (map is null || ship is null || ship.IsDestroyed) yield break;

        // Feasibility uses the carrier design (cabins for passengers, total hold), never a momentary free count:
        // the actual free space is checked again on acceptance.
        var capacity = BuildPassengerCapacityProjection();
        int passengerCabins = capacity is null ? 0 : Math.Max(0, capacity.TotalCabins - capacity.CrewCabins - capacity.ReservedCabins);
        int maxPassengers = Math.Min(jobs.Limits.PassengerMax, passengerCabins);
        int maxTonnes = (int)Math.Min(jobs.Limits.CargoTonnesMax, CarrierHoldCapacityKg(ship) / 1000);
        double speedKmS = GetMaxSpeedKmS(ship) ?? map.Rules.ReferenceSpeedMps / 1000;
        long publishedAt = checked(bucket * jobs.Limits.PublicationGameTimeMs);

        foreach (string stationId in cluster.StationIds.Order(StringComparer.Ordinal))
        {
            string role = cluster.Roles[cluster.StationIds.IndexOf(stationId)];
            var filter = jobs.RoleFilters.FirstOrDefault(f => f.Role == role);
            if (filter is null) continue;
            var routes = map.Edges
                .Where(e => e.FromStationObjectId == stationId || e.ToStationObjectId == stationId)
                .Select(e => (Destination: e.FromStationObjectId == stationId ? e.ToStationObjectId : e.FromStationObjectId, Edge: e))
                .Where(r => r.Destination != stationId && cluster.StationIds.Contains(r.Destination, StringComparer.Ordinal) &&
                    jobs.Risk.Any(k => k.RiskProfileId == r.Edge.RiskProfileId))
                .OrderBy(r => r.Destination, StringComparer.Ordinal).ToArray();
            if (routes.Length == 0) continue;

            int passengerWeight = maxPassengers > 0 ? filter.PassengerWeight : 0;
            int urgentWeight = maxPassengers > 0 ? filter.UrgentPassengerWeight : 0;
            int cargoWeight = maxTonnes >= jobs.Limits.CargoTonnesMin && jobs.CargoItemTypeIds.Length > 0 ? filter.CargoWeight : 0;
            int totalWeight = passengerWeight + urgentWeight + cargoWeight;
            if (totalWeight == 0) continue;

            int slots = jobs.Limits.Offers - live.Count(o => o.OriginStationId == stationId);
            for (int slot = 0; slot < slots; slot++)
            {
                var random = RngStreamNames.CreateDeterministicRandom(RngStreamSeedDerivation.DeriveStreamSeed(MasterSeed,
                    string.Create(CultureInfo.InvariantCulture, $"{StationJobsRngStream}:{stationId}:{bucket}:{slot}")));
                bool guaranteed = firstPublication && slot == 0 && stationId == cluster.StartStationId;
                string type;
                if (guaranteed) type = jobs.FirstOffer.Type;
                else
                {
                    int roll = random.Next(totalWeight);
                    type = roll < passengerWeight ? StationJobTypes.Passenger
                        : roll < passengerWeight + urgentWeight ? StationJobTypes.UrgentPassenger
                        : StationJobTypes.Cargo;
                }
                // The guaranteed first job is a gentle start: the shortest route from the start station.
                var route = guaranteed
                    ? routes.OrderBy(r => r.Edge.DistanceKm).ThenBy(r => r.Destination, StringComparer.Ordinal).First()
                    : routes[random.Next(routes.Length)];
                yield return BuildStationJobOffer(jobs, stationId, bucket, slot, type, route.Destination, route.Edge, random,
                    guaranteed ? Math.Min(jobs.FirstOffer.Count, maxPassengers) : 0, maxPassengers, maxTonnes, speedKmS, publishedAt);
            }
        }
    }

    private JobOffer BuildStationJobOffer(DemoJobsDefinition jobs, string stationId, long bucket, int slot, string type,
        string destination, TradingMapEdgeData edge, Random random, int fixedPassengers, int maxPassengers, int maxTonnes,
        double speedKmS, long publishedAt)
    {
        var risk = jobs.Risk.First(r => r.RiskProfileId == edge.RiskProfileId);
        // Distances inside a rigid cluster group are constant; whole kilometres freeze them for the offer's lifetime.
        long routeKm = Math.Max(1, (long)Math.Round(edge.DistanceKm, MidpointRounding.AwayFromZero));
        long etaMs = Math.Max(1, (long)Math.Ceiling(routeKm / speedKmS * 300 * 1000));

        int count;
        long massKg = 0, loadTokens;
        string? itemTypeId = null;
        int deadlinePermille;
        if (StationJobTypes.CarriesPassengers(type))
        {
            count = fixedPassengers > 0 ? fixedPassengers : random.Next(1, maxPassengers + 1);
            loadTokens = checked(jobs.Rates.PerPassenger * count);
            deadlinePermille = type == StationJobTypes.UrgentPassenger ? jobs.Deadlines.UrgentPermille : jobs.Deadlines.OrdinaryPermille;
        }
        else
        {
            itemTypeId = jobs.CargoItemTypeIds[random.Next(jobs.CargoItemTypeIds.Length)];
            long unitMassKg = _registry.ItemTypes.GetDefinition(_registry.ItemTypes.GetIndex(itemTypeId)).UnitMassKg;
            int tonnes = random.Next(jobs.Limits.CargoTonnesMin, maxTonnes + 1);
            long units = Math.Max(1, checked(tonnes * 1000L) / unitMassKg);
            massKg = checked(units * unitMassKg);
            count = (int)Math.Min(int.MaxValue, units);
            loadTokens = checked(jobs.Rates.PerCargoTonne * tonnes);
            deadlinePermille = jobs.Deadlines.CargoPermille;
        }

        int urgency = type == StationJobTypes.UrgentPassenger ? jobs.Rates.UrgencyPermille : 1000;
        var terms = new JobPayoutTerms(jobs.Rates.Base, loadTokens, checked(jobs.Rates.PerDistanceKm * routeKm), urgency, risk.Permille);
        long deadline = Math.Max(jobs.Deadlines.MinimumGameTimeMs, CeilDiv(checked(etaMs * deadlinePermille), 1000));
        return new JobOffer(
            string.Create(CultureInfo.InvariantCulture, $"jobs:{stationId}:{bucket}:{slot}"), type, stationId, destination,
            count, massKg, StationJobPayout(terms), deadline, publishedAt,
            checked(publishedAt + jobs.Limits.ExpiryGameTimeMs), risk.Band, routeKm, terms, itemTypeId);
    }

    /// <summary>Payout = ceil((base + load + distance) x urgency x risk / 1 000 000) whole Tokens.</summary>
    internal static long StationJobPayout(JobPayoutTerms terms) =>
        CeilDiv(checked((terms.BaseTokens + terms.LoadTokens + terms.DistanceTokens) * terms.UrgencyPermille * terms.RiskPermille), 1_000_000);

    private static long CeilDiv(long value, long divisor) => value <= 0 ? 0 : checked((value + divisor - 1) / divisor);

    private long CarrierHoldCapacityKg(SpaceObjectRuntime ship)
    {
        long total = 0;
        foreach (var module in ship.Modules)
        {
            var moduleType = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
            if (moduleType.CargoCapacityKg is > 0 && module.StructurePoints > 0 && !module.Destroyed)
                total = checked(total + moduleType.CargoCapacityKg.Value);
        }
        return total;
    }

    private long CarrierFreeHoldKg(SpaceObjectRuntime ship)
    {
        long free = 0;
        foreach (var module in ship.Modules)
        {
            var moduleType = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
            if (moduleType.CargoCapacityKg is > 0 && module.StructurePoints > 0 && !module.Destroyed)
                free = checked(free + Math.Max(0, moduleType.CargoCapacityKg.Value - ComputeCargoMassKg(module.Cargo)));
        }
        return free;
    }

    /// <summary>
    /// Single acceptance rule used by the command and by the projection, so the screen never guesses.
    /// Active obligations not yet carried out still hold their cabins and hold space.
    /// </summary>
    private string? StationJobBlocker(JobOffer offer, StationJobsStateData state, long gameTimeMs)
    {
        if (gameTimeMs >= offer.ExpiresAtGameTimeMs) return StationJobReasons.OfferExpired;
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (ship is null) return DemoCampaignReasons.Unavailable;
        if (ship.IsDestroyed) return DemoCampaignReasons.CarrierDestroyed;
        if (!ship.IsDocked || ship.DockedStationObjectId != offer.OriginStationId) return StationJobReasons.NotDockedAtOrigin;
        var active = state.Obligations.Where(o => o.Status == StationJobStatuses.Active).ToArray();
        if (StationJobTypes.CarriesPassengers(offer.Type))
        {
            var capacity = BuildPassengerCapacityProjection();
            string? blocker = PassengerCapacityReasons.BoardingBlocker(capacity);
            if (blocker is not null && blocker != PassengerCapacityReasons.NoFreeCabins) return blocker;
            long committed = PassengerSeatsAwaitingBoarding(active); // boarded passengers already occupy cabins (US-0002)
            return capacity!.FreeCabins - committed < offer.Count ? PassengerCapacityReasons.NoFreeCabins : null;
        }
        if (offer.Type == StationJobTypes.Cargo)
        {
            long committed = active.Where(o => o.Type == StationJobTypes.Cargo).Sum(o => o.MassKg);
            return CarrierFreeHoldKg(ship) - committed < offer.MassKg ? StationJobReasons.InsufficientCargoSpace : null;
        }
        return DemoCampaignReasons.Unsupported; // bounty acceptance is owned by EP-0036
    }

    /// <summary>Explicit dispatch for board commands; leaves every other command to the shared partial hook.</summary>
    private void PrepareStationJobsCommand(DemoCampaignCommand command, ref DemoCampaignData candidate,
        ref bool handled, ref string? reason)
    {
        if (handled || reason is not null || command is not AcceptJobOfferCommand accept) return;
        handled = true;
        if (!StationJobsEnabled) { reason = DemoCampaignReasons.Unavailable; return; }
        long now = StationJobsNow;
        var state = candidate.StationJobs ?? EmptyStationJobs;
        var offer = string.IsNullOrWhiteSpace(accept.OfferId) ? null : state.Offers.FirstOrDefault(o => o.Id == accept.OfferId);
        if (offer is null)
        {
            reason = state.Obligations.Any(o => o.OfferId == accept.OfferId) ? StationJobReasons.OfferAlreadyAccepted : StationJobReasons.OfferNotFound;
            return;
        }
        reason = StationJobBlocker(offer, state, now);
        if (reason is not null) return;
        var obligation = new JobObligation("contract:" + offer.Id, offer.Id, offer.Type, offer.OriginStationId,
            offer.DestinationStationId, offer.Count, offer.MassKg, offer.Payout, now,
            checked(now + offer.DeadlineDurationMs), StationJobStatuses.Active, offer.RiskBand, offer.ItemTypeId);
        candidate = candidate with
        {
            StationJobs = state with { Offers = state.Offers.Remove(offer), Obligations = state.Obligations.Add(obligation) }
        };
    }

    /// <summary>One projection for the board and the journal, used both docked and in flight.</summary>
    private StationJobsSnapshot? BuildStationJobsProjection()
    {
        if (!StationJobsEnabled) return null;
        long now = StationJobsNow;
        var state = _demoCampaign.StationJobs ?? EmptyStationJobs;
        var offers = state.Offers.Where(o => o.ExpiresAtGameTimeMs > now)
            .OrderBy(o => o.OriginStationId, StringComparer.Ordinal).ThenBy(o => o.PublishedAtGameTimeMs).ThenBy(o => o.Id, StringComparer.Ordinal)
            .ToImmutableArray();
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        return new StationJobsSnapshot(offers, state.Obligations, now,
            ship is { IsDocked: true } ? ship.DockedStationObjectId : null,
            offers.Select(o => new JobOfferAvailability(o.Id, StationJobBlocker(o, state, now))).ToImmutableArray());
    }
}
