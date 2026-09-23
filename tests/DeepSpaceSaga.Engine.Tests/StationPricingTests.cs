using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// Covers <see cref="StationPricing.ComputeUnitPriceCredits"/> — final unit price at a station
/// = round(basePrice x Product(applicable StationPriceFactor)), each factor fixed-point
/// (1000 = 1.0), decimal arithmetic only (no float/double on the authoritative path), single
/// final rounding step so factor order never changes the result (requirements §59,
/// Documentation\02-FirstRelease\TechnicalTasks\StationEconomyProductionAndSizing.md "Формула цены").
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

    [Fact]
    public void Final_multiplier_clamp_uses_decimal_before_rounding()
    {
        // EP-0001-US-0015-TK-0002 / Documentation.md:96: final multiplier clamp 0.50..3.00 applies to the exact
        // decimal multiplier (numerator / denominator), then one AwayFromZero round of base × multiplier.

        // 0.4999 is clamped up to 0.50: 3 × 0.50 = 1.5 -> 2 (rounding 3 × 0.4999 = 1.4997 first would give 1).
        Assert.Equal(2, StationPricing.ComputeClampedUnitPriceCredits(3, 4999m, 10000m, 500, 3000, out var floor));
        Assert.Equal(PriceClampKind.Floor, floor);

        // Exactly 0.50 is inside the bound: not reported as clamped, same price.
        Assert.Equal(2, StationPricing.ComputeClampedUnitPriceCredits(3, 1m, 2m, 500, 3000, out var atFloor));
        Assert.Equal(PriceClampKind.None, atFloor);

        // 3.0001 is clamped down to 3.00: 7 × 3.00 = 21 (7 × 3.0001 = 21.0007 is never materialized).
        Assert.Equal(21, StationPricing.ComputeClampedUnitPriceCredits(7, 30001m, 10000m, 500, 3000, out var ceiling));
        Assert.Equal(PriceClampKind.Ceiling, ceiling);

        // Exactly 3.00 via a non-terminating ratio (69000 / 23000): not clamped.
        Assert.Equal(21, StationPricing.ComputeClampedUnitPriceCredits(7, 69000m, 23000m, 500, 3000, out var atCeiling));
        Assert.Equal(PriceClampKind.None, atCeiling);

        // Division happens once, last: 300 × 3700 × 1150 / 3000000 = 425.5 -> 426 (exact midpoint through a
        // 1/3 ratio that an early 28-digit division can push just below .5).
        Assert.Equal(426, StationPricing.ComputeClampedUnitPriceCredits(300, 3700m * 1150m, 3_000_000m, 500, 3000, out var inside));
        Assert.Equal(PriceClampKind.None, inside);

        // Positive base never prices below one credit: 1 × 0.50 = 0.5 -> 1.
        Assert.Equal(1, StationPricing.ComputeClampedUnitPriceCredits(1, 1m, 10m, 500, 3000, out _));

        // The unclamped legacy API keeps its behavior: no clamp, no minimum.
        Assert.Equal(15, StationPricing.ComputeUnitPriceCredits(30, new[] { 500 }));
        Assert.Equal(600, StationPricing.ComputeUnitPriceCredits(100, new[] { 2000, 3000 }));
        Assert.Equal(0, StationPricing.ComputeUnitPriceCredits(0, new[] { 1000 }));
    }
}
