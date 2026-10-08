namespace DeepSpaceSaga.Contracts;

/// <summary>Authoritative exactly-once fuel settlement for one completed or interrupted voyage.</summary>
public sealed record VoyageFuelSettlementSnapshot(string VoyageId, long ReservedFuelKg,
    long ConsumedFuelKg, long ReturnedFuelKg, long RouteFuelCostCredits);
