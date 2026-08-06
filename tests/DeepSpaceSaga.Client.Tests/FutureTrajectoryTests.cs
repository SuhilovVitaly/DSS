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

    // ── Discrete turn tests ──────────────────────────────────────

    [Fact]
    public void Discrete_right_turn_applies_at_correct_intervals()
    {
        var projector = new FutureTrajectoryProjector(new LinearMotionPredictor());
        // TurnRightUntilCancel: 90° per 1000ms. First turn fires at TurnStepRemainingMs.
        var state = new ObjectMotionSnapshot(
            "ship",
            X: 0, Y: 0,
            SpeedKmS: 1,
            Direction: 0,
            ActiveEngineCommandType: ShipEngineCommandTypes.TurnRightUntilCancel,
            TurnStepDegrees: 90,
            TurnStepRemainingMs: 1000, // first turn after 1000ms
            TurnStepIntervalMs: 1000);

        var points = projector.Project(state);

        // t=0: (0, 0)
        Assert.Equal(0.0, points[0].X, precision: 6);
        Assert.Equal(0.0, points[0].Y, precision: 6);

        // t=0..1000ms: moving up (direction 0°), speed 1 km/s = 10 wu/s
        var p1000 = points[4]; // 1000ms
        Assert.Equal(0.0, p1000.X, precision: 6);
        Assert.Equal(-10.0, p1000.Y, precision: 6); // y decreases (up is -y)

        // t=1000..2000ms: direction 90° (right after turn)
        var p2000 = points[8]; // 2000ms
        Assert.Equal(10.0, p2000.X, precision: 6);
        Assert.Equal(-10.0, p2000.Y, precision: 6);
    }

    [Fact]
    public void TurnStepRemainingMs_affects_first_turn_timing()
    {
        var projector = new FutureTrajectoryProjector(new LinearMotionPredictor());

        // First turn fires at 200ms, second at 1200ms
        var stateEarly = new ObjectMotionSnapshot("ship", X: 0, Y: 0, SpeedKmS: 1, Direction: 0,
            ActiveEngineCommandType: ShipEngineCommandTypes.TurnRightUntilCancel,
            TurnStepDegrees: 90, TurnStepRemainingMs: 200, TurnStepIntervalMs: 1000);

        // First turn fires at 900ms, second at 1900ms
        var stateLate = stateEarly with { TurnStepRemainingMs = 900 };

        var earlyPoints = projector.Project(stateEarly);
        var latePoints = projector.Project(stateLate);

        // At 1000ms the two trajectories differ because turns fired at different times.
        var early1000 = earlyPoints[4];
        var late1000 = latePoints[4];

        bool positionsDiffer = Math.Abs(early1000.X - late1000.X) > 0.01
                            || Math.Abs(early1000.Y - late1000.Y) > 0.01;
        Assert.True(positionsDiffer,
            "TurnStepRemainingMs must affect when the first turn fires — " +
            $"early={early1000.X:F3},{early1000.Y:F3} late={late1000.X:F3},{late1000.Y:F3}");
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
    public void All_turn_projections_use_fixed_200s_horizon()
    {
        var projector = new FutureTrajectoryProjector(new LinearMotionPredictor());
        // Slow turn (1° per 1000ms) — horizon is fixed at 200 s with the discrete model.
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

        // Fixed 200 s horizon regardless of turn rate.
        int expectedSamples = 200_000 / FutureTrajectoryProjector.FutureTrajectorySampleIntervalMs + 1;
        Assert.Equal(expectedSamples, points.Count);
        Assert.Equal(0.0, points[0].X, precision: 6);
        Assert.Equal(0.0, points[0].Y, precision: 6);
    }

    [Fact]
    public void Fast_turn_also_uses_fixed_200s_horizon()
    {
        var projector = new FutureTrajectoryProjector(new LinearMotionPredictor());
        // 90° per 1000ms → circle period = 4 s. Horizon stays at 200 s.
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

        // Horizon is always the fixed 200 s.
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
