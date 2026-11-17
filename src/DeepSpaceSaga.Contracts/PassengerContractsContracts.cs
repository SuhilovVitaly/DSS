using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Delivery state of an accepted passenger contract (EP-0034 US-0002). The engine derives it on every projection
/// from the obligation, the deadline and the carrier's confirmed docking; the client only displays it.
/// </summary>
public static class PassengerDeliveryStates
{
    /// <summary>Passengers are aboard and the deadline has not passed: dock at the destination to deliver.</summary>
    public const string Aboard = "aboard";

    /// <summary>The deadline has passed before delivery; no payout is due (failure handling belongs to US-0003).</summary>
    public const string Overdue = "overdue";

    /// <summary>The passengers left the carrier at the destination and the payout was credited exactly once.</summary>
    public const string Delivered = "delivered";

    public static bool IsKnown(string? state) => state is Aboard or Overdue or Delivered;
}

/// <summary>
/// One accepted passenger order. Created by <see cref="AcceptJobOfferCommand"/> for a passenger offer: the same
/// command boards <see cref="Count"/> real passengers (<see cref="PassengerIds"/>) into free cabins. Times are
/// calendar game milliseconds; <see cref="PostingId"/> is the durable voyage-ledger posting of the payout.
/// <see cref="PaidCredits"/> and <see cref="DeliveredAtGameTimeMs"/> are set only after the confirmed delivery.
/// </summary>
public sealed record PassengerContract(
    string ContractId,
    string OfferId,
    string Type,
    string OriginStationId,
    string DestinationStationId,
    int Count,
    long AcceptedAtGameTimeMs,
    long DeadlineGameTimeMs,
    long Payout,
    string Status,
    string PostingId,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<string>))] ImmutableArray<string> PassengerIds,
    string DeliveryState,
    long? DeliveredAtGameTimeMs = null,
    long? PaidCredits = null);

/// <summary>
/// Passenger contracts of the demo campaign, newest acceptance last. <see cref="PassengersAboard"/> counts only
/// contract passengers still on the carrier; they eat rations and hold cabins like any person aboard.
/// </summary>
public sealed record PassengerContractsSnapshot(
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<PassengerContract>))] ImmutableArray<PassengerContract> Contracts,
    long ObservedAtGameTimeMs,
    int PassengersAboard)
{
    /// <summary>Contracts whose passengers are still aboard (active obligations).</summary>
    public IEnumerable<PassengerContract> Active =>
        Contracts.IsDefault ? [] : Contracts.Where(c => c is not null && c.Status == StationJobStatuses.Active);
}

/// <summary>Transport section part owned by the passenger contracts.</summary>
public sealed partial record DemoTransportSnapshot
{
    /// <summary>Null when the session has no demo passenger contracts; never an implicit list or payout.</summary>
    public PassengerContractsSnapshot? PassengerContracts { get; init; }
}

/// <summary>
/// Stable codes of the passenger contract lifecycle; the client localizes them and never shows the raw code.
/// Acceptance refusals reuse <see cref="StationJobReasons"/>, <see cref="PassengerCapacityReasons"/> and
/// <see cref="DemoCampaignReasons"/> because acceptance is the single board command.
/// </summary>
public static class PassengerContractReasons
{
    /// <summary>Durable voyage-ledger posting prefix of a passenger payout: <c>passenger:{contractId}</c>.</summary>
    public const string PostingPrefix = "passenger:";

    public static string PostingIdFor(string contractId) => PostingPrefix + contractId;

    /// <summary>
    /// Delivery state of a contract as published by the engine. Missing section or unknown contract is
    /// <see cref="DemoCampaignReasons.Unavailable"/>; it is never guessed as delivered or paid.
    /// </summary>
    public static string DeliveryState(PassengerContractsSnapshot? contracts, string? contractId)
    {
        if (contracts is null || string.IsNullOrWhiteSpace(contractId) || contracts.Contracts.IsDefault)
            return DemoCampaignReasons.Unavailable;
        foreach (var contract in contracts.Contracts)
            if (contract is not null && contract.ContractId == contractId && PassengerDeliveryStates.IsKnown(contract.DeliveryState))
                return contract.DeliveryState;
        return DemoCampaignReasons.Unavailable;
    }
}
