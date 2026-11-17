using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Demo;

public static class PilotLossPresentation
{
    public static string Entry(PilotMemorialEntry entry) => string.Format(Localization.Get("Demo.PilotLoss.Entry"),
        entry.Pilot.DisplayName, entry.Pilot.Accuracy, entry.Pilot.Maneuver, entry.Pilot.Initiative, entry.GameTimeMs);
}
