using DeepSpaceSaga.Client.UI.Screens.Save;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>Pure geometry and hit-test coverage for the always-open Save editor.</summary>
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
        var hit = SaveLayout.HitTest(0, 0, ScreenWidth, ScreenHeight, visibleSlotCount: 0);
        Assert.Equal(SaveZone.None, hit.Zone);
    }

    [Fact]
    public void HitTest_SaveButton_returns_Save()
    {
        var (x, y) = Center(SaveLayout.SaveButtonRect());
        var hit = SaveLayout.HitTest(x, y, ScreenWidth, ScreenHeight, visibleSlotCount: 0);
        Assert.Equal(SaveZone.Save, hit.Zone);
    }

    [Fact]
    public void HitTest_Close_returns_Close()
    {
        var (x, y) = Center(SaveLayout.CloseButtonRect());
        var hit = SaveLayout.HitTest(x, y, ScreenWidth, ScreenHeight, visibleSlotCount: 0);
        Assert.Equal(SaveZone.Close, hit.Zone);
    }

    [Fact]
    public void HitTest_Delete_returns_Delete()
    {
        var (x, y) = Center(SaveLayout.DeleteButtonRect());
        var hit = SaveLayout.HitTest(x, y, ScreenWidth, ScreenHeight, visibleSlotCount: 3);
        Assert.Equal(SaveZone.Delete, hit.Zone);
    }

    [Fact]
    public void HitTest_Row_returns_visible_row_index()
    {
        var (x, y) = Center(SaveLayout.RowRect(1));
        var hit = SaveLayout.HitTest(x, y, ScreenWidth, ScreenHeight, visibleSlotCount: 3);
        Assert.Equal(SaveZone.Row, hit.Zone);
        Assert.Equal(1, hit.RowIndex);
    }

    [Fact]
    public void PanelLeftAndTop_center_the_panel()
    {
        Assert.Equal((ScreenWidth - SaveLayout.PanelWidth) / 2f, SaveLayout.PanelLeft(ScreenWidth));
        Assert.Equal((ScreenHeight - SaveLayout.PanelHeight) / 2f, SaveLayout.PanelTop(ScreenHeight));
    }

    [Fact]
    public void Name_input_is_always_above_the_slot_list_inside_content_panel()
    {
        var input = SaveLayout.NameInputRect();
        var firstRow = SaveLayout.RowRect(0);
        var content = SaveLayout.ContentPanelRect();

        Assert.True(input.Y >= content.Y);
        Assert.True(input.Y + input.H < firstRow.Y);
        Assert.True(firstRow.Y + SaveLayout.VisibleRows * SaveLayout.RowHeight <= content.Y + content.H);
    }

    [Fact]
    public void All_actions_share_one_centered_bottom_row()
    {
        var close = SaveLayout.CloseButtonRect();
        var delete = SaveLayout.DeleteButtonRect();
        var save = SaveLayout.SaveButtonRect();

        Assert.Equal(close.Y, delete.Y);
        Assert.Equal(close.Y, save.Y);
        Assert.True(close.X + close.W <= delete.X);
        Assert.True(delete.X + delete.W <= save.X);
        Assert.Equal(close.X, SaveLayout.PanelWidth - (save.X + save.W));
    }

    [Fact]
    public void ScrollbarThumb_at_top_when_scrollOffset_is_zero()
    {
        var track = SaveLayout.ScrollbarTrackRect();
        var thumb = SaveLayout.ScrollbarThumbRect(0, SaveLayout.VisibleRows + 5);
        Assert.Equal(track.Y, thumb.Y);
    }

    [Fact]
    public void ScrollbarThumb_at_bottom_when_scrollOffset_is_maxOffset()
    {
        int total = SaveLayout.VisibleRows + 5;
        int maxOffset = total - SaveLayout.VisibleRows;
        var track = SaveLayout.ScrollbarTrackRect();
        var thumb = SaveLayout.ScrollbarThumbRect(maxOffset, total);
        Assert.Equal(track.Y + track.H - thumb.H, thumb.Y, precision: 3);
    }

    [Fact]
    public void ScrollbarThumb_shrinks_as_totalSlotCount_grows()
    {
        var few = SaveLayout.ScrollbarThumbRect(0, SaveLayout.VisibleRows + 1);
        var many = SaveLayout.ScrollbarThumbRect(0, SaveLayout.VisibleRows + 50);
        Assert.True(many.H < few.H);
    }

    [Fact]
    public void ScrollbarThumb_never_shrinks_below_minimum_height()
    {
        var thumb = SaveLayout.ScrollbarThumbRect(0, SaveLayout.VisibleRows + 500);
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
