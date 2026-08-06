using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Binary visibility of the status square on the tactical map.
/// The square is drawn during the first 1000 ms of each 2000 ms period
/// and not drawn during the second half. Freezes visible when paused (Speed0).
/// </summary>
internal static class StatusSquareAnimator
{
    public const double PeriodMs = 2000.0;

    private const double VisiblePhaseMs = 1000.0;

    /// <summary>
    /// Returns true while the status square should be drawn.
    /// Visible during [0, 1000) ms of each 2000 ms period, hidden during
    /// [1000, 2000) ms, then the cycle repeats. At Speed0 the square stays
    /// visible — it never blinks while paused.
    /// </summary>
    public static bool IsStatusSquareVisible(long gameTimeMs, SimulationSpeed speed)
    {
        if (speed == SimulationSpeed.Speed0)
            return true;

        double phase = gameTimeMs % (long)PeriodMs;
        if (phase < 0)
            phase += PeriodMs;

        return phase < VisiblePhaseMs;
    }
}
