namespace DeepSpaceSaga.Motion.Tests;

public class NavigationWaypointMathTests
{
    // Engine module defaults (module.engine.basic): TurnStepDegrees = 1, AngularInertia = 4 °/s.
    private const int TurnStepDegrees = 1;
    private const int AngularInertiaDegPerSec = 4;

    // R = v/ω in world units: v km/s × 1000 / (ω × π/180) / 100.
    // 1 km/s → 1800/(4π) ≈ 143.24 units; 4 km/s → ≈ 572.96 units.

    [Fact]
    public void Step_is_deterministic_for_identical_inputs()
    {
        var a = NavigationWaypointMath.Step(0, 0, 0, 4, 1000, -3000, TurnStepDegrees, AngularInertiaDegPerSec);
        var b = NavigationWaypointMath.Step(0, 0, 0, 4, 1000, -3000, TurnStepDegrees, AngularInertiaDegPerSec);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Ship_already_at_target_arrives()
    {
        var result = NavigationWaypointMath.Step(0, 0, 0, 4, 0.5, -0.5, TurnStepDegrees, AngularInertiaDegPerSec);

        Assert.True(result.IsArrived);
        Assert.Equal(0, result.TurnDeltaDegrees);
    }

    [Fact]
    public void Target_within_arrival_epsilon_arrives()
    {
        var result = NavigationWaypointMath.Step(0, 0, 0, 4, 0.9, -0.4, TurnStepDegrees, AngularInertiaDegPerSec);

        Assert.True(result.IsArrived);
    }

    [Fact]
    public void Ship_aimed_at_target_ahead_keeps_course()
    {
        // Target straight ahead on the course line (0° = up). |delta| = 0 ≤ step/2 → no turn.
        var result = NavigationWaypointMath.Step(0, 0, 0, 1, 0, -500, TurnStepDegrees, AngularInertiaDegPerSec);

        Assert.False(result.IsArrived);
        Assert.Equal(0, result.TurnDeltaDegrees);
    }

    [Fact]
    public void Ship_that_flew_past_target_arrives()
    {
        // Target straight ahead on the course; the ship's discrete step jumped over it
        // and the target now sits just behind it (r ≤ ArrivalEpsilon) → arrived.
        var result = NavigationWaypointMath.Step(0, -100.8, 0, 4, 0, -100, TurnStepDegrees, AngularInertiaDegPerSec);

        Assert.True(result.IsArrived);
        Assert.Equal(0, result.TurnDeltaDegrees);
    }

    [Fact]
    public void Target_close_side_behind_r_below_R_waits()
    {
        // Target at (100, 70) behind-side: r ≈ 122 < R ≈ 143 at 1 km/s → no turn (AC7).
        var result = NavigationWaypointMath.Step(0, 0, 0, 1, 100, 70, TurnStepDegrees, AngularInertiaDegPerSec);

        Assert.False(result.IsArrived);
        Assert.Equal(0, result.TurnDeltaDegrees);
    }

    [Fact]
    public void Staged_approach_keeps_turning_inside_turn_radius()
    {
        // Close-target staged navigation already performed EscapeTurn/EscapeDepart.
        // Once in Approach, it must not re-enter the old r < R wait, or it draws
        // a wide loop around the destination.
        var result = NavigationWaypointMath.StagedStep(
            0, 0,
            directionDegrees: 0,
            speedKmS: 1,
            targetX: 100,
            targetY: 70,
            TurnStepDegrees,
            AngularInertiaDegPerSec,
            stepTimeMs: 250,
            phase: "Approach",
            escapeCourseDegrees: 305,
            requiredDepartureDistance: 320);

        Assert.False(result.IsArrived);
        Assert.NotEqual(0, result.TurnDeltaDegrees);
    }

    [Fact]
    public void Target_far_side_r_above_R_turns_by_step()
    {
        // Target (1000, -3000) far ahead-side at 4 km/s: r ≈ 3162 ≥ R ≈ 573.
        // Bearing ≈ 18.43° → delta ≈ 18.43° > 0.5 → turn right by one step (1°).
        var result = NavigationWaypointMath.Step(0, 0, 0, 4, 1000, -3000, TurnStepDegrees, AngularInertiaDegPerSec);

        Assert.False(result.IsArrived);
        Assert.Equal(1, result.TurnDeltaDegrees);
    }

    [Fact]
    public void Partial_turn_is_never_larger_than_turn_step()
    {
        // Target (100, -10000) almost straight ahead at 1 km/s: r ≈ 10000 ≥ R ≈ 143.
        // Bearing ≈ 0.57° → delta ≈ 0.57° > 0.5 → partial turn of exactly 0.57° (< step).
        var result = NavigationWaypointMath.Step(0, 0, 0, 1, 100, -10000, TurnStepDegrees, AngularInertiaDegPerSec);

        Assert.Equal(0.57, result.TurnDeltaDegrees, precision: 2);
        Assert.True(result.TurnDeltaDegrees < TurnStepDegrees);
    }

    [Fact]
    public void Stationary_ship_can_turn_on_the_spot()
    {
        // v = 0 → R = 0 → r ≥ R always → turn is possible even for a close target.
        var result = NavigationWaypointMath.Step(0, 0, 0, 0, 100, 70, TurnStepDegrees, AngularInertiaDegPerSec);

        Assert.False(result.IsArrived);
        Assert.Equal(1, result.TurnDeltaDegrees);
    }

    [Fact]
    public void Zero_angular_inertia_never_turns()
    {
        // Module without angular inertia cannot turn at all (engine rejects such modules
        // earlier; the pure math must stay safe: no division by zero, no NaN).
        var result = NavigationWaypointMath.Step(0, 0, 0, 4, 1000, -3000, TurnStepDegrees, 0);

        Assert.False(result.IsArrived);
        Assert.Equal(0, result.TurnDeltaDegrees);
    }

    [Fact]
    public void Turn_direction_follows_bearing_sign()
    {
        // Target to the LEFT of course (bearing 341.57° = -18.43°): delta negative → turn left (-1°).
        var left = NavigationWaypointMath.Step(0, 0, 0, 4, -1000, -3000, TurnStepDegrees, AngularInertiaDegPerSec);
        Assert.Equal(-1, left.TurnDeltaDegrees);

        // Target to the RIGHT of course: delta positive → turn right (+1°).
        var right = NavigationWaypointMath.Step(0, 0, 0, 4, 1000, -3000, TurnStepDegrees, AngularInertiaDegPerSec);
        Assert.Equal(1, right.TurnDeltaDegrees);
    }

    [Fact]
    public void Normalizes_non_canonical_directions()
    {
        // Direction 360 == 0: target straight ahead → on course, no turn.
        var result = NavigationWaypointMath.Step(0, 0, 360, 1, 0, -500, TurnStepDegrees, AngularInertiaDegPerSec);

        Assert.False(result.IsArrived);
        Assert.Equal(0, result.TurnDeltaDegrees);
    }
}
