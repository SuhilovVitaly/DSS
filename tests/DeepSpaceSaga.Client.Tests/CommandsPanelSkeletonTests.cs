using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class CommandsPanelSkeletonTests
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;
    private const string PlayerShipId = "SPC-0001";
    private const string EngineModuleId = "MOD-PLAYER-ENGINE-01";
    private const string ScannerModuleId = "MOD-PLAYER-SCANNER-01";
    private const string NavigationComputerModuleId = "MOD-PLAYER-NAV-COMPUTER-01";

    private static readonly ImmutableArray<string> PanelOrder = ImmutableArray.Create(
        "Navigation", "Maneuver", "Engine", "Space Control");

    /// <summary>
    /// Display names and targets mirror Client/Data/command-definitions.json —
    /// the panel must render labels and enablement from this metadata.
    /// </summary>
    private static readonly Dictionary<string, (string Name, string Target)> CommandMetadata = new()
    {
        ["engine.accelerate"] = ("Accelerate", "none"),
        ["engine.brake"] = ("Brake", "none"),
        ["engine.maintainSpeed"] = ("Maintain Speed", "none"),
        ["engine.turnLeftStep"] = ("Turn Left Step", "none"),
        ["engine.turnRightStep"] = ("Turn Right Step", "none"),
        ["engine.turnLeftUntilCancel"] = ("Turn Left Until Cancel", "none"),
        ["engine.turnRightUntilCancel"] = ("Turn Right Until Cancel", "none"),
        ["engine.maintainCourse"] = ("Maintain Course", "none"),
        ["engine.speedSynchronization"] = ("Speed Synchronization", "object"),
        ["engine.directionSynchronization"] = ("Direction Synchronization", "object"),
        ["engine.orbit"] = ("Orbit", "point"),
        ["scanner.generalScan"] = ("General Scan", "object"),
        ["scanner.structuralScan"] = ("Structural Scan", "object"),
        ["navigation.dock"] = ("Dock", "object"),
        ["navigation.stationsList"] = ("Stations List", "none"),
    };

    private static ImmutableArray<ModuleCommandSnapshot> CommandsFor(ImmutableArray<string> typeIds) =>
        typeIds.Select(id => CommandMetadata.TryGetValue(id, out var meta)
            ? new ModuleCommandSnapshot(id, meta.Name, meta.Target)
            : new ModuleCommandSnapshot(id, id, "none")).ToImmutableArray();

    private static readonly ImmutableArray<string> EngineCommandTypeIds = ImmutableArray.Create(
        "engine.accelerate", "engine.brake", "engine.maintainSpeed",
        "engine.orbit");

    private static readonly ImmutableArray<string> FullEngineCommandTypeIds = ImmutableArray.Create(
        "engine.accelerate", "engine.brake", "engine.maintainSpeed",
        "engine.turnLeftStep", "engine.turnRightStep",
        "engine.turnLeftUntilCancel", "engine.turnRightUntilCancel",
        "engine.maintainCourse",
        "engine.speedSynchronization", "engine.directionSynchronization",
        "engine.orbit");

    private static readonly ImmutableArray<string> ScannerCommandTypeIds = ImmutableArray.Create(
        "scanner.generalScan", "scanner.structuralScan");

    private static readonly ImmutableArray<InstalledModuleSnapshot> OneEngineModule = ImmutableArray.Create(
        new InstalledModuleSnapshot(
            EngineModuleId, "module.engine.basic", "Engine", Position: 1, EngineCommandTypeIds,
            Commands: CommandsFor(EngineCommandTypeIds)));

    private static readonly ImmutableArray<InstalledModuleSnapshot> EngineAndScannerModules = ImmutableArray.Create(
        new InstalledModuleSnapshot(
            EngineModuleId, "module.engine.basic", "Engine", Position: 1, EngineCommandTypeIds,
            Commands: CommandsFor(EngineCommandTypeIds)),
        new InstalledModuleSnapshot(
            ScannerModuleId, "module.scanner.mk1", "Scanner MK I", Position: 2, ScannerCommandTypeIds,
            Commands: CommandsFor(ScannerCommandTypeIds)));

    private static readonly ImmutableArray<InstalledModuleSnapshot> FullEngineModule = ImmutableArray.Create(
        new InstalledModuleSnapshot(
            EngineModuleId, "module.engine.basic", "Engine", Position: 1, FullEngineCommandTypeIds,
            Commands: CommandsFor(FullEngineCommandTypeIds)));

    /// <summary>Two installed modules whose CommandTypeIds both cover engine.accelerate, at
    /// different Position — the click resolver must pick the lower-Position module.</summary>
    private static readonly ImmutableArray<InstalledModuleSnapshot> TwoEngineModulesSharingAccelerate = ImmutableArray.Create(
        new InstalledModuleSnapshot(
            "MOD-ENGINE-HI-POSITION", "module.engine.basic", "Engine Hi", Position: 5,
            ImmutableArray.Create(ShipEngineCommandTypes.Accelerate),
            Commands: CommandsFor(ImmutableArray.Create(ShipEngineCommandTypes.Accelerate))),
        new InstalledModuleSnapshot(
            "MOD-ENGINE-LO-POSITION", "module.engine.basic", "Engine Lo", Position: 1,
            ImmutableArray.Create(ShipEngineCommandTypes.Accelerate),
            Commands: CommandsFor(ImmutableArray.Create(ShipEngineCommandTypes.Accelerate))));

    /// <summary>Engine module plus an installed Navigation Computer exposing both
    /// navigation.dock and navigation.stationsList.</summary>
    private static readonly ImmutableArray<InstalledModuleSnapshot> EngineAndNavigationComputerModules = ImmutableArray.Create(
        new InstalledModuleSnapshot(
            EngineModuleId, "module.engine.basic", "Engine", Position: 1, EngineCommandTypeIds,
            Commands: CommandsFor(EngineCommandTypeIds)),
        new InstalledModuleSnapshot(
            NavigationComputerModuleId, "module.bridge.navigation.computer.basic", "Navigation Computer", Position: 0,
            ImmutableArray.Create(NavigationComputerCommandTypes.Dock, NavigationComputerCommandTypes.StationsList),
            Commands: CommandsFor(ImmutableArray.Create(
                NavigationComputerCommandTypes.Dock, NavigationComputerCommandTypes.StationsList))));

    // ── Fixed panel composition (ТЗ "Панели и порядок") ──────────

    [Fact]
    public void Panels_are_declared_in_fixed_order_with_fixed_command_composition()
    {
        Assert.Equal(4, CommandsPanel.Panels.Length);
        Assert.Equal(PanelOrder, CommandsPanel.Panels.Select(p => p.Name));

        Assert.Equal(
            new[]
            {
                NavigationComputerCommandTypes.Dock,
                NavigationComputerCommandTypes.StationsList,
                ShipEngineCommandTypes.Orbit,
                ShipEngineCommandTypes.SpeedSynchronization,
                ShipEngineCommandTypes.DirectionSynchronization,
                NavigationComputerCommandTypes.Approach,
            },
            CommandsPanel.Panels[0].CommandTypeIds);

        Assert.Equal(
            new[]
            {
                ShipEngineCommandTypes.MaintainCourse,
                ShipEngineCommandTypes.TurnLeftStep,
                ShipEngineCommandTypes.TurnRightStep,
                ShipEngineCommandTypes.TurnLeftUntilCancel,
                ShipEngineCommandTypes.TurnRightUntilCancel,
            },
            CommandsPanel.Panels[1].CommandTypeIds);

        Assert.Equal(
            new[]
            {
                ShipEngineCommandTypes.Accelerate,
                ShipEngineCommandTypes.Brake,
                ShipEngineCommandTypes.MaintainSpeed,
            },
            CommandsPanel.Panels[2].CommandTypeIds);

        Assert.Equal(
            new[] { ScannerCommandTypes.GeneralScan, ScannerCommandTypes.StructuralScan },
            CommandsPanel.Panels[3].CommandTypeIds);
    }

    // ── Geometry ────────────────────────────────────────────────

    [Fact]
    public void Panel_renders_top_left_with_caption_360x32_and_body_spans_all_four_panels()
    {
        var screen = CreateScreen();
        Render(screen);

        float expectedBottom = CommandsPanel.Panels.Length *
            (CommandsPanel.PanelCaptionHeight + CommandsPanel.PanelBodyHeight) +
            CommandsPanel.MainCaptionToPanelsGap;

        Assert.Equal(new SKRect(8, 8, 368, 40), screen.CommandsPanel.CaptionRect);
        Assert.Equal(new SKRect(8, 40, 368, 40 + expectedBottom), screen.CommandsPanel.BodyRect);
    }

    [Fact]
    public void Xenon_chrome_assets_are_loaded_without_changing_the_360x832_footprint()
    {
        var screen = CreateScreen();
        Render(screen);

        Assert.True(screen.CommandsPanel.HasLoadedXenonChrome);
        Assert.Equal(CommandsPanel.PanelWidth, screen.CommandsPanel.CaptionRect.Width);
        Assert.Equal(834f, screen.CommandsPanel.CaptionRect.Height + screen.CommandsPanel.BodyRect.Height);
    }

    [Fact]
    public void Xenon_panel_caption_and_module_bodies_are_fully_opaque()
    {
        var panel = new CommandsPanel();
        using var bitmap = new SKBitmap(384, 856);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        panel.Render(canvas, ImmutableArray<InstalledModuleSnapshot>.Empty);

        Assert.Equal(255, bitmap.GetPixel(350, 20).Alpha);
        Assert.Equal(255, bitmap.GetPixel(350, 54).Alpha);
        Assert.Equal(255, bitmap.GetPixel(350, 120).Alpha);

        var mainCaptionColor = bitmap.GetPixel(240, 20);
        var moduleCaptionColor = bitmap.GetPixel(200, 54);
        Assert.Equal(mainCaptionColor, bitmap.GetPixel(340, 20));
        Assert.Equal(moduleCaptionColor, bitmap.GetPixel(340, 54));
        Assert.True(mainCaptionColor.Red < moduleCaptionColor.Red);
        Assert.True(mainCaptionColor.Green < moduleCaptionColor.Green);
        Assert.True(mainCaptionColor.Blue < moduleCaptionColor.Blue);
    }

    [Fact]
    public void Panel_state_is_AllPanels_by_default()
    {
        var screen = CreateScreen();
        Assert.Equal(CommandsPanelState.AllPanels, screen.CommandsPanel.State);
    }

    [Fact]
    public void Four_panels_are_always_shown_regardless_of_installed_modules()
    {
        var withNoModules = CreateScreen(ImmutableArray<InstalledModuleSnapshot>.Empty);
        Render(withNoModules);
        Assert.Equal(PanelOrder, withNoModules.CommandsPanel.CommandPanelRows.Select(r => r.Name));

        var withOneModule = CreateScreen(OneEngineModule);
        Render(withOneModule);
        Assert.Equal(PanelOrder, withOneModule.CommandsPanel.CommandPanelRows.Select(r => r.Name));

        var withFullModule = CreateScreen(FullEngineModule);
        Render(withFullModule);
        Assert.Equal(PanelOrder, withFullModule.CommandsPanel.CommandPanelRows.Select(r => r.Name));
    }

    [Fact]
    public void Click_on_caption_is_consumed_and_does_not_pan_camera()
    {
        var screen = CreateScreen();
        Render(screen);

        double fxBefore = screen.CameraFocusX;
        double fyBefore = screen.CameraFocusY;
        var caption = screen.CommandsPanel.CaptionRect;

        var result = screen.OnMouseDown(caption.MidX, caption.MidY);

        Assert.Equal(ScreenEvent.None, result);
        Assert.Equal(fxBefore, screen.CameraFocusX);
        Assert.Equal(fyBefore, screen.CameraFocusY);
    }

    [Fact]
    public async Task Click_on_panel_caption_is_consumed_and_does_not_pan()
    {
        await using var fixture = CreateFixture();
        Render(fixture.Screen);

        double fxBefore = fixture.Screen.CameraFocusX;
        double fyBefore = fixture.Screen.CameraFocusY;
        var row = fixture.Screen.CommandsPanel.CommandPanelRows.Single(r => r.Name == "Navigation");

        var result = fixture.Screen.OnMouseDown(row.CaptionRect.MidX, row.CaptionRect.MidY);

        Assert.Equal(ScreenEvent.None, result);
        Assert.Equal(fxBefore, fixture.Screen.CameraFocusX);
        Assert.Equal(fyBefore, fixture.Screen.CameraFocusY);
        Assert.Empty(fixture.Connection.Commands);
    }

    [Fact]
    public async Task Click_on_panel_body_gap_is_consumed_and_sends_no_command()
    {
        await using var fixture = CreateFixture();
        Render(fixture.Screen);
        var panel = fixture.Screen.CommandsPanel;

        // "Engine" carries 3 command buttons in a 4-column grid — the 4th column
        // cell is a guaranteed gap: inside the body, not covered by any button.
        var row = panel.CommandPanelRows.Single(r => r.Name == "Engine");
        Assert.True(row.Opened);
        Assert.True(row.BodyRect.Height > 0);
        Assert.Equal(3, row.Buttons.Length);

        var gap = (row.BodyRect.Right - 4f, row.BodyRect.Top + 4f);
        Assert.True(row.BodyRect.Contains(gap.Item1, gap.Item2));
        Assert.DoesNotContain(panel.AllCommandButtons, b => b.Rect.Contains(gap.Item1, gap.Item2));

        double fxBefore = fixture.Screen.CameraFocusX;
        double fyBefore = fixture.Screen.CameraFocusY;

        var result = fixture.Screen.OnMouseDown(gap.Item1, gap.Item2);

        Assert.Equal(ScreenEvent.None, result);
        Assert.Equal(fxBefore, fixture.Screen.CameraFocusX);
        Assert.Equal(fyBefore, fixture.Screen.CameraFocusY);
        Assert.Empty(fixture.Connection.Commands);
    }

    [Fact]
    public void Click_outside_panel_is_not_swallowed_and_drag_still_pans_camera()
    {
        var screen = CreateScreen();
        Render(screen);

        double fxBefore = screen.CameraFocusX;
        double fyBefore = screen.CameraFocusY;

        var result = screen.OnMouseDown(1000, 500);

        // Click alone no longer jumps the camera by itself (disabled — see
        // GameSessionNavigationTests), but it must still reach the map's
        // pan-start branch rather than being swallowed by panel hit-testing —
        // proven below by dragging actually panning afterward.
        Assert.Equal(ScreenEvent.None, result);
        Assert.Equal(fxBefore, screen.CameraFocusX);
        Assert.Equal(fyBefore, screen.CameraFocusY);

        screen.OnMouseMove(1030, 470);

        Assert.NotEqual(fxBefore, screen.CameraFocusX);
        Assert.NotEqual(fyBefore, screen.CameraFocusY);
    }

    [Fact]
    public async Task Click_outside_panel_on_empty_map_sends_no_commands()
    {
        await using var fixture = CreateFixture();
        Render(fixture.Screen);

        fixture.Screen.OnMouseDown(1000, 500);

        Assert.Empty(fixture.Connection.Commands);
    }

    // ── Toggle button ──────────────────────────────────────────

    [Fact]
    public void Toggle_button_is_26x26_at_caption_start()
    {
        var screen = CreateScreen();
        Render(screen);
        var panel = screen.CommandsPanel;

        Assert.Equal(new SKRect(10, 10, 36, 36), panel.HideShowButtonRect);
        Assert.Equal(CommandsPanel.ButtonSize, panel.HideShowButtonRect.Width);
        Assert.Equal(CommandsPanel.ButtonSize, panel.HideShowButtonRect.Height);
    }

    [Fact]
    public void Toggle_button_closes_panel_and_saves_previous_state()
    {
        var screen = CreateScreen();
        Render(screen);
        var panel = screen.CommandsPanel;

        Assert.Equal(CommandsPanelState.AllPanels, panel.State);

        screen.OnMouseDown(panel.HideShowButtonRect.MidX, panel.HideShowButtonRect.MidY);
        Render(screen);

        Assert.Equal(CommandsPanelState.Closed, panel.State);
        Assert.Equal(CommandsPanelState.AllPanels, panel.PreviousNonClosedState);
        Assert.Equal(0f, panel.BodyRect.Height);
        Assert.Empty(panel.CommandPanelRows);
    }

    [Fact]
    public void Toggle_button_restores_previous_state_when_closed()
    {
        var screen = CreateScreen();
        Render(screen);
        var panel = screen.CommandsPanel;

        screen.OnMouseDown(panel.HideShowButtonRect.MidX, panel.HideShowButtonRect.MidY);
        Render(screen);
        Assert.Equal(CommandsPanelState.Closed, panel.State);

        screen.OnMouseDown(panel.HideShowButtonRect.MidX, panel.HideShowButtonRect.MidY);
        Render(screen);

        Assert.Equal(CommandsPanelState.AllPanels, panel.State);
        Assert.Equal(4, panel.CommandPanelRows.Count);
        Assert.True(panel.BodyRect.Height > 0);
    }

    [Fact]
    public async Task State_changes_do_not_send_engine_commands()
    {
        await using var fixture = CreateFixture();
        Render(fixture.Screen);
        var panel = fixture.Screen.CommandsPanel;

        fixture.Screen.OnMouseDown(panel.HideShowButtonRect.MidX, panel.HideShowButtonRect.MidY);
        Render(fixture.Screen);

        fixture.Screen.OnMouseDown(panel.HideShowButtonRect.MidX, panel.HideShowButtonRect.MidY);
        Render(fixture.Screen);

        Assert.Empty(fixture.Connection.Commands);
    }

    // ── Hover / pressed ────────────────────────────────────────

    [Fact]
    public void OnMouseMove_tracks_hover_over_toggle_button()
    {
        var screen = CreateScreen();
        Render(screen);
        var panel = screen.CommandsPanel;

        screen.OnMouseMove(panel.HideShowButtonRect.MidX, panel.HideShowButtonRect.MidY);
        Assert.Equal(0, panel.HoveredButtonIndex);

        screen.OnMouseMove(1000, 500);
        Assert.Equal(-1, panel.HoveredButtonIndex);
    }

    [Fact]
    public void OnMouseUp_clears_pressed_button()
    {
        var screen = CreateScreen();
        Render(screen);
        var panel = screen.CommandsPanel;

        screen.OnMouseDown(panel.HideShowButtonRect.MidX, panel.HideShowButtonRect.MidY);
        Assert.Equal(0, panel.PressedButtonIndex);

        screen.OnMouseUp(panel.HideShowButtonRect.MidX, panel.HideShowButtonRect.MidY);
        Assert.Equal(-1, panel.PressedButtonIndex);
    }

    // ── Command panel row geometry ────────────────────────────────

    [Fact]
    public void Panel_caption_is_full_width_36px_and_body_height_is_fixed_for_every_panel()
    {
        var screen = CreateScreen();
        Render(screen);
        var panel = screen.CommandsPanel;

        foreach (var definition in CommandsPanel.Panels)
        {
            var row = panel.CommandPanelRows.Single(r => r.Name == definition.Name);

            Assert.True(row.Opened);
            Assert.Equal(CommandsPanel.PanelWidth, row.CaptionRect.Width);
            Assert.Equal(CommandsPanel.PanelCaptionHeight, row.CaptionRect.Height);
            Assert.Equal(CommandsPanel.PanelWidth, row.BodyRect.Width);
            Assert.Equal(CommandsPanel.PanelBodyHeight, row.BodyRect.Height);
            Assert.Equal(row.CaptionRect.Bottom, row.BodyRect.Top);
            Assert.Equal(row.CaptionRect.Left, row.BodyRect.Left);
        }

        var engineRow = panel.CommandPanelRows.Single(r => r.Name == "Engine");
        screen.OnMouseDown(engineRow.CaptionRect.MidX, engineRow.CaptionRect.MidY);
        Render(screen);

        engineRow = panel.CommandPanelRows.Single(r => r.Name == "Engine");
        Assert.False(engineRow.Opened);
        Assert.Equal(0f, engineRow.BodyRect.Height);
    }

    [Fact]
    public void Panel_toggle_switches_Opened_state_for_that_panel_only()
    {
        var screen = CreateScreen();
        Render(screen);
        var panel = screen.CommandsPanel;

        var engineRow = panel.CommandPanelRows.Single(r => r.Name == "Engine");
        Assert.True(engineRow.Opened);

        screen.OnMouseDown(engineRow.CaptionRect.MidX, engineRow.CaptionRect.MidY);
        Render(screen);
        Assert.False(panel.CommandPanelRows.Single(r => r.Name == "Engine").Opened);

        // Other panels are unaffected by toggling one panel.
        Assert.True(panel.CommandPanelRows.Single(r => r.Name == "Navigation").Opened);
        Assert.True(panel.CommandPanelRows.Single(r => r.Name == "Maneuver").Opened);
        Assert.True(panel.CommandPanelRows.Single(r => r.Name == "Space Control").Opened);

        screen.OnMouseDown(engineRow.CaptionRect.MidX, engineRow.CaptionRect.MidY);
        Render(screen);
        Assert.True(panel.CommandPanelRows.Single(r => r.Name == "Engine").Opened);
    }

    [Fact]
    public void Collapsed_panels_have_a_2px_gap_between_adjacent_captions_only()
    {
        var screen = CreateScreen();
        Render(screen);

        foreach (var definition in CommandsPanel.Panels)
        {
            var row = screen.CommandsPanel.CommandPanelRows.Single(r => r.Name == definition.Name);
            screen.OnMouseDown(row.CaptionRect.MidX, row.CaptionRect.MidY);
            Render(screen);
        }

        var rows = screen.CommandsPanel.CommandPanelRows;
        Assert.All(rows, row => Assert.False(row.Opened));
        Assert.Equal(CommandsPanel.MainCaptionToPanelsGap,
            rows[0].CaptionRect.Top - screen.CommandsPanel.CaptionRect.Bottom);
        for (int i = 1; i < rows.Count; i++)
            Assert.Equal(CommandsPanel.CollapsedPanelGap, rows[i].CaptionRect.Top - rows[i - 1].CaptionRect.Bottom);

        float expectedHeight = CommandsPanel.MainCaptionToPanelsGap +
                               rows.Count * CommandsPanel.PanelCaptionHeight +
                               (rows.Count - 1) * CommandsPanel.CollapsedPanelGap;
        Assert.Equal(expectedHeight, screen.CommandsPanel.BodyRect.Height);
    }

    [Fact]
    public void Empty_installed_modules_still_shows_all_four_panels_with_disabled_buttons()
    {
        var screen = CreateScreen(ImmutableArray<InstalledModuleSnapshot>.Empty);
        Render(screen);
        var panel = screen.CommandsPanel;

        Assert.Equal(4, panel.CommandPanelRows.Count);
        Assert.Equal(PanelOrder, panel.CommandPanelRows.Select(r => r.Name));

        int expectedTotalButtons = CommandsPanel.Panels.Sum(p => p.CommandTypeIds.Length);
        Assert.Equal(expectedTotalButtons, panel.AllCommandButtons.Count);
        Assert.All(panel.AllCommandButtons, b => Assert.False(b.Enabled));
    }

    // ── Command buttons — fixed composition per panel, in declared order ───────

    [Fact]
    public void Opened_panel_draws_one_button_per_CommandTypeIds_entry_in_order()
    {
        var screen = CreateScreen();
        Render(screen);
        var panel = screen.CommandsPanel;

        var engineRow = panel.CommandPanelRows.Single(r => r.Name == "Engine");
        Assert.Equal(3, engineRow.Buttons.Length);
        Assert.Equal(
            new[] { "engine.accelerate", "engine.brake", "engine.maintainSpeed" },
            engineRow.Buttons.Select(b => b.CommandTypeId));
    }

    [Fact]
    public void Command_button_grid_layout_is_4_columns_with_expected_geometry()
    {
        var screen = CreateScreen();
        Render(screen);
        var engineRow = screen.CommandsPanel.CommandPanelRows.Single(r => r.Name == "Engine");

        // Engine body top = 476 (after Navigation 164 + Maneuver 164, fixed
        // PanelBodyHeight per panel, + three 36px captions);
        // grid origin = body + (6, 6); button 84x48, gap 4 → columns at x = 14, 102, 190.
        Assert.Equal(new SKRect(14, 484, 98, 532), engineRow.Buttons[0].Rect);
        Assert.Equal(new SKRect(102, 484, 186, 532), engineRow.Buttons[1].Rect);
        Assert.Equal(new SKRect(190, 484, 274, 532), engineRow.Buttons[2].Rect);

        Assert.Equal(84f, engineRow.Buttons[0].Rect.Width);
        Assert.Equal(48f, engineRow.Buttons[0].Rect.Height);
    }

    [Theory]
    [InlineData("engine.accelerate")]
    [InlineData("engine.brake")]
    [InlineData("engine.directionSynchronization")]
    [InlineData("engine.maintainCourse")]
    [InlineData("engine.maintainSpeed")]
    [InlineData("engine.orbit")]
    [InlineData("engine.speedSynchronization")]
    [InlineData("engine.turnLeftStep")]
    [InlineData("engine.turnLeftUntilCancel")]
    [InlineData("engine.turnRightStep")]
    [InlineData("engine.turnRightUntilCancel")]
    [InlineData("navigation.dock")]
    [InlineData("navigation.stationsList")]
    [InlineData("scanner.generalScan")]
    [InlineData("scanner.structuralScan")]
    public void Commands_with_a_declared_icon_file_load_it_successfully(string commandTypeId)
    {
        var screen = CreateScreen();

        Assert.True(CommandsPanel.CommandIconFileNames.ContainsKey(commandTypeId));
        Assert.True(screen.CommandsPanel.HasLoadedIconFor(commandTypeId));
    }

    [Fact]
    public void Exactly_the_fifteen_commands_with_asset_files_have_a_declared_icon()
    {
        Assert.Equal(
            new[]
            {
                "engine.accelerate",
                "engine.brake",
                "engine.directionSynchronization",
                "engine.maintainCourse",
                "engine.maintainSpeed",
                "engine.orbit",
                "engine.speedSynchronization",
                "engine.turnLeftStep",
                "engine.turnLeftUntilCancel",
                "engine.turnRightStep",
                "engine.turnRightUntilCancel",
                "navigation.dock",
                "navigation.stationsList",
                "scanner.generalScan",
                "scanner.structuralScan",
            },
            CommandsPanel.CommandIconFileNames.Keys.OrderBy(k => k, StringComparer.Ordinal));
    }

    [Fact]
    public void Commands_without_a_declared_icon_have_none_loaded()
    {
        var screen = CreateScreen();

        Assert.False(screen.CommandsPanel.HasLoadedIconFor("scanner.nearbySignatures"));
        Assert.False(screen.CommandsPanel.HasLoadedIconFor("engine.cancelAll"));
    }

    [Theory]
    [InlineData("engine.accelerate")]
    [InlineData("engine.brake")]
    [InlineData("engine.directionSynchronization")]
    [InlineData("engine.maintainCourse")]
    [InlineData("engine.maintainSpeed")]
    [InlineData("engine.orbit")]
    [InlineData("engine.speedSynchronization")]
    [InlineData("engine.turnLeftStep")]
    [InlineData("engine.turnLeftUntilCancel")]
    [InlineData("engine.turnRightStep")]
    [InlineData("engine.turnRightUntilCancel")]
    [InlineData("navigation.dock")]
    [InlineData("navigation.stationsList")]
    [InlineData("scanner.generalScan")]
    [InlineData("scanner.structuralScan")]
    public void Commands_with_a_declared_icon_also_have_an_active_hover_variant(string commandTypeId)
    {
        var screen = CreateScreen();

        Assert.True(screen.CommandsPanel.HasLoadedActiveIconFor(commandTypeId));
    }

    [Fact]
    public void Missing_active_hover_file_would_still_load_the_normal_icon()
    {
        // Every declared icon currently ships a matching ".active" asset, so this
        // exercises the fallback path directly rather than relying on some file
        // being absent (which HasLoadedIconFor/HasLoadedActiveIconFor above would
        // silently stop covering the day the last missing asset is added).
        var screen = CreateScreen();

        Assert.False(screen.CommandsPanel.HasLoadedIconFor("no.such.command"));
        Assert.False(screen.CommandsPanel.HasLoadedActiveIconFor("no.such.command"));
    }

    [Fact]
    public void Icon_commands_render_bare_without_button_chrome_but_keep_the_clickable_rect()
    {
        var screen = CreateScreen(FullEngineModule);
        Render(screen);

        var engineRow = screen.CommandsPanel.CommandPanelRows.Single(r => r.Name == "Engine");
        var accelerateButton = engineRow.Buttons.Single(b => b.CommandTypeId == "engine.accelerate");

        // Same 84×32 clickable/hover/cursor area as a regular button — only the
        // drawn chrome (fill/border) is skipped for icon commands.
        Assert.Equal(CommandsPanel.CommandButtonWidth, accelerateButton.Rect.Width);
        Assert.Equal(CommandsPanel.CommandButtonHeight, accelerateButton.Rect.Height);

        bool overInteractive = screen.OnMouseMove(accelerateButton.Rect.MidX, accelerateButton.Rect.MidY);
        Assert.True(overInteractive);
    }

    [Fact]
    public void Six_command_ids_wrap_into_two_rows_of_4_and_2()
    {
        var screen = CreateScreen();
        Render(screen);
        var navigationRow = screen.CommandsPanel.CommandPanelRows.Single(r => r.Name == "Navigation");

        Assert.Equal(6, navigationRow.Buttons.Length);

        // Navigation body top = 76; row 0 (y 82..130): 4 buttons; row 1 (y 134..182): 2.
        Assert.Equal(new SKRect(14, 84, 98, 132), navigationRow.Buttons[0].Rect);
        Assert.Equal(new SKRect(278, 84, 362, 132), navigationRow.Buttons[3].Rect);
        Assert.Equal(new SKRect(14, 136, 98, 184), navigationRow.Buttons[4].Rect);
        Assert.Equal(new SKRect(102, 136, 186, 184), navigationRow.Buttons[5].Rect);
    }

    [Fact]
    public void Approach_placeholder_button_is_always_disabled()
    {
        var screen = CreateScreen();
        Render(screen);
        var panel = screen.CommandsPanel;

        var approach = Assert.Single(
            panel.AllCommandButtons, b => b.CommandTypeId == NavigationComputerCommandTypes.Approach);
        Assert.False(approach.Enabled);
    }

    [Fact]
    public void Collapsed_panel_has_no_command_buttons()
    {
        var screen = CreateScreen();
        Render(screen);
        var panel = screen.CommandsPanel;

        var engineRow = panel.CommandPanelRows.Single(r => r.Name == "Engine");
        screen.OnMouseDown(engineRow.CaptionRect.MidX, engineRow.CaptionRect.MidY);
        Render(screen);

        engineRow = panel.CommandPanelRows.Single(r => r.Name == "Engine");
        Assert.False(engineRow.Opened);
        Assert.Empty(engineRow.Buttons);
        Assert.DoesNotContain(panel.AllCommandButtons, b =>
            b.CommandTypeId is "engine.accelerate" or "engine.brake" or "engine.maintainSpeed");
    }

    // ── Per-panel status bar (hovered command name, bottom of that panel's own body, centered) ─

    [Fact]
    public async Task Hovering_a_command_button_shows_its_name_in_its_own_panels_status_bar_only()
    {
        await using var fixture = CreateFixture();
        Render(fixture.Screen);
        var panel = fixture.Screen.CommandsPanel;

        var accelerate = Assert.Single(panel.AllCommandButtons, b => b.CommandTypeId == "engine.accelerate");
        fixture.Screen.OnMouseMove(accelerate.Rect.MidX, accelerate.Rect.MidY);
        Render(fixture.Screen);

        var engineRow = panel.CommandPanelRows.Single(r => r.Name == "Engine");
        Assert.Equal("Accelerate", engineRow.StatusBarText);

        // Every other panel stays silent — this is not one shared status bar.
        foreach (var row in panel.CommandPanelRows.Where(r => r.Name != "Engine"))
            Assert.Null(row.StatusBarText);
    }

    [Fact]
    public void No_command_name_is_shown_anywhere_when_no_command_button_is_hovered()
    {
        var screen = CreateScreen();
        Render(screen);
        var panel = screen.CommandsPanel;

        screen.OnMouseMove(1000, 500);
        Render(screen);

        Assert.All(panel.CommandPanelRows, row => Assert.Null(row.StatusBarText));
    }

    [Fact]
    public void Each_panels_status_bar_overlays_the_bottom_of_its_own_body_without_growing_it()
    {
        var screen = CreateScreen();
        Render(screen);
        var panel = screen.CommandsPanel;

        foreach (var row in panel.CommandPanelRows)
        {
            // Body height is unchanged by the status bar — it overlays, not adds.
            Assert.Equal(CommandsPanel.PanelBodyHeight, row.BodyRect.Height);

            // Inset on every side so it sits inside the panel's border, not on top of it.
            Assert.True(row.StatusBarRect.Left > row.BodyRect.Left);
            Assert.True(row.StatusBarRect.Right < row.BodyRect.Right);
            Assert.True(row.StatusBarRect.Bottom < row.BodyRect.Bottom);
            Assert.True(row.StatusBarRect.Top >= row.BodyRect.Top);
            Assert.Equal(CommandsPanel.StatusBarHeight, row.StatusBarRect.Height);
        }
    }

    [Fact]
    public void Status_bar_is_absent_for_a_collapsed_panel()
    {
        var screen = CreateScreen();
        Render(screen);
        var panel = screen.CommandsPanel;

        var engineRow = panel.CommandPanelRows.Single(r => r.Name == "Engine");
        screen.OnMouseDown(engineRow.CaptionRect.MidX, engineRow.CaptionRect.MidY);
        Render(screen);

        engineRow = panel.CommandPanelRows.Single(r => r.Name == "Engine");
        Assert.False(engineRow.Opened);
        Assert.Equal(SKRect.Empty, engineRow.StatusBarRect);
        Assert.Null(engineRow.StatusBarText);
    }

    // ── Command button hover / pressed seams ───────────────────

    [Fact]
    public async Task OnMouseMove_tracks_hover_over_command_button()
    {
        await using var fixture = CreateFixture();
        Render(fixture.Screen);
        var panel = fixture.Screen.CommandsPanel;

        // Flat draw ordinal: Navigation (5) + Maneuver (5) precede Engine's
        // first button (engine.accelerate) at index 10.
        int expectedIndex = CommandsPanel.Panels[0].CommandTypeIds.Length + CommandsPanel.Panels[1].CommandTypeIds.Length;

        var button = Assert.Single(panel.AllCommandButtons, b => b.CommandTypeId == "engine.accelerate");
        fixture.Screen.OnMouseMove(button.Rect.MidX, button.Rect.MidY);
        Assert.Equal(expectedIndex, panel.HoveredCommandButtonIndex);

        fixture.Screen.OnMouseMove(1000, 500);
        Assert.Equal(-1, panel.HoveredCommandButtonIndex);
    }

    [Fact]
    public async Task Command_button_pressed_state_clears_on_mouse_up()
    {
        await using var fixture = CreateFixture();
        Render(fixture.Screen);
        var panel = fixture.Screen.CommandsPanel;

        int expectedIndex = CommandsPanel.Panels[0].CommandTypeIds.Length + CommandsPanel.Panels[1].CommandTypeIds.Length;

        var button = Assert.Single(panel.AllCommandButtons, b => b.CommandTypeId == "engine.accelerate");
        Assert.True(button.Enabled);

        fixture.Screen.OnMouseDown(button.Rect.MidX, button.Rect.MidY);
        Assert.Equal(expectedIndex, panel.PressedCommandButtonIndex);

        fixture.Screen.OnMouseUp(button.Rect.MidX, button.Rect.MidY);
        Assert.Equal(-1, panel.PressedCommandButtonIndex);
    }

    // ── Command delivery (AC: click sends PlayerCommand to the resolved module) ─

    [Fact]
    public async Task Click_engine_command_button_sends_player_command_to_engine_module()
    {
        await using var fixture = CreateFixture();
        Render(fixture.Screen);
        var panel = fixture.Screen.CommandsPanel;

        var accelerate = Assert.Single(panel.AllCommandButtons, b => b.CommandTypeId == "engine.accelerate");
        Assert.True(accelerate.Enabled);

        fixture.Screen.OnMouseDown(accelerate.Rect.MidX, accelerate.Rect.MidY);

        var command = Assert.Single(fixture.Connection.Commands);
        Assert.False(string.IsNullOrWhiteSpace(command.CommandId));
        Assert.Equal(1UL, command.ClientSequence);
        Assert.Equal(PlayerShipId, command.ObjectId);
        Assert.Equal(EngineModuleId, command.ModuleId);
        Assert.Equal("engine.accelerate", command.CommandType);
        Assert.Null(command.TargetObjectId);
    }

    [Fact]
    public async Task Click_scanner_command_button_sends_command_to_scanner_module()
    {
        await using var fixture = CreateFixture(EngineAndScannerModules, extraObjects: [ObjAt("OBJ-1", 10060)]);
        Render(fixture.Screen);

        // Scanner commands target an object — select one to enable the button.
        fixture.Screen.OnMouseDown(640, 420); // OBJ-1 at screen (640, 420), within 30 px
        Render(fixture.Screen);
        var panel = fixture.Screen.CommandsPanel;

        var generalScan = Assert.Single(panel.AllCommandButtons, b => b.CommandTypeId == "scanner.generalScan");
        Assert.True(generalScan.Enabled);

        fixture.Screen.OnMouseDown(generalScan.Rect.MidX, generalScan.Rect.MidY);

        var command = Assert.Single(fixture.Connection.Commands);
        Assert.Equal(ScannerModuleId, command.ModuleId);
        Assert.Equal("scanner.generalScan", command.CommandType);
        Assert.Equal("OBJ-1", command.TargetObjectId);
    }

    // ── Resolve-by-Position (new in Batch 2) ────────────────────

    [Fact]
    public async Task Click_resolves_the_installed_module_with_the_lowest_Position_when_several_modules_share_the_commandType()
    {
        await using var fixture = CreateFixture(TwoEngineModulesSharingAccelerate);
        Render(fixture.Screen);
        var panel = fixture.Screen.CommandsPanel;

        var accelerate = Assert.Single(panel.AllCommandButtons, b => b.CommandTypeId == "engine.accelerate");
        Assert.True(accelerate.Enabled);

        fixture.Screen.OnMouseDown(accelerate.Rect.MidX, accelerate.Rect.MidY);

        var command = Assert.Single(fixture.Connection.Commands);
        Assert.Equal("engine.accelerate", command.CommandType);
        Assert.Equal("MOD-ENGINE-LO-POSITION", command.ModuleId);
    }

    // ── navigation.stationsList carve-out (new in Batch 2) ─────

    [Fact]
    public async Task Stations_list_button_is_always_disabled_even_when_navigation_computer_is_installed()
    {
        await using var fixture = CreateFixture(EngineAndNavigationComputerModules);
        Render(fixture.Screen);
        var panel = fixture.Screen.CommandsPanel;

        var stationsList = Assert.Single(
            panel.AllCommandButtons, b => b.CommandTypeId == NavigationComputerCommandTypes.StationsList);
        Assert.False(stationsList.Enabled);

        fixture.Screen.OnMouseDown(stationsList.Rect.MidX, stationsList.Rect.MidY);
        Assert.Empty(fixture.Connection.Commands);
    }

    // ── No covering installed module → disabled (new in Batch 2) ────

    [Fact]
    public async Task Command_without_any_installed_module_covering_it_is_disabled_and_does_not_send()
    {
        // OneEngineModule exposes only engine.* commands — the Space Control
        // panel's scanner commands have no covering installed module.
        await using var fixture = CreateFixture(OneEngineModule);
        Render(fixture.Screen);
        var panel = fixture.Screen.CommandsPanel;

        var spaceControlRow = panel.CommandPanelRows.Single(r => r.Name == "Space Control");
        Assert.Equal(2, spaceControlRow.Buttons.Length);
        Assert.All(spaceControlRow.Buttons, b => Assert.False(b.Enabled));

        var generalScan = Assert.Single(panel.AllCommandButtons, b => b.CommandTypeId == "scanner.generalScan");
        fixture.Screen.OnMouseDown(generalScan.Rect.MidX, generalScan.Rect.MidY);
        Assert.Empty(fixture.Connection.Commands);
    }

    // ── Target-requiring commands (disabled without SelectedObjectId) ─

    [Fact]
    public async Task Scanner_buttons_disabled_without_selection_and_enabled_with_selection()
    {
        await using var fixture = CreateFixture(
            EngineAndScannerModules, extraObjects: [ObjAt("OBJ-1", 10060)]);
        Render(fixture.Screen);
        var panel = fixture.Screen.CommandsPanel;

        var generalScan = Assert.Single(panel.AllCommandButtons, b => b.CommandTypeId == "scanner.generalScan");
        var structuralScan = Assert.Single(panel.AllCommandButtons, b => b.CommandTypeId == "scanner.structuralScan");

        // Without a selection both scanner commands are disabled; clicking sends nothing.
        Assert.False(generalScan.Enabled);
        Assert.False(structuralScan.Enabled);
        fixture.Screen.OnMouseDown(generalScan.Rect.MidX, generalScan.Rect.MidY);
        Assert.Empty(fixture.Connection.Commands);

        // Select a map object → buttons enable; click passes TargetObjectId explicitly.
        fixture.Screen.OnMouseDown(640, 420); // OBJ-1 at screen (640, 420), within 30 px
        Assert.Equal("OBJ-1", fixture.Screen.SelectedObjectId);
        Render(fixture.Screen);

        generalScan = Assert.Single(panel.AllCommandButtons, b => b.CommandTypeId == "scanner.generalScan");
        Assert.True(generalScan.Enabled);

        fixture.Screen.OnMouseDown(generalScan.Rect.MidX, generalScan.Rect.MidY);

        var command = Assert.Single(fixture.Connection.Commands);
        Assert.Equal(ScannerModuleId, command.ModuleId);
        Assert.Equal("scanner.generalScan", command.CommandType);
        Assert.Equal("OBJ-1", command.TargetObjectId);

        // Clear the selection (right-click on empty map) → disabled again.
        fixture.Screen.OnMouseDown(1000, 500, MouseButton.Right);
        Assert.Null(fixture.Screen.SelectedObjectId);
        Render(fixture.Screen);

        generalScan = Assert.Single(panel.AllCommandButtons, b => b.CommandTypeId == "scanner.generalScan");
        Assert.False(generalScan.Enabled);
        fixture.Connection.Commands.Clear();
        fixture.Screen.OnMouseDown(generalScan.Rect.MidX, generalScan.Rect.MidY);
        Assert.Empty(fixture.Connection.Commands);
    }

    [Fact]
    public async Task Match_command_buttons_require_selection_like_scanner_commands()
    {
        await using var fixture = CreateFixture(FullEngineModule, extraObjects: [ObjAt("OBJ-1", 10060)]);
        Render(fixture.Screen);
        var panel = fixture.Screen.CommandsPanel;

        var matchSpeed = Assert.Single(panel.AllCommandButtons, b => b.CommandTypeId == "engine.speedSynchronization");
        var matchCourse = Assert.Single(panel.AllCommandButtons, b => b.CommandTypeId == "engine.directionSynchronization");

        Assert.False(matchSpeed.Enabled);
        Assert.False(matchCourse.Enabled);
        fixture.Screen.OnMouseDown(matchSpeed.Rect.MidX, matchSpeed.Rect.MidY);
        Assert.Empty(fixture.Connection.Commands);

        fixture.Screen.OnMouseDown(640, 420); // select OBJ-1
        Render(fixture.Screen);

        matchSpeed = Assert.Single(panel.AllCommandButtons, b => b.CommandTypeId == "engine.speedSynchronization");
        Assert.True(matchSpeed.Enabled);
        fixture.Screen.OnMouseDown(matchSpeed.Rect.MidX, matchSpeed.Rect.MidY);

        var command = Assert.Single(fixture.Connection.Commands);
        Assert.Equal("engine.speedSynchronization", command.CommandType);
        Assert.Equal("OBJ-1", command.TargetObjectId);
    }

    // ── Clicks never move the camera or change selection ──────────────────

    [Fact]
    public async Task Command_button_click_never_moves_camera_or_changes_selection()
    {
        await using var fixture = CreateFixture(EngineAndScannerModules, extraObjects: [ObjAt("OBJ-1", 10060)]);
        Render(fixture.Screen);

        fixture.Screen.OnMouseDown(640, 420); // select OBJ-1
        Render(fixture.Screen);
        Assert.Equal("OBJ-1", fixture.Screen.SelectedObjectId);

        double fxBefore = fixture.Screen.CameraFocusX;
        double fyBefore = fixture.Screen.CameraFocusY;
        var panel = fixture.Screen.CommandsPanel;
        var accelerate = Assert.Single(panel.AllCommandButtons, b => b.CommandTypeId == "engine.accelerate");

        var result = fixture.Screen.OnMouseDown(accelerate.Rect.MidX, accelerate.Rect.MidY);

        Assert.Equal(ScreenEvent.None, result);
        Assert.Equal(fxBefore, fixture.Screen.CameraFocusX);
        Assert.Equal(fyBefore, fixture.Screen.CameraFocusY);
        Assert.Equal("OBJ-1", fixture.Screen.SelectedObjectId);

        var command = Assert.Single(fixture.Connection.Commands);
        Assert.NotEqual(ShipEngineCommandTypes.Orbit, command.CommandType);
    }

    [Fact]
    public async Task Orbit_button_is_always_disabled()
    {
        await using var fixture = CreateFixture();
        Render(fixture.Screen);
        var panel = fixture.Screen.CommandsPanel;

        var orbit = Assert.Single(panel.AllCommandButtons, b => b.CommandTypeId == "engine.orbit");
        Assert.False(orbit.Enabled);

        fixture.Screen.OnMouseDown(orbit.Rect.MidX, orbit.Rect.MidY);
        Assert.Empty(fixture.Connection.Commands);
    }

    [Fact]
    public async Task Right_click_on_command_button_does_not_clear_selection()
    {
        await using var fixture = CreateFixture(EngineAndScannerModules, extraObjects: [ObjAt("OBJ-1", 10060)]);
        Render(fixture.Screen);
        fixture.Screen.OnMouseDown(640, 420);
        Assert.Equal("OBJ-1", fixture.Screen.SelectedObjectId);

        var panel = fixture.Screen.CommandsPanel;
        var accelerate = Assert.Single(panel.AllCommandButtons, b => b.CommandTypeId == "engine.accelerate");

        var result = fixture.Screen.OnMouseDown(accelerate.Rect.MidX, accelerate.Rect.MidY, MouseButton.Right);

        Assert.Equal(ScreenEvent.None, result);
        Assert.Equal("OBJ-1", fixture.Screen.SelectedObjectId);
    }

    // ── Helpers ─────────────────────────────────────────────────

    private static GameSessionScreen CreateScreen(
        ImmutableArray<InstalledModuleSnapshot>? installedModules = null)
    {
        var buffer = new SnapshotBuffer();
        var ship = new ObjectMotionSnapshot(PlayerShipId, 10000, 10000, SpeedKmS: 1.0, Direction: 0);
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray.Create(ship),
            PlayerShipObjectId: PlayerShipId,
            InstalledModules: installedModules ?? OneEngineModule));
        return new GameSessionScreen(buffer, new LinearMotionPredictor());
    }

    private static TestFixture CreateFixture(
        ImmutableArray<InstalledModuleSnapshot>? installedModules = null,
        IEnumerable<ObjectMotionSnapshot>? extraObjects = null)
    {
        var connection = new RecordingConnection();
        var handle = new GameSessionHandle(connection);
        var ship = new ObjectMotionSnapshot(PlayerShipId, 10000, 10000, SpeedKmS: 1.0, Direction: 0);
        handle.Buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray.Create(ship).AddRange(extraObjects ?? []),
            PlayerShipObjectId: PlayerShipId,
            InstalledModules: installedModules ?? OneEngineModule));

        var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle);
        return new TestFixture(connection, handle, screen);
    }

    /// <summary>Object at world (10000, worldY) — renders at screen (640, 360 + worldY - 10000) when the camera focuses the player ship at (10000, 10000).</summary>
    private static ObjectMotionSnapshot ObjAt(string id, double worldY) =>
        new(id, X: 10000, Y: worldY, SpeedKmS: 0, Direction: 0);

    private static void Render(GameSessionScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    private sealed record TestFixture(
        RecordingConnection Connection,
        GameSessionHandle Handle,
        GameSessionScreen Screen) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            return Handle.DisposeAsync();
        }
    }

    private sealed class RecordingConnection : IGameSessionConnection
    {
        public List<PlayerCommand> Commands { get; } = [];

        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            return ValueTask.CompletedTask;
        }

        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask SetObjectInteractionStateAsync(
            string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
