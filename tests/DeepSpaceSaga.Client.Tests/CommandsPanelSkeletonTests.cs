using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class CommandsPanelSkeletonTests
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;
    private const string PlayerShipId = "SPC-0001";
    private const string EngineModuleId = "MOD-PLAYER-ENGINE-01";

    private static readonly ImmutableArray<string> EngineCommandTypeIds = ImmutableArray.Create(
        "engine.accelerate", "engine.brake", "engine.maintain-speed",
        "engine.navigate-to-point");

    private static readonly ImmutableArray<InstalledModuleSnapshot> OneEngineModule = ImmutableArray.Create(
        new InstalledModuleSnapshot(EngineModuleId, "module.engine.basic", "Engine", Position: 1, EngineCommandTypeIds));

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
    public async Task Click_on_module_body_is_consumed_and_sends_no_command()
    {
        await using var fixture = CreateFixture();
        Render(fixture.Screen);
        var panel = fixture.Screen.CommandsPanel;

        var row = Assert.Single(panel.ModuleRows);
        Assert.True(row.Opened);
        Assert.True(row.BodyRect.Height > 0);

        double fxBefore = fixture.Screen.CameraFocusX;
        double fyBefore = fixture.Screen.CameraFocusY;

        var result = fixture.Screen.OnMouseDown(row.BodyRect.MidX, row.BodyRect.MidY);

        Assert.Equal(ScreenEvent.None, result);
        Assert.Equal(fxBefore, fixture.Screen.CameraFocusX);
        Assert.Equal(fyBefore, fixture.Screen.CameraFocusY);
        Assert.Empty(fixture.Connection.Commands);
    }

    [Fact]
    public void Click_outside_panel_still_pans_camera()
    {
        var screen = CreateScreen();
        Render(screen);

        double fxBefore = screen.CameraFocusX;
        double fyBefore = screen.CameraFocusY;

        var result = screen.OnMouseDown(1000, 500);

        Assert.Equal(ScreenEvent.None, result);
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
                ImmutableArray.Create("scanner.deep-scan")));

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
        ImmutableArray<InstalledModuleSnapshot>? installedModules = null)
    {
        var connection = new RecordingConnection();
        var handle = new GameSessionHandle(connection);
        var ship = new ObjectMotionSnapshot(PlayerShipId, 10000, 10000, SpeedKmS: 1.0, Direction: 0);
        handle.Buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray.Create(ship),
            PlayerShipObjectId: PlayerShipId,
            InstalledModules: installedModules ?? OneEngineModule));

        var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle);
        return new TestFixture(connection, handle, screen);
    }

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
