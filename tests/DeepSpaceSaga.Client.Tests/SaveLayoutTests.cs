using DeepSpaceSaga.Client.UI.Screens.Save;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// Pure hit-test geometry tests for <see cref="SaveLayout"/> — no SKCanvas, no SaveScreen.
/// Mirrors the pure-function testing convention used for GameMenu/Settings layouts.
/// </summary>
public class SaveLayoutTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static float PanelLeft => SaveLayout.PanelLeft(ScreenWidth);
    private static float PanelTop => SaveLayout.PanelTop(ScreenHeight);

    private static (float x, float y) Center((float X, float Y, float W, float H) local) =>
        (PanelLeft + local.X + local.W / 2f, PanelTop + local.Y + local.H / 2f);

    [Fact]
    public void HitTest_outside_panel_returns_None()
    {
        var hit = SaveLayout.HitTest(0, 0, ScreenWidth, ScreenHeight, visibleSlotCount: 0, isNewSaveActive: false);
        Assert.Equal(SaveZone.None, hit.Zone);
    }

    [Fact]
    public void HitTest_NewSaveButton_when_collapsed()
    {
        var (x, y) = Center(SaveLayout.NewSaveButtonRect());
        var hit = SaveLayout.HitTest(x, y, ScreenWidth, ScreenHeight, visibleSlotCount: 0, isNewSaveActive: false);
        Assert.Equal(SaveZone.NewSave, hit.Zone);
    }

    [Fact]
    public void HitTest_NewSaveButton_area_returns_None_when_expanded()
    {
        // When expanded, the New Save button rect is replaced by the name-entry
        // controls — clicking its old location must not hit NewSave again.
        var (x, y) = Center(SaveLayout.NewSaveButtonRect());
        var hit = SaveLayout.HitTest(x, y, ScreenWidth, ScreenHeight, visibleSlotCount: 0, isNewSaveActive: true);
        Assert.NotEqual(SaveZone.NewSave, hit.Zone);
    }

    [Fact]
    public void HitTest_ConfirmNewSave_only_when_expanded()
    {
        var (x, y) = Center(SaveLayout.ConfirmButtonRect());

        var whenCollapsed = SaveLayout.HitTest(x, y, ScreenWidth, ScreenHeight, visibleSlotCount: 0, isNewSaveActive: false);
        var whenExpanded = SaveLayout.HitTest(x, y, ScreenWidth, ScreenHeight, visibleSlotCount: 0, isNewSaveActive: true);

        Assert.NotEqual(SaveZone.ConfirmNewSave, whenCollapsed.Zone);
        Assert.Equal(SaveZone.ConfirmNewSave, whenExpanded.Zone);
    }

    [Fact]
    public void HitTest_CancelNewSave_only_when_expanded()
    {
        var (x, y) = Center(SaveLayout.CancelButtonRect());
        var hit = SaveLayout.HitTest(x, y, ScreenWidth, ScreenHeight, visibleSlotCount: 0, isNewSaveActive: true);
        Assert.Equal(SaveZone.CancelNewSave, hit.Zone);
    }

    [Fact]
    public void HitTest_Close_returns_Close()
    {
        var (x, y) = Center(SaveLayout.CloseButtonRect());
        var hit = SaveLayout.HitTest(x, y, ScreenWidth, ScreenHeight, visibleSlotCount: 0, isNewSaveActive: false);
        Assert.Equal(SaveZone.Close, hit.Zone);
    }

    [Fact]
    public void HitTest_Overwrite_returns_Overwrite()
    {
        var (x, y) = Center(SaveLayout.OverwriteButtonRect());
        var hit = SaveLayout.HitTest(x, y, ScreenWidth, ScreenHeight, visibleSlotCount: 3, isNewSaveActive: false);
        Assert.Equal(SaveZone.Overwrite, hit.Zone);
    }

    [Fact]
    public void HitTest_Delete_returns_Delete()
    {
        var (x, y) = Center(SaveLayout.DeleteButtonRect());
        var hit = SaveLayout.HitTest(x, y, ScreenWidth, ScreenHeight, visibleSlotCount: 3, isNewSaveActive: false);
        Assert.Equal(SaveZone.Delete, hit.Zone);
    }

    [Fact]
    public void HitTest_Row_returns_visible_row_index()
    {
        var (x, y) = Center(SaveLayout.RowRect(1));
        var hit = SaveLayout.HitTest(x, y, ScreenWidth, ScreenHeight, visibleSlotCount: 3, isNewSaveActive: false);
        Assert.Equal(SaveZone.Row, hit.Zone);
        Assert.Equal(1, hit.RowIndex);
    }

    [Fact]
    public void HitTest_Overwrite_and_Delete_do_not_overlap()
    {
        var (ox, oy) = Center(SaveLayout.OverwriteButtonRect());
        var hitAtOverwriteCenter = SaveLayout.HitTest(ox, oy, ScreenWidth, ScreenHeight, visibleSlotCount: 1, isNewSaveActive: false);
        Assert.Equal(SaveZone.Overwrite, hitAtOverwriteCenter.Zone);

        var (dx, dy) = Center(SaveLayout.DeleteButtonRect());
        var hitAtDeleteCenter = SaveLayout.HitTest(dx, dy, ScreenWidth, ScreenHeight, visibleSlotCount: 1, isNewSaveActive: false);
        Assert.Equal(SaveZone.Delete, hitAtDeleteCenter.Zone);
    }

    [Fact]
    public void PanelLeftAndTop_center_the_panel()
    {
        Assert.Equal((ScreenWidth - SaveLayout.PanelWidth) / 2f, SaveLayout.PanelLeft(ScreenWidth));
        Assert.Equal((ScreenHeight - SaveLayout.PanelHeight) / 2f, SaveLayout.PanelTop(ScreenHeight));
    }

    [Fact]
    public void Main_actions_share_one_bottom_row()
    {
        var close = SaveLayout.CloseButtonRect();
        var delete = SaveLayout.DeleteButtonRect();
        var overwrite = SaveLayout.OverwriteButtonRect();
        var create = SaveLayout.NewSaveButtonRect();

        Assert.Equal(close.Y, delete.Y);
        Assert.Equal(delete.Y, overwrite.Y);
        Assert.Equal(overwrite.Y, create.Y);
        Assert.True(close.X + close.W <= delete.X);
        Assert.True(delete.X + delete.W <= overwrite.X);
        Assert.True(overwrite.X + overwrite.W <= create.X);
    }

    [Fact]
    public void NewSave_actions_share_the_bottom_row()
    {
        var close = SaveLayout.CloseButtonRect(isNewSaveActive: true);
        var cancel = SaveLayout.CancelButtonRect();
        var save = SaveLayout.ConfirmButtonRect();

        Assert.Equal(close.Y, cancel.Y);
        Assert.Equal(cancel.Y, save.Y);
        Assert.True(close.X + close.W <= cancel.X);
        Assert.True(cancel.X + cancel.W <= save.X);
    }

    // --- Scrollbar geometry (shown only when slots.Count > VisibleRows) ---

    [Fact]
    public void ScrollbarThumb_at_top_when_scrollOffset_is_zero()
    {
        var track = SaveLayout.ScrollbarTrackRect();
        var thumb = SaveLayout.ScrollbarThumbRect(scrollOffset: 0, totalSlotCount: SaveLayout.VisibleRows + 5);
        Assert.Equal(track.Y, thumb.Y);
    }

    [Fact]
    public void ScrollbarThumb_at_bottom_when_scrollOffset_is_maxOffset()
    {
        int total = SaveLayout.VisibleRows + 5;
        int maxOffset = total - SaveLayout.VisibleRows;
        var track = SaveLayout.ScrollbarTrackRect();
        var thumb = SaveLayout.ScrollbarThumbRect(scrollOffset: maxOffset, totalSlotCount: total);
        Assert.Equal(track.Y + track.H - thumb.H, thumb.Y, precision: 3);
    }

    [Fact]
    public void ScrollbarThumb_shrinks_as_totalSlotCount_grows()
    {
        var thumbFewExtra = SaveLayout.ScrollbarThumbRect(scrollOffset: 0, totalSlotCount: SaveLayout.VisibleRows + 1);
        var thumbManyExtra = SaveLayout.ScrollbarThumbRect(scrollOffset: 0, totalSlotCount: SaveLayout.VisibleRows + 50);
        Assert.True(thumbManyExtra.H < thumbFewExtra.H);
    }

    [Fact]
    public void ScrollbarThumb_never_shrinks_below_minimum_height()
    {
        var thumb = SaveLayout.ScrollbarThumbRect(scrollOffset: 0, totalSlotCount: SaveLayout.VisibleRows + 500);
        Assert.True(thumb.H >= SaveLayout.ScrollbarThumbMinHeight);
    }

    [Fact]
    public void ScrollbarTrack_stays_inside_the_content_panel()
    {
        var track = SaveLayout.ScrollbarTrackRect();
        var row = SaveLayout.RowRect(0);
        var content = SaveLayout.ContentPanelRect();
        Assert.True(track.X >= row.X + row.W);
        Assert.True(track.X + track.W <= content.X + content.W);
    }
}
