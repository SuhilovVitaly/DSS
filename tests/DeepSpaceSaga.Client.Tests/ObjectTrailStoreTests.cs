using System.Collections.Immutable;
using System.Diagnostics;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class ObjectTrailStoreTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    [Fact]
    public void Trail_does_not_appear_for_stationary_object()
    {
        var clock = new FakeTimestamp();
        var store = new ObjectTrailStore(() => clock.Timestamp);
        var obj = new ObjectMotionSnapshot("stationary", 0, 0, SpeedKmS: 0, Direction: 90);

        store.Update(States(obj), SimulationSpeed.Speed1, currentGameTimeMs: 0);
        clock.AdvanceMs(100);
        store.Update(States(obj with { X = 10 }), SimulationSpeed.Speed1, currentGameTimeMs: 100);

        Assert.Empty(store.GetTrail("stationary"));
    }

    [Fact]
    public void Trail_appears_for_moving_object_after_multiple_updates()
    {
        var clock = new FakeTimestamp();
        var store = new ObjectTrailStore(() => clock.Timestamp);

        store.Update(States(MovingObject("ship", x: 0)), SimulationSpeed.Speed1, currentGameTimeMs: 0);
        var initialTrail = store.GetTrail("ship");
        Assert.Single(initialTrail);
        Assert.Equal(0, initialTrail[^1].X);
        Assert.Equal(0, initialTrail[^1].Timestamp);

        clock.AdvanceMs(ObjectTrailStore.TrailSampleIntervalMs);
        store.Update(States(MovingObject("ship", x: 5)), SimulationSpeed.Speed1, currentGameTimeMs: 50);

        var trail = store.GetTrail("ship");
        Assert.Equal(2, trail.Count);
        Assert.Equal(5, trail[^1].X);
        Assert.Equal(50, trail[^1].Timestamp);
    }

    [Fact]
    public void Trail_does_not_grow_while_paused()
    {
        var clock = new FakeTimestamp();
        var store = new ObjectTrailStore(() => clock.Timestamp);

        store.Update(States(MovingObject("ship", x: 0)), SimulationSpeed.Speed1, currentGameTimeMs: 0);
        clock.AdvanceMs(ObjectTrailStore.TrailSampleIntervalMs);
        store.Update(States(MovingObject("ship", x: 5)), SimulationSpeed.Speed1, currentGameTimeMs: 50);

        // Transition to Speed0: catch-up may add a final point at current position
        store.Update(States(MovingObject("ship", x: 10)), SimulationSpeed.Speed0, currentGameTimeMs: 50);
        int countAfterCatchUp = store.GetTrail("ship").Count;

        // Sustained pause: no further growth
        clock.AdvanceMs(1_000);
        store.Update(States(MovingObject("ship", x: 20)), SimulationSpeed.Speed0, currentGameTimeMs: 50);

        Assert.Equal(countAfterCatchUp, store.GetTrail("ship").Count);

        clock.AdvanceMs(ObjectTrailStore.TrailSampleIntervalMs);
        store.Update(States(MovingObject("ship", x: 25)), SimulationSpeed.Speed1, currentGameTimeMs: 100);

        Assert.Equal(25, store.GetTrail("ship")[^1].X);
    }

    [Fact]
    public void Bootstrap_request_while_paused_creates_initial_visual_history()
    {
        var clock = new FakeTimestamp();
        var store = new ObjectTrailStore(() => clock.Timestamp);

        store.Update(
            States(MovingObject("ship", x: 0)),
            SimulationSpeed.Speed0,
            currentGameTimeMs: 0,
            bootstrapMissingTrails: true);

        var trail = store.GetTrail("ship");
        Assert.True(trail.Count > 2);
        Assert.Equal(-10_000, trail[0].Timestamp);
        Assert.Equal(0, trail[^1].Timestamp);
    }

    [Fact]
    public void Bootstrap_request_at_zero_game_time_creates_initial_visual_history()
    {
        var clock = new FakeTimestamp();
        var store = new ObjectTrailStore(() => clock.Timestamp);

        store.Update(
            States(MovingObject("ship", x: 0)),
            SimulationSpeed.Speed1,
            currentGameTimeMs: 0,
            bootstrapMissingTrails: true);

        var trail = store.GetTrail("ship");
        Assert.True(trail.Count > 2);
        Assert.Equal(-10_000, trail[0].Timestamp);
        Assert.Equal(0, trail[^1].Timestamp);
    }

    [Fact]
    public void New_object_after_initial_bootstrap_phase_starts_with_current_point_only()
    {
        var clock = new FakeTimestamp();
        var store = new ObjectTrailStore(() => clock.Timestamp);

        store.Update(
            States(MovingObject("ship", x: 0)),
            SimulationSpeed.Speed1,
            currentGameTimeMs: 0,
            bootstrapMissingTrails: true);
        Assert.True(store.GetTrail("ship").Count > 2);

        clock.AdvanceMs(ObjectTrailStore.TrailSampleIntervalMs);
        store.Update(
            States(
                MovingObject("ship", x: 5),
                MovingObject("probe", x: 100)),
            SimulationSpeed.Speed1,
            currentGameTimeMs: 50);

        var probeTrail = store.GetTrail("probe");
        Assert.Single(probeTrail);
        Assert.Equal(100, probeTrail[0].X);
        Assert.Equal(50, probeTrail[0].Timestamp);
    }

    [Fact]
    public void Bootstrap_uses_configured_motion_predictor()
    {
        var clock = new FakeTimestamp();
        var store = new ObjectTrailStore(new FakeMotionPredictor(), () => clock.Timestamp);

        store.Update(
            States(MovingObject("ship", x: 100)),
            SimulationSpeed.Speed1,
            currentGameTimeMs: 10_000,
            bootstrapMissingTrails: true);

        var trail = store.GetTrail("ship");
        Assert.Equal(-900, trail[0].X);
        Assert.Equal(100, trail[^1].X);
    }

    [Fact]
    public void Trail_prunes_points_older_than_ten_game_seconds()
    {
        var clock = new FakeTimestamp();
        var store = new ObjectTrailStore(() => clock.Timestamp);

        store.Update(States(MovingObject("ship", x: 0)), SimulationSpeed.Speed1, currentGameTimeMs: 0);
        clock.AdvanceMs(5_000);
        store.Update(States(MovingObject("ship", x: 5)), SimulationSpeed.Speed1, currentGameTimeMs: 5_000);
        clock.AdvanceMs(5_001);
        store.Update(States(MovingObject("ship", x: 10)), SimulationSpeed.Speed1, currentGameTimeMs: 10_001);

        var trail = store.GetTrail("ship");
        Assert.Equal(2, trail.Count);
        Assert.Equal(5, trail[0].X);
        Assert.Equal(10, trail[1].X);
    }

    [Fact]
    public void Authoritative_snapshot_rewind_does_not_append_out_of_order_trail_point()
    {
        var clock = new FakeTimestamp();
        var store = new ObjectTrailStore(() => clock.Timestamp);

        store.Update(States(MovingObject("ship", x: 0)), SimulationSpeed.Speed1, currentGameTimeMs: 0);
        clock.AdvanceMs(ObjectTrailStore.TrailSampleIntervalMs);
        store.Update(States(MovingObject("ship", x: 10)), SimulationSpeed.Speed1, currentGameTimeMs: 1_000);

        clock.AdvanceMs(ObjectTrailStore.TrailSampleIntervalMs);
        store.Update(States(MovingObject("ship", x: 9)), SimulationSpeed.Speed1, currentGameTimeMs: 900);

        var trail = store.GetTrail("ship");
        Assert.True(TimestampsAreMonotonic(trail));
        Assert.Equal(1_000, trail[^1].Timestamp);
    }

    [Fact]
    public void Missing_object_trail_is_removed()
    {
        var clock = new FakeTimestamp();
        var store = new ObjectTrailStore(() => clock.Timestamp);

        store.Update(States(MovingObject("ship", x: 0)), SimulationSpeed.Speed1, currentGameTimeMs: 0);
        clock.AdvanceMs(ObjectTrailStore.TrailSampleIntervalMs);
        store.Update(Array.Empty<ObjectRenderState>(), SimulationSpeed.Speed1, currentGameTimeMs: 50);

        Assert.Empty(store.GetTrail("ship"));
    }

    [Fact]
    public void Same_game_time_interval_has_same_world_trail_length_at_different_game_speeds()
    {
        var speed1Clock = new FakeTimestamp();
        var speed4Clock = new FakeTimestamp();
        var speed1Store = new ObjectTrailStore(() => speed1Clock.Timestamp);
        var speed4Store = new ObjectTrailStore(() => speed4Clock.Timestamp);

        speed1Store.Update(States(MovingObject("ship", x: 0)), SimulationSpeed.Speed1, currentGameTimeMs: 0);
        speed4Store.Update(States(MovingObject("ship", x: 0)), SimulationSpeed.Speed4, currentGameTimeMs: 0);

        speed1Clock.AdvanceMs(10_000);
        speed4Clock.AdvanceMs(100);

        speed1Store.Update(States(MovingObject("ship", x: 100)), SimulationSpeed.Speed1, currentGameTimeMs: 10_000);
        speed4Store.Update(States(MovingObject("ship", x: 100)), SimulationSpeed.Speed4, currentGameTimeMs: 10_000);

        Assert.Equal(TrailWorldLength(speed1Store.GetTrail("ship")), TrailWorldLength(speed4Store.GetTrail("ship")));
    }

    [Fact]
    public void Game_session_screen_keeps_trail_world_length_independent_of_game_speed()
    {
        var speed1Trail = RenderTrailAfterGameTime(
            SimulationSpeed.Speed1,
            realElapsedMs: 10_000);
        var speed2Trail = RenderTrailAfterGameTime(
            SimulationSpeed.Speed2,
            realElapsedMs: 2_000);

        Assert.Equal(TrailWorldLength(speed1Trail), TrailWorldLength(speed2Trail), precision: 6);
    }

    [Fact]
    public void Same_world_trail_occupies_more_pixels_after_zoom_in()
    {
        var camera = new CameraState(focusX: 0, focusY: 0, pixelsPerWorldUnit: 1.0);
        var (startX, _) = camera.WorldToScreen(0, 0, ScreenWidth, ScreenHeight);
        var (endX, _) = camera.WorldToScreen(10, 0, ScreenWidth, ScreenHeight);
        float lengthBefore = Math.Abs(endX - startX);

        camera.SetZoom(2.0);
        (startX, _) = camera.WorldToScreen(0, 0, ScreenWidth, ScreenHeight);
        (endX, _) = camera.WorldToScreen(10, 0, ScreenWidth, ScreenHeight);
        float lengthAfter = Math.Abs(endX - startX);

        Assert.True(lengthAfter > lengthBefore);
    }

    [Fact]
    public void Game_session_screen_collects_trail_from_predicted_positions()
    {
        var clock = new FakeTimestamp();
        var buffer = new SnapshotBuffer(() => clock.Timestamp);
        var ship = new ObjectMotionSnapshot("ship", 10000, 10000, SpeedKmS: 1, Direction: 90);

        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(ship)));

        var screen = new GameSessionScreen(
            buffer,
            new LinearMotionPredictor(),
            timestampProvider: () => clock.Timestamp);

        Render(screen);
        var initialTrail = screen.GetObjectTrail("ship");
        Assert.True(initialTrail.Count > 2);

        clock.AdvanceMs(100);
        Render(screen);

        var trail = screen.GetObjectTrail("ship");
        Assert.True(trail.Count > 2);
        Assert.True(trail[^1].X > trail[^2].X);
    }

    [Fact]
    public void Game_session_screen_bootstraps_trail_on_initial_paused_render()
    {
        var clock = new FakeTimestamp();
        var buffer = new SnapshotBuffer(() => clock.Timestamp);
        var ship = new ObjectMotionSnapshot("ship", 10000, 10000, SpeedKmS: 1, Direction: 90);

        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray.Create(ship)));

        var screen = new GameSessionScreen(
            buffer,
            new LinearMotionPredictor(),
            timestampProvider: () => clock.Timestamp);

        Render(screen);

        var trail = screen.GetObjectTrail("ship");
        Assert.True(trail.Count > 2);
        Assert.Equal(0, trail[^1].Timestamp);
    }

    [Fact]
    public void Game_session_screen_does_not_bootstrap_object_added_after_initial_render()
    {
        var clock = new FakeTimestamp();
        var buffer = new SnapshotBuffer(() => clock.Timestamp);
        var ship = new ObjectMotionSnapshot("ship", 10000, 10000, SpeedKmS: 1, Direction: 90);
        var probe = new ObjectMotionSnapshot("probe", 10100, 10000, SpeedKmS: 1, Direction: 90);

        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(ship)));

        var screen = new GameSessionScreen(
            buffer,
            new LinearMotionPredictor(),
            timestampProvider: () => clock.Timestamp);

        Render(screen);
        Assert.True(screen.GetObjectTrail("ship").Count > 2);

        clock.AdvanceMs(100);
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 2,
            GameTimeMs: 100,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(ship with { X = 10001 }, probe)));

        Render(screen);

        var probeTrail = screen.GetObjectTrail("probe");
        Assert.Single(probeTrail);
        Assert.Equal(100, probeTrail[0].Timestamp);
    }

    [Fact]
    public void Game_session_screen_does_not_append_out_of_order_point_after_authoritative_rewind()
    {
        var clock = new FakeTimestamp();
        var buffer = new SnapshotBuffer(() => clock.Timestamp);
        var ship = new ObjectMotionSnapshot("ship", 10000, 10000, SpeedKmS: 1, Direction: 90);

        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(ship)));

        var screen = new GameSessionScreen(
            buffer,
            new LinearMotionPredictor(),
            timestampProvider: () => clock.Timestamp);

        Render(screen);
        clock.AdvanceMs(1_000);
        Render(screen);

        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 2,
            GameTimeMs: 900,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(ship with { X = 10009 })));

        Render(screen);

        var trail = screen.GetObjectTrail("ship");
        Assert.True(TimestampsAreMonotonic(trail));
        Assert.Equal(1_000, trail[^1].Timestamp);
    }

    private static ObjectMotionSnapshot MovingObject(string objectId, double x)
    {
        return new ObjectMotionSnapshot(objectId, x, 0, SpeedKmS: 1, Direction: 90);
    }

    private static ObjectRenderState[] States(params ObjectMotionSnapshot[] objects)
    {
        var states = new ObjectRenderState[objects.Length];
        for (int i = 0; i < objects.Length; i++)
            states[i] = new ObjectRenderState(objects[i], objects[i], IsPlayerShip: false);

        return states;
    }

    private static double TrailWorldLength(IReadOnlyList<ObjectTrailPoint> points)
    {
        Assert.True(points.Count >= 2);
        return points[^1].X - points[0].X;
    }

    private static bool TimestampsAreMonotonic(IReadOnlyList<ObjectTrailPoint> points)
    {
        for (int i = 1; i < points.Count; i++)
        {
            if (points[i].Timestamp < points[i - 1].Timestamp)
                return false;
        }

        return true;
    }

    private static IReadOnlyList<ObjectTrailPoint> RenderTrailAfterGameTime(
        SimulationSpeed speed,
        long realElapsedMs)
    {
        var clock = new FakeTimestamp();
        var buffer = new SnapshotBuffer(() => clock.Timestamp);
        var ship = new ObjectMotionSnapshot("ship", 10000, 10000, SpeedKmS: 1, Direction: 90);

        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: speed,
            Objects: ImmutableArray.Create(ship)));

        var screen = new GameSessionScreen(
            buffer,
            new LinearMotionPredictor(),
            timestampProvider: () => clock.Timestamp);

        Render(screen);
        clock.AdvanceMs(realElapsedMs);
        Render(screen);

        return screen.GetObjectTrail("ship");
    }

    private static void Render(GameSessionScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    private sealed class FakeTimestamp
    {
        public long Timestamp { get; private set; }

        public void AdvanceMs(long milliseconds)
        {
            Timestamp += milliseconds * Stopwatch.Frequency / 1000;
        }
    }

    [Fact]
    public void Trail_catches_up_to_object_position_on_Speed4_to_Speed0_transition()
    {
        var clock = new FakeTimestamp();
        var store = new ObjectTrailStore(() => clock.Timestamp);

        // Build a tail at Speed4 with a large game time jump
        store.Update(States(MovingObject("ship", x: 0)), SimulationSpeed.Speed4, currentGameTimeMs: 0);
        clock.AdvanceMs(ObjectTrailStore.TrailSampleIntervalMs);
        store.Update(States(MovingObject("ship", x: 100)), SimulationSpeed.Speed4, currentGameTimeMs: 5_000);

        int countBeforePause = store.GetTrail("ship").Count;

        // Transition to Speed0 — catch-up should add final point at object position
        clock.AdvanceMs(1_000); // real time passes, but at Speed0 game time doesn't advance
        store.Update(States(MovingObject("ship", x: 100)), SimulationSpeed.Speed0, currentGameTimeMs: 5_000);

        var trail = store.GetTrail("ship");
        Assert.True(trail.Count >= countBeforePause);
        // Last point must match object position at pause time
        Assert.Equal(100, trail[^1].X);
        Assert.Equal(5_000, trail[^1].Timestamp);
        Assert.True(TimestampsAreMonotonic(trail));
    }

    [Fact]
    public void Trail_does_not_grow_during_sustained_Speed0_after_catch_up()
    {
        var clock = new FakeTimestamp();
        var store = new ObjectTrailStore(() => clock.Timestamp);

        store.Update(States(MovingObject("ship", x: 0)), SimulationSpeed.Speed1, currentGameTimeMs: 0);
        clock.AdvanceMs(ObjectTrailStore.TrailSampleIntervalMs);
        store.Update(States(MovingObject("ship", x: 5)), SimulationSpeed.Speed1, currentGameTimeMs: 50);

        // Transition Speed1 → Speed0 (catch-up happens here)
        clock.AdvanceMs(ObjectTrailStore.TrailSampleIntervalMs);
        store.Update(States(MovingObject("ship", x: 5)), SimulationSpeed.Speed0, currentGameTimeMs: 50);

        int countAfterCatchUp = store.GetTrail("ship").Count;
        Assert.True(countAfterCatchUp > 0);

        // Several subsequent updates at Speed0 — count must not grow
        clock.AdvanceMs(1_000);
        store.Update(States(MovingObject("ship", x: 5)), SimulationSpeed.Speed0, currentGameTimeMs: 50);

        clock.AdvanceMs(1_000);
        store.Update(States(MovingObject("ship", x: 5)), SimulationSpeed.Speed0, currentGameTimeMs: 50);

        Assert.Equal(countAfterCatchUp, store.GetTrail("ship").Count);
    }

    [Fact]
    public void Trail_resumes_normally_after_Speed0_to_Speed1_transition()
    {
        var clock = new FakeTimestamp();
        var store = new ObjectTrailStore(() => clock.Timestamp);

        store.Update(States(MovingObject("ship", x: 0)), SimulationSpeed.Speed1, currentGameTimeMs: 0);
        clock.AdvanceMs(ObjectTrailStore.TrailSampleIntervalMs);
        store.Update(States(MovingObject("ship", x: 5)), SimulationSpeed.Speed1, currentGameTimeMs: 50);

        // Pause
        clock.AdvanceMs(1_000);
        store.Update(States(MovingObject("ship", x: 5)), SimulationSpeed.Speed0, currentGameTimeMs: 50);

        int countAtPause = store.GetTrail("ship").Count;

        // Resume
        clock.AdvanceMs(ObjectTrailStore.TrailSampleIntervalMs);
        store.Update(States(MovingObject("ship", x: 10)), SimulationSpeed.Speed1, currentGameTimeMs: 100);

        var trail = store.GetTrail("ship");
        Assert.True(trail.Count > countAtPause);
        Assert.Equal(10, trail[^1].X);
        Assert.Equal(100, trail[^1].Timestamp);
        Assert.True(TimestampsAreMonotonic(trail));
    }

    [Fact]
    public void Trail_rebuilds_when_pause_baseline_rewinds()
    {
        var clock = new FakeTimestamp();
        var store = new ObjectTrailStore(() => clock.Timestamp);

        // Speed4: predicted game time ahead of authoritative.
        // Use separate authoritative (behind) and predicted (ahead) snapshots.
        var auth = new ObjectMotionSnapshot("ship", 50, 0, SpeedKmS: 1, Direction: 90);
        var predicted = new ObjectMotionSnapshot("ship", 60, 0, SpeedKmS: 1, Direction: 90);

        // First frame at Speed4: seed the first point
        store.Update(
            StatesWithPredicted(new ObjectMotionSnapshot("ship", 0, 0, SpeedKmS: 1, Direction: 90),
                               new ObjectMotionSnapshot("ship", 0, 0, SpeedKmS: 1, Direction: 90)),
            SimulationSpeed.Speed4,
            currentGameTimeMs: 0);

        // Next frame: prediction jumps ahead. Distance > 1.0 → point added at predicted position.
        clock.AdvanceMs(16);
        store.Update(
            StatesWithPredicted(auth, predicted),
            SimulationSpeed.Speed4,
            currentGameTimeMs: 6_000); // predicted game time includes delta

        int countBeforePause = store.GetTrail("ship").Count;

        // Reconciliation replaces the predicted baseline with an earlier paused state.
        store.Update(
            StatesWithPredicted(auth, auth),
            SimulationSpeed.Speed0,
            currentGameTimeMs: 5_000); // authoritative game time, no prediction

        var trail = store.GetTrail("ship");
        Assert.True(trail.Count > countBeforePause,
            $"Expected trail to rebuild from {countBeforePause} points, got {trail.Count}");

        Assert.True(TimestampsAreMonotonic(trail));
        Assert.All(trail, point => Assert.True(point.Timestamp <= 5_000));

        // Last point at authoritative object position
        Assert.Equal(50, trail[^1].X);
        Assert.Equal(5_000, trail[^1].Timestamp);
    }

    [Fact]
    public void Game_session_screen_keeps_visual_pose_when_paused_snapshot_replaces_prediction()
    {
        var clock = new FakeTimestamp();
        var buffer = new SnapshotBuffer(() => clock.Timestamp);
        var ship = new ObjectMotionSnapshot("ship", 0, 0, SpeedKmS: 1, Direction: 90);

        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed4,
            Objects: ImmutableArray.Create(ship),
            PlayerShipObjectId: "ship"));

        var screen = new GameSessionScreen(
            buffer,
            new LinearMotionPredictor(),
            timestampProvider: () => clock.Timestamp);

        Render(screen);
        clock.AdvanceMs(900);
        Render(screen);

        buffer.CurrentSpeed = SimulationSpeed.Speed0;
        Render(screen);
        Assert.Equal(900, screen.GetObjectTrail("ship")[^1].X);
        Assert.Equal(900, screen.CameraFocusX);

        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 2,
            GameTimeMs: 50_000,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray.Create(ship with { X = 500 }),
            PlayerShipObjectId: "ship"));

        Render(screen);

        var trail = screen.GetObjectTrail("ship");
        Assert.True(TimestampsAreMonotonic(trail));
        Assert.All(trail, point => Assert.True(point.Timestamp <= 90_000));
        Assert.Equal(900, trail[^1].X);
        Assert.Equal(90_000, trail[^1].Timestamp);
        Assert.Equal(900, screen.CameraFocusX);

        buffer.CurrentSpeed = SimulationSpeed.Speed1;
        Render(screen);
        Assert.Equal(900, screen.CameraFocusX);

        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 3,
            GameTimeMs: 50_000,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray.Create(ship with { X = 500 }),
            PlayerShipObjectId: "ship"));

        Render(screen);
        Assert.Equal(SimulationSpeed.Speed1, buffer.CurrentSpeed);
        Assert.Equal(900, screen.CameraFocusX);

        clock.AdvanceMs(400);
        Render(screen);
        Assert.Equal(904, screen.CameraFocusX, precision: 6);
        Assert.Equal(904, screen.GetObjectTrail("ship")[^1].X, precision: 6);

        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 4,
            GameTimeMs: 90_000,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(ship with { X = 900 }),
            PlayerShipObjectId: "ship"));

        Render(screen);
        Assert.Equal(904, screen.CameraFocusX, precision: 6);
        Assert.Equal(904, screen.GetObjectTrail("ship")[^1].X, precision: 6);
    }

    private static ObjectRenderState[] StatesWithPredicted(
        ObjectMotionSnapshot authoritative,
        ObjectMotionSnapshot predicted)
    {
        return new[] { new ObjectRenderState(authoritative, predicted, IsPlayerShip: false) };
    }

    private sealed class FakeMotionPredictor : IMotionPredictor
    {
        public ObjectMotionSnapshot Predict(ObjectMotionSnapshot state, long elapsedMs)
        {
            return state with { X = state.X + elapsedMs / 10.0 };
        }
    }
}
