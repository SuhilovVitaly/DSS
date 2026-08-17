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

    /// <summary>
    /// Display names and targets mirror Client/Data/command-definitions.json —
    /// the panel must render labels and enablement from this metadata.
    /// </summary>
    private static readonly Dictionary<string, (string Name, string Target)> CommandMetadata = new()
    {
        ["engine.accelerate"] = ("Accelerate", "none"),
        ["engine.brake"] = ("Brake", "none"),
        ["engine.maintain-speed"] = ("Maintain Speed", "none"),
        ["engine.turn-left-step"] = ("Turn Left Step", "none"),
        ["engine.turn-right-step"] = ("Turn Right Step", "none"),
        ["engine.turn-left-until-cancel"] = ("Turn Left Until Cancel", "none"),
        ["engine.turn-right-until-cancel"] = ("Turn Right Until Cancel", "none"),
        ["engine.maintain-course"] = ("Maintain Course", "none"),
        ["engine.speed-synchronization"] = ("Speed Synchronization", "object"),
        ["engine.direction-synchronization"] = ("Direction Synchronization", "object"),
        ["engine.orbit"] = ("Orbit", "point"),
        ["scanner.general-scan"] = ("General Scan", "object"),
        ["scanner.structural-scan"] = ("Structural Scan", "object"),
    };

    private static ImmutableArray<ModuleCommandSnapshot> CommandsFor(ImmutableArray<string> typeIds) =>
        typeIds.Select(id => CommandMetadata.TryGetValue(id, out var meta)
            ? new ModuleCommandSnapshot(id, meta.Name, meta.Target)
            : new ModuleCommandSnapshot(id, id, "none")).ToImmutableArray();

    private static readonly ImmutableArray<string> EngineCommandTypeIds = ImmutableArray.Create(
        "engine.accelerate", "engine.brake", "engine.maintain-speed",
        "engine.orbit");

    private static readonly ImmutableArray<string> FullEngineCommandTypeIds = ImmutableArray.Create(
        "engine.accelerate", "engine.brake", "engine.maintain-speed",
        "engine.turn-left-step", "engine.turn-right-step",
        "engine.turn-left-until-cancel", "engine.turn-right-until-cancel",
        "engine.maintain-course",
        "engine.speed-synchronization", "engine.direction-synchronization",
        "engine.orbit");

    private static readonly ImmutableArray<string> ScannerCommandTypeIds = ImmutableArray.Create(
        "scanner.general-scan", "scanner.structural-scan");

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

    // ── Geometry ────────────────────────────────────────────────

    [Fact]
    public void Panel_renders_top_left_with_caption_360x32_and_body_depends_on_modules()
    {
        var screen = CreateScreen();
        Render(screen);

        Assert.Equal(new SKRect(8, 8, 368, 40), screen.CommandsPanel.CaptionRect);
        Assert.Equal(new SKRect(8, 40, 368, 240), screen.CommandsPanel.BodyRect);
    }

    [Fact]
    public void Panel_state_is_AllModules_by_default()
    {
        var screen = CreateScreen();
        Assert.Equal(CommandsPanelState.AllModules, screen.CommandsPanel.State);
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
    public async Task Click_on_module_caption_is_consumed_and_does_not_pan()
    {
        await using var fixture = CreateFixture();
        Render(fixture.Screen);

        double fxBefore = fixture.Screen.CameraFocusX;
        double fyBefore = fixture.Screen.CameraFocusY;
        var row = Assert.Single(fixture.Screen.CommandsPanel.ModuleRows);

        var result = fixture.Screen.OnMouseDown(row.CaptionRect.MidX, row.CaptionRect.MidY);

        Assert.Equal(ScreenEvent.None, result);
        Assert.Equal(fxBefore, fixture.Screen.CameraFocusX);
        Assert.Equal(fyBefore, fixture.Screen.CameraFocusY);
        Assert.Empty(fixture.Connection.Commands);
    }

    [Fact]
    public async Task Click_on_module_body_gap_is_consumed_and_sends_no_command()
    {
        await using var fixture = CreateFixture();
        Render(fixture.Screen);
        var panel = fixture.Screen.CommandsPanel;

        var row = Assert.Single(panel.ModuleRows);
        Assert.True(row.Opened);
        Assert.True(row.BodyRect.Height > 0);

        double fxBefore = fixture.Screen.CameraFocusX;
        double fyBefore = fixture.Screen.CameraFocusY;

        // Guaranteed gap: below the command button grid, inside the module body
        // (the engine row's 4 buttons occupy the top 32px of the body).
        var gap = (panel.BodyRect.Left + 4f, panel.BodyRect.Bottom - 4f);
        Assert.True(row.BodyRect.Contains(gap.Item1, gap.Item2));
        Assert.DoesNotContain(panel.AllCommandButtons, b => b.Rect.Contains(gap.Item1, gap.Item2));

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
    public async Task Bottom_center_engine_panel_still_renders()
    {
        await using var fixture = CreateFixture();
        Render(fixture.Screen);

        Assert.Equal(ScreenWidth / 2f, fixture.Screen.LastCommandPanelRect.MidX, precision: 3);
        Assert.Equal(8, fixture.Screen.EngineCommandButtonRects.Count);
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

        Assert.Equal(CommandsPanelState.AllModules, panel.State);

        screen.OnMouseDown(panel.HideShowButtonRect.MidX, panel.HideShowButtonRect.MidY);
        Render(screen);

        Assert.Equal(CommandsPanelState.Closed, panel.State);
        Assert.Equal(CommandsPanelState.AllModules, panel.PreviousNonClosedState);
        Assert.Equal(0f, panel.BodyRect.Height);
        Assert.Empty(panel.ModuleRows);
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

        Assert.Equal(CommandsPanelState.AllModules, panel.State);
        Assert.NotEmpty(panel.ModuleRows);
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

    // ── Module row geometry ─────────────────────────────────────

    [Fact]
    public void Module_caption_is_full_width_36px_and_body_is_360x164()
    {
        var screen = CreateScreen();
        Render(screen);
        var panel = screen.CommandsPanel;

        var row = Assert.Single(panel.ModuleRows);

        Assert.True(row.Opened);
        Assert.Equal(CommandsPanel.PanelWidth, row.CaptionRect.Width);
        Assert.Equal(CommandsPanel.ModuleCaptionHeight, row.CaptionRect.Height);
        Assert.Equal(CommandsPanel.PanelWidth, row.BodyRect.Width);
        Assert.Equal(164f, row.BodyRect.Height);
        Assert.Equal(row.CaptionRect.Bottom, row.BodyRect.Top);
        Assert.Equal(row.CaptionRect.Left, row.BodyRect.Left);

        screen.OnMouseDown(row.CaptionRect.MidX, row.CaptionRect.MidY);
        Render(screen);

        row = Assert.Single(panel.ModuleRows);
        Assert.False(row.Opened);
        Assert.Equal(0f, row.BodyRect.Height);
    }

    [Fact]
    public void Module_rows_ordered_by_Position()
    {
        var modules = ImmutableArray.Create(
            new InstalledModuleSnapshot("M2", "mt.a", "Alpha", Position: 2, EngineCommandTypeIds),
            new InstalledModuleSnapshot("M0", "mt.b", "Beta", Position: 0, EngineCommandTypeIds));

        var screen = CreateScreen(modules);
        Render(screen);

        var rows = screen.CommandsPanel.ModuleRows;
        Assert.Equal(2, rows.Count);
        Assert.Equal(0, rows[0].Position);
        Assert.Equal(2, rows[1].Position);
        Assert.Equal(40f, rows[0].CaptionRect.Top);
        Assert.Equal(240f, rows[1].CaptionRect.Top);
    }

    [Fact]
    public void Engine_module_always_sorts_first()
    {
        var modules = ImmutableArray.Create(
            new InstalledModuleSnapshot("M-E", "module.engine.basic", "Engine", Position: 5, EngineCommandTypeIds),
            new InstalledModuleSnapshot("M-S", "module.scanner.mk1", "Scanner MK I", Position: 0,
                ImmutableArray.Create("scanner.general-scan")));

        var screen = CreateScreen(modules);
        Render(screen);

        Assert.Equal(2, screen.CommandsPanel.ModuleRows.Count);
        Assert.Equal("Engine", screen.CommandsPanel.ModuleRows[0].DisplayName);
        Assert.Equal("Scanner MK I", screen.CommandsPanel.ModuleRows[1].DisplayName);
    }

    [Fact]
    public void Module_toggle_switches_Opened_state()
    {
        var screen = CreateScreen();
        Render(screen);
        var panel = screen.CommandsPanel;

        var row = Assert.Single(panel.ModuleRows);
        Assert.True(row.Opened);

        screen.OnMouseDown(row.CaptionRect.MidX, row.CaptionRect.MidY);
        Render(screen);
        Assert.False(Assert.Single(panel.ModuleRows).Opened);

        screen.OnMouseDown(row.CaptionRect.MidX, row.CaptionRect.MidY);
        Render(screen);
        Assert.True(Assert.Single(panel.ModuleRows).Opened);
    }

    // ── Filtering ───────────────────────────────────────────────

    [Fact]
    public void Module_with_empty_CommandTypeIds_is_not_shown()
    {
        var modules = ImmutableArray.Create(
            new InstalledModuleSnapshot("M-E", "module.engine.basic", "Engine", Position: 0, EngineCommandTypeIds),
            new InstalledModuleSnapshot("M-S", "module.scanner.mk1", "Scanner MK I", Position: 1, ImmutableArray<string>.Empty));

        var screen = CreateScreen(modules);
        Render(screen);

        var rows = screen.CommandsPanel.ModuleRows;
        Assert.Single(rows);
        Assert.Equal("Engine", rows[0].DisplayName);
    }

    [Fact]
    public void Only_modules_with_nonempty_CommandTypeIds_are_active()
    {
        var screen = CreateScreen();
        Render(screen);

        var rows = screen.CommandsPanel.ModuleRows;
        Assert.Single(rows);
        Assert.Equal("Engine", rows[0].DisplayName);
        Assert.Equal(EngineModuleId, rows[0].ModuleId);
    }

    [Fact]
    public void Empty_snapshot_shows_only_caption()
    {
        var screen = CreateScreen(ImmutableArray<InstalledModuleSnapshot>.Empty);
        Render(screen);
        var panel = screen.CommandsPanel;

        Assert.Empty(panel.ModuleRows);
        Assert.Equal(new SKRect(8, 40, 368, 40), panel.BodyRect);
    }

    // ── Command buttons (ТЗ-04, AC1: one button per CommandTypeIds entry) ───────

    [Fact]
    public void Opened_row_draws_one_button_per_CommandTypeIds_entry_in_order()
    {
        var screen = CreateScreen();
        Render(screen);
        var panel = screen.CommandsPanel;

        var row = Assert.Single(panel.ModuleRows);
        Assert.Equal(4, row.Buttons.Length);
        Assert.Equal(4, panel.AllCommandButtons.Count);

        Assert.Equal(EngineCommandTypeIds, row.Buttons.Select(b => b.CommandTypeId));
        Assert.Equal(EngineCommandTypeIds, panel.AllCommandButtons.Select(b => b.CommandTypeId));
    }

    [Fact]
    public void Command_button_grid_layout_is_4_columns_with_expected_geometry()
    {
        var screen = CreateScreen();
        Render(screen);
        var row = Assert.Single(screen.CommandsPanel.ModuleRows);

        // Module body (8, 76, 368, 240); grid origin = body + (6, 6);
        // button 84x32, gap 4 → columns at x = 14, 102, 190, 278.
        Assert.Equal(new SKRect(14, 82, 98, 114), row.Buttons[0].Rect);
        Assert.Equal(new SKRect(102, 82, 186, 114), row.Buttons[1].Rect);
        Assert.Equal(new SKRect(190, 82, 274, 114), row.Buttons[2].Rect);
        Assert.Equal(new SKRect(278, 82, 362, 114), row.Buttons[3].Rect);

        Assert.Equal(84f, row.Buttons[0].Rect.Width);
        Assert.Equal(32f, row.Buttons[0].Rect.Height);
    }

    [Fact]
    public void Eleven_command_ids_wrap_into_three_rows_of_4_4_3()
    {
        var screen = CreateScreen(FullEngineModule);
        Render(screen);
        var panel = screen.CommandsPanel;

        var row = Assert.Single(panel.ModuleRows);
        Assert.Equal(11, row.Buttons.Length);
        Assert.Equal(11, panel.AllCommandButtons.Count);

        // Row 0 (y 82..114): 4 buttons; row 1 (y 118..150): 4; row 2 (y 154..186): 3.
        Assert.Equal(new SKRect(14, 82, 98, 114), row.Buttons[0].Rect);
        Assert.Equal(new SKRect(278, 82, 362, 114), row.Buttons[3].Rect);
        Assert.Equal(new SKRect(14, 118, 98, 150), row.Buttons[4].Rect);
        Assert.Equal(new SKRect(278, 118, 362, 150), row.Buttons[7].Rect);
        Assert.Equal(new SKRect(14, 154, 98, 186), row.Buttons[8].Rect);
        Assert.Equal(new SKRect(102, 154, 186, 186), row.Buttons[9].Rect);
        Assert.Equal(new SKRect(190, 154, 274, 186), row.Buttons[10].Rect);
    }

    [Fact]
    public void Collapsed_row_has_no_command_buttons()
    {
        var screen = CreateScreen();
        Render(screen);
        var panel = screen.CommandsPanel;

        var row = Assert.Single(panel.ModuleRows);
        screen.OnMouseDown(row.CaptionRect.MidX, row.CaptionRect.MidY);
        Render(screen);

        row = Assert.Single(panel.ModuleRows);
        Assert.False(row.Opened);
        Assert.Empty(row.Buttons);
        Assert.Empty(panel.AllCommandButtons);
    }

    // ── Command button hover / pressed seams ───────────────────

    [Fact]
    public async Task OnMouseMove_tracks_hover_over_command_button()
    {
        await using var fixture = CreateFixture();
        Render(fixture.Screen);
        var panel = fixture.Screen.CommandsPanel;

        var button = Assert.Single(panel.AllCommandButtons, b => b.CommandTypeId == "engine.accelerate");
        fixture.Screen.OnMouseMove(button.Rect.MidX, button.Rect.MidY);
        Assert.Equal(0, panel.HoveredCommandButtonIndex);

        fixture.Screen.OnMouseMove(1000, 500);
        Assert.Equal(-1, panel.HoveredCommandButtonIndex);
    }

    [Fact]
    public async Task Command_button_pressed_state_clears_on_mouse_up()
    {
        await using var fixture = CreateFixture();
        Render(fixture.Screen);
        var panel = fixture.Screen.CommandsPanel;

        var button = Assert.Single(panel.AllCommandButtons, b => b.CommandTypeId == "engine.accelerate");
        Assert.True(button.Enabled);

        fixture.Screen.OnMouseDown(button.Rect.MidX, button.Rect.MidY);
        Assert.Equal(0, panel.PressedCommandButtonIndex);

        fixture.Screen.OnMouseUp(button.Rect.MidX, button.Rect.MidY);
        Assert.Equal(-1, panel.PressedCommandButtonIndex);
    }

    // ── Command delivery (ТЗ-04, AC2: click sends PlayerCommand to row's ModuleId) ─

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

        // Scanner commands target an object — select one to enable the button (AC3).
        fixture.Screen.OnMouseDown(640, 420); // OBJ-1 at screen (640, 420), within 30 px
        Render(fixture.Screen);
        var panel = fixture.Screen.CommandsPanel;

        var scannerRow = panel.ModuleRows.Single(r => r.ModuleId == ScannerModuleId);
        var generalScan = Assert.Single(scannerRow.Buttons, b => b.CommandTypeId == "scanner.general-scan");
        Assert.True(generalScan.Enabled);

        fixture.Screen.OnMouseDown(generalScan.Rect.MidX, generalScan.Rect.MidY);

        var command = Assert.Single(fixture.Connection.Commands);
        Assert.Equal(ScannerModuleId, command.ModuleId);
        Assert.Equal("scanner.general-scan", command.CommandType);
        Assert.Equal("OBJ-1", command.TargetObjectId);
    }

    // ── Target-requiring commands (ТЗ-04, AC3: disabled without SelectedObjectId) ─

    [Fact]
    public async Task Scanner_buttons_disabled_without_selection_and_enabled_with_selection()
    {
        await using var fixture = CreateFixture(
            EngineAndScannerModules, extraObjects: [ObjAt("OBJ-1", 10060)]);
        Render(fixture.Screen);
        var panel = fixture.Screen.CommandsPanel;

        var scannerRow = panel.ModuleRows.Single(r => r.ModuleId == ScannerModuleId);
        var generalScan = Assert.Single(scannerRow.Buttons, b => b.CommandTypeId == "scanner.general-scan");
        var structuralScan = Assert.Single(scannerRow.Buttons, b => b.CommandTypeId == "scanner.structural-scan");

        // Without a selection both scanner commands are disabled; clicking sends nothing.
        Assert.False(generalScan.Enabled);
        Assert.False(structuralScan.Enabled);
        fixture.Screen.OnMouseDown(generalScan.Rect.MidX, generalScan.Rect.MidY);
        Assert.Empty(fixture.Connection.Commands);

        // Select a map object → buttons enable; click passes TargetObjectId explicitly.
        fixture.Screen.OnMouseDown(640, 420); // OBJ-1 at screen (640, 420), within 30 px
        Assert.Equal("OBJ-1", fixture.Screen.SelectedObjectId);
        Render(fixture.Screen);

        generalScan = Assert.Single(panel.ModuleRows.Single(r => r.ModuleId == ScannerModuleId).Buttons,
            b => b.CommandTypeId == "scanner.general-scan");
        Assert.True(generalScan.Enabled);

        fixture.Screen.OnMouseDown(generalScan.Rect.MidX, generalScan.Rect.MidY);

        var command = Assert.Single(fixture.Connection.Commands);
        Assert.Equal(ScannerModuleId, command.ModuleId);
        Assert.Equal("scanner.general-scan", command.CommandType);
        Assert.Equal("OBJ-1", command.TargetObjectId);

        // Clear the selection (right-click on empty map) → disabled again.
        fixture.Screen.OnMouseDown(1000, 500, MouseButton.Right);
        Assert.Null(fixture.Screen.SelectedObjectId);
        Render(fixture.Screen);

        generalScan = Assert.Single(panel.ModuleRows.Single(r => r.ModuleId == ScannerModuleId).Buttons,
            b => b.CommandTypeId == "scanner.general-scan");
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

        var matchSpeed = Assert.Single(panel.AllCommandButtons, b => b.CommandTypeId == "engine.speed-synchronization");
        var matchCourse = Assert.Single(panel.AllCommandButtons, b => b.CommandTypeId == "engine.direction-synchronization");

        Assert.False(matchSpeed.Enabled);
        Assert.False(matchCourse.Enabled);
        fixture.Screen.OnMouseDown(matchSpeed.Rect.MidX, matchSpeed.Rect.MidY);
        Assert.Empty(fixture.Connection.Commands);

        fixture.Screen.OnMouseDown(640, 420); // select OBJ-1
        Render(fixture.Screen);

        matchSpeed = Assert.Single(panel.AllCommandButtons, b => b.CommandTypeId == "engine.speed-synchronization");
        Assert.True(matchSpeed.Enabled);
        fixture.Screen.OnMouseDown(matchSpeed.Rect.MidX, matchSpeed.Rect.MidY);

        var command = Assert.Single(fixture.Connection.Commands);
        Assert.Equal("engine.speed-synchronization", command.CommandType);
        Assert.Equal("OBJ-1", command.TargetObjectId);
    }

    // ── AC4: clicks never move the camera or change selection ──────────────────

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

        var navigate = Assert.Single(panel.AllCommandButtons, b => b.CommandTypeId == "engine.orbit");
        Assert.False(navigate.Enabled);

        fixture.Screen.OnMouseDown(navigate.Rect.MidX, navigate.Rect.MidY);
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

        public ValueTask SaveAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
