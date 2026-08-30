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
        // targetSpeedKmS is genuinely non-zero here — a real moving target — so the
        // full directional trailing offset applies (Post-implementation bug fix #3,
        // story-20260827-083137.md).
        var (x, y) = ApproachPursuitMath.ComputeAimPoint(
            targetX: 100, targetY: 200, targetDirectionDegrees: targetDirectionDegrees,
            targetSpeedKmS: 5.0,
            trailDistanceWorldUnits: 50);

        Assert.Equal(expectedX, x, precision: 6);
        Assert.Equal(expectedY, y, precision: 6);
    }

    [Fact]
    public void ComputeAimPoint_handles_diagonal_heading()
    {
        // 45°: forward vector (sin45, -cos45) ≈ (0.70710678, -0.70710678).
        // Genuinely moving target (non-zero speed) — full offset applies.
        var (x, y) = ApproachPursuitMath.ComputeAimPoint(
            targetX: 100, targetY: 200, targetDirectionDegrees: 45,
            targetSpeedKmS: 5.0,
            trailDistanceWorldUnits: 50);

        Assert.Equal(64.644661, x, precision: 5);
        Assert.Equal(235.355339, y, precision: 5);
    }

    [Fact]
    public void ComputeAimPoint_returns_target_position_when_target_is_genuinely_stationary()
    {
        // Post-implementation bug fix #3 (story-20260827-083137.md): a genuinely
        // stationary target (speed == 0, e.g. a Station) has no meaningful "direction
        // of travel" — DirectionDegrees on such an object is an arbitrary placeholder
        // (the Default scenario's Station SPC-0002 has directionDegrees: 0 with no
        // physical meaning). Applying the full trailing offset in that meaningless
        // direction previously sent the ship's aim point far away from the actual
        // object (the reported "flies past the station, never arrives" bug). The
        // corrected behavior: for a genuinely stationary target the effective trail
        // distance is 0, so the aim point is simply the target's own position.
        var stationary = ApproachPursuitMath.ComputeAimPoint(
            targetX: 500, targetY: -500, targetDirectionDegrees: 90,
            targetSpeedKmS: 0,
            trailDistanceWorldUnits: 150);

        Assert.Equal(500, stationary.X, precision: 6);
        Assert.Equal(-500, stationary.Y, precision: 6);
    }

    [Fact]
    public void ComputeAimPoint_applies_full_trailing_offset_for_a_slow_but_genuinely_moving_target()
    {
        // Contrast with the stationary case above: a target moving however slowly —
        // e.g. a drifting asteroid — is still "genuinely moving" and must keep the
        // full directional trailing offset. The epsilon (1e-9 km/s) is tight enough
        // to only treat EXACT (or numerically indistinguishable from exact) zero as
        // stationary.
        var slow = ApproachPursuitMath.ComputeAimPoint(
            targetX: 500, targetY: -500, targetDirectionDegrees: 90,
            targetSpeedKmS: 1e-6,
            trailDistanceWorldUnits: 150);

        Assert.Equal(350, slow.X, precision: 6);
        Assert.Equal(-500, slow.Y, precision: 6);
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
    public void Step_aims_directly_at_a_genuinely_stationary_target_not_a_meaningless_direction_offset()
    {
        // Post-implementation bug fix #3 (story-20260827-083137.md): a genuinely
        // stationary target (speed == 0, e.g. a Station) has no meaningful heading to
        // trail behind — its Direction field is an arbitrary placeholder. Step must
        // therefore aim directly at the target's own position (effective trail
        // distance 0), not offset 150 world units away in a meaningless direction —
        // this is the corrected reasoning superseding the old
        // "uses target direction even when speed is zero" behavior.
        var result = ApproachPursuitMath.Step(
            shipX: 0, shipY: 0, shipDirectionDegrees: 90, shipSpeedKmS: 1,
            targetX: 500, targetY: -500, targetDirectionDegrees: 90, targetSpeedKmS: 0,
            trailDistanceWorldUnits: 150,
            turnStepDegrees: TurnStepDegrees, angularInertiaDegPerSec: AngularInertiaDegPerSec,
            stepTimeMs: 0);

        Assert.Equal(500, result.AimPointX, precision: 6);
        Assert.Equal(-500, result.AimPointY, precision: 6);
    }

    [Fact]
    public void Step_still_applies_full_trailing_offset_for_a_genuinely_moving_target()
    {
        // Contrast with the stationary case above: a genuinely moving target (non-zero
        // speed) must still get the full directional trailing offset — this fix must
        // not regress the original moving-target behavior.
        var result = ApproachPursuitMath.Step(
            shipX: 0, shipY: 0, shipDirectionDegrees: 90, shipSpeedKmS: 1,
            targetX: 500, targetY: -500, targetDirectionDegrees: 90, targetSpeedKmS: 3,
            trailDistanceWorldUnits: 150,
            turnStepDegrees: TurnStepDegrees, angularInertiaDegPerSec: AngularInertiaDegPerSec,
            stepTimeMs: 0);

        // Expected aim point: (500 - 150, -500) = (350, -500), matching direction 90°'s
        // trailing offset.
        Assert.Equal(350, result.AimPointX, precision: 6);
        Assert.Equal(-500, result.AimPointY, precision: 6);
    }

    [Fact]
    public void Step_converges_without_wide_looping_for_a_badly_misaligned_moving_ship()
    {
        // Regression for the user-reported "wide ever-widening arc that sweeps past the
        // target and never closes back toward it" bug (story-20260827-083137.md,
        // Post-implementation bug fix #2). Drives many successive Step calls exactly the
        // way the Engine/client loop drives them — each call's LockedCourseDegrees fed
        // back into the next — using the SAME realistic module constants as production
        // (module.engine.basic: turnStepDegrees=1, angularInertiaDegPerSec=4) at the
        // ~250 ms cadence the companion cadence fix gives Approach
        // (MinTurnIntervalMs(4) = 250 ms).
        const int turnStepDegreesRealistic = 1;
        const int angularInertia = 4;
        const long stepTimeMs = 250;
        const double trailDistanceWorldUnits = 1500; // 150 km — irrelevant here, see below.

        // Stationary target (speed 0) at the origin, heading 0. Per Post-implementation
        // bug fix #3 (story-20260827-083137.md), a genuinely stationary target's
        // Direction is a meaningless placeholder, so the effective trail distance is 0
        // and the aim point is simply the target's own position (0, 0) — not offset by
        // trailDistanceWorldUnits. This test still validates the anti-circling
        // convergence guarantee itself (Post-implementation bug fix #2), independent of
        // the aim-point-location fix.
        const double targetX = 0, targetY = 0, targetDirectionDegrees = 0, targetSpeedKmS = 0;

        // Ship starts off to the side with a heading badly mismatched (~135° initial
        // angular error) relative to the bearing toward the aim point, moving at a
        // realistic cruising speed — mirrors the screenshot scenario (ship issues
        // Approach against a nearby object with a heading that isn't already aimed at it).
        double shipX = 3000, shipY = 3000, shipDirection = 180, shipSpeedKmS = 3;
        double? lockedCourse = null;

        bool arrived = false;
        int stepsTaken = 0;
        const int maxSteps = 2000; // generous upper bound (500 simulated seconds)

        for (; stepsTaken < maxSteps; stepsTaken++)
        {
            var result = ApproachPursuitMath.Step(
                shipX, shipY, shipDirection, shipSpeedKmS,
                targetX, targetY, targetDirectionDegrees, targetSpeedKmS,
                trailDistanceWorldUnits, turnStepDegreesRealistic, angularInertia,
                stepTimeMs, lockedCourse);

            if (result.IsArrived)
            {
                arrived = true;
                break;
            }

            // Advance the ship exactly like a caller (Engine/client) would between calls.
            double distance = shipSpeedKmS * (stepTimeMs / 1000.0) * 10.0;
            double angleRad = result.NewDirectionDegrees * Math.PI / 180.0;
            shipX += distance * Math.Sin(angleRad);
            shipY -= distance * Math.Cos(angleRad);
            shipDirection = result.NewDirectionDegrees;
            lockedCourse = result.LockedCourseDegrees;
        }

        Assert.True(arrived,
            $"Ship failed to arrive within {maxSteps} steps ({maxSteps * stepTimeMs / 1000.0}s simulated) " +
            $"— last direction {shipDirection:F2}, position ({shipX:F1},{shipY:F1}), locked course {lockedCourse}");
        Assert.True(stepsTaken < maxSteps / 2,
            $"Convergence took {stepsTaken} steps — expected comfortably under half the generous budget, " +
            "not a slow non-converging loop");
    }

    [Fact]
    public void Locked_course_holds_heading_instead_of_rederiving_a_near_identical_bearing()
    {
        // Once aligned (delta within turnStepDegrees/2 of the locked course), Step must
        // continue steering toward the HELD course rather than a freshly recomputed
        // bearing — even when the fresh bearing differs by a hair due to the ship having
        // moved. Ship already flying exactly along the locked course (0°): a naive
        // re-derivation would compute a near-0 bearing too here (aim point almost
        // straight ahead), so this mainly documents that the locked branch is taken and
        // returns the SAME lock back unchanged (LockedCourseDegrees echoed, not cleared).
        var result = ApproachPursuitMath.Step(
            shipX: 0, shipY: -100, shipDirectionDegrees: 0, shipSpeedKmS: 1,
            targetX: 0, targetY: -1650, targetDirectionDegrees: 0, targetSpeedKmS: 0,
            trailDistanceWorldUnits: 0, // aim point coincides with target: (0, -1650)
            turnStepDegrees: TurnStepDegrees, angularInertiaDegPerSec: AngularInertiaDegPerSec,
            stepTimeMs: 250,
            lockedCourseDegrees: 0);

        Assert.False(result.IsArrived);
        Assert.Equal(0, result.NewDirectionDegrees, precision: 6);
        Assert.NotNull(result.LockedCourseDegrees);
        Assert.Equal(0, result.LockedCourseDegrees!.Value, precision: 6);
    }

    [Fact]
    public void Locked_course_detects_aim_point_fallen_behind_the_ship_even_outside_arrival_tolerance()
    {
        // Regression for the exact mechanism NavigationWaypointMath's HoldLockedCourse
        // already uses for Orbit (dot ≤ 0 "target behind ship" check), now added to
        // Approach (story-20260827-083137.md, Post-implementation bug fix #2). The ship
        // already locked heading 0° (flying "up", -Y) and has flown past the aim point,
        // which now sits laterally offset (20 world units — well outside
        // ArrivalToleranceUnits=5) AND behind the ship along its heading. A
        // position-only distance/segment check would never trigger for this offset, so
        // WITHOUT the dot-product safeguard the ship would keep re-deriving a bearing
        // toward an already-passed point and never terminate (the exact "endlessly
        // re-chasing a point already flown past" failure mode this fix targets).
        var result = ApproachPursuitMath.Step(
            shipX: 0, shipY: -100, shipDirectionDegrees: 0, shipSpeedKmS: 0,
            targetX: 20, targetY: -50, targetDirectionDegrees: 0, targetSpeedKmS: 0,
            trailDistanceWorldUnits: 0, // aim point coincides with target: (20, -50)
            turnStepDegrees: TurnStepDegrees, angularInertiaDegPerSec: AngularInertiaDegPerSec,
            stepTimeMs: 0,
            lockedCourseDegrees: 0);

        // Sanity: the aim point really is outside the plain arrival tolerance —
        // this must be the dot-product safeguard doing the work, not the distance check.
        double distanceToAim = Math.Sqrt(20 * 20 + 50 * 50);
        Assert.True(distanceToAim > ApproachPursuitMath.ArrivalToleranceUnits);

        Assert.True(result.IsArrived);
    }

    [Fact]
    public void Default_asteroid_pose_uses_one_right_straight_right_tail_entry()
    {
        var plan = ApproachPursuitMath.CreateFlyThroughPlan(
            shipX: 10000, shipY: 9998.25,
            shipDirectionDegrees: 0,
            shipSpeedKmS: 0.7,
            targetX: 10400, targetY: 10000,
            targetDirectionDegrees: 256,
            angularInertiaDegPerSec: 4);

        Assert.Equal("RSR", plan.Type);
        Assert.InRange(plan.RemainingUnits, 700, 800);
        Assert.True(plan.FirstRemainingUnits > 0);
        Assert.True(plan.ThirdRemainingUnits > 0);
    }

    [Fact]
    public void Fly_through_plan_finishes_with_exact_target_direction()
    {
        var plan = ApproachPursuitMath.CreateFlyThroughPlan(
            10000, 9998.25, 0, 0.7,
            10400, 10000, 256, 4);
        double direction = 0;

        var step = ApproachPursuitMath.AdvanceFlyThroughPlan(
            plan, direction, 256, travelledUnits: 0, turnStepDegrees: 1);
        for (int i = 0; i < 1000 && !step.IsArrived; i++)
        {
            direction = step.NewDirectionDegrees;
            step = ApproachPursuitMath.AdvanceFlyThroughPlan(
                step.RemainingPlan, direction, 256,
                travelledUnits: 1.75,
                turnStepDegrees: 1);
        }

        Assert.True(step.IsArrived);
        Assert.Equal(256, step.NewDirectionDegrees, precision: 6);
    }

    /// <summary>
    /// Tests for <see cref="ApproachPursuitMath.SolveInterceptFlyThroughPlan"/>
    /// (story-20260829-210641.md, §10, Unit 1 — Batch 1).
    ///
    /// Fixture construction note (per-type tests below): for a STATIONARY target
    /// (targetSpeedKmS = 0), the target's pose never changes, so each curve type's own
    /// length L_X(t) is a CONSTANT (independent of t) — the self-consistency equation
    /// L_X(t) - shipSpeed*t = 0 is then simply LINEAR, with the closed-form solution
    /// t* = L_X(0) / shipSpeed. Because the winning type is the one with the globally
    /// SHORTEST L_X(0) (same ranking <see cref="ApproachPursuitMath.CreateFlyThroughPlan"/>
    /// already picks via argmin), each fixture below was found by brute-force sampling
    /// geometries where <c>CreateFlyThroughPlan</c> already returns exactly the desired
    /// type as its argmin winner (verified independently, outside this repo, against the
    /// exact same production AddLsl/AddRsr/.../AddLrl formulas via reflection — see the
    /// commit's PR description for the search method), which guarantees
    /// <see cref="ApproachPursuitMath.SolveInterceptFlyThroughPlan"/> must find that same
    /// type with t* = plan.RemainingUnits / (shipSpeedKmS * 10).
    /// </summary>
    public class SolveInterceptFlyThroughPlanTests
    {
        private const int Inertia = 4;
        private const int Precision = 2; // decimal places for t*/length assertions.

        private static double ExpectedInterceptTimeSeconds(double lengthUnits, double shipSpeedKmS) =>
            lengthUnits / (shipSpeedKmS * 10.0);

        [Theory]
        // type, shipDir, targetX, targetY, targetDir, expectedLength (stationary target; shipX=10000, shipY=9998.25, shipSpeed=0.7 km/s).
        [InlineData("LSL", 202.0038, 9660.0809, 10994.0972, 195.9278, 1052.2677)]
        [InlineData("RSR", 316.6852, 9230.0014, 9094.4952, 322.4691, 1187.2998)]
        [InlineData("LSR", 37.7877, 10620.6896, 9084.4096, 37.1262, 1104.7054)]
        [InlineData("RSL", 40.6968, 10848.8751, 9109.7925, 40.4525, 1228.8039)]
        [InlineData("RLR", 124.0015, 10193.5022, 9654.7474, 101.2860, 604.4212)]
        [InlineData("LRL", 211.7537, 10078.9365, 10351.0098, 270.3860, 551.9562)]
        public void Solve_finds_the_known_root_for_each_of_the_six_curve_types(
            string expectedType, double shipDirectionDegrees, double targetX, double targetY,
            double targetDirectionDegrees, double expectedLengthUnits)
        {
            const double shipX = 10000, shipY = 9998.25, shipSpeedKmS = 0.7;

            var solution = ApproachPursuitMath.SolveInterceptFlyThroughPlan(
                shipX, shipY, shipDirectionDegrees, shipSpeedKmS,
                targetX, targetY, targetDirectionDegrees, targetSpeedKmS: 0,
                angularInertiaDegPerSec: Inertia);

            Assert.True(solution.HasIntercept);
            Assert.Equal(expectedType, solution.Type);

            double expectedTimeSeconds = ExpectedInterceptTimeSeconds(expectedLengthUnits, shipSpeedKmS);
            Assert.Equal(expectedTimeSeconds, solution.InterceptTimeSeconds, precision: Precision);
            Assert.Equal(expectedLengthUnits, solution.Plan.RemainingUnits, precision: Precision);

            // Stationary target: pose at intercept equals the (unchanging) input pose.
            Assert.Equal(targetX, solution.TargetXAtIntercept, precision: 6);
            Assert.Equal(targetY, solution.TargetYAtIntercept, precision: 6);

            // Self-consistency: the ship covers exactly the curve's length in t* seconds.
            Assert.Equal(
                solution.Plan.RemainingUnits,
                shipSpeedKmS * 10.0 * solution.InterceptTimeSeconds,
                precision: Precision);
        }

        [Fact]
        public void Solve_is_deterministic_for_identical_inputs()
        {
            var a = ApproachPursuitMath.SolveInterceptFlyThroughPlan(
                10000, 9998.25, 47.8626, 4.9966,
                9789.6958, 10088.3081, 299.7607, 1.8953,
                angularInertiaDegPerSec: Inertia);
            var b = ApproachPursuitMath.SolveInterceptFlyThroughPlan(
                10000, 9998.25, 47.8626, 4.9966,
                9789.6958, 10088.3081, 299.7607, 1.8953,
                angularInertiaDegPerSec: Inertia);

            Assert.Equal(a, b);
        }

        [Fact]
        public void Solve_returns_no_intercept_when_ship_is_not_faster_than_target()
        {
            // Same shipSpeed/targetSpeed relationship as the protected SPC-0003/Default
            // scenario (Default_asteroid_pose_uses_one_right_straight_right_tail_entry):
            // shipSpeedKmS (0.7) is far below a plausible target speed — the ship simply
            // cannot out-run the target on any lead-pursuit course, so `a = shipSpeed^2 -
            // |vTarget|^2 <= 0` in the lead-pursuit quadratic and this must resolve to "no
            // intercept" as a DIRECT CONSEQUENCE of that degeneration (story-20260829-
            // 210641.md Checkpoint 1) — not via a separately bolted-on speed check.
            var solution = ApproachPursuitMath.SolveInterceptFlyThroughPlan(
                shipX: 10000, shipY: 9998.25, shipDirectionDegrees: 0, shipSpeedKmS: 0.7,
                targetX: 10400, targetY: 10000, targetDirectionDegrees: 256, targetSpeedKmS: 2.0,
                angularInertiaDegPerSec: Inertia);

            Assert.False(solution.HasIntercept);
            Assert.Equal(ApproachInterceptSolution.None, solution);
        }

        [Fact]
        public void Solve_returns_no_intercept_for_a_stationary_ship_or_zero_inertia()
        {
            var stationaryShip = ApproachPursuitMath.SolveInterceptFlyThroughPlan(
                0, 0, 0, shipSpeedKmS: 0,
                targetX: 100, targetY: 100, targetDirectionDegrees: 45, targetSpeedKmS: 1,
                angularInertiaDegPerSec: Inertia);
            Assert.False(stationaryShip.HasIntercept);

            var noInertia = ApproachPursuitMath.SolveInterceptFlyThroughPlan(
                0, 0, 0, shipSpeedKmS: 5,
                targetX: 100, targetY: 100, targetDirectionDegrees: 45, targetSpeedKmS: 1,
                angularInertiaDegPerSec: 0);
            Assert.False(noInertia.HasIntercept);
        }

        [Fact]
        public void Solve_handles_a_target_almost_at_the_ships_position_without_nan_or_exception()
        {
            // normalizedDistance ~= 0 degenerate case: target essentially co-located with
            // the ship. Must not throw and must not produce NaN in any field, regardless
            // of whether an intercept is actually found.
            var solution = ApproachPursuitMath.SolveInterceptFlyThroughPlan(
                shipX: 10000, shipY: 10000, shipDirectionDegrees: 30, shipSpeedKmS: 3,
                targetX: 10000.001, targetY: 10000.001, targetDirectionDegrees: 200, targetSpeedKmS: 0.1,
                angularInertiaDegPerSec: Inertia);

            Assert.False(double.IsNaN(solution.InterceptTimeSeconds));
            Assert.False(double.IsNaN(solution.TargetXAtIntercept));
            Assert.False(double.IsNaN(solution.TargetYAtIntercept));
            if (solution.HasIntercept)
            {
                Assert.False(double.IsNaN(solution.Plan.RemainingUnits));
                Assert.True(solution.InterceptTimeSeconds >= 0);
            }
        }

        [Fact]
        public void Solve_handles_head_on_courses_without_nan_or_exception()
        {
            // Ship and target flying directly at each other (180 degrees apart on a
            // straight line). Must resolve deterministically, without NaN/exception.
            var solution = ApproachPursuitMath.SolveInterceptFlyThroughPlan(
                shipX: 0, shipY: 0, shipDirectionDegrees: 0, shipSpeedKmS: 5,
                targetX: 0, targetY: -5000, targetDirectionDegrees: 180, targetSpeedKmS: 2,
                angularInertiaDegPerSec: Inertia);

            Assert.False(double.IsNaN(solution.InterceptTimeSeconds));
            Assert.False(double.IsNaN(solution.TargetXAtIntercept));
            Assert.False(double.IsNaN(solution.TargetYAtIntercept));
            if (solution.HasIntercept)
            {
                Assert.True(solution.InterceptTimeSeconds > 0);
                Assert.Equal(
                    solution.Plan.RemainingUnits,
                    5 * 10.0 * solution.InterceptTimeSeconds,
                    precision: 2);
            }
        }

        [Fact]
        public void Solve_avoids_the_type_switch_discontinuity_that_broke_the_reverted_bisection_attempt()
        {
            // Regression for story-20260829-210641.md §2 ("why bisection on t failed"):
            // the previous (reverted) attempt bisected on the ARGMIN-selected curve
            // length as a single function of t, which is discontinuous at points where
            // the globally-shortest curve type switches between the 6 Dubins types even
            // though the underlying geometry (target pose) changes smoothly.
            //
            // This fixture reproduces exactly that failure mode (found by brute-force
            // search over the SAME production CreateFlyThroughPlan, outside this repo —
            // the original bug's exact numbers were never persisted anywhere in the repo
            // or git history, per story-20260829-210641.md §10, Unit 1, so this is a
            // freshly constructed, independently documented equivalent), and additionally
            // surfaces a SECOND, more subtle discontinuity hazard within a single curve
            // type's own formula:
            //
            //   Sampling CreateFlyThroughPlan(shipPose, target(t)) — i.e. what an
            //   argmin-only bisection would evaluate — at t=17s and t=18s along this
            //   target's constant-velocity path gives:
            //     t=17s: type RSL, argmin-vs-shipSpeed*t residual = +4148.78 (positive)
            //     t=18s: type RSR, argmin-vs-shipSpeed*t residual =  -375.47 (negative)
            //   The sign flips ACROSS a type switch (RSL -> RSR) — exactly the spurious
            //   "root" a naive bisection on the argmin function would chase.
            //
            //   Worse: RSR's OWN formula (evaluated with ITS OWN formula alone, never
            //   mixing with RSL) ALSO appears to cross zero right around t ~= 17.95s —
            //   but that apparent crossing is itself an artifact: one of RSR's three
            //   segment angles crosses a 2*pi boundary there (Mod2Pi wraps it from ~2*pi
            //   back to ~0), making RSR's reported length jump from ~5020 to ~523 units
            //   within a 0.02s step even though the target barely moved — a
            //   representation artifact of always reporting the shortest-mod-2*pi
            //   segment angles (the same convention CreateFlyThroughPlan intentionally
            //   uses and must keep), not a genuine physical root. A solver that only
            //   guards against ADMISSIBILITY-domain boundaries (p^2 &lt; 0 / |x| &gt; 1)
            //   and not this wrap artifact would silently accept t~=17.95s as an "RSR"
            //   answer, which does not actually match what a ship flying the fly-through
            //   plan for that pose would need. SolveInterceptFlyThroughPlan must reject
            //   this spurious bracket for RSR too and keep searching until it finds the
            //   real earliest self-consistent root — which for this fixture turns out to
            //   be a DIFFERENT curve type (LRL, t* ~= 104.55s) once both the type-switch
            //   and the intra-type wrap artifact are correctly excluded.
            const double shipX = 10000, shipY = 9998.25, shipDirectionDegrees = 259.7105, shipSpeedKmS = 4.9966;
            const double targetX = 9789.6958, targetY = 10088.3081, targetDirectionDegrees = 299.7607, targetSpeedKmS = 1.8953;

            // Sanity check: confirm the discontinuity actually exists for this fixture
            // (documents WHY this is a meaningful regression fixture, not just an
            // arbitrary intercept test).
            double targetSpeedUnits = targetSpeedKmS * 10.0;
            double targetDirRad = targetDirectionDegrees * Math.PI / 180.0;
            double targetVx = targetSpeedUnits * Math.Sin(targetDirRad);
            double targetVy = -targetSpeedUnits * Math.Cos(targetDirRad);
            double shipSpeedUnits = shipSpeedKmS * 10.0;

            var planAt17 = ApproachPursuitMath.CreateFlyThroughPlan(
                shipX, shipY, shipDirectionDegrees, shipSpeedKmS,
                targetX + 17 * targetVx, targetY + 17 * targetVy, targetDirectionDegrees, Inertia);
            var planAt18 = ApproachPursuitMath.CreateFlyThroughPlan(
                shipX, shipY, shipDirectionDegrees, shipSpeedKmS,
                targetX + 18 * targetVx, targetY + 18 * targetVy, targetDirectionDegrees, Inertia);

            double residualAt17 = planAt17.RemainingUnits - shipSpeedUnits * 17;
            double residualAt18 = planAt18.RemainingUnits - shipSpeedUnits * 18;

            Assert.NotEqual(planAt17.Type, planAt18.Type); // Type switch really happens here.
            Assert.True(residualAt17 > 0 && residualAt18 < 0); // Naive argmin sign flip.

            // Now the actual solver: must NOT land on the spurious ~17.95s RSR wrap
            // artifact, nor on the RSL/RSR type-switch bracket — it must find the real
            // earliest self-consistent root across all 6 types.
            var solution = ApproachPursuitMath.SolveInterceptFlyThroughPlan(
                shipX, shipY, shipDirectionDegrees, shipSpeedKmS,
                targetX, targetY, targetDirectionDegrees, targetSpeedKmS,
                angularInertiaDegPerSec: Inertia);

            Assert.True(solution.HasIntercept);
            Assert.NotEqual("RSR", solution.Type); // Not the spurious wrap-artifact "root".
            Assert.True(solution.InterceptTimeSeconds > 20); // Well clear of the [17,18] artifact zone.
            Assert.Equal("LRL", solution.Type);
            Assert.InRange(solution.InterceptTimeSeconds, 104, 105);

            // Self-consistency: the ship covers exactly the curve's length in t* seconds
            // — computed from the winning type's OWN formula at t*, not the argmin's
            // (possibly discontinuous) length.
            Assert.Equal(
                solution.Plan.RemainingUnits,
                shipSpeedUnits * solution.InterceptTimeSeconds,
                precision: 2);
        }
    }
}
