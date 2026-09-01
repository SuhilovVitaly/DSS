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
/// hover glow — hovering it only shows an explanatory tooltip below the toolbar, drawn
/// by <see cref="DrawTooltips"/> as the consuming screen's last draw call so it stays
/// on top of the screen's own buttons and panels.
///
/// Immediately to the left of the food-rations readout, a crew/cabins icon + count (see
/// <see cref="ResolveCrewCount"/>/<see cref="ResolveCabinsCount"/>) in "N / N" form —
/// crew and passengers aboard the ship, out of total cabin capacity. Same plain-readout
/// rule as the food-rations icon: never clickable, no hover glow, hover-only tooltip
/// (also drawn by <see cref="DrawTooltips"/>).
///
/// Immediately to the left of the crew readout, a tokens icon + credits balance (see
/// <see cref="ResolveCreditsCount"/>): the player's money, same plain-readout rule as
/// the other two — never clickable, no hover glow, hover-only tooltip (also drawn by
/// <see cref="DrawTooltips"/>).
///
/// Immediately to the left of the tokens readout, a fuel icon + "current / capacity"
/// count (see <see cref="ResolveFuelAmountKg"/>/<see cref="ResolveFuelCapacityKg"/>):
/// the fuel stored in the ship's engine tanks, out of total tank capacity. Same
/// plain-readout rule — never clickable, no hover glow, hover-only tooltip (also drawn
/// by <see cref="DrawTooltips"/>).
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
    /// Standard gap between adjacent blocks of the toolbar's right-side info panel
    /// (tokens / crew / rations readouts). The exit button is not an info block and
    /// keeps its own wider <see cref="ResourceInfoGapFromExitButton"/>.
    /// </summary>
    public const float InfoBlockGap = 44f;

    /// <summary>
    /// The reserved value-field width is measured from this 4-digit sample, so the field
    /// never has to grow/shrink (and the icon never has to shift) as the actual count
    /// crosses a digit-count boundary — up to 9999 always fits without re-layout.
    /// </summary>
    private const string ResourceValueFieldSample = "9999";

    /// <summary>
    /// The crew/cabins value field is reserved from this sample instead of
    /// <see cref="ResourceValueFieldSample"/> — realistically both sides of the "N / N"
    /// count stay single-digit, so a 4-digit reservation would leave an oversized gap;
    /// "9 / 9" is wide enough for a single digit on each side without clipping.
    /// </summary>
    private const string CrewValueFieldSample = "9 / 9";

    /// <summary>
    /// The tokens/credits value field is reserved from this sample — unlike the other
    /// readouts a credits balance routinely crosses into five and six digits (a single
    /// station's trade budget alone is 10k–50k, see Mechanics/Money.md), so "9999" would
    /// overflow the reserved field after the first profitable trade.
    /// </summary>
    private const string CreditsValueFieldSample = "999999";

    /// <summary>
    /// The fuel value field is reserved from this sample — like
    /// <see cref="CrewValueFieldSample"/> it is a "N / N" pair (current fuel / tank
    /// capacity), but each side reserves four digits ("9999") like
    /// <see cref="ResourceValueFieldSample"/> rather than one, since the tank capacity
    /// alone is four digits (1000 kg).
    /// </summary>
    private const string FuelValueFieldSample = "9999 / 9999";

    public const string FoodRationsItemTypeId = "item.food-rations";

    private static readonly string[] FoodRationsTooltipLines =
    {
        "Food rations stored aboard the ship.",
        "Each crew member or passenger consumes 2 rations per day."
    };

    private static readonly string[] CrewTooltipLines =
    {
        "Crew and passengers aboard the ship, out of total cabin capacity.",
        "More crew than cabins can hold will hurt morale."
    };

    private static readonly string[] TokensTooltipLines =
    {
        "Tokens held by the player.",
        "Earned from selling goods; spent on goods and refueling."
    };

    private static readonly string[] FuelTooltipLines =
    {
        "Fuel stored in the ship's engine tanks, out of total tank capacity.",
        "Refuel at a station - engine commands don't consume fuel yet."
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

    private static readonly SKBitmap? CrewImage =
        LoadImage("Images/UI/Panels/station-toolbar/toolbar-crew.png");

    /// <summary>True if the crew PNG was found and decoded at startup.</summary>
    internal static bool HasLoadedCrewImage => CrewImage is not null;

    private static readonly SKBitmap? TokensImage =
        LoadImage("Images/UI/Panels/station-toolbar/toolbar-tokens.png");

    /// <summary>True if the tokens PNG was found and decoded at startup.</summary>
    internal static bool HasLoadedTokensImage => TokensImage is not null;

    private static readonly SKBitmap? FuelImage =
        LoadImage("Images/UI/Panels/station-toolbar/toolbar-oil.png");

    /// <summary>True if the fuel PNG was found and decoded at startup.</summary>
    internal static bool HasLoadedFuelImage => FuelImage is not null;

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

    /// <summary>Reserved width for the crew/cabins value field — see <see cref="CrewValueFieldSample"/>.</summary>
    private static readonly float CrewValueFieldWidth = ResourceValuePaint.MeasureText(CrewValueFieldSample);

    /// <summary>Reserved width for the tokens/credits value field — see <see cref="CreditsValueFieldSample"/>.</summary>
    private static readonly float CreditsValueFieldWidth = ResourceValuePaint.MeasureText(CreditsValueFieldSample);

    /// <summary>Reserved width for the fuel value field — see <see cref="FuelValueFieldSample"/>.</summary>
    private static readonly float FuelValueFieldWidth = ResourceValuePaint.MeasureText(FuelValueFieldSample);

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
    /// Crew/cabins icon+value hit-test rect, local to the panel — sits immediately to the
    /// left of <see cref="FoodRationsLocalRect"/> (see <see cref="InfoBlockGap"/>),
    /// spanning from the icon's left edge to the reserved value field's right edge (see
    /// <see cref="CrewValueFieldWidth"/>). Used only to show the tooltip on hover — this
    /// readout is never clickable and never gets a hover glow.
    /// </summary>
    public static SKRect CrewLocalRect()
    {
        float blockRight = FoodRationsLocalRect().Left - InfoBlockGap;
        float valueFieldLeft = blockRight - CrewValueFieldWidth;
        float iconLeft = valueFieldLeft - ResourceIconTextGap - ResourceIconSize;
        float top = (Height - ResourceIconSize) / 2f;
        return new SKRect(iconLeft, top, blockRight, top + ResourceIconSize);
    }

    /// <summary>
    /// Tokens/credits icon+value hit-test rect, local to the panel — sits immediately to
    /// the left of <see cref="CrewLocalRect"/> (see <see cref="InfoBlockGap"/>), spanning
    /// from the icon's left edge to the reserved value field's right edge (see
    /// <see cref="CreditsValueFieldWidth"/>). Used only to show the tooltip on hover —
    /// this readout is never clickable and never gets a hover glow.
    /// </summary>
    public static SKRect TokensLocalRect()
    {
        float blockRight = CrewLocalRect().Left - InfoBlockGap;
        float valueFieldLeft = blockRight - CreditsValueFieldWidth;
        float iconLeft = valueFieldLeft - ResourceIconTextGap - ResourceIconSize;
        float top = (Height - ResourceIconSize) / 2f;
        return new SKRect(iconLeft, top, blockRight, top + ResourceIconSize);
    }

    /// <summary>
    /// Fuel icon+value hit-test rect, local to the panel — sits immediately to the left
    /// of <see cref="TokensLocalRect"/> (see <see cref="InfoBlockGap"/>), spanning from
    /// the icon's left edge to the reserved value field's right edge (see
    /// <see cref="FuelValueFieldWidth"/>). Used only to show the tooltip on hover — this
    /// readout is never clickable and never gets a hover glow.
    /// </summary>
    public static SKRect FuelLocalRect()
    {
        float blockRight = TokensLocalRect().Left - InfoBlockGap;
        float valueFieldLeft = blockRight - FuelValueFieldWidth;
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
    /// never gets a hover glow, unlike the exit-button icon right next to it; its hover
    /// tooltip (see <see cref="FoodRationsLocalRect"/>) is drawn by
    /// <see cref="DrawTooltips"/>, the screen's last draw call.
    ///
    /// <paramref name="crewCount"/>/<paramref name="cabinsCount"/> feed an identical
    /// readout immediately to the left of the food-rations one (see
    /// <see cref="InfoBlockGap"/>): the crew icon followed by
    /// "<paramref name="crewCount"/> / <paramref name="cabinsCount"/>". Same plain-readout
    /// rule — no hover glow; its hover tooltip (see <see cref="CrewLocalRect"/>) is also
    /// drawn by <see cref="DrawTooltips"/>.
    ///
    /// <paramref name="creditsCount"/> feeds the tokens readout, immediately left of the
    /// crew one (see <see cref="InfoBlockGap"/>): the tokens icon followed by the
    /// player's credits balance in a field reserved wide enough for six digits (999999).
    /// Same plain-readout rule — no hover glow; its hover tooltip (see
    /// <see cref="TokensLocalRect"/>) is also drawn by <see cref="DrawTooltips"/>.
    ///
    /// <paramref name="fuelAmountKg"/>/<paramref name="fuelCapacityKg"/> feed the
    /// leftmost readout, immediately left of the tokens one (see
    /// <see cref="InfoBlockGap"/>): the fuel icon followed by
    /// "<paramref name="fuelAmountKg"/> / <paramref name="fuelCapacityKg"/>" — current
    /// fuel out of total tank capacity, in a field reserved for four digits per side.
    /// Same plain-readout rule — no hover glow; its hover tooltip (see
    /// <see cref="FuelLocalRect"/>) is also drawn by <see cref="DrawTooltips"/>.
    /// </summary>
    public static void Draw(
        SKCanvas canvas, float panelLeft, float panelTop, string? stationName, bool isStationHub,
        bool isHovered = false, string? windowName = null, bool isExitButtonHovered = false,
        long foodRationsCount = 0, int crewCount = 0, int cabinsCount = 0, long creditsCount = 0,
        long fuelAmountKg = 0, long fuelCapacityKg = 0)
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
            var block = FoodRationsLocalRect();

            var iconRect = new SKRect(
                panelLeft + block.Left, panelTop + block.Top,
                panelLeft + block.Left + ResourceIconSize, panelTop + block.Bottom);
            canvas.DrawBitmap(FoodRationsImage, iconRect);

            // The value starts at a fixed ResourceIconTextGap right of the icon's visible
            // art (see IconTextLeft) — the icon-to-text gap is identical for every
            // readout regardless of the digit count. The reserved field
            // (FoodRationsLocalRect/ResourceValueFieldWidth) only keeps the icon from
            // shifting and the value inside the field.
            float textLeft = IconTextLeft(panelLeft, block, FoodRationsImage);

            float textBaselineY = MenuStyle.VerticalCenterBaseline(
                new SKRect(0, panelTop, 0, panelTop + Height), ResourceValuePaint);
            canvas.DrawText(foodRationsCount.ToString(), textLeft, textBaselineY, ResourceValuePaint);
        }

        if (CrewImage is not null)
        {
            var block = CrewLocalRect();

            var iconRect = new SKRect(
                panelLeft + block.Left, panelTop + block.Top,
                panelLeft + block.Left + ResourceIconSize, panelTop + block.Bottom);
            canvas.DrawBitmap(CrewImage, iconRect);

            // Same fixed icon-to-text gap as the food-rations readout — the reserved
            // field (CrewLocalRect/CrewValueFieldWidth) only keeps the icon from shifting.
            float textLeft = IconTextLeft(panelLeft, block, CrewImage);

            float textBaselineY = MenuStyle.VerticalCenterBaseline(
                new SKRect(0, panelTop, 0, panelTop + Height), ResourceValuePaint);
            canvas.DrawText($"{crewCount} / {cabinsCount}", textLeft, textBaselineY, ResourceValuePaint);
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

        if (TokensImage is not null)
        {
            var block = TokensLocalRect();

            var iconRect = new SKRect(
                panelLeft + block.Left, panelTop + block.Top,
                panelLeft + block.Left + ResourceIconSize, panelTop + block.Bottom);
            canvas.DrawBitmap(TokensImage, iconRect);

            // Same fixed icon-to-text gap as the other readouts — the reserved field
            // (TokensLocalRect/CreditsValueFieldWidth) only keeps the icon from shifting.
            float textLeft = IconTextLeft(panelLeft, block, TokensImage);

            float textBaselineY = MenuStyle.VerticalCenterBaseline(
                new SKRect(0, panelTop, 0, panelTop + Height), ResourceValuePaint);
            canvas.DrawText(creditsCount.ToString(), textLeft, textBaselineY, ResourceValuePaint);
        }

        if (FuelImage is not null)
        {
            var block = FuelLocalRect();

            var iconRect = new SKRect(
                panelLeft + block.Left, panelTop + block.Top,
                panelLeft + block.Left + ResourceIconSize, panelTop + block.Bottom);
            canvas.DrawBitmap(FuelImage, iconRect);

            // Same fixed icon-to-text gap as the other readouts — the reserved field
            // (FuelLocalRect/FuelValueFieldWidth) only keeps the icon from shifting.
            float textLeft = IconTextLeft(panelLeft, block, FuelImage);

            float textBaselineY = MenuStyle.VerticalCenterBaseline(
                new SKRect(0, panelTop, 0, panelTop + Height), ResourceValuePaint);
            canvas.DrawText($"{fuelAmountKg} / {fuelCapacityKg}", textLeft, textBaselineY, ResourceValuePaint);
        }
    }

    /// <summary>
    /// Draws the readout tooltips anchored below their toolbar icons. Must be the
    /// consuming screen's LAST draw call — the tooltip box hangs below the toolbar into
    /// the body area, so drawing it anywhere earlier lets the screen's own buttons and
    /// panels paint over it (the toolbar itself is drawn first, right after the panel
    /// background). Same hover-gating as before the split out of <see cref="Draw"/>: a
    /// tooltip only appears while its readout's hover delay has elapsed (the screen
    /// passes its visible-flag here) and its icon image is loaded.
    /// </summary>
    public static void DrawTooltips(
        SKCanvas canvas, float panelLeft, float panelTop,
        bool isFoodRationsHovered, bool isCrewHovered, bool isTokensHovered, bool isFuelHovered)
    {
        if (isFoodRationsHovered && FoodRationsImage is not null)
        {
            var block = FoodRationsLocalRect();
            DrawTooltip(canvas, ToScreenRect(panelLeft, panelTop, block), FoodRationsTooltipLines);
        }

        if (isCrewHovered && CrewImage is not null)
        {
            var block = CrewLocalRect();
            DrawTooltip(canvas, ToScreenRect(panelLeft, panelTop, block), CrewTooltipLines);
        }

        if (isTokensHovered && TokensImage is not null)
        {
            var block = TokensLocalRect();
            DrawTooltip(canvas, ToScreenRect(panelLeft, panelTop, block), TokensTooltipLines);
        }

        if (isFuelHovered && FuelImage is not null)
        {
            var block = FuelLocalRect();
            DrawTooltip(canvas, ToScreenRect(panelLeft, panelTop, block), FuelTooltipLines);
        }
    }

    private static SKRect ToScreenRect(float panelLeft, float panelTop, SKRect local) =>
        new(panelLeft + local.Left, panelTop + local.Top, panelLeft + local.Right, panelTop + local.Bottom);

    /// <summary>
    /// Screen-space X where a readout's value text starts: a fixed
    /// <see cref="ResourceIconTextGap"/> right of the icon's *visible art* — not of the
    /// icon box — because the source PNGs have differing transparent composition margins
    /// on their right edge (crew/tokens touch the canvas edge, rations/oil do not). Without
    /// this compensation the visible icon-to-text gap would differ per readout.
    /// </summary>
    private static float IconTextLeft(float panelLeft, SKRect block, SKBitmap icon) =>
        panelLeft + block.Left + ResourceIconSize + ResourceIconTextGap
        - IconRightPaddingPx(icon) * (ResourceIconSize / icon.Width);

    /// <summary>
    /// Transparent padding on the right edge of an icon's source PNG, in source pixels —
    /// the number of fully-transparent pixel columns from the canvas edge to the first
    /// column with any visible art (alpha &gt; 16; the threshold skips antialiased fringes).
    /// </summary>
    private static float IconRightPaddingPx(SKBitmap icon)
    {
        for (int x = icon.Width - 1; x >= 0; x--)
        {
            for (int y = 0; y < icon.Height; y++)
                if (icon.GetPixel(x, y).Alpha > 16)
                    return icon.Width - 1 - x;
        }

        return icon.Width; // fully transparent icon — never happens for shipped art
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
    /// Number of crew members (and passengers) aboard the player ship — 0 for a null
    /// snapshot or any snapshot/save predating this field (story-20260901-112254).
    /// </summary>
    public static int ResolveCrewCount(AuthoritativeSnapshot? snapshot) => snapshot?.PlayerCrewCount ?? 0;

    /// <summary>
    /// Sums cabin capacity (<see cref="InstalledModuleSnapshot.CabinesCount"/>) across every
    /// installed module — 0 for a null snapshot, no installed modules, or no module type
    /// that houses crew. Mirrors <see cref="ResolveCargoQuantity"/>'s null-safe iteration.
    /// </summary>
    public static int ResolveCabinsCount(AuthoritativeSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.InstalledModules.IsDefaultOrEmpty)
            return 0;

        int total = 0;
        foreach (var module in snapshot.InstalledModules)
            total += module.CabinesCount ?? 0;

        return total;
    }

    /// <summary>The player's credits balance — 0 for a null snapshot (or a snapshot/save predating the field).</summary>
    public static long ResolveCreditsCount(AuthoritativeSnapshot? snapshot) => snapshot?.PlayerCredits ?? 0;

    /// <summary>
    /// Total fuel (kg) aboard the player ship, summed across every installed module that
    /// carries fuel (engine modules project FuelAmountKg) — 0 for a null snapshot, no
    /// installed modules, or no fuel-carrying module. Mirrors
    /// <see cref="ResolveCabinsCount"/>'s null-safe iteration.
    /// </summary>
    public static long ResolveFuelAmountKg(AuthoritativeSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.InstalledModules.IsDefaultOrEmpty)
            return 0;

        long total = 0;
        foreach (var module in snapshot.InstalledModules)
            total += module.FuelAmountKg ?? 0;

        return total;
    }

    /// <summary>
    /// Total fuel tank capacity (kg) aboard the player ship — the sum of every installed
    /// module's FuelCapacityKg, mirroring <see cref="ResolveFuelAmountKg"/>.
    /// </summary>
    public static long ResolveFuelCapacityKg(AuthoritativeSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.InstalledModules.IsDefaultOrEmpty)
            return 0;

        long total = 0;
        foreach (var module in snapshot.InstalledModules)
            total += module.FuelCapacityKg ?? 0;

        return total;
    }

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
