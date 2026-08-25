using DeepSpaceSaga.Client.UI.Screens.Load;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// Pure hit-test geometry tests for <see cref="LoadLayout"/> — no SKCanvas, no LoadScreen.
/// A full-width content panel holds selectable rows; CLOSE, DELETE, and LOAD share one
/// bottom horizontal action row.
/// </summary>
public class LoadLayoutTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static float PanelLeft => LoadLayout.PanelLeft(ScreenWidth);
    private static float PanelTop => LoadLayout.PanelTop(ScreenHeight);

    private static (float x, float y) Center((float X, float Y, float W, float H) local) =>
        (PanelLeft + local.X + local.W / 2f, PanelTop + local.Y + local.H / 2f);

    [Fact]
    public void HitTest_outside_panel_returns_None()
    {
        var hit = LoadLayout.HitTest(0, 0, ScreenWidth, ScreenHeight, visibleSlotCount: 0);
        Assert.Equal(LoadZone.None, hit.Zone);
    }

    [Fact]
    public void HitTest_Close_returns_Close()
    {
        var (x, y) = Center(LoadLayout.CloseButtonRect());
        var hit = LoadLayout.HitTest(x, y, ScreenWidth, ScreenHeight, visibleSlotCount: 0);
        Assert.Equal(LoadZone.Close, hit.Zone);
    }

    [Fact]
    public void HitTest_Load_returns_Load()
    {
        var (x, y) = Center(LoadLayout.LoadButtonRect());
        var hit = LoadLayout.HitTest(x, y, ScreenWidth, ScreenHeight, visibleSlotCount: 0);
        Assert.Equal(LoadZone.Load, hit.Zone);
    }

    [Fact]
    public void HitTest_Delete_returns_Delete()
    {
        var (x, y) = Center(LoadLayout.DeleteButtonRect());
        var hit = LoadLayout.HitTest(x, y, ScreenWidth, ScreenHeight, visibleSlotCount: 0);
        Assert.Equal(LoadZone.Delete, hit.Zone);
    }

    [Fact]
    public void HitTest_Row_returns_its_visible_row_index()
    {
        var (x, y) = Center(LoadLayout.RowRect(2));
        var hit = LoadLayout.HitTest(x, y, ScreenWidth, ScreenHeight, visibleSlotCount: 3);
        Assert.Equal(LoadZone.Row, hit.Zone);
        Assert.Equal(2, hit.RowIndex);
    }

    [Fact]
    public void HitTest_row_beyond_visibleSlotCount_is_not_hit()
    {
        // Only 1 slot visible — row index 1's rect must not register a Row hit.
        var (x, y) = Center(LoadLayout.RowRect(1));
        var hit = LoadLayout.HitTest(x, y, ScreenWidth, ScreenHeight, visibleSlotCount: 1);
        Assert.Equal(LoadZone.None, hit.Zone);
    }

    [Fact]
    public void PanelLeftAndTop_center_the_panel()
    {
        Assert.Equal((ScreenWidth - LoadLayout.PanelWidth) / 2f, LoadLayout.PanelLeft(ScreenWidth));
        Assert.Equal((ScreenHeight - LoadLayout.PanelHeight) / 2f, LoadLayout.PanelTop(ScreenHeight));
    }

    // --- Content panel / row list / bottom action geometry ---

    [Fact]
    public void ContentPanel_spans_the_inner_window_width()
    {
        var content = LoadLayout.ContentPanelRect();
        Assert.Equal(LoadLayout.Margin, content.X);
        Assert.Equal(LoadLayout.PanelWidth - 2 * LoadLayout.Margin, content.W);
    }

    [Fact]
    public void RowList_fits_within_the_content_panel()
    {
        var content = LoadLayout.ContentPanelRect();
        var firstRow = LoadLayout.RowRect(0);
        var lastRow = LoadLayout.RowRect(LoadLayout.VisibleRows - 1);

        Assert.True(firstRow.X >= content.X);
        Assert.True(firstRow.Y >= content.Y);
        Assert.True(lastRow.Y + lastRow.H <= content.Y + content.H + 0.01f);
    }

    [Fact]
    public void All_three_buttons_share_one_bottom_row_without_overlap()
    {
        var close = LoadLayout.CloseButtonRect();
        var load = LoadLayout.LoadButtonRect();
        var delete = LoadLayout.DeleteButtonRect();

        Assert.Equal(close.Y, delete.Y);
        Assert.Equal(delete.Y, load.Y);
        Assert.Equal(close.H, delete.H);
        Assert.Equal(delete.H, load.H);
        Assert.True(close.X + close.W <= delete.X);
        Assert.True(delete.X + delete.W <= load.X);
        Assert.Equal(LoadLayout.Margin, close.X);
        Assert.Equal(LoadLayout.PanelWidth - LoadLayout.Margin, load.X + load.W);
    }

    [Fact]
    public void Bottom_buttons_sit_below_the_content_panel()
    {
        var content = LoadLayout.ContentPanelRect();
        var close = LoadLayout.CloseButtonRect();

        Assert.True(content.Y + content.H <= close.Y + 0.01f);
        Assert.True(close.Y + close.H <= LoadLayout.PanelHeight);
    }

    // --- Scrollbar geometry (shown only when slots.Count > VisibleRows) ---

    [Fact]
    public void ScrollbarThumb_at_top_when_scrollOffset_is_zero()
    {
        var track = LoadLayout.ScrollbarTrackRect();
        var thumb = LoadLayout.ScrollbarThumbRect(scrollOffset: 0, totalSlotCount: LoadLayout.VisibleRows + 5);
        Assert.Equal(track.Y, thumb.Y);
    }

    [Fact]
    public void ScrollbarThumb_at_bottom_when_scrollOffset_is_maxOffset()
    {
        int total = LoadLayout.VisibleRows + 5;
        int maxOffset = total - LoadLayout.VisibleRows;
        var track = LoadLayout.ScrollbarTrackRect();
        var thumb = LoadLayout.ScrollbarThumbRect(scrollOffset: maxOffset, totalSlotCount: total);
        Assert.Equal(track.Y + track.H - thumb.H, thumb.Y, precision: 3);
    }

    [Fact]
    public void ScrollbarThumb_shrinks_as_totalSlotCount_grows()
    {
        var thumbFewExtra = LoadLayout.ScrollbarThumbRect(scrollOffset: 0, totalSlotCount: LoadLayout.VisibleRows + 1);
        var thumbManyExtra = LoadLayout.ScrollbarThumbRect(scrollOffset: 0, totalSlotCount: LoadLayout.VisibleRows + 50);
        Assert.True(thumbManyExtra.H < thumbFewExtra.H);
    }

    [Fact]
    public void ScrollbarThumb_never_shrinks_below_minimum_height()
    {
        var thumb = LoadLayout.ScrollbarThumbRect(scrollOffset: 0, totalSlotCount: LoadLayout.VisibleRows + 500);
        Assert.True(thumb.H >= LoadLayout.ScrollbarThumbMinHeight);
    }

    [Fact]
    public void ScrollbarTrack_does_not_overlap_the_row_list_or_leave_the_content_panel()
    {
        var track = LoadLayout.ScrollbarTrackRect();
        var row = LoadLayout.RowRect(0);
        var content = LoadLayout.ContentPanelRect();

        Assert.True(track.X >= row.X + row.W);
        Assert.True(track.X + track.W <= content.X + content.W + 0.01f);
    }
}
