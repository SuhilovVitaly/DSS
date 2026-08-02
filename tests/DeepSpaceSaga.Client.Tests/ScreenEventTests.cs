using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameMenu;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.MainMenu;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class ScreenEventTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static float PanelLeft => MenuLayout.PanelLeft(ScreenWidth);
    private static float PanelTop => MenuLayout.PanelTop(ScreenHeight);

    private static (float x, float y) ButtonCenter(float buttonLocalY)
    {
        float bx = PanelLeft + (MenuLayout.PanelWidth - MenuLayout.ButtonWidth) / 2f;
        return (bx + MenuLayout.ButtonWidth / 2f,
                PanelTop + buttonLocalY + MenuLayout.ButtonHeight / 2f);
    }

    private static void TriggerRender(IScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    // --- MainMenu tests ---

    [Fact]
    public void MainMenu_NewGame_click_returns_NewGame()
    {
        var screen = new MainMenuScreen();
        var (x, y) = ButtonCenter(MenuLayout.NewGameY);
        TriggerRender(screen);
        Assert.Equal(ScreenEvent.NewGame, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void MainMenu_Load_click_returns_None()
    {
        var screen = new MainMenuScreen();
        var (x, y) = ButtonCenter(MenuLayout.LoadY);
        TriggerRender(screen);
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void MainMenu_Exit_click_returns_Exit()
    {
        var screen = new MainMenuScreen();
        var (x, y) = ButtonCenter(MenuLayout.ExitY);
        TriggerRender(screen);
        Assert.Equal(ScreenEvent.Exit, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void GameSessionScreen_click_returns_None()
    {
        var buffer = new DeepSpaceSaga.Client.SnapshotBuffer();
        var screen = new GameSessionScreen(buffer);
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(100, 100));
    }

    // --- GameMenu tests ---

    private static float GamePanelLeft => GameMenuLayout.PanelLeft(ScreenWidth);
    private static float GamePanelTop => GameMenuLayout.PanelTop(ScreenHeight);

    private static (float x, float y) GameButtonCenter(float buttonLocalY)
    {
        float bx = GamePanelLeft + (GameMenuLayout.PanelWidth - GameMenuLayout.ButtonWidth) / 2f;
        return (bx + GameMenuLayout.ButtonWidth / 2f,
                GamePanelTop + buttonLocalY + GameMenuLayout.ButtonHeight / 2f);
    }

    private static void TriggerGameRender(IScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    [Fact]
    public void GameMenu_Resume_click_returns_Resume()
    {
        var screen = new GameMenuScreen();
        var (x, y) = GameButtonCenter(GameMenuLayout.ResumeY);
        TriggerGameRender(screen);
        Assert.Equal(ScreenEvent.Resume, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void GameMenu_MainMenu_click_returns_MainMenu()
    {
        var screen = new GameMenuScreen();
        var (x, y) = GameButtonCenter(GameMenuLayout.MainMenuY);
        TriggerGameRender(screen);
        Assert.Equal(ScreenEvent.MainMenu, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void GameMenu_Save_click_returns_None()
    {
        var screen = new GameMenuScreen();
        var (x, y) = GameButtonCenter(GameMenuLayout.SaveY);
        TriggerGameRender(screen);
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void GameMenu_Esc_returns_Resume()
    {
        var screen = new GameMenuScreen();
        TriggerGameRender(screen);
        Assert.Equal(ScreenEvent.Resume, screen.OnKeyDown(Key.Escape));
    }

    [Fact]
    public void GameSession_Esc_returns_OpenGameMenu()
    {
        var buffer = new DeepSpaceSaga.Client.SnapshotBuffer();
        var screen = new GameSessionScreen(buffer);
        Assert.Equal(ScreenEvent.OpenGameMenu, screen.OnKeyDown(Key.Escape));
    }

    [Fact]
    public void GameSession_other_key_returns_None()
    {
        var buffer = new DeepSpaceSaga.Client.SnapshotBuffer();
        var screen = new GameSessionScreen(buffer);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.A));
    }

    [Fact]
    public void GameMenu_other_key_returns_None()
    {
        var screen = new GameMenuScreen();
        TriggerGameRender(screen);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.A));
    }

}
