using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class ManualManeuverInputTests
{
    [Fact]
    public async Task Alias_edges_ten_second_holds_and_opposite_stacks_send_only_axis_state_changes()
    {
        await using var f = new Fixture();
        f.Down(Key.A); f.Down(Key.Left); Assert.Single(f.Commands);
        f.Emit(10000); f.Render(); f.Down(Key.A); Assert.Single(f.Commands);
        f.Up(Key.A); Assert.Single(f.Commands); f.Down(Key.D);
        Assert.Equal(TurnDirection.Right, f.Commands[^1].TurnDirection); Assert.Equal(2, f.Commands.Count);
        f.Up(Key.D); Assert.Equal(TurnDirection.Left, f.Commands[^1].TurnDirection); Assert.Equal(3, f.Commands.Count);
        f.Up(Key.Left); Assert.False(f.Commands[^1].Active); Assert.Equal(4, f.Commands.Count);
        f.Down(Key.W); f.Down(Key.Up); Assert.Equal(5, f.Commands.Count);
        f.Down(Key.S); Assert.Equal(SpeedDirection.Brake, f.Commands[^1].SpeedDirection); Assert.Equal(6, f.Commands.Count);
        f.Up(Key.S); Assert.Equal(SpeedDirection.Accelerate, f.Commands[^1].SpeedDirection); Assert.Equal(7, f.Commands.Count);
        f.Up(Key.W); Assert.Equal(7, f.Commands.Count); f.Up(Key.Up); Assert.False(f.Commands[^1].Active); Assert.Equal(8, f.Commands.Count);
        Assert.All(f.Commands, command => Assert.Contains(command.CommandType, new[] { ShipEngineCommandTypes.TurnHold, ShipEngineCommandTypes.SpeedHold }));
    }

    [Fact]
    public async Task Shift_is_latched_after_release_turn_preserves_it_and_manual_speed_replaces_it()
    {
        await using var f = new Fixture();
        f.Down(Key.ShiftLeft); f.Up(Key.ShiftLeft); Assert.Single(f.Commands); Assert.True(f.Commands[0].Latched);
        f.Down(Key.D); Assert.Equal(2, f.Commands.Count); Assert.True(f.Player.SpeedHoldLatched);
        f.Emit(2000); f.Render(); Assert.Equal(4.2, f.Player.SpeedKmS, 8); Assert.Equal(40, f.Player.Direction, 8);
        f.Down(Key.S); Assert.False(f.Commands[^1].Latched); Assert.Equal(SpeedDirection.Brake, f.Commands[^1].SpeedDirection);
        Assert.False(f.Player.SpeedHoldLatched); Assert.Equal(20, f.Player.TurnHoldRateDegPerSec);
        f.Up(Key.S); f.Down(Key.ShiftLeft); f.Up(Key.ShiftLeft);
        int count = f.Commands.Count; f.Emit(10000); f.Render();
        Assert.Equal(5, f.Player.SpeedKmS); Assert.False(f.Player.SpeedHoldLatched); Assert.Null(f.Player.SpeedHoldAccelerationKmS2);
        f.Render(); Assert.Equal(count, f.Commands.Count);
    }

    [Fact]
    public async Task Panel_aim_pause_Ctrl_actions_and_outside_combat_controls_coexist()
    {
        await using var f = new Fixture();
        f.Down(Key.F12); f.Up(Key.F12); Assert.True(f.Screen.DebugPanel.Visible);
        f.Down(Key.Q); f.Up(Key.Q); Assert.Equal(ProjectileKind.Torpedo, f.Screen.PointAim!.Kind);
        f.Down(Key.W); f.Down(Key.D); Assert.Equal(2, f.Commands.Count); Assert.NotNull(f.Screen.PointAim);
        f.Down(Key.Escape); f.Up(Key.Escape); Assert.Null(f.Screen.PointAim); Assert.True(f.Screen.DebugPanel.Visible);
        f.Down(Key.Escape); f.Up(Key.Escape); Assert.False(f.Screen.DebugPanel.Visible);
        f.Up(Key.W); f.Up(Key.D);
        int count = f.Commands.Count;
        f.Screen.SynchronizeModifiers(true, false);
        Assert.Equal(ScreenEvent.OpenShip, f.Screen.OnKeyDown(Key.S));
        Assert.Equal(ScreenEvent.None, f.Screen.OnKeyDown(Key.C)); Assert.Equal(count, f.Commands.Count);
        f.Screen.SynchronizeModifiers(false, false);
        Assert.Equal(ScreenEvent.None, f.Screen.OnKeyDown(Key.S)); f.Emit();
        Assert.Equal(SpeedDirection.Brake, f.Commands[^1].SpeedDirection); f.Screen.OnKeyUp(Key.S); f.Emit();
        f.Down(Key.E); f.Up(Key.E); Assert.Equal(ProjectileKind.Countermissile, f.Screen.PointAim!.Kind);
    }

    [Fact]
    public async Task Public_disabled_module_recovery_resends_held_axes_and_focus_resync_requires_fresh_edges()
    {
        await using var f = new Fixture();
        f.Down(Key.A); f.Down(Key.W); Assert.Equal(2, f.Commands.Count);
        f.Disabled = true; f.Emit(); f.Render(); f.Render(); Assert.Equal(2, f.Commands.Count);
        f.Disabled = false; f.Emit(); f.Render(); Assert.Equal(4, f.Commands.Count);
        f.Screen.OnDeactivated(); Assert.Equal(6, f.Commands.Count); f.Emit();
        Assert.Null(f.Player.SpeedHoldAccelerationKmS2); Assert.Null(f.Player.TurnHoldRateDegPerSec);
        f.Resync(); f.Poll(); Assert.Equal(6, f.Commands.Count);
        f.Up(Key.A); f.Up(Key.W); Assert.Equal(6, f.Commands.Count);
        f.Down(Key.A); f.Down(Key.W); Assert.Equal(8, f.Commands.Count);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string _directory = Path.Combine("D:/DeepSpaceSaga/artifacts/validation/ep0013/manual-input-fixtures", Guid.NewGuid().ToString("N"));
        private readonly SimulationEngine _engine;
        private readonly SnapshotBuffer _buffer = new();
        private readonly KeyboardEdgeTracker _keys = new();
        private readonly HashSet<Key> _down = [];
        private readonly Key[] _pressed = new Key[64], _released = new Key[64];
        private readonly SKSurface _surface = SKSurface.Create(new SKImageInfo(1920, 1200));
        private readonly GameSessionHandle _handle;
        private readonly ProbeConnection _connection;
        private long _time;
        internal readonly GameSessionScreen Screen;
        internal bool Disabled;
        internal List<PlayerCommand> Commands => _connection.Commands;
        internal ObjectMotionSnapshot Player => _buffer.Latest!.Snapshot.Objects.Single(o => o.ObjectId == _buffer.Latest.Snapshot.PlayerShipObjectId);
        internal Fixture()
        {
            Directory.CreateDirectory(_directory);
            string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/DeepSpaceSaga.Client"));
            var source = ScenarioLoader.LoadFromFile(Path.Combine(root, "Scenarios/PlayerShipOnly/scenario.json"));
            _engine = SimulationEngine.CreateFromScenarioFile(Path.Combine(root, "Settings.json"), Path.Combine(root, "Scenarios/PlayerShipOnly/scenario.json"));
            _engine.LoadScenario(source with
            {
                GameState = source.GameState with
                {
                    CurrentSpeed = "Speed0",
                    SpaceObjects = source.GameState.SpaceObjects.Select(o => o with
                    {
                        SpeedMps = o.ObjectId == source.GameState.PlayerShipObjectId ? 3000 : o.SpeedMps,
                        Ai = null,
                        Modules = o.Modules!.Select(m => m with { PowerState = o.ObjectId == source.GameState.PlayerShipObjectId ? "On" : "Off" }).ToArray()
                    }).ToArray()
                }
            });
            Emit();
            _connection = new(_engine);
            _handle = new(_connection, _buffer, new CombatMetricsWriter(Path.Combine(_directory, "metrics")), new DebugTuningFileWriter(_directory));
            Screen = new(_buffer, new LinearMotionPredictor(), _handle);
            Render();
        }
        internal void Emit(long? time = null)
        {
            _time = time ?? _time;
            var snapshot = _engine.CaptureSnapshotForTests(_time * 300, SimulationSpeed.Speed0, _time);
            if (Disabled) snapshot = snapshot with
            {
                InstalledModules = snapshot.InstalledModules.Select(m => m.ModuleTypeId.StartsWith("module.engine.", StringComparison.Ordinal)
                ? m with { DisabledUntilMotionTimeMs = long.MaxValue } : m).ToImmutableArray()
            };
            _buffer.Update(snapshot);
        }
        internal void Render() => Screen.Render(_surface.Canvas, 1920, 1200);
        internal void Down(Key key) { _down.Add(key); Poll(); Emit(); Render(); }
        internal void Up(Key key) { _down.Remove(key); Poll(); Emit(); Render(); }
        internal void Resync() => _keys.ResyncToCurrentState(_down.Contains);
        internal void Poll()
        {
            var (pressed, released) = _keys.PollBoth(_down.Contains, _pressed, _released);
            Screen.SynchronizeModifiers(_down.Contains(Key.ControlLeft), _down.Contains(Key.ControlRight));
            for (int i = 0; i < pressed; i++) Screen.OnKeyDown(_pressed[i]);
            for (int i = 0; i < released; i++) Screen.OnKeyUp(_released[i]);
        }
        public async ValueTask DisposeAsync()
        {
            Screen.OnDeactivated(); await _handle.DisposeAsync(); _surface.Dispose(); _engine.Dispose(); Directory.Delete(_directory, true);
        }
    }

    private sealed class ProbeConnection(SimulationEngine engine) : IGameSessionConnection
    {
        private readonly Channel<AuthoritativeSnapshot> _snapshots = Channel.CreateUnbounded<AuthoritativeSnapshot>();
        internal readonly List<PlayerCommand> Commands = [];
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) { Commands.Add(command); engine.ReceiveCommand(command); return ValueTask.CompletedTask; }
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        { await foreach (var snapshot in _snapshots.Reader.ReadAllAsync(cancellationToken)) yield return snapshot; }
        public ValueTask DisposeAsync() { _snapshots.Writer.TryComplete(); return ValueTask.CompletedTask; }
    }
}
