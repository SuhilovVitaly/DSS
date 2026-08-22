using DeepSpaceSaga.Client.UI.Screens.ScenarioSelect;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// Pure hit-test geometry tests for <see cref="ScenarioSelectLayout"/> — no SKCanvas, no
/// ScenarioSelectScreen. Mirrors <see cref="LoadLayoutTests"/>.
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

    [Fact]
    public void HitTest_outside_panel_returns_None()
    {
        var hit = ScenarioSelectLayout.HitTest(0, 0, ScreenWidth, ScreenHeight, visibleScenarioCount: 0);
        Assert.Equal(ScenarioSelectZone.None, hit.Zone);
    }

    [Fact]
    public void HitTest_Back_returns_Back()
    {
        var (x, y) = ActionCenter(ScenarioSelectLayout.BackButtonRect());
        var hit = ScenarioSelectLayout.HitTest(x, y, ScreenWidth, ScreenHeight, visibleScenarioCount: 0);
        Assert.Equal(ScenarioSelectZone.Back, hit.Zone);
    }

    [Fact]
    public void HitTest_Play_returns_Play()
    {
        var (x, y) = ActionCenter(ScenarioSelectLayout.PlayButtonRect());
        var hit = ScenarioSelectLayout.HitTest(x, y, ScreenWidth, ScreenHeight, visibleScenarioCount: 3);
        Assert.Equal(ScenarioSelectZone.Play, hit.Zone);
    }

    [Fact]
    public void HitTest_Row_returns_row_index()
    {
        var (x, y) = Center(ScenarioSelectLayout.RowRect(2));
        var hit = ScenarioSelectLayout.HitTest(x, y, ScreenWidth, ScreenHeight, visibleScenarioCount: 3);
        Assert.Equal(ScenarioSelectZone.Row, hit.Zone);
        Assert.Equal(2, hit.RowIndex);
    }

    [Fact]
    public void HitTest_Row_beyond_visibleScenarioCount_is_not_hit()
    {
        var (x, y) = Center(ScenarioSelectLayout.RowRect(1));
        var hit = ScenarioSelectLayout.HitTest(x, y, ScreenWidth, ScreenHeight, visibleScenarioCount: 1);
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

    // --- Scrollbar geometry (shown only when scenarios.Count > VisibleRows) ---

    [Fact]
    public void ScrollbarThumb_at_top_when_scrollOffset_is_zero()
    {
        var track = ScenarioSelectLayout.ScrollbarTrackRect();
        var thumb = ScenarioSelectLayout.ScrollbarThumbRect(scrollOffset: 0, totalScenarioCount: ScenarioSelectLayout.VisibleRows + 5);
        Assert.Equal(track.Y, thumb.Y);
    }

    [Fact]
    public void ScrollbarThumb_at_bottom_when_scrollOffset_is_maxOffset()
    {
        int total = ScenarioSelectLayout.VisibleRows + 5;
        int maxOffset = total - ScenarioSelectLayout.VisibleRows;
        var track = ScenarioSelectLayout.ScrollbarTrackRect();
        var thumb = ScenarioSelectLayout.ScrollbarThumbRect(scrollOffset: maxOffset, totalScenarioCount: total);
        Assert.Equal(track.Y + track.H - thumb.H, thumb.Y, precision: 3);
    }

    [Fact]
    public void ScrollbarTrack_does_not_overlap_the_row_list()
    {
        var track = ScenarioSelectLayout.ScrollbarTrackRect();
        var row = ScenarioSelectLayout.RowRect(0);
        Assert.True(track.X >= row.X + row.W);
    }
}
