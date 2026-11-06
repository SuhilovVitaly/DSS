using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using Silk.NET.Windowing;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0012TK0001Tests
{
    public class WindowProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.Name switch
        {
            "set_TopMost" or "Dispose" => null,
            _ => throw new NotSupportedException(method.Name)
        };
    }

    public class KeyboardProxy : DispatchProxy
    {
        internal readonly HashSet<Key> Down = [];
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.Name switch
        {
            "IsKeyPressed" => Down.Contains((Key)args![0]!),
            "add_KeyChar" or "remove_KeyChar" => null,
            _ => throw new NotSupportedException(method.Name)
        };
    }

    [Theory]
    [InlineData(Key.Left)]
    [InlineData(Key.Right)]
    public async Task Native_focus_loss_clears_input_before_keyboard_resync(Key key)
    {
        await using var f = await PointCombatUiFixture.Create();
        using var window = new SkiaWindow(f.Screen, f, null, createNativeWindow: false);
        typeof(SkiaWindow).GetField("_window", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(window, DispatchProxy.Create<IWindow, WindowProxy>());
        var keyboard = DispatchProxy.Create<IKeyboard, KeyboardProxy>();
        var down = ((KeyboardProxy)(object)keyboard).Down;
        typeof(SkiaWindow).GetField("_keyboard", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, keyboard);
        void Invoke(string name, params object[] args) => typeof(SkiaWindow)
            .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, args);

        f.Render(); down.Add(key); Invoke("PollKeyboard"); f.Render();
        bool turn = key is Key.Left or Key.Right;
        if (turn)
        {
            await f.Wait(() => f.Handle.Buffer.FindCommandResult(f.Screen.LastManeuverCommandId!) is { Status: CommandResultStatus.Executed });
            Assert.NotNull(f.Engine.CaptureSnapshot().Objects.Single(o => o.ObjectId == PointCombatUiFixture.Player).TurnHoldRateDegPerSec);
        }

        Invoke("OnFocusChanged", false);
        down.Clear(); Invoke("PollKeyboard");
        if (turn)
        {
            await f.Wait(() => f.Engine.CaptureSnapshot().Objects.Single(o => o.ObjectId == PointCombatUiFixture.Player).TurnHoldRateDegPerSec is null);
        }
        int commands = f.Engine.ReceivedCommandCount;
        Invoke("OnFocusChanged", true); Invoke("PollKeyboard"); f.Render();
        Assert.Equal(commands, f.Engine.ReceivedCommandCount);

        // A fresh edge after regaining focus still works.
        down.Add(key); Invoke("PollKeyboard"); f.Render();
        if (turn) await f.Wait(() => f.Engine.CaptureSnapshot().Objects.Single(o => o.ObjectId == PointCombatUiFixture.Player).TurnHoldRateDegPerSec is not null);
    }

    private sealed class Session : IAsyncDisposable
    {
        internal readonly PointCombatSessionFixture World;
        internal readonly GameSessionHandle Handle;
        internal readonly GameSessionScreen Screen;
        private readonly KeyboardEdgeTracker _keys;
        private readonly HashSet<Key> _down = [];
        private readonly Key[] _pressed = new Key[64], _released = new Key[64];
        private readonly SKBitmap _bitmap = new(1920, 1200);
        private readonly SKCanvas _canvas;
        private Session(bool enabled, bool combat, ManeuverInputSettings? maneuver)
        {
            World = new(engineEnabled: enabled, enemyX: combat ? 400 : 2000);
            Handle = new(World.Connection); Screen = new(Handle.Buffer, new LinearMotionPredictor(), Handle, maneuverInputs: maneuver);
            _keys = new(maneuver: maneuver); _canvas = new(_bitmap);
        }
        internal static async Task<Session> Create(bool enabled = true, bool combat = true,
            ManeuverInputSettings? maneuver = null)
        { var s = new Session(enabled, combat, maneuver); await s.Wait(() => s.Screen.RenderStates.Any(o => o.IsPlayerShip)); return s; }
        internal void Render() => Screen.Render(_canvas, 1920, 1200);
        private void Poll()
        {
            var (p, r) = _keys.PollBoth(_down.Contains, _pressed, _released);
            Screen.SynchronizeModifiers(_down.Contains(Key.ControlLeft), _down.Contains(Key.ControlRight));
            for (int i = 0; i < p; i++) Screen.OnKeyDown(_pressed[i]);
            for (int i = 0; i < r; i++) Screen.OnKeyUp(_released[i]);
        }
        internal void Down(Key key) { _down.Add(key); Poll(); Render(); }
        internal void Up(Key key) { _down.Remove(key); Poll(); }
        internal async Task Wait(Func<bool> predicate, bool render = true)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            while (true) { if (render) Render(); Assert.Null(Handle.Failure); if (predicate()) return; await Task.Delay(10, timeout.Token); }
        }
        public async ValueTask DisposeAsync()
        { Screen.OnDeactivated(); _canvas.Dispose(); _bitmap.Dispose(); await Handle.DisposeAsync(); await World.DisposeAsync(); }
    }
    private static ObjectMotionSnapshot Player => new("player", 0, 0, 0, 0, MaxSpeedKmS: 6, RenderObjectType: SpaceObjectType.PlayerShip);

    [Fact]
    public async Task Arrow_hold_release_and_shift_send_turn_and_full_speed()
    {
        await using var s = await Session.Create();
        s.Down(Key.Left); string left = Assert.IsType<string>(s.Screen.LastManeuverCommandId);
        await s.Wait(() => s.Handle.Buffer.FindCommandResult(left) is { Status: CommandResultStatus.Executed });
        Assert.True(s.World.Engine.CaptureSnapshot().Objects.Single(o => o.ObjectId == PointCombatSessionFixture.Player).TurnHoldRateDegPerSec < 0);
        int count = s.World.Engine.ReceivedCommandCount; s.Down(Key.Left); Assert.Equal(count, s.World.Engine.ReceivedCommandCount);
        s.Down(Key.Right); string right = s.Screen.LastManeuverCommandId!;
        await s.Wait(() => s.Handle.Buffer.FindCommandResult(right) is { Status: CommandResultStatus.Executed });
        s.Up(Key.Left); Assert.Equal(right, s.Screen.LastManeuverCommandId); // delayed opposite release cannot cancel current turn
        Assert.True(s.World.Engine.CaptureSnapshot().Objects.Single(o => o.ObjectId == PointCombatSessionFixture.Player).TurnHoldRateDegPerSec > 0);
        s.Up(Key.Right); string stop = s.Screen.LastManeuverCommandId!;
        await s.Wait(() => s.Handle.Buffer.FindCommandResult(stop) is { Status: CommandResultStatus.Executed });
        Assert.Null(s.World.Engine.CaptureSnapshot().Objects.Single(o => o.ObjectId == PointCombatSessionFixture.Player).TurnHoldRateDegPerSec);
        count = s.World.Engine.ReceivedCommandCount; s.Down(Key.ShiftLeft); s.Down(Key.ShiftLeft);
        await s.Wait(() => s.World.Engine.CaptureSnapshot().Objects.Single(o => o.ObjectId == PointCombatSessionFixture.Player).ActiveEngineCommandType == ShipEngineCommandTypes.SpeedHold);
        Assert.Equal(count + 1, s.World.Engine.ReceivedCommandCount);
        Assert.Equal(0, s.World.Engine.CaptureSnapshot().Objects.Single(o => o.ObjectId == PointCombatSessionFixture.Player).SpeedKmS);
        await s.Handle.SetSpeedAsync(SimulationSpeed.Speed1); s.World.SetReal(4000);
        var advanced = s.World.Engine.CaptureSnapshot(advanceClock: true).Objects.Single(o => o.ObjectId == PointCombatSessionFixture.Player);
        Assert.True(advanced.SpeedKmS > 0); Assert.True(advanced.SpeedKmS <= advanced.MaxSpeedKmS);
    }

    [Fact]
    public async Task Focus_loss_releases_actual_turn_once()
    {
        await using var s = await Session.Create(); s.Down(Key.Left); string start = s.Screen.LastManeuverCommandId!;
        await s.Wait(() => s.Handle.Buffer.FindCommandResult(start) is { Status: CommandResultStatus.Executed });
        s.Screen.OnDeactivated(); string stop = s.Screen.LastManeuverCommandId!;
        await s.Wait(() => s.Handle.Buffer.FindCommandResult(stop) is { Status: CommandResultStatus.Executed }, render: false);
        Assert.Null(s.World.Engine.CaptureSnapshot().Objects.Single(o => o.ObjectId == PointCombatSessionFixture.Player).TurnHoldRateDegPerSec);
        int count = s.World.Engine.ReceivedCommandCount; s.Up(Key.Left); Assert.Equal(count, s.World.Engine.ReceivedCommandCount);
    }

    [Fact]
    public async Task Disabled_engine_does_not_submit_manual_turn_or_speed()
    {
        await using var s = await Session.Create(enabled: false);
        s.Down(Key.Left); s.Up(Key.Left); s.Down(Key.ShiftLeft);
        Assert.Equal(0, s.World.Engine.ReceivedCommandCount);
    }

    [Fact]
    public async Task Outside_combat_arrows_use_the_same_continuous_turn_command()
    {
        await using var s = await Session.Create(combat: false);
        Assert.False(s.Handle.Buffer.Presented!.Snapshot.IsTacticalBattleActive);
        s.Down(Key.Left);
        await s.Wait(() => s.World.Engine.CaptureSnapshot().Objects.Single(o => o.ObjectId == PointCombatSessionFixture.Player).TurnHoldRateDegPerSec < 0);
        Assert.Equal(ShipEngineCommandTypes.TurnHold, s.World.Engine.CaptureSnapshot().Objects.Single(o => o.ObjectId == PointCombatSessionFixture.Player).ActiveEngineCommandType);
    }
}
