using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// Covers <see cref="StationPricing.ComputeUnitPriceCredits"/> — final unit price at a station
/// = round(basePrice x Product(applicable StationPriceFactor)), each factor fixed-point
/// (1000 = 1.0), decimal arithmetic only (no float/double on the authoritative path), single
/// final rounding step so factor order never changes the result (requirements §59,
/// Docs\FirstRelease\TechnicalTasks\StationEconomyProductionAndSizing.md "Формула цены").
/// Story-20260825-084409, Batch 1, Unit 4 — generalizes the story-20260822-193700 Batch 3
/// single-coefficient version to an arbitrary list of factors.
/// </summary>
public class StationPricingTests
{
    [Fact]
    public void Empty_factor_list_returns_base_price_unchanged()
    {
        Assert.Equal(200, StationPricing.ComputeUnitPriceCredits(200, Array.Empty<int>()));
    }

    [Fact]
    public void Single_neutral_factor_returns_base_price_unchanged()
    {
        Assert.Equal(200, StationPricing.ComputeUnitPriceCredits(200, new[] { 1000 }));
    }

    [Fact]
    public void Single_factor_above_neutral_scales_price_up()
    {
        Assert.Equal(300, StationPricing.ComputeUnitPriceCredits(200, new[] { 1500 }));
    }

    [Fact]
    public void Single_factor_below_neutral_scales_price_down()
    {
        Assert.Equal(15, StationPricing.ComputeUnitPriceCredits(30, new[] { 500 }));
    }

    [Fact]
    public void Fractional_result_rounds_half_away_from_zero()
    {
        // 85 * 1234 / 1000 = 104.89 -> rounds up to 105.
        Assert.Equal(105, StationPricing.ComputeUnitPriceCredits(85, new[] { 1234 }));
    }

    [Fact]
    public void Exact_half_rounds_away_from_zero()
    {
        // 1 * 1500 / 1000 = 1.5 -> rounds up to 2 (AwayFromZero, not banker's rounding).
        Assert.Equal(2, StationPricing.ComputeUnitPriceCredits(1, new[] { 1500 }));
    }

    [Fact]
    public void Multiple_factors_multiply_together_before_the_single_final_round()
    {
        // §59 example: Large station, Good => 1.10; here combined with a second 1.05 factor.
        // 200 * 1.10 * 1.05 = 231.0 exactly.
        Assert.Equal(231, StationPricing.ComputeUnitPriceCredits(200, new[] { 1100, 1050 }));
    }

    [Fact]
    public void Large_station_good_factor_matches_acceptance_criteria_example()
    {
        // §59 acceptance criteria: "Для Large станции Good получает коэффициент 1.10".
        Assert.Equal(220, StationPricing.ComputeUnitPriceCredits(200, new[] { 1100 }));
    }

    [Theory]
    [InlineData(1150, 1300, 900)]
    [InlineData(900, 1150, 1300)]
    [InlineData(1300, 900, 1150)]
    public void Factor_order_does_not_affect_the_result(int f1, int f2, int f3)
    {
        // §59: "порядок перемножения факторов не должен менять результат для одинакового
        // набора входных данных" — the baseline is computed with a fixed order and every
        // permutation supplied by the Theory must match it exactly.
        long baseline = StationPricing.ComputeUnitPriceCredits(1000, new[] { 1150, 1300, 900 });

        Assert.Equal(baseline, StationPricing.ComputeUnitPriceCredits(1000, new[] { f1, f2, f3 }));
    }
}
