using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// Covers <see cref="StationSizeFactors"/> — the data-driven 4 (size) x 3 (nomenclature
/// category) fixed-point StationPriceFactor table (requirements §59, Docs\FirstRelease\
/// TechnicalTasks\StationEconomyProductionAndSizing.md "Размеры станции"). Story-20260825-
/// 084409, Batch 1, Unit 3.
/// </summary>
/// <remarks>
/// Test methods take the size as a plain <c>int</c> InlineData argument (cast to the internal
/// <see cref="StationSize"/> inside the method body) rather than the enum itself — xUnit
/// requires <c>[Theory]</c> methods to be public, and a public method cannot expose an
/// internal-visibility parameter type (CS0051).
/// </remarks>
public class StationSizeFactorsTests
{
    [Theory]
    [InlineData((int)StationSize.Huge, 1200)]
    [InlineData((int)StationSize.Large, 1150)]
    [InlineData((int)StationSize.Medium, 1100)]
    [InlineData((int)StationSize.Outpost, 1000)]
    public void General_resource_factor_matches_documented_table(int size, int expectedFactor)
    {
        Assert.Equal(
            expectedFactor,
            StationSizeFactors.Resolve((StationSize)size, TradeCategory.Resource, isConsumedResource: false));
    }

    [Theory]
    [InlineData((int)StationSize.Huge, 1300)]
    [InlineData((int)StationSize.Large, 1250)]
    [InlineData((int)StationSize.Medium, 1150)]
    [InlineData((int)StationSize.Outpost, 1100)]
    public void Consumed_resource_factor_matches_documented_table(int size, int expectedFactor)
    {
        Assert.Equal(
            expectedFactor,
            StationSizeFactors.Resolve((StationSize)size, TradeCategory.Resource, isConsumedResource: true));
    }

    [Theory]
    [InlineData((int)StationSize.Huge, 1000)]
    [InlineData((int)StationSize.Large, 1100)]
    [InlineData((int)StationSize.Medium, 1150)]
    [InlineData((int)StationSize.Outpost, 1200)]
    public void Good_factor_matches_documented_table(int size, int expectedFactor)
    {
        Assert.Equal(expectedFactor, StationSizeFactors.Resolve((StationSize)size, TradeCategory.Good));
    }

    [Fact]
    public void Resource_defaults_to_general_when_isConsumedResource_is_omitted()
    {
        // Batch 1 has no ConsumedResource concept yet (lands in Batch 2, U7) — every Resource
        // must resolve as "general" by default.
        Assert.Equal(
            StationSizeFactors.Resolve(StationSize.Large, TradeCategory.Resource, isConsumedResource: false),
            StationSizeFactors.Resolve(StationSize.Large, TradeCategory.Resource));
    }

    [Fact]
    public void Large_station_acceptance_criteria_triplet_matches_the_story()
    {
        // §59 acceptance criteria: "Для Large станции Good получает коэффициент 1.10, общий
        // Resource получает 1.15, потребляемый Resource получает 1.25".
        Assert.Equal(1100, StationSizeFactors.Resolve(StationSize.Large, TradeCategory.Good));
        Assert.Equal(1150, StationSizeFactors.Resolve(StationSize.Large, TradeCategory.Resource, false));
        Assert.Equal(1250, StationSizeFactors.Resolve(StationSize.Large, TradeCategory.Resource, true));
    }

    [Fact]
    public void Module_neutral_factor_is_1000_for_every_size()
    {
        // §59: "Для Module коэффициент размера станции пока считается 1.00" — a fixed
        // placeholder, not looked up per-size (Module trading itself is out of scope, Batch 1
        // Protect list).
        Assert.Equal(1000, StationSizeFactors.ModuleNeutralFactor);
    }
}
