using DeepSpaceSaga.Engine.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class Mulberry32Tests
{
    [Fact]
    public void GenerationSectionOneControlValuesMatchExactly()
    {
        var rng = new Mulberry32(1u);
        Assert.Equal(0.6270739405881613, rng.Random());
        Assert.Equal(0.002735721180215478, rng.Random());
        Assert.Equal(0.5274470399599522, rng.Random());
        Assert.Equal(3, rng.Calls);
        Assert.Equal(0.8964226141106337, new Mulberry32(uint.MaxValue).Random());
    }

    [Theory]
    [InlineData(-1L, uint.MaxValue)]
    [InlineData(4294967296L, 0u)]
    [InlineData(4294967297L, 1u)]
    [InlineData(long.MinValue, 0u)]
    [InlineData(long.MaxValue, uint.MaxValue)]
    public void LongSeedKeepsLowThirtyTwoBits(long seed, uint reduced)
    {
        var actual = new Mulberry32(seed);
        var expected = new Mulberry32(reduced);
        for (int i = 0; i < 1000; i++) Assert.Equal(expected.Random(), actual.Random());
        Assert.Equal(expected.State, actual.State);
        Assert.Equal(1000, actual.Calls);
    }

    [Fact]
    public void DerivedOperationsUseOneDrawAndSpecifiedArithmeticOrder()
    {
        Assert.Equal(-12 + 24 * 0.6270739405881613, new Mulberry32(1u).Uniform(-12, 12));
        Assert.Equal(50, new Mulberry32(1u).RandInt(40, 55));
        Assert.Equal("c", new Mulberry32(1u).Choice(new[] { "a", "b", "c", "d" }));
        Assert.Equal("second", new Mulberry32(1u).Weighted(new[] { ("first", 1.0), ("second", 1.0) }));
        var rng = new Mulberry32(1u);
        Assert.Equal(7, rng.Uniform(7, 7));
        Assert.Equal(5, rng.RandInt(5, 5));
        Assert.Equal("only", rng.Choice(new[] { "only" }));
        Assert.Equal("last", rng.Weighted(new[] { ("zero", 0.0), ("last", 0.0) }));
        Assert.Equal(4, rng.Calls);
    }

    [Fact]
    public void NpcSkillsAndScatterUseTheIndependentAssemblyTraceOrder()
    {
        using var source = ReferenceFixtureTests.Open("trace-seed-1");
        var units = source.RootElement.GetProperty("data").GetProperty("assembly").GetProperty("units");
        var rng = new Mulberry32(1u);
        for (int i = 4; i < 10; i++)
        {
            Assert.Equal(units[i].GetProperty("accuracy").GetInt32(), rng.RandInt(40, 55));
            Assert.Equal(units[i].GetProperty("maneuver").GetInt32(), rng.RandInt(40, 55));
        }
        // Independently published initial coordinates, Data/README scenario table.
        double[] x = [145, 145, 138, 135, 755, 755, 762, 765, 770, 772];
        double[] y = [228, 275, 212, 288, 172, 125, 188, 112, 205, 95];
        for (int i = 0; i < 10; i++)
        {
            Assert.InRange(Math.Abs(x[i] + rng.Uniform(-12, 12) - units[i].GetProperty("x").GetDouble()), 0, 0.0000005);
            Assert.InRange(Math.Abs(y[i] + rng.Uniform(-12, 12) - units[i].GetProperty("y").GetDouble()), 0, 0.0000005);
            rng.Uniform(-6, 6);
        }
        Assert.Equal(42, rng.Calls);
    }

    [Fact]
    public void InvalidRequestsDoNotConsumeDrawsAndFullIntegerDomainIsSupported()
    {
        var rng = new Mulberry32(1u);
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.RandInt(1, 0));
        Assert.Throws<ArgumentException>(() => rng.Choice(Array.Empty<int>()));
        Assert.Throws<ArgumentException>(() => rng.Weighted(Array.Empty<(int, double)>()));
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.Weighted(new[] { (1, -1.0) }));
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.Weighted(new[] { (1, double.NaN) }));
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.Uniform(0, double.PositiveInfinity));
        Assert.Equal(0, rng.Calls);
        Assert.Equal(545778419, rng.RandInt(int.MinValue, int.MaxValue));
        Assert.Equal(1, rng.Calls);
    }
}
