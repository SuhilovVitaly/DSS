using System.Collections.Immutable;
using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0012TK0002Tests
{
    private static ObjectMotionSnapshot Player => new("player", 0, 0, 0, 0, MaxSpeedKmS: 6, RenderObjectType: SpaceObjectType.PlayerShip);
    private static ObjectMotionSnapshot Foreign(string id, double x, double y = 0) => new(id, x, y, 6, 270,
        RenderObjectType: SpaceObjectType.Missile, ObservedProjectile: new(ProjectileKind.Torpedo, x, y, -1, 0, 6, "slow", 9));

    [Fact]
    public void Threat_panel_sorts_and_filters_observed_traces()
    {
        var threats = GameSessionScreen.AssessPointThreats(Player,
            [Foreign("later", 200), Foreign("first", 100), Foreign("near-limit", 200, 120), Foreign("outside", 200, 120.001)]);
        var rows = GameSessionScreen.MakeThreatRows(threats, ["first"], "later", .25, true, false);
        Assert.Equal(new[] { "first", "later", "near-limit" }, rows.Select(r => r.Id));
        Assert.Equal("slow", rows[0].Profile); Assert.True(rows[0].PredictedIntercept); Assert.True(rows[0].Dangerous);
        Assert.Equal((10 - 9.5) / 6 * 4, rows[0].EntryRealSeconds!.Value, 10);
        Assert.True(rows[1].Selected); Assert.Null(rows[2].EntryRealSeconds); Assert.False(rows[2].Dangerous);
        Assert.All(rows, r => { Assert.True(r.CounterEnabled); Assert.False(r.EvadeEnabled); });
    }

    [Theory]
    [InlineData(1920, 1200, 1f)]
    [InlineData(1280, 720, 1.5f)]
    public async Task Panel_rows_actions_and_available_map_stay_separate_at_supported_scale(int width, int height, float scale)
    {
        await using var world = new PointCombatSessionFixture(engineEnabled: true);
        var scene = world.Engine.CaptureSnapshot();
        scene = scene with { Objects = scene.Objects.Where(o => o.ObservedProjectile is null).Append(Foreign("a", 200)).Append(Foreign("b", 300)).ToImmutableArray() };
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(scene);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0, uiScale: scale);
        using var bitmap = new SKBitmap(width, height); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, width, height); screen.Render(canvas, width, height);
        var panel = screen.ThreatsPanel; Assert.Equal(2, panel.Rows.Count); Assert.NotEmpty(panel.VisibleRows);
        Assert.True(panel.Bounds.Left >= 0 && panel.Bounds.Top >= 0 && panel.Bounds.Right * scale <= width && panel.Bounds.Bottom * scale <= height);
        Assert.All(panel.Actions, h => Assert.True(panel.Bounds.Contains(h.Rect), $"{panel.Bounds} must contain {h.Rect}"));
        var physical = new SKRect(panel.Bounds.Left * scale, panel.Bounds.Top * scale, panel.Bounds.Right * scale, panel.Bounds.Bottom * scale);
        Assert.False(screen.AvailableMapRect().IntersectsWith(physical));
        Assert.True(screen.AvailableMapRect().Width >= 100 && screen.AvailableMapRect().Height >= 100, $"free={screen.AvailableMapRect()} commands={screen.CommandsPanel.BodyRect} threats={panel.Bounds} info={screen.ObjectInfoPanel.Bounds}");
        if (Environment.GetEnvironmentVariable("DSS_POINT_RENDER_ARTIFACT_DIRECTORY") is { Length: > 0 } directory && scale == 1)
        {
            Directory.CreateDirectory(directory); using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var output = File.Create(Path.Combine(directory, "threat-panel-headless.png")); data.SaveTo(output);
        }
        screen.OnDeactivated();
    }

    [Fact]
    public async Task Scrolling_panel_does_not_zoom_map_or_select_objects_behind_it()
    {
        await using var world = new PointCombatSessionFixture(); var scene = world.Engine.CaptureSnapshot();
        scene = scene with { Objects = scene.Objects.Where(o => o.ObservedProjectile is null).Concat(Enumerable.Range(0, 10).Select(i => Foreign("threat-" + i, 100 + i * 10))).ToImmutableArray() };
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(scene);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1920, 1200); using var canvas = new SKCanvas(bitmap); screen.Render(canvas, 1920, 1200);
        var panel = screen.ThreatsPanel; Assert.Equal(10, panel.Rows.Count); Assert.True(panel.VisibleRows.Count < 10);
        string first = panel.VisibleRows[0].Id; double ppu = screen.CameraPixelsPerWorldUnit;
        screen.OnMouseWheel(panel.Bounds.MidX, panel.Bounds.MidY, -1); screen.Render(canvas, 1920, 1200);
        Assert.NotEqual(first, panel.VisibleRows[0].Id); Assert.Equal(ppu, screen.CameraPixelsPerWorldUnit);
        float x = panel.Bounds.MidX, y = panel.Bounds.Top + 10;
        var behind = new ObjectMotionSnapshot("behind", screen.CameraFocusX + (x - 960) / ppu, screen.CameraFocusY + (y - 600) / ppu,
            0, 0, RenderObjectType: SpaceObjectType.Asteroid);
        buffer.Update(scene with { SnapshotSequence = scene.SnapshotSequence + 1, Objects = scene.Objects.Add(behind) });
        screen.Render(canvas, 1920, 1200); string? selected = screen.SelectedObjectId;
        Assert.True(screen.OnMouseMove(x, y)); screen.OnMouseDown(x, y); screen.Render(canvas, 1920, 1200);
        Assert.Null(screen.ActiveObjectId); Assert.Equal(selected, screen.SelectedObjectId);
        screen.OnDeactivated();
    }

    private sealed class Session : IAsyncDisposable
    {
        internal readonly PointCombatSessionFixture World;
        internal readonly GameSessionHandle Handle;
        internal readonly GameSessionScreen Screen;
        internal long Tick;
        private readonly SKBitmap _bitmap = new(1920, 1200);
        private readonly SKCanvas _canvas;
        private Session(bool controlled, bool launch)
        {
            World = new(engineEnabled: true, launchHostile: launch, enemyX: 100);
            Handle = controlled ? new(World.Connection, new SnapshotBuffer(() => Tick)) : new(World.Connection);
            Screen = new(Handle.Buffer, new LinearMotionPredictor(), Handle, timestampProvider: () => controlled ? Tick : Stopwatch.GetTimestamp());
            _canvas = new(_bitmap);
        }
        internal static async Task<Session> Create(bool controlled = false, bool launch = true)
        { var s = new Session(controlled, launch); await s.Wait(() => s.Screen.RenderStates.Any(o => o.IsPlayerShip) && (!launch || s.Screen.ThreatsPanel.Rows.Count > 0)); return s; }
        internal void Render() => Screen.Render(_canvas, 1920, 1200);
        internal void Click(ThreatPanel.Action action, MouseButton button = MouseButton.Left)
        {
            var hit = Assert.Single(Screen.ThreatsPanel.Actions, h => h.Action == action);
            Screen.OnMouseMove(hit.Rect.MidX, hit.Rect.MidY); Screen.OnMouseDown(hit.Rect.MidX, hit.Rect.MidY, button);
        }
        internal async Task Wait(Func<bool> predicate, bool render = true)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            while (true) { if (render) Render(); Assert.Null(Handle.Failure); if (predicate()) return; await Task.Delay(10, timeout.Token); }
        }
        public async ValueTask DisposeAsync()
        { Screen.OnDeactivated(); _canvas.Dispose(); _bitmap.Dispose(); await Handle.DisposeAsync(); await World.DisposeAsync(); }
    }

    [Theory]
    [InlineData((int)ThreatPanel.Action.Countermissile)]
    [InlineData((int)ThreatPanel.Action.EvadeLeft)]
    [InlineData((int)ThreatPanel.Action.EvadeRight)]
    public async Task Actual_panel_actions_select_threat_and_reach_real_executor(int actionValue)
    {
        var action = (ThreatPanel.Action)actionValue;
        await using var s = await Session.Create(); var row = Assert.Single(s.Screen.ThreatsPanel.Rows);
        var hit = Assert.Single(s.Screen.ThreatsPanel.Actions, h => h.Action == action); Assert.True(hit.Enabled);
        s.Click(action); Assert.Equal(row.Id, s.Screen.CurrentThreatId);
        Assert.Equal(row.Id, s.Screen.SelectedObjectId);
        if (action == ThreatPanel.Action.Countermissile)
        {
            Assert.Equal(ProjectileKind.Countermissile, s.Screen.PointAim!.Kind); Assert.Equal(0, s.World.Engine.ReceivedCommandCount);
            // Right click must still cancel aim even when it lands on the new panel.
            s.Click(action, MouseButton.Right); Assert.Null(s.Screen.PointAim);
            s.Click(action); Assert.NotNull(s.Screen.PointAim);
            float x = (float)(960 + (200 - s.Screen.CameraFocusX) * s.Screen.CameraPixelsPerWorldUnit);
            float y = (float)(600 + (20 - s.Screen.CameraFocusY) * s.Screen.CameraPixelsPerWorldUnit);
            s.Screen.OnMouseMove(x, y); s.Screen.OnMouseDown(x, y); string id = Assert.IsType<string>(s.Screen.LastPointCommandId);
            await s.Wait(() => s.Handle.Buffer.FindCommandResult(id) is { Status: CommandResultStatus.Executed } &&
                s.Handle.Buffer.Presented!.Snapshot.Objects.Any(o => o.OwnProjectile?.Kind == ProjectileKind.Countermissile));
            Assert.Equal(DefenseCommandTypes.Launch, s.Handle.Buffer.FindCommandResult(id)!.CommandType);
        }
        else
        {
            string id = Assert.IsType<string>(s.Screen.LastManeuverCommandId);
            await s.Wait(() => s.Handle.Buffer.FindCommandResult(id) is { Status: CommandResultStatus.Executed } &&
                s.Handle.Buffer.Presented!.Snapshot.Objects.Single(o => o.ObjectId == PointCombatSessionFixture.Player).Evasion is not null);
            var evade = s.Handle.Buffer.Presented!.Snapshot.Objects.Single(o => o.ObjectId == PointCombatSessionFixture.Player).Evasion!;
            Assert.Equal(row.Id, evade.ThreatProjectileId); Assert.Equal(action == ThreatPanel.Action.EvadeLeft ? EvadeSide.Left : EvadeSide.Right, evade.Side);
        }
        Assert.Equal(1, s.World.Engine.ReceivedCommandCount);
    }

    [Fact]
    public async Task Threat_row_appears_within_200ms_of_admitted_launch()
    {
        await using var s = await Session.Create(controlled: true, launch: false); Assert.Empty(s.Screen.ThreatsPanel.Rows);
        Assert.Equal(DeepSpaceSaga.Engine.CommandStartDisposition.Started,
            s.World.Engine.TryStartHostilePointLaunch(PointCombatSessionFixture.Enemy, "MOD-PIRATE-TORPEDO-01", -100, 0, "slow", 0).Disposition);
        await s.Wait(() => s.Handle.Buffer.Latest?.Snapshot.Objects.Any(o => o.ObservedProjectile is not null) == true, render: false);
        Assert.DoesNotContain(s.Handle.Buffer.Presented!.Snapshot.Objects, o => o.ObservedProjectile is not null);
        Assert.Empty(s.Screen.ThreatsPanel.Rows);
        s.Tick = Stopwatch.Frequency * 150 / 1000;
        var watch = Stopwatch.StartNew(); s.Render(); watch.Stop();
        Assert.Contains(s.Handle.Buffer.Presented!.Snapshot.Objects, o => o.ObservedProjectile is not null);
        Assert.Single(s.Screen.ThreatsPanel.Rows); Assert.True(watch.Elapsed.TotalMilliseconds <= 200, $"Admitted row render {watch.Elapsed.TotalMilliseconds:0.000} ms");
    }

    [Fact]
    public async Task Panel_interception_badge_is_same_public_conditional_prediction_as_aim()
    {
        await using var world = new PointCombatSessionFixture(); var scene = world.Engine.CaptureSnapshot();
        var foreign = Foreign("incoming", 280);
        var counter = new ObjectMotionSnapshot("own-counter", 0, 0, 15, 90, RenderObjectType: SpaceObjectType.Countermeasure,
            OwnProjectile: new(ProjectileKind.Countermissile, PointCombatSessionFixture.Player, "defense", 0, 0, 0, 0, 200, 0, 0, 4000d / 3, 15, "wide", 1, 0, 0));
        scene = scene with { Objects = scene.Objects.Where(o => o.ObservedProjectile is null).Append(foreign).Append(counter).ToImmutableArray() };
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(scene); var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1920, 1200); using var canvas = new SKCanvas(bitmap); screen.Render(canvas, 1920, 1200);
        Assert.True(Assert.Single(screen.ThreatsPanel.Rows).PredictedIntercept);
        Assert.Equal(new[] { foreign.ObjectId }, GameSessionScreen.PredictPointInterceptions(scene.Objects, PointCombatSessionFixture.Player, 0));
        Assert.False(scene.Objects.Single(o => o.ObjectId == foreign.ObjectId).IsDestroyed); Assert.Empty(scene.CombatImpacts);
        screen.OnDeactivated();
    }
}

