using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// Header + zebra-striped row grid + scrollbar control (Trade screen's resources list
/// mockup, extracted ahead of the real redesign — see TradeScreenTests). Geometry/active-
/// state are a public contract; Draw is a smoke test only, matching
/// QuantityStepperTests.Draw_does_not_throw_for_any_combination_of_state_and_value.
/// </summary>
public class GridPanelTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(8, true)]
    public void IsScrollbarActive_is_true_only_above_four_rows(int rowCount, bool expected)
    {
        Assert.Equal(expected, GridPanel.IsScrollbarActive(rowCount));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    public void ScrollbarTrackLocalRect_height_matches_the_drawn_row_count_when_below_capacity(int rowCount)
    {
        var track = GridPanel.ScrollbarTrackLocalRect(0f, 0f, rowCount);
        Assert.Equal(rowCount * GridPanel.RowHeight, track.Height);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(100)]
    public void ScrollbarTrackLocalRect_height_is_capped_at_max_visible_rows(int rowCount)
    {
        var track = GridPanel.ScrollbarTrackLocalRect(0f, 0f, rowCount);
        Assert.Equal(GridPanel.MaxVisibleRows * GridPanel.RowHeight, track.Height);
    }

    /// <summary>An empty grid still draws — and syncs the scrollbar track to — a full page of dark-gray placeholder rows, not zero.</summary>
    [Fact]
    public void ScrollbarTrackLocalRect_height_is_a_full_page_when_the_grid_is_empty()
    {
        var track = GridPanel.ScrollbarTrackLocalRect(0f, 0f, rowCount: 0);
        Assert.Equal(GridPanel.MaxVisibleRows * GridPanel.RowHeight, track.Height);
    }

    [Fact]
    public void RowLocalRect_rows_are_stacked_without_gaps()
    {
        var row0 = GridPanel.RowLocalRect(10f, 20f, 0);
        var row1 = GridPanel.RowLocalRect(10f, 20f, 1);

        Assert.Equal(row0.Bottom, row1.Top);
        Assert.Equal(row0.Left, row1.Left);
        Assert.Equal(row0.Width, row1.Width);
    }

    [Fact]
    public void Scroll_arrow_rects_sit_at_the_top_and_bottom_of_the_track()
    {
        var track = GridPanel.ScrollbarTrackLocalRect(0f, 0f, GridPanel.MaxVisibleRows);
        var up = GridPanel.ScrollUpArrowLocalRect(0f, 0f, GridPanel.MaxVisibleRows);
        var down = GridPanel.ScrollDownArrowLocalRect(0f, 0f, GridPanel.MaxVisibleRows);

        Assert.Equal(track.Top, up.Top);
        Assert.Equal(track.Bottom, down.Bottom);
    }

    [Fact]
    public void Draw_does_not_throw_for_any_row_count_or_hover_combination()
    {
        using var bitmap = new SKBitmap(1200, 400);
        using var canvas = new SKCanvas(bitmap);

        var rowCounts = new[] { 0, 1, 4, 5, 6, 8 };
        var boolOptions = new[] { true, false };
        // Deliberately shorter than the largest rowCount above, to exercise the "fewer
        // values than rows" bound-check path for all five optional column lists.
        var labels = new[] { "A", "B", "C", "D", "E" };
        var sellingPrices = new[] { "10", "20", "30", "40", "50" };
        var sellingCounts = new[] { "1", "2", "3", "4", "5" };
        var buyingPrices = new[] { "11", "21", "31", "41", "51" };
        var buyingCounts = new[] { "6", "7", "8", "9", "10" };

        foreach (int rowCount in rowCounts)
        foreach (bool isUpHovered in boolOptions)
        foreach (bool isDownHovered in boolOptions)
            GridPanel.Draw(canvas, 10f, 10f, "Resources", rowCount, scrollOffset: 1,
                isScrollUpHovered: isUpHovered, isScrollDownHovered: isDownHovered,
                rowLabels: labels, sellingPriceValues: sellingPrices, sellingCountValues: sellingCounts,
                buyingPriceValues: buyingPrices, buyingCountValues: buyingCounts);
    }

    /// <summary>
    /// Directly guards the reported bug — 6 real rows in a 5-row visible window must let
    /// the scrollbar reach an offset that reveals the 6th row (previously the thumb moved
    /// but the drawn window never did, since <c>rowCount &gt; MaxVisibleRows</c> made the
    /// scrollbar active while nothing actually consumed the offset).
    /// </summary>
    [Theory]
    [InlineData(4, 0)]
    [InlineData(5, 0)]
    [InlineData(6, 1)]
    [InlineData(8, 3)]
    public void MaxScrollOffset_is_rows_hidden_past_the_visible_window(int rowCount, int expected)
    {
        Assert.Equal(expected, GridPanel.MaxScrollOffset(rowCount));
    }

    [Fact]
    public void HitTestRow_returns_the_absolute_row_index_under_the_click()
    {
        var row2 = GridPanel.RowLocalRect(0f, 0f, 2);
        int hit = GridPanel.HitTestRow(0f, 0f, rowCount: 5, scrollOffset: 0, row2.MidX, row2.MidY);
        Assert.Equal(2, hit);
    }

    /// <summary>Absolute index accounts for scrollOffset — visible slot 0 is scrollOffset + 0, not row index 0.</summary>
    [Fact]
    public void HitTestRow_accounts_for_scroll_offset()
    {
        var slot0 = GridPanel.RowLocalRect(0f, 0f, 0);
        int hit = GridPanel.HitTestRow(0f, 0f, rowCount: 6, scrollOffset: 1, slot0.MidX, slot0.MidY);
        Assert.Equal(1, hit);
    }

    [Fact]
    public void HitTestRow_returns_minus_one_outside_any_row_or_when_the_grid_is_empty()
    {
        Assert.Equal(-1, GridPanel.HitTestRow(0f, 0f, rowCount: 5, scrollOffset: 0, localX: -100f, localY: -100f));
        Assert.Equal(-1, GridPanel.HitTestRow(0f, 0f, rowCount: 0, scrollOffset: 0, localX: 100f, localY: 100f));
    }
}
