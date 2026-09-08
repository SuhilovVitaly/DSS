using System.Collections.Generic;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

/// <summary>Which column title a click/hover landed on, or the grid is currently sorted by — see <see cref="GridPanel.HitTestColumnTitle"/>.</summary>
public enum GridSortColumn
{
    Name,
    SellingPrice,
    SellingCount,
    BuyingPrice,
    BuyingCount
}

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

    // ── Trailing Selling/Buying price+count columns, right edge of the row ─────
    // Left-to-right visual order; right-aligned to the row's trailing edge, each the
    // same width ("того же типа" — same size/style as the Selling columns).
    private static readonly string[] _trailingColumnHeaders =
        { "Selling price", "Selling count", "Buying price", "Buying count" };

    private const float TrailingColumnWidth = 140f;
    private const int TrailingColumnCount = 4;

    // ── Sort indicator (glow on the active title + direction arrow beside it) ──
    private const float SortArrowSize = 8f;
    private const float SortArrowGap = 6f;

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

    /// <summary>
    /// Sort-indicator glow drawn behind a column title — same technique as
    /// <see cref="StationToolbar"/>'s hovered station-name link (blurred duplicate of the
    /// text behind the sharp one, using its own hover-glow color/blur radius) so a sorted
    /// column reads as "active" the same way that name link does.
    /// </summary>
    private static readonly SKPaint _titleGlowPaint = new()
    {
        Color = StationToolbar.ColorNameGlow, TextSize = 18f, IsAntialias = true, FakeBoldText = true,
        TextAlign = SKTextAlign.Left, Typeface = MenuStyle.TypefaceHumaroid,
        MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, StationToolbar.NameHoverGlowSigma)
    };

    /// <summary>Same glow technique as <see cref="_titleGlowPaint"/>, sized/aligned for the centered trailing column headers instead of the left-aligned title.</summary>
    private static readonly SKPaint _columnHeaderGlowPaint = new()
    {
        Color = StationToolbar.ColorNameGlow, TextSize = 13f, IsAntialias = true, FakeBoldText = true,
        TextAlign = SKTextAlign.Center, Typeface = MenuStyle.TypefaceRegular,
        MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, StationToolbar.NameHoverGlowSigma)
    };

    /// <summary>Sort-direction triangle drawn beside the active sort column's title.</summary>
    private static readonly SKPaint _sortArrowPaint = new()
    {
        Color = SKColors.White, Style = SKPaintStyle.Fill, IsAntialias = true
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

    /// <summary>Fill for the selected row — replaces the zebra fill entirely (not blended over it).</summary>
    private static readonly SKPaint _rowFillSelectedPaint = new()
    {
        Color = new SKColor(0xFF, 0xC8, 0x80), Style = SKPaintStyle.Fill, IsAntialias = true
    };

    /// <summary>Left-edge indicator on the selected row — same accent as the old MVP TradeScreen's row selection (XenonStyle.OrangeAccent).</summary>
    private static readonly SKPaint _selectedRowIndicatorPaint = new()
    {
        Color = XenonStyle.OrangeAccent, Style = SKPaintStyle.Stroke, StrokeWidth = 3f, IsAntialias = true
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

    /// <summary>
    /// Absolute row index (accounting for <paramref name="scrollOffset"/>, same indexing
    /// as <c>rowLabels</c>) hit by a click at (<paramref name="localX"/>, <paramref name="localY"/>),
    /// or -1 if the click missed every drawn row or the grid is empty
    /// (<paramref name="rowCount"/> == 0 — its placeholder rows aren't selectable).
    /// </summary>
    public static int HitTestRow(float originX, float originY, int rowCount, int scrollOffset, float localX, float localY)
    {
        if (rowCount == 0)
            return -1;

        int effectiveOffset = IsScrollbarActive(rowCount) ? Math.Clamp(scrollOffset, 0, MaxScrollOffset(rowCount)) : 0;
        int drawnRows = DrawnRowCount(rowCount);
        for (int i = 0; i < drawnRows; i++)
        {
            var rowRect = RowLocalRect(originX, originY, i);
            if (localX >= rowRect.Left && localX <= rowRect.Right && localY >= rowRect.Top && localY <= rowRect.Bottom)
                return effectiveOffset + i;
        }

        return -1;
    }

    /// <summary>
    /// Horizontal center of trailing column <paramref name="columnIndex"/> (0 = leftmost
    /// of the four, i.e. Selling price; <see cref="TrailingColumnCount"/> - 1 = rightmost,
    /// Buying count, flush with the row's trailing edge) — shared by the header label and
    /// every row's value, so they always line up.
    /// </summary>
    private static float TrailingColumnCenterX(float originX, int columnIndex)
    {
        float leftOffset = RowWidth - (TrailingColumnCount - columnIndex) * TrailingColumnWidth;
        return originX + RowOffsetX + leftOffset + TrailingColumnWidth / 2f;
    }

    private static GridSortColumn TrailingSortColumn(int columnIndex) => columnIndex switch
    {
        0 => GridSortColumn.SellingPrice,
        1 => GridSortColumn.SellingCount,
        2 => GridSortColumn.BuyingPrice,
        _ => GridSortColumn.BuyingCount
    };

    private static int TrailingColumnIndex(GridSortColumn column) => column switch
    {
        GridSortColumn.SellingPrice => 0,
        GridSortColumn.SellingCount => 1,
        GridSortColumn.BuyingPrice => 2,
        GridSortColumn.BuyingCount => 3,
        _ => -1
    };

    /// <summary>Clickable/hoverable rect for the main <paramref name="title"/>, tight around its rendered glyphs (same technique as <see cref="StationToolbar.NameLocalRect"/>).</summary>
    public static SKRect TitleLocalRect(float originX, float originY, string title)
    {
        var header = HeaderLocalRect(originX, originY);
        var bounds = new SKRect();
        _titlePaint.MeasureText(title, ref bounds);
        float baselineY = header.Top + TitlePadding + MenuStyle.ButtonFontSize - TitleBaselineShift;
        float x = header.Left + TitlePadding;
        return new SKRect(x + bounds.Left, baselineY + bounds.Top, x + bounds.Right, baselineY + bounds.Bottom);
    }

    /// <summary>Clickable/hoverable rect for trailing column header <paramref name="columnIndex"/> — the full column width, header bar's full height (a generous target, not text-tight).</summary>
    public static SKRect TrailingColumnHeaderLocalRect(float originX, float originY, int columnIndex)
    {
        var header = HeaderLocalRect(originX, originY);
        float centerX = TrailingColumnCenterX(originX, columnIndex);
        return new SKRect(centerX - TrailingColumnWidth / 2f, header.Top, centerX + TrailingColumnWidth / 2f, header.Bottom);
    }

    /// <summary>
    /// Which sortable column title (see <see cref="GridSortColumn"/>) a click/hover at
    /// (<paramref name="localX"/>, <paramref name="localY"/>) landed on, or null. Drives
    /// both the click-to-sort behavior and the hover cursor swap — callers should treat a
    /// non-null result as "interactive" the same way they do the toolbar's exit-button/
    /// name-link hits.
    /// </summary>
    public static GridSortColumn? HitTestColumnTitle(float originX, float originY, string title, float localX, float localY)
    {
        var titleRect = TitleLocalRect(originX, originY, title);
        if (localX >= titleRect.Left && localX <= titleRect.Right && localY >= titleRect.Top && localY <= titleRect.Bottom)
            return GridSortColumn.Name;

        for (int c = 0; c < TrailingColumnCount; c++)
        {
            var rect = TrailingColumnHeaderLocalRect(originX, originY, c);
            if (localX >= rect.Left && localX <= rect.Right && localY >= rect.Top && localY <= rect.Bottom)
                return TrailingSortColumn(c);
        }

        return null;
    }

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
    /// <param name="sellingPriceValues">Optional per-row "Selling price" column text, same <c>scrollOffset + i</c> indexing as <paramref name="rowLabels"/>.</param>
    /// <param name="sellingCountValues">Optional per-row "Selling count" column text (station stock), same indexing.</param>
    /// <param name="buyingPriceValues">Optional per-row "Buying price" column text, same indexing.</param>
    /// <param name="buyingCountValues">Optional per-row "Buying count" column text (player's own quantity), same indexing.</param>
    /// <param name="selectedRowIndex">
    /// Absolute row index (see <see cref="HitTestRow"/>) to highlight — a distinct fill
    /// color replacing the zebra stripe plus a left-edge accent bar, same convention as the
    /// pre-redesign TradeScreen's row selection. Null (default) selects nothing.
    /// </param>
    /// <param name="sortColumn">
    /// Column the grid is currently sorted by (see <see cref="HitTestColumnTitle"/>) — its
    /// title gets the hover-glow treatment plus a direction arrow. Null draws every title
    /// plain, with no arrow.
    /// </param>
    /// <param name="sortDescending">Arrow direction for <paramref name="sortColumn"/> — ignored when it's null.</param>
    public static void Draw(
        SKCanvas canvas, float originX, float originY, string title, int rowCount,
        int scrollOffset, bool isScrollUpHovered, bool isScrollDownHovered,
        IReadOnlyList<string>? rowLabels = null,
        IReadOnlyList<string>? sellingPriceValues = null, IReadOnlyList<string>? sellingCountValues = null,
        IReadOnlyList<string>? buyingPriceValues = null, IReadOnlyList<string>? buyingCountValues = null,
        int? selectedRowIndex = null, GridSortColumn? sortColumn = null, bool sortDescending = false)
    {
        var header = HeaderLocalRect(originX, originY);
        canvas.DrawRoundRect(header, HeaderCornerRadius, HeaderCornerRadius, _headerPaint);

        float titleX = header.Left + TitlePadding;
        float titleBaselineY = header.Top + TitlePadding + MenuStyle.ButtonFontSize - TitleBaselineShift;
        if (sortColumn == GridSortColumn.Name)
            canvas.DrawText(title, titleX, titleBaselineY, _titleGlowPaint);
        canvas.DrawText(title, titleX, titleBaselineY, _titlePaint);
        if (sortColumn == GridSortColumn.Name)
        {
            float arrowX = titleX + _titlePaint.MeasureText(title) + SortArrowGap;
            DrawSortArrow(canvas, arrowX, header.MidY, sortDescending);
        }

        float columnHeaderBaselineY = MenuStyle.VerticalCenterBaseline(header, _columnHeaderPaint);
        for (int c = 0; c < TrailingColumnCount; c++)
        {
            string headerText = _trailingColumnHeaders[c];
            float centerX = TrailingColumnCenterX(originX, c);
            bool isSortedColumn = sortColumn == TrailingSortColumn(c);

            if (isSortedColumn)
                canvas.DrawText(headerText, centerX, columnHeaderBaselineY, _columnHeaderGlowPaint);
            canvas.DrawText(headerText, centerX, columnHeaderBaselineY, _columnHeaderPaint);

            if (isSortedColumn)
            {
                float arrowX = centerX + _columnHeaderPaint.MeasureText(headerText) / 2f + SortArrowGap;
                DrawSortArrow(canvas, arrowX, header.MidY, sortDescending);
            }
        }

        var trailingColumnValues = new[] { sellingPriceValues, sellingCountValues, buyingPriceValues, buyingCountValues };

        bool isEmpty = rowCount == 0;
        bool isActive = IsScrollbarActive(rowCount);
        int effectiveOffset = isActive ? Math.Clamp(scrollOffset, 0, MaxScrollOffset(rowCount)) : 0;
        int drawnRows = DrawnRowCount(rowCount);
        for (int i = 0; i < drawnRows; i++)
        {
            var rowRect = RowLocalRect(originX, originY, i);
            int labelIndex = effectiveOffset + i;
            bool isSelected = !isEmpty && selectedRowIndex == labelIndex;

            var fillPaint = isSelected ? _rowFillSelectedPaint
                : isEmpty ? _emptyRowFillPaint
                : i % 2 == 0 ? _rowFillLightPaint : _rowFillDarkPaint;
            canvas.DrawRect(rowRect, fillPaint);
            canvas.DrawRect(rowRect, _rowBorderPaint);

            if (isSelected)
                canvas.DrawLine(rowRect.Left, rowRect.Top, rowRect.Left, rowRect.Bottom, _selectedRowIndicatorPaint);

            float baselineY = MenuStyle.VerticalCenterBaseline(rowRect, _rowLabelPaint);

            if (rowLabels is not null && labelIndex < rowLabels.Count)
                canvas.DrawText(rowLabels[labelIndex], rowRect.Left + RowLabelPaddingX, baselineY, _rowLabelPaint);

            for (int c = 0; c < TrailingColumnCount; c++)
            {
                var values = trailingColumnValues[c];
                if (values is not null && labelIndex < values.Count)
                    canvas.DrawText(values[labelIndex], TrailingColumnCenterX(originX, c), baselineY, _rowValuePaint);
            }
        }

        DrawScrollbar(canvas, originX, originY, rowCount, effectiveOffset, isActive, isScrollUpHovered, isScrollDownHovered);
    }

    /// <summary>Small filled triangle marking sort direction — apex up for ascending, down for descending — left edge at <paramref name="leftX"/>, vertically centered on <paramref name="centerY"/>.</summary>
    private static void DrawSortArrow(SKCanvas canvas, float leftX, float centerY, bool descending)
    {
        float halfWidth = SortArrowSize / 2f;
        float top = centerY - halfWidth;
        float bottom = centerY + halfWidth;

        using var path = new SKPath();
        if (descending)
        {
            path.MoveTo(leftX, top);
            path.LineTo(leftX + SortArrowSize, top);
            path.LineTo(leftX + halfWidth, bottom);
        }
        else
        {
            path.MoveTo(leftX, bottom);
            path.LineTo(leftX + SortArrowSize, bottom);
            path.LineTo(leftX + halfWidth, top);
        }
        path.Close();
        canvas.DrawPath(path, _sortArrowPaint);
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
