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

        var rowCounts = new[] { 0, 1, 4, 5, 8 };
        var boolOptions = new[] { true, false };

        foreach (int rowCount in rowCounts)
        foreach (bool isUpHovered in boolOptions)
        foreach (bool isDownHovered in boolOptions)
            GridPanel.Draw(canvas, 10f, 10f, "Resources", rowCount, scrollPosition: 2, scrollStepCount: 4,
                isScrollUpHovered: isUpHovered, isScrollDownHovered: isDownHovered);
    }
}
