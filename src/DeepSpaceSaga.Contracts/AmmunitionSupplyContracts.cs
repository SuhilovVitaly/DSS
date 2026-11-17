using System.Collections.Immutable;

namespace DeepSpaceSaga.Contracts;

public sealed record DecoyQuoteRequest(string CommandId, long ExpectedRevision, string DroneId, int Quantity)
    : DemoCampaignCommand(CommandId, ExpectedRevision);
public sealed record ResupplyDecoyCommand(string CommandId, long ExpectedRevision, string DroneId, string QuoteId, int Quantity)
    : DemoCampaignCommand(CommandId, ExpectedRevision);
public sealed record DecoySupplyQuote(string Id, string StationId, string DroneId, long Revision,
    int CarrierRevision, long ExpiresAt, int Quantity, int Before, long Total);
public sealed record DroneSupply(string DroneId, int AmmoCurrent, int AmmoCapacity, int DecoyCharges, int DecoyCapacity);
/// <summary>Stock is actual torpedoes in cargo; loaded ammunition is reported separately.</summary>
public sealed record SupplySnapshot(long Stock, ImmutableArray<DroneSupply> Drones, long MaximumShots,
    long? DecoyUnitPrice = null, DecoySupplyQuote? Quote = null, string? Reason = null);
