using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion.Tests;

public class CombatRangeMathTests
{
    [Theory]
    [InlineData(2000, 0, 200, true)]
    [InlineData(2000.01, 0, 200, false)]
    [InlineData(300, 400, 50, true)]
    [InlineData(300, 400.01, 50, false)]
    [InlineData(0, 0, 1, true)]
    [InlineData(0, 0, 0, false)]
    [InlineData(0, 0, double.NaN, false)]
    [InlineData(double.PositiveInfinity, 0, 200, false)]
    public void Inclusive_range_matches_world_units(double x, double y, double range, bool expected) =>
        Assert.Equal(expected, CombatRangeMath.IsWithinRange(new("a", 0, 0, 0, 0), new("b", x, y, 0, 0), range));

    [Fact]
    public void Finite_extreme_coordinates_do_not_overflow_distance()
    {
        var a = new ObjectMotionSnapshot("a", -double.MaxValue, -double.MaxValue, 0, 0);
        var b = new ObjectMotionSnapshot("b", double.MaxValue, double.MaxValue, 0, 0);
        Assert.True(CombatRangeMath.IsWithinRange(a, b, double.MaxValue));
        Assert.False(CombatRangeMath.IsWithinRange(a, b, 200));
    }
}
