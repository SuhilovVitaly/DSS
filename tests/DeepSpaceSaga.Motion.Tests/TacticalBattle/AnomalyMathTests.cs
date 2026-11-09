using System.Collections.Immutable;
using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Motion.Tests.TacticalBattle;

public sealed class AnomalyMathTests
{
    private static ImmutableArray<TacticalAnomaly> One(double rotation = 0) =>
        [new("heat", TacticalAnomalyKind.Heat, [new(0, 0, 100, 2, 1, rotation)])];

    [Fact]
    public void Field_of_single_clump()
    {
        Assert.Equal(200, TacticalAnomalyMath.Field(One(), TacticalAnomalyKind.Heat, 0, 0, 100));
        Assert.Equal(160.65306597126334, TacticalAnomalyMath.Field(One(), TacticalAnomalyKind.Heat, 2, 0, 100), 12);
        Assert.Equal(113.53352832366127, TacticalAnomalyMath.Field(One(Math.PI / 2), TacticalAnomalyKind.Heat, 2, 0, 100), 12);
        Assert.Equal(160.65306597126334, TacticalAnomalyMath.Field(One(Math.PI / 2), TacticalAnomalyKind.Heat, 0, 2, 100), 12);
        Assert.Equal(100, TacticalAnomalyMath.Field(One(), TacticalAnomalyKind.Dust, 0, 0, 100));
        Assert.Equal(17, TacticalAnomalyMath.Field([], TacticalAnomalyKind.Heat, 0, 0, 17));
        var overlapping = One().Add(new("other", TacticalAnomalyKind.Heat, [new(0, 0, 50, 1, 1, 0)]));
        Assert.Equal(250, TacticalAnomalyMath.Field(overlapping, TacticalAnomalyKind.Heat, 0, 0, 100));
    }

    [Fact]
    public void Inside_uses_zone_edge()
    {
        Assert.True(TacticalAnomalyMath.Inside(One(), TacticalAnomalyKind.Heat, 0, 0, 100, 200));
        Assert.False(TacticalAnomalyMath.Inside(One(), TacticalAnomalyKind.Heat, 0, 0, 100, 200.000001));
        Assert.False(TacticalAnomalyMath.Inside(One(), TacticalAnomalyKind.Dust, 0, 0, 100, 170));
    }

    [Fact]
    public void Absorption_is_share_of_samples_inside_heat()
    {
        var field = One();
        Assert.Equal(1, TacticalAnomalyMath.Absorption(field, new(-.1, 0, 0), new(.1, 0, 0), 100, 170, 200));
        Assert.Equal(0, TacticalAnomalyMath.Absorption(field, new(100, 0, 0), new(101, 0, 0), 100, 170, 200));
        Assert.Equal(.5, TacticalAnomalyMath.Absorption(field, new(-4, 0, 0), new(4, 0, 0), 100, 160.65306597126334, 200));
        Assert.Equal(.5, TacticalAnomalyMath.Absorption(field, new(4, 0, 0), new(-4, 0, 0), 100, 160.65306597126334, 200));
        Assert.Equal(1, TacticalAnomalyMath.Absorption(field, new(0, 0, 0), new(0, 0, 0), 100, 170, 200));
        Assert.Equal(0, TacticalAnomalyMath.Absorption(field, new(100, 0, 0), new(100, 0, 0), 100, 170, 200));
        Assert.Equal(0, TacticalAnomalyMath.Absorption([], new(0, 0, 0), new(1, 0, 0), 100, 170, 200));
        ImmutableArray<TacticalAnomaly> dust = [new("dust", TacticalAnomalyKind.Dust, [new(0, 0, 100, 10, 10, 0)])];
        Assert.Equal(0, TacticalAnomalyMath.Absorption(dust, new(0, 0, 0), new(1, 0, 0), 100, 170, 200));
    }

    [Fact]
    public void Segments_merge_adjacent_samples()
    {
        var segments = TacticalAnomalyMath.Segments(One(), new(-4, 0, 0), new(4, 0, 0), 100, 160.65306597126334, 200);
        Assert.Equal(new TacticalAnomalySegment(.25, .75), Assert.Single(segments));
        ImmutableArray<TacticalAnomaly> two = [new("two", TacticalAnomalyKind.Heat,
            [new(-2, 0, 100, .2, .2, 0), new(2, 0, 100, .2, .2, 0)])];
        var separate = TacticalAnomalyMath.Segments(two, new(-3, 0, 0), new(3, 0, 0), 100, 160.65306597126334, 600);
        Assert.Equal(2, separate.Length);
        Assert.InRange(Math.Abs(separate[0].T0 - 2.0 / 15), 0, 1.0 / 600);
        Assert.InRange(Math.Abs(separate[0].T1 - .2), 0, 1.0 / 600);
        Assert.InRange(Math.Abs(separate[1].T0 - .8), 0, 1.0 / 600);
        Assert.InRange(Math.Abs(separate[1].T1 - 13.0 / 15), 0, 1.0 / 600);
        Assert.Empty(TacticalAnomalyMath.Segments([], new(0, 0, 0), new(1, 0, 0), 100, 170, 200));
    }

    [Fact]
    public void Gradient_matches_finite_difference()
    {
        double value = TacticalAnomalyMath.FieldWithGradient(One(), TacticalAnomalyKind.Heat, 2, 0, 100, out double x, out double y);
        Assert.Equal(160.65306597126334, value, 12);
        Assert.Equal(-30.32653298563167, x, 11); Assert.Equal(0, y);
        var field = One(.37); const double step = .00001;
        value = TacticalAnomalyMath.FieldWithGradient(field, TacticalAnomalyKind.Heat, .7, -.2, 100, out x, out y);
        double dx = (TacticalAnomalyMath.Field(field, TacticalAnomalyKind.Heat, .7 + step, -.2, 100)
            - TacticalAnomalyMath.Field(field, TacticalAnomalyKind.Heat, .7 - step, -.2, 100)) / (2 * step);
        double dy = (TacticalAnomalyMath.Field(field, TacticalAnomalyKind.Heat, .7, -.2 + step, 100)
            - TacticalAnomalyMath.Field(field, TacticalAnomalyKind.Heat, .7, -.2 - step, 100)) / (2 * step);
        Assert.Equal(dx, x, 6); Assert.Equal(dy, y, 6);
        Assert.Equal(TacticalAnomalyMath.Field(field, TacticalAnomalyKind.Heat, .7, -.2, 100), value);
        TacticalAnomalyMath.FieldWithGradient(field, TacticalAnomalyKind.Heat, 1e200, 1e200, 100, out x, out y);
        Assert.Equal(0, x); Assert.Equal(0, y);
    }

    [Theory]
    [InlineData(1e-200)]
    [InlineData(1e200)]
    public void Sigma_squares_do_not_underflow_or_overflow(double sigma)
    {
        ImmutableArray<TacticalAnomaly> field = [new("scale", TacticalAnomalyKind.Heat, [new(0, 0, 100, sigma, sigma, 0)])];
        Assert.Equal(200, TacticalAnomalyMath.Field(field, TacticalAnomalyKind.Heat, 0, 0, 100));
        Assert.Equal(160.65306597126334, TacticalAnomalyMath.Field(field, TacticalAnomalyKind.Heat, sigma, 0, 100), 12);
    }

    [Fact]
    public void Invalid_sampling_or_sigma_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TacticalAnomalyMath.Absorption([], default, default, 100, 170, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => TacticalAnomalyMath.Segments([], default, default, 100, 170, 0));
        ImmutableArray<TacticalAnomaly> invalid = [new("bad", TacticalAnomalyKind.Heat, [new(0, 0, 100, 0, 1, 0)])];
        Assert.Throws<ArgumentOutOfRangeException>(() => TacticalAnomalyMath.Field(invalid, TacticalAnomalyKind.Heat, 0, 0, 100));
    }
}
