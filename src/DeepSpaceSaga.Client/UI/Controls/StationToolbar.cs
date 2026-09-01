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
/// Station, e.g. ScreenEvent.NavigateToStation). Sub-windows additionally pass a
/// windowName to <see cref="Draw"/>, rendered as a breadcrumb after the station name:
/// <c>stationName  &gt;&gt;  windowName</c> (see <see cref="Draw"/> for the full rule).
/// </summary>
public static class StationToolbar
{
    public const float Width = 1400f;
    public const float Height = 60f;
    public const float BorderWidth = 1f;

    public const float NameOffsetX = 20f;
    public const float NameOffsetY = 20f;
    public const float NameFontSize = 26f;
    public const float NameHoverGlowSigma = 10f;

    /// <summary>Gap on each side of the ">>" breadcrumb separator (see <see cref="Draw"/>).</summary>
    public const float NameSegmentGap = 16f;

    private const string SeparatorText = ">>";

    public static readonly SKColor ColorBackground = new(0x5e, 0x5e, 0x5e);
    public static readonly SKColor ColorBorder = new(0x99, 0x99, 0x99);

    /// <summary>Station-name color on the Station hub itself — the current, active location.
    /// Also the color of the breadcrumb window-name segment on every other station window.</summary>
    public static readonly SKColor ColorNameActive = new(0xe9, 0x9e, 0x58);

    /// <summary>Station-name color everywhere else — a clickable link back to the hub.</summary>
    public static readonly SKColor ColorNameLink = new(0xff, 0xff, 0xff);

    /// <summary>Breadcrumb ">>" separator color — same white as the inactive/link station name.</summary>
    public static readonly SKColor ColorSeparator = ColorNameLink;

    private static readonly SKPaint FillPaint = new() { Color = ColorBackground, Style = SKPaintStyle.Fill };
    private static readonly SKPaint BorderPaint =
        new() { Color = ColorBorder, Style = SKPaintStyle.Stroke, StrokeWidth = BorderWidth };

    private static readonly SKPaint NamePaintActive = MakeNamePaint(ColorNameActive);
    private static readonly SKPaint NamePaintLink = MakeNamePaint(ColorNameLink);
    private static readonly SKPaint SeparatorPaint = MakeNamePaint(ColorSeparator);

    /// <summary>
    /// Hover glow color — <see cref="ColorNameActive"/>'s hue and brightness at full (100%)
    /// saturation, so the blurred halo still reads as a vivid, saturated orange rather
    /// than washed-out pastel (blurring inherently spreads alpha thinner toward the
    /// edges, which dilutes color unless it starts fully saturated).
    /// </summary>
    public static readonly SKColor ColorNameGlow = FullySaturated(ColorNameActive);

    /// <summary>
    /// Hover-only glow drawn behind the link text (see <see cref="Draw"/>) — a soft
    /// blurred halo around the glyphs reads as "this text is brighter / a live link"
    /// without needing a separate hover typeface or layout shift.
    /// </summary>
    private static readonly SKPaint NameGlowPaint = MakeGlowPaint();

    // MenuStyle.TypefaceHumaroid loads humaroid.regular.otf — there is no separate bold
    // weight file for this font, so FakeBoldText synthetically embolds it (SkiaSharp's
    // standard approach for a family with no true bold face).
    private static SKPaint MakeNamePaint(SKColor color) => new()
    {
        Color = color,
        TextSize = NameFontSize,
        IsAntialias = true,
        FakeBoldText = true,
        TextAlign = SKTextAlign.Left,
        Typeface = MenuStyle.TypefaceHumaroid
    };

    private static SKPaint MakeGlowPaint()
    {
        var paint = MakeNamePaint(ColorNameGlow);
        paint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, NameHoverGlowSigma);
        return paint;
    }

    /// <summary>Returns <paramref name="color"/> with its HSV saturation set to 100%, same hue/brightness.</summary>
    private static SKColor FullySaturated(SKColor color)
    {
        color.ToHsv(out float h, out _, out float v);
        return SKColor.FromHsv(h, 100f, v, color.Alpha);
    }

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
    /// station window). <paramref name="isHovered"/> only has an effect on a non-hub
    /// window — it adds the blurred <see cref="ColorNameGlow"/> halo that marks the name
    /// as a live link; the hub's own label is never hoverable, so hover state is ignored
    /// there.
    ///
    /// <paramref name="windowName"/> is the breadcrumb trailing segment for a station
    /// sub-window (e.g. "TRADE", "HIRE") — rendered after the station name as
    /// <c>stationName  &gt;&gt;  windowName</c>, same font/size as the station name, in
    /// <see cref="ColorNameActive"/> (marking it as the current location, same as the
    /// hub's own label color). The Station hub itself passes null here: it has no
    /// separate sub-window to name. If <paramref name="stationName"/> is empty (not
    /// docked) but <paramref name="windowName"/> is given, the breadcrumb and separator
    /// are skipped and only the window name is drawn — there is no station to link from.
    /// Neither the separator nor the window-name segment is clickable or hoverable.
    /// </summary>
    public static void Draw(
        SKCanvas canvas, float panelLeft, float panelTop, string? stationName, bool isStationHub,
        bool isHovered = false, string? windowName = null)
    {
        var rect = new SKRect(panelLeft, panelTop, panelLeft + Width, panelTop + Height);
        canvas.DrawRect(rect, FillPaint);
        canvas.DrawRect(rect, BorderPaint);

        float baselineY = panelTop + NameOffsetY - NamePaintLink.FontMetrics.Ascent;
        float x = panelLeft + NameOffsetX;

        if (!string.IsNullOrEmpty(stationName))
        {
            var namePaint = isStationHub ? NamePaintActive : NamePaintLink;

            if (isHovered && !isStationHub)
                canvas.DrawText(stationName, x, baselineY, NameGlowPaint);

            canvas.DrawText(stationName, x, baselineY, namePaint);
            x += namePaint.MeasureText(stationName);
        }

        if (string.IsNullOrEmpty(windowName))
            return;

        if (!string.IsNullOrEmpty(stationName))
        {
            x += NameSegmentGap;
            canvas.DrawText(SeparatorText, x, baselineY, SeparatorPaint);
            x += SeparatorPaint.MeasureText(SeparatorText) + NameSegmentGap;
        }

        canvas.DrawText(windowName, x, baselineY, NamePaintActive);
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
