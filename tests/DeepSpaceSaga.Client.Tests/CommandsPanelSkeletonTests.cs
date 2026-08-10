using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// Commands Panel tests (ТЗ подзадачи 1+2, CommandPanelPlan.md): geometry,
/// hit-test consumption, state-machine, buttons and regressions.
/// </summary>
public class CommandsPanelSkeletonTests
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;
    private const string PlayerShipId = "SPC-0001";

    // ── Skeleton geometry / hit-test (подзадача 1 regressions) ──

    [Fact]
    public void Panel_renders_top_left_with_caption_360x40_and_body_360_wide()
    {
        var screen = CreateScreen();
        Render(screen);

        Assert.Equal(new SKRect(8, 8, 368, 48), screen.CommandsPanel.CaptionRect);
        Assert.Equal(new SKRect(8, 48, 368, 248), screen.CommandsPanel.BodyRect);
        Assert.Equal(screen.CommandsPanel.CaptionRect.Left, screen.CommandsPanel.BodyRect.Left);
        Assert.Equal(screen.CommandsPanel.CaptionRect.Bottom, screen.CommandsPanel.BodyRect.Top);
    }

    [Fact]
    public void Panel_state_is_Opened_by_default()
    {
        var screen = CreateScreen();

        Assert.Equal(CommandsPanelState.Opened, screen.CommandsPanel.State);
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
    public async Task Click_on_body_is_consumed()
    {
        await using var fixture = CreateFixture();
        Render(fixture.Screen);

        double fxBefore = fixture.Screen.CameraFocusX;
        double fyBefore = fixture.Screen.CameraFocusY;
        var body = fixture.Screen.CommandsPanel.BodyRect;

        var result = fixture.Screen.OnMouseDown(body.MidX, body.MidY);

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

    // ── Buttons geometry (подзадача 2) ──────────────────────────

    [Fact]
    public void Buttons_are_32x32_positioned_left_in_caption_with_10px_padding()
    {
        var screen = CreateScreen();
        Render(screen);

        var panel = screen.CommandsPanel;

        // Hide button: first button, 10 px from panel left edge (= Margin + 10 = 18).
        Assert.Equal(new SKRect(18, 12, 50, 44), panel.HideButtonRect);
        Assert.Equal(CommandsPanel.ButtonSize, panel.HideButtonRect.Width);
        Assert.Equal(CommandsPanel.ButtonSize, panel.HideButtonRect.Height);

        // Show button: 4 px gap after Hide.
        Assert.Equal(new SKRect(54, 12, 86, 44), panel.ShowButtonRect);

        // Show Active button: 4 px gap after Show.
        Assert.Equal(new SKRect(90, 12, 122, 44), panel.ShowActiveButtonRect);

        // Buttons are vertically centred in the 40 px caption: top = (40-32)/2 + Margin = 12.
        Assert.Equal(12f, panel.HideButtonRect.Top);
        Assert.Equal(12f, panel.ShowButtonRect.Top);
        Assert.Equal(12f, panel.ShowActiveButtonRect.Top);

        // All buttons are inside the caption rect.
        Assert.True(panel.CaptionRect.Contains(panel.HideButtonRect));
        Assert.True(panel.CaptionRect.Contains(panel.ShowButtonRect));
        Assert.True(panel.CaptionRect.Contains(panel.ShowActiveButtonRect));
    }

    // ── State machine (подзадача 2) ─────────────────────────────

    [Fact]
    public void Hide_button_sets_state_Closed_and_body_height_is_zero()
    {
        var screen = CreateScreen();
        Render(screen);
        var panel = screen.CommandsPanel;

        var hideBtn = panel.HideButtonRect;
        screen.OnMouseDown(hideBtn.MidX, hideBtn.MidY);
        Render(screen); // re-layout after state change

        Assert.Equal(CommandsPanelState.Closed, panel.State);
        Assert.Equal(0f, panel.BodyRect.Height);
    }

    [Fact]
    public void Show_button_sets_state_Opened_and_body_is_visible()
    {
        var screen = CreateScreen();
        Render(screen);
        var panel = screen.CommandsPanel;

        // Go to Closed first.
        screen.OnMouseDown(panel.HideButtonRect.MidX, panel.HideButtonRect.MidY);
        Render(screen);
        Assert.Equal(CommandsPanelState.Closed, panel.State);

        // Now Show → Opened.
        screen.OnMouseDown(panel.ShowButtonRect.MidX, panel.ShowButtonRect.MidY);
        Render(screen);

        Assert.Equal(CommandsPanelState.Opened, panel.State);
        Assert.True(panel.BodyRect.Height > 0);
    }

    [Fact]
    public void ShowActive_button_sets_state_ActiveModules_and_body_is_visible()
    {
        var screen = CreateScreen();
        Render(screen);
        var panel = screen.CommandsPanel;

        var showActiveBtn = panel.ShowActiveButtonRect;
        screen.OnMouseDown(showActiveBtn.MidX, showActiveBtn.MidY);
        Render(screen);

        Assert.Equal(CommandsPanelState.ActiveModules, panel.State);
        Assert.True(panel.BodyRect.Height > 0);
    }

    [Fact]
    public void Button_clicks_consume_the_click_and_do_not_pan()
    {
        var screen = CreateScreen();
        Render(screen);
        var panel = screen.CommandsPanel;

        foreach (var btn in new[] { panel.HideButtonRect, panel.ShowButtonRect, panel.ShowActiveButtonRect })
        {
            // Re-open so every button click is a state transition.
            screen.OnMouseDown(panel.ShowButtonRect.MidX, panel.ShowButtonRect.MidY);
            Render(screen);

            double fxBefore = screen.CameraFocusX;
            double fyBefore = screen.CameraFocusY;

            var result = screen.OnMouseDown(btn.MidX, btn.MidY);

            Assert.Equal(ScreenEvent.None, result);
            Assert.Equal(fxBefore, screen.CameraFocusX);
            Assert.Equal(fyBefore, screen.CameraFocusY);
        }
    }

    [Fact]
    public async Task State_changes_do_not_send_engine_commands()
    {
        await using var fixture = CreateFixture();
        Render(fixture.Screen);
        var panel = fixture.Screen.CommandsPanel;

        // Click all three buttons.
        fixture.Screen.OnMouseDown(panel.HideButtonRect.MidX, panel.HideButtonRect.MidY);
        Render(fixture.Screen);

        fixture.Screen.OnMouseDown(panel.ShowButtonRect.MidX, panel.ShowButtonRect.MidY);
        Render(fixture.Screen);

        fixture.Screen.OnMouseDown(panel.ShowActiveButtonRect.MidX, panel.ShowActiveButtonRect.MidY);
        Render(fixture.Screen);

        Assert.Empty(fixture.Connection.Commands);
    }

    // ── Hover tracking ──────────────────────────────────────────

    [Fact]
    public void OnMouseMove_tracks_hover_over_buttons()
    {
        var screen = CreateScreen();
        Render(screen);
        var panel = screen.CommandsPanel;

        screen.OnMouseMove(panel.HideButtonRect.MidX, panel.HideButtonRect.MidY);
        Assert.Equal(0, panel.HoveredButtonIndex);

        screen.OnMouseMove(panel.ShowButtonRect.MidX, panel.ShowButtonRect.MidY);
        Assert.Equal(1, panel.HoveredButtonIndex);

        screen.OnMouseMove(panel.ShowActiveButtonRect.MidX, panel.ShowActiveButtonRect.MidY);
        Assert.Equal(2, panel.HoveredButtonIndex);

        // Move outside.
        screen.OnMouseMove(1000, 500);
        Assert.Equal(-1, panel.HoveredButtonIndex);
    }

    [Fact]
    public void OnMouseUp_clears_pressed_button()
    {
        var screen = CreateScreen();
        Render(screen);
        var panel = screen.CommandsPanel;

        screen.OnMouseDown(panel.HideButtonRect.MidX, panel.HideButtonRect.MidY);
        Assert.Equal(0, panel.PressedButtonIndex);

        screen.OnMouseUp(panel.HideButtonRect.MidX, panel.HideButtonRect.MidY);
        Assert.Equal(-1, panel.PressedButtonIndex);
    }

    // ── Helpers ─────────────────────────────────────────────────

    private static GameSessionScreen CreateScreen()
    {
        var buffer = new SnapshotBuffer();
        var ship = new ObjectMotionSnapshot(PlayerShipId, 10000, 10000, SpeedKmS: 1.0, Direction: 0);
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray.Create(ship),
            PlayerShipObjectId: PlayerShipId));
        return new GameSessionScreen(buffer, new LinearMotionPredictor());
    }

    private static TestFixture CreateFixture()
    {
        var connection = new RecordingConnection();
        var handle = new GameSessionHandle(connection);
        var ship = new ObjectMotionSnapshot(PlayerShipId, 10000, 10000, SpeedKmS: 1.0, Direction: 0);
        handle.Buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray.Create(ship),
            PlayerShipObjectId: PlayerShipId));

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
