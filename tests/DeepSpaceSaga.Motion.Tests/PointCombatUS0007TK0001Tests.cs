using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion.Tests;

public sealed class PointCombatUS0007TK0001Tests
{
    private static HostileCombatSnapshot Plan(ObjectMotionSnapshot target, double speed = 8, double flank = 0) =>
        HostileOrbitMath.PlanAttack(new("npc", 0, 0, 0, 0), target, 0, "engine", "launcher", 25, speed, 1, 3600, 8, flank);

    [Fact]
    public void Fast_target_is_reachable_without_120_second_runaway()
    {
        // Target crosses the line of sight at 6 km/s, pursuer reaches its 25 km circle at t=12.5.
        // sqrt(100^2 + (6*t)^2) = 25 + 8*t -> t = 12.5.
        double t = (-400 + Math.Sqrt(400 * 400 + 4 * 28 * 9375)) / 56;
        var plan = Plan(new("target", 1000, 0, 6, 0));
        double expectedBearing = Math.Atan2(100, 6 * t) * 180 / Math.PI;
        Assert.Equal(expectedBearing / .1, plan.Segment.TurnRateDegPerSec, 9);
        Assert.Equal(Math.Sqrt(10000 + 36 * t * t), 25 + 8 * t, 9);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(6.000000000001)]
    [InlineData(8)]
    public void Unreachable_receding_target_uses_present_position(double targetSpeed)
    {
        var plan = Plan(new("target", 1000, -1000, targetSpeed, 0), speed: 6);
        Assert.Equal(450, plan.Segment.TurnRateDegPerSec, 8);
        Assert.True(double.IsFinite(plan.Segment.TurnRateDegPerSec));
    }

    [Fact]
    public void Equal_speed_crossing_target_has_a_finite_reachable_intercept()
    {
        // 10000 + 36*t^2 = (25 + 6*t)^2 => t=31.25.
        var plan = Plan(new("target", 1000, 0, 6, 0), speed: 6);
        Assert.Equal(Math.Atan2(100, 187.5) * 180 / Math.PI / .1, plan.Segment.TurnRateDegPerSec, 8);
    }

    [Fact]
    public void Orbit_fix_is_deterministic_and_keeps_flank_sign()
    {
        var target = new ObjectMotionSnapshot("target", 1000, 0, 6, 0);
        var straight = Plan(target);
        var left = Plan(target, flank: -35);
        var right = Plan(target, flank: 35);
        Assert.Equal(right, Plan(target, flank: 35));
        Assert.Equal(262.5, right.Segment.TurnRateDegPerSec - straight.Segment.TurnRateDegPerSec, 8);
        Assert.Equal(-262.5, left.Segment.TurnRateDegPerSec - straight.Segment.TurnRateDegPerSec, 8);
        Assert.Equal(left.Clockwise, right.Clockwise);
    }
}
