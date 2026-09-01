using System.Linq;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

/// <summary>
/// Shared toolbar strip for the station hub and every window opened from it (Station,
/// Trade, Hire, Contracts, Finance — all built on the 1400×800 gameplay-mechanic panel
/// standard, Docs/FirstRelease/Screens/ScreenCatalog.md). Flush against the panel's
/// top-left corner, spanning the full standard panel width, so every consuming screen
/// draws it with a single <see cref="Draw"/> call right after its panel background.
///
/// Also renders the docked station's name at the toolbar's top-left (20px/20px inset):
/// on the Station hub itself it is the "active location" label (orange, not clickable —
/// the player is already there); on every other station window it is a white link back
/// to the hub (see <see cref="NameLocalRect"/> for its click hit-test rect — the
/// consuming screen's OnMouseDown should return an event that closes it and opens
/// Station, e.g. ScreenEvent.NavigateToStation).
/// </summary>
public static class StationToolbar
{
    public const float Width = 1400f;
    public const float Height = 60f;
    public const float BorderWidth = 1f;

    public const float NameOffsetX = 20f;
    public const float NameOffsetY = 20f;
    public const float NameFontSize = 18f;

    public static readonly SKColor ColorBackground = new(0x5e, 0x5e, 0x5e);
    public static readonly SKColor ColorBorder = new(0x99, 0x99, 0x99);

    /// <summary>Station-name color on the Station hub itself — the current, active location.</summary>
    public static readonly SKColor ColorNameActive = new(0xe9, 0x9e, 0x58);

    /// <summary>Station-name color everywhere else — a clickable link back to the hub.</summary>
    public static readonly SKColor ColorNameLink = new(0xff, 0xff, 0xff);

    private static readonly SKPaint FillPaint = new() { Color = ColorBackground, Style = SKPaintStyle.Fill };
    private static readonly SKPaint BorderPaint =
        new() { Color = ColorBorder, Style = SKPaintStyle.Stroke, StrokeWidth = BorderWidth };

    private static readonly SKPaint NamePaintActive = MakeNamePaint(ColorNameActive);
    private static readonly SKPaint NamePaintLink = MakeNamePaint(ColorNameLink);

    private static SKPaint MakeNamePaint(SKColor color) => new()
    {
        Color = color,
        TextSize = NameFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Left,
        Typeface = MenuStyle.TypefaceHumaroid
    };

    /// <summary>Toolbar rect, local to the panel (add the panel's left/top to get screen space).</summary>
    public static SKRect LocalRect() => new(0, 0, Width, Height);

    /// <summary>
    /// Station-name label's local hit-test rect (panel-relative), tight around the
    /// rendered glyphs at the shared 20px/20px inset. Null/empty name yields an empty
    /// rect (never hit).
    /// </summary>
    public static SKRect NameLocalRect(string? stationName)
    {
        if (string.IsNullOrEmpty(stationName))
            return SKRect.Empty;

        var bounds = new SKRect();
        NamePaintLink.MeasureText(stationName, ref bounds);
        float baselineY = NameOffsetY - NamePaintLink.FontMetrics.Ascent;
        return new SKRect(
            NameOffsetX + bounds.Left, baselineY + bounds.Top,
            NameOffsetX + bounds.Right, baselineY + bounds.Bottom);
    }

    /// <summary>
    /// Draws the toolbar at the panel's top-left corner (panelLeft, panelTop), plus the
    /// station name if non-empty. <paramref name="isStationHub"/> selects the "active
    /// location" color (Station itself) vs. the "link back to hub" color (every other
    /// station window).
    /// </summary>
    public static void Draw(SKCanvas canvas, float panelLeft, float panelTop, string? stationName, bool isStationHub)
    {
        var rect = new SKRect(panelLeft, panelTop, panelLeft + Width, panelTop + Height);
        canvas.DrawRect(rect, FillPaint);
        canvas.DrawRect(rect, BorderPaint);

        if (string.IsNullOrEmpty(stationName))
            return;

        var paint = isStationHub ? NamePaintActive : NamePaintLink;
        float baselineY = panelTop + NameOffsetY - paint.FontMetrics.Ascent;
        canvas.DrawText(stationName, panelLeft + NameOffsetX, baselineY, paint);
    }

    /// <summary>
    /// Resolves the player ship's docked station name from a snapshot — null while not
    /// docked (e.g. Finance opened via Ctrl+F outside the Station flow), or before the
    /// first snapshot arrives. The single source of truth for what every station window's
    /// toolbar shows, so <see cref="Draw"/>'s callers all resolve it the same way.
    /// </summary>
    public static string? ResolveDockedStationName(AuthoritativeSnapshot? snapshot)
    {
        if (snapshot is null)
            return null;

        var ship = snapshot.Objects.FirstOrDefault(o => o.ObjectId == snapshot.PlayerShipObjectId);
        if (ship is null || !ship.IsDocked || ship.DockedStationObjectId is null)
            return null;

        var station = snapshot.Objects.FirstOrDefault(o => o.ObjectId == ship.DockedStationObjectId);
        return station?.DisplayName;
    }
}
