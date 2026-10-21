using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Motion.Tests;

public sealed class EvasionSteeringTests
{
    [Fact]
    public void Bounded_turn_acceleration_return_and_partitioning_match_analytic_motion()
    {
        var basis = new ObjectMotionSnapshot("ship", 0, 0, 3, 0);
        var plan = EvasionMotionMath.Plan(basis, basis, 0, 10000, "threat", EvadeSide.Right, 90, 5, 20, .6);
        var start = basis with { Evasion = plan };
        var first = CurrentManeuverForecast.Sample(start, 1000);
        Assert.Equal(20, first.Direction, 10); Assert.Equal(3.6, first.SpeedKmS, 10);
        var analytic = HostileOrbitMath.Sample(new(0, 1000, 0, 0, 0, 3, 0, .6, 20), 1000);
        Assert.Equal(analytic.X, first.X, 10); Assert.Equal(analytic.Y, first.Y, 10);
        var whole = CurrentManeuverForecast.Sample(start, 12000);
        var split = CurrentManeuverForecast.Sample(CurrentManeuverForecast.Sample(start, 1123), 10877);
        Assert.Equal(whole.X, split.X, 8); Assert.Equal(whole.Y, split.Y, 8);
        Assert.Equal(50, whole.Direction, 8); Assert.Equal(3.8, whole.SpeedKmS, 8);
        var ended = CurrentManeuverForecast.Sample(start, 16000);
        Assert.Null(ended.Evasion); Assert.Equal(0, ended.Direction); Assert.Equal(3, ended.SpeedKmS);
    }

    [Theory]
    [InlineData(60, 0, 0, 0, 6, 10)]
    [InlineData(60, 0, -4, 0, 6, 6)]
    [InlineData(60, 0, 4, 0, 6, 30)]
    [InlineData(0, 0, 4, 0, 6, 0)]
    public void Line_intercept_independent_vectors(double x, double y, double vx, double vy, double speed, double expected) =>
        Assert.Equal(expected, LineInterceptMath.Seconds(x, y, vx, vy, speed)!.Value, 9);

    [Fact]
    public void Line_intercept_handles_equal_speed_and_unreachable_targets()
    {
        Assert.Equal(5, LineInterceptMath.Seconds(60, 0, -6, 0, 6));
        Assert.Null(LineInterceptMath.Seconds(60, 0, 6, 0, 6));
        Assert.Null(LineInterceptMath.Seconds(60, 0, 10, 0, 6));
        Assert.Equal(6, LineInterceptMath.Seconds(0, 60, 8, 0, 12.806248474865697)!.Value, 8);
    }
}
