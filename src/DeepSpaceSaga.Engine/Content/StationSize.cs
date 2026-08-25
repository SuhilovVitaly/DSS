namespace DeepSpaceSaga.Engine.Content;

/// <summary>
/// Station size classification (requirements §59, Docs\FirstRelease\TechnicalTasks\
/// StationEconomyProductionAndSizing.md "Размеры станции"). Drives the per-trade-category
/// <c>StationPriceFactor</c> multiplier resolved by <see cref="StationSizeFactors"/> and
/// consumed by <see cref="StationPricing.ComputeUnitPriceCredits"/>.
/// </summary>
/// <remarks>
/// Not yet persisted on <see cref="Scenario.SpaceObjectData"/>/<c>SpaceObjectRuntime</c> — that
/// is story-20260825-084409 Batch 2 (U5). Batch 1 introduces only the enum and the data-driven
/// coefficient table (U3); the two existing <c>StationPricing</c> call sites in
/// <c>SimulationEngine</c> pass an empty factor list until Batch 2 wires a real resolved
/// per-station <see cref="StationSize"/> through.
/// </remarks>
internal enum StationSize
{
    Huge,
    Large,
    Medium,
    Outpost
}

/// <summary>
/// Data-driven <c>StationPriceFactor</c> lookup table for station size (§59 "Коэффициенты
/// общих ресурсов" / "Коэффициенты потребляемых ресурсов" / "Коэффициенты товаров") — 4 sizes x
/// 3 nomenclature categories (general Resource, consumed Resource, Good) = 12 fixed-point
/// coefficients (int, 1000 == 1.0x; no float/double on the authoritative path, per project
/// convention).
/// </summary>
internal static class StationSizeFactors
{
    /// <summary>
    /// Module's station-size price factor is a fixed 1.00 (1000) placeholder for every
    /// <see cref="StationSize"/> (§59: "Для Module коэффициент размера станции пока считается
    /// 1.00, если отдельное решение по модулям не принято"). Module trading itself is out of
    /// scope (Batch 1 Protect list) — this constant exists only so a future Module-pricing unit
    /// does not need to re-derive the placeholder value.
    /// </summary>
    internal const int ModuleNeutralFactor = 1000;

    private static readonly IReadOnlyDictionary<StationSize, int> GeneralResource = new Dictionary<StationSize, int>
    {
        [StationSize.Huge] = 1200,
        [StationSize.Large] = 1150,
        [StationSize.Medium] = 1100,
        [StationSize.Outpost] = 1000,
    };

    private static readonly IReadOnlyDictionary<StationSize, int> ConsumedResource = new Dictionary<StationSize, int>
    {
        [StationSize.Huge] = 1300,
        [StationSize.Large] = 1250,
        [StationSize.Medium] = 1150,
        [StationSize.Outpost] = 1100,
    };

    private static readonly IReadOnlyDictionary<StationSize, int> Good = new Dictionary<StationSize, int>
    {
        [StationSize.Huge] = 1000,
        [StationSize.Large] = 1100,
        [StationSize.Medium] = 1150,
        [StationSize.Outpost] = 1200,
    };

    /// <summary>
    /// Resolve the fixed-point size factor for a nomenclature category at a given station size.
    /// <paramref name="isConsumedResource"/> is only meaningful for
    /// <see cref="TradeCategory.Resource"/> (§59: a Resource is "consumed" when at least one of
    /// the station's producing modules needs it as input) and defaults to <c>false</c> because
    /// Batch 1 has no notion of producing modules/ConsumedResource yet (that lands in Batch 2,
    /// U7) — every Resource resolves as "general" until then.
    /// </summary>
    internal static int Resolve(StationSize size, TradeCategory category, bool isConsumedResource = false) =>
        category switch
        {
            TradeCategory.Good => Good[size],
            TradeCategory.Resource => isConsumedResource ? ConsumedResource[size] : GeneralResource[size],
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown TradeCategory.")
        };
}
