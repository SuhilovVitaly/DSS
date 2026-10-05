using System.Collections.Immutable;
using System.Globalization;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.Station;

internal sealed record StationRouteRow(string DestinationStationObjectId, string PrimaryText, string SecondaryText,
    string? ReasonText, bool IsEnabled, TradingRouteRisk Risk, TradingRouteAvailability Availability,
    string? ActiveEventText = null);

internal static class StationRoutePresentation
{
    internal const string EmptyMessage = "Маршруты недоступны";

    internal static ImmutableArray<StationRouteRow> Build(ImmutableArray<TradingRouteSnapshot> routes)
    {
        if (routes.IsDefaultOrEmpty) return [];
        return routes.Select(r =>
        {
            string primary = $"{r.DestinationStationObjectId}  [{(r.Risk == TradingRouteRisk.Safe ? "SAFE" : "RISK")}]  {r.Availability}";
            string secondary = $"{r.DistanceClass}  ETA {FormatGameDuration(r.EffectiveTravelEstimateGameTimeMs)}  Fuel x{FormatFuelMultiplier(r.EffectiveFuelMultiplierPermille)}";
            if (r.BaseTravelEstimateGameTimeMs != r.EffectiveTravelEstimateGameTimeMs || r.BaseFuelMultiplierPermille != r.EffectiveFuelMultiplierPermille)
                secondary += $"  (base {FormatGameDuration(r.BaseTravelEstimateGameTimeMs)}, x{FormatFuelMultiplier(r.BaseFuelMultiplierPermille)})";
            return new StationRouteRow(r.DestinationStationObjectId, primary, secondary, r.ReasonText,
                r.Availability is TradingRouteAvailability.Available or TradingRouteAvailability.Restricted, r.Risk, r.Availability,
                r.ActiveEventIds.IsDefaultOrEmpty ? null : "Events: " + string.Join("; ", r.ActiveEventIds));
        }).ToImmutableArray();
    }

    internal static string FormatGameDuration(long time)
    {
        long minutes = Math.Max(0, time) / 60000 + (time > 0 && time % 60000 != 0 ? 1 : 0);
        long days = minutes / 1440;
        return (days > 0 ? days.ToString(CultureInfo.InvariantCulture) + "d " : "") +
            (minutes / 60 % 24).ToString("D2", CultureInfo.InvariantCulture) + ":" + (minutes % 60).ToString("D2", CultureInfo.InvariantCulture);
    }

    internal static string FormatFuelMultiplier(int permille) =>
        decimal.Round(permille / 1000m, 2, MidpointRounding.AwayFromZero).ToString("F2", CultureInfo.InvariantCulture);
}
