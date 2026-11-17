using System.Collections.Immutable;
using System.Text.Json.Serialization;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>Campaign metadata only. Resources remain in the existing authoritative world.</summary>
public sealed record DemoCampaignData(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion = 1,
    [property: JsonPropertyName("revision")] long Revision = 0,
    [property: JsonPropertyName("commitSequence")] long CommitSequence = 0,
    [property: JsonPropertyName("lastTickGameTimeMs")] long LastTickGameTimeMs = 0,
    [property: JsonPropertyName("company")] DemoCompanySnapshot? Company = null,
    [property: JsonPropertyName("transport")] DemoTransportSnapshot? Transport = null,
    [property: JsonPropertyName("encounters")] DemoEncountersSnapshot? Encounters = null,
    [property: JsonPropertyName("rewards")] DemoRewardsSnapshot? Rewards = null,
    [property: JsonPropertyName("progression")] DemoProgressionSnapshot? Progression = null,
    [property: JsonPropertyName("story")] DemoStorySnapshot? Story = null)
{
    [JsonPropertyName("catalogFingerprint")]
    public string? CatalogFingerprint { get; init; }

    [JsonPropertyName("pilotMemorial")]
    public ImmutableArray<PilotMemorialEntry> PilotMemorial { get; init; } = [];

    [JsonPropertyName("nextCrewSequence")]
    public long NextCrewSequence { get; init; } = 1;

    public long NextDroneSequence { get; init; } = 1;
    public ImmutableDictionary<string, int> ServiceOfferUsage { get; init; } = ImmutableDictionary<string, int>.Empty;
    public ImmutableHashSet<string> HiredOfferIds { get; init; } = ImmutableHashSet<string>.Empty;
    public ImmutableArray<DemoCrewEmploymentData> Employment { get; init; } = [];
    public ImmutableDictionary<string, string> CommittedCommandHashes { get; init; } = ImmutableDictionary<string, string>.Empty;

    [JsonPropertyName("receipts")]
    public ImmutableArray<DemoCampaignReceiptData> Receipts { get; init; } = [];

    [JsonPropertyName("pirateCompositions")]
    public ImmutableArray<PirateCompositionData> PirateCompositions { get; init; } = [];

    [JsonPropertyName("pirateWorld")]
    public PirateWorldData? PirateWorld { get; init; }

    public ImmutableArray<SensorContactReport> SensorReports { get; init; } = [];
    public SensorContactReportBatch? PendingSensorReport { get; init; }

    /// <summary>EP-0034 US-0006: published offers and the obligations journal; null until the first publication.</summary>
    [JsonPropertyName("stationJobs")]
    public StationJobsStateData? StationJobs { get; init; }

    /// <summary>EP-0034 US-0002: boarded passengers, payout posting and delivery receipt per passenger obligation.</summary>
    [JsonPropertyName("passengerContracts")]
    public ImmutableArray<PassengerContractData> PassengerContracts { get; init; } = [];

    /// <summary>EP-0034 US-0003: terminal failures (cancelled, expired, lost) with penalty, debt and disembarkation.</summary>
    [JsonPropertyName("transportFailures")]
    public ImmutableArray<TransportFailureData> TransportFailures { get; init; } = [];

    /// <summary>EP-0034 US-0004: sealed cargo per cargo obligation — payout posting, delivery and return receipts.</summary>
    [JsonPropertyName("cargoContracts")]
    public ImmutableArray<CargoContractData> CargoContracts { get; init; } = [];

    internal static DemoCampaignData Validate(DemoCampaignData? data, long gameTimeMs)
    {
        data ??= new(LastTickGameTimeMs: gameTimeMs);
        if (data.SensorReports.IsDefault || data.SensorReports.Length > 4 || data.SensorReports.Any(r => r is null ||
            r.ReportId != "report:" + r.GroupId || string.IsNullOrWhiteSpace(r.GroupId) || r.ObservedMotionTimeMs < 0 ||
            !double.IsFinite(r.BearingDegrees) || r.BearingDegrees < 0 || r.BearingDegrees >= 360 ||
            !double.IsFinite(r.RangeWorldUnits) || r.RangeWorldUnits < 0 || r.ApproximateDroneCount < 0 ||
            r.EtaRealSecondsAt1X is { } eta && (!double.IsFinite(eta) || eta < 0) ||
            (r.EtaRealSecondsAt1X is null ? r.EtaUnavailableReason != "no_intercept" : r.EtaUnavailableReason is not null) ||
            r.SignatureTypeIds.IsDefaultOrEmpty || r.SignatureTypeIds.Length > 2 || r.SignatureTypeIds.Any(s => s is not ("lair" or "tancred"))) ||
            data.SensorReports.Select(r => r.GroupId).Distinct(StringComparer.Ordinal).Count() != data.SensorReports.Length)
            throw new ScenarioException("Invalid frozen sensor reports.");
        if (data.PendingSensorReport is { } batch && (string.IsNullOrWhiteSpace(batch.BatchId) || batch.Reports.IsDefaultOrEmpty ||
            batch.Reports.Any(r => r is null) ||
            batch.Reports.Select(r => r.ReportId).Distinct(StringComparer.Ordinal).Count() != batch.Reports.Length ||
            batch.Reports.Any(r => r is null || !data.SensorReports.Any(saved => System.Text.Json.JsonSerializer.Serialize(saved) == System.Text.Json.JsonSerializer.Serialize(r)))))
            throw new ScenarioException("Invalid pending sensor report batch.");
        if (data.PirateCompositions.IsDefault || data.PirateCompositions.Any(c => c is null ||
            string.IsNullOrWhiteSpace(c.GroupId) || string.IsNullOrWhiteSpace(c.CarrierId) || c.GenerationVersion != 1 ||
            c.SignatureTypeId is not (PirateSignatureTypes.Lair or PirateSignatureTypes.Tancred) || c.Drones.IsDefault ||
            c.Drones.Length != (c.SignatureTypeId == PirateSignatureTypes.Lair ? 6 : 10) ||
            c.Drones.Any(d => d is null || string.IsNullOrWhiteSpace(d.DroneId) ||
                d.LayoutId is not ("light.sword.shield" or "light.spear" or "heavy.sword.shield" or "heavy.spear.shield"))) ||
            data.PirateCompositions.Select(c => c.CarrierId).Distinct(StringComparer.Ordinal).Count() != data.PirateCompositions.Length ||
            data.PirateCompositions.SelectMany(c => c.Drones).Select(d => d.DroneId).Distinct(StringComparer.Ordinal).Count() != data.PirateCompositions.Sum(c => c.Drones.Length))
            throw new ScenarioException("Invalid saved pirate composition.");
        if (data.CommittedCommandHashes is null || data.CommittedCommandHashes.Any(p => string.IsNullOrWhiteSpace(p.Key) ||
            p.Value is not { Length: 64 } || !p.Value.All(Uri.IsHexDigit)))
            throw new ScenarioException("Invalid committed campaign identities.");
        if (data.Company?.Upkeep is { } upkeep && (upkeep.Accrued < 0 || upkeep.Paid < 0 || upkeep.Paid > upkeep.Accrued ||
            upkeep.Outstanding != upkeep.Accrued - upkeep.Paid || upkeep.LastProcessedDay < 0 ||
            upkeep.LastProcessedDay > gameTimeMs / 86400000 || upkeep.PeopleFed < 0 || upkeep.FoodRemaining < 0 || upkeep.MissingRations < 0))
            throw new ScenarioException("Invalid crew upkeep ledger.");
        if (data.Company?.StationRepair is { } repair)
        {
            if (repair.Postings.IsDefault || repair.Postings.Any(p => p is null || string.IsNullOrWhiteSpace(p.Id) ||
                string.IsNullOrWhiteSpace(p.TargetId) || p.Kind is not ("repair" or "decoy" or "drone" or "equipment" or "hire") || p.Amount < 0 || p.GameTimeMs < 0 || p.GameTimeMs > gameTimeMs) ||
                repair.Postings.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() != repair.Postings.Length)
                throw new ScenarioException("Invalid company repair postings.");
            if (repair.LastResult is { } result && (result.Paid < 0 ||
                !repair.Postings.Any(p => p.Id == result.PostingId && p.Amount == result.Paid && p.TargetId == result.TargetId)))
                throw new ScenarioException("Invalid company repair result.");
            if (repair.Quote is { } quote && (string.IsNullOrWhiteSpace(quote.Id) || quote.Total < 0 ||
                quote.Items.IsDefault || quote.Items.Any(i => i is null || i.Quantity < 0 || i.UnitPrice < 0 ||
                    (decimal)i.Quantity * i.UnitPrice != i.Total) || quote.Items.Sum(i => (decimal)i.Total) != quote.Total))
                throw new ScenarioException("Invalid company repair quote.");
        }
        if (data.SchemaVersion != 1 || data.Revision < 0 || data.CommitSequence < data.Revision ||
            data.LastTickGameTimeMs < 0 || data.LastTickGameTimeMs > gameTimeMs || data.Receipts.IsDefault ||
            data.NextCrewSequence < 1 || data.NextDroneSequence < 1 || data.ServiceOfferUsage is null ||
            data.ServiceOfferUsage.Any(p => string.IsNullOrWhiteSpace(p.Key) || p.Value < 0) || data.PilotMemorial.IsDefault)
            throw new ScenarioException("Invalid demo campaign version or counters.");
        if (data.HiredOfferIds is null || data.Employment.IsDefault || data.Employment.Any(e => e is null ||
            string.IsNullOrWhiteSpace(e.CrewId) || e.DailyWage < 0 || e.HiredAt < 0 || e.HiredAt > gameTimeMs) ||
            data.Employment.Select(e => e.CrewId).Distinct().Count() != data.Employment.Length)
            throw new ScenarioException("Invalid crew employment terms.");
        // EP-0034 US-0001: passenger capacity is derived from the world on projection, never trusted from a file.
        if (data.Transport?.PassengerCapacity is not null)
            throw new ScenarioException("Invalid demo campaign transport: passenger capacity is derived, not persisted.");
        ValidateStationJobs(data, gameTimeMs);
        ValidatePassengerContracts(data, gameTimeMs);
        ValidateTransportFailures(data, gameTimeMs);
        ValidateCargoContracts(data, gameTimeMs);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        long previousSequence = 0;
        long previousRevision = 0;
        foreach (var receipt in data.Receipts)
        {
            if (receipt is null || receipt.Response is null || string.IsNullOrWhiteSpace(receipt.Response.CommandId) ||
                !ids.Add(receipt.Response.CommandId) || receipt.PayloadHash is not { Length: 64 } ||
                !receipt.PayloadHash.All(Uri.IsHexDigit) || receipt.CommitSequence <= previousSequence ||
                receipt.CommitSequence > data.CommitSequence || receipt.Response.Revision < 0 ||
                receipt.Response.Revision > data.Revision || receipt.Response.Revision < previousRevision ||
                (receipt.Response.Applied ? receipt.Response.Reason is not null : string.IsNullOrWhiteSpace(receipt.Response.Reason)))
                throw new ScenarioException("Invalid demo campaign receipt.");
            previousSequence = receipt.CommitSequence;
            previousRevision = receipt.Response.Revision;
        }
        return data;
    }

    /// <summary>Atomic structural check of the board/journal; a corrupted section rejects the whole Load.</summary>
    private static void ValidateStationJobs(DemoCampaignData data, long gameTimeMs)
    {
        if (data.Transport?.StationJobs is not null)
            throw new ScenarioException("Invalid demo campaign transport: the job board projection is not persisted.");
        if (data.StationJobs is not { } jobs) return;
        static bool Text(string? value) => !string.IsNullOrWhiteSpace(value);
        bool valid = data.CatalogFingerprint is not null && jobs.PublishedBucket >= 0 &&
            !jobs.Offers.IsDefault && !jobs.Obligations.IsDefault &&
            jobs.Offers.All(o => o is not null && Text(o.Id) && StationJobTypes.IsKnown(o.Type) && Text(o.OriginStationId) &&
                Text(o.DestinationStationId) && o.OriginStationId != o.DestinationStationId && o.Count > 0 && o.MassKg >= 0 &&
                StationJobRiskBands.IsKnown(o.RiskBand) && o.RouteDistanceKm > 0 && o.DeadlineDurationMs > 0 &&
                o.PublishedAtGameTimeMs >= 0 && o.PublishedAtGameTimeMs <= gameTimeMs && o.ExpiresAtGameTimeMs > o.PublishedAtGameTimeMs &&
                o.Terms is { BaseTokens: >= 0, LoadTokens: >= 0, DistanceTokens: >= 0, UrgencyPermille: >= 1000, RiskPermille: >= 1000 } &&
                o.Payout == SimulationEngine.StationJobPayout(o.Terms) &&
                (o.Type == StationJobTypes.Cargo) == (o.ItemTypeId is not null && o.MassKg > 0)) &&
            jobs.Obligations.All(o => o is not null && Text(o.ContractId) && Text(o.OfferId) && StationJobTypes.IsKnown(o.Type) &&
                Text(o.OriginStationId) && Text(o.DestinationStationId) && o.Count > 0 && o.MassKg >= 0 && o.Payout >= 0 &&
                StationJobStatuses.IsKnown(o.Status) && StationJobRiskBands.IsKnown(o.RiskBand) &&
                o.AcceptedAtGameTimeMs >= 0 && o.AcceptedAtGameTimeMs <= gameTimeMs && o.DeadlineGameTimeMs > o.AcceptedAtGameTimeMs &&
                (o.Status == StationJobStatuses.Active) == (o.ClosedAtGameTimeMs is null) &&
                (o.ClosedAtGameTimeMs is not { } closed || closed >= o.AcceptedAtGameTimeMs && closed <= gameTimeMs)) &&
            jobs.Offers.Select(o => o.Id).Distinct(StringComparer.Ordinal).Count() == jobs.Offers.Length &&
            jobs.Obligations.Select(o => o.ContractId).Distinct(StringComparer.Ordinal).Count() == jobs.Obligations.Length &&
            jobs.Obligations.Select(o => o.OfferId).Distinct(StringComparer.Ordinal).Count() == jobs.Obligations.Length &&
            !jobs.Offers.Any(o => jobs.Obligations.Any(b => b.OfferId == o.Id));
        if (!valid) throw new ScenarioException("Invalid demo campaign station jobs.");
    }

    /// <summary>
    /// Atomic structural check of the passenger contracts (EP-0034 US-0002): every record belongs to one passenger
    /// obligation, owns unique passenger ids and the canonical posting id, and a delivery receipt exists exactly
    /// for Completed obligations with the obligation's payout. World membership is checked by the engine on Load.
    /// </summary>
    private static void ValidatePassengerContracts(DemoCampaignData data, long gameTimeMs)
    {
        if (data.Transport?.PassengerContracts is not null)
            throw new ScenarioException("Invalid demo campaign transport: the passenger contracts projection is not persisted.");
        if (data.PassengerContracts.IsDefault)
            throw new ScenarioException("Invalid demo campaign passenger contracts.");
        var obligations = data.StationJobs?.Obligations ?? [];
        var contracts = new HashSet<string>(StringComparer.Ordinal);
        var passengers = new HashSet<string>(StringComparer.Ordinal);
        bool valid = data.PassengerContracts.IsEmpty || data.StationJobs is not null;
        foreach (var record in data.PassengerContracts)
        {
            if (!valid) break;
            var obligation = record is null ? null : obligations.FirstOrDefault(o => o.ContractId == record.ContractId);
            valid = record is not null && obligation is not null && contracts.Add(record.ContractId) &&
                StationJobTypes.CarriesPassengers(obligation.Type) &&
                record.PostingId == PassengerContractReasons.PostingIdFor(record.ContractId) &&
                !record.PassengerIds.IsDefault && record.PassengerIds.All(id => !string.IsNullOrWhiteSpace(id) && passengers.Add(id)) &&
                // Empty ids only for a delivered legacy acceptance that never boarded anyone.
                (record.PassengerIds.Length == obligation.Count || record.PassengerIds.IsEmpty && record.DeliveredAtGameTimeMs is not null) &&
                (record.DeliveredAtGameTimeMs is null) == (record.PaidCredits is null) &&
                (record.DeliveredAtGameTimeMs is not { } delivered
                    ? obligation.Status != StationJobStatuses.Completed
                    : obligation.Status == StationJobStatuses.Completed && record.PaidCredits == obligation.Payout &&
                      delivered == obligation.ClosedAtGameTimeMs && delivered >= obligation.AcceptedAtGameTimeMs &&
                      delivered <= obligation.DeadlineGameTimeMs && delivered <= gameTimeMs);
        }
        // A completed passenger obligation always carries its delivery receipt, so a payout cannot be replayed.
        valid = valid && obligations.All(o => o is null || !StationJobTypes.CarriesPassengers(o.Type) ||
            o.Status != StationJobStatuses.Completed || contracts.Contains(o.ContractId));
        if (!valid) throw new ScenarioException("Invalid demo campaign passenger contracts.");
    }

    /// <summary>
    /// Atomic structural check of the transport failures (EP-0034 US-0003): exactly one record per failed or cancelled
    /// transport obligation, a reason matching the journal status, the penalty split paid + repaid + debt, the boarded
    /// passengers of the contract and a consistent disembarkation. World membership is checked by the engine on Load.
    /// </summary>
    private static void ValidateTransportFailures(DemoCampaignData data, long gameTimeMs)
    {
        if (data.Transport?.TransportFailures is not null)
            throw new ScenarioException("Invalid demo campaign transport: the transport failures projection is not persisted.");
        if (data.TransportFailures.IsDefault)
            throw new ScenarioException("Invalid demo campaign transport failures.");
        var obligations = data.StationJobs?.Obligations ?? [];
        var contracts = new HashSet<string>(StringComparer.Ordinal);
        bool valid = data.TransportFailures.IsEmpty || data.StationJobs is not null;
        foreach (var f in data.TransportFailures)
        {
            if (!valid) break;
            var o = f is null ? null : obligations.FirstOrDefault(x => x.ContractId == f.ContractId);
            var boarded = f is null ? null : data.PassengerContracts.FirstOrDefault(c => c.ContractId == f.ContractId);
            valid = f is not null && o is not null && contracts.Add(f.ContractId) && SimulationEngine.IsTransportJob(o.Type) &&
                (f.Reason == TransportFailureReasons.Cancelled ? o.Status == StationJobStatuses.Cancelled
                    : TransportFailureReasons.IsKnownOutcome(f.Reason) && o.Status == StationJobStatuses.Failed) &&
                o.ClosedAtGameTimeMs == f.FailedAtGameTimeMs &&
                (f.Reason != TransportFailureReasons.Expired || f.FailedAtGameTimeMs == o.DeadlineGameTimeMs + 1) &&
                f.PostingId == TransportFailureReasons.PostingIdFor(f.ContractId) &&
                f.PenaltyCredits >= 0 && f.PenaltyPaid >= 0 && f.RepaidCredits >= 0 && f.DebtCredits >= 0 &&
                f.PenaltyPaid + f.RepaidCredits + f.DebtCredits == f.PenaltyCredits &&
                (f.Reason != TransportFailureReasons.CarrierLost || f.PenaltyCredits == 0) &&
                !f.PassengerIds.IsDefault && f.PassengerIds.SequenceEqual(boarded?.PassengerIds ?? [], StringComparer.Ordinal) &&
                boarded?.DeliveredAtGameTimeMs is null &&
                (f.DisembarkedAtGameTimeMs is null) == (f.DisembarkStationId is null) &&
                (f.DisembarkedAtGameTimeMs is not { } left ||
                    !f.PassengerIds.IsEmpty && f.Reason != TransportFailureReasons.CarrierLost &&
                    !string.IsNullOrWhiteSpace(f.DisembarkStationId) && left >= f.FailedAtGameTimeMs && left <= gameTimeMs);
        }
        // A closed transport obligation always carries its failure record, so a penalty cannot be replayed.
        valid = valid && obligations.All(o => o is null || !SimulationEngine.IsTransportJob(o.Type) ||
            o.Status is not (StationJobStatuses.Failed or StationJobStatuses.Cancelled) || contracts.Contains(o.ContractId));
        if (!valid) throw new ScenarioException("Invalid demo campaign transport failures.");
    }

    /// <summary>
    /// Atomic structural check of the cargo contracts (EP-0034 US-0004): every cargo obligation names a sealed item and
    /// its mass; every record belongs to one cargo obligation and owns the canonical posting id; a delivery receipt
    /// exists exactly for Completed obligations with the obligation's payout; a return receipt exists only for a
    /// cancelled or expired (not lost) obligation, after its failure. Item ids are checked by the engine on Load.
    /// </summary>
    private static void ValidateCargoContracts(DemoCampaignData data, long gameTimeMs)
    {
        if (data.Transport?.CargoContracts is not null)
            throw new ScenarioException("Invalid demo campaign transport: the cargo contracts projection is not persisted.");
        if (data.CargoContracts.IsDefault)
            throw new ScenarioException("Invalid demo campaign cargo contracts.");
        var obligations = data.StationJobs?.Obligations ?? [];
        var contracts = new HashSet<string>(StringComparer.Ordinal);
        bool valid = (data.CargoContracts.IsEmpty || data.StationJobs is not null) &&
            obligations.All(o => o is null || o.Type != StationJobTypes.Cargo ||
                !string.IsNullOrWhiteSpace(o.ItemTypeId) && o.MassKg > 0 && o.Count > 0);
        foreach (var record in data.CargoContracts)
        {
            if (!valid) break;
            var o = record is null ? null : obligations.FirstOrDefault(x => x.ContractId == record.ContractId);
            var failure = record is null ? null : data.TransportFailures.FirstOrDefault(f => f.ContractId == record.ContractId);
            valid = record is not null && o is not null && contracts.Add(record.ContractId) && o.Type == StationJobTypes.Cargo &&
                record.PostingId == CargoContractReasons.PostingIdFor(record.ContractId) &&
                (record.DeliveredAtGameTimeMs is null) == (record.PaidCredits is null) &&
                (record.DeliveredAtGameTimeMs is not { } delivered
                    ? o.Status != StationJobStatuses.Completed
                    : o.Status == StationJobStatuses.Completed && record.PaidCredits == o.Payout &&
                      delivered == o.ClosedAtGameTimeMs && delivered >= o.AcceptedAtGameTimeMs &&
                      delivered <= o.DeadlineGameTimeMs && delivered <= gameTimeMs) &&
                (record.ReturnedAtGameTimeMs is null) == (record.ReturnStationId is null) &&
                (record.ReturnedAtGameTimeMs is not { } returned ||
                    o.Status is StationJobStatuses.Failed or StationJobStatuses.Cancelled && failure is not null &&
                    failure.Reason != TransportFailureReasons.CarrierLost && !string.IsNullOrWhiteSpace(record.ReturnStationId) &&
                    returned >= failure.FailedAtGameTimeMs && returned <= gameTimeMs);
        }
        // A completed cargo obligation always carries its delivery receipt, so a payout cannot be replayed.
        valid = valid && obligations.All(o => o is null || o.Type != StationJobTypes.Cargo ||
            o.Status != StationJobStatuses.Completed || contracts.Contains(o.ContractId));
        if (!valid) throw new ScenarioException("Invalid demo campaign cargo contracts.");
    }
}

public sealed record DemoCrewEmploymentData(string CrewId, long DailyWage, long HiredAt);
/// <summary>
/// Persisted cargo contract part (EP-0034 US-0004): the durable voyage posting and, once settled, either the delivery
/// (arrival time and credited payout) or the return of failed cargo (time and station). Item, quantity, mass, route,
/// deadline and payout stay in the journal obligation; the sealed cargo is never a module cargo stack.
/// </summary>
public sealed record CargoContractData(
    [property: JsonPropertyName("contractId")] string ContractId,
    [property: JsonPropertyName("postingId")] string PostingId,
    [property: JsonPropertyName("deliveredAtGameTimeMs")] long? DeliveredAtGameTimeMs = null,
    [property: JsonPropertyName("paidCredits")] long? PaidCredits = null,
    [property: JsonPropertyName("returnedAtGameTimeMs")] long? ReturnedAtGameTimeMs = null,
    [property: JsonPropertyName("returnStationId")] string? ReturnStationId = null);

/// <summary>
/// Persisted transport failure (EP-0034 US-0003). The penalty is split into what was paid at the failure, what later
/// contract payouts repaid and what is still owed. Passengers of a failed contract stay aboard until a confirmed
/// docking; then the time and station of their disembarkation are recorded once.
/// </summary>
public sealed record TransportFailureData(
    [property: JsonPropertyName("contractId")] string ContractId,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("failedAtGameTimeMs")] long FailedAtGameTimeMs,
    [property: JsonPropertyName("penaltyCredits")] long PenaltyCredits,
    [property: JsonPropertyName("penaltyPaid")] long PenaltyPaid,
    [property: JsonPropertyName("repaidCredits")] long RepaidCredits,
    [property: JsonPropertyName("debtCredits")] long DebtCredits,
    [property: JsonPropertyName("postingId")] string PostingId,
    [property: JsonPropertyName("passengerIds")] ImmutableArray<string> PassengerIds,
    [property: JsonPropertyName("disembarkedAtGameTimeMs")] long? DisembarkedAtGameTimeMs = null,
    [property: JsonPropertyName("disembarkStationId")] string? DisembarkStationId = null)
{
    /// <summary>Passengers still aboard waiting for a confirmed docking (never for a lost carrier).</summary>
    [JsonIgnore]
    public bool DisembarkPending => !PassengerIds.IsDefaultOrEmpty && DisembarkedAtGameTimeMs is null && Reason != TransportFailureReasons.CarrierLost;
}

/// <summary>
/// Persisted passenger contract part (EP-0034 US-0002): who boarded, the durable voyage posting and, once
/// delivered, the arrival time and the credited payout. Route, deadline and payout stay in the journal obligation.
/// </summary>
public sealed record PassengerContractData(
    [property: JsonPropertyName("contractId")] string ContractId,
    [property: JsonPropertyName("passengerIds")] ImmutableArray<string> PassengerIds,
    [property: JsonPropertyName("postingId")] string PostingId,
    [property: JsonPropertyName("deliveredAtGameTimeMs")] long? DeliveredAtGameTimeMs = null,
    [property: JsonPropertyName("paidCredits")] long? PaidCredits = null);

/// <summary>Persisted job board: last published calendar bucket, live offers with frozen terms and the journal.</summary>
public sealed record StationJobsStateData(
    [property: JsonPropertyName("publishedBucket")] long PublishedBucket,
    [property: JsonPropertyName("offers")] ImmutableArray<JobOffer> Offers,
    [property: JsonPropertyName("obligations")] ImmutableArray<JobObligation> Obligations);

public sealed record DemoCampaignReceiptData(
    [property: JsonPropertyName("payloadHash")] string PayloadHash,
    [property: JsonPropertyName("commitSequence")] long CommitSequence,
    [property: JsonPropertyName("response")] DemoCampaignResponse Response);
