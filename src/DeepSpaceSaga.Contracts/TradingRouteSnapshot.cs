using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter<TradingRouteAvailability>))]
public enum TradingRouteAvailability { Available, Restricted, Unavailable }

[JsonConverter(typeof(JsonStringEnumConverter<TradingRouteRisk>))]
public enum TradingRouteRisk { Safe, Elevated }

/// <summary>Authoritative economic conditions of a direction; physical motion remains independent.</summary>
public sealed record TradingRouteSnapshot(
    string OriginStationObjectId,
    string DestinationStationObjectId,
    string DistanceClass,
    long BaseTravelEstimateGameTimeMs,
    long EffectiveTravelEstimateGameTimeMs,
    int BaseFuelMultiplierPermille,
    int EffectiveFuelMultiplierPermille,
    string RiskProfileId,
    TradingRouteRisk Risk,
    TradingRouteAvailability Availability,
    string? ReasonText = null,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<string>))]
    ImmutableArray<string> ActiveEventIds = default);
