using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

/// <summary>
/// Shared toolbar strip for the station hub and every window opened from it (Station,
/// Trade, Hire, Contracts, Finance — all built on the 1400×800 gameplay-mechanic panel
/// standard, Docs/FirstRelease/Screens/ScreenCatalog.md). Flush against the panel's
/// top-left corner, spanning the full standard panel width, so every consuming screen
/// draws it with a single <see cref="Draw"/> call right after its panel background.
/// </summary>
public static class StationToolbar
{
    public const float Width = 1400f;
    public const float Height = 60f;
    public const float BorderWidth = 1f;

    public static readonly SKColor ColorBackground = new(0x5e, 0x5e, 0x5e);
    public static readonly SKColor ColorBorder = new(0x99, 0x99, 0x99);

    private static readonly SKPaint FillPaint = new() { Color = ColorBackground, Style = SKPaintStyle.Fill };
    private static readonly SKPaint BorderPaint =
        new() { Color = ColorBorder, Style = SKPaintStyle.Stroke, StrokeWidth = BorderWidth };

    /// <summary>Toolbar rect, local to the panel (add the panel's left/top to get screen space).</summary>
    public static SKRect LocalRect() => new(0, 0, Width, Height);

    /// <summary>Draws the toolbar at the panel's top-left corner (panelLeft, panelTop).</summary>
    public static void Draw(SKCanvas canvas, float panelLeft, float panelTop)
    {
        var rect = new SKRect(panelLeft, panelTop, panelLeft + Width, panelTop + Height);
        canvas.DrawRect(rect, FillPaint);
        canvas.DrawRect(rect, BorderPaint);
    }
}
