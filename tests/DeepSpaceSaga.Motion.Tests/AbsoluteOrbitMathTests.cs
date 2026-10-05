using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion.Tests;

public sealed class AbsoluteOrbitMathTests
{
    [Theory]
    [InlineData("clockwise", 1)]
    [InlineData("counterclockwise", -1)]
    public void QuarterOrbitMatchesIndependentDerivative(string direction, int sign)
    {
        var orbit = new OrbitalElements(100, 50, 1200000, 0, 0, 12345, direction);
        var state = new ObjectMotionSnapshot("planet", 0, 0, 0, 0);
        var expected = new[] { (0d, -50d, 5 * Math.PI, sign == 1 ? 90d : 270d),
            (sign * 100d, 0d, 2.5 * Math.PI, 180d), (0d, 50d, 5 * Math.PI, sign == 1 ? 270d : 90d),
            (-sign * 100d, 0d, 2.5 * Math.PI, 0d) };
        for (int i = 0; i < 4; i++)
        {
            var actual = OrbitalMotionMath.At(state, orbit, 12345 + i * 1000);
            Assert.Equal(expected[i].Item1, actual.X, 8);
            Assert.Equal(expected[i].Item2, actual.Y, 8);
            Assert.Equal(expected[i].Item3, actual.SpeedKmS, 8);
            Assert.Equal(expected[i].Item4, actual.Direction, 8);
        }
    }

    [Fact]
    public void PartitionAndLargeEpochInvariant()
    {
        var orbit = new OrbitalElements(720000, 720000, 1200000, 17, 0.000001, long.MaxValue - 10000, "clockwise");
        var initial = OrbitalMotionMath.At(new("planet", 0, 0, 0, 0), orbit, orbit.EpochSimulationTimeMs);
        var predictor = new LinearMotionPredictor();
        var full = predictor.Predict(initial, 4001);
        var split = initial;
        foreach (int step in new[] { 1, 3, 997, 3000 }) split = predictor.Predict(split, step);
        Assert.Equal(full, split);
        Assert.Equal(predictor.Predict(initial, 1).X, full.X, 8);
        var coarse = OrbitalMotionMath.At(initial, orbit with { PhaseOffsetDegrees = 0 }, orbit.EpochSimulationTimeMs);
        double distance = Math.Sqrt(Math.Pow(initial.X - coarse.X, 2) + Math.Pow(initial.Y - coarse.Y, 2));
        Assert.InRange(distance, 0.01256636, 0.01256638);
        var offset = OrbitalMotionMath.At(initial with { WorldOffsetX = 1, WorldOffsetY = 1 }, orbit, orbit.EpochSimulationTimeMs);
        Assert.Equal(initial.X + 1, offset.X); Assert.Equal(initial.Y + 1, offset.Y);
        Assert.False(LinearMotionPredictor.IsLinear(initial));
        Assert.False(LinearMotionPredictor.TryPredictLinearPosition(initial, 1, out _, out _));
        // Non-divisible calendar period and time near Int64.MaxValue must not lose the remainder.
        var odd = orbit with { OrbitalPeriodMs = 301, EpochSimulationTimeMs = 0, InitialPhase = 0, PhaseOffsetDegrees = 0 };
        var far = OrbitalMotionMath.At(initial, odd, long.MaxValue);
        var near = OrbitalMotionMath.At(initial, odd, long.MaxValue % 301);
        Assert.Equal(near.X, far.X); Assert.Equal(near.Y, far.Y);
        Assert.Throws<ArgumentOutOfRangeException>(() => OrbitalMotionMath.At(initial, odd with { SemiMajorAxis = double.NaN }, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => OrbitalMotionMath.At(initial, odd with { OrbitalPeriodMs = 0 }, 0));
    }

    [Fact]
    public void LinearAndApproachRegression()
    {
        var predictor = new LinearMotionPredictor();
        var linear = new ObjectMotionSnapshot("ship", 10, 20, 4, 90);
        var result = predictor.Predict(linear, 1000);
        Assert.Equal(50, result.X, 8); Assert.Equal(20, result.Y, 8);
        Assert.True(LinearMotionPredictor.IsLinear(linear));
        var approach = linear with
        {
            ActiveEngineCommandType = NavigationComputerCommandTypes.Approach,
            NavigationTargetX = 1000,
            NavigationTargetY = 20,
            NavigationAngularInertiaDegPerSec = 1
        };
        result = predictor.Predict(approach, 1000);
        Assert.InRange(Math.Sqrt(Math.Pow(result.X - 10, 2) + Math.Pow(result.Y - 20, 2)), 39.99, 40.01);
        Assert.Null(result.Orbit);
    }
}
