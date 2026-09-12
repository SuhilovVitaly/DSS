using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion.Tests;

public class MotionPositionTests
{
    [Theory]
    [InlineData(-10000)]
    [InlineData(0)]
    [InlineData(375)]
    [InlineData(200000)]
    public void Linear_coordinates_equal_snapshot_prediction(long time)
    {
        var predictor = new LinearMotionPredictor();
        foreach (double speed in new[] { 0d, .01, 3, 1000 })
        for (int direction = 0; direction < 360; direction += 7)
        {
            var state = new ObjectMotionSnapshot("test", -500, 800, speed, direction);
            Assert.True(LinearMotionPredictor.TryPredictLinearPosition(state, time, out double x, out double y));
            var expected = predictor.Predict(state, time);
            Assert.Equal(expected.X, x);
            Assert.Equal(expected.Y, y);
        }
    }

    [Fact]
    public void Position_fast_path_rejects_turning_and_navigation_states()
    {
        var state = new ObjectMotionSnapshot("test", 0, 0, 3, 90);
        Assert.False(LinearMotionPredictor.IsLinear(state with { TurnStepDegrees = 1, TurnStepIntervalMs = 250 }));
        Assert.False(LinearMotionPredictor.IsLinear(state with { ActiveEngineCommandType = NavigationComputerCommandTypes.Approach }));
        Assert.False(LinearMotionPredictor.IsLinear(state with { ActiveEngineCommandType = ShipEngineCommandTypes.Orbit }));
    }

    [Fact]
    public void Route_pose_follows_a_quarter_circle_then_continues_straight()
    {
        double radius = 20 / Math.PI;
        var route = new ApproachRoute(0, 0, 0, 1, 90, "RSL", 10, 20, 0, 100, 0, 90, 1, 10);
        foreach (var (time, x, y, direction) in new[]
        {
            (0d, 0d, 0d, 0d),
            (500d, radius * (1 - Math.Sqrt(.5)), -radius * Math.Sqrt(.5), 45d),
            (1000d, radius, -radius, 90d),
            (2000d, radius + 10, -radius, 90d),
            (3000d, radius + 20, -radius, 90d),
            (5000d, radius + 40, -radius, 90d)
        })
        {
            var pose = ApproachLineCaptureMath.PredictPose(route, time);
            Assert.Equal(x, pose.X, 8);
            Assert.Equal(y, pose.Y, 8);
            Assert.Equal(direction, pose.Direction, 8);
        }
    }
}
