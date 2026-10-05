using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

public static class VoyagePhases
{
    public const string Docked = "Docked";
    public const string Undocking = "Undocking";
    public const string InTransit = "InTransit";
    public const string Docking = "Docking";
}

public sealed record VoyageRouteOptionSnapshot(
    string DestinationStationObjectId,
    string DestinationDisplayName,
    long TravelEstimateGameTimeMs,
    string DistanceClass,
    bool IsAvailable = true,
    string? BlockReasonCode = null);

public sealed record VoyageSnapshot(
    string Phase,
    string? VoyageId = null,
    string? OriginStationObjectId = null,
    string? DestinationStationObjectId = null,
    string? DestinationDisplayName = null,
    int ProgressPermille = 0,
    string? BlockReasonCode = null,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<VoyageRouteOptionSnapshot>))]
    ImmutableArray<VoyageRouteOptionSnapshot> RouteOptions = default,
    long? ReservedFuelKg = null,
    long? ProjectedConsumedFuelKg = null,
    long? ProjectedRouteFuelCostCredits = null)
{
    [JsonIgnore]
    public string State => Phase;
}
