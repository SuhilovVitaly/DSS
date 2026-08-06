using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Smooth status square color animation using smoothstep easing.
/// Blends between the object color and a perceptually-shifted variant
/// over a 1500 ms period. Freezes when the game is paused (Speed0).
/// </summary>
internal static class StatusSquareAnimator
{
    public const double PeriodMs = 1500.0;

    private const double ShiftFactor = 0.85;

    /// <summary>
    /// Smoothly blends between the object color and a shifted variant.
    /// Uses smoothstep easing: phase 0→1→0 over the period.
    /// At Speed0, always returns the object color — no animation.
    /// </summary>
    public static SKColor GetStatusColor(SKColor objectColor, long gameTimeMs, SimulationSpeed speed)
    {
        if (speed == SimulationSpeed.Speed0)
            return objectColor;

        double raw = (gameTimeMs % (long)PeriodMs) / PeriodMs;
        if (raw < 0) raw += 1.0;

        // Map 0→0.5→1.0 to 0→1→0 (peak at mid-period)
        double t = raw <= 0.5 ? raw * 2.0 : (1.0 - raw) * 2.0;

        // Smoothstep
        double phase = t * t * (3.0 - 2.0 * t);

        SKColor shifted = ShiftBrightness(objectColor);
        return Blend(objectColor, shifted, phase);
    }

    private static SKColor ShiftBrightness(SKColor c)
    {
        double perceived = 0.299 * c.Red + 0.587 * c.Green + 0.114 * c.Blue;
        bool isDark = perceived < 128;

        return isDark
            ? new SKColor(
                LerpByte(c.Red, 255, ShiftFactor),
                LerpByte(c.Green, 255, ShiftFactor),
                LerpByte(c.Blue, 255, ShiftFactor),
                c.Alpha)
            : new SKColor(
                LerpByte(c.Red, 0, ShiftFactor),
                LerpByte(c.Green, 0, ShiftFactor),
                LerpByte(c.Blue, 0, ShiftFactor),
                c.Alpha);
    }

    private static SKColor Blend(SKColor a, SKColor b, double t)
    {
        return new SKColor(
            LerpByte(a.Red, b.Red, t),
            LerpByte(a.Green, b.Green, t),
            LerpByte(a.Blue, b.Blue, t),
            LerpByte(a.Alpha, b.Alpha, t));
    }

    private static byte LerpByte(byte from, byte to, double t)
        => (byte)(from + (to - from) * t);
}
