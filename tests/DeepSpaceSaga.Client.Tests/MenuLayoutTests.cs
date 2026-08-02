using DeepSpaceSaga.Client.UI.Screens.MainMenu;

namespace DeepSpaceSaga.Client.Tests;

public class MenuLayoutTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static float PanelLeft => MenuLayout.PanelLeft(ScreenWidth);
    private static float PanelTop => MenuLayout.PanelTop(ScreenHeight);

    private static (float x, float y) ButtonCenterInScreen(float buttonLocalY)
    {
        float bx = PanelLeft + (MenuLayout.PanelWidth - MenuLayout.ButtonWidth) / 2f;
        return (bx + MenuLayout.ButtonWidth / 2f,
                PanelTop + buttonLocalY + MenuLayout.ButtonHeight / 2f);
    }

    [Fact]
    public void HitTest_NewGame_center_returns_NewGame()
    {
        var (x, y) = ButtonCenterInScreen(MenuLayout.NewGameY);
        Assert.Equal(MenuButton.NewGame, MenuLayout.HitTest(x, y, ScreenWidth, ScreenHeight));
    }

    [Fact]
    public void HitTest_Load_center_returns_Load()
    {
        var (x, y) = ButtonCenterInScreen(MenuLayout.LoadY);
        Assert.Equal(MenuButton.Load, MenuLayout.HitTest(x, y, ScreenWidth, ScreenHeight));
    }

    [Fact]
    public void HitTest_Exit_center_returns_Exit()
    {
        var (x, y) = ButtonCenterInScreen(MenuLayout.ExitY);
        Assert.Equal(MenuButton.Exit, MenuLayout.HitTest(x, y, ScreenWidth, ScreenHeight));
    }

    [Fact]
    public void HitTest_outside_panel_returns_None()
    {
        Assert.Equal(MenuButton.None, MenuLayout.HitTest(0, 0, ScreenWidth, ScreenHeight));
    }

    [Fact]
    public void HitTest_between_buttons_returns_None()
    {
        float cx = ScreenWidth / 2f;
        float midY = PanelTop + (MenuLayout.LoadY + MenuLayout.ExitY) / 2f;
        Assert.Equal(MenuButton.None, MenuLayout.HitTest(cx, midY, ScreenWidth, ScreenHeight));
    }

    [Fact]
    public void Panel_is_centered()
    {
        Assert.Equal((1920f - MenuLayout.PanelWidth) / 2f, PanelLeft);
        Assert.Equal((1080f - MenuLayout.PanelHeight) / 2f, PanelTop);
    }

    [Fact]
    public void Different_resolution_still_centers_panel()
    {
        int w = 2560, h = 1440;
        var (x, y) = (MenuLayout.PanelLeft(w) + (MenuLayout.PanelWidth - MenuLayout.ButtonWidth) / 2f + MenuLayout.ButtonWidth / 2f,
                       MenuLayout.PanelTop(h) + MenuLayout.ExitY + MenuLayout.ButtonHeight / 2f);
        Assert.Equal(MenuButton.Exit, MenuLayout.HitTest(x, y, w, h));
    }
}
