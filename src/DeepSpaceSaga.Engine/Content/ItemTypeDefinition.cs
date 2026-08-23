namespace DeepSpaceSaga.Engine.Content;

internal sealed record ItemTypeDefinition(
    string TypeId,
    string DisplayName,
    long UnitMassKg,
    /// <summary>
    /// Base Credits price before a station's PriceCoefficient is applied
    /// (Docs\FirstRelease\Mechanics\StationInventory.md). Null means this item type is not
    /// currently sold/bought by any station — content-only, not a domain invariant.
    /// </summary>
    long? BasePriceCredits = null) : ITypeDefinition;
