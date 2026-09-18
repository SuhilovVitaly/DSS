namespace DeepSpaceSaga.Engine.Content;

/// <summary>
/// Trade category of a tradeable <see cref="ItemTypeDefinition"/> (requirements §59,
/// Docs\FirstRelease\TechnicalTasks\StationEconomyProductionAndSizing.md "Номенклатура"):
/// selects which <see cref="StationSizeFactors"/> table applies when resolving a
/// station's <see cref="StationPricing"/> factors. Module is intentionally not a value here —
/// Module trading has no <see cref="ItemTypeDefinition"/> representation at all (out of scope,
/// story-20260825-084409 Batch 1 Protect list).
/// </summary>
internal enum TradeCategory
{
    Resource,
    Good
}

internal enum TradeUnit { Kilogram, Piece, Ration, EnergyCell }

internal enum ItemStorageKind { Cargo, FuelTank }

internal sealed record ItemTypeDefinition(
    string TypeId,
    string DisplayName,
    long UnitMassKg,
    /// <summary>
    /// Base Credits price before station <c>StationPriceFactor</c>s are applied (§59,
    /// Docs\FirstRelease\TechnicalTasks\StationEconomyProductionAndSizing.md "Формула цены").
    /// Null means this item type is not currently sold/bought by any station — content-only,
    /// not a domain invariant.
    /// </summary>
    long? BasePriceCredits = null,
    /// <summary>
    /// Resource vs Good trade category (§59). Defaults to <see cref="TradeCategory.Good"/> for
    /// content/test fixtures that predate this field and never populate it explicitly.
    /// </summary>
    TradeCategory Category = TradeCategory.Good,
    /// <summary>
    /// Optional stable design-document spec id (e.g. "RES-2001", "ITM-3001",
    /// Docs\FirstRelease\TechnicalTasks\StationEconomyProductionAndSizing.md "Формула цены").
    /// Display/traceability only — never a domain key; <see cref="TypeId"/> (kebab-case,
    /// e.g. "item.ice") remains the one stable internal id. Null for item types the design
    /// document never assigned a spec id to (e.g. Food Rations — story-20260825-084409 decision).
    /// </summary>
    string? CatalogCode = null,
    TradeUnit TradeUnit = TradeUnit.Piece,
    ItemStorageKind StorageKind = ItemStorageKind.Cargo,
    long BuyQuantityStep = 1,
    long SellQuantityStep = 1) : ITypeDefinition;
