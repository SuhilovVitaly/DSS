using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameMenu;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.MainMenu;
using DeepSpaceSaga.Client.UI.Screens.Settings;
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
    public void MainMenu_Settings_click_returns_OpenSettings()
    {
        var screen = new MainMenuScreen();
        var (x, y) = ButtonCenter(MenuLayout.SettingsY);
        TriggerRender(screen);
        Assert.Equal(ScreenEvent.OpenSettings, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void GameSessionScreen_click_returns_None()
    {
        var buffer = new DeepSpaceSaga.Client.SnapshotBuffer();
        var predictor = new DeepSpaceSaga.Motion.LinearMotionPredictor();
        var screen = new GameSessionScreen(buffer, predictor);
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
        var predictor = new DeepSpaceSaga.Motion.LinearMotionPredictor();
        var screen = new GameSessionScreen(buffer, predictor);
        Assert.Equal(ScreenEvent.OpenGameMenu, screen.OnKeyDown(Key.Escape));
    }

    [Fact]
    public void GameSession_other_key_returns_None()
    {
        var buffer = new DeepSpaceSaga.Client.SnapshotBuffer();
        var predictor = new DeepSpaceSaga.Motion.LinearMotionPredictor();
        var screen = new GameSessionScreen(buffer, predictor);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.A));
    }

    [Fact]
    public void GameMenu_other_key_returns_None()
    {
        var screen = new GameMenuScreen();
        TriggerGameRender(screen);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.A));
    }

    // --- Settings tests ---

    private static float SettingsPanelLeft => SettingsLayout.PanelLeft(ScreenWidth);
    private static float SettingsPanelTop => SettingsLayout.PanelTop(ScreenHeight);

    private static (float x, float y) SettingsButtonCenter(float buttonLocalY)
    {
        float bx = SettingsPanelLeft + (SettingsLayout.PanelWidth - SettingsLayout.ButtonWidth) / 2f;
        return (bx + SettingsLayout.ButtonWidth / 2f,
                SettingsPanelTop + buttonLocalY + SettingsLayout.ButtonHeight / 2f);
    }

    private static void TriggerSettingsRender(IScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    private static readonly string[] TestMonitorNames = { "Monitor 1 (1920x1080)", "Monitor 2 (2560x1440)" };

    private static SettingsScreen NewSettingsScreen(
        int selectedMonitorIndex = 0, Action<int>? onMonitorSelected = null,
        double selectedUiScale = 1.0, Action<double>? onUiScaleSelected = null) =>
        new(TestMonitorNames, selectedMonitorIndex, onMonitorSelected ?? (_ => { }),
            selectedUiScale, onUiScaleSelected ?? (_ => { }));

    private static (float x, float y) SettingsMonitorComboCenter()
    {
        float bx = SettingsPanelLeft + (SettingsLayout.PanelWidth - SettingsLayout.MonitorComboWidth) / 2f;
        return (bx + SettingsLayout.MonitorComboWidth / 2f,
                SettingsPanelTop + SettingsLayout.MonitorComboY + SettingsLayout.MonitorComboHeight / 2f);
    }

    private static (float x, float y) SettingsMonitorOptionCenter(int optionIndex)
    {
        float bx = SettingsPanelLeft + (SettingsLayout.PanelWidth - SettingsLayout.MonitorComboWidth) / 2f;
        float oy = SettingsPanelTop + SettingsLayout.MonitorComboY + SettingsLayout.MonitorComboHeight
            + optionIndex * SettingsLayout.MonitorOptionHeight;
        return (bx + SettingsLayout.MonitorComboWidth / 2f, oy + SettingsLayout.MonitorOptionHeight / 2f);
    }

    [Fact]
    public void Settings_Exit_click_returns_CloseSettings()
    {
        var screen = NewSettingsScreen();
        var (x, y) = SettingsButtonCenter(SettingsLayout.ExitY);
        TriggerSettingsRender(screen);
        Assert.Equal(ScreenEvent.CloseSettings, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void Settings_Esc_returns_CloseSettings()
    {
        var screen = NewSettingsScreen();
        TriggerSettingsRender(screen);
        Assert.Equal(ScreenEvent.CloseSettings, screen.OnKeyDown(Key.Escape));
    }

    [Fact]
    public void Settings_other_key_returns_None()
    {
        var screen = NewSettingsScreen();
        TriggerSettingsRender(screen);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.A));
    }

    [Fact]
    public void Settings_click_outside_button_returns_None()
    {
        var screen = NewSettingsScreen();
        TriggerSettingsRender(screen);
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(0, 0));
    }

    [Fact]
    public void Settings_monitor_combo_click_opens_dropdown_without_screen_event()
    {
        var screen = NewSettingsScreen();
        TriggerSettingsRender(screen);
        var (x, y) = SettingsMonitorComboCenter();
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void Settings_selecting_monitor_option_saves_immediately_and_stays_open()
    {
        int? saved = null;
        var screen = NewSettingsScreen(selectedMonitorIndex: 0, onMonitorSelected: i => saved = i);
        TriggerSettingsRender(screen);

        var (comboX, comboY) = SettingsMonitorComboCenter();
        screen.OnMouseDown(comboX, comboY); // open dropdown

        var (optX, optY) = SettingsMonitorOptionCenter(1);
        var evt = screen.OnMouseDown(optX, optY); // pick "Monitor 2"

        Assert.Equal(ScreenEvent.None, evt);
        Assert.Equal(1, saved);
    }

    [Fact]
    public void Settings_Esc_while_dropdown_open_closes_dropdown_not_screen()
    {
        var screen = NewSettingsScreen();
        TriggerSettingsRender(screen);

        var (comboX, comboY) = SettingsMonitorComboCenter();
        screen.OnMouseDown(comboX, comboY); // open dropdown

        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Escape));
        // Second Escape (dropdown now closed) should close the Settings screen.
        Assert.Equal(ScreenEvent.CloseSettings, screen.OnKeyDown(Key.Escape));
    }

    [Fact]
    public void Settings_reselecting_same_monitor_does_not_invoke_callback()
    {
        int callCount = 0;
        var screen = NewSettingsScreen(selectedMonitorIndex: 0, onMonitorSelected: _ => callCount++);
        TriggerSettingsRender(screen);

        var (comboX, comboY) = SettingsMonitorComboCenter();
        screen.OnMouseDown(comboX, comboY); // open dropdown

        var (optX, optY) = SettingsMonitorOptionCenter(0);
        screen.OnMouseDown(optX, optY); // re-pick the already-selected "Monitor 1"

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void Settings_panel_same_size_and_position_as_MainMenu()
    {
        Assert.Equal(MenuLayout.PanelWidth, SettingsLayout.PanelWidth);
        Assert.Equal(MenuLayout.PanelHeight, SettingsLayout.PanelHeight);
        Assert.Equal(MenuLayout.PanelLeft(ScreenWidth), SettingsLayout.PanelLeft(ScreenWidth));
        Assert.Equal(MenuLayout.PanelTop(ScreenHeight), SettingsLayout.PanelTop(ScreenHeight));
    }

    // --- Settings: UI scale combo (100% / 120% / 150%) ---

    private static (float x, float y) SettingsUiScaleComboCenter()
    {
        float bx = SettingsPanelLeft + (SettingsLayout.PanelWidth - SettingsLayout.UiScaleComboWidth) / 2f;
        return (bx + SettingsLayout.UiScaleComboWidth / 2f,
                SettingsPanelTop + SettingsLayout.UiScaleComboY + SettingsLayout.UiScaleComboHeight / 2f);
    }

    private static (float x, float y) SettingsUiScaleOptionCenter(int optionIndex)
    {
        float bx = SettingsPanelLeft + (SettingsLayout.PanelWidth - SettingsLayout.UiScaleComboWidth) / 2f;
        float oy = SettingsPanelTop + SettingsLayout.UiScaleComboY + SettingsLayout.UiScaleComboHeight
            + optionIndex * SettingsLayout.UiScaleOptionHeight;
        return (bx + SettingsLayout.UiScaleComboWidth / 2f, oy + SettingsLayout.UiScaleOptionHeight / 2f);
    }

    [Fact]
    public void Settings_uiscale_combo_click_opens_dropdown_without_screen_event()
    {
        var screen = NewSettingsScreen();
        TriggerSettingsRender(screen);
        var (x, y) = SettingsUiScaleComboCenter();
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void Settings_selecting_uiscale_option_saves_immediately_and_stays_open()
    {
        double? saved = null;
        var screen = NewSettingsScreen(selectedUiScale: 1.0, onUiScaleSelected: v => saved = v);
        TriggerSettingsRender(screen);

        var (comboX, comboY) = SettingsUiScaleComboCenter();
        screen.OnMouseDown(comboX, comboY); // open dropdown

        var (optX, optY) = SettingsUiScaleOptionCenter(3); // 150%
        var evt = screen.OnMouseDown(optX, optY);

        Assert.Equal(ScreenEvent.None, evt);
        Assert.Equal(1.5, saved);
    }

    [Fact]
    public void Settings_reselecting_same_uiscale_does_not_invoke_callback()
    {
        int callCount = 0;
        var screen = NewSettingsScreen(selectedUiScale: 1.0, onUiScaleSelected: _ => callCount++);
        TriggerSettingsRender(screen);

        var (comboX, comboY) = SettingsUiScaleComboCenter();
        screen.OnMouseDown(comboX, comboY); // open dropdown

        var (optX, optY) = SettingsUiScaleOptionCenter(1); // re-pick already-selected 100%
        screen.OnMouseDown(optX, optY);

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void Settings_selecting_80_percent_saves_immediately()
    {
        double? saved = null;
        var screen = NewSettingsScreen(selectedUiScale: 1.0, onUiScaleSelected: v => saved = v);
        TriggerSettingsRender(screen);

        var (comboX, comboY) = SettingsUiScaleComboCenter();
        screen.OnMouseDown(comboX, comboY); // open dropdown

        var (optX, optY) = SettingsUiScaleOptionCenter(0); // 80%
        var evt = screen.OnMouseDown(optX, optY);

        Assert.Equal(ScreenEvent.None, evt);
        Assert.Equal(0.8, saved);
    }

    [Fact]
    public void Settings_invalid_persisted_uiscale_falls_back_to_100_percent()
    {
        // A corrupted/out-of-set persisted value (e.g. garbage or a removed option)
        // must never break Settings — it falls back to 100% (index of 1.0), so
        // re-picking the 100% slot is a no-op while picking any other slot still fires.
        int callCount = 0;
        var screen = NewSettingsScreen(selectedUiScale: 0.95, onUiScaleSelected: _ => callCount++);
        TriggerSettingsRender(screen);

        var (comboX, comboY) = SettingsUiScaleComboCenter();
        screen.OnMouseDown(comboX, comboY); // open dropdown

        var (optX, optY) = SettingsUiScaleOptionCenter(1); // 100% slot — already selected via fallback
        screen.OnMouseDown(optX, optY);

        Assert.Equal(0, callCount);
    }

}
