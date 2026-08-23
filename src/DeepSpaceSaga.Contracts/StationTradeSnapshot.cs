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
    long MaxSellableQuantity);
