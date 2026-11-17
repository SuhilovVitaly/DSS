using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Where the sealed cargo of an accepted cargo order is (EP-0034 US-0004). The engine derives it on every projection
/// from the obligation, the deadline, the failure record and the return receipt; the client only displays it.
/// </summary>
public static class CargoHoldStates
{
    /// <summary>The sealed cargo is in the hold and the deadline has not passed: dock at the destination to deliver.</summary>
    public const string Aboard = "aboard";

    /// <summary>The deadline has passed before delivery; no payout is due until the failure is recorded (US-0003).</summary>
    public const string Overdue = "overdue";

    /// <summary>The cargo was handed over at the destination, the hold was freed and the payout credited exactly once.</summary>
    public const string Delivered = "delivered";

    /// <summary>The contract was cancelled or expired; the cargo stays in the hold until the next human station takes it back.</summary>
    public const string ReturnPending = "return_pending";

    /// <summary>The cargo of a failed contract was returned without a payout and no longer occupies the hold.</summary>
    public const string Returned = "returned";

    /// <summary>The cargo was lost with the carrier.</summary>
    public const string Lost = "lost";

    public static bool IsKnown(string? state) => state is Aboard or Overdue or Delivered or ReturnPending or Returned or Lost;

    /// <summary>States in which the sealed cargo physically occupies hold mass.</summary>
    public static bool OccupiesHold(string? state) => state is Aboard or Overdue or ReturnPending;
}

/// <summary>
/// One accepted cargo order. Created by <see cref="AcceptJobOfferCommand"/> for a cargo offer: the same command seals
/// <see cref="Quantity"/> units of <see cref="ItemTypeId"/> (<see cref="MassKg"/> kilograms) into the hold. Sealed cargo
/// is not a player stack: it cannot be sold, bought into or spent by ordinary trade. Times are calendar game
/// milliseconds; <see cref="PostingId"/> is the durable voyage-ledger posting of the payout.
/// </summary>
public sealed record CargoContract(
    string ContractId,
    string OfferId,
    string ItemTypeId,
    long MassKg,
    long Quantity,
    string OriginStationId,
    string DestinationStationId,
    long AcceptedAtGameTimeMs,
    long DeadlineGameTimeMs,
    long Payout,
    string Status,
    string PostingId,
    string HoldState,
    long? DeliveredAtGameTimeMs = null,
    long? PaidCredits = null,
    long? ReturnedAtGameTimeMs = null,
    string? ReturnStationId = null);

/// <summary>
/// Cargo contracts of the demo campaign, oldest acceptance first, and the authoritative hold split of the player
/// carrier: total operational hold, mass sealed for contracts and mass of the player's own goods.
/// </summary>
public sealed record CargoContractsSnapshot(
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<CargoContract>))] ImmutableArray<CargoContract> Contracts,
    long ObservedAtGameTimeMs,
    long ReservedMassKg,
    long HoldCapacityKg,
    long PlayerCargoMassKg)
{
    /// <summary>Hold mass still free for the player's own goods; never negative.</summary>
    [JsonIgnore]
    public long FreeHoldKg => Math.Max(0, HoldCapacityKg - ReservedMassKg - PlayerCargoMassKg);

    /// <summary>Contracts whose sealed cargo is still to be delivered (active obligations).</summary>
    [JsonIgnore]
    public IEnumerable<CargoContract> Active =>
        Contracts.IsDefault ? [] : Contracts.Where(c => c is not null && c.Status == StationJobStatuses.Active);
}

/// <summary>Transport section part owned by the cargo contracts.</summary>
public sealed partial record DemoTransportSnapshot
{
    /// <summary>Null when the session has no demo cargo contracts; never an implicit reservation or payout.</summary>
    public CargoContractsSnapshot? CargoContracts { get; init; }
}

/// <summary>
/// Stable codes of the cargo contract lifecycle; the client localizes them and never shows the raw code. Acceptance
/// refusals reuse <see cref="StationJobReasons"/> (for example <see cref="StationJobReasons.InsufficientCargoSpace"/>);
/// cancellation and failures reuse <see cref="TransportFailureReasons"/>.
/// </summary>
public static class CargoContractReasons
{
    /// <summary>Durable voyage-ledger posting prefix of a cargo payout: <c>cargo:{contractId}</c>.</summary>
    public const string PostingPrefix = "cargo:";

    public static string PostingIdFor(string contractId) => PostingPrefix + contractId;

    /// <summary>
    /// Hold state of a contract as published by the engine. Missing section or unknown contract is
    /// <see cref="DemoCampaignReasons.Unavailable"/>; it is never guessed as delivered or paid.
    /// </summary>
    public static string HoldState(CargoContractsSnapshot? contracts, string? contractId)
    {
        if (contracts is null || string.IsNullOrWhiteSpace(contractId) || contracts.Contracts.IsDefault)
            return DemoCampaignReasons.Unavailable;
        foreach (var contract in contracts.Contracts)
            if (contract is not null && contract.ContractId == contractId && CargoHoldStates.IsKnown(contract.HoldState))
                return contract.HoldState;
        return DemoCampaignReasons.Unavailable;
    }
}
