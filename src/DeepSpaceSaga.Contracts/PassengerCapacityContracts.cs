namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Authoritative cabin split of the player carrier (EP-0034 US-0001). The engine derives every number
/// from the operational living-quarters modules, the live crew and the people aboard on each snapshot;
/// the client never estimates it and a save never stores it. <see cref="FreeCabins"/> is never negative:
/// a deficit is reported through <see cref="LifeSupportReason"/> instead.
/// </summary>
public sealed record PassengerCapacitySnapshot(
    int TotalCabins,
    int CrewCabins,
    int PassengerCabins,
    int ReservedCabins,
    int FreeCabins,
    string? LifeSupportReason = null,
    int PrisonerCabins = 0);

/// <summary>Transport section of the demo campaign projection; further parts are added by later stories.</summary>
public sealed partial record DemoTransportSnapshot
{
    /// <summary>Null when the session has no passenger-capable carrier; never a fabricated capacity.</summary>
    public PassengerCapacitySnapshot? PassengerCapacity { get; init; }
}

/// <summary>Stable reason codes; the client localizes them and never shows the raw code.</summary>
public static class PassengerCapacityReasons
{
    public const string NoFreeCabins = "no_free_cabins";
    public const string CabinsOvercommitted = "cabins_overcommitted";
    public const string InsufficientFoodRations = "insufficient_food_rations";

    /// <summary>
    /// First reason a new passenger cannot be taken aboard, or null when at least one cabin is free and
    /// life support is sufficient. Missing capacity is <see cref="DemoCampaignReasons.Unavailable"/>.
    /// </summary>
    public static string? BoardingBlocker(PassengerCapacitySnapshot? capacity) =>
        capacity is null ? DemoCampaignReasons.Unavailable
        : capacity.LifeSupportReason ?? (capacity.FreeCabins > 0 ? null : NoFreeCabins);
}
