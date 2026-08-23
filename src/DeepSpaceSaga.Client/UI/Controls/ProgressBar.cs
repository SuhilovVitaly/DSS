using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

/// <summary>
/// Flat horizontal fill bar (Trade screen's CARGO LOAD / FUEL TANK indicators). Stateless —
/// draws a dark track (SettingsScreen's flat combo-box colors) with a colored fill
/// proportional to <c>fraction</c>. No label/percentage text — the owning screen draws
/// that separately alongside the bar.
/// </summary>
public static class ProgressBar
{
    /// <summary>
    /// Default fill accent — the same orange (0xFF8404) already used as the "selected /
    /// highlighted" accent color on ScenarioSelectScreen's row indicator and info-panel
    /// separator, reused here so the Trade screen's fill bars read as the same accent
    /// family as the rest of the UI rather than introducing a new hue.
    /// </summary>
    private static readonly SKColor _defaultFillColor = new(0xFF, 0x84, 0x04);

    /// <summary>Track background — same flat dark fill as SettingsScreen's combo-box.</summary>
    private static readonly SKPaint _trackFill = new() { Color = new SKColor(0x2D, 0x2D, 0x2D), Style = SKPaintStyle.Fill };

    /// <summary>Track border — same as SettingsScreen's combo-box border.</summary>
    private static readonly SKPaint _trackBorder = new() { Color = new SKColor(0x50, 0x50, 0x50), Style = SKPaintStyle.Stroke, StrokeWidth = 1f };

    /// <summary>
    /// Single reusable fill paint — its Color is set per <see cref="Draw"/> call (defaulting
    /// to <see cref="_defaultFillColor"/>) rather than allocating a new SKPaint per call/color,
    /// matching this control set's "paints as static fields, created once" convention.
    /// </summary>
    private static readonly SKPaint _fillPaint = new() { Color = _defaultFillColor, Style = SKPaintStyle.Fill };

    public static double ClampFraction(double fraction) => Math.Clamp(fraction, 0.0, 1.0);

    /// <summary>Fill width in pixels for a track of <paramref name="trackWidth"/>, clamping fraction to 0..1 first.</summary>
    public static float ComputeFillWidth(float trackWidth, double fraction) =>
        (float)(trackWidth * ClampFraction(fraction));

    public static void Draw(SKCanvas canvas, SKRect rect, double fraction, SKColor? fillColor = null)
    {
        canvas.DrawRect(rect, _trackFill);

        var fillWidth = ComputeFillWidth(rect.Width, fraction);
        if (fillWidth > 0f)
        {
            var fillRect = new SKRect(rect.Left, rect.Top, rect.Left + fillWidth, rect.Bottom);
            _fillPaint.Color = fillColor ?? _defaultFillColor;
            canvas.DrawRect(fillRect, _fillPaint);
        }

        canvas.DrawRect(rect, _trackBorder);
    }
}
