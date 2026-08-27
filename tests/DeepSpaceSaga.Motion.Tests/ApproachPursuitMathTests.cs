namespace DeepSpaceSaga.Motion.Tests;

/// <summary>
/// Tests for <see cref="ApproachPursuitMath"/> — trailing-pursuit steering used by
/// `navigation.approach` (see story-20260827-083137.md, Phase 2c, U1).
/// </summary>
public class ApproachPursuitMathTests
{
    // Engine module defaults (module.engine.basic), matching NavigationWaypointMathTests.
    private const int TurnStepDegrees = 1;
    private const int AngularInertiaDegPerSec = 4;

    [Fact]
    public void Step_is_deterministic_for_identical_inputs()
    {
        var a = ApproachPursuitMath.Step(
            shipX: 0, shipY: 0, shipDirectionDegrees: 0, shipSpeedKmS: 4,
            targetX: 1000, targetY: -3000, targetDirectionDegrees: 90, targetSpeedKmS: 3,
            trailDistanceWorldUnits: 150,
            turnStepDegrees: TurnStepDegrees, angularInertiaDegPerSec: AngularInertiaDegPerSec,
            stepTimeMs: 1000);

        var b = ApproachPursuitMath.Step(
            shipX: 0, shipY: 0, shipDirectionDegrees: 0, shipSpeedKmS: 4,
            targetX: 1000, targetY: -3000, targetDirectionDegrees: 90, targetSpeedKmS: 3,
            trailDistanceWorldUnits: 150,
            turnStepDegrees: TurnStepDegrees, angularInertiaDegPerSec: AngularInertiaDegPerSec,
            stepTimeMs: 1000);

        Assert.Equal(a, b);
    }

    [Theory]
    [InlineData(0, 100, 250)]     // target moving up (0°=up): trailing point is below (larger Y).
    [InlineData(90, 50, 200)]     // target moving right: trailing point is to the left (smaller X).
    [InlineData(180, 100, 150)]   // target moving down: trailing point is above (smaller Y).
    public void ComputeAimPoint_trails_behind_target_along_its_heading(
        double targetDirectionDegrees, double expectedX, double expectedY)
    {
        var (x, y) = ApproachPursuitMath.ComputeAimPoint(
            targetX: 100, targetY: 200, targetDirectionDegrees: targetDirectionDegrees,
            trailDistanceWorldUnits: 50);

        Assert.Equal(expectedX, x, precision: 6);
        Assert.Equal(expectedY, y, precision: 6);
    }

    [Fact]
    public void ComputeAimPoint_handles_diagonal_heading()
    {
        // 45°: forward vector (sin45, -cos45) ≈ (0.70710678, -0.70710678).
        var (x, y) = ApproachPursuitMath.ComputeAimPoint(
            targetX: 100, targetY: 200, targetDirectionDegrees: 45,
            trailDistanceWorldUnits: 50);

        Assert.Equal(64.644661, x, precision: 5);
        Assert.Equal(235.355339, y, precision: 5);
    }

    [Fact]
    public void ComputeAimPoint_uses_target_direction_even_when_target_speed_is_zero()
    {
        // ComputeAimPoint never takes speed as input — it is purely a function of
        // position + heading, so a stationary target (speed ≈ 0, e.g. a Station)
        // still yields a trailing aim point using its Direction field.
        var stationary = ApproachPursuitMath.ComputeAimPoint(
            targetX: 500, targetY: -500, targetDirectionDegrees: 90,
            trailDistanceWorldUnits: 150);

        Assert.Equal(350, stationary.X, precision: 6);
        Assert.Equal(-500, stationary.Y, precision: 6);
    }

    [Fact]
    public void ExtrapolatePosition_advances_by_constant_velocity()
    {
        // 4 km/s = 40 world units/s. Over 500 ms, heading 90° (right): +20 units on X.
        var (x, y) = ApproachPursuitMath.ExtrapolatePosition(
            x: 0, y: 0, directionDegrees: 90, speedKmS: 4, elapsedMs: 500);

        Assert.Equal(20, x, precision: 6);
        Assert.Equal(0, y, precision: 6);
    }

    [Fact]
    public void ExtrapolatePosition_is_a_pure_function_of_its_inputs()
    {
        var a = ApproachPursuitMath.ExtrapolatePosition(10, -20, 33, 2.5, 1234);
        var b = ApproachPursuitMath.ExtrapolatePosition(10, -20, 33, 2.5, 1234);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Partial_turn_matches_NavigationWaypointMath_for_equivalent_inputs()
    {
        // Same geometry as NavigationWaypointMathTests.Partial_turn_is_never_larger_than_turn_step:
        // target (100, -10000) almost straight ahead at 1 km/s → bearing ≈ 0.57° → partial turn.
        // trailDistanceWorldUnits = 0 → aim point coincides with target, so the steering
        // geometry is identical to the shared NavigationWaypointMath turn-clamp primitive.
        var result = ApproachPursuitMath.Step(
            shipX: 0, shipY: 0, shipDirectionDegrees: 0, shipSpeedKmS: 1,
            targetX: 100, targetY: -10000, targetDirectionDegrees: 0, targetSpeedKmS: 1,
            trailDistanceWorldUnits: 0,
            turnStepDegrees: TurnStepDegrees, angularInertiaDegPerSec: AngularInertiaDegPerSec,
            stepTimeMs: 0);

        Assert.Equal(0.57, result.NewDirectionDegrees, precision: 2);
        Assert.True(result.NewDirectionDegrees < TurnStepDegrees);
    }

    [Fact]
    public void Large_heading_change_is_clamped_to_turn_step()
    {
        // Same geometry as NavigationWaypointMathTests.Target_far_side_r_above_R_turns_by_step:
        // target (1000, -3000) far ahead-side at 4 km/s → bearing ≈ 18.43° → clamp to +1°.
        var result = ApproachPursuitMath.Step(
            shipX: 0, shipY: 0, shipDirectionDegrees: 0, shipSpeedKmS: 4,
            targetX: 1000, targetY: -3000, targetDirectionDegrees: 0, targetSpeedKmS: 4,
            trailDistanceWorldUnits: 0,
            turnStepDegrees: TurnStepDegrees, angularInertiaDegPerSec: AngularInertiaDegPerSec,
            stepTimeMs: 0);

        Assert.False(result.IsArrived);
        Assert.Equal(1, result.NewDirectionDegrees);
    }

    [Fact]
    public void Ship_exactly_at_aim_point_has_arrived()
    {
        var result = ApproachPursuitMath.Step(
            shipX: 500, shipY: -500, shipDirectionDegrees: 0, shipSpeedKmS: 0,
            targetX: 500, targetY: -500, targetDirectionDegrees: 0, targetSpeedKmS: 0,
            trailDistanceWorldUnits: 0,
            turnStepDegrees: TurnStepDegrees, angularInertiaDegPerSec: AngularInertiaDegPerSec,
            stepTimeMs: 0);

        Assert.True(result.IsArrived);
    }

    [Fact]
    public void Ship_within_arrival_tolerance_of_aim_point_has_arrived()
    {
        // Aim point at (0, -10000) trailing a target flying straight up (0°) starting at
        // (0, -10150) with trailDistance 150. Ship sits 3 units short of the aim point —
        // inside ArrivalToleranceUnits (5.0).
        var result = ApproachPursuitMath.Step(
            shipX: 0, shipY: -9997, shipDirectionDegrees: 0, shipSpeedKmS: 0,
            targetX: 0, targetY: -10150, targetDirectionDegrees: 0, targetSpeedKmS: 4,
            trailDistanceWorldUnits: 150,
            turnStepDegrees: TurnStepDegrees, angularInertiaDegPerSec: AngularInertiaDegPerSec,
            stepTimeMs: 0);

        Assert.True(result.IsArrived);
    }

    [Fact]
    public void Ship_far_from_aim_point_has_not_arrived()
    {
        var result = ApproachPursuitMath.Step(
            shipX: 0, shipY: 0, shipDirectionDegrees: 0, shipSpeedKmS: 1,
            targetX: 10000, targetY: -10000, targetDirectionDegrees: 45, targetSpeedKmS: 2,
            trailDistanceWorldUnits: 150,
            turnStepDegrees: TurnStepDegrees, angularInertiaDegPerSec: AngularInertiaDegPerSec,
            stepTimeMs: 1000);

        Assert.False(result.IsArrived);
    }

    [Fact]
    public void Segment_closest_approach_catches_fast_pass_through_that_endpoint_sampling_would_miss()
    {
        // Aim point sits exactly on the ship's traveled segment this step, but neither the
        // start nor the end of the segment is within ArrivalToleranceUnits of it — a naive
        // "sample only the end-of-step position" check would report no arrival even though
        // the ship physically swept through the arrival zone mid-step.
        //
        // Ship at (0,0) heading 0° (up) at 2 km/s over a 1000 ms step travels
        // 2 * 10 * 1 = 20 world units, i.e. from (0,0) to (0,-20) — more than
        // 2 * ArrivalToleranceUnits (10). The stationary target sits at (0,-10) with
        // trailDistance = 0, so the aim point is exactly the segment's midpoint:
        // distance from (0,0) to (0,-10) is 10 (> 5, tolerance) and distance from
        // (0,-20) to (0,-10) is also 10 (> 5) — both endpoint samples would miss it.
        const double aimDistanceFromEndpoints = 10;
        Assert.True(aimDistanceFromEndpoints > ApproachPursuitMath.ArrivalToleranceUnits);

        var result = ApproachPursuitMath.Step(
            shipX: 0, shipY: 0, shipDirectionDegrees: 0, shipSpeedKmS: 2,
            targetX: 0, targetY: -10, targetDirectionDegrees: 0, targetSpeedKmS: 0,
            trailDistanceWorldUnits: 0,
            turnStepDegrees: TurnStepDegrees, angularInertiaDegPerSec: AngularInertiaDegPerSec,
            stepTimeMs: 1000);

        Assert.True(result.IsArrived);
    }

    [Fact]
    public void Step_uses_target_direction_as_heading_reference_when_target_speed_is_near_zero()
    {
        // A stationary Station (speed ≈ 0) must still steer the aim point using its
        // Direction field rather than being rejected or defaulting to "no heading".
        var result = ApproachPursuitMath.Step(
            shipX: 0, shipY: 0, shipDirectionDegrees: 90, shipSpeedKmS: 1,
            targetX: 500, targetY: -500, targetDirectionDegrees: 90, targetSpeedKmS: 0,
            trailDistanceWorldUnits: 150,
            turnStepDegrees: TurnStepDegrees, angularInertiaDegPerSec: AngularInertiaDegPerSec,
            stepTimeMs: 0);

        // Expected aim point: (500 - 150, -500) = (350, -500), matching direction 90°'s
        // trailing offset — same formula ComputeAimPoint_trails_behind_target_along_its_heading
        // exercises directly, now confirmed reachable through Step with a zero-speed target.
        Assert.Equal(350, result.AimPointX, precision: 6);
        Assert.Equal(-500, result.AimPointY, precision: 6);
    }
}
