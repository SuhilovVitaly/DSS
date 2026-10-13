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
}
