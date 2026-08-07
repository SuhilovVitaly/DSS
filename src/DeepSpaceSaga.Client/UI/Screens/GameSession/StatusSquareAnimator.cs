using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Binary visibility of the status square on the tactical map.
/// The square blinks once per second in real/UI time — a full
/// visible → hidden → visible cycle every 1000 ms — independent of
/// simulation speed, zoom and game time.
/// When the simulation is paused (Speed0), the square stays always
/// visible to indicate the frozen state.
/// </summary>
internal static class StatusSquareAnimator
{
    public const double PeriodMs = 1000.0;

    private const double VisiblePhaseMs = 500.0;

    /// <summary>
    /// Returns true while the status square should be drawn.
    /// Visible during [0, 500) ms of each 1000 ms UI-time period,
    /// hidden during [500, 1000) ms, then the cycle repeats.
    /// When <paramref name="speed"/> is <see cref="SimulationSpeed.Speed0"/>,
    /// always returns true — the square stays continuously visible
    /// to indicate the paused state.
    /// </summary>
    public static bool IsStatusSquareVisible(long uiTimeMs, SimulationSpeed speed)
    {
        if (speed == SimulationSpeed.Speed0)
            return true;

        double phase = uiTimeMs % (long)PeriodMs;
        if (phase < 0)
            phase += PeriodMs;

        return phase < VisiblePhaseMs;
    }
}
