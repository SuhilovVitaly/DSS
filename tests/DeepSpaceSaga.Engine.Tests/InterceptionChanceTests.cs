using DeepSpaceSaga.Engine.Combat;
namespace DeepSpaceSaga.Engine.Tests;

public class InterceptionChanceTests
{
    [Theory]
    [InlineData(30, 30, 500)]
    [InlineData(30, 48, 320)]
    [InlineData(0, 60, 0)]
    [InlineData(60, 0, 1000)]
    [InlineData(30.05, 30, 501)]
    public void Chance_uses_unrounded_difference_and_away_from_zero(decimal defense, decimal torpedo, int expected)
        => Assert.Equal(expected, InterceptionMath.ChanceTenths(defense, torpedo));
    [Fact]
    public void Decimal_extremes_clamp_without_overflow()
    {
        Assert.Equal(1000, InterceptionMath.ChanceTenths(decimal.MaxValue, 0));
        Assert.Equal(0, InterceptionMath.ChanceTenths(0, decimal.MaxValue));
    }
    [Fact]
    public void Named_stream_golden_vectors_and_resume()
    {
        var rng = new CountermeasureRng(42);
        int[] golden = [801, 820, 781, 237, 617, 381, 400, 470];
        Assert.Equal(golden.Take(4), Enumerable.Range(0, 4).Select(_ => rng.NextRoll()));
        Assert.Equal(4UL, rng.Counter);
        var resumed = new CountermeasureRng(rng.State, rng.Counter);
        Assert.Equal(golden.Skip(4), Enumerable.Range(0, 4).Select(_ => resumed.NextRoll()));
        Assert.Equal(8UL, resumed.Counter);
    }
}
