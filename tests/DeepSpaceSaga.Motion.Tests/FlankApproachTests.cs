using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion.Tests;

public sealed class FlankApproachTests
{
    private static HostileCombatSnapshot Attack(ObjectMotionSnapshot ship, ObjectMotionSnapshot target,
        double standoff = 50, double band = 10, double flank = 35) =>
        HostileOrbitMath.PlanAttack(ship, target, 0, "engine", "weapon", standoff,
            8, 10, 1000, band, flank);

    [Fact]
    public void Missing_config_uses_exact_ep8_plan()
    {
        var ship = new ObjectMotionSnapshot("npc", 1000, 100, 2, 93);
        var target = new ObjectMotionSnapshot("player", 0, 0, 1, 45);
        var old = HostileOrbitMath.Plan(ship, target, 100, "engine", "weapon", 50, 8, 0.8, 12);
        var unconfigured = HostileOrbitMath.PlanAttack(ship, target, 100, "engine", "weapon", 50,
            8, 0.8, 12);
        Assert.Equal(old, unconfigured);
    }

    [Fact]
    public void Far_target_uses_lead_and_distance_scaled_flank()
    {
        var ship = new ObjectMotionSnapshot("npc", 0, 1000, 0, 0);
        var target = new ObjectMotionSnapshot("player", 0, 0, 1, 90);
        var direct = Attack(ship, target, flank: 0);
        var right = Attack(ship, target);
        var left = Attack(ship, target, flank: -35);
        Assert.Equal(HostileCombatPhase.FlankApproach, right.Phase);
        double directHeading = direct.Segment.TurnRateDegPerSec * 0.1;
        Assert.True(directHeading > 0); // lead east of the present target pose
        Assert.Equal(17.5, (right.Segment.TurnRateDegPerSec - direct.Segment.TurnRateDegPerSec) * 0.1, 8);
        Assert.Equal(-17.5, (left.Segment.TurnRateDegPerSec - direct.Segment.TurnRateDegPerSec) * 0.1, 8);
    }

    [Fact]
    public void Inside_band_orbits_and_too_close_retreats()
    {
        var target = new ObjectMotionSnapshot("player", 0, 0, 0, 0);
        var orbit = Attack(new("npc", 0, 500, 1, 90), target);
        Assert.Equal(HostileCombatPhase.OrbitHold, orbit.Phase);
        var retreat = Attack(new("npc", 0, 100, 1, 180), target);
        Assert.Equal(HostileCombatPhase.Retreat, retreat.Phase);
        Assert.Equal(0, retreat.Segment.TurnRateDegPerSec, 8);
        Assert.True(retreat.Segment.AccelerationKmS2 > 0);
        var unreachableCentre = Attack(new("npc", 0, 500, 1, 90),
            target with { SpeedKmS = 8 });
        Assert.Equal(HostileCombatPhase.Pursuit, unreachableCentre.Phase);
    }
    [Fact]
    public void Band_boundary_has_continuous_controls()
    {
        var target = new ObjectMotionSnapshot("player", 0, 0, 2, 0);
        foreach (double edge in new[] { 40d, 60d })
        {
            var below = Attack(new("npc", 0, edge * 10 - .0001, 3, 90), target);
            var above = Attack(new("npc", 0, edge * 10 + .0001, 3, 90), target);
            Assert.InRange(Math.Abs(below.Segment.TurnRateDegPerSec - above.Segment.TurnRateDegPerSec), 0, .01);
            Assert.InRange(Math.Abs(below.Segment.AccelerationKmS2 - above.Segment.AccelerationKmS2), 0, .01);
        }
    }

    [Fact]
    public void Consecutive_controls_ramp_instead_of_reversing_at_full_rate()
    {
        var ship = new ObjectMotionSnapshot("npc", 0, 1000, 3, 90);
        var target = new ObjectMotionSnapshot("player", 0, 0, 2, 0);
        var previous = HostileOrbitMath.PlanAttack(ship, target, 0, "engine", "weapon", 50, 8, .8, 12, 10, 35);
        var next = HostileOrbitMath.PlanAttack(ship, target with { X = 2000 }, 100, "engine", "weapon", 50, 8, .8, 12, 10, -35, previous);
        Assert.InRange(Math.Abs(next.Segment.TurnRateDegPerSec - previous.Segment.TurnRateDegPerSec), 0, 1.200001);
        Assert.InRange(Math.Abs(next.Segment.AccelerationKmS2 - previous.Segment.AccelerationKmS2), 0, .080001);
    }

    [Theory]
    [InlineData(-35, 1800, -2400)]
    [InlineData(0, 2100, -2142.428528562855)]
    [InlineData(35, 2400, -1800)]
    public void Crossing_encounter_stays_continuous_and_within_engine_limits(double flank, double x, double y)
    {
        var ship = new ObjectMotionSnapshot("npc", x, y, 3, 270);
        HostileCombatSnapshot? previous = null;
        for (long time = 0; time < 500000; time += 100)
        {
            var target = new ObjectMotionSnapshot("player", 0, -time * .02, 2, 0);
            var plan = HostileOrbitMath.PlanAttack(ship, target, time, "engine", "weapon", 50, 8, .8, 12, 10, flank, previous);
            Assert.InRange(plan.Segment.AccelerationKmS2, -.800001, .800001);
            Assert.InRange(plan.Segment.TurnRateDegPerSec, -12, 12);
            if (previous is not null)
                Assert.InRange(Math.Abs(plan.Segment.TurnRateDegPerSec - previous.Segment.TurnRateDegPerSec), 0, 1.200001);
            var sample = HostileOrbitMath.Sample(plan.Segment, 100);
            Assert.InRange(sample.SpeedKmS, 0, 8.000001);
            Assert.InRange(Math.Sqrt(Math.Pow(sample.X - ship.X, 2) + Math.Pow(sample.Y - ship.Y, 2)), 0, 8.000001);
            ship = ship with { X = sample.X, Y = sample.Y, SpeedKmS = sample.SpeedKmS, Direction = sample.Direction };
            previous = plan;
        }
    }

}
