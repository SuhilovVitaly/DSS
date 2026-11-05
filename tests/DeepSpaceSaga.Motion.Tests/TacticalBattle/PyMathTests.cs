using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Motion.Tests.TacticalBattle;

public sealed class PyMathTests
{
    [Theory]
    [InlineData(0L, long.MinValue)]
    [InlineData(long.MinValue, 0L)]
    public void EqualSignedZerosKeepFirst(long firstBits, long secondBits)
    {
        double first = BitConverter.Int64BitsToDouble(firstBits), second = BitConverter.Int64BitsToDouble(secondBits);
        Assert.Equal(firstBits, BitConverter.DoubleToInt64Bits(PyMath.Min(first, second)));
        Assert.Equal(firstBits, BitConverter.DoubleToInt64Bits(PyMath.Max(first, second)));
        Assert.Equal(firstBits, BitConverter.DoubleToInt64Bits(PyMath.Clamp(first, second, second)));
    }

    [Fact]
    public void ClampUsesBranchesEvenForReversedBoundsAndNaN()
    {
        Assert.Equal(10, PyMath.Clamp(5, 10, 0));
        Assert.Equal(0, PyMath.Clamp(15, 10, 0));
        Assert.Equal(0, PyMath.Clamp(10, 10, 0));
    }

    [Fact]
    public void StrictComparisonsPreserveUnorderedFirstAndDoNotReplaceWithNaN()
    {
        double nan = BitConverter.Int64BitsToDouble(unchecked((long)0x7ff8000000000042));
        Assert.Equal(BitConverter.DoubleToInt64Bits(nan), BitConverter.DoubleToInt64Bits(PyMath.Min(nan, 1)));
        Assert.Equal(BitConverter.DoubleToInt64Bits(nan), BitConverter.DoubleToInt64Bits(PyMath.Max(nan, 1)));
        Assert.Equal(1, PyMath.Min(1, nan));
        Assert.Equal(1, PyMath.Max(1, nan));
        Assert.True(double.IsNaN(PyMath.Clamp(nan, 0, 1)));
        Assert.Equal(1, PyMath.Clamp(1, nan, nan));
        Assert.Equal(-4, PyMath.Min(3, -4));
        Assert.Equal(3, PyMath.Max(3, -4));
        Assert.Equal(0, PyMath.Clamp(-1, 0, 2));
        Assert.Equal(2, PyMath.Clamp(3, 0, 2));
        Assert.Equal(1, PyMath.Clamp(1, 0, 2));
    }

    [Theory]
    [InlineData(-7.5, 4, -3.5)]
    [InlineData(7.5, -4, 3.5)]
    [InlineData(-7.5, -4, -3.5)]
    public void RemainderTruncatesQuotientRatherThanRounding(double value, double divisor, double expected) => Assert.Equal(expected, PyMath.Fmod(value, divisor));

    [Fact]
    public void RemainderAndFloorPreserveZeroAndFloorTruncatesNonnegativeValues()
    {
        Assert.Equal(long.MinValue, BitConverter.DoubleToInt64Bits(PyMath.Fmod(-4, 2)));
        Assert.Equal(long.MinValue, BitConverter.DoubleToInt64Bits(PyMath.FloorNonNeg(-0.0)));
        Assert.Equal(3, PyMath.FloorNonNeg(3.999));
        Assert.Equal(0, PyMath.FloorNonNeg(double.Epsilon));
        Assert.Equal(double.MaxValue, PyMath.FloorNonNeg(double.MaxValue));
        foreach (double invalid in new[] { -1, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => PyMath.FloorNonNeg(invalid));
    }

    [Fact]
    public void MinimumKeepsFirstTieAndEvaluatesEachKeyOnceInOrder()
    {
        var items = new[] { new Item("first", 2), new Item("winner", 1), new Item("tie", 1) };
        var calls = new List<string>();
        Assert.Same(items[1], PyMath.MinByFirst(items, item => { calls.Add(item.Id); return item.Key; }));
        Assert.Equal(new[] { "first", "winner", "tie" }, calls);
        Assert.Throws<InvalidOperationException>(() => PyMath.MinByFirst(Array.Empty<Item>(), item => item.Key));
        Assert.Equal("nan", PyMath.MinByFirst(new[] { new Item("nan", double.NaN), new Item("finite", 0) }, i => i.Key).Id);
    }

    [Fact]
    public void UniqueRetainsFirstInstancesAndExplicitStringComparison()
    {
        Assert.Equal(new[] { "b", "a", "B" }, PyMath.OrderedUnique(new[] { "b", "a", "b", "B" }, StringComparer.Ordinal));
        Assert.Equal(new[] { "b", "a" }, PyMath.OrderedUnique(new[] { "b", "a", "B", "A" }, StringComparer.OrdinalIgnoreCase));
        Assert.Empty(PyMath.OrderedUnique(Array.Empty<int>()));
        Assert.Equal(new string?[] { null, "a" }, PyMath.OrderedUnique(new string?[] { null, "a", null }));
    }

    private sealed record Item(string Id, double Key);
}
