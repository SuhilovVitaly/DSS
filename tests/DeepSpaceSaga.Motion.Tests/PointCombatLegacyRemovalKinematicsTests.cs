using DeepSpaceSaga.Contracts;
namespace DeepSpaceSaga.Motion.Tests;

public sealed class PointCombatLegacyRemovalKinematicsTests
{
    [Fact]
    public void Generic_held_turn_retains_quarter_circle_geometry_and_partitioned_speed()
    {
        var ship = new ObjectMotionSnapshot("ship", 0, 0, 3, 0, TurnHoldRateDegPerSec: 90);
        var whole = CurrentManeuverForecast.Sample(ship, 1000);
        var split = CurrentManeuverForecast.Sample(CurrentManeuverForecast.Sample(ship, 100), 900);
        double radius = 30 / (Math.PI / 2);
        Assert.Equal(radius, whole.X, 10); Assert.Equal(-radius, whole.Y, 10);
        Assert.Equal(90, whole.Direction, 10); Assert.Equal(3, whole.SpeedKmS);
        Assert.Equal(whole.X, split.X, 10); Assert.Equal(whole.Y, split.Y, 10); Assert.Equal(whole.Direction, split.Direction, 10);
    }

    [Theory]
    [InlineData(0, 1000, 0, -30)]
    [InlineData(90, 1000, 30, 0)]
    [InlineData(90, -.25, -.0075, 0)]
    [InlineData(270, .75, -.0225, 0)]
    public void Fractional_straight_integer_clock_anchoring_requires_no_guidance(double heading, double time, double x, double y)
    {
        var point = LinearMotionPredictor.PredictStraightPose(0, 0, heading, 3, time);
        Assert.Equal(x, point.X, 12); Assert.Equal(y, point.Y, 12);
        var back = LinearMotionPredictor.PredictStraightPose(point.X, point.Y, point.Direction, 3, -time);
        Assert.Equal(0, back.X, 12); Assert.Equal(0, back.Y, 12);
    }

    [Theory]
    [InlineData(.5, 2)]
    [InlineData(-.5, 0)]
    public void Generic_current_acceleration_clamps_without_reversing(double acceleration, double expected)
    {
        var ship = new ObjectMotionSnapshot("ship", 0, -300, 1, 90, MaxSpeedKmS: 2,
            HostileCombat: new("owner", "engine", "launcher", 50, true, HostileCombatPhase.Pursuit,
                new(0, 100, 0, 0, -300, 1, 90, acceleration, 30)));
        var sample = CurrentManeuverForecast.Sample(ship, 10000);
        Assert.Equal(expected, sample.SpeedKmS); Assert.InRange(sample.Direction, 0, 360);
    }

    [Fact]
    public void Retired_guided_payloads_are_rejected_instead_of_activating_an_alternate_predictor()
    {
        var legacy = new ObjectMotionSnapshot("old", 0, 0, 3, 0, Torpedo: new("owner", "module", "target", 0, 3, 90, 200, 0,
            new(0, 1, TorpedoRoutePhase.Straight, false, [new(0, 0, 0, 3, 0, 0)])));
        Assert.False(LinearMotionPredictor.IsLinear(legacy));
        Assert.Throws<ArgumentException>(() => new LinearMotionPredictor().Predict(legacy, 1));
        Assert.Throws<ArgumentException>(() => CombatForecast.Torpedo(legacy, new("target", 100, 0, 0, 0), 0));
        foreach (string name in new[] { "TorpedoGuidance" + "Math", "CountermeasureGuidance" + "Math", "CombatGuidance" + "Rules" })
            Assert.Null(typeof(LinearMotionPredictor).Assembly.GetType("DeepSpaceSaga.Motion." + name));
    }
}
