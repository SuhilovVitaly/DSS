using DeepSpaceSaga.Client.UI.Screens.GameMenu;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class GameMenuLayoutTests
{
    private static readonly GameMenuButton[] InteractiveRects =
    [
        GameMenuButton.Resume,
        GameMenuButton.Save,
        GameMenuButton.Load,
        GameMenuButton.Settings,
        GameMenuButton.MainMenu,
        GameMenuButton.Close
    ];

    [Theory]
    [InlineData(1280, 720)]
    [InlineData(1920, 1080)]
    [InlineData(2560, 1440)]
    public void Panel_is_centered_and_inside_supported_viewports(int width, int height)
    {
        var panel = GameMenuLayout.PanelRect(width, height);
        Assert.Equal(width / 2f, panel.MidX);
        Assert.Equal(height / 2f, panel.MidY);
        Assert.True(panel.Left >= 0 && panel.Top >= 0);
        Assert.True(panel.Right <= width && panel.Bottom <= height);
    }

    [Fact]
    public void Buttons_and_close_have_non_overlapping_hit_areas()
    {
        var rects = InteractiveRects.Select(RectFor).ToArray();
        for (int i = 0; i < rects.Length; i++)
        for (int j = i + 1; j < rects.Length; j++)
            Assert.False(Overlaps(rects[i], rects[j]), $"{InteractiveRects[i]} overlaps {InteractiveRects[j]}");
    }

    [Fact]
    public void HitTest_returns_each_declared_control()
    {
        foreach (var control in InteractiveRects)
        {
            var rect = RectFor(control);
            Assert.Equal(control, GameMenuLayout.HitTest(rect.MidX, rect.MidY, 1920, 1080));
        }
    }

    [Fact]
    public void HitTest_outside_panel_returns_none()
    {
        Assert.Equal(GameMenuButton.None, GameMenuLayout.HitTest(0, 0, 1920, 1080));
    }

    private static SKRect RectFor(GameMenuButton control) => control == GameMenuButton.Close
        ? GameMenuLayout.CloseRect(1920, 1080)
        : GameMenuLayout.ButtonRect(control, 1920, 1080);

    private static bool Overlaps(SKRect a, SKRect b) =>
        a.Left < b.Right && a.Right > b.Left && a.Top < b.Bottom && a.Bottom > b.Top;
}
