namespace DeepSpaceSaga.Engine.Content;

/// <summary>
/// Final unit price at a station (requirements §59, Docs\FirstRelease\TechnicalTasks\
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
    internal static long ComputeUnitPriceCredits(long basePriceCredits, IReadOnlyList<int> factors)
    {
        decimal product = 1m;
        for (int i = 0; i < factors.Count; i++)
            product *= factors[i] / 1000m;

        return (long)Math.Round(basePriceCredits * product, MidpointRounding.AwayFromZero);
    }
}
