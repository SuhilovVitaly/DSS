using DeepSpaceSaga.Client.UI.Screens.ScenarioSelect;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// Pure hit-test geometry tests for <see cref="ScenarioSelectLayout"/> — no SKCanvas, no
/// SKPaint, no ScenarioSelectScreen (row heights/wrapping are the screen's job; here rows
/// are just arbitrary (Y, Height) pairs the caller hands in). Mirrors <see cref="LoadLayoutTests"/>.
/// </summary>
public class ScenarioSelectLayoutTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static float PanelLeft => ScenarioSelectLayout.PanelLeft(ScreenWidth);
    private static float PanelTop => ScenarioSelectLayout.PanelTop(ScreenHeight);

    private static (float x, float y) Center((float X, float Y, float W, float H) local) =>
        (PanelLeft + local.X + local.W / 2f, PanelTop + local.Y + local.H / 2f);

    /// <summary>BACK/PLAY rects are local to the action panel — add its offset too.</summary>
    private static (float x, float y) ActionCenter((float X, float Y, float W, float H) local) =>
        (PanelLeft + ScenarioSelectLayout.ActionPanelX + local.X + local.W / 2f,
         PanelTop + ScenarioSelectLayout.ActionPanelY + local.Y + local.H / 2f);

    private static ScenarioSelectLayout.VisibleRow Row(int absoluteIndex, float y, float height) =>
        new(absoluteIndex, y, height);

    [Fact]
    public void HitTest_outside_panel_returns_None()
    {
        var hit = ScenarioSelectLayout.HitTest(0, 0, ScreenWidth, ScreenHeight, Array.Empty<ScenarioSelectLayout.VisibleRow>());
        Assert.Equal(ScenarioSelectZone.None, hit.Zone);
    }

    [Fact]
    public void HitTest_Back_returns_Back()
    {
        var (x, y) = ActionCenter(ScenarioSelectLayout.BackButtonRect());
        var hit = ScenarioSelectLayout.HitTest(x, y, ScreenWidth, ScreenHeight, Array.Empty<ScenarioSelectLayout.VisibleRow>());
        Assert.Equal(ScenarioSelectZone.Back, hit.Zone);
    }

    [Fact]
    public void HitTest_Play_returns_Play()
    {
        var (x, y) = ActionCenter(ScenarioSelectLayout.PlayButtonRect());
        var hit = ScenarioSelectLayout.HitTest(x, y, ScreenWidth, ScreenHeight, Array.Empty<ScenarioSelectLayout.VisibleRow>());
        Assert.Equal(ScenarioSelectZone.Play, hit.Zone);
    }

    [Fact]
    public void HitTest_Row_returns_its_absolute_index()
    {
        var rows = new[] { Row(0, 170, 54), Row(1, 232, 54), Row(7, 294, 70) };
        var (x, y) = Center(ScenarioSelectLayout.RowRect(rows[2].Y, rows[2].Height));

        var hit = ScenarioSelectLayout.HitTest(x, y, ScreenWidth, ScreenHeight, rows);

        Assert.Equal(ScenarioSelectZone.Row, hit.Zone);
        Assert.Equal(7, hit.RowIndex);
    }

    [Fact]
    public void HitTest_below_the_last_visible_row_is_not_hit()
    {
        var rows = new[] { Row(0, 170, 54) };
        var hit = ScenarioSelectLayout.HitTest(PanelLeft + ScenarioSelectLayout.ListLeft + 10, PanelTop + 400, ScreenWidth, ScreenHeight, rows);
        Assert.Equal(ScenarioSelectZone.None, hit.Zone);
    }

    [Fact]
    public void PanelLeftAndTop_center_the_panel()
    {
        Assert.Equal((ScreenWidth - ScenarioSelectLayout.PanelWidth) / 2f, ScenarioSelectLayout.PanelLeft(ScreenWidth));
        Assert.Equal((ScreenHeight - ScenarioSelectLayout.PanelHeight) / 2f, ScenarioSelectLayout.PanelTop(ScreenHeight));
    }

    [Fact]
    public void ActionPanel_sits_immediately_right_of_the_content_panel()
    {
        Assert.Equal(ScenarioSelectLayout.ContentPanelX + ScenarioSelectLayout.ContentPanelWidth, ScenarioSelectLayout.ActionPanelX);
    }

    [Fact]
    public void PlayButton_sits_right_of_BackButton()
    {
        var back = ScenarioSelectLayout.BackButtonRect();
        var play = ScenarioSelectLayout.PlayButtonRect();
        Assert.True(play.X >= back.X + back.W);
    }

    // --- Row height grows with wrapped description line count ---

    [Fact]
    public void RowHeightFor_grows_with_more_description_lines()
    {
        float oneLine = ScenarioSelectLayout.RowHeightFor(1);
        float threeLines = ScenarioSelectLayout.RowHeightFor(3);
        Assert.True(threeLines > oneLine);
        Assert.Equal(2 * ScenarioSelectLayout.RowDescriptionLineHeight, threeLines - oneLine);
    }

    [Fact]
    public void RowHeightFor_treats_zero_lines_as_one()
    {
        Assert.Equal(ScenarioSelectLayout.RowHeightFor(1), ScenarioSelectLayout.RowHeightFor(0));
    }

    // --- Scrollbar geometry (shown only when the caller decides the list overflows) ---

    [Fact]
    public void ScrollbarThumb_at_top_when_scrollOffset_is_zero()
    {
        var track = ScenarioSelectLayout.ScrollbarTrackRect();
        var thumb = ScenarioSelectLayout.ScrollbarThumbRect(scrollOffset: 0, totalScenarioCount: 11, visibleRowCount: 6);
        Assert.Equal(track.Y, thumb.Y);
    }

    [Fact]
    public void ScrollbarThumb_at_bottom_when_scrollOffset_is_maxOffset()
    {
        int total = 11;
        int visible = 6;
        int maxOffset = total - visible;
        var track = ScenarioSelectLayout.ScrollbarTrackRect();
        var thumb = ScenarioSelectLayout.ScrollbarThumbRect(scrollOffset: maxOffset, totalScenarioCount: total, visibleRowCount: visible);
        Assert.Equal(track.Y + track.H - thumb.H, thumb.Y, precision: 3);
    }

    [Fact]
    public void ScrollbarTrack_does_not_overlap_the_row_list()
    {
        var track = ScenarioSelectLayout.ScrollbarTrackRect();
        var row = ScenarioSelectLayout.RowRect(ScenarioSelectLayout.ListTop, 54);
        Assert.True(track.X >= row.X + row.W);
    }
}
