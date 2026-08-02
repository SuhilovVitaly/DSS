namespace DeepSpaceSaga.Client.Tests;

public class MenuLayoutTests
{
    // Use a standard 1920x1080 screen for tests
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static float PanelLeft => (ScreenWidth - MenuLayout.PanelWidth) / 2f;
    private static float PanelTop => (ScreenHeight - MenuLayout.PanelHeight) / 2f;

    private static (float x, float y) PanelLocalToScreen(float localX, float localY)
    {
        return (PanelLeft + localX, PanelTop + localY);
    }

    [Fact]
    public void HitTest_NewGameButton_center_returns_NewGame()
    {
        var (x, y) = PanelLocalToScreen(
            MenuLayout.ButtonLeft + MenuLayout.ButtonWidth / 2f,
            MenuLayout.NewGameButtonY + MenuLayout.ButtonHeight / 2f);

        var result = MenuLayout.HitTest(x, y, ScreenWidth, ScreenHeight);

        Assert.Equal(MenuButton.NewGame, result);
    }

    [Fact]
    public void HitTest_LoadButton_center_returns_Load()
    {
        var (x, y) = PanelLocalToScreen(
            MenuLayout.ButtonLeft + MenuLayout.ButtonWidth / 2f,
            MenuLayout.LoadButtonY + MenuLayout.ButtonHeight / 2f);

        var result = MenuLayout.HitTest(x, y, ScreenWidth, ScreenHeight);

        Assert.Equal(MenuButton.Load, result);
    }

    [Fact]
    public void HitTest_ExitButton_center_returns_Exit()
    {
        var (x, y) = PanelLocalToScreen(
            MenuLayout.ButtonLeft + MenuLayout.ButtonWidth / 2f,
            MenuLayout.ExitButtonY + MenuLayout.ButtonHeight / 2f);

        var result = MenuLayout.HitTest(x, y, ScreenWidth, ScreenHeight);

        Assert.Equal(MenuButton.Exit, result);
    }

    [Fact]
    public void HitTest_outside_panel_returns_None()
    {
        // Click in top-left corner (outside the centered panel)
        var result = MenuLayout.HitTest(0, 0, ScreenWidth, ScreenHeight);

        Assert.Equal(MenuButton.None, result);
    }

    [Fact]
    public void HitTest_inside_panel_but_not_on_button_returns_None()
    {
        // Click in the gap between buttons
        var (x, y) = PanelLocalToScreen(
            MenuLayout.ButtonLeft + MenuLayout.ButtonWidth / 2f,
            MenuLayout.LoadButtonY + MenuLayout.ButtonHeight + 10f); // just below LOAD

        var result = MenuLayout.HitTest(x, y, ScreenWidth, ScreenHeight);

        Assert.Equal(MenuButton.None, result);
    }

    [Fact]
    public void HitTest_with_different_resolution_centers_correctly()
    {
        // 2560x1440 — panel should still be centered
        int w = 2560, h = 1440;
        float expectedPanelLeft = (w - MenuLayout.PanelWidth) / 2f;
        var (x, y) = (expectedPanelLeft + MenuLayout.ButtonLeft + MenuLayout.ButtonWidth / 2f,
                       (h - MenuLayout.PanelHeight) / 2f + MenuLayout.ExitButtonY + MenuLayout.ButtonHeight / 2f);

        var result = MenuLayout.HitTest(x, y, w, h);

        Assert.Equal(MenuButton.Exit, result);
    }
}
