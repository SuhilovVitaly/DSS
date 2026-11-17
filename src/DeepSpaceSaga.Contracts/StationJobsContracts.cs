using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

/// <summary>Stable order types of the demo station job board (EP-0034 US-0006). Bounty is published by EP-0036.</summary>
public static class StationJobTypes
{
    public const string Passenger = "passenger";
    public const string UrgentPassenger = "urgent_passenger";
    public const string Cargo = "cargo";
    public const string Bounty = "bounty";

    public static bool IsKnown(string? type) => type is Passenger or UrgentPassenger or Cargo or Bounty;

    public static bool CarriesPassengers(string? type) => type is Passenger or UrgentPassenger;
}

/// <summary>Known route risk published with an offer; derived from the route's risk profile.</summary>
public static class StationJobRiskBands
{
    public const string Safe = "safe";
    public const string Normal = "normal";
    public const string Elevated = "elevated";

    public static bool IsKnown(string? band) => band is Safe or Normal or Elevated;
}

/// <summary>Lifecycle of an accepted obligation shown in the single journal.</summary>
public static class StationJobStatuses
{
    public const string Active = "active";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";

    public static bool IsKnown(string? status) => status is Active or Completed or Failed or Cancelled;
}

/// <summary>
/// Explainable payout: <c>Payout = ceil((BaseTokens + LoadTokens + DistanceTokens) x UrgencyPermille x RiskPermille / 1 000 000)</c>.
/// Every factor is fixed by the engine at publication; the client only displays them.
/// </summary>
public sealed record JobPayoutTerms(long BaseTokens, long LoadTokens, long DistanceTokens, int UrgencyPermille, int RiskPermille);

/// <summary>
/// One published offer. Times are calendar game milliseconds. <see cref="DeadlineDurationMs"/> starts only when the
/// offer is accepted; <see cref="ExpiresAtGameTimeMs"/> ends the publication. Mass is kilograms (0 for passengers).
/// </summary>
public sealed record JobOffer(
    string Id,
    string Type,
    string OriginStationId,
    string DestinationStationId,
    int Count,
    long MassKg,
    long Payout,
    long DeadlineDurationMs,
    long PublishedAtGameTimeMs,
    long ExpiresAtGameTimeMs,
    string RiskBand,
    long RouteDistanceKm,
    JobPayoutTerms Terms,
    string? ItemTypeId = null);

/// <summary>An accepted offer in the journal; <see cref="ContractId"/> is stable across Save/Load.</summary>
public sealed record JobObligation(
    string ContractId,
    string OfferId,
    string Type,
    string OriginStationId,
    string DestinationStationId,
    int Count,
    long MassKg,
    long Payout,
    long AcceptedAtGameTimeMs,
    long DeadlineGameTimeMs,
    string Status,
    string RiskBand,
    string? ItemTypeId = null,
    long? ClosedAtGameTimeMs = null);

/// <summary>Authoritative acceptance verdict for one offer: null blocker means the offer can be accepted now.</summary>
public sealed record JobOfferAvailability(string OfferId, string? Blocker);

/// <summary>
/// Board and journal projection. Offers of every board in the cluster are listed so the journal works in flight;
/// acceptance is decided only by <see cref="Availability"/>. An offer missing there is unavailable, never acceptable.
/// </summary>
public sealed record StationJobsSnapshot(
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<JobOffer>))] ImmutableArray<JobOffer> Offers,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<JobObligation>))] ImmutableArray<JobObligation> Obligations,
    long ObservedAtGameTimeMs,
    string? DockedStationId = null,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<JobOfferAvailability>))] ImmutableArray<JobOfferAvailability> Availability = default);

/// <summary>Transport section part owned by the station job board.</summary>
public sealed partial record DemoTransportSnapshot
{
    /// <summary>Null when the session has no demo job board; never an implicit list of offers.</summary>
    public StationJobsSnapshot? StationJobs { get; init; }
}

/// <summary>
/// Accept one published offer. The engine re-checks docking, publication, capacity and revision; the client never
/// sends prices or deadlines. Owning stories (passenger/cargo) extend what acceptance does for their order types.
/// </summary>
public sealed record AcceptJobOfferCommand(string CommandId, long ExpectedRevision, string OfferId)
    : DemoCampaignCommand(CommandId, ExpectedRevision);

/// <summary>Stable rejection codes of the board; the client localizes them and never shows the raw code.</summary>
public static class StationJobReasons
{
    public const string OfferNotFound = "offer_not_found";
    public const string OfferExpired = "offer_expired";
    public const string OfferAlreadyAccepted = "offer_already_accepted";
    public const string NotDockedAtOrigin = "not_docked_at_origin";
    public const string InsufficientCargoSpace = "insufficient_cargo_space";

    /// <summary>
    /// Blocker of an offer as published by the engine. Missing board, missing verdict or unknown offer are
    /// <see cref="DemoCampaignReasons.Unavailable"/>; only an explicit null verdict allows acceptance.
    /// </summary>
    public static string? AcceptBlocker(StationJobsSnapshot? board, string? offerId)
    {
        if (board is null || string.IsNullOrWhiteSpace(offerId) || board.Availability.IsDefault)
            return DemoCampaignReasons.Unavailable;
        foreach (var verdict in board.Availability)
            if (verdict is not null && verdict.OfferId == offerId)
                return verdict.Blocker;
        return DemoCampaignReasons.Unavailable;
    }
}
