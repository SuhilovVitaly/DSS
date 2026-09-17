using System.Text.Json.Serialization;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>Versioned time-dependent session state; wall-clock timestamps are never used to resume it.</summary>
public sealed record EconomyTimeData(
    [property: JsonPropertyName("rulesVersion")] int RulesVersion = EconomyTimeData.CurrentRulesVersion,
    [property: JsonPropertyName("stationDistrict")] StationDistrict StationDistrict = StationDistrict.Dock,
    [property: JsonPropertyName("travelReceipts")] IReadOnlyList<string>? TravelReceipts = null,
    [property: JsonPropertyName("activeContracts")] IReadOnlyList<TimedContractState>? ActiveContracts = null,
    [property: JsonPropertyName("routeArrivalGameTimeMs")] long? RouteArrivalGameTimeMs = null,
    [property: JsonPropertyName("missingRations")] long MissingRations = 0)
{
    public const int CurrentRulesVersion = 1;
}
