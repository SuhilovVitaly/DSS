using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Terminal failure of one accepted transport obligation (EP-0034 US-0003). Times are calendar game milliseconds,
/// money is whole Tokens. <see cref="PenaltyCredits"/> is what the failure costs; <see cref="PenaltyPaid"/> was taken
/// from the available Tokens at the failure, <see cref="RepaidCredits"/> was collected later from contract payouts and
/// <see cref="Debt"/> is still owed (paid + repaid + debt = penalty). Passengers of a failed passenger contract stay
/// aboard (<see cref="DisembarkPending"/>) until the carrier's next confirmed docking; they are never removed in
/// open space. <see cref="PostingId"/> is the durable voyage-ledger posting of the penalty.
/// </summary>
public sealed record TransportFailureSnapshot(
    string ContractId,
    string Type,
    string Reason,
    long FailedAtGameTimeMs,
    long PenaltyCredits,
    long PenaltyPaid,
    long Debt,
    bool DisembarkPending,
    string PostingId,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<string>))] ImmutableArray<string> PassengerIds,
    long RepaidCredits = 0,
    long? DisembarkedAtGameTimeMs = null,
    string? DisembarkStationId = null);

/// <summary>
/// Consequences of an active obligation published before the player acts: the penalty of a cancellation now, the
/// penalty if the deadline passes, the deadline itself and the authoritative cancellation verdict (null = allowed).
/// </summary>
public sealed record TransportCancellationTerms(
    string ContractId,
    long CancelPenaltyCredits,
    long ExpiryPenaltyCredits,
    long DeadlineGameTimeMs,
    string? Blocker);

/// <summary>
/// Failure section of the demo transport: terminal failures (oldest first), terms of every active obligation and the
/// total contract debt. An obligation missing from <see cref="Terms"/> cannot be cancelled.
/// </summary>
public sealed record TransportFailuresSnapshot(
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<TransportFailureSnapshot>))] ImmutableArray<TransportFailureSnapshot> Failures,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<TransportCancellationTerms>))] ImmutableArray<TransportCancellationTerms> Terms,
    long ContractDebt,
    long ObservedAtGameTimeMs);

/// <summary>Transport section part owned by the transport failure rules.</summary>
public sealed partial record DemoTransportSnapshot
{
    /// <summary>Null when the session has no demo transport contracts; never an implicit debt or cancellation.</summary>
    public TransportFailuresSnapshot? TransportFailures { get; init; }
}

/// <summary>
/// Cancel one active transport obligation. The engine decides the penalty, the debt and where passengers leave the
/// carrier; the client sends only the contract identity.
/// </summary>
public sealed record CancelTransportCommand(string CommandId, long ExpectedRevision, string ContractId)
    : DemoCampaignCommand(CommandId, ExpectedRevision);

/// <summary>Stable outcome and refusal codes of transport failures; the client localizes them, never shows raw codes.</summary>
public static class TransportFailureReasons
{
    /// <summary>The player cancelled the obligation (journal status <see cref="StationJobStatuses.Cancelled"/>).</summary>
    public const string Cancelled = "cancelled";

    /// <summary>The calendar deadline passed before delivery (journal status <see cref="StationJobStatuses.Failed"/>).</summary>
    public const string Expired = "expired";

    /// <summary>The carrier was destroyed; the obligation is lost without a penalty (status Failed).</summary>
    public const string CarrierLost = "carrier_lost";

    public static bool IsKnownOutcome(string? reason) => reason is Cancelled or Expired or CarrierLost;

    /// <summary>Cancellation refusal: no obligation with this contract id exists in the journal.</summary>
    public const string ContractNotFound = "contract_not_found";

    /// <summary>Cancellation refusal: the obligation is already completed, failed or cancelled.</summary>
    public const string ContractNotActive = "contract_not_active";

    /// <summary>Cancellation refusal: this obligation type is not a transport order (bounty belongs to EP-0036).</summary>
    public const string NotCancellable = "not_cancellable";

    /// <summary>Durable voyage-ledger posting prefix of a penalty: <c>penalty:{contractId}</c>.</summary>
    public const string PostingPrefix = "penalty:";

    public static string PostingIdFor(string contractId) => PostingPrefix + contractId;

    /// <summary>
    /// Cancellation verdict as published by the engine. Missing section, missing terms or an unknown contract are
    /// <see cref="DemoCampaignReasons.Unavailable"/>; only an explicit null verdict allows cancellation.
    /// </summary>
    public static string? CancelBlocker(TransportFailuresSnapshot? failures, string? contractId)
    {
        if (failures is null || string.IsNullOrWhiteSpace(contractId) || failures.Terms.IsDefault)
            return DemoCampaignReasons.Unavailable;
        foreach (var terms in failures.Terms)
            if (terms is not null && terms.ContractId == contractId)
                return terms.Blocker;
        return DemoCampaignReasons.Unavailable;
    }

    /// <summary>Published failure of a contract, or null when none is known (never a guessed outcome).</summary>
    public static TransportFailureSnapshot? Outcome(TransportFailuresSnapshot? failures, string? contractId)
    {
        if (failures is null || string.IsNullOrWhiteSpace(contractId) || failures.Failures.IsDefault) return null;
        foreach (var failure in failures.Failures)
            if (failure is not null && failure.ContractId == contractId && IsKnownOutcome(failure.Reason))
                return failure;
        return null;
    }
}
