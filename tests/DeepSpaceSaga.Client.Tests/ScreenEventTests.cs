using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameMenu;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.Load;
using DeepSpaceSaga.Client.UI.Screens.MainMenu;
using DeepSpaceSaga.Client.UI.Screens.Save;
using DeepSpaceSaga.Client.UI.Screens.ScenarioSelect;
using DeepSpaceSaga.Client.UI.Screens.Settings;
using DeepSpaceSaga.Contracts;
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
    public void MainMenu_Load_click_returns_OpenLoadWindow()
    {
        var screen = new MainMenuScreen();
        var (x, y) = ButtonCenter(MenuLayout.LoadY);
        TriggerRender(screen);
        Assert.Equal(ScreenEvent.OpenLoadWindow, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void MainMenu_Load_hover_is_reported_interactive()
    {
        // Load is no longer suppressed/disabled — hovering it behaves like every
        // other MainMenu button (used by SkiaWindow to pick the interactive cursor).
        var screen = new MainMenuScreen();
        var (x, y) = ButtonCenter(MenuLayout.LoadY);
        TriggerRender(screen);
        Assert.True(screen.OnMouseMove(x, y));
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
    public void GameMenu_Save_click_returns_OpenSaveWindow()
    {
        var screen = new GameMenuScreen();
        var (x, y) = GameButtonCenter(GameMenuLayout.SaveY);
        TriggerGameRender(screen);
        Assert.Equal(ScreenEvent.OpenSaveWindow, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void GameMenu_Load_click_returns_OpenLoadWindow()
    {
        var screen = new GameMenuScreen();
        var (x, y) = GameButtonCenter(GameMenuLayout.LoadY);
        TriggerGameRender(screen);
        Assert.Equal(ScreenEvent.OpenLoadWindow, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void GameMenu_Load_hover_is_reported_interactive()
    {
        var screen = new GameMenuScreen();
        var (x, y) = GameButtonCenter(GameMenuLayout.LoadY);
        TriggerGameRender(screen);
        Assert.True(screen.OnMouseMove(x, y));
    }

    [Fact]
    public void GameMenu_former_close_area_is_not_interactive()
    {
        var screen = new GameMenuScreen();
        TriggerGameRender(screen);
        var panel = GameMenuLayout.PanelRect(ScreenWidth, ScreenHeight);
        float x = panel.Right - 34f;
        float y = panel.Top + 36f;
        Assert.False(screen.OnMouseMove(x, y));
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void GameMenu_Settings_is_disabled_for_hover_and_click()
    {
        var screen = new GameMenuScreen();
        var (x, y) = GameButtonCenter(GameMenuLayout.SettingsY);
        TriggerGameRender(screen);
        Assert.False(screen.OnMouseMove(x, y));
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void GameMenu_right_click_does_not_activate_controls()
    {
        var screen = new GameMenuScreen();
        var (x, y) = GameButtonCenter(GameMenuLayout.ResumeY);
        TriggerGameRender(screen);
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(x, y, MouseButton.Right));
    }

    [Fact]
    public void GameMenu_click_outside_panel_does_nothing()
    {
        var screen = new GameMenuScreen();
        TriggerGameRender(screen);
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(0, 0));
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
        double selectedUiScale = 1.0, Action<double>? onUiScaleSelected = null,
        string selectedLanguage = "English", Action<string>? onLanguageSelected = null) =>
        new(TestMonitorNames, selectedMonitorIndex, onMonitorSelected ?? (_ => { }),
            selectedUiScale, onUiScaleSelected ?? (_ => { }),
            selectedLanguage, onLanguageSelected ?? (_ => { }));

    private static (float x, float y) SettingsMonitorComboCenter()
    {
        float bx = SettingsPanelLeft + SettingsLayout.RowRightX - SettingsLayout.MonitorComboWidth;
        return (bx + SettingsLayout.MonitorComboWidth / 2f,
                SettingsPanelTop + SettingsLayout.MonitorRowY + SettingsLayout.MonitorComboHeight / 2f);
    }

    private static (float x, float y) SettingsMonitorOptionCenter(int optionIndex)
    {
        float bx = SettingsPanelLeft + SettingsLayout.RowRightX - SettingsLayout.MonitorComboWidth;
        float oy = SettingsPanelTop + SettingsLayout.MonitorRowY + SettingsLayout.MonitorComboHeight
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
        float bx = SettingsPanelLeft + SettingsLayout.RowRightX - SettingsLayout.UiScaleComboWidth;
        return (bx + SettingsLayout.UiScaleComboWidth / 2f,
                SettingsPanelTop + SettingsLayout.UiScaleRowY + SettingsLayout.UiScaleComboHeight / 2f);
    }

    private static (float x, float y) SettingsUiScaleOptionCenter(int optionIndex)
    {
        float bx = SettingsPanelLeft + SettingsLayout.RowRightX - SettingsLayout.UiScaleComboWidth;
        float oy = SettingsPanelTop + SettingsLayout.UiScaleRowY + SettingsLayout.UiScaleComboHeight
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

    // --- Save tests ---

    private static float SavePanelLeft => SaveLayout.PanelLeft(ScreenWidth);
    private static float SavePanelTop => SaveLayout.PanelTop(ScreenHeight);

    private static (float x, float y) SaveCenter((float X, float Y, float W, float H) local) =>
        (SavePanelLeft + local.X + local.W / 2f, SavePanelTop + local.Y + local.H / 2f);

    private static void TriggerSaveRender(IScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    private static SaveSlotInfo Slot(string id, string displayName, DateTime savedAtUtc) =>
        new(id, displayName, savedAtUtc);

    private static SaveScreen NewSaveScreen(
        IReadOnlyList<SaveSlotInfo>? slots = null,
        Action<string>? saveSlot = null,
        Action<string>? deleteSlot = null,
        Func<long>? nowMs = null)
    {
        var currentSlots = slots ?? Array.Empty<SaveSlotInfo>();
        return new SaveScreen(
            () => currentSlots,
            saveSlot ?? (_ => { }),
            deleteSlot ?? (_ => { }),
            nowMs);
    }

    [Fact]
    public void Save_Close_click_returns_CloseSaveWindow()
    {
        var screen = NewSaveScreen();
        TriggerSaveRender(screen);
        var (x, y) = SaveCenter(SaveLayout.CloseButtonRect());
        Assert.Equal(ScreenEvent.CloseSaveWindow, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void Save_Esc_returns_CloseSaveWindow()
    {
        var screen = NewSaveScreen();
        TriggerSaveRender(screen);
        Assert.Equal(ScreenEvent.CloseSaveWindow, screen.OnKeyDown(Key.Escape));
    }

    [Fact]
    public void Save_other_key_returns_None()
    {
        var screen = NewSaveScreen();
        TriggerSaveRender(screen);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.A));
    }

    [Fact]
    public void Save_click_outside_button_returns_None()
    {
        var screen = NewSaveScreen();
        TriggerSaveRender(screen);
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(0, 0));
    }

    [Fact]
    public void Save_NewSave_click_activates_name_entry_without_screen_event()
    {
        var screen = NewSaveScreen();
        TriggerSaveRender(screen);
        var (x, y) = SaveCenter(SaveLayout.NewSaveButtonRect());
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void Save_Esc_while_new_save_active_cancels_without_closing_window()
    {
        var screen = NewSaveScreen();
        TriggerSaveRender(screen);

        var (nx, ny) = SaveCenter(SaveLayout.NewSaveButtonRect());
        screen.OnMouseDown(nx, ny); // activate name entry

        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Escape));
        // Save window itself is still open — a second Escape now closes it.
        Assert.Equal(ScreenEvent.CloseSaveWindow, screen.OnKeyDown(Key.Escape));
    }

    [Fact]
    public void Save_ConfirmNewSave_with_empty_name_does_not_call_saveSlot()
    {
        bool called = false;
        var screen = NewSaveScreen(saveSlot: _ => called = true);
        TriggerSaveRender(screen);

        var (nx, ny) = SaveCenter(SaveLayout.NewSaveButtonRect());
        screen.OnMouseDown(nx, ny);
        TriggerSaveRender(screen);

        var (cx, cy) = SaveCenter(SaveLayout.ConfirmButtonRect());
        var evt = screen.OnMouseDown(cx, cy);

        Assert.Equal(ScreenEvent.None, evt);
        Assert.False(called);
    }

    [Fact]
    public void Save_ConfirmNewSave_with_typed_name_calls_saveSlot_with_that_name()
    {
        string? savedName = null;
        var screen = NewSaveScreen(saveSlot: id => savedName = id);
        TriggerSaveRender(screen);

        var (nx, ny) = SaveCenter(SaveLayout.NewSaveButtonRect());
        screen.OnMouseDown(nx, ny);
        TriggerSaveRender(screen);

        foreach (char c in "My Save")
            screen.OnTextInput(c);

        var (cx, cy) = SaveCenter(SaveLayout.ConfirmButtonRect());
        var evt = screen.OnMouseDown(cx, cy);

        Assert.Equal(ScreenEvent.None, evt);
        Assert.Equal("My Save", savedName);
    }

    [Fact]
    public void Save_ConfirmNewSave_reserved_quicksave_name_does_not_call_saveSlot()
    {
        // Must be rejected even though "quicksave" is NOT in the current slot list (e.g.
        // F5 has never been pressed yet) — the reserved-name check is unconditional, not
        // just a side effect of the ordinary duplicate-in-list check.
        bool called = false;
        var screen = NewSaveScreen(saveSlot: _ => called = true);
        TriggerSaveRender(screen);

        var (nx, ny) = SaveCenter(SaveLayout.NewSaveButtonRect());
        screen.OnMouseDown(nx, ny);
        TriggerSaveRender(screen);

        foreach (char c in "quicksave")
            screen.OnTextInput(c);

        var (cx, cy) = SaveCenter(SaveLayout.ConfirmButtonRect());
        var evt = screen.OnMouseDown(cx, cy);

        Assert.Equal(ScreenEvent.None, evt);
        Assert.False(called);
    }

    [Fact]
    public void Save_ConfirmNewSave_reserved_quicksave_name_is_rejected_case_insensitively()
    {
        bool called = false;
        var screen = NewSaveScreen(saveSlot: _ => called = true);
        TriggerSaveRender(screen);

        var (nx, ny) = SaveCenter(SaveLayout.NewSaveButtonRect());
        screen.OnMouseDown(nx, ny);
        TriggerSaveRender(screen);

        foreach (char c in "QuickSave")
            screen.OnTextInput(c);

        var (cx, cy) = SaveCenter(SaveLayout.ConfirmButtonRect());
        screen.OnMouseDown(cx, cy);

        Assert.False(called);
    }

    [Fact]
    public void Save_ConfirmNewSave_duplicate_name_first_click_arms_overwrite_without_calling_saveSlot()
    {
        // First SAVE click on a duplicate name only arms the overwrite confirm — it
        // must not overwrite yet (mirrors the per-row DELETE button's first click).
        bool called = false;
        var slots = new[] { Slot("Existing", "Existing", DateTime.UtcNow) };
        var screen = NewSaveScreen(slots: slots, saveSlot: _ => called = true);
        TriggerSaveRender(screen);

        var (nx, ny) = SaveCenter(SaveLayout.NewSaveButtonRect());
        screen.OnMouseDown(nx, ny);
        TriggerSaveRender(screen);

        foreach (char c in "Existing")
            screen.OnTextInput(c);

        var (cx, cy) = SaveCenter(SaveLayout.ConfirmButtonRect());
        screen.OnMouseDown(cx, cy);

        Assert.False(called);
    }

    [Fact]
    public void Save_ConfirmNewSave_duplicate_name_second_click_within_window_overwrites()
    {
        string? saved = null;
        long fakeNow = 0;
        var slots = new[] { Slot("Existing", "Existing", DateTime.UtcNow) };
        var screen = NewSaveScreen(slots: slots, saveSlot: id => saved = id, nowMs: () => fakeNow);
        TriggerSaveRender(screen);

        var (nx, ny) = SaveCenter(SaveLayout.NewSaveButtonRect());
        screen.OnMouseDown(nx, ny);
        TriggerSaveRender(screen);

        foreach (char c in "Existing")
            screen.OnTextInput(c);

        var (cx, cy) = SaveCenter(SaveLayout.ConfirmButtonRect());
        screen.OnMouseDown(cx, cy); // first click → arms overwrite
        fakeNow += 500;
        var evt = screen.OnMouseDown(cx, cy); // second click within window → overwrites

        Assert.Equal(ScreenEvent.None, evt);
        Assert.Equal("Existing", saved);
    }

    [Fact]
    public void Save_ConfirmNewSave_duplicate_name_second_click_after_timeout_re_arms_instead_of_overwriting()
    {
        bool called = false;
        long fakeNow = 0;
        var slots = new[] { Slot("Existing", "Existing", DateTime.UtcNow) };
        var screen = NewSaveScreen(slots: slots, saveSlot: _ => called = true, nowMs: () => fakeNow);
        TriggerSaveRender(screen);

        var (nx, ny) = SaveCenter(SaveLayout.NewSaveButtonRect());
        screen.OnMouseDown(nx, ny);
        TriggerSaveRender(screen);

        foreach (char c in "Existing")
            screen.OnTextInput(c);

        var (cx, cy) = SaveCenter(SaveLayout.ConfirmButtonRect());
        screen.OnMouseDown(cx, cy); // first click → arms overwrite
        fakeNow += 5000; // past the confirm window
        screen.OnMouseDown(cx, cy); // treated as a fresh first click, not an overwrite

        Assert.False(called);
    }

    [Fact]
    public void Save_CancelNewSave_clears_pending_overwrite_so_reopening_requires_arming_again()
    {
        bool called = false;
        var slots = new[] { Slot("Existing", "Existing", DateTime.UtcNow) };
        var screen = NewSaveScreen(slots: slots, saveSlot: _ => called = true);
        TriggerSaveRender(screen);

        var (nx, ny) = SaveCenter(SaveLayout.NewSaveButtonRect());
        var (cx, cy) = SaveCenter(SaveLayout.ConfirmButtonRect());
        var (canx, cany) = SaveCenter(SaveLayout.CancelButtonRect());

        screen.OnMouseDown(nx, ny); // open name entry
        TriggerSaveRender(screen);
        foreach (char c in "Existing")
            screen.OnTextInput(c);
        screen.OnMouseDown(cx, cy); // first click → arms overwrite

        screen.OnMouseDown(canx, cany); // cancel discards the armed state
        TriggerSaveRender(screen);

        screen.OnMouseDown(nx, ny); // reopen name entry
        TriggerSaveRender(screen);
        foreach (char c in "Existing")
            screen.OnTextInput(c);
        screen.OnMouseDown(cx, cy); // this must be a fresh first click again, not an overwrite

        Assert.False(called);
    }

    [Fact]
    public void Save_CancelNewSave_click_discards_typed_name()
    {
        bool called = false;
        var screen = NewSaveScreen(saveSlot: _ => called = true);
        TriggerSaveRender(screen);

        var (nx, ny) = SaveCenter(SaveLayout.NewSaveButtonRect());
        screen.OnMouseDown(nx, ny);
        TriggerSaveRender(screen);

        foreach (char c in "Whatever")
            screen.OnTextInput(c);

        var (canx, cany) = SaveCenter(SaveLayout.CancelButtonRect());
        var evt = screen.OnMouseDown(canx, cany);
        TriggerSaveRender(screen);

        Assert.Equal(ScreenEvent.None, evt);

        // Back to collapsed state — clicking NEW SAVE again starts from empty text.
        var (nx2, ny2) = SaveCenter(SaveLayout.NewSaveButtonRect());
        screen.OnMouseDown(nx2, ny2);
        var (cx, cy) = SaveCenter(SaveLayout.ConfirmButtonRect());
        screen.OnMouseDown(cx, cy); // empty name → rejected, saveSlot still never called

        Assert.False(called);
    }

    [Fact]
    public void Save_Overwrite_click_calls_saveSlot_with_the_row_SlotId()
    {
        string? saved = null;
        var slots = new[] { Slot("slot-a", "Slot A", DateTime.UtcNow) };
        var screen = NewSaveScreen(slots: slots, saveSlot: id => saved = id);
        TriggerSaveRender(screen);

        var (x, y) = SaveCenter(SaveLayout.OverwriteButtonRect(0));
        var evt = screen.OnMouseDown(x, y);

        Assert.Equal(ScreenEvent.None, evt);
        Assert.Equal("slot-a", saved);
    }

    [Fact]
    public void Save_Delete_first_click_does_not_delete()
    {
        bool called = false;
        var slots = new[] { Slot("slot-a", "Slot A", DateTime.UtcNow) };
        var screen = NewSaveScreen(slots: slots, deleteSlot: _ => called = true);
        TriggerSaveRender(screen);

        var (x, y) = SaveCenter(SaveLayout.DeleteButtonRect(0));
        var evt = screen.OnMouseDown(x, y);

        Assert.Equal(ScreenEvent.None, evt);
        Assert.False(called);
    }

    [Fact]
    public void Save_Delete_second_click_within_window_deletes()
    {
        string? deleted = null;
        long fakeNow = 0;
        var slots = new[] { Slot("slot-a", "Slot A", DateTime.UtcNow) };
        var screen = NewSaveScreen(slots: slots, deleteSlot: id => deleted = id, nowMs: () => fakeNow);
        TriggerSaveRender(screen);

        var (x, y) = SaveCenter(SaveLayout.DeleteButtonRect(0));
        screen.OnMouseDown(x, y); // first click → CONFIRM
        fakeNow += 500;
        var evt = screen.OnMouseDown(x, y); // second click within window → deletes

        Assert.Equal(ScreenEvent.None, evt);
        Assert.Equal("slot-a", deleted);
    }

    [Fact]
    public void Save_Delete_second_click_after_timeout_does_not_delete()
    {
        bool called = false;
        long fakeNow = 0;
        var slots = new[] { Slot("slot-a", "Slot A", DateTime.UtcNow) };
        var screen = NewSaveScreen(slots: slots, deleteSlot: _ => called = true, nowMs: () => fakeNow);
        TriggerSaveRender(screen);

        var (x, y) = SaveCenter(SaveLayout.DeleteButtonRect(0));
        screen.OnMouseDown(x, y); // first click → CONFIRM
        fakeNow += 5000; // past the confirm window
        screen.OnMouseDown(x, y); // treated as a fresh first click, not a delete

        Assert.False(called);
    }

    [Fact]
    public void Save_Delete_click_elsewhere_clears_confirm_state()
    {
        bool called = false;
        var slots = new[]
        {
            Slot("slot-a", "Slot A", DateTime.UtcNow),
            Slot("slot-b", "Slot B", DateTime.UtcNow)
        };
        var screen = NewSaveScreen(slots: slots, deleteSlot: _ => called = true);
        TriggerSaveRender(screen);

        var (dx, dy) = SaveCenter(SaveLayout.DeleteButtonRect(0));
        screen.OnMouseDown(dx, dy); // first click on row 0 → CONFIRM

        var (ox, oy) = SaveCenter(SaveLayout.OverwriteButtonRect(1));
        screen.OnMouseDown(ox, oy); // click elsewhere clears the confirm state

        var evt = screen.OnMouseDown(dx, dy); // this is now a fresh first click again

        Assert.Equal(ScreenEvent.None, evt);
        Assert.False(called);
    }

    [Fact]
    public void Save_Delete_click_on_different_row_clears_previous_confirm_and_arms_new_row()
    {
        // Regression for clicking DELETE on a *different* row while another row is
        // already armed (as opposed to clicking Overwrite on a different row, already
        // covered by Save_Delete_click_elsewhere_clears_confirm_state above). The single
        // _deleteConfirmIndex field must be reset to the newly clicked row, not left
        // pointing at the old one and not treated as that old row's confirming click.
        string? deleted = null;
        long fakeNow = 0;
        var slots = new[]
        {
            Slot("slot-a", "Slot A", DateTime.UtcNow),
            Slot("slot-b", "Slot B", DateTime.UtcNow)
        };
        var screen = NewSaveScreen(slots: slots, deleteSlot: id => deleted = id, nowMs: () => fakeNow);
        TriggerSaveRender(screen);

        var (d0x, d0y) = SaveCenter(SaveLayout.DeleteButtonRect(0));
        var (d1x, d1y) = SaveCenter(SaveLayout.DeleteButtonRect(1));

        screen.OnMouseDown(d0x, d0y); // first click on row 0 → CONFIRM armed for row 0

        // Clicking DELETE on a different row (row 1) must not delete either row: it's a
        // fresh first click for row 1, and it must clear row 0's armed state.
        var evt = screen.OnMouseDown(d1x, d1y);
        Assert.Equal(ScreenEvent.None, evt);
        Assert.Null(deleted);

        // Row 0's arm was cleared by the click above: clicking it again now is a fresh
        // first click too (not a delete), even though it's within the confirm window.
        fakeNow += 500;
        var evtRow0Again = screen.OnMouseDown(d0x, d0y);
        Assert.Equal(ScreenEvent.None, evtRow0Again);
        Assert.Null(deleted);

        // Row 1's own arm/confirm cycle still works correctly from a fresh start.
        fakeNow += 100;
        screen.OnMouseDown(d1x, d1y); // fresh first click on row 1 (clears row 0's re-arm)
        fakeNow += 500;
        var evtRow1Confirm = screen.OnMouseDown(d1x, d1y); // second click within window → deletes

        Assert.Equal(ScreenEvent.None, evtRow1Confirm);
        Assert.Equal("slot-b", deleted);
    }

    [Fact]
    public void Save_NotifySaveCompleted_refreshes_the_slot_list()
    {
        // Mirrors what SkiaWindow.SaveToSlotAsync calls once a fire-and-forget New
        // Save/Overwrite write completes, so the still-open window's list reflects it
        // without the click itself having blocked for the save's duration.
        var currentSlots = new List<SaveSlotInfo> { Slot("slot-a", "Slot A", DateTime.UtcNow) };
        string? saved = null;
        var screen = new SaveScreen(() => currentSlots, id => saved = id, _ => { });
        TriggerSaveRender(screen);

        // Before the notify, row index 1 doesn't exist yet — clicking there hits nothing.
        var (x, y) = SaveCenter(SaveLayout.OverwriteButtonRect(1));
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(x, y));
        Assert.Null(saved);

        currentSlots.Add(Slot("slot-b", "Slot B", DateTime.UtcNow));
        var exception = Record.Exception(() => screen.NotifySaveCompleted("slot-b"));
        Assert.Null(exception);
        TriggerSaveRender(screen);

        // The newly-appeared row is now clickable, proving RefreshSlots() picked up the update.
        screen.OnMouseDown(x, y);
        Assert.Equal("slot-b", saved);
    }

    [Fact]
    public void Save_MouseWheel_scrolls_row_zero_through_overflowing_slots_and_clamps_at_both_ends()
    {
        int extra = 3;
        var slots = Enumerable.Range(0, SaveLayout.VisibleRows + extra)
            .Select(i => Slot($"slot-{i}", $"Slot {i}", DateTime.UtcNow))
            .ToArray();

        var (ox, oy) = SaveCenter(SaveLayout.OverwriteButtonRect(0));

        string? saved = null;
        var screenWithCallback = NewSaveScreen(slots: slots, saveSlot: id => saved = id);
        TriggerSaveRender(screenWithCallback);

        screenWithCallback.OnMouseDown(ox, oy);
        Assert.Equal("slot-0", saved);

        for (int i = 0; i < extra; i++)
            screenWithCallback.OnMouseWheel(0, 0, -1); // scroll down one row at a time

        TriggerSaveRender(screenWithCallback);
        screenWithCallback.OnMouseDown(ox, oy);
        Assert.Equal($"slot-{extra}", saved);

        // Further downward scrolling past the last row is clamped, not out of range.
        screenWithCallback.OnMouseWheel(0, 0, -1);
        TriggerSaveRender(screenWithCallback);
        screenWithCallback.OnMouseDown(ox, oy);
        Assert.Equal($"slot-{extra}", saved);

        // Scrolling back up returns to (and clamps at) the top.
        for (int i = 0; i < extra + 1; i++)
            screenWithCallback.OnMouseWheel(0, 0, 1);
        TriggerSaveRender(screenWithCallback);
        screenWithCallback.OnMouseDown(ox, oy);
        Assert.Equal("slot-0", saved);
    }

    // --- Load tests ---

    private static float LoadPanelLeft => LoadLayout.PanelLeft(ScreenWidth);
    private static float LoadPanelTop => LoadLayout.PanelTop(ScreenHeight);

    private static (float x, float y) LoadCenter((float X, float Y, float W, float H) local) =>
        (LoadPanelLeft + local.X + local.W / 2f, LoadPanelTop + local.Y + local.H / 2f);

    private static void TriggerLoadRender(IScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    private static LoadScreen NewLoadScreen(
        IReadOnlyList<SaveSlotInfo>? slots = null,
        Action<string>? deleteSlot = null,
        Func<long>? nowMs = null)
    {
        var currentSlots = slots ?? Array.Empty<SaveSlotInfo>();
        return new LoadScreen(() => currentSlots, deleteSlot ?? (_ => { }), nowMs);
    }

    [Fact]
    public void Load_background_image_is_loaded()
    {
        // Regression: the window-background-700x600.png asset must resolve at the
        // client's working directory and be registered in the .csproj with
        // CopyToOutputDirectory, or the panel silently falls back to a plain fill.
        Assert.True(LoadScreen.HasLoadedBackground);
    }

    [Fact]
    public void Load_Close_click_returns_CloseLoadWindow()
    {
        var screen = NewLoadScreen();
        TriggerLoadRender(screen);
        var (x, y) = LoadCenter(LoadLayout.CloseButtonRect());
        Assert.Equal(ScreenEvent.CloseLoadWindow, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void Load_Esc_returns_CloseLoadWindow()
    {
        var screen = NewLoadScreen();
        TriggerLoadRender(screen);
        Assert.Equal(ScreenEvent.CloseLoadWindow, screen.OnKeyDown(Key.Escape));
    }

    [Fact]
    public void Load_other_key_returns_None()
    {
        var screen = NewLoadScreen();
        TriggerLoadRender(screen);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.A));
    }

    [Fact]
    public void Load_click_outside_button_returns_None()
    {
        var screen = NewLoadScreen();
        TriggerLoadRender(screen);
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(0, 0));
    }

    [Fact]
    public void Load_first_slot_is_selected_by_default_so_LOAD_works_without_a_row_click()
    {
        var slots = new[] { Slot("slot-a", "Slot A", DateTime.UtcNow) };
        var screen = NewLoadScreen(slots: slots);
        TriggerLoadRender(screen);

        var (x, y) = LoadCenter(LoadLayout.LoadButtonRect());
        var evt = screen.OnMouseDown(x, y);

        Assert.Equal(ScreenEvent.LoadSlotRequested, evt);
        // Mirrors how SkiaWindow's mouse-dispatch site reads this: synchronously, in the
        // same call frame, immediately after OnMouseDown returns.
        Assert.Equal("slot-a", screen.LastRequestedSlotId);
    }

    [Fact]
    public void Load_LOAD_with_no_slots_returns_None()
    {
        var screen = NewLoadScreen();
        TriggerLoadRender(screen);

        var (x, y) = LoadCenter(LoadLayout.LoadButtonRect());
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void Load_clicking_a_row_selects_it_without_loading()
    {
        var slots = new[]
        {
            Slot("slot-a", "Slot A", DateTime.UtcNow),
            Slot("slot-b", "Slot B", DateTime.UtcNow)
        };
        var screen = NewLoadScreen(slots: slots);
        TriggerLoadRender(screen);

        var (x, y) = LoadCenter(LoadLayout.RowRect(1));
        var evt = screen.OnMouseDown(x, y);

        Assert.Equal(ScreenEvent.None, evt);
        Assert.Null(screen.LastRequestedSlotId);
    }

    [Fact]
    public void Load_LOAD_click_loads_whichever_row_was_last_selected()
    {
        var slots = new[]
        {
            Slot("slot-a", "Slot A", DateTime.UtcNow),
            Slot("slot-b", "Slot B", DateTime.UtcNow)
        };
        var screen = NewLoadScreen(slots: slots);
        TriggerLoadRender(screen);

        var (rowX, rowY) = LoadCenter(LoadLayout.RowRect(1));
        screen.OnMouseDown(rowX, rowY);

        var (loadX, loadY) = LoadCenter(LoadLayout.LoadButtonRect());
        var evt = screen.OnMouseDown(loadX, loadY);

        Assert.Equal(ScreenEvent.LoadSlotRequested, evt);
        Assert.Equal("slot-b", screen.LastRequestedSlotId);
    }

    [Fact]
    public void Load_reserved_quicksave_slot_is_not_shown_when_caller_excludes_it()
    {
        // LoadScreen doesn't require the caller to include the reserved slot — if it
        // happens to be pre-filtered out, the remaining slots are still all that's
        // selectable/loadable.
        // (In production, SkiaWindow.OpenLoadWindowAsync no longer filters it out — see
        // the quicksave-protection tests below — but LoadScreen still checks the id
        // directly to protect it from deletion regardless of what the caller passes.)
        var slots = SaveSlots.ExcludeReserved(new[]
        {
            Slot(SaveSlots.Quicksave, "Quicksave", DateTime.UtcNow),
            Slot("slot-a", "Slot A", DateTime.UtcNow)
        });
        var screen = NewLoadScreen(slots: slots);
        TriggerLoadRender(screen);

        // Only one row exists (the reserved slot was excluded before construction) — the
        // default selection (row 0) is "slot-a".
        var (x, y) = LoadCenter(LoadLayout.LoadButtonRect());
        var evt = screen.OnMouseDown(x, y);

        Assert.Equal(ScreenEvent.LoadSlotRequested, evt);
        Assert.Equal("slot-a", screen.LastRequestedSlotId);
    }

    [Fact]
    public void Load_quicksave_slot_is_shown_and_can_be_loaded()
    {
        var slots = new[]
        {
            Slot(SaveSlots.Quicksave, "quicksave", DateTime.UtcNow),
            Slot("slot-a", "Slot A", DateTime.UtcNow)
        };
        var screen = NewLoadScreen(slots: slots);
        TriggerLoadRender(screen);

        // Default selection is row 0 — the quicksave slot.
        var (x, y) = LoadCenter(LoadLayout.LoadButtonRect());
        var evt = screen.OnMouseDown(x, y);

        Assert.Equal(ScreenEvent.LoadSlotRequested, evt);
        Assert.Equal(SaveSlots.Quicksave, screen.LastRequestedSlotId);
    }

    [Fact]
    public void Load_quicksave_slot_delete_click_is_a_no_op_even_after_two_clicks()
    {
        bool called = false;
        long fakeNow = 0;
        var slots = new[] { Slot(SaveSlots.Quicksave, "quicksave", DateTime.UtcNow) };
        var screen = NewLoadScreen(slots: slots, deleteSlot: _ => called = true, nowMs: () => fakeNow);
        TriggerLoadRender(screen);

        var (x, y) = LoadCenter(LoadLayout.DeleteButtonRect());
        screen.OnMouseDown(x, y); // would arm CONFIRM for an ordinary slot
        fakeNow += 500;
        var evt = screen.OnMouseDown(x, y); // would delete an ordinary slot on this second click

        Assert.Equal(ScreenEvent.None, evt);
        Assert.False(called);
    }

    [Fact]
    public void Load_Delete_first_click_does_not_delete()
    {
        bool called = false;
        var slots = new[] { Slot("slot-a", "Slot A", DateTime.UtcNow) };
        var screen = NewLoadScreen(slots: slots, deleteSlot: _ => called = true);
        TriggerLoadRender(screen);

        var (x, y) = LoadCenter(LoadLayout.DeleteButtonRect());
        var evt = screen.OnMouseDown(x, y);

        Assert.Equal(ScreenEvent.None, evt);
        Assert.False(called);
    }

    [Fact]
    public void Load_Delete_second_click_within_window_deletes()
    {
        string? deleted = null;
        long fakeNow = 0;
        var slots = new[] { Slot("slot-a", "Slot A", DateTime.UtcNow) };
        var screen = NewLoadScreen(slots: slots, deleteSlot: id => deleted = id, nowMs: () => fakeNow);
        TriggerLoadRender(screen);

        var (x, y) = LoadCenter(LoadLayout.DeleteButtonRect());
        screen.OnMouseDown(x, y); // first click → CONFIRM
        fakeNow += 500;
        var evt = screen.OnMouseDown(x, y); // second click within window → deletes

        Assert.Equal(ScreenEvent.None, evt);
        Assert.Equal("slot-a", deleted);
    }

    [Fact]
    public void Load_Delete_second_click_after_timeout_does_not_delete()
    {
        bool called = false;
        long fakeNow = 0;
        var slots = new[] { Slot("slot-a", "Slot A", DateTime.UtcNow) };
        var screen = NewLoadScreen(slots: slots, deleteSlot: _ => called = true, nowMs: () => fakeNow);
        TriggerLoadRender(screen);

        var (x, y) = LoadCenter(LoadLayout.DeleteButtonRect());
        screen.OnMouseDown(x, y); // first click → CONFIRM
        fakeNow += 5000; // past the confirm window
        screen.OnMouseDown(x, y); // treated as a fresh first click, not a delete

        Assert.False(called);
    }

    [Fact]
    public void Load_Delete_click_elsewhere_clears_confirm_state()
    {
        bool called = false;
        var slots = new[] { Slot("slot-a", "Slot A", DateTime.UtcNow) };
        var screen = NewLoadScreen(slots: slots, deleteSlot: _ => called = true);
        TriggerLoadRender(screen);

        var (dx, dy) = LoadCenter(LoadLayout.DeleteButtonRect());
        screen.OnMouseDown(dx, dy); // first click → CONFIRM

        var (lx, ly) = LoadCenter(LoadLayout.LoadButtonRect());
        screen.OnMouseDown(lx, ly); // click elsewhere (a LOAD click) clears the confirm state

        var evt = screen.OnMouseDown(dx, dy); // this is now a fresh first click again

        Assert.Equal(ScreenEvent.None, evt);
        Assert.False(called);
    }

    [Fact]
    public void Load_Delete_click_after_selecting_a_different_row_clears_previous_confirm_and_arms_new_row()
    {
        // Regression mirroring the old per-row
        // Load_Delete_click_on_different_row_clears_previous_confirm_and_arms_new_row —
        // "different row" is now expressed as a Row click changing the selection between
        // DELETE clicks, since DELETE itself is a single button acting on the selection.
        string? deleted = null;
        long fakeNow = 0;
        var slots = new[]
        {
            Slot("slot-a", "Slot A", DateTime.UtcNow),
            Slot("slot-b", "Slot B", DateTime.UtcNow)
        };
        var screen = NewLoadScreen(slots: slots, deleteSlot: id => deleted = id, nowMs: () => fakeNow);
        TriggerLoadRender(screen);

        var (deleteX, deleteY) = LoadCenter(LoadLayout.DeleteButtonRect());
        var (row1X, row1Y) = LoadCenter(LoadLayout.RowRect(1));

        screen.OnMouseDown(deleteX, deleteY); // first click on row 0 (default selection) → CONFIRM armed

        screen.OnMouseDown(row1X, row1Y); // select row 1 instead — clears row 0's armed confirm
        fakeNow += 500;
        var evtRow1FirstClick = screen.OnMouseDown(deleteX, deleteY); // fresh first click on row 1
        Assert.Equal(ScreenEvent.None, evtRow1FirstClick);
        Assert.Null(deleted);

        fakeNow += 500;
        var evtRow1Confirm = screen.OnMouseDown(deleteX, deleteY); // second click within window → deletes
        Assert.Equal(ScreenEvent.None, evtRow1Confirm);
        Assert.Equal("slot-b", deleted);
    }

    [Fact]
    public void Load_row_click_after_a_pending_delete_confirm_still_loads_and_clears_confirm()
    {
        // A LOAD click while a DELETE confirm is armed for the currently selected row must
        // not be swallowed by the delete state machine — it clears the confirm and proceeds
        // to request a load, exactly like an Overwrite click does on Save's screen.
        string? deleted = null;
        var slots = new[] { Slot("slot-a", "Slot A", DateTime.UtcNow) };
        var screen = NewLoadScreen(slots: slots, deleteSlot: id => deleted = id);
        TriggerLoadRender(screen);

        var (dx, dy) = LoadCenter(LoadLayout.DeleteButtonRect());
        screen.OnMouseDown(dx, dy); // first click → CONFIRM armed

        var (lx, ly) = LoadCenter(LoadLayout.LoadButtonRect());
        var evt = screen.OnMouseDown(lx, ly);

        Assert.Equal(ScreenEvent.LoadSlotRequested, evt);
        Assert.Equal("slot-a", screen.LastRequestedSlotId);
        Assert.Null(deleted);
    }

    [Fact]
    public void Load_MouseWheel_scrolls_row_zero_through_overflowing_slots_and_clamps_at_both_ends()
    {
        // VisibleRows + 3 slots so there's exactly 3 steps of scroll room.
        int extra = 3;
        var slots = Enumerable.Range(0, LoadLayout.VisibleRows + extra)
            .Select(i => Slot($"slot-{i}", $"Slot {i}", DateTime.UtcNow))
            .ToArray();
        var screen = NewLoadScreen(slots: slots);
        TriggerLoadRender(screen);

        var (rowX, rowY) = LoadCenter(LoadLayout.RowRect(0));
        var (loadX, loadY) = LoadCenter(LoadLayout.LoadButtonRect());

        void SelectRowZeroThenLoad()
        {
            screen.OnMouseDown(rowX, rowY);
            screen.OnMouseDown(loadX, loadY);
        }

        SelectRowZeroThenLoad();
        Assert.Equal("slot-0", screen.LastRequestedSlotId);

        for (int i = 0; i < extra; i++)
            screen.OnMouseWheel(0, 0, -1); // scroll down one row at a time

        TriggerLoadRender(screen);
        SelectRowZeroThenLoad();
        Assert.Equal($"slot-{extra}", screen.LastRequestedSlotId);

        // Further downward scrolling past the last row is clamped, not out of range.
        screen.OnMouseWheel(0, 0, -1);
        TriggerLoadRender(screen);
        SelectRowZeroThenLoad();
        Assert.Equal($"slot-{extra}", screen.LastRequestedSlotId);

        // Scrolling back up returns to (and clamps at) the top.
        for (int i = 0; i < extra + 1; i++)
            screen.OnMouseWheel(0, 0, 1);
        TriggerLoadRender(screen);
        SelectRowZeroThenLoad();
        Assert.Equal("slot-0", screen.LastRequestedSlotId);
    }

    // --- ScenarioSelect tests ---

    private static float ScenarioSelectPanelLeft => ScenarioSelectLayout.PanelLeft(ScreenWidth);
    private static float ScenarioSelectPanelTop => ScenarioSelectLayout.PanelTop(ScreenHeight);

    private static (float x, float y) ScenarioSelectCenter((float X, float Y, float W, float H) local) =>
        (ScenarioSelectPanelLeft + local.X + local.W / 2f, ScenarioSelectPanelTop + local.Y + local.H / 2f);

    /// <summary>BACK/PLAY rects are local to the action panel — add its offset too.</summary>
    private static (float x, float y) ScenarioSelectActionCenter((float X, float Y, float W, float H) local) =>
        (ScenarioSelectPanelLeft + ScenarioSelectLayout.ActionPanelX + local.X + local.W / 2f,
         ScenarioSelectPanelTop + ScenarioSelectLayout.ActionPanelY + local.Y + local.H / 2f);

    /// <summary>
    /// Center of the row at <paramref name="rowIndexFromTop"/>, assuming every row above
    /// it (and this one) has a single-line description — true for every scenario these
    /// tests construct, so rows stack at the fixed single-line height/spacing step.
    /// </summary>
    private static (float x, float y) ScenarioSelectRowCenter(int rowIndexFromTop)
    {
        float rowHeight = ScenarioSelectLayout.RowHeightFor(1);
        float y = ScenarioSelectLayout.ListTop + rowIndexFromTop * (rowHeight + ScenarioSelectLayout.RowSpacing);
        return ScenarioSelectCenter(ScenarioSelectLayout.RowRect(y, rowHeight));
    }

    /// <summary>How many single-line rows fit in the list before it needs to scroll.</summary>
    private static int VisibleSingleLineRowCount
    {
        get
        {
            float rowStep = ScenarioSelectLayout.RowHeightFor(1) + ScenarioSelectLayout.RowSpacing;
            return (int)((ScenarioSelectLayout.ListHeight + ScenarioSelectLayout.RowSpacing) / rowStep);
        }
    }

    private static void TriggerScenarioSelectRender(IScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    private static ScenarioInfo Scenario(string id, string name, string description) =>
        new($"/fake/{id}/scenario.json", id, name, description);

    private static ScenarioSelectScreen NewScenarioSelectScreen(IReadOnlyList<ScenarioInfo>? scenarios = null)
    {
        var currentScenarios = scenarios ?? Array.Empty<ScenarioInfo>();
        return new ScenarioSelectScreen(() => currentScenarios);
    }

    [Fact]
    public void ScenarioSelect_Back_click_returns_MainMenu()
    {
        var screen = NewScenarioSelectScreen();
        TriggerScenarioSelectRender(screen);
        var (x, y) = ScenarioSelectActionCenter(ScenarioSelectLayout.BackButtonRect());
        Assert.Equal(ScreenEvent.MainMenu, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void ScenarioSelect_Esc_returns_MainMenu()
    {
        var screen = NewScenarioSelectScreen();
        TriggerScenarioSelectRender(screen);
        Assert.Equal(ScreenEvent.MainMenu, screen.OnKeyDown(Key.Escape));
    }

    [Fact]
    public void ScenarioSelect_other_key_returns_None()
    {
        var screen = NewScenarioSelectScreen();
        TriggerScenarioSelectRender(screen);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.A));
    }

    [Fact]
    public void ScenarioSelect_click_outside_button_returns_None()
    {
        var screen = NewScenarioSelectScreen();
        TriggerScenarioSelectRender(screen);
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(0, 0));
    }

    [Fact]
    public void ScenarioSelect_first_scenario_is_selected_by_default_so_Play_works_without_a_row_click()
    {
        var scenarios = new[] { Scenario("default", "Default Scenario", "A basic start.") };
        var screen = NewScenarioSelectScreen(scenarios);
        TriggerScenarioSelectRender(screen);

        var (x, y) = ScenarioSelectActionCenter(ScenarioSelectLayout.PlayButtonRect());
        var evt = screen.OnMouseDown(x, y);

        Assert.Equal(ScreenEvent.ScenarioSelected, evt);
        // Mirrors how SkiaWindow's mouse-dispatch site reads this: synchronously, in the
        // same call frame, immediately after OnMouseDown returns.
        Assert.Equal("/fake/default/scenario.json", screen.LastSelectedScenarioPath);
    }

    [Fact]
    public void ScenarioSelect_Play_with_no_scenarios_returns_None()
    {
        var screen = NewScenarioSelectScreen();
        TriggerScenarioSelectRender(screen);

        var (x, y) = ScenarioSelectActionCenter(ScenarioSelectLayout.PlayButtonRect());
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void ScenarioSelect_clicking_a_row_selects_it_without_playing()
    {
        var scenarios = new[]
        {
            Scenario("default", "Default Scenario", "A basic start."),
            Scenario("docked", "Docked at Station", "Start docked.")
        };
        var screen = NewScenarioSelectScreen(scenarios);
        TriggerScenarioSelectRender(screen);

        var (rowX, rowY) = ScenarioSelectRowCenter(1);
        var evt = screen.OnMouseDown(rowX, rowY);

        Assert.Equal(ScreenEvent.None, evt);
        Assert.Null(screen.LastSelectedScenarioPath);
    }

    [Fact]
    public void ScenarioSelect_Play_click_plays_whichever_row_was_last_selected()
    {
        var scenarios = new[]
        {
            Scenario("default", "Default Scenario", "A basic start."),
            Scenario("docked", "Docked at Station", "Start docked.")
        };
        var screen = NewScenarioSelectScreen(scenarios);
        TriggerScenarioSelectRender(screen);

        var (rowX, rowY) = ScenarioSelectRowCenter(1);
        screen.OnMouseDown(rowX, rowY);

        var (playX, playY) = ScenarioSelectActionCenter(ScenarioSelectLayout.PlayButtonRect());
        var evt = screen.OnMouseDown(playX, playY);

        Assert.Equal(ScreenEvent.ScenarioSelected, evt);
        Assert.Equal("/fake/docked/scenario.json", screen.LastSelectedScenarioPath);
    }

    [Fact]
    public void ScenarioSelect_MouseWheel_scrolls_row_zero_through_overflowing_scenarios_and_clamps_at_both_ends()
    {
        int extra = 3;
        var scenarios = Enumerable.Range(0, VisibleSingleLineRowCount + extra)
            .Select(i => Scenario($"scenario-{i}", $"Scenario {i}", "Desc"))
            .ToArray();
        var screen = NewScenarioSelectScreen(scenarios);
        TriggerScenarioSelectRender(screen);

        var (rowX, rowY) = ScenarioSelectRowCenter(0);
        var (playX, playY) = ScenarioSelectActionCenter(ScenarioSelectLayout.PlayButtonRect());

        void SelectRowZeroThenPlay()
        {
            screen.OnMouseDown(rowX, rowY);
            screen.OnMouseDown(playX, playY);
        }

        SelectRowZeroThenPlay();
        Assert.Equal("/fake/scenario-0/scenario.json", screen.LastSelectedScenarioPath);

        for (int i = 0; i < extra; i++)
            screen.OnMouseWheel(0, 0, -1); // scroll down one row at a time

        TriggerScenarioSelectRender(screen);
        SelectRowZeroThenPlay();
        Assert.Equal($"/fake/scenario-{extra}/scenario.json", screen.LastSelectedScenarioPath);

        // Further downward scrolling past the last row is clamped, not out of range.
        screen.OnMouseWheel(0, 0, -1);
        TriggerScenarioSelectRender(screen);
        SelectRowZeroThenPlay();
        Assert.Equal($"/fake/scenario-{extra}/scenario.json", screen.LastSelectedScenarioPath);

        // Scrolling back up returns to (and clamps at) the top.
        for (int i = 0; i < extra + 1; i++)
            screen.OnMouseWheel(0, 0, 1);
        TriggerScenarioSelectRender(screen);
        SelectRowZeroThenPlay();
        Assert.Equal("/fake/scenario-0/scenario.json", screen.LastSelectedScenarioPath);
    }

    [Fact]
    public void ScenarioSelect_a_wrapped_multiline_description_pushes_the_next_row_down()
    {
        // Long enough to word-wrap to several lines at the content panel's list width.
        string longDescription = string.Join(" ", Enumerable.Repeat("word", 40));
        var scenarios = new[]
        {
            Scenario("long", "Long Scenario", longDescription),
            Scenario("short", "Short Scenario", "Tiny.")
        };
        var screen = NewScenarioSelectScreen(scenarios);
        TriggerScenarioSelectRender(screen);

        var (playX, playY) = ScenarioSelectActionCenter(ScenarioSelectLayout.PlayButtonRect());

        // Where row 1 would sit if every row were single-line height — with the wrapped
        // row 0 now taller, that point must land inside row 0's grown bounds (or the gap
        // before the real row 1), never on scenario 1.
        var (naiveRow1X, naiveRow1Y) = ScenarioSelectRowCenter(1);
        screen.OnMouseDown(naiveRow1X, naiveRow1Y);
        screen.OnMouseDown(playX, playY);
        Assert.Equal("/fake/long/scenario.json", screen.LastSelectedScenarioPath);

        // Comfortably past row 0's extra wrapped height, the real row 1 is selectable.
        screen.OnMouseDown(naiveRow1X, naiveRow1Y + 60f);
        screen.OnMouseDown(playX, playY);
        Assert.Equal("/fake/short/scenario.json", screen.LastSelectedScenarioPath);
    }
}
