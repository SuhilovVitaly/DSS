using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

internal static class PanelTime
{
    internal static SimulationSpeed EffectiveSpeed(SimulationSpeed current, SimulationSpeed previous) =>
        current != SimulationSpeed.Speed0 ? current : previous != SimulationSpeed.Speed0 ? previous : SimulationSpeed.Speed1;

    internal static double RealSeconds(double physicalSeconds, SimulationSpeed speed) =>
        Math.Max(0, physicalSeconds) / EffectiveSpeed(speed, SimulationSpeed.Speed1).MotionTimeMultiplier();

    internal static string Format(double realSeconds) =>
        realSeconds.ToString(realSeconds < 10 ? "0.0" : "0");

    internal static string Tooltip(double physicalSeconds, SimulationSpeed speed) =>
        string.Format(Localization.Combat("Panel.TimeTooltip"), (int)speed, physicalSeconds.ToString("0.##"));
}
