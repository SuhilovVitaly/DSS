using System.Collections.Immutable;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0012TK0001Tests
{
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
        private Session(bool enabled, CombatInputSettings? bindings, bool combat)
        {
            World = new(engineEnabled: enabled, launchHostile: combat, enemyX: combat ? 400 : 2000);
            Handle = new(World.Connection); Screen = new(Handle.Buffer, new LinearMotionPredictor(), Handle, combatInputs: bindings);
            _keys = new(bindings); _canvas = new(_bitmap);
        }
        internal static async Task<Session> Create(bool enabled = true, CombatInputSettings? bindings = null, bool combat = true)
        { var s = new Session(enabled, bindings, combat); await s.Wait(() => s.Screen.RenderStates.Any(o => o.IsPlayerShip)); return s; }
        internal void Render() => Screen.Render(_canvas, 1920, 1200);
        private void Poll()
        {
            var (p, r) = _keys.PollBoth(_down.Contains, _pressed, _released);
            Screen.SynchronizeModifiers(_down.Contains(Key.ControlLeft), _down.Contains(Key.ControlRight), _down.Contains(Key.AltLeft), _down.Contains(Key.AltRight));
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
    private static ObjectMotionSnapshot Foreign(string id, double x) => new(id, x, 0, 6, 270,
        RenderObjectType: SpaceObjectType.Missile, ObservedProjectile: new(ProjectileKind.Torpedo, x, 0, -1, 0, 6, "slow", 9));

    [Fact]
    public void Default_threat_is_earliest_observed_deadline_and_exit_preview_is_bounded()
    {
        var threats = GameSessionScreen.AssessPointThreats(Player, [Foreign("later", 300), Foreign("first", 200)]);
        Assert.Equal("first", threats[0].Id);
        Assert.Equal("a", GameSessionScreen.AssessPointThreats(Player, [Foreign("b", 10), Foreign("a", 20)])[0].Id);
        var left = GameSessionScreen.PreviewEvade(Player, threats[0], EvadeSide.Left, .25);
        Assert.Equal(9.5 / 6, left.ExitPhysicalSeconds, 10); Assert.Equal(9.5 / 6 * 4, left.ExitRealSeconds, 10);
        Assert.True(left.CanEscape); Assert.Equal(0, left.DirectionX); Assert.Equal(1, left.DirectionY);
        var right = GameSessionScreen.PreviewEvade(Player, threats[0], EvadeSide.Right, .25);
        Assert.Equal(-1, right.DirectionY);
        var close = GameSessionScreen.AssessPointThreats(Player, [Foreign("close", 20)])[0];
        Assert.False(GameSessionScreen.PreviewEvade(Player, close, EvadeSide.Left, .25).CanEscape);
        Assert.False(GameSessionScreen.PreviewEvade(Player with { MaxSpeedKmS = null }, threats[0], EvadeSide.Left, .25).CanEscape);
    }

    [Theory]
    [InlineData(Key.A, EvadeSide.Left)]
    [InlineData(Key.D, EvadeSide.Right)]
    public async Task Evade_preview_on_hold_command_on_release(Key key, EvadeSide side)
    {
        await using var s = await Session.Create();
        var before = s.World.Engine.CaptureSnapshot().Objects.Single(o => o.ObjectId == PointCombatSessionFixture.Player);
        s.Down(key); Assert.Equal(0, s.World.Engine.ReceivedCommandCount);
        Assert.Equal(side, s.Screen.CurrentEvadePreview!.Side);
        Assert.NotNull(s.Screen.CurrentThreatId);
        Assert.Null(s.World.Engine.CaptureSnapshot().Objects.Single(o => o.ObjectId == PointCombatSessionFixture.Player).Evasion);
        Assert.Equal(before.X, s.Screen.RenderStates.Single(o => o.IsPlayerShip).Pose.X);
        s.Up(key); string id = Assert.IsType<string>(s.Screen.LastManeuverCommandId);
        await s.Wait(() => s.Handle.Buffer.FindCommandResult(id) is { Status: CommandResultStatus.Executed } &&
            s.Handle.Buffer.Presented!.Snapshot.Objects.Single(o => o.ObjectId == PointCombatSessionFixture.Player).Evasion is not null);
        var accepted = s.Handle.Buffer.Presented!.Snapshot.Objects.Single(o => o.ObjectId == PointCombatSessionFixture.Player).Evasion!;
        Assert.Equal(side, accepted.Side); Assert.Equal(s.Screen.CurrentThreatId, accepted.ThreatProjectileId);
        Assert.Equal(1, s.World.Engine.ReceivedCommandCount); Assert.Null(s.Screen.CurrentEvadePreview);
    }

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
        await s.Wait(() => s.World.Engine.CaptureSnapshot().Objects.Single(o => o.ObjectId == PointCombatSessionFixture.Player).ActiveEngineCommandType == ShipEngineCommandTypes.Accelerate);
        Assert.Equal(count + 1, s.World.Engine.ReceivedCommandCount);
        Assert.Equal(0, s.World.Engine.CaptureSnapshot().Objects.Single(o => o.ObjectId == PointCombatSessionFixture.Player).SpeedKmS);
        await s.Handle.SetSpeedAsync(SimulationSpeed.Speed1); s.World.SetReal(4000);
        var advanced = s.World.Engine.CaptureSnapshot(advanceClock: true).Objects.Single(o => o.ObjectId == PointCombatSessionFixture.Player);
        Assert.True(advanced.SpeedKmS > 0); Assert.True(advanced.SpeedKmS <= advanced.MaxSpeedKmS);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cancel_or_focus_loss_does_not_send_held_evasion(bool deactivate)
    {
        await using var s = await Session.Create(); s.Down(Key.A);
        if (deactivate) s.Screen.OnDeactivated(); else s.Screen.OnKeyDown(Key.Escape);
        s.Up(Key.A); Assert.Equal(0, s.World.Engine.ReceivedCommandCount); Assert.Null(s.Screen.CurrentEvadePreview);
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
    public async Task Disabled_engine_can_preview_but_does_not_submit_evade_or_turn()
    {
        await using var s = await Session.Create(enabled: false); s.Down(Key.A);
        Assert.NotNull(s.Screen.CurrentEvadePreview); Assert.False(s.Screen.CurrentEvadePreview.CanEscape);
        s.Up(Key.A); s.Down(Key.Left); s.Up(Key.Left); s.Down(Key.ShiftLeft);
        Assert.Equal(0, s.World.Engine.ReceivedCommandCount);
    }

    [Fact]
    public async Task Outside_combat_arrows_keep_step_turn_command()
    {
        await using var s = await Session.Create(combat: false); Assert.Empty(s.Screen.PointThreats);
        Assert.NotEqual(CombatEncounterState.Active, s.Handle.Buffer.Presented!.Snapshot.Encounter!.State);
        s.Down(Key.Left);
        await s.Wait(() => s.World.Engine.CaptureSnapshot().Objects.Single(o => o.ObjectId == PointCombatSessionFixture.Player).ActiveEngineCommandType == ShipEngineCommandTypes.TurnLeftStep);
        Assert.Null(s.World.Engine.CaptureSnapshot().Objects.Single(o => o.ObjectId == PointCombatSessionFixture.Player).TurnHoldRateDegPerSec);
    }

    [Fact]
    public async Task Click_and_Tab_change_current_observed_threat()
    {
        await using var world = new PointCombatSessionFixture(); var scene = world.Engine.CaptureSnapshot();
        scene = scene with { Objects = scene.Objects.Where(o => o.ObservedProjectile is null).Append(Foreign("a", 200)).Append(Foreign("b", 300)).ToImmutableArray() };
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(scene);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1920, 1200); using var canvas = new SKCanvas(bitmap); screen.Render(canvas, 1920, 1200);
        Assert.Equal("a", screen.CurrentThreatId); screen.OnKeyDown(Key.Tab); Assert.Equal("b", screen.CurrentThreatId);
        screen.OnKeyDown(Key.Tab); Assert.Equal("a", screen.CurrentThreatId);
        float x = (float)(960 + (300 - screen.CameraFocusX) * screen.CameraPixelsPerWorldUnit);
        float y = (float)(600 - screen.CameraFocusY * screen.CameraPixelsPerWorldUnit);
        screen.OnMouseMove(x, y); screen.OnMouseDown(x, y); Assert.Equal("b", screen.CurrentThreatId);
        screen.OnKeyDown(Key.A); Assert.Equal("b", screen.CurrentEvadePreview!.ThreatId);
        buffer.Update(scene with { SnapshotSequence = scene.SnapshotSequence + 1, Objects = scene.Objects.Where(o => o.ObjectId != "b").ToImmutableArray() });
        screen.Render(canvas, 1920, 1200); Assert.Null(screen.CurrentEvadePreview); screen.OnKeyUp(Key.A);
        screen.OnDeactivated();
    }

    [Fact]
    public async Task Data_remap_reaches_real_maneuver_payload_and_legacy_schema_defaults_survive()
    {
        string source = Path.Combine(AppContext.BaseDirectory, CombatInputSettings.RelativePath);
        string file = Path.GetTempFileName();
        try
        {
            var json = JsonNode.Parse(File.ReadAllText(source))!.AsObject(); json["evadeLeft"] = "Z";
            json["turnLeft"] = "J"; json["fullSpeed"] = new JsonArray("G"); File.WriteAllText(file, json.ToJsonString());
            var input = CombatInputSettings.Load(file); await using var s = await Session.Create(bindings: input);
            s.Down(Key.A); Assert.Null(s.Screen.CurrentEvadePreview); s.Up(Key.A);
            s.Down(Key.Z); Assert.Equal(EvadeSide.Left, s.Screen.CurrentEvadePreview!.Side); s.Up(Key.Z);
            string id = s.Screen.LastManeuverCommandId!;
            await s.Wait(() => s.Handle.Buffer.FindCommandResult(id) is { Status: CommandResultStatus.Executed });
            Assert.Equal(ManeuverCommandTypes.Evade, s.Handle.Buffer.FindCommandResult(id)!.CommandType);
            s.Down(Key.J); id = s.Screen.LastManeuverCommandId!;
            await s.Wait(() => s.Handle.Buffer.FindCommandResult(id) is { Status: CommandResultStatus.Executed });
            Assert.Equal(ShipEngineCommandTypes.TurnHold, s.Handle.Buffer.FindCommandResult(id)!.CommandType); s.Up(Key.J);
            foreach (string key in new[] { "evadeLeft", "evadeRight", "nextThreat", "turnLeft", "turnRight", "fullSpeed" }) json.Remove(key);
            File.WriteAllText(file, json.ToJsonString()); var legacy = CombatInputSettings.Load(file);
            Assert.Equal(Key.A, legacy.EvadeLeft); Assert.Equal(new[] { Key.ShiftLeft, Key.ShiftRight }, legacy.FullSpeed.ToArray());
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public async Task Base_course_arrow_and_engine_disabled_timer_use_own_public_state_and_freeze_on_pause()
    {
        await using var world = new PointCombatSessionFixture(engineEnabled: true);
        var scene = world.Engine.CaptureSnapshot();
        var own = scene.Objects.Single(o => o.ObjectId == PointCombatSessionFixture.Player);
        scene = scene with
        {
            Objects = scene.Objects.Select(o => o.ObjectId == own.ObjectId ? o with
            { Direction = 270, Evasion = new(0, 2000, "observed", EvadeSide.Left, own with { Direction = 123 }) } : o).ToImmutableArray(),
            InstalledModules = scene.InstalledModules.Select(m => m.ModuleTypeId.StartsWith("module.engine.", StringComparison.Ordinal)
                ? m with { DisabledUntilMotionTimeMs = 1000 } : m).ToImmutableArray()
        };
        long ui = 0;
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(scene);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => ui);
        using var bitmap = new SKBitmap(1920, 1200); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1200); Assert.Equal(123, screen.DisplayedBaseCourse); Assert.Contains("4.0", screen.EngineDisabledFeedback!);
        Assert.False(screen.EngineDisabledIconBounds.IsEmpty);
        Assert.False(screen.MapLabels[own.ObjectId].PlaqueRect.IntersectsWith(screen.EngineDisabledIconBounds));
        string initial = screen.EngineDisabledFeedback!;
        ui = System.Diagnostics.Stopwatch.Frequency * 10; screen.Render(canvas, 1920, 1200);
        Assert.Equal(initial, screen.EngineDisabledFeedback);
        if (Environment.GetEnvironmentVariable("DSS_POINT_RENDER_ARTIFACT_DIRECTORY") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory); using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var output = File.Create(Path.Combine(directory, "maneuver-input-headless.png")); data.SaveTo(output);
        }
        screen.OnDeactivated();
    }
}
