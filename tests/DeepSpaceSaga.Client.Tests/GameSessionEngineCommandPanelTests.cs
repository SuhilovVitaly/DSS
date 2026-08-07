using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class GameSessionEngineCommandPanelTests
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;
    private const string PlayerShipId = "SPC-0001";
    private const string EngineModuleId = "MOD-PLAYER-ENGINE-01";

    [Fact]
    public async Task Engine_command_panel_renders_bottom_center_with_7_square_buttons()
    {
        await using var fixture = CreateFixture();

        Render(fixture.Screen);

        var panel = fixture.Screen.LastCommandPanelRect;
        Assert.Equal(ScreenWidth / 2f, panel.MidX, precision: 3);
        Assert.True(panel.MidY > ScreenHeight / 2f);
        Assert.Equal(7, fixture.Screen.EngineCommandButtonRects.Count);
        foreach (var rect in fixture.Screen.EngineCommandButtonRects)
        {
            Assert.Equal(32f, rect.Width);
            Assert.Equal(32f, rect.Height);
        }
    }

    [Theory]
    [InlineData(0, ShipEngineCommandTypes.Accelerate)]
    [InlineData(1, ShipEngineCommandTypes.Brake)]
    [InlineData(2, ShipEngineCommandTypes.TurnRightStep)]
    [InlineData(3, ShipEngineCommandTypes.TurnLeftStep)]
    [InlineData(4, ShipEngineCommandTypes.TurnRightUntilCancel)]
    [InlineData(5, ShipEngineCommandTypes.TurnLeftUntilCancel)]
    [InlineData(6, ShipEngineCommandTypes.CancelAll)]
    public async Task Engine_command_button_click_sends_expected_player_command(int buttonIndex, string expectedCommandType)
    {
        await using var fixture = CreateFixture();
        Render(fixture.Screen);

        var button = fixture.Screen.EngineCommandButtonRects[buttonIndex];
        fixture.Screen.OnMouseDown(button.MidX, button.MidY);

        var command = Assert.Single(fixture.Connection.Commands);
        Assert.False(string.IsNullOrWhiteSpace(command.CommandId));
        Assert.Equal(1UL, command.ClientSequence);
        Assert.Equal(PlayerShipId, command.ObjectId);
        Assert.Equal(EngineModuleId, command.ModuleId);
        Assert.Equal(expectedCommandType, command.CommandType);
    }

    [Theory]
    [InlineData(Key.Up, ShipEngineCommandTypes.Accelerate)]
    [InlineData(Key.Down, ShipEngineCommandTypes.Brake)]
    [InlineData(Key.Right, ShipEngineCommandTypes.TurnRightStep)]
    [InlineData(Key.Left, ShipEngineCommandTypes.TurnLeftStep)]
    public async Task Arrow_keys_send_expected_engine_commands(Key key, string expectedCommandType)
    {
        await using var fixture = CreateFixture();

        fixture.Screen.OnKeyDown(key);

        var command = Assert.Single(fixture.Connection.Commands);
        Assert.Equal(expectedCommandType, command.CommandType);
        Assert.Equal(PlayerShipId, command.ObjectId);
        Assert.Equal(EngineModuleId, command.ModuleId);
    }

    [Fact]
    public async Task Client_sequence_is_monotonic_and_command_ids_are_unique()
    {
        await using var fixture = CreateFixture();

        await fixture.Handle.SendEngineCommandAsync(PlayerShipId, EngineModuleId, ShipEngineCommandTypes.Accelerate);
        await fixture.Handle.SendEngineCommandAsync(PlayerShipId, EngineModuleId, ShipEngineCommandTypes.Brake);

        Assert.Equal([1UL, 2UL], fixture.Connection.Commands.Select(c => c.ClientSequence));
        Assert.NotEqual(fixture.Connection.Commands[0].CommandId, fixture.Connection.Commands[1].CommandId);
    }

    [Fact]
    public async Task Click_on_engine_command_panel_gap_does_not_pan_camera()
    {
        await using var fixture = CreateFixture();
        Render(fixture.Screen);

        double fxBefore = fixture.Screen.CameraFocusX;
        double fyBefore = fixture.Screen.CameraFocusY;
        var panel = fixture.Screen.LastCommandPanelRect;

        fixture.Screen.OnMouseDown(panel.Left + 2f, panel.MidY);

        Assert.Equal(fxBefore, fixture.Screen.CameraFocusX);
        Assert.Equal(fyBefore, fixture.Screen.CameraFocusY);
        Assert.Empty(fixture.Connection.Commands);
    }

    [Fact]
    public async Task Engine_command_button_pressed_state_is_released_on_mouse_up()
    {
        await using var fixture = CreateFixture();
        Render(fixture.Screen);

        var button = fixture.Screen.EngineCommandButtonRects[0];
        fixture.Screen.OnMouseDown(button.MidX, button.MidY);
        Assert.Equal(0, fixture.Screen.PressedEngineCommandButtonIndex);

        fixture.Screen.OnMouseUp(button.MidX, button.MidY);
        Assert.Equal(-1, fixture.Screen.PressedEngineCommandButtonIndex);
    }

    [Fact]
    public async Task Active_periodic_command_blocks_only_its_own_button()
    {
        await using var fixture = CreateFixture();
        var activeShip = new ObjectMotionSnapshot(
            PlayerShipId,
            10000,
            10000,
            SpeedKmS: 1.0,
            Direction: 0,
            ActiveEngineCommandType: ShipEngineCommandTypes.TurnRightUntilCancel,
            TurnStepDegrees: 1,
            TurnStepRemainingMs: 1000,
            TurnStepIntervalMs: 1000);
        fixture.Handle.Buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 2,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(activeShip),
            PlayerShipObjectId: PlayerShipId));
        Render(fixture.Screen);

        // Own button (TurnRightUntilCancel) — blocked (idempotent).
        var sameTurn = fixture.Screen.EngineCommandButtonRects[4];
        fixture.Screen.OnMouseDown(sameTurn.MidX, sameTurn.MidY);
        Assert.Empty(fixture.Connection.Commands);

        Assert.Equal(4, fixture.Screen.ActiveEngineCommandButtonIndex);

        // Unrelated cyclic (Accelerate) — allowed (cancel-and-replace).
        var accelerate = fixture.Screen.EngineCommandButtonRects[0];
        fixture.Screen.OnMouseDown(accelerate.MidX, accelerate.MidY);
        Assert.Equal(ShipEngineCommandTypes.Accelerate, Assert.Single(fixture.Connection.Commands).CommandType);
        fixture.Connection.Commands.Clear();

        // Opposite until-cancel turn — allowed (mutual replacement, now a special case).
        var oppositeTurn = fixture.Screen.EngineCommandButtonRects[5];
        fixture.Screen.OnMouseDown(oppositeTurn.MidX, oppositeTurn.MidY);
        Assert.Equal(ShipEngineCommandTypes.TurnLeftUntilCancel, Assert.Single(fixture.Connection.Commands).CommandType);
    }

    [Fact]
    public async Task Brake_button_disabled_at_zero_speed()
    {
        await using var fixture = CreateFixture(speedKmS: 0);
        Render(fixture.Screen);

        var brake = fixture.Screen.EngineCommandButtonRects[1];
        fixture.Screen.OnMouseDown(brake.MidX, brake.MidY);
        Assert.Empty(fixture.Connection.Commands);
    }

    [Fact]
    public async Task Accelerate_button_disabled_at_max_speed()
    {
        await using var fixture = CreateFixture();
        var maxShip = new ObjectMotionSnapshot(
            PlayerShipId,
            10000,
            10000,
            SpeedKmS: 4.0,
            Direction: 0,
            MaxSpeedKmS: 4.0);
        fixture.Handle.Buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 2,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(maxShip),
            PlayerShipObjectId: PlayerShipId));
        Render(fixture.Screen);

        var accelerate = fixture.Screen.EngineCommandButtonRects[0];
        fixture.Screen.OnMouseDown(accelerate.MidX, accelerate.MidY);
        Assert.Empty(fixture.Connection.Commands);
    }

    [Fact]
    public async Task Turn_step_buttons_stay_enabled_while_periodic_command_active()
    {
        await using var fixture = CreateFixture();
        var activeShip = new ObjectMotionSnapshot(
            PlayerShipId,
            10000,
            10000,
            SpeedKmS: 1.0,
            Direction: 0,
            ActiveEngineCommandType: ShipEngineCommandTypes.Accelerate);
        fixture.Handle.Buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 2,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(activeShip),
            PlayerShipObjectId: PlayerShipId));
        Render(fixture.Screen);

        // TurnRightStep enabled while Accelerate is active.
        var turnRight = fixture.Screen.EngineCommandButtonRects[2];
        fixture.Screen.OnMouseDown(turnRight.MidX, turnRight.MidY);
        Assert.Equal(ShipEngineCommandTypes.TurnRightStep, Assert.Single(fixture.Connection.Commands).CommandType);
        fixture.Connection.Commands.Clear();

        // TurnLeftStep enabled while Accelerate is active.
        var turnLeft = fixture.Screen.EngineCommandButtonRects[3];
        fixture.Screen.OnMouseDown(turnLeft.MidX, turnLeft.MidY);
        Assert.Equal(ShipEngineCommandTypes.TurnLeftStep, Assert.Single(fixture.Connection.Commands).CommandType);
    }

    private static TestFixture CreateFixture(double speedKmS = 1.0)
    {
        var connection = new RecordingConnection();
        var handle = new GameSessionHandle(connection);
        var ship = new ObjectMotionSnapshot(PlayerShipId, 10000, 10000, SpeedKmS: speedKmS, Direction: 0);
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

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
