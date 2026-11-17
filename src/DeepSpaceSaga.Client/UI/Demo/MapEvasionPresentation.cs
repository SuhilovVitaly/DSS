using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Demo;

internal static class MapEvasionPresentation
{
    internal static string Describe(PursuitSnapshot? pursuit)
    {
        if (pursuit is null) return "";
        string key = pursuit.ObservedStatus switch
        {
            PursuitStatuses.Captured => "Captured",
            PursuitStatuses.Escaped => "Escaped",
            PursuitStatuses.Approaching => "Approaching",
            _ => "Receding"
        };
        string reason = pursuit.EndReason switch
        {
            "start_safe" => " · " + Localization.Get("Demo.Pursuit.Safe"),
            "contact_lost" => " · " + Localization.Get("Demo.Pursuit.Lost"),
            _ => ""
        };
        return Localization.Get("Demo.Pursuit." + key) + reason + " · " +
            string.Format(Localization.Get("Demo.Pursuit.Telemetry"), pursuit.RangeWorldUnits / 10,
                pursuit.ClosingSpeedWorldUnitsPerSecond / 10);
    }
}
