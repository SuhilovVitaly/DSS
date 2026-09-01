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
///
/// Also draws the shared exit-button icon, right-aligned (see
/// <see cref="ExitButtonLocalRect"/>) — this replaces the per-panel × close button every
/// station window used to have; the consuming screen hit-tests it for the click (same
/// close event as before), the hover cursor swap, and the same blurred hover glow the
/// station-name link gets (see <see cref="Draw"/>'s isExitButtonHovered parameter).
///
/// Also draws a resource info panel pinned to the right, immediately before the exit
/// button — starting with a food-rations icon + count (see
/// <see cref="ResolveFoodRationsCount"/>), in a field reserved wide enough for 4 digits.
/// Unlike the exit button, this icon is a plain readout: never clickable, never gets a
/// hover glow — hovering it only shows an explanatory tooltip below the toolbar.
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

    public const float ExitButtonSize = 32f;
    public const float ExitButtonMarginRight = 15f;

    public const float ResourceIconSize = 32f;
    public const float ResourceIconTextGap = 8f;
    public const float ResourceValueFontSize = 22f;

    /// <summary>Gap between the resource info panel's right edge and the exit button's left edge.</summary>
    public const float ResourceInfoGapFromExitButton = 70f;

    /// <summary>
    /// The reserved value-field width is measured from this 4-digit sample, so the field
    /// never has to grow/shrink (and the icon never has to shift) as the actual count
    /// crosses a digit-count boundary — up to 9999 always fits without re-layout.
    /// </summary>
    private const string ResourceValueFieldSample = "9999";

    public const string FoodRationsItemTypeId = "item.food-rations";

    private static readonly string[] FoodRationsTooltipLines =
    {
        "Food rations stored aboard the ship.",
        "Each crew member or passenger consumes 2 rations per day."
    };

    private const float TooltipPaddingX = 10f;
    private const float TooltipPaddingY = 8f;
    private const float TooltipFontSize = 13f;
    private const float TooltipLineHeight = 16f;
    private const float TooltipGapBelowToolbar = 6f;
    private const float TooltipCornerRadius = 4f;

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

    private static readonly SKBitmap? ExitButtonImage =
        LoadImage("Images/UI/Panels/station-toolbar/toolbar-exit.png");

    /// <summary>True if the exit-button PNG was found and decoded at startup.</summary>
    internal static bool HasLoadedExitButtonImage => ExitButtonImage is not null;

    private static readonly SKBitmap? FoodRationsImage =
        LoadImage("Images/UI/Panels/station-toolbar/toolbar-rations.png");

    /// <summary>True if the food-rations PNG was found and decoded at startup.</summary>
    internal static bool HasLoadedFoodRationsImage => FoodRationsImage is not null;

    private static readonly SKPaint ResourceValuePaint = new()
    {
        Color = MenuStyle.ColorText,
        TextSize = ResourceValueFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Left,
        Typeface = MenuStyle.TypefaceHumaroid
    };

    /// <summary>Reserved width for a resource value field — always fits up to 4 digits (9999).</summary>
    private static readonly float ResourceValueFieldWidth = ResourceValuePaint.MeasureText(ResourceValueFieldSample);

    private static readonly SKPaint TooltipBackgroundPaint =
        new() { Color = new SKColor(0x1a, 0x1a, 0x1a, 235), Style = SKPaintStyle.Fill, IsAntialias = true };

    private static readonly SKPaint TooltipBorderPaint = new()
    {
        Color = ColorBorder, Style = SKPaintStyle.Stroke, StrokeWidth = 1f, IsAntialias = true
    };

    private static readonly SKPaint TooltipTextPaint = new()
    {
        Color = SKColors.White,
        TextSize = TooltipFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Left,
        Typeface = MenuStyle.TypefaceRegular
    };

    private static SKBitmap? LoadImage(string path)
    {
        try { return File.Exists(path) ? SKBitmap.Decode(path) : null; }
        catch { return null; }
    }

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

    /// <summary>
    /// Hover-only glow for the exit-button icon — same blur radius and glow color as the
    /// station-name link's hover glow (<see cref="NameGlowPaint"/>), so both hoverable
    /// elements on the toolbar read as the same kind of "live" affordance. Drawn as a
    /// filled, blurred oval behind the icon's rect (a mask-filter blur on the icon bitmap
    /// itself is not reliably supported by every Skia raster backend for image draws),
    /// then the sharp icon is drawn on top in <see cref="Draw"/>.
    /// </summary>
    private static readonly SKPaint ExitButtonGlowPaint = new()
    {
        Color = ColorNameGlow,
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
        MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, NameHoverGlowSigma)
    };

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
    /// Exit-button rect, local to the panel (add the panel's left/top for screen space) —
    /// right-aligned with a <see cref="ExitButtonMarginRight"/> margin from the toolbar's
    /// (and panel's) right edge, vertically centered in the toolbar's height. The
    /// consuming screen hit-tests this for both the click (return the same close event the
    /// removed × button used to) and the hover cursor swap — the icon itself has no
    /// separate hover art, so hover is cursor-only.
    /// </summary>
    public static SKRect ExitButtonLocalRect()
    {
        float right = Width - ExitButtonMarginRight;
        float left = right - ExitButtonSize;
        float top = (Height - ExitButtonSize) / 2f;
        return new SKRect(left, top, right, top + ExitButtonSize);
    }

    /// <summary>
    /// Food-rations icon+value hit-test rect, local to the panel — spans from the icon's
    /// left edge to the reserved value field's right edge (see
    /// <see cref="ResourceValueFieldWidth"/>), so it covers both regardless of the actual
    /// count's digit width. Used only to show the tooltip on hover — this readout is never
    /// clickable and never gets a hover glow.
    /// </summary>
    public static SKRect FoodRationsLocalRect()
    {
        float blockRight = ExitButtonLocalRect().Left - ResourceInfoGapFromExitButton;
        float valueFieldLeft = blockRight - ResourceValueFieldWidth;
        float iconLeft = valueFieldLeft - ResourceIconTextGap - ResourceIconSize;
        float top = (Height - ResourceIconSize) / 2f;
        return new SKRect(iconLeft, top, blockRight, top + ResourceIconSize);
    }

    /// <summary>
    /// Draws the toolbar at the panel's top-left corner (panelLeft, panelTop): background,
    /// border, the station name if non-empty, and the exit-button icon. <paramref
    /// name="isStationHub"/> selects the "active location" color (Station itself) vs. the
    /// "link back to hub" color (every other station window). <paramref name="isHovered"/>
    /// only has an effect on a non-hub window — it adds the blurred <see
    /// cref="ColorNameGlow"/> halo that marks the name as a live link; the hub's own label
    /// is never hoverable, so hover state is ignored there.
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
    ///
    /// <paramref name="isExitButtonHovered"/> adds the same blurred glow behind the
    /// exit-button icon as <paramref name="isHovered"/> adds behind the station-name link.
    ///
    /// <paramref name="foodRationsCount"/> feeds the resource info panel — pinned to the
    /// toolbar's right side, immediately before the exit button (see
    /// <see cref="ResourceInfoGapFromExitButton"/>): the food-rations icon followed by its
    /// count in a field reserved wide enough for 4 digits (9999) so the icon never shifts
    /// as the count's digit width changes. This icon is a plain readout, not a button — it
    /// never gets a hover glow, unlike the exit-button icon right next to it; hovering it
    /// (<paramref name="isFoodRationsHovered"/>, see <see cref="FoodRationsLocalRect"/>)
    /// only shows an explanatory tooltip below the toolbar.
    /// </summary>
    public static void Draw(
        SKCanvas canvas, float panelLeft, float panelTop, string? stationName, bool isStationHub,
        bool isHovered = false, string? windowName = null, bool isExitButtonHovered = false,
        long foodRationsCount = 0, bool isFoodRationsHovered = false)
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

        if (!string.IsNullOrEmpty(windowName))
        {
            if (!string.IsNullOrEmpty(stationName))
            {
                x += NameSegmentGap;
                canvas.DrawText(SeparatorText, x, baselineY, SeparatorPaint);
                x += SeparatorPaint.MeasureText(SeparatorText) + NameSegmentGap;
            }

            canvas.DrawText(windowName, x, baselineY, NamePaintActive);
        }

        if (FoodRationsImage is not null)
        {
            string valueText = foodRationsCount.ToString();
            float actualTextWidth = ResourceValuePaint.MeasureText(valueText);

            var block = FoodRationsLocalRect();
            float valueFieldRight = block.Right;
            // Right-align the actual digits within the reserved 4-digit-wide field — the
            // icon's position (fixed by FoodRationsLocalRect/ResourceValueFieldWidth) never
            // moves regardless of how many digits the current count actually has.
            float textLeft = valueFieldRight - actualTextWidth;

            var iconRect = new SKRect(
                panelLeft + block.Left, panelTop + block.Top,
                panelLeft + block.Left + ResourceIconSize, panelTop + block.Bottom);
            canvas.DrawBitmap(FoodRationsImage, iconRect);

            float textBaselineY = MenuStyle.VerticalCenterBaseline(
                new SKRect(0, panelTop, 0, panelTop + Height), ResourceValuePaint);
            canvas.DrawText(valueText, panelLeft + textLeft, textBaselineY, ResourceValuePaint);

            if (isFoodRationsHovered)
            {
                var screenBlock = new SKRect(
                    panelLeft + block.Left, panelTop + block.Top,
                    panelLeft + block.Right, panelTop + block.Bottom);
                DrawTooltip(canvas, screenBlock, FoodRationsTooltipLines);
            }
        }

        if (ExitButtonImage is not null)
        {
            var local = ExitButtonLocalRect();
            var exitRect = new SKRect(
                panelLeft + local.Left, panelTop + local.Top, panelLeft + local.Right, panelTop + local.Bottom);

            if (isExitButtonHovered)
                canvas.DrawOval(exitRect, ExitButtonGlowPaint);

            canvas.DrawBitmap(ExitButtonImage, exitRect);
        }
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

    /// <summary>
    /// Sums the player ship's total quantity of one cargo item type across every
    /// installed module's Cargo (e.g. a container module) — 0 for a null snapshot, no
    /// installed modules, or no matching stacks. The single source of truth for the
    /// toolbar's resource readouts (see <see cref="ResolveFoodRationsCount"/>).
    /// </summary>
    public static long ResolveCargoQuantity(AuthoritativeSnapshot? snapshot, string itemTypeId)
    {
        if (snapshot is null || snapshot.InstalledModules.IsDefaultOrEmpty)
            return 0;

        long total = 0;
        foreach (var module in snapshot.InstalledModules)
        {
            if (module.Cargo.IsDefaultOrEmpty)
                continue;

            foreach (var stack in module.Cargo)
                if (stack.ItemTypeId == itemTypeId)
                    total += stack.Quantity;
        }

        return total;
    }

    /// <summary>Total <see cref="FoodRationsItemTypeId"/> quantity aboard the player ship.</summary>
    public static long ResolveFoodRationsCount(AuthoritativeSnapshot? snapshot) =>
        ResolveCargoQuantity(snapshot, FoodRationsItemTypeId);

    /// <summary>
    /// Draws a small dark tooltip box directly below <paramref name="anchorRect"/> (screen
    /// space), one line per string in <paramref name="lines"/>, sized to the widest line.
    /// </summary>
    private static void DrawTooltip(SKCanvas canvas, SKRect anchorRect, string[] lines)
    {
        float maxLineWidth = 0f;
        foreach (var line in lines)
            maxLineWidth = System.Math.Max(maxLineWidth, TooltipTextPaint.MeasureText(line));

        float boxWidth = maxLineWidth + TooltipPaddingX * 2f;
        float boxHeight = lines.Length * TooltipLineHeight + TooltipPaddingY * 2f;

        // Right edge of the box lines up with the anchor's right edge so it stays fully
        // on-screen even when the anchor sits near the panel's own right edge.
        float boxRight = anchorRect.Right;
        float boxTop = anchorRect.Bottom + TooltipGapBelowToolbar;
        var box = new SKRect(boxRight - boxWidth, boxTop, boxRight, boxTop + boxHeight);

        canvas.DrawRoundRect(box, TooltipCornerRadius, TooltipCornerRadius, TooltipBackgroundPaint);
        canvas.DrawRoundRect(box, TooltipCornerRadius, TooltipCornerRadius, TooltipBorderPaint);

        float lineY = box.Top + TooltipPaddingY - TooltipTextPaint.FontMetrics.Ascent;
        foreach (var line in lines)
        {
            canvas.DrawText(line, box.Left + TooltipPaddingX, lineY, TooltipTextPaint);
            lineY += TooltipLineHeight;
        }
    }
}
