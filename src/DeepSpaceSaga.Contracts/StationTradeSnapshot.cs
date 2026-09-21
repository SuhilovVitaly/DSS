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
/// serialized here (Documentation\02-FirstRelease\Mechanics\Money.md — hidden from the player);
/// <see cref="MaxSellableQuantity"/> is the only way it influences the client, and it
/// bounds SELLING to the station (the direction the station's hidden balance actually
/// limits), not buying — buying from the station is bounded by the player's own
/// Credits/cargo capacity, which the client already has and can compute itself.
/// </summary>
public sealed record StationInventoryItemSnapshot(
    string ItemTypeId,
    long StockQuantity,
    long UnitPriceCredits,
    /// <summary>
    /// Upper bound on how many units the player may SELL to this station in one trade.
    /// It is the Engine-side minimum of what the station's hidden purchasing budget can afford
    /// and, for a bounded-economy cargo row, the remaining <see cref="FreeStockCapacity"/> —
    /// so the client can enforce both the money and the storage limit without ever learning the
    /// station's Credits or its market budget cap (neither is serialized here).
    /// </summary>
    long MaxSellableQuantity,
    /// <summary>
    /// One of <see cref="TradeItemCategories"/> (Resource/Good) — mirrors the item type's
    /// Engine-internal trade category (DeepSpaceSaga.Engine.Content.TradeCategory) without
    /// exposing that internal enum across the assembly boundary. Buy/Sell quantity is fully
    /// per-unit for every category (Documentation/02-FirstRelease/Screens/Trade.md, "UI-решение: панель
    /// действия" — this field no longer drives a package-size step); it is still used to label
    /// the action panel's title (e.g. "Steel (Good)") and elsewhere the category itself matters.
    /// Defaults to <see cref="TradeItemCategories.Good"/> for callers/fixtures that predate
    /// this field (story-20260825-084409 Batch 3, U10).
    /// </summary>
    string Category = TradeItemCategories.Good,
    /// <summary>Mass per cargo unit, used to quote capacity before submitting a trade.</summary>
    long UnitMassKg = 1,
    /// <summary>
    /// Stock level the station's bounded economy converges to, in trade units (quantity, not
    /// mass — mass per unit is <see cref="UnitMassKg"/>). <c>null</c> means this row has no
    /// configured bounded economy (bootstrap-only profiles, legacy rows, Fuel) — never 0 and
    /// never an implied <see cref="StationMarketStockState.Normal"/>. The four bounded-market
    /// fields below are published either all together or all null.
    /// </summary>
    long? TargetStock = null,
    /// <summary>
    /// Hard storage cap for this item on the station, in trade units — twice
    /// <see cref="TargetStock"/>. <c>StockQuantity</c> never exceeds it.
    /// <c>null</c> when the row has no bounded economy.
    /// </summary>
    long? MaxStock = null,
    /// <summary>
    /// Storage room left on the station, in trade units: <see cref="MaxStock"/> minus
    /// <c>StockQuantity</c>. 0 is a meaningful value — a full market that can accept no further
    /// units. <c>null</c> when the row has no bounded economy.
    /// </summary>
    long? FreeStockCapacity = null,
    /// <summary>
    /// Authoritative shortage/normal/surplus band for this row, computed by the Engine.
    /// The client displays it and never recomputes or simulates it.
    /// <c>null</c> when the row has no bounded economy.
    /// </summary>
    StationMarketStockState? StockState = null);

/// <summary>
/// Authoritative stock band of one bounded-economy market row, computed by the Engine from the
/// station profile's configured thresholds (by default: strictly below 0.5×TargetStock is
/// <see cref="Shortage"/>, strictly above 1.5×TargetStock is <see cref="Surplus"/>, the
/// inclusive range between them is <see cref="Normal"/>). Serialized as its member name so
/// persisted snapshots stay readable and stable if members are ever reordered.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<StationMarketStockState>))]
public enum StationMarketStockState
{
    /// <summary>Stock is under the profile's shortage threshold — replenishment is pending.</summary>
    Shortage,

    /// <summary>Stock sits between the shortage and surplus thresholds, bounds included.</summary>
    Normal,

    /// <summary>Stock is over the profile's surplus threshold.</summary>
    Surplus,
}

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
