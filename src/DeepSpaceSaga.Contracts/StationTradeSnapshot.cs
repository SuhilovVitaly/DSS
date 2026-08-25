using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Docked station's tradeable inventory, published only while the player ship is
/// actually docked (see <see cref="AuthoritativeSnapshot.DockedStationTrade"/>).
/// </summary>
public sealed record StationTradeSnapshot(
    string StationObjectId,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<StationInventoryItemSnapshot>))]
    ImmutableArray<StationInventoryItemSnapshot> Items = default);

/// <summary>
/// One tradeable item on a docked station. The station's own Credits balance is never
/// serialized here (Docs\FirstRelease\Mechanics\Money.md — hidden from the player);
/// <see cref="MaxSellableQuantity"/> is the only way it influences the client, and it
/// bounds SELLING to the station (the direction the station's hidden balance actually
/// limits), not buying — buying from the station is bounded by the player's own
/// Credits/cargo capacity, which the client already has and can compute itself.
/// </summary>
public sealed record StationInventoryItemSnapshot(
    string ItemTypeId,
    long StockQuantity,
    long UnitPriceCredits,
    long MaxSellableQuantity,
    /// <summary>
    /// One of <see cref="TradeItemCategories"/> (Resource/Good) — mirrors the item type's
    /// Engine-internal trade category (DeepSpaceSaga.Engine.Content.TradeCategory, §59
    /// StationEconomyProductionAndSizing.md) without exposing that internal enum across the
    /// assembly boundary. Drives the client's Buy/Sell quantity stepper package-size step
    /// (Resource=100, Good — including Fuel — =10; see TradeScreen.ResolveQuantityStep) — the
    /// client must read this field rather than re-deriving category from the item id itself.
    /// Defaults to <see cref="TradeItemCategories.Good"/> for callers/fixtures that predate
    /// this field (story-20260825-084409 Batch 3, U10).
    /// </summary>
    string Category = TradeItemCategories.Good);

/// <summary>
/// String values for <see cref="StationInventoryItemSnapshot.Category"/> — a string mirror of
/// the Engine-internal <c>DeepSpaceSaga.Engine.Content.TradeCategory</c> enum (§59), kept as a
/// string here (rather than a duplicate Contracts enum of the same name) to avoid an
/// unqualified-name collision with that internal Engine type inside SimulationEngine.cs, which
/// imports both the Contracts and Engine.Content namespaces.
/// </summary>
public static class TradeItemCategories
{
    public const string Resource = "Resource";
    public const string Good = "Good";
}
