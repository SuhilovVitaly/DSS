using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.DockingConfirm;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// The docking-confirmation modal itself (opened from GameSessionScreen's Commands Panel —
/// see GameSessionDockingConfirmTests.cs). Mirrors ShipScreenTests.cs' open/close-mechanics
/// coverage, plus the confirm button's deferred SendCommandAsync call.
/// </summary>
public class DockingConfirmScreenTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;
    private const string PlayerShipId = "SPC-0001";
    private const string ModuleId = "MOD-PLAYER-NAV-COMPUTER-01";
    private const string StationId = "STN-0001";

    private static DockingConfirmRequest Request() => new(PlayerShipId, ModuleId, StationId);

    private static void RenderScreen(DockingConfirmScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    [Fact]
    public async Task Escape_returns_CloseDockingConfirm_without_sending_a_command()
    {
        await using var fixture = CreateFixture();

        var result = fixture.Screen.OnKeyDown(Key.Escape);

        Assert.Equal(ScreenEvent.CloseDockingConfirm, result);
        Assert.Empty(fixture.Connection.Commands);
    }

    [Fact]
    public async Task Click_outside_panel_returns_CloseDockingConfirm_without_sending_a_command()
    {
        await using var fixture = CreateFixture();
        RenderScreen(fixture.Screen);

        // Top-left corner of the screen — well outside the centered panel.
        var result = fixture.Screen.OnMouseDown(2f, 2f);

        Assert.Equal(ScreenEvent.CloseDockingConfirm, result);
        Assert.Empty(fixture.Connection.Commands);
    }

    [Fact]
    public async Task Click_inside_panel_outside_the_confirm_button_returns_None_and_sends_nothing()
    {
        await using var fixture = CreateFixture();
        RenderScreen(fixture.Screen);

        float px = DockingConfirmLayout.PanelLeft(ScreenWidth) + DockingConfirmLayout.PanelWidth / 2f;
        float py = DockingConfirmLayout.PanelTop(ScreenHeight) + 40f; // near the title, above the button

        var result = fixture.Screen.OnMouseDown(px, py);

        Assert.Equal(ScreenEvent.None, result);
        Assert.Empty(fixture.Connection.Commands);
    }

    [Fact]
    public async Task Confirm_button_click_sends_the_deferred_dock_command_exactly_once_and_returns_CloseDockingConfirm()
    {
        await using var fixture = CreateFixture();
        RenderScreen(fixture.Screen);

        var (cx, cy) = ConfirmButtonCenter();
        var result = fixture.Screen.OnMouseDown(cx, cy);

        Assert.Equal(ScreenEvent.CloseDockingConfirm, result);
        var command = Assert.Single(fixture.Connection.Commands);
        Assert.Equal(PlayerShipId, command.ObjectId);
        Assert.Equal(ModuleId, command.ModuleId);
        Assert.Equal(NavigationComputerCommandTypes.Dock, command.CommandType);
        Assert.Equal(StationId, command.TargetObjectId);
    }

    [Fact]
    public async Task Right_click_on_confirm_button_does_not_send_a_command()
    {
        await using var fixture = CreateFixture();
        RenderScreen(fixture.Screen);

        var (cx, cy) = ConfirmButtonCenter();
        var result = fixture.Screen.OnMouseDown(cx, cy, MouseButton.Right);

        Assert.Equal(ScreenEvent.None, result);
        Assert.Empty(fixture.Connection.Commands);
    }

    [Fact]
    public void Null_handle_does_not_throw_on_confirm()
    {
        // Mirrors every other screen's _handle-less fallback (tests constructing the
        // screen directly against a buffer, no live session).
        var screen = new DockingConfirmScreen(buffer: null, handle: null, Request());
        RenderScreen(screen);

        var (cx, cy) = ConfirmButtonCenter();
        var result = screen.OnMouseDown(cx, cy);

        Assert.Equal(ScreenEvent.CloseDockingConfirm, result);
    }

    private static (float X, float Y) ConfirmButtonCenter()
    {
        var local = DockingConfirmLayout.ConfirmButtonLocalRect();
        float cx = DockingConfirmLayout.PanelLeft(ScreenWidth) + (local.Left + local.Right) / 2f;
        float cy = DockingConfirmLayout.PanelTop(ScreenHeight) + (local.Top + local.Bottom) / 2f;
        return (cx, cy);
    }

    private static TestFixture CreateFixture()
    {
        var connection = new RecordingConnection();
        var handle = new GameSessionHandle(connection);
        var screen = new DockingConfirmScreen(handle.Buffer, handle, Request());
        return new TestFixture(connection, handle, screen);
    }

    private sealed record TestFixture(
        RecordingConnection Connection,
        GameSessionHandle Handle,
        DockingConfirmScreen Screen) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Handle.DisposeAsync();
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
