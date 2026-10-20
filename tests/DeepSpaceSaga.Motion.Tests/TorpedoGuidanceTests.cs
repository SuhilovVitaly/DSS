using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion.Tests;

public sealed class TorpedoGuidanceTests
{
    [Theory]
    [InlineData(0, 0, -300, 180, 1)]
    [InlineData(0, 300, 0, 90, 1)]
    [InlineData(0, 0, 300, 0, 0)]
    [InlineData(270, 500, -700, 120, .4)]
    [InlineData(90, 300, 20, 90, 2.999999)]
    [InlineData(0, 0, -300, 180, 4)]
    [InlineData(0, 0, 0, 180, 0)]
    public void Torpedo_intercepts_linear_target_from_carrier_heading(double heading, double x, double y,
        double targetHeading, double targetSpeed)
    {
        var route = TorpedoGuidanceMath.Plan(new("p", 0, 0, 999, heading), new("t", x, y, targetSpeed, targetHeading), 3, 90);
        Assert.True(route.HasIntercept);
        double time = route.Segments.Sum(s => s.DurationMs);
        var end = TorpedoGuidanceMath.PredictPose(route, time);
        double distance = targetSpeed * time / 100;
        Assert.InRange(Math.Abs(end.X - x - distance * Math.Sin(targetHeading * Math.PI / 180)), 0, .001);
        Assert.InRange(Math.Abs(end.Y - y + distance * Math.Cos(targetHeading * Math.PI / 180)), 0, .001);
        Assert.Equal(heading, TorpedoGuidanceMath.PredictPose(route, 0).Direction);
        if (heading == 0 && x == 0 && y == -300 && targetSpeed == 1)
            Assert.InRange(Math.Abs(time - 7500), 0, 1e-6);
    }

    // Captured before the performance optimization: exact route doubles are
    // authoritative inputs to guidance, contact ordering and save continuation.
    [Theory]
    [InlineData(0, 0, 6666.666666666667, 7.894943379604946e-13, 1333.3333333333323, 1299.9999991734512)]
    [InlineData(1, 539.799596582183, 6875.86966625748, 4.736927478352391e-13, 1387.1303283121176, 1369.3155538607389)]
    [InlineData(2, 1322.0122906034583, 7204.229834066681, 0, 1583.3472779642807, 1581.5318842295092)]
    [InlineData(3, 2053.424228786781, 8502.029208556587, 0, 1889.416172259475, 1903.0194911949802)]
    [InlineData(7, 1856.557282450719, 6863.358127316225, 0, 2050.7462892764847, 2129.1246948302723)]
    [InlineData(41, 183.83856928812455, 13719.18728543411, 0, 1990.7352272924422, 2613.0810384175275)]
    [InlineData(99, 1048.5271636669409, 9633.961610914166, 4.736927478352391e-13, 1764.6702550453765, 3314.668057134608)]
    public void Planner_optimization_preserves_exact_routes_and_encounter_times(int i,
        double turn, double straight, double defenseTurn, double defenseStraight, double encounter)
    {
        var origin = new ObjectMotionSnapshot("p", (i % 8) * 20, 0, 3, (i % 5) * 71.3);
        var target = new ObjectMotionSnapshot("t", (i % 7) * 37, -200 - i, i % 4 == 0 ? 0 : 1, 90 + i * .01);
        var route = TorpedoGuidanceMath.Plan(origin, target, 3, 90);
        Assert.True(route.HasIntercept);
        Assert.Equal(2, route.Segments.Length);
        Assert.Equal(turn, route.Segments[0].DurationMs);
        Assert.Equal(straight, route.Segments[1].DurationMs);
        var defender = target with { ObjectId = "cm", Direction = target.Direction + 13 };
        var defense = CountermeasureGuidanceMath.PlanManualLaunch(defender, route, i * 16, 60000, 12, 90,
            route.Segments.Sum(s => s.DurationMs));
        Assert.True(defense.Route.HasIntercept);
        Assert.Equal(2, defense.Route.Segments.Length);
        Assert.Equal(defenseTurn, defense.Route.Segments[0].DurationMs);
        Assert.Equal(defenseStraight, defense.Route.Segments[1].DurationMs);
        Assert.Equal(encounter, defense.PredictedEncounterMotionTimeMs);
    }

    [Fact]
    public void Torpedo_turn_rate_and_speed_are_bounded()
    {
        var route = TorpedoGuidanceMath.Plan(new("p", 0, 0, 50, 0), new("t", 300, 0, 0, 0), 3, 90);
        Assert.All(route.Segments, s => { Assert.Equal(3, s.SpeedKmS); Assert.InRange(Math.Abs(s.AngularVelocityDegPerSec), 0, 90); });
        var quarter = TorpedoGuidanceMath.PredictSegment(new(0, 0, 0, 3, 90, 1000), 1000);
        double radius = 30 / (Math.PI / 2);
        Assert.InRange(Math.Abs(quarter.X - radius), 0, 1e-9);
        Assert.InRange(Math.Abs(quarter.Y + radius), 0, 1e-9);
        Assert.Equal(90, quarter.Direction);
    }

    [Fact]
    public void Torpedo_endpoint_has_no_trail_offset_or_heading_match()
    {
        var route = TorpedoGuidanceMath.Plan(new("p", 0, 0, 0, 90), new("t", 300, 0, 0, 270), 3, 90);
        var end = TorpedoGuidanceMath.PredictPose(route, route.Segments.Sum(s => s.DurationMs));
        Assert.InRange(Math.Abs(end.X - 300), 0, 1e-8);
        Assert.InRange(Math.Abs(end.Y), 0, 1e-8);
        Assert.Equal(90, end.Direction);
    }

    [Fact]
    public void No_intercept_returns_null_eta_without_stopping_motion()
    {
        var route = TorpedoGuidanceMath.Plan(new("p", 0, 0, 0, 90), new("t", 300, 0, 4, 90), 3, 90);
        Assert.False(route.HasIntercept);
        var pose = TorpedoGuidanceMath.PredictPose(route, 100_000_000);
        Assert.InRange(pose.X, 2_999_999, 3_000_001);
    }

    [Fact]
    public void Guidance_is_time_partition_invariant()
    {
        var route = TorpedoGuidanceMath.Plan(new("p", 0, 0, 0, 0), new("t", 300, 100, 1, 90), 3, 90);
        var state = new ObjectMotionSnapshot("p", 0, 0, 3, 0,
            Torpedo: new("owner", "launcher", "t", 0, 3, 90, 150, 0, route));
        var predictor = new LinearMotionPredictor();
        Assert.False(LinearMotionPredictor.IsLinear(state));
        var whole = predictor.Predict(state, 12789);
        var partition = state;
        foreach (long step in new long[] { 1, 99, 901, 788, 11000 }) partition = predictor.Predict(partition, step);
        Assert.Equal(whole, partition);
        Assert.Equal(3, whole.SpeedKmS);
    }
}
