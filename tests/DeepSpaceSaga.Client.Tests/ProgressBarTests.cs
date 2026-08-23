using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// The flat horizontal fill bar (Trade screen's CARGO LOAD / FUEL TANK indicators).
/// ClampFraction/ComputeFillWidth are a public contract consumed by TradeScreen (Batch 6);
/// Draw is a smoke test only, matching ImageButtonTests.Draw_does_not_throw_for_any_button_state.
/// </summary>
public class ProgressBarTests
{
    [Theory]
    [InlineData(0.5, 0.5)]
    [InlineData(-0.5, 0.0)]
    [InlineData(1.5, 1.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(1.0, 1.0)]
    public void ClampFraction_clamps_to_0_1(double input, double expected)
    {
        Assert.Equal(expected, ProgressBar.ClampFraction(input));
    }

    [Fact]
    public void ComputeFillWidth_scales_by_fraction()
    {
        Assert.Equal(50f, ProgressBar.ComputeFillWidth(100f, 0.5));
    }

    [Fact]
    public void ComputeFillWidth_clamps_fraction_above_one()
    {
        Assert.Equal(100f, ProgressBar.ComputeFillWidth(100f, 1.5));
    }

    [Fact]
    public void ComputeFillWidth_clamps_fraction_below_zero()
    {
        Assert.Equal(0f, ProgressBar.ComputeFillWidth(100f, -0.5));
    }

    [Fact]
    public void ComputeFillWidth_handles_zero_width_track()
    {
        Assert.Equal(0f, ProgressBar.ComputeFillWidth(0f, 0.5));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(-0.5)]
    [InlineData(1.5)]
    public void Draw_does_not_throw_for_any_fraction(double fraction)
    {
        using var bitmap = new SKBitmap(200, 24);
        using var canvas = new SKCanvas(bitmap);
        var rect = new SKRect(10, 10, 190, 22);

        ProgressBar.Draw(canvas, rect, fraction);
        ProgressBar.Draw(canvas, rect, fraction, SKColors.Blue);
    }
}
