using DeepSpaceSaga.Client.UI.Screens.Load;
using DeepSpaceSaga.Client.UI.Screens.Save;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// Pure hit-test geometry tests for <see cref="LoadLayout"/> — no SKCanvas, no LoadScreen.
/// Mirrors <see cref="SaveLayoutTests"/>.
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
    public void HitTest_Load_returns_row_index()
    {
        var (x, y) = Center(LoadLayout.LoadButtonRect(2));
        var hit = LoadLayout.HitTest(x, y, ScreenWidth, ScreenHeight, visibleSlotCount: 3);
        Assert.Equal(LoadZone.Load, hit.Zone);
        Assert.Equal(2, hit.RowIndex);
    }

    [Fact]
    public void HitTest_Delete_returns_row_index()
    {
        var (x, y) = Center(LoadLayout.DeleteButtonRect(1));
        var hit = LoadLayout.HitTest(x, y, ScreenWidth, ScreenHeight, visibleSlotCount: 3);
        Assert.Equal(LoadZone.Delete, hit.Zone);
        Assert.Equal(1, hit.RowIndex);
    }

    [Fact]
    public void HitTest_row_buttons_beyond_visibleSlotCount_are_not_hit()
    {
        // Only 1 slot visible — row index 1's Load/Delete rects must not register a hit.
        var (lx, ly) = Center(LoadLayout.LoadButtonRect(1));
        var hitLoad = LoadLayout.HitTest(lx, ly, ScreenWidth, ScreenHeight, visibleSlotCount: 1);
        Assert.Equal(LoadZone.None, hitLoad.Zone);

        var (dx, dy) = Center(LoadLayout.DeleteButtonRect(1));
        var hitDelete = LoadLayout.HitTest(dx, dy, ScreenWidth, ScreenHeight, visibleSlotCount: 1);
        Assert.Equal(LoadZone.None, hitDelete.Zone);
    }

    [Fact]
    public void HitTest_Load_and_Delete_do_not_overlap()
    {
        var (lx, ly) = Center(LoadLayout.LoadButtonRect(0));
        var hitAtLoadCenter = LoadLayout.HitTest(lx, ly, ScreenWidth, ScreenHeight, visibleSlotCount: 1);
        Assert.Equal(LoadZone.Load, hitAtLoadCenter.Zone);

        var (dx, dy) = Center(LoadLayout.DeleteButtonRect(0));
        var hitAtDeleteCenter = LoadLayout.HitTest(dx, dy, ScreenWidth, ScreenHeight, visibleSlotCount: 1);
        Assert.Equal(LoadZone.Delete, hitAtDeleteCenter.Zone);
    }

    [Fact]
    public void PanelLeftAndTop_center_the_panel()
    {
        Assert.Equal((ScreenWidth - LoadLayout.PanelWidth) / 2f, LoadLayout.PanelLeft(ScreenWidth));
        Assert.Equal((ScreenHeight - LoadLayout.PanelHeight) / 2f, LoadLayout.PanelTop(ScreenHeight));
    }

    [Fact]
    public void Panel_same_size_as_SaveLayout()
    {
        // Load and Save share the same panel footprint per the story's design ("in the
        // same style" as Save).
        Assert.Equal(SaveLayout.PanelWidth, LoadLayout.PanelWidth);
        Assert.Equal(SaveLayout.PanelHeight, LoadLayout.PanelHeight);
    }

    [Fact]
    public void RowButton_size_matches_SaveLayout()
    {
        // The LOAD/DELETE button columns must line up visually with Save's
        // OVERWRITE/DELETE columns, so both overlays share one fixed button size.
        Assert.Equal(SaveLayout.RowButtonWidth, LoadLayout.RowButtonWidth);
        Assert.Equal(SaveLayout.RowButtonHeight, LoadLayout.RowButtonHeight);
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
    public void ScrollbarTrack_does_not_overlap_row_buttons()
    {
        // The track sits in the right margin strip, clear of where DeleteButtonRect ends.
        var track = LoadLayout.ScrollbarTrackRect();
        var deleteButton = LoadLayout.DeleteButtonRect(0);
        Assert.True(track.X >= deleteButton.X + deleteButton.W);
    }
}
