using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Last observed coarse market information. Timestamp, revision and stock bands describe the
/// observation, not the current remote market. The Engine projects IsStale without refreshing it.
/// Exact quantities, prices, budgets and quotes are deliberately absent.
/// </summary>
public sealed record StationMarketKnowledgeSnapshot(
    string StationObjectId,
    string StationRole,
    bool IsAvailable,
    long ObservedAtGameTimeMs,
    ulong ObservedMarketRevision,
    bool IsStale,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<StationMarketStockBandSnapshot>))]
    ImmutableArray<StationMarketStockBandSnapshot> StockBands = default);

/// <summary>Observed authoritative band of an economy-managed item, without exact market values.</summary>
public sealed record StationMarketStockBandSnapshot(string ItemTypeId, StationMarketStockState StockState);
