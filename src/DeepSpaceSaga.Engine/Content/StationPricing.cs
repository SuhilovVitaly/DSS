namespace DeepSpaceSaga.Engine.Content;

/// <summary>Which bound of a final price-multiplier clamp fired, if any.</summary>
internal enum PriceClampKind
{
    None,
    Floor,
    Ceiling,
}

/// <summary>
/// Final unit price at a station (requirements §59, Documentation\02-FirstRelease\TechnicalTasks\
/// StationEconomyProductionAndSizing.md "Формула цены"):
/// <c>unitPriceCredits = RoundToCredits(BasePriceCredits * Product(applicable
/// StationPriceFactor))</c>. Each factor is fixed-point (1000 = 1.0) — no float/double on the
/// authoritative path. Multiplication happens entirely in <c>decimal</c> with a single final
/// rounding step (<see cref="MidpointRounding.AwayFromZero"/>), so the order the caller supplies
/// <paramref name="factors"/> in never changes the result for the same input set (§59: "порядок
/// перемножения факторов не должен менять результат").
/// </summary>
internal static class StationPricing
{
    /// <summary>
    /// <paramref name="factors"/> is an open list so future <c>StationPriceFactor</c> sources
    /// (station events/buffs/debuffs, producing-module effects — story-20260825-084409 Batch 2+)
    /// can be added by the caller without another signature change; an empty list is a valid
    /// "no adjustment yet" input and simply yields <paramref name="basePriceCredits"/> unchanged.
    /// </summary>
    internal static long ComputeUnitPriceCredits(long basePriceCredits, IReadOnlyList<int> factors) =>
        RoundToCredits(basePriceCredits * FactorProduct(factors));

    /// <summary>
    /// Exact decimal product of fixed-point factors (1000 = 1.0); an empty list is 1. Shared by the static
    /// station price above and the sequential quote curve (EP-0001-US-0015-TK-0002).
    /// </summary>
    internal static decimal FactorProduct(IReadOnlyList<int> factorsPermille)
    {
        decimal product = 1m;
        for (int i = 0; i < factorsPermille.Count; i++)
            product *= factorsPermille[i] / 1000m;

        return product;
    }

    /// <summary>
    /// Clamped unit price (EP-0001-US-0015-TK-0002; epic baseline Documentation.md:96, final bound 0.50..3.00):
    /// the raw multiplier is the exact ratio <paramref name="multiplierNumerator"/> /
    /// <paramref name="multiplierDenominator"/> (both positive). It is compared with the permille bounds without
    /// dividing, then <c>base × multiplier</c> is divided once and rounded once
    /// (<see cref="MidpointRounding.AwayFromZero"/>) into a checked <see cref="long"/>. A positive base never
    /// prices below one credit. Out-of-range values throw <see cref="OverflowException"/>.
    /// </summary>
    internal static long ComputeClampedUnitPriceCredits(
        long basePriceCredits,
        decimal multiplierNumerator,
        decimal multiplierDenominator,
        int minMultiplierPermille,
        int maxMultiplierPermille,
        out PriceClampKind clamp)
    {
        decimal amount;
        if (multiplierNumerator * 1000m < minMultiplierPermille * multiplierDenominator)
        {
            clamp = PriceClampKind.Floor;
            amount = basePriceCredits * (minMultiplierPermille / 1000m);
        }
        else if (multiplierNumerator * 1000m > maxMultiplierPermille * multiplierDenominator)
        {
            clamp = PriceClampKind.Ceiling;
            amount = basePriceCredits * (maxMultiplierPermille / 1000m);
        }
        else
        {
            clamp = PriceClampKind.None;
            amount = basePriceCredits * multiplierNumerator / multiplierDenominator;
        }

        long price = RoundToCredits(amount);
        return basePriceCredits > 0 && price < 1 ? 1 : price;
    }

    // Explicit decimal -> long conversion throws OverflowException when out of range.
    private static long RoundToCredits(decimal amount) => (long)Math.Round(amount, MidpointRounding.AwayFromZero);
}
