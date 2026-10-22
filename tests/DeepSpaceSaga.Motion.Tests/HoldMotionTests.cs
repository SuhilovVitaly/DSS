using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion.Tests;

public sealed class HoldMotionTests
{
    private static ObjectMotionSnapshot Ship(double acceleration = .6, double turn = 20, double speed = 3, bool latched = false) =>
        new("player", 0, 0, speed, 0, MaxSpeedKmS: 5, TurnHoldRateDegPerSec: turn, SpeedHoldAccelerationKmS2: acceleration, SpeedHoldLatched: latched);

    [Theory]
    [InlineData(0, -20, 320, 3)]
    [InlineData(0, 20, 40, 3)]
    [InlineData(.6, 0, 0, 4.2)]
    [InlineData(-.6, 0, 0, 1.8)]
    [InlineData(.6, 20, 40, 4.2)]
    public void Source_table_two_second_cases_match_analytical_orbit(double acceleration, double turn, double direction, double speed)
    {
        var ship = Ship(acceleration, turn);
        var expected = HostileOrbitMath.Sample(new(0, long.MaxValue, 0, 0, 0, 3, 0, acceleration, turn), 2000);
        var actual = CurrentManeuverForecast.Sample(ship, 2000);
        EqualPose(actual, expected.X, expected.Y, direction, speed);
        Assert.False(LinearMotionPredictor.IsLinear(ship));
        EqualPose(new LinearMotionPredictor().Predict(ship, 2000), expected.X, expected.Y, direction, speed);
    }

    [Theory]
    [InlineData(.6, 20, false)]
    [InlineData(.6, -20, true)]
    [InlineData(-.6, 20, false)]
    [InlineData(-.6, 0, false)]
    public void Speed_boundary_splits_exactly_and_is_invariant_to_physical_step_partition(double acceleration, double turn, bool latched)
    {
        var ship = Ship(acceleration, turn, latched: latched);
        double cap = acceleration > 0 ? (5 - 3) / acceleration * 1000 : -3 / acceleration * 1000;
        var powered = HostileOrbitMath.Sample(new(0, long.MaxValue, 0, 0, 0, 3, 0, acceleration, turn), cap);
        var expected = HostileOrbitMath.Sample(new(0, long.MaxValue, 0, powered.X, powered.Y, acceleration > 0 ? 5 : 0, powered.Direction, 0, turn), 20000 - cap);
        var direct = HoldMotion.Sample(ship, 20000);
        EqualPose(direct, expected.X, expected.Y, expected.Direction, expected.SpeedKmS);
        var partitioned = ship;
        for (int elapsed = 0; elapsed < 20000; elapsed += 17) partitioned = HoldMotion.Sample(partitioned, Math.Min(17, 20000 - elapsed));
        EqualPose(partitioned, direct.X, direct.Y, direct.Direction, direct.SpeedKmS);
        Assert.Equal(turn, partitioned.TurnHoldRateDegPerSec);
        Assert.Equal(latched ? null : acceleration, partitioned.SpeedHoldAccelerationKmS2);
        Assert.False(partitioned.SpeedHoldLatched);
    }

    [Fact]
    public void Straight_acceleration_and_stop_have_exact_distances_and_latch_releases_at_limit()
    {
        var accelerating = HoldMotion.Sample(Ship(turn: 0, latched: true), 10000);
        double capSeconds = 2 / .6;
        Assert.Equal(-(3 * capSeconds + .3 * capSeconds * capSeconds + 5 * (10 - capSeconds)) * 10, accelerating.Y, 8);
        Assert.Null(accelerating.SpeedHoldAccelerationKmS2); Assert.False(accelerating.SpeedHoldLatched);
        var braking = HoldMotion.Sample(Ship(-.6, 0), 10000);
        Assert.Equal(-75, braking.Y, 8); Assert.Equal(0, braking.SpeedKmS);
        var atMax = HoldMotion.Sample(Ship(speed: 5, latched: true), 0);
        Assert.Null(atMax.SpeedHoldAccelerationKmS2); Assert.Equal(5, atMax.SpeedKmS);
    }

    private static void EqualPose(ObjectMotionSnapshot actual, double x, double y, double direction, double speed)
    {
        Assert.InRange(Math.Abs(actual.X - x), 0, 1e-6); Assert.InRange(Math.Abs(actual.Y - y), 0, 1e-6);
        Assert.InRange(Math.Abs(actual.Direction - direction), 0, 1e-6); Assert.InRange(Math.Abs(actual.SpeedKmS - speed), 0, 1e-6);
    }
}
