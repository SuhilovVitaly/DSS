using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Client.UI.Screens.Station;

namespace DeepSpaceSaga.Client.UI.Demo;

internal static class ThreatRoutesPresentation
{
    internal static ImmutableArray<StationRouteRow> Rows(ThreatRoutesSnapshot routes, long time) => routes.Options.Select(option =>
    {
        string risk = Localization.Get("Demo.Route." + (option.KnownRiskBand switch { "lower" => "Lower", "exposed" => "Exposed", _ => "Unknown" }));
        string terms = string.Format(Localization.Get("Demo.Route.Terms"),
            StationRoutePresentation.FormatGameDuration(Math.Max(0, option.EtaGameTimeMs - time)), option.FuelRequiredKg?.ToString() ?? "?", risk);
        string deadline = option.DeadlineDeltaGameTimeMs is { } slack
            ? string.Format(Localization.Get(slack < 0 ? "Demo.Route.Late" : "Demo.Route.Slack"),
                StationRoutePresentation.FormatGameDuration(slack == long.MinValue ? long.MaxValue : Math.Abs(slack)))
            : Localization.Get("Demo.Route.NoDeadline");
        return new StationRouteRow(option.DestinationStationId,
            option.DestinationStationId + " · " + Localization.Get(option.IsDetour ? "Demo.Route.Detour" : "Demo.Route.Direct"), terms,
            option.UnavailableReason is null ? deadline : Reason(option.UnavailableReason) + " · " + deadline,
            option.UnavailableReason is null, option.KnownRiskBand == "lower" ? TradingRouteRisk.Safe : TradingRouteRisk.Elevated,
            option.UnavailableReason is null ? TradingRouteAvailability.Available : TradingRouteAvailability.Unavailable,
            RouteId: option.RouteId);
    }).ToImmutableArray();

    internal static string Reason(string? reason) => Localization.Get("Demo.Route." + (reason switch
    {
        DemoCampaignReasons.StaleRevision => "Stale",
        CommandReasonCodes.InsufficientVoyageFuel => "Fuel",
        CommandReasonCodes.FuelEfficiencyUnavailable or CommandReasonCodes.ModuleUnavailable => "Engine",
        CommandReasonCodes.VoyageOutstandingDebt => "Debt",
        null => "Selected",
        _ => "Unavailable"
    }));
}
