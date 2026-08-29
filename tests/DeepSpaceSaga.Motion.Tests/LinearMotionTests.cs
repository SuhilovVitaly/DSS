using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Motion.Tests;

public class LinearMotionTests
{
    [Fact]
    public void Predict_moves_right_at_90_degrees()
    {
        var predictor = new LinearMotionPredictor();
        // 90° = right, 5 km/s * 1 sec = 50 world units
        var state = new ObjectMotionSnapshot("o1", X: 100, Y: 0, SpeedKmS: 5, Direction: 90);

        var predicted = predictor.Predict(state, elapsedMs: 1000);

        Assert.Equal(150.0, predicted.X, precision: 6); // 100 + 50
        Assert.Equal(0.0, predicted.Y, precision: 6);
    }

    [Fact]
    public void Predict_moves_up_at_0_degrees()
    {
        var predictor = new LinearMotionPredictor();
        // 0° = up (negative Y), 10 km/s * 1 sec = 100 world units
        var state = new ObjectMotionSnapshot("o1", X: 0, Y: 200, SpeedKmS: 10, Direction: 0);

        var predicted = predictor.Predict(state, elapsedMs: 1000);

        Assert.Equal(0.0, predicted.X, precision: 6);
        Assert.Equal(100.0, predicted.Y, precision: 6); // 200 - 100
    }

    [Fact]
    public void Predict_moves_down_at_180_degrees()
    {
        var predictor = new LinearMotionPredictor();
        var state = new ObjectMotionSnapshot("o1", X: 0, Y: 100, SpeedKmS: 10, Direction: 180);

        var predicted = predictor.Predict(state, elapsedMs: 1000);

        Assert.Equal(0.0, predicted.X, precision: 6);
        Assert.Equal(200.0, predicted.Y, precision: 6); // 100 + 100
    }

    [Fact]
    public void Predict_with_zero_elapsed_returns_same_position()
    {
        var predictor = new LinearMotionPredictor();
        var state = new ObjectMotionSnapshot("o1", X: 42, Y: 73, SpeedKmS: 100, Direction: 45);

        var predicted = predictor.Predict(state, elapsedMs: 0);

        Assert.Equal(42.0, predicted.X, precision: 6);
        Assert.Equal(73.0, predicted.Y, precision: 6);
    }

    [Fact]
    public void Predict_half_second_produces_half_distance()
    {
        var predictor = new LinearMotionPredictor();
        // 90° = right, 10 km/s * 0.5 sec = 50 world units
        var state = new ObjectMotionSnapshot("o1", X: 0, Y: 0, SpeedKmS: 10, Direction: 90);

        var predicted = predictor.Predict(state, elapsedMs: 500);

        Assert.Equal(50.0, predicted.X, precision: 6);
        Assert.Equal(0.0, predicted.Y, precision: 6);
    }

    [Fact]
    public void Predict_with_discrete_turn_steps_matches_engine_cycle_timing()
    {
        var predictor = new LinearMotionPredictor();
        var state = new ObjectMotionSnapshot(
            "ship",
            X: 0,
            Y: 0,
            SpeedKmS: 1,
            Direction: 0,
            TurnStepDegrees: 90,
            TurnStepRemainingMs: 1000,
            TurnStepIntervalMs: 1000);

        var predicted = predictor.Predict(state, elapsedMs: 2000);

        Assert.Equal(10, predicted.X, precision: 8);
        Assert.Equal(-10, predicted.Y, precision: 8);
        Assert.Equal(180, predicted.Direction, precision: 8);
    }

    [Fact]
    public void Predict_backward_with_discrete_turn_steps_reconstructs_prior_trajectory()
    {
        var predictor = new LinearMotionPredictor();
        var state = new ObjectMotionSnapshot(
            "ship",
            X: 10,
            Y: -10,
            SpeedKmS: 1,
            Direction: 180,
            TurnStepDegrees: 90,
            TurnStepRemainingMs: 1000,
            TurnStepIntervalMs: 1000);

        var projected = predictor.Predict(state, elapsedMs: -2000);

        Assert.Equal(0, projected.X, precision: 8);
        Assert.Equal(0, projected.Y, precision: 8);
        Assert.Equal(0, projected.Direction, precision: 8);
    }

    [Fact]
    public void Predict_approach_fly_through_hands_off_to_live_target_instead_of_the_stale_captured_pose()
    {
        // Mirrors ApproachCommandTests' engine-side regression (SimulationEngine
        // .ApplyApproachStep): the fly-through leg only ever aims at the target's pose
        // CAPTURED when the command started, so a genuinely moving target has moved
        // well past it by the time a single long Predict() call (as the client makes
        // between authoritative snapshots) plays the whole thing out. Before the fix,
        // arriving at the fly-through's endpoint cleared navigation at that stale
        // captured position; it must instead keep tracking the target's live state.
        var predictor = new LinearMotionPredictor();
        var state = new ObjectMotionSnapshot(
            "ship",
            X: 0, Y: 10000,
            SpeedKmS: 3,
            Direction: 90,
            ActiveEngineCommandType: NavigationComputerCommandTypes.Approach,
            TurnStepDegrees: 4,
            TurnStepRemainingMs: 250,
            TurnStepIntervalMs: 250,
            NavigationTargetX: 10000,
            NavigationTargetY: 10000,
            NavigationAngularInertiaDegPerSec: 4,
            NavigationPhase: ApproachPursuitMath.FlyThroughPendingPhase,
            NavigationTargetSpeedKmS: 1,
            NavigationTargetDirectionDegrees: 90,
            NavigationApproachTrailDistanceWorldUnits: 1500);

        var projected = predictor.Predict(state, elapsedMs: 600_000);

        // The ship must have arrived (ship is 3 km/s vs. target's 1 km/s) and finished
        // facing the target's direction, not still mid-chase.
        Assert.Null(projected.ActiveEngineCommandType);
        Assert.Equal(90, projected.Direction, precision: 3);

        // The stale captured pose was the target's position AT t=0 (10000,10000). By
        // the time a 3 km/s ship can plausibly get there, a 1 km/s target starting from
        // that same point has moved thousands of world units further along +X — the
        // predicted ship position must reflect that, not stop at the stale point.
        Assert.True(projected.X > 12000,
            $"Ship ended at X={projected.X:F1}, too close to the stale captured position " +
            "(10000,10000) — expected it to have kept tracking the moving target.");
    }
}
