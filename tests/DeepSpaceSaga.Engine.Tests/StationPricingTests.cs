using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// Covers <see cref="StationPricing.ComputeUnitPriceCredits"/> — final unit price at a station
/// = round(basePrice x coefficient), coefficient fixed-point (1000 = 1.0), decimal arithmetic
/// only (no float/double on the authoritative path). Story-20260822-193700, Batch 3, Unit 3.1.
/// </summary>
public class StationPricingTests
{
    [Fact]
    public void Neutral_coefficient_returns_base_price_unchanged()
    {
        Assert.Equal(200, StationPricing.ComputeUnitPriceCredits(200, 1000));
    }

    [Fact]
    public void Coefficient_above_neutral_scales_price_up()
    {
        Assert.Equal(300, StationPricing.ComputeUnitPriceCredits(200, 1500));
    }

    [Fact]
    public void Coefficient_below_neutral_scales_price_down()
    {
        Assert.Equal(15, StationPricing.ComputeUnitPriceCredits(30, 500));
    }

    [Fact]
    public void Fractional_result_rounds_half_away_from_zero()
    {
        // 85 * 1234 / 1000 = 104.89 -> rounds up to 105.
        Assert.Equal(105, StationPricing.ComputeUnitPriceCredits(85, 1234));
    }

    [Fact]
    public void Exact_half_rounds_away_from_zero()
    {
        // 1 * 1500 / 1000 = 1.5 -> rounds up to 2 (AwayFromZero, not banker's rounding).
        Assert.Equal(2, StationPricing.ComputeUnitPriceCredits(1, 1500));
    }
}
