using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Motion.Tests;

public sealed class PointCombatUS0003TK0001Tests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(2.9, 1)]
    [InlineData(3, 1)]
    [InlineData(4, 6d / 7)]
    [InlineData(5, 5d / 7)]
    [InlineData(9, 1d / 7)]
    [InlineData(3.7, 0.9)]
    [InlineData(4.4, 0.8)]
    [InlineData(5.1, 0.7)]
    [InlineData(5.8, 0.6)]
    [InlineData(6.5, 0.5)]
    [InlineData(7.2, 0.4)]
    [InlineData(7.9, 0.3)]
    [InlineData(8.6, 0.2)]
    [InlineData(9.3, 0.1)]
    [InlineData(10, 0)]
    [InlineData(11, 0)]
    public void Falloff_has_core_linear_edge_and_zero_outside(double distance, double expected) =>
        Assert.Equal(expected, BlastMath.Falloff(distance, 10, 0.3), 12);

    [Theory]
    [InlineData(0, 1.4995)]
    [InlineData(5, 1.333)]
    [InlineData(10, 1.1665)]
    [InlineData(15, 1)]
    [InlineData(20, 0.8335)]
    [InlineData(25, 0.667)]
    [InlineData(30, 0.5005)]
    public void Scatter_sigma_uses_fraction_and_skill(int skill, double expected) =>
        Assert.Equal(expected, BlastMath.ScatterSigma(50, 0.02, skill), 12);

    [Fact]
    public void Scatter_uses_configured_coefficients_and_minimum()
    {
        Assert.Equal(0.6, BlastMath.ScatterSigma(50, 0.04, 30, 15, 0.1, 0.3), 12);
        Assert.Equal(2, BlastMath.ScatterSigma(50, 0.04, 30, 15, 0, 0.3), 12);
        Assert.Equal(0, BlastMath.ScatterSigma(50, 0, 15));
        Assert.Equal(0, BlastMath.ScatterSigma(0, 0.02, 15));
        Assert.Equal(4, BlastMath.ScatterSigma(50, 0.08, 15));
    }

    [Fact]
    public void Position_and_deadline_are_linear()
    {
        Assert.Equal(10000, BlastMath.DetonationTimeMs(0, 60, 6));
        Assert.Equal(10123, BlastMath.DetonationTimeMs(123, 60, 6));
        Assert.Equal(5000, BlastMath.DetonationTimeMs(1000, 40, 10));
        Assert.Equal(18.75, BlastMath.DetonationTimeMs(0, 0.3, 16));
        Assert.Equal(321, BlastMath.DetonationTimeMs(321, 0, 16));
        Assert.Equal((30d, 0d), BlastMath.PositionAt(0, 0, 60, 0, 0.5));
        Assert.Equal((0d, 0d), BlastMath.PositionAt(0, 0, 600, 0, 0));
        Assert.Equal((300d, 0d), BlastMath.PositionAt(0, 0, 600, 0, 0.5));
        Assert.Equal((600d, 0d), BlastMath.PositionAt(0, 0, 600, 0, 1));
        Assert.Equal((-2d, 3d), BlastMath.PositionAt(-2, 3, 10, -5, -1));
        Assert.Equal((1d, 1d), BlastMath.PositionAt(-2, 3, 10, -5, 0.25));
        Assert.Equal((10d, -5d), BlastMath.PositionAt(-2, 3, 10, -5, 2));
        Assert.Equal((0d, 0d), BlastMath.PositionAt(-double.MaxValue, 0, double.MaxValue, 0, 0.5));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-1)]
    public void Invalid_distances_do_not_produce_simulation_values(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BlastMath.Falloff(value, 10, 0.3));
        Assert.Throws<ArgumentOutOfRangeException>(() => BlastMath.ScatterSigma(value, 0.02, 15));
        Assert.Throws<ArgumentOutOfRangeException>(() => BlastMath.DetonationTimeMs(0, value, 6));
    }

    [Fact]
    public void Invalid_rules_and_overflow_fail_explicitly()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BlastMath.Falloff(0, 0, 0.3));
        Assert.Throws<ArgumentOutOfRangeException>(() => BlastMath.Falloff(0, 10, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => BlastMath.DetonationTimeMs(0, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => BlastMath.DetonationTimeMs(0, double.MaxValue, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => BlastMath.ScatterSigma(1, 0.02, 31));
        Assert.Throws<ArgumentOutOfRangeException>(() => BlastMath.ScatterSigma(1, -1, 15));
        Assert.Throws<ArgumentOutOfRangeException>(() => BlastMath.ScatterSigma(1, 0.02, 15, 15, 0.1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => BlastMath.PositionAt(0, 0, 1, 1, double.NaN));
    }
}
