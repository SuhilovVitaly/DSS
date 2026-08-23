namespace DeepSpaceSaga.Engine.Content;

/// <summary>
/// Final unit price at a station = round(basePrice x coefficient), where coefficient is
/// fixed-point (1000 = 1.0, see Docs\FirstRelease\Mechanics\StationInventory.md /
/// SpaceObjectRuntime.PriceCoefficient) — no float/double on the authoritative path.
/// </summary>
internal static class StationPricing
{
    internal static long ComputeUnitPriceCredits(long basePriceCredits, int priceCoefficient) =>
        (long)Math.Round(basePriceCredits * (decimal)priceCoefficient / 1000m, MidpointRounding.AwayFromZero);
}
