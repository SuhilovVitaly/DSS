namespace DeepSpaceSaga.Client.Tests;

public class ScreenEventTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static float PanelLeft => (ScreenWidth - MenuLayout.PanelWidth) / 2f;
    private static float PanelTop => (ScreenHeight - MenuLayout.PanelHeight) / 2f;

    private static (float x, float y) ButtonCenter(float buttonY)
    {
        return (
            PanelLeft + MenuLayout.ButtonLeft + MenuLayout.ButtonWidth / 2f,
            PanelTop + buttonY + MenuLayout.ButtonHeight / 2f
        );
    }

    [Fact]
    public void MainMenu_NewGame_click_returns_NewGame_event()
    {
        var screen = new MainMenuScreen();
        var (x, y) = ButtonCenter(MenuLayout.NewGameButtonY);

        // Render once to set screen dimensions for hit testing
        TriggerRender(screen);

        var result = screen.OnMouseDown(x, y);

        Assert.Equal(ScreenEvent.NewGame, result);
    }

    [Fact]
    public void MainMenu_Load_click_returns_None()
    {
        var screen = new MainMenuScreen();
        var (x, y) = ButtonCenter(MenuLayout.LoadButtonY);

        TriggerRender(screen);

        var result = screen.OnMouseDown(x, y);

        Assert.Equal(ScreenEvent.None, result);
    }

    [Fact]
    public void MainMenu_Exit_click_returns_Exit_event()
    {
        var screen = new MainMenuScreen();
        var (x, y) = ButtonCenter(MenuLayout.ExitButtonY);

        TriggerRender(screen);

        var result = screen.OnMouseDown(x, y);

        Assert.Equal(ScreenEvent.Exit, result);
    }

    [Fact]
    public void MainMenu_click_outside_returns_None()
    {
        var screen = new MainMenuScreen();
        TriggerRender(screen);

        var result = screen.OnMouseDown(0, 0);

        Assert.Equal(ScreenEvent.None, result);
    }

    [Fact]
    public void GameSessionScreen_click_always_returns_None()
    {
        // Need a dummy connection to create GameSessionScreen
        var connection = new DummyConnection();
        var screen = new GameSessionScreen(connection);

        var result = screen.OnMouseDown(100, 100);

        Assert.Equal(ScreenEvent.None, result);
    }

    [Fact]
    public void MainMenu_Load_disabled_does_not_create_session()
    {
        var screen = new MainMenuScreen();
        var (x, y) = ButtonCenter(MenuLayout.LoadButtonY);

        TriggerRender(screen);

        var result = screen.OnMouseDown(x, y);
        Assert.Equal(ScreenEvent.None, result);
    }

    [Fact]
    public void Multiple_clicks_on_NewGame_both_return_NewGame()
    {
        var screen = new MainMenuScreen();
        var (x, y) = ButtonCenter(MenuLayout.NewGameButtonY);

        TriggerRender(screen);

        var first = screen.OnMouseDown(x, y);
        var second = screen.OnMouseDown(x, y);

        Assert.Equal(ScreenEvent.NewGame, first);
        Assert.Equal(ScreenEvent.NewGame, second);
    }

    /// <summary>
    /// Triggers a render to set the internal screen dimensions used for hit testing.
    /// Uses a CPU-backed bitmap surface since we don't need GPU for hit testing.
    /// </summary>
    private static void TriggerRender(IScreen screen)
    {
        using var bitmap = new SkiaSharp.SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SkiaSharp.SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    private sealed class DummyConnection : DeepSpaceSaga.Contracts.IGameSessionConnection
    {
        public void SendCommand(DeepSpaceSaga.Contracts.Command command) { }

#pragma warning disable CS0067
        public event Action<DeepSpaceSaga.Contracts.AuthoritativeSnapshot>? SnapshotReceived;
#pragma warning restore CS0067
    }
}
