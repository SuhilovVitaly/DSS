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
