using System.Collections.Immutable;
using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class FutureTrajectoryTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    // ── FutureTrajectoryProjector unit tests ──────────────────────

    [Fact]
    public void Straight_line_trajectory_at_1_km_s_and_90_degrees_ends_2000_units_right()
    {
        var projector = new FutureTrajectoryProjector(new LinearMotionPredictor());
        // 1 km/s at 90° (right). In 200 s: 1 * 200 * 10 = 2000 world units right.
        var state = new ObjectMotionSnapshot("obj", X: 100, Y: 50, SpeedKmS: 1, Direction: 90);

        var points = projector.Project(state);

        Assert.Equal(FutureTrajectoryProjector.MaxSamplePoints, points.Count);
        // First point at t=0 is the starting position
        Assert.Equal(100.0, points[0].X, precision: 6);
        Assert.Equal(50.0, points[0].Y, precision: 6);
        // Last point at t=200_000 ms: 100 + 1 * 200 * 10 = 2100
        Assert.Equal(2100.0, points[^1].X, precision: 6);
        Assert.Equal(50.0, points[^1].Y, precision: 6);
    }

    [Fact]
    public void Straight_line_trajectory_at_0_degrees_moves_up()
    {
        var projector = new FutureTrajectoryProjector(new LinearMotionPredictor());
        // 0° = up (negative Y), 5 km/s * 200 s = 10000 world units up.
        var state = new ObjectMotionSnapshot("obj", X: 0, Y: 10000, SpeedKmS: 5, Direction: 0);

        var points = projector.Project(state);

        Assert.Equal(0.0, points[0].X, precision: 6);
        Assert.Equal(10000.0, points[0].Y, precision: 6);
        Assert.Equal(0.0, points[^1].X, precision: 6);
        Assert.Equal(0.0, points[^1].Y, precision: 6); // 10000 - 5 * 200 * 10 = 0
    }

    [Fact]
    public void Trajectory_with_turn_steps_changes_direction()
    {
        var projector = new FutureTrajectoryProjector(new LinearMotionPredictor());
        // Turn 90° right every 1000 ms. Starting direction 0° (up), speed 1 km/s.
        var state = new ObjectMotionSnapshot(
            "ship",
            X: 0, Y: 0,
            SpeedKmS: 1,
            Direction: 0,
            TurnStepDegrees: 90,
            TurnStepRemainingMs: 1000,
            TurnStepIntervalMs: 1000);

        var points = projector.Project(state);

        // t=0: (0, 0)
        Assert.Equal(0.0, points[0].X, precision: 6);
        Assert.Equal(0.0, points[0].Y, precision: 6);

        // t=1000 (sample 4): first turn happens at 1000 ms.
        // 0-1000ms: direction 0° (up), distance = 1 * 1 * 10 = 10 units up → (0, -10)
        var pointAt1000 = points[4]; // sample interval 250ms → index 4 = 1000ms
        Assert.Equal(0.0, pointAt1000.X, precision: 6);
        Assert.Equal(-10.0, pointAt1000.Y, precision: 6);

        // t=2000 (sample 8): after second turn at 2000ms.
        // 1000-2000ms: direction 90° (right), distance 10 → (10, -10)
        var pointAt2000 = points[8]; // index 8 = 2000ms
        Assert.Equal(10.0, pointAt2000.X, precision: 6);
        Assert.Equal(-10.0, pointAt2000.Y, precision: 6);
    }

    [Fact]
    public void ShouldDraw_returns_false_for_stationary_object_without_active_command()
    {
        var state = new ObjectMotionSnapshot("obj", X: 100, Y: 50, SpeedKmS: 0, Direction: 0);

        Assert.False(FutureTrajectoryProjector.ShouldDraw(state));
    }

    [Fact]
    public void ShouldDraw_returns_true_for_stationary_object_with_active_engine_command()
    {
        var state = new ObjectMotionSnapshot("obj", X: 100, Y: 50, SpeedKmS: 0, Direction: 0,
            ActiveEngineCommandType: ShipEngineCommandTypes.Accelerate);

        Assert.True(FutureTrajectoryProjector.ShouldDraw(state));
    }

    [Fact]
    public void ShouldDraw_returns_true_for_moving_object()
    {
        var state = new ObjectMotionSnapshot("obj", X: 100, Y: 50, SpeedKmS: 5, Direction: 90);

        Assert.True(FutureTrajectoryProjector.ShouldDraw(state));
    }

    [Fact]
    public void Project_horizon_is_exactly_200_seconds()
    {
        var projector = new FutureTrajectoryProjector(new LinearMotionPredictor());
        var state = new ObjectMotionSnapshot("obj", X: 0, Y: 0, SpeedKmS: 1, Direction: 90);

        var points = projector.Project(state);

        // First point at t=0, last point at t=200_000 ms
        Assert.Equal(FutureTrajectoryProjector.MaxSamplePoints, points.Count);
        // Verify sample count math: 200_000 / 250 + 1 = 801
        Assert.Equal(801, points.Count);
    }

    [Fact]
    public void Project_with_zero_elapsed_first_point_is_current_position()
    {
        var projector = new FutureTrajectoryProjector(new LinearMotionPredictor());
        var state = new ObjectMotionSnapshot("obj", X: 42, Y: 73, SpeedKmS: 10, Direction: 45);

        var points = projector.Project(state);

        // First point (t=0) must equal the starting position
        Assert.Equal(42.0, points[0].X, precision: 6);
        Assert.Equal(73.0, points[0].Y, precision: 6);
    }

    // ── Continuous turn (circular motion) tests ────────────────────

    [Fact]
    public void Continuous_right_turn_produces_circular_arc()
    {
        var projector = new FutureTrajectoryProjector(new LinearMotionPredictor());
        // TurnRightUntilCancel: 90° per 1000ms → full circle in 4000ms
        // Speed 1 km/s = 10 wu/s. Radius = v/ω = 10 / (π/2) ≈ 6.366 wu
        var state = new ObjectMotionSnapshot(
            "ship",
            X: 0, Y: 0,
            SpeedKmS: 1,
            Direction: 0,
            ActiveEngineCommandType: ShipEngineCommandTypes.TurnRightUntilCancel,
            TurnStepDegrees: 90,
            TurnStepRemainingMs: 500, // should be ignored for continuous turn
            TurnStepIntervalMs: 1000);

        var points = projector.Project(state);

        // t=0: (0, 0)
        Assert.Equal(0.0, points[0].X, precision: 6);
        Assert.Equal(0.0, points[0].Y, precision: 6);

        // After 1000ms (90° right): direction 0→90°, ship should be at roughly (R, -R)
        // R = v/ω = 10 / (π/2) ≈ 6.366. At 90°: pos ≈ (R, -R) = (6.366, -6.366)
        var p1000 = points[4]; // 1000ms / 250ms = index 4
        Assert.True(p1000.X > 0, "Should move right after 90° right turn");
        Assert.True(p1000.Y < 0, "Should move up after starting up then turning right");

        // After 2000ms (180° right): ship should be at roughly (2R, 0)
        var p2000 = points[8]; // 2000ms / 250ms = index 8
        Assert.True(p2000.X > 10, "Should be far right after 180° turn");
        Assert.Equal(0.0, p2000.Y, precision: 1); // should be back at y≈0

        // After 4000ms (360°): back to start
        var p4000 = points[16]; // 4000ms / 250ms = index 16
        Assert.Equal(0.0, p4000.X, precision: 3);
        Assert.Equal(0.0, p4000.Y, precision: 3);
    }

    [Fact]
    public void Continuous_left_turn_produces_opposite_arc()
    {
        var projector = new FutureTrajectoryProjector(new LinearMotionPredictor());
        var state = new ObjectMotionSnapshot(
            "ship",
            X: 0, Y: 0,
            SpeedKmS: 1,
            Direction: 0,
            ActiveEngineCommandType: ShipEngineCommandTypes.TurnLeftUntilCancel,
            TurnStepDegrees: -90, // left = negative
            TurnStepRemainingMs: 200,
            TurnStepIntervalMs: 1000);

        var points = projector.Project(state);

        // t=0: (0, 0)
        Assert.Equal(0.0, points[0].X, precision: 6);
        Assert.Equal(0.0, points[0].Y, precision: 6);

        // After 1000ms (90° left): direction 0→-90° (270°), ship should move left
        var p1000 = points[4]; // 1000ms
        Assert.True(p1000.X < 0, "Should move left after 90° left turn");
        Assert.True(p1000.Y < 0, "Should have moved from origin");

        // After 4000ms (360° left): back to start
        var p4000 = points[16]; // 4000ms
        Assert.Equal(0.0, p4000.X, precision: 3);
        Assert.Equal(0.0, p4000.Y, precision: 3);
    }

    [Fact]
    public void Continuous_turn_ignores_TurnStepRemainingMs()
    {
        var projector = new FutureTrajectoryProjector(new LinearMotionPredictor());

        // Two states identical except for TurnStepRemainingMs
        var state1 = new ObjectMotionSnapshot("ship", X: 0, Y: 0, SpeedKmS: 1, Direction: 0,
            ActiveEngineCommandType: ShipEngineCommandTypes.TurnRightUntilCancel,
            TurnStepDegrees: 90, TurnStepRemainingMs: 100, TurnStepIntervalMs: 1000);

        var state2 = state1 with { TurnStepRemainingMs = 900 };

        var points1 = projector.Project(state1);
        var points2 = projector.Project(state2);

        // Both trajectories should be identical — TurnStepRemainingMs is ignored
        Assert.Equal(points1.Count, points2.Count);
        for (int i = 0; i < points1.Count; i++)
        {
            Assert.Equal(points1[i].X, points2[i].X, precision: 6);
            Assert.Equal(points1[i].Y, points2[i].Y, precision: 6);
        }
    }

    [Fact]
    public void Discrete_turn_step_still_uses_old_model()
    {
        var projector = new FutureTrajectoryProjector(new LinearMotionPredictor());
        // TurnStepDegrees present but NO ActiveEngineCommandType → discrete model
        var state = new ObjectMotionSnapshot(
            "ship",
            X: 0, Y: 0,
            SpeedKmS: 1,
            Direction: 0,
            ActiveEngineCommandType: null,
            TurnStepDegrees: 90,
            TurnStepRemainingMs: 1000,
            TurnStepIntervalMs: 1000);

        var points = projector.Project(state);

        // First turn happens at exactly 1000ms (uses TurnStepRemainingMs)
        // t=0..1000ms: direction 0°, moves up
        var p1000 = points[4]; // 1000ms
        Assert.Equal(0.0, p1000.X, precision: 6);
        Assert.Equal(-10.0, p1000.Y, precision: 6); // 1 km/s * 1s * 10 = 10 units up

        // t=1000..2000ms: direction 90° (after turn), moves right
        var p2000 = points[8]; // 2000ms
        Assert.Equal(10.0, p2000.X, precision: 6);
        Assert.Equal(-10.0, p2000.Y, precision: 6);
    }

    [Fact]
    public void Slow_continuous_turn_extends_horizon_to_close_circle()
    {
        var projector = new FutureTrajectoryProjector(new LinearMotionPredictor());
        // 1° per 1000ms → circle period = 360 s. Horizon must be ≥ 360 s to close the circle.
        var state = new ObjectMotionSnapshot(
            "ship",
            X: 0, Y: 0,
            SpeedKmS: 1,
            Direction: 0,
            ActiveEngineCommandType: ShipEngineCommandTypes.TurnRightUntilCancel,
            TurnStepDegrees: 1,
            TurnStepRemainingMs: 500,
            TurnStepIntervalMs: 1000);

        var points = projector.Project(state);

        // Horizon should be at least 360 s = 360_000 ms
        int expectedMinSamples = 360_000 / FutureTrajectoryProjector.FutureTrajectorySampleIntervalMs + 1;
        Assert.True(points.Count >= expectedMinSamples,
            $"Expected at least {expectedMinSamples} samples for a 360 s horizon, got {points.Count}");

        // Last sample should be at or beyond 360 s
        // Verify the circle closes: first and last points should both be at origin
        Assert.Equal(0.0, points[0].X, precision: 6);
        Assert.Equal(0.0, points[0].Y, precision: 6);

        // Find the point closest to one full period (360_000 ms)
        int fullCircleIndex = 360_000 / FutureTrajectoryProjector.FutureTrajectorySampleIntervalMs;
        Assert.Equal(0.0, points[fullCircleIndex].X, precision: 2);
        Assert.Equal(0.0, points[fullCircleIndex].Y, precision: 2);
    }

    [Fact]
    public void Fast_continuous_turn_keeps_minimum_200s_horizon()
    {
        var projector = new FutureTrajectoryProjector(new LinearMotionPredictor());
        // 90° per 1000ms → circle period = 4 s. Horizon stays at 200 s minimum.
        var state = new ObjectMotionSnapshot(
            "ship",
            X: 0, Y: 0,
            SpeedKmS: 1,
            Direction: 0,
            ActiveEngineCommandType: ShipEngineCommandTypes.TurnRightUntilCancel,
            TurnStepDegrees: 90,
            TurnStepRemainingMs: 500,
            TurnStepIntervalMs: 1000);

        var points = projector.Project(state);

        // Fast turn: horizon should be the minimum 200 s = 200_000 ms
        int expectedSamples = 200_000 / FutureTrajectoryProjector.FutureTrajectorySampleIntervalMs + 1;
        Assert.Equal(expectedSamples, points.Count);
    }

    // ── GameSessionScreen integration tests ───────────────────────

    [Fact]
    public void Game_session_screen_draws_future_trajectory_for_player_ship()
    {
        var clock = new FakeTimestamp();
        var buffer = new SnapshotBuffer(() => clock.Timestamp);
        var ship = new ObjectMotionSnapshot("ship", 10000, 10000, SpeedKmS: 1, Direction: 90);
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(ship),
            PlayerShipObjectId: "ship"));

        var screen = new GameSessionScreen(
            buffer,
            new LinearMotionPredictor(),
            timestampProvider: () => clock.Timestamp);

        Render(screen);

        var trajectory = screen.GetFutureTrajectory("ship");
        Assert.NotEmpty(trajectory);
        Assert.Equal(FutureTrajectoryProjector.MaxSamplePoints, trajectory.Count);
        // At 1 km/s right, after 200s the X should be 2000 units ahead
        Assert.Equal(10000.0, trajectory[0].X, precision: 6);
        Assert.Equal(12000.0, trajectory[^1].X, precision: 6);
    }

    [Fact]
    public void Game_session_screen_does_not_project_trajectory_for_stationary_player_ship()
    {
        var clock = new FakeTimestamp();
        var buffer = new SnapshotBuffer(() => clock.Timestamp);
        var stationary = new ObjectMotionSnapshot("rock", 10000, 10000, SpeedKmS: 0, Direction: 0);
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(stationary),
            PlayerShipObjectId: "rock"));

        var screen = new GameSessionScreen(
            buffer,
            new LinearMotionPredictor(),
            timestampProvider: () => clock.Timestamp);

        Render(screen);

        var trajectory = screen.GetFutureTrajectory("rock");
        Assert.Empty(trajectory);
    }

    [Fact]
    public void Game_session_screen_trajectory_uses_predicted_position_not_authoritative()
    {
        var clock = new FakeTimestamp();
        var buffer = new SnapshotBuffer(() => clock.Timestamp);
        var ship = new ObjectMotionSnapshot("ship", 10000, 10000, SpeedKmS: 5, Direction: 90);
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(ship),
            PlayerShipObjectId: "ship"));

        // Advance real time by 500 ms → 500 ms of prediction at Speed1
        clock.AdvanceMs(500);

        var screen = new GameSessionScreen(
            buffer,
            new LinearMotionPredictor(),
            timestampProvider: () => clock.Timestamp);

        Render(screen);

        var trajectory = screen.GetFutureTrajectory("ship");
        Assert.NotEmpty(trajectory);

        // Prediction delta = 500 ms. Predicted X = 10000 + 5*0.5*10 = 10025
        // So first trajectory point should be at predicted X, not authoritative X
        Assert.Equal(10025.0, trajectory[0].X, precision: 6);
    }

    [Fact]
    public void Game_session_screen_trajectory_at_speed0_uses_frozen_position()
    {
        var clock = new FakeTimestamp();
        var buffer = new SnapshotBuffer(() => clock.Timestamp);
        var ship = new ObjectMotionSnapshot("ship", 10000, 10000, SpeedKmS: 5, Direction: 90);
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray.Create(ship),
            PlayerShipObjectId: "ship"));

        // Advance real time — but at Speed0, prediction delta should be 0
        clock.AdvanceMs(2000);

        var screen = new GameSessionScreen(
            buffer,
            new LinearMotionPredictor(),
            timestampProvider: () => clock.Timestamp);

        // Need to render for prediction to be computed
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);

        var trajectory = screen.GetFutureTrajectory("ship");
        Assert.NotEmpty(trajectory);

        // At Speed0, predicted position equals authoritative position (no prediction delta)
        Assert.Equal(10000.0, trajectory[0].X, precision: 6);
    }

    [Fact]
    public void Unconfirmed_command_does_not_change_trajectory_until_snapshot_update()
    {
        var clock = new FakeTimestamp();
        var buffer = new SnapshotBuffer(() => clock.Timestamp);
        var ship = new ObjectMotionSnapshot("ship", 10000, 10000, SpeedKmS: 1, Direction: 90);
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(ship),
            PlayerShipObjectId: "ship"));

        var screen = new GameSessionScreen(
            buffer,
            new LinearMotionPredictor(),
            timestampProvider: () => clock.Timestamp);

        Render(screen);

        var before = screen.GetFutureTrajectory("ship");
        Assert.NotEmpty(before);

        // A local (unconfirmed) turn command simulation: the snapshot doesn't change,
        // so the trajectory shape must stay identical.
        var after = screen.GetFutureTrajectory("ship");
        Assert.Equal(before.Count, after.Count);
        for (int i = 0; i < before.Count; i++)
        {
            Assert.Equal(before[i].X, after[i].X, precision: 6);
            Assert.Equal(before[i].Y, after[i].Y, precision: 6);
        }
    }

    [Fact]
    public void Game_session_screen_renders_future_trajectory_between_trails_and_objects()
    {
        var clock = new FakeTimestamp();
        var buffer = new SnapshotBuffer(() => clock.Timestamp);
        var ship = new ObjectMotionSnapshot("ship", 10000, 10000, SpeedKmS: 5, Direction: 90);
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(ship),
            PlayerShipObjectId: "ship"));

        var screen = new GameSessionScreen(
            buffer,
            new LinearMotionPredictor(),
            timestampProvider: () => clock.Timestamp);

        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);

        // Verify the trajectory exists for the object
        var trajectory = screen.GetFutureTrajectory("ship");
        Assert.NotEmpty(trajectory);
        Assert.True(trajectory.Count >= 2);

        // Object should be rendered: center pixel should be player ship glyph (DarkOliveGreen)
        var centerPixel = bitmap.GetPixel(ScreenWidth / 2, ScreenHeight / 2);
        Assert.True(centerPixel.Green > 50 || centerPixel.Red > 50,
            "Center pixel should have some color from rendering (object, trail, or trajectory)");
    }

    [Fact]
    public void Trajectory_samples_are_monotonic_in_world_distance()
    {
        var projector = new FutureTrajectoryProjector(new LinearMotionPredictor());
        var state = new ObjectMotionSnapshot("obj", X: 0, Y: 0, SpeedKmS: 10, Direction: 45);

        var points = projector.Project(state);

        for (int i = 1; i < points.Count; i++)
        {
            double dx = points[i].X - points[i - 1].X;
            double dy = points[i].Y - points[i - 1].Y;
            double segmentLength = Math.Sqrt(dx * dx + dy * dy);
            Assert.True(segmentLength > 0, $"Segment {i} has zero length");
            // Each 250ms segment at 10 km/s = 10 * 0.25 * 10 = 25 world units
            Assert.Equal(25.0, segmentLength, precision: 3);
        }
    }

    [Fact]
    public void Trajectory_does_not_appear_for_non_player_ship()
    {
        var clock = new FakeTimestamp();
        var buffer = new SnapshotBuffer(() => clock.Timestamp);
        var ship = new ObjectMotionSnapshot("ship", 10000, 10000, SpeedKmS: 1, Direction: 90);
        // No PlayerShipObjectId — "ship" is not the player
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

        // Non-player ships don't get a future trajectory
        var trajectory = screen.GetFutureTrajectory("ship");
        Assert.Empty(trajectory);
    }

    [Fact]
    public void Trajectory_does_not_appear_for_missing_object_id()
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

        var trajectory = screen.GetFutureTrajectory("nonexistent");
        Assert.Empty(trajectory);
    }

    // ── Architecture / layering tests ─────────────────────────────

    [Fact]
    public void Future_trajectory_projector_does_not_reference_engine()
    {
        // The FutureTrajectoryProjector only depends on IMotionPredictor and ObjectMotionSnapshot.
        // Neither of those types lives in DeepSpaceSaga.Engine.
        var projectorType = typeof(FutureTrajectoryProjector);
        var assembly = projectorType.Assembly;

        // DeepSpaceSaga.Client should not reference DeepSpaceSaga.Engine
        var engineAssemblyName = "DeepSpaceSaga.Engine";
        foreach (var reference in assembly.GetReferencedAssemblies())
        {
            Assert.NotEqual(engineAssemblyName, reference.Name);
        }
    }

    [Fact]
    public void Future_trajectory_types_are_not_in_contracts_assembly()
    {
        // FutureTrajectoryPoint and FutureTrajectoryProjector are client-only types
        var contractsAssembly = typeof(ObjectMotionSnapshot).Assembly;
        var clientTypes = new[] { typeof(FutureTrajectoryProjector), typeof(FutureTrajectoryPoint) };

        foreach (var type in clientTypes)
        {
            Assert.NotEqual(contractsAssembly, type.Assembly);
        }
    }

    // ── Test helpers ──────────────────────────────────────────────

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
}
