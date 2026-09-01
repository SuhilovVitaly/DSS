using System.Collections.Generic;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

/// <summary>
/// Header bar + zebra-striped row grid + scrollbar control (reference mockup screenshot),
/// extracted out of <c>Screens.Trade.TradeScreen</c> ahead of the real Trade redesign
/// layout (Docs/FirstRelease/Screens/Trade.md). Pure geometry/drawing, no state of its
/// own — the owning screen tracks arrow-hover and scroll-position state and passes it in,
/// the same pattern <see cref="StationToolbar"/> uses for its own hover flags.
///
/// <c>rowCount</c> below or at <see cref="ScrollbarInactiveMaxRowCount"/> means everything
/// fits without scrolling: only that many rows are drawn (no blank filler rows up to
/// <see cref="MaxVisibleRows"/>) and the scrollbar renders in its inactive/dark-gray state
/// — see <see cref="IsScrollbarActive"/>. The scrollbar track's height always matches the
/// drawn rows' height, so it never floats detached from a shorter grid. An empty grid
/// (<c>rowCount == 0</c>) is a distinct case: it still draws <see cref="MaxVisibleRows"/>
/// placeholder row slots so the grid's shape stays visible, but uniformly dark gray
/// instead of the zebra fill — matching the scrollbar's inactive color for one consistent
/// "nothing here yet" look.
/// </summary>
public static class GridPanel
{
    public const int MaxVisibleRows = 5;
    private const int ScrollbarInactiveMaxRowCount = 4;

    // ── Header ──────────────────────────────────────────────────────────────────
    public const float HeaderWidth = 925f;
    public const float HeaderHeight = 30f;
    private const float HeaderCornerRadius = 12f;
    private const float TitlePadding = 10f;
    private const float TitleBaselineShift = 7f;

    // ── Rows, relative to the header's top-left ────────────────────────────────
    private const float RowOffsetX = 25f;
    private const float RowOffsetY = 44f;
    public const float RowWidth = 900f;
    public const float RowHeight = 30f;
    private const float RowLabelPaddingX = 20f;

    // ── Trailing Selling price / Selling count columns, right edge of the row ──
    private const string PriceColumnHeader = "Selling price";
    private const string CountColumnHeader = "Selling count";
    private const float PriceColumnWidth = 140f;
    private const float CountColumnWidth = 140f;
    private const float PriceColumnLeftOffset = RowWidth - PriceColumnWidth - CountColumnWidth;
    private const float CountColumnLeftOffset = RowWidth - CountColumnWidth;

    // ── Scrollbar, relative to the header's top-left ───────────────────────────
    private const float ScrollbarOffsetX = 935f;
    public const float ScrollbarWidth = 22f;
    private const float ScrollbarArrowSize = 22f;
    private const float ScrollbarThumbInset = 3f;
    private const float ScrollbarThumbCornerRadius = 6f;
    private const float ScrollbarThumbHeightRatio = 0.35f;

    /// <summary>Shared dark-gray tone for every "inactive/empty" element — inactive scrollbar arrows/thumb and empty-grid placeholder rows.</summary>
    private static readonly SKColor InactiveGray = new(0x4A, 0x4A, 0x4A);

    private static readonly SKPaint _headerPaint = new()
    {
        Color = new SKColor(0x5E, 0x5E, 0x5E), Style = SKPaintStyle.Fill, IsAntialias = true
    };

    private static readonly SKPaint _titlePaint = new()
    {
        Color = SKColors.White, TextSize = 18f, IsAntialias = true,
        TextAlign = SKTextAlign.Left, Typeface = MenuStyle.TypefaceHumaroid
    };

    private static readonly SKPaint _rowFillLightPaint = new()
    {
        Color = new SKColor(0xCC, 0xCC, 0xCC), Style = SKPaintStyle.Fill, IsAntialias = true
    };

    private static readonly SKPaint _rowFillDarkPaint = new()
    {
        Color = new SKColor(0xB3, 0xB3, 0xB3), Style = SKPaintStyle.Fill, IsAntialias = true
    };

    private static readonly SKPaint _rowBorderPaint = new()
    {
        Color = new SKColor(0x5F, 0x5F, 0x5F), Style = SKPaintStyle.Stroke, StrokeWidth = 1f, IsAntialias = true
    };

    /// <summary>Placeholder row fill for an empty grid (<c>rowCount == 0</c>) — same dark-gray tone as the inactive scrollbar.</summary>
    private static readonly SKPaint _emptyRowFillPaint = new()
    {
        Color = InactiveGray, Style = SKPaintStyle.Fill, IsAntialias = true
    };

    private static readonly SKPaint _rowLabelPaint = new()
    {
        Color = SKColors.Black, TextSize = 14f, IsAntialias = true,
        TextAlign = SKTextAlign.Left, Typeface = MenuStyle.TypefaceRegular
    };

    /// <summary>Selling price/count value, centered under its column header — same size/typeface as <see cref="_rowLabelPaint"/>, just center-aligned.</summary>
    private static readonly SKPaint _rowValuePaint = new()
    {
        Color = SKColors.Black, TextSize = 14f, IsAntialias = true,
        TextAlign = SKTextAlign.Center, Typeface = MenuStyle.TypefaceRegular
    };

    /// <summary>Selling price/count column header, drawn on the gray header bar next to the title.</summary>
    private static readonly SKPaint _columnHeaderPaint = new()
    {
        Color = SKColors.White, TextSize = 13f, IsAntialias = true,
        TextAlign = SKTextAlign.Center, Typeface = MenuStyle.TypefaceRegular
    };

    private static readonly SKPaint _scrollbarTrackPaint = new()
    {
        Color = new SKColor(0x2B, 0x2B, 0x2B), Style = SKPaintStyle.Fill, IsAntialias = true
    };

    private static readonly SKPaint _scrollbarBorderPaint = new()
    {
        Color = new SKColor(0x5F, 0x5F, 0x5F), Style = SKPaintStyle.Stroke, StrokeWidth = 1f, IsAntialias = true
    };

    private static readonly SKPaint _scrollbarThumbActivePaint = new()
    {
        Color = SKColors.White, Style = SKPaintStyle.Fill, IsAntialias = true
    };

    private static readonly SKPaint _scrollbarThumbInactivePaint = new()
    {
        Color = InactiveGray, Style = SKPaintStyle.Fill, IsAntialias = true
    };

    private static readonly SKPaint _scrollbarArrowActivePaint = new()
    {
        Color = SKColors.White, Style = SKPaintStyle.Fill, IsAntialias = true
    };

    private static readonly SKPaint _scrollbarArrowInactivePaint = new()
    {
        Color = InactiveGray, Style = SKPaintStyle.Fill, IsAntialias = true
    };

    /// <summary>Hover highlight behind an arrow button — same fill as MenuStyle's button hover state. Only ever drawn when the scrollbar is active.</summary>
    private static readonly SKPaint _scrollbarArrowHoverPaint = new()
    {
        Color = MenuStyle.ButtonFillHover.Color, Style = SKPaintStyle.Fill, IsAntialias = true
    };

    /// <summary>
    /// True when there's more data than fits in <see cref="MaxVisibleRows"/> without
    /// scrolling — the scrollbar is only interactive/white in that case; at
    /// <see cref="ScrollbarInactiveMaxRowCount"/> rows or fewer it renders gray and ignores
    /// hover/click (callers should skip their own hit-testing when this is false).
    /// </summary>
    public static bool IsScrollbarActive(int rowCount) => rowCount > ScrollbarInactiveMaxRowCount;

    /// <summary>
    /// Highest valid <c>scrollOffset</c> for <paramref name="rowCount"/> — 0 while
    /// everything fits in <see cref="MaxVisibleRows"/>, otherwise the number of rows
    /// hidden past the visible window (<paramref name="rowCount"/> - <see cref="MaxVisibleRows"/>).
    /// Callers should clamp their scroll-offset state to this after every arrow click and
    /// whenever <paramref name="rowCount"/> itself can change (e.g. live data).
    /// </summary>
    public static int MaxScrollOffset(int rowCount) => Math.Max(0, rowCount - MaxVisibleRows);

    /// <summary>
    /// Rows actually drawn: <paramref name="rowCount"/> capped at <see cref="MaxVisibleRows"/>,
    /// except an empty grid (<c>rowCount == 0</c>) which still draws a full
    /// <see cref="MaxVisibleRows"/> worth of dark-gray placeholder slots — see the type doc
    /// comment.
    /// </summary>
    private static int DrawnRowCount(int rowCount) => rowCount == 0 ? MaxVisibleRows : Math.Min(rowCount, MaxVisibleRows);

    public static SKRect HeaderLocalRect(float originX, float originY) =>
        new(originX, originY, originX + HeaderWidth, originY + HeaderHeight);

    public static SKRect RowLocalRect(float originX, float originY, int rowIndex)
    {
        float top = originY + RowOffsetY + rowIndex * RowHeight;
        return new SKRect(originX + RowOffsetX, top, originX + RowOffsetX + RowWidth, top + RowHeight);
    }

    /// <summary>Horizontal center of the Selling price column — shared by its header label and every row's value, so they always line up.</summary>
    private static float PriceColumnCenterX(float originX) =>
        originX + RowOffsetX + PriceColumnLeftOffset + PriceColumnWidth / 2f;

    /// <summary>Horizontal center of the Selling count column, at the row's trailing edge — see <see cref="PriceColumnCenterX"/>.</summary>
    private static float CountColumnCenterX(float originX) =>
        originX + RowOffsetX + CountColumnLeftOffset + CountColumnWidth / 2f;

    /// <summary>Track height matches the currently drawn rows — <see cref="DrawnRowCount"/> of <paramref name="rowCount"/>, not the fixed <see cref="MaxVisibleRows"/> capacity.</summary>
    public static SKRect ScrollbarTrackLocalRect(float originX, float originY, int rowCount)
    {
        float top = originY + RowOffsetY;
        float height = DrawnRowCount(rowCount) * RowHeight;
        return new SKRect(originX + ScrollbarOffsetX, top, originX + ScrollbarOffsetX + ScrollbarWidth, top + height);
    }

    public static SKRect ScrollUpArrowLocalRect(float originX, float originY, int rowCount)
    {
        var track = ScrollbarTrackLocalRect(originX, originY, rowCount);
        return new SKRect(track.Left, track.Top, track.Right, track.Top + ScrollbarArrowSize);
    }

    public static SKRect ScrollDownArrowLocalRect(float originX, float originY, int rowCount)
    {
        var track = ScrollbarTrackLocalRect(originX, originY, rowCount);
        return new SKRect(track.Left, track.Bottom - ScrollbarArrowSize, track.Right, track.Bottom);
    }

    /// <summary>The thumb's usable vertical travel, between the two arrow buttons.</summary>
    private readonly record struct ThumbTravel(float MinTop, float Travel, float Height);

    private static ThumbTravel ComputeThumbTravel(SKRect track)
    {
        float middleTop = track.Top + ScrollbarArrowSize;
        float middleHeight = Math.Max(0f, track.Height - 2 * ScrollbarArrowSize);
        float thumbHeight = middleHeight * ScrollbarThumbHeightRatio;
        float travel = Math.Max(0f, middleHeight - thumbHeight - 2 * ScrollbarThumbInset);
        return new ThumbTravel(middleTop + ScrollbarThumbInset, travel, thumbHeight);
    }

    /// <summary>Current thumb position/size — the target for both drawing and hit-testing a drag start.</summary>
    public static SKRect ScrollThumbLocalRect(float originX, float originY, int rowCount, int scrollOffset)
    {
        var track = ScrollbarTrackLocalRect(originX, originY, rowCount);
        var thumbTravel = ComputeThumbTravel(track);

        int maxOffset = MaxScrollOffset(rowCount);
        float progress = maxOffset > 0 ? (float)Math.Clamp(scrollOffset, 0, maxOffset) / maxOffset : 0f;
        float thumbTop = thumbTravel.MinTop + thumbTravel.Travel * progress;
        return new SKRect(track.Left + ScrollbarThumbInset, thumbTop, track.Right - ScrollbarThumbInset, thumbTop + thumbTravel.Height);
    }

    /// <summary>
    /// Inverse of <see cref="ScrollThumbLocalRect"/>'s vertical mapping — the scroll offset
    /// whose thumb top would land at <paramref name="desiredThumbTopY"/> (local Y,
    /// clamped to the track's travel range). For a drag: the caller keeps the pointer's
    /// offset from the thumb's top fixed (grabbed-at-a-point-, not "thumb snaps to
    /// pointer"), so it should pass <c>pointerLocalY - grabOffsetY</c> here every move.
    /// </summary>
    public static int ResolveScrollOffsetForThumbTop(float originX, float originY, int rowCount, float desiredThumbTopY)
    {
        int maxOffset = MaxScrollOffset(rowCount);
        if (maxOffset <= 0)
            return 0;

        var track = ScrollbarTrackLocalRect(originX, originY, rowCount);
        var thumbTravel = ComputeThumbTravel(track);
        if (thumbTravel.Travel <= 0f)
            return 0;

        float clampedTop = Math.Clamp(desiredThumbTopY, thumbTravel.MinTop, thumbTravel.MinTop + thumbTravel.Travel);
        float progress = (clampedTop - thumbTravel.MinTop) / thumbTravel.Travel;
        return (int)Math.Round(progress * maxOffset);
    }

    /// <param name="scrollOffset">
    /// Index of the first row currently shown, 0..<see cref="MaxScrollOffset"/> of
    /// <paramref name="rowCount"/> — both which <paramref name="rowLabels"/> window is
    /// drawn and the thumb's travel come from this, so the two never disagree. Ignored
    /// (treated as 0) while the scrollbar is inactive.
    /// </param>
    /// <param name="rowLabels">
    /// Optional per-row text (e.g. item names), drawn in black with a <see cref="RowLabelPaddingX"/>
    /// left inset — row slot <c>i</c> shows <c>rowLabels[scrollOffset + i]</c>. Must have
    /// exactly <paramref name="rowCount"/> entries when provided; omit for a plain colored
    /// grid with no text.
    /// </param>
    /// <param name="priceValues">Optional per-row "Selling price" column text, same <c>scrollOffset + i</c> indexing as <paramref name="rowLabels"/>.</param>
    /// <param name="countValues">Optional per-row "Selling count" column text, same <c>scrollOffset + i</c> indexing as <paramref name="rowLabels"/>.</param>
    public static void Draw(
        SKCanvas canvas, float originX, float originY, string title, int rowCount,
        int scrollOffset, bool isScrollUpHovered, bool isScrollDownHovered,
        IReadOnlyList<string>? rowLabels = null,
        IReadOnlyList<string>? priceValues = null, IReadOnlyList<string>? countValues = null)
    {
        var header = HeaderLocalRect(originX, originY);
        canvas.DrawRoundRect(header, HeaderCornerRadius, HeaderCornerRadius, _headerPaint);
        canvas.DrawText(title, header.Left + TitlePadding, header.Top + TitlePadding + MenuStyle.ButtonFontSize - TitleBaselineShift, _titlePaint);

        float columnHeaderBaselineY = MenuStyle.VerticalCenterBaseline(header, _columnHeaderPaint);
        canvas.DrawText(PriceColumnHeader, PriceColumnCenterX(originX), columnHeaderBaselineY, _columnHeaderPaint);
        canvas.DrawText(CountColumnHeader, CountColumnCenterX(originX), columnHeaderBaselineY, _columnHeaderPaint);

        bool isEmpty = rowCount == 0;
        bool isActive = IsScrollbarActive(rowCount);
        int effectiveOffset = isActive ? Math.Clamp(scrollOffset, 0, MaxScrollOffset(rowCount)) : 0;
        int drawnRows = DrawnRowCount(rowCount);
        for (int i = 0; i < drawnRows; i++)
        {
            var rowRect = RowLocalRect(originX, originY, i);
            var fillPaint = isEmpty ? _emptyRowFillPaint : i % 2 == 0 ? _rowFillLightPaint : _rowFillDarkPaint;
            canvas.DrawRect(rowRect, fillPaint);
            canvas.DrawRect(rowRect, _rowBorderPaint);

            int labelIndex = effectiveOffset + i;
            float baselineY = MenuStyle.VerticalCenterBaseline(rowRect, _rowLabelPaint);

            if (rowLabels is not null && labelIndex < rowLabels.Count)
                canvas.DrawText(rowLabels[labelIndex], rowRect.Left + RowLabelPaddingX, baselineY, _rowLabelPaint);

            if (priceValues is not null && labelIndex < priceValues.Count)
                canvas.DrawText(priceValues[labelIndex], PriceColumnCenterX(originX), baselineY, _rowValuePaint);

            if (countValues is not null && labelIndex < countValues.Count)
                canvas.DrawText(countValues[labelIndex], CountColumnCenterX(originX), baselineY, _rowValuePaint);
        }

        DrawScrollbar(canvas, originX, originY, rowCount, effectiveOffset, isActive, isScrollUpHovered, isScrollDownHovered);
    }

    private static void DrawScrollbar(
        SKCanvas canvas, float originX, float originY, int rowCount,
        int scrollOffset, bool isActive, bool isScrollUpHovered, bool isScrollDownHovered)
    {
        var track = ScrollbarTrackLocalRect(originX, originY, rowCount);
        canvas.DrawRect(track, _scrollbarTrackPaint);
        canvas.DrawRect(track, _scrollbarBorderPaint);

        var arrowPaint = isActive ? _scrollbarArrowActivePaint : _scrollbarArrowInactivePaint;
        var thumbPaint = isActive ? _scrollbarThumbActivePaint : _scrollbarThumbInactivePaint;

        var upArrowRect = ScrollUpArrowLocalRect(originX, originY, rowCount);
        DrawScrollbarArrow(canvas, upArrowRect, pointingUp: true, isHovered: isActive && isScrollUpHovered, arrowPaint);

        var downArrowRect = ScrollDownArrowLocalRect(originX, originY, rowCount);
        DrawScrollbarArrow(canvas, downArrowRect, pointingUp: false, isHovered: isActive && isScrollDownHovered, arrowPaint);

        if (track.Height <= 2 * ScrollbarArrowSize)
            return;

        var thumbRect = ScrollThumbLocalRect(originX, originY, rowCount, isActive ? scrollOffset : 0);
        canvas.DrawRoundRect(thumbRect, ScrollbarThumbCornerRadius, ScrollbarThumbCornerRadius, thumbPaint);
    }

    private static void DrawScrollbarArrow(SKCanvas canvas, SKRect bounds, bool pointingUp, bool isHovered, SKPaint arrowPaint)
    {
        if (isHovered)
            canvas.DrawRect(bounds, _scrollbarArrowHoverPaint);

        float cx = bounds.MidX;
        float halfWidth = bounds.Width * 0.25f;
        float apex = pointingUp ? bounds.Top + bounds.Height * 0.3f : bounds.Bottom - bounds.Height * 0.3f;
        float baseY = pointingUp ? bounds.Bottom - bounds.Height * 0.3f : bounds.Top + bounds.Height * 0.3f;

        using var path = new SKPath();
        path.MoveTo(cx, apex);
        path.LineTo(cx - halfWidth, baseY);
        path.LineTo(cx + halfWidth, baseY);
        path.Close();
        canvas.DrawPath(path, arrowPaint);
    }
}
