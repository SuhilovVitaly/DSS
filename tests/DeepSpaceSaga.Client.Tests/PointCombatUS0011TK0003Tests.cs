using System.Diagnostics;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0011TK0003Tests
{
    private static ObjectMotionSnapshot Player => new("player", 0, 0, 0, 0, RenderObjectType: SpaceObjectType.PlayerShip);
    private static ObjectMotionSnapshot Foreign(string id = "enemy-torpedo", double x = 200, double y = 0, double dx = -1, double speed = 6) =>
        new(id, x, y, speed, dx < 0 ? 270 : 90, RenderObjectType: SpaceObjectType.Missile,
            ObservedProjectile: new(ProjectileKind.Torpedo, x, y, dx, 0, speed, "slow", 9));
    private static GameSessionScreen.CounterAimPreview Project(ObjectMotionSnapshot[] rows, double x = 100, double y = 0, bool alt = false,
        double ppu = 1, LaunchProfileSnapshot? profile = null) => GameSessionScreen.ProjectCounterAim(Player, rows,
            profile ?? new("wide", 15, 1), 30, x, y, ppu, alt, 0, .25);

    [Theory]
    [InlineData(12, false, true)]
    [InlineData(12.01, false, false)]
    [InlineData(11, true, false)]
    public void Countermissile_preview_snaps_within_12px_and_alt_disables(double offset, bool alt, bool expected)
    {
        var result = Project([Foreign()], y: offset, alt: alt);
        Assert.Equal(expected ? "enemy-torpedo" : null, result.SnappedObjectId);
        Assert.Equal(expected ? 0 : offset, result.Y);
        Assert.True(double.IsFinite(result.PhysicalSeconds));
        Assert.Equal(result.PhysicalSeconds * 4, result.RealSeconds);
    }

    [Fact]
    public void Snap_uses_physical_pixels_forward_ray_and_ordinal_ties()
    {
        Assert.NotNull(Project([Foreign()], y: 24, ppu: .5).SnappedObjectId);
        Assert.Null(Project([Foreign(dx: 1)], x: 100).SnappedObjectId); // point lies 100 units behind ray origin
        var result = Project([Foreign("z"), Foreign("a")], y: 5);
        Assert.Equal("a", result.SnappedObjectId);
    }

    [Fact]
    public void Threat_deadline_danger_segment_arrival_and_spare_use_observed_motion()
    {
        var result = Project([Foreign(x: 300)], x: 100);
        Assert.True(result.Threat!.IsThreat);
        // Relative threat radius is 9+.5 km: entry at (30-9.5)/6 physical s.
        Assert.Equal((30 - 9.5) / 6, result.Threat.EntryAfterSeconds!.Value, 10);
        Assert.Equal(95, result.Threat.DangerStartX!.Value, 10); Assert.Equal(-95, result.Threat.DangerEndX!.Value, 10);
        Assert.Equal(20d / 6 * 4, result.ArrivalRealSeconds!.Value, 10);
        Assert.Equal(((30 - 9.5) / 6 - 10d / 15) * 4, result.SpareRealSeconds!.Value, 10);
        Assert.False(result.Late); Assert.True(result.Unreachable); // counter arrives long before this chosen ray point is occupied
        Assert.True(Project([Foreign(x: 20)], x: 20).Late);
        Assert.True(Project([Foreign()], x: 400, alt: true).Unreachable);
    }

    [Fact]
    public void Salvo_counts_all_observed_projectiles_at_counter_detonation_and_warns_own_torpedo()
    {
        // CM reaches 150 units in 1 s; both incoming torpedoes then occupy its 1 km circle.
        var first = Foreign(x: 210); var second = Foreign("other", x: 210, y: 5);
        var own = new ObjectMotionSnapshot("own", 90, 0, 6, 90,
            OwnProjectile: new(ProjectileKind.Torpedo, "player", "launcher", 90, 0, 90, 0, 330, 0, 0, 4000, 6, "slow", 9, 200, 20));
        var result = Project([first, second, own], x: 150);
        Assert.Equal(2, result.SalvoCount); Assert.True(result.OwnTorpedoWarning); Assert.False(result.Unreachable); Assert.False(result.Late);
        Assert.False(Project([first, own with { OwnProjectile = own.OwnProjectile! with { DetonationMotionTimeMs = 500 } }], x: 150).OwnTorpedoWarning);
    }

    [Fact]
    public void Interception_enroute_badge_is_prediction_not_confirmed_kill()
    {
        var incoming = Foreign(x: 300);
        var own = new ObjectMotionSnapshot("cm", 0, 0, 15, 90,
            OwnProjectile: new(ProjectileKind.Countermissile, "player", "defense", 0, 0, 0, 0, 240, 0, 0, 1000, 24, "wide", 1, 0, 0));
        var result = Project([incoming, own], x: 100);
        Assert.Equal(new[] { incoming.ObjectId }, result.InterceptionsEnroute);
        Assert.NotNull(incoming.ObservedProjectile); Assert.False(incoming.IsDestroyed);
        // A malicious mixed observation must not let private own-fuse data become a foreign prediction input.
        var privateFields = incoming with { OwnProjectile = own.OwnProjectile! with { DetonationX = 1e6, DetonationMotionTimeMs = 1 } };
        var unchanged = Project([privateFields, own], x: 100);
        Assert.Equal(result.X, unchanged.X); Assert.Equal(result.ArrivalRealSeconds, unchanged.ArrivalRealSeconds);
        Assert.Equal(result.SpareRealSeconds, unchanged.SpareRealSeconds); Assert.Equal(result.InterceptionsEnroute, unchanged.InterceptionsEnroute);
        Assert.False(unchanged.OwnTorpedoWarning);
    }

    [Fact]
    public void Alt_edges_precede_aim_and_focus_resync_preserves_held_state()
    {
        var tracker = new KeyboardEdgeTracker(); var down = new HashSet<Key> { Key.AltRight, Key.W };
        var pressed = new Key[64]; var released = new Key[64];
        var (count, _) = tracker.PollBoth(down.Contains, pressed, released);
        Assert.Equal(new[] { Key.AltRight, Key.W }, pressed[..count]);
        tracker.ResyncToCurrentState(down.Contains);
        (count, _) = tracker.PollBoth(down.Contains, pressed, released); Assert.Equal(0, count);
        down.Clear(); var (_, up) = tracker.PollBoth(down.Contains, pressed, released);
        Assert.Contains(Key.AltRight, released[..up]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Actual_input_submits_snapped_or_Alt_free_point_to_real_executor(bool alt)
    {
        await using var world = new PointCombatSessionFixture();
        await using var handle = new GameSessionHandle(world.Connection);
        var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle);
        using var bitmap = new SKBitmap(1920, 1200); using var canvas = new SKCanvas(bitmap);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
        async Task Wait(Func<bool> predicate)
        { while (true) { screen.Render(canvas, 1920, 1200); Assert.Null(handle.Failure); if (predicate()) return; await Task.Delay(10, timeout.Token); } }
        await Wait(() => handle.Buffer.Presented?.Snapshot.InstalledModules.Length > 0 && screen.RenderStates.Any(o => o.IsPlayerShip));
        await world.Connection.SetSimulationSpeedAsync(SimulationSpeed.Speed1); world.SetReal(40000);
        await world.Connection.SetSimulationSpeedAsync(SimulationSpeed.Speed0);
        Assert.Equal(10000, world.Engine.CaptureSnapshot().MotionTimeMs);
        Assert.Equal(DeepSpaceSaga.Engine.CommandStartDisposition.Started,
            world.Engine.TryStartHostilePointLaunch(PointCombatSessionFixture.Enemy, "MOD-PIRATE-TORPEDO-01", 0, 0, "slow", 10000).Disposition);
        await world.Connection.SetSimulationSpeedAsync(SimulationSpeed.Speed1); world.SetReal(48000);
        await world.Connection.SetSimulationSpeedAsync(SimulationSpeed.Speed0);
        await Wait(() => handle.Buffer.Presented?.Snapshot.MotionTimeMs == 12000);
        var foreign = Assert.Single(handle.Buffer.Presented!.Snapshot.Objects, o => o.ObservedProjectile is not null);
        Assert.Equal(280, foreign.X, 6);
        var tracker = new KeyboardEdgeTracker(); var down = new HashSet<Key> { Key.W };
        if (alt) down.Add(Key.AltLeft);
        var pressed = new Key[64]; var released = new Key[64];
        var (count, _) = tracker.PollBoth(down.Contains, pressed, released);
        for (int i = 0; i < count; i++) screen.OnKeyDown(pressed[i]);
        float x = (float)(960 + (200 - screen.CameraFocusX) * screen.CameraPixelsPerWorldUnit);
        float y = (float)(600 + (11 - screen.CameraFocusY) * screen.CameraPixelsPerWorldUnit);
        screen.OnMouseMove(x, y); screen.Render(canvas, 1920, 1200);
        Assert.Equal(alt ? null : foreign.ObjectId, screen.CurrentCounterAimPreview!.SnappedObjectId);
        Assert.True(screen.AvailableMapRect().Contains(screen.CounterAimLabelBounds));
        screen.OnMouseDown(x, y);
        string id = Assert.IsType<string>(screen.LastPointCommandId);
        Assert.DoesNotContain(handle.Buffer.Presented!.Snapshot.Objects, o => o.OwnProjectile?.Kind == ProjectileKind.Countermissile);
        await Wait(() => handle.Buffer.FindCommandResult(id) is { Status: CommandResultStatus.Executed } &&
            handle.Buffer.Presented!.Snapshot.Objects.Any(o => o.OwnProjectile?.Kind == ProjectileKind.Countermissile));
        var launched = Assert.Single(handle.Buffer.Presented!.Snapshot.Objects, o => o.OwnProjectile?.Kind == ProjectileKind.Countermissile).OwnProjectile!;
        Assert.Equal(200, launched.DetonationX, 6); Assert.Equal(alt ? 11 : 0, launched.DetonationY, 6);
        if (alt)
        {
            screen.OnKeyUp(Key.AltLeft); screen.Render(canvas, 1920, 1200);
            Assert.Equal(foreign.ObjectId, screen.CurrentCounterAimPreview!.SnappedObjectId);
        }
        screen.OnDeactivated(); Assert.Null(screen.CurrentCounterAimPreview);
    }

    [Fact]
    public async Task Unreachable_snapped_aim_does_not_submit_countermissile()
    {
        await using var f = await PointCombatUiFixture.Create(hostileShots: 1);
        var foreign = f.Presented.Objects.Single(o => o.ObservedProjectile is not null);
        var flight = foreign.ObservedProjectile!;
        double targetX = foreign.X + flight.DirectionX * 100, targetY = foreign.Y + flight.DirectionY * 100;
        f.Expose(targetX, targetY); f.Aim(counter: true, profile: 1);
        // The counter explodes before the incoming projectile reaches this chosen point.
        var pixel = f.Pixel(targetX, targetY);
        f.Screen.OnMouseMove(pixel.X, pixel.Y); f.Render();
        Assert.NotNull(f.Screen.CurrentCounterAimPreview!.SnappedObjectId);
        Assert.True(f.Screen.CurrentCounterAimPreview.Unreachable);
        int before = f.Engine.ReceivedCommandCount;
        f.Screen.OnMouseDown(pixel.X, pixel.Y);
        Assert.Equal(before, f.Engine.ReceivedCommandCount);
        Assert.Null(f.Screen.LastPointCommandId);
        Assert.Equal(GameSessionScreen.CombatText("Aim.Unreachable", "Unreachable"), f.Screen.PointAimFeedback);
    }
}
