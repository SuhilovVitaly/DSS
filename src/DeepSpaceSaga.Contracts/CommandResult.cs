using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Final disposition of a player command after processing by the authoritative
/// engine (requirements §27.1). Published in the next
/// <see cref="AuthoritativeSnapshot"/> — results of commands processed since the
/// previous snapshot.
/// </summary>
/// <remarks>
/// <para>
/// Push-notifications are not required: the client observes command outcomes
/// through the snapshot stream.
/// </para>
/// <para>
/// <c>deferAttemptCount</c> from §27.1 is intentionally NOT present yet: the
/// engine does not track deferred-attempt limits. It will be added together
/// with the §26.5 DeferredLimitExceeded rule.
/// </para>
/// </remarks>
public enum CommandResultStatus
{
    /// <summary>Command started successfully (or was an idempotent no-op / replacement).</summary>
    Executed,

    /// <summary>Command was rejected — it never entered the pending queue (§26.1).</summary>
    Rejected,

    /// <summary>Command was deferred because the module was busy; it stays pending and is retried.</summary>
    Deferred,

    /// <summary>Command was cancelled (future: §26.5 supersession by a newer command).</summary>
    Cancelled,

    /// <summary>Command failed at execution (future: §56.5 success roll).</summary>
    Failed
}

/// <summary>
/// Machine-readable outcome of one processed player command (requirements §27.1).
/// <see cref="ReasonCode"/> is null on success and a snake_case code on failure
/// (style per §56.6, e.g. <c>power_off</c>, <c>no_power</c>).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CommandResultStatus.Deferred"/> is <b>per-processing</b>: if a
/// command stays deferred for N ticks, the engine publishes one Deferred result
/// per tick it was processed (retried and requeued). The final disposition
/// (Executed, Rejected, or Cancelled) appears exactly once in the snapshot where
/// the command leaves the pending queue.
/// </para>
/// <para>
/// Within a single snapshot, each CommandId appears at most once — the engine
/// deduplicates by CommandId during drain, keeping the last disposition.
/// </para>
/// </remarks>
public sealed record CommandResult(
    string CommandId,
    string ObjectId,
    string ModuleId,
    string CommandType,
    CommandResultStatus Status,
    long EffectiveGameTimeMs,
    string? ReasonCode = null,
    /// <summary>
    /// Actually executed quantity for a trade command. Null for all non-trade commands
    /// and for trade commands that executed fully. For <see cref="TradeCommandTypes.Sell"/>
    /// executed partially because the station's hidden Credits balance ran out
    /// (Documentation\02-FirstRelease\Mechanics\Money.md), carries the quantity actually sold
    /// (less than the requested <see cref="PlayerCommand.Quantity"/>).
    /// Kept partial-only for existing consumers; <see cref="TradeReceipt"/> always carries the actual quantity.
    /// </summary>
    long? ExecutedQuantity = null,
    /// <summary>
    /// Exact execution receipt of a quoted trade command, both on success and on rejection.
    /// Null for non-trade commands and for legacy results (including saves written before receipts existed).
    /// </summary>
    TradeExecutionReceipt? TradeReceipt = null);

/// <summary>
/// Authoritative record of what a quoted trade actually transferred. <see cref="TotalCredits"/> is the exact
/// executed sum from the quote curve and cannot be reconstructed by multiplying a single unit price.
/// The DTO computes and validates nothing; the engine enforces the semantics below.
/// </summary>
/// <remarks>
/// <para>
/// Success (<see cref="CommandResult.ReasonCode"/> is null): all ids and revisions and
/// <see cref="RequestedQuantity"/> are present, <c>RequestedQuantity &gt; 0</c>,
/// <c>0 &lt; ExecutedQuantity &lt;= RequestedQuantity</c>, <c>TotalCredits &gt;= 0</c> and
/// <c>ResultMarketRevision &gt; QuotedMarketRevision</c>. A smaller fill lists its causes in
/// <see cref="LimitReasons"/>, never in <see cref="CommandResult.ReasonCode"/>.
/// </para>
/// <para>
/// Rejection: <c>ExecutedQuantity = TotalCredits = 0</c> and the known market revision is unchanged.
/// Item, quote and requested quantity echo the raw command (may be null or invalid, which is why they
/// are nullable); station and revisions are the known authoritative context, never invented ids.
/// </para>
/// <para>
/// <see cref="TotalCredits"/> is an absolute amount; its direction (paid or received) is given by
/// <see cref="CommandResult.CommandType"/>.
/// </para>
/// </remarks>
public sealed record TradeExecutionReceipt(
    string? StationObjectId,
    string? ItemTypeId,
    string? QuoteId,
    long? QuotedMarketRevision,
    long? ResultMarketRevision,
    long? RequestedQuantity,
    long ExecutedQuantity,
    long TotalCredits,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<string>))]
    ImmutableArray<string> LimitReasons = default);

/// <summary>
/// Machine-readable reason codes for non-executed command results (snake_case,
/// style per requirements §56.6).
/// </summary>
public static class CommandReasonCodes
{
    /// <summary>Command addressed an object that is not the player ship or does not exist.</summary>
    public const string UnknownObject = "unknown_object";

    /// <summary>Command addressed a module that does not exist on the target object.</summary>
    public const string UnknownModule = "unknown_module";

    /// <summary>Command type is not supported by the target module.</summary>
    public const string UnknownCommandType = "unknown_command_type";

    /// <summary>Module exists but cannot execute commands (power Off / Disabled / no structure points / no inertia).</summary>
    public const string ModuleUnavailable = "module_unavailable";

    /// <summary>Module is busy with an active non-auto-repeat cycle; command deferred.</summary>
    public const string Busy = "busy";

    /// <summary>Match command (e.g. speed-synchronization) arrived without the required targetObjectId (§56.9).</summary>
    public const string MissingTarget = "missing_target";

    /// <summary>Match command referenced a targetObjectId that does not exist in the world (§56.9).</summary>
    public const string UnknownTarget = "unknown_target";

    /// <summary>Turn command arrived inside the angular-inertia window since the previous turn.</summary>
    public const string TurnInertiaBlocked = "turn_inertia_blocked";

    /// <summary>Navigate-to-point command arrived without finite world target coordinates.</summary>
    public const string InvalidTargetCoordinates = "invalid_target_coordinates";

    /// <summary>Navigate-to-point target is too close — inside the turn radius and not on the current straight-line path.</summary>
    public const string NavigationTargetTooClose = "navigation_target_too_close";
    public const string NavigationRequiresMotion = "navigation_requires_motion";

    /// <summary>Dock command's targetObjectId does not resolve to a Station object.</summary>
    public const string DockTargetNotStation = "dock_target_not_station";

    /// <summary>Dock command's target station is farther than the command's configured range.</summary>
    public const string DockOutOfRange = "dock_out_of_range";

    /// <summary>Dock command arrived before the ship's speed and direction matched the target station's.</summary>
    public const string DockNotSynchronized = "dock_not_synchronized";

    /// <summary>Player does not have enough Credits to buy/refuel the requested quantity (Buy/Refuel are rejected in full, all-or-nothing).</summary>
    public const string InsufficientPlayerCredits = "insufficient_player_credits";

    /// <summary>Station does not have enough stock of the item to sell the requested quantity to the player.</summary>
    public const string InsufficientStationStock = "insufficient_station_stock";

    /// <summary>Buying the requested quantity would exceed the target container module's CargoCapacityKg.</summary>
    public const string CargoCapacityExceeded = "cargo_capacity_exceeded";

    /// <summary>Refueling the requested quantity would exceed the target module's FuelCapacityKg.</summary>
    public const string FuelCapacityExceeded = "fuel_capacity_exceeded";

    /// <summary>The command's ItemTypeId does not resolve to any known tradeable item.</summary>
    public const string UnknownItemType = "unknown_item_type";

    /// <summary>A Buy/Sell/Refuel command arrived while the ship is not docked to a station.</summary>
    public const string NotDocked = "not_docked";

    /// <summary>Sell command requested more of an item than the addressed module's cargo currently holds.</summary>
    public const string InsufficientCargoQuantity = "insufficient_cargo_quantity";

    /// <summary>Trade command's Quantity was missing, zero, or negative.</summary>
    public const string InvalidQuantity = "invalid_quantity";

    /// <summary>Trade at a profile market arrived without a quote binding (QuoteId + MarketRevision).</summary>
    public const string QuoteRequired = "quote_required";

    /// <summary>Quote is expired, evicted, consumed, or its market revision/context is no longer current.</summary>
    public const string StaleQuote = "stale_quote";

    /// <summary>Quote binding is partial, malformed, or bound to a different station/object/module/item/type/quantity.</summary>
    public const string InvalidQuote = "invalid_quote";

    /// <summary>Station trading budget cannot pay for even one unit of the sell.</summary>
    public const string StationBudgetExceeded = "station_budget_exceeded";

    /// <summary>Station storage cannot accept even one unit of the sell.</summary>
    public const string StationCapacityExceeded = "station_capacity_exceeded";

    /// <summary>Fuel may only be traded via Refuel, and Refuel only accepts fuel.</summary>
    public const string FuelTradeForbidden = "fuel_trade_forbidden";
}
