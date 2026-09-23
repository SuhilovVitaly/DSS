using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

/// <summary>
/// What a caller wants priced: one trade command (<see cref="TradeCommandTypes"/>) for one item and
/// quantity, addressed to a module of the ship at its docked station. <see cref="RequestId"/> is chosen by
/// the caller and echoed in <see cref="TradeQuoteSnapshot.RequestId"/>. The DTO validates nothing; the
/// authoritative session answers invalid requests with a disabled quote.
/// </summary>
public sealed record TradeQuoteRequest(
    string RequestId,
    string ObjectId,
    string ModuleId,
    string CommandType,
    string ItemTypeId,
    long Quantity);

/// <summary>
/// One segment of a quote curve: <see cref="Quantity"/> consecutive units at <see cref="UnitPriceCredits"/>
/// each. In an executable quote every step has <c>Quantity &gt; 0</c> and <c>UnitPriceCredits &gt;= 1</c>.
/// </summary>
public sealed record TradePriceStep(long Quantity, long UnitPriceCredits);

/// <summary>
/// One factor that shaped a quote's price, for display only — the client never recomputes a price from it.
/// <see cref="Code"/> is a stable snake_case code (e.g. <c>base_price</c>, <c>station_profile</c>,
/// <c>stock_shortage</c>, <c>event</c>, <c>buy_spread</c>, <c>price_floor</c>);
/// <see cref="FactorPermille"/> is the multiplier in thousandths (1000 = ×1.0);
/// <see cref="SourceId"/> names the profile/event behind the factor, or null when there is none.
/// </summary>
public sealed record TradePriceReason(
    string Code,
    int FactorPermille,
    string? SourceId = null);

/// <summary>
/// Authoritative, immutable trade quote issued by the game session for exactly one binding: station,
/// <see cref="ObjectId"/>, <see cref="ModuleId"/>, <see cref="CommandType"/>, <see cref="ItemTypeId"/>,
/// <see cref="RequestedQuantity"/> and <see cref="MarketRevision"/>. A quoted
/// <see cref="PlayerCommand"/> executes it by <see cref="QuoteId"/>; the client never supplies a price.
/// The DTO computes and validates nothing; the engine enforces the semantics below.
/// </summary>
/// <remarks>
/// <para>
/// Usable quote (<see cref="DisabledReason"/> is null): non-empty <see cref="QuoteId"/>,
/// <c>MarketRevision &gt;= 1</c>, <c>0 &lt;= ExecutableQuantity &lt;= RequestedQuantity</c>,
/// <c>MaximumQuantity &gt;= ExecutableQuantity</c> and <c>TotalCredits &gt;= 0</c>. The step quantities of
/// <see cref="Curve"/> sum to <see cref="ExecutableQuantity"/> and their checked weighted sum equals
/// <see cref="TotalCredits"/>. A smaller fill lists its causes in <see cref="LimitReasons"/>.
/// </para>
/// <para>
/// Disabled quote: a non-empty <see cref="DisabledReason"/>, an empty <see cref="QuoteId"/>,
/// <c>ExecutableQuantity = TotalCredits = 0</c> and an empty <see cref="Curve"/>; it cannot be executed.
/// </para>
/// <para>
/// Hidden station Credits and market budget never appear here — only the quantities they allow.
/// <see cref="TotalCredits"/> is an absolute amount; its direction (paid or received) is given by
/// <see cref="CommandType"/>.
/// </para>
/// </remarks>
public sealed record TradeQuoteSnapshot(
    string RequestId,
    string QuoteId,
    long MarketRevision,
    string StationObjectId,
    string ObjectId,
    string ModuleId,
    string CommandType,
    string ItemTypeId,
    long RequestedQuantity,
    long ExecutableQuantity,
    long MaximumQuantity,
    long TotalCredits,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<TradePriceStep>))]
    ImmutableArray<TradePriceStep> Curve,
    string? DisabledReason,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<string>))]
    ImmutableArray<string> LimitReasons,
    /// <summary>
    /// Factors that shaped the price, in application order. Default/empty for quotes built without
    /// explanation (trailing optional addition).
    /// </summary>
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<TradePriceReason>))]
    ImmutableArray<TradePriceReason> PriceReasons = default);
