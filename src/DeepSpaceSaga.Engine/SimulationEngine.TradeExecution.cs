using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine;

/// <summary>What a caller wants priced: one trade command for one item at the ship's docked station.</summary>
internal sealed record TradeQuoteRequest(
    string RequestId,
    string ObjectId,
    string ModuleId,
    string CommandType,
    string ItemTypeId,
    long Quantity);

/// <summary>One segment of a quote curve: <see cref="Quantity"/> units at <see cref="UnitPriceCredits"/> each.</summary>
internal sealed record TradePriceStep(long Quantity, long UnitPriceCredits);

/// <summary>
/// Authoritative, immutable quote issued by <see cref="SimulationEngine.GetTradeQuote"/>. A usable quote has a
/// non-empty <see cref="QuoteId"/> and a null <see cref="DisabledReason"/>; its <see cref="Curve"/> covers exactly
/// the <see cref="ExecutableQuantity"/> prefix and sums to <see cref="TotalCredits"/>. A disabled quote carries an
/// empty id, zero executable/total and an empty curve, and is never cached, so it cannot be executed.
/// Hidden station Credits/budget never appear here — only the quantities they allow.
/// </summary>
internal sealed record TradeQuoteSnapshot(
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
    ImmutableArray<TradePriceStep> Curve,
    string? DisabledReason,
    ImmutableArray<string> LimitReasons);

/// <summary>
/// Quoted trade execution (EP-0001-US-0003-TK-0002): the engine issues a quote bound to one station, ship, module,
/// item, command type, quantity and market revision, and later executes exactly that quote as one transaction or
/// rejects it with no effect. The quote cache and nonce are session state only — never simulated, never saved —
/// so every load starts a fresh quote session and every earlier quote becomes stale.
/// </summary>
public sealed partial class SimulationEngine
{
    private const int QuoteCacheLimit = 1024;
    private const string ValueOverflow = "value_overflow";

    private readonly Dictionary<string, IssuedQuote> _issuedQuotes = new(StringComparer.Ordinal);
    private readonly Queue<string> _issuedQuoteOrder = new();
    private string _quoteNonce = "";
    private long _quoteCounter;

    private sealed record IssuedQuote(TradeQuoteSnapshot Quote, QuoteContext Context);

    /// <summary>
    /// Everything a quote's numbers depend on. A quote is only executable while the current context equals the
    /// one captured at issue time, which also catches market changes that do not bump the revision (hourly
    /// production, budget refill, port fees, events).
    /// </summary>
    private readonly record struct QuoteContext(
        string StationObjectId,
        long MarketRevision,
        long PlayerCredits,
        long StationCredits,
        long? StationBudgetCredits,
        long StockQuantity,
        long UnitPriceCredits,
        long? MaxStock,
        bool IsDocked,
        string? DockedStationObjectId,
        bool ModuleCanExecute,
        long CargoQuantity,
        long FreeCargoKg,
        long FuelAmountKg,
        long FuelCapacityKg);

    /// <summary>Resolved addressing of one trade command; indexes are into <see cref="_objects"/> and its lists.</summary>
    private readonly record struct TradeTarget(
        int ObjectIndex,
        SpaceObjectRuntime Ship,
        int ModuleIndex,
        InstalledModuleRuntime Module,
        ModuleTypeDefinition ModuleType,
        int StationIndex,
        SpaceObjectRuntime Station,
        int StationInventoryIndex,
        int ItemTypeIndex,
        ItemTypeDefinition ItemType,
        long Quantity);

    /// <summary>Quote numbers for the current state; <see cref="DisabledReason"/> is set when nothing can execute.</summary>
    private readonly record struct QuoteTerms(
        long UnitPriceCredits,
        long? MaxStock,
        long MaximumQuantity,
        long ExecutableQuantity,
        long TotalCredits,
        string? DisabledReason,
        ImmutableArray<string> LimitReasons);

    /// <summary>
    /// Price one trade at the ship's docked station without changing the world. Invalid input yields a disabled
    /// quote whose reason is the same code the command itself would be rejected with.
    /// </summary>
    internal TradeQuoteSnapshot GetTradeQuote(TradeQuoteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        lock (_worldStateLock)
        {
            if (!TryResolveTradeTarget(request.ObjectId, request.ModuleId, request.CommandType, request.ItemTypeId,
                    request.Quantity, out var target, out string reasonCode))
                return DisabledQuote(request, reasonCode, 0);

            QuoteTerms terms;
            QuoteContext context;
            try
            {
                terms = ComputeQuoteTerms(target, request.CommandType);
                if (terms.DisabledReason is { } disabled)
                    return DisabledQuote(request, disabled, terms.MaximumQuantity);
                context = CaptureQuoteContext(target, terms.UnitPriceCredits, terms.MaxStock);
            }
            catch (OverflowException)
            {
                return DisabledQuote(request, ValueOverflow, 0);
            }

            var quote = new TradeQuoteSnapshot(
                request.RequestId,
                $"QTE-{_quoteNonce}-{++_quoteCounter}",
                target.Station.MarketRevision,
                target.Station.InitialMotion.ObjectId,
                request.ObjectId,
                request.ModuleId,
                request.CommandType,
                request.ItemTypeId,
                request.Quantity,
                terms.ExecutableQuantity,
                terms.MaximumQuantity,
                terms.TotalCredits,
                [new TradePriceStep(terms.ExecutableQuantity, terms.UnitPriceCredits)],
                null,
                terms.LimitReasons);
            RememberQuote(quote, context);
            return quote;
        }
    }

    private TradeQuoteSnapshot DisabledQuote(TradeQuoteRequest request, string reasonCode, long maximumQuantity)
    {
        var (stationObjectId, marketRevision) = KnownDockedMarket(request.ObjectId);
        return new TradeQuoteSnapshot(
            request.RequestId, "", marketRevision ?? 0, stationObjectId ?? "", request.ObjectId, request.ModuleId,
            request.CommandType, request.ItemTypeId, request.Quantity, 0, maximumQuantity, 0,
            ImmutableArray<TradePriceStep>.Empty, reasonCode, ImmutableArray<string>.Empty);
    }

    /// <summary>
    /// Buy/Refuel are all-or-nothing, so the first limiter below the requested quantity disables the quote. Sell
    /// fills the largest prefix the budget and the free storage allow and names what limited it.
    /// </summary>
    private QuoteTerms ComputeQuoteTerms(in TradeTarget target, string commandType)
    {
        long unitPrice = ResolveUnitPriceCredits(target);
        long? maxStock = ResolveMaxStock(target.Station, target.ItemType);
        if (unitPrice < 1)
            return new QuoteTerms(unitPrice, maxStock, 0, 0, 0, CommandReasonCodes.UnknownItemType, ImmutableArray<string>.Empty);

        long qty = target.Quantity;
        long stock = target.Station.Inventory[target.StationInventoryIndex].StockQuantity;
        long maximum;
        long executable;
        string? disabled = null;
        var limits = ImmutableArray<string>.Empty;

        if (commandType == TradeCommandTypes.Sell)
        {
            long cargoQty = CargoQuantityOf(target.Module, target.ItemTypeIndex);
            long budgetCap = Math.Max(0, SellPurse(target.Station)) / unitPrice;
            long roomCap = maxStock is { } cap ? Math.Max(0, cap - stock) : long.MaxValue;
            maximum = Math.Min(cargoQty, Math.Min(budgetCap, roomCap));
            if (qty > cargoQty)
                return new QuoteTerms(unitPrice, maxStock, maximum, 0, 0, CommandReasonCodes.InsufficientCargoQuantity, limits);

            executable = Math.Min(qty, Math.Min(budgetCap, roomCap));
            var builder = ImmutableArray.CreateBuilder<string>();
            if (budgetCap < qty && budgetCap == executable) builder.Add(CommandReasonCodes.StationBudgetExceeded);
            if (roomCap < qty && roomCap == executable) builder.Add(CommandReasonCodes.StationCapacityExceeded);
            limits = builder.ToImmutable();
            if (executable <= 0) disabled = limits[0];
        }
        else
        {
            long byCredits = PlayerCredits / unitPrice;
            long byRoom;
            string roomReason;
            if (commandType == TradeCommandTypes.Refuel)
            {
                byRoom = Math.Max(0, (target.ModuleType.FuelCapacityKg ?? 0) - target.Module.FuelAmountKg);
                roomReason = CommandReasonCodes.FuelCapacityExceeded;
            }
            else
            {
                long freeKg = FreeCargoKg(target);
                byRoom = target.ItemType.UnitMassKg > 0 ? freeKg / target.ItemType.UnitMassKg : long.MaxValue;
                roomReason = CommandReasonCodes.CargoCapacityExceeded;
            }

            maximum = Math.Min(stock, Math.Min(byCredits, byRoom));
            executable = qty;
            if (qty > byCredits) disabled = CommandReasonCodes.InsufficientPlayerCredits;
            else if (qty > stock) disabled = CommandReasonCodes.InsufficientStationStock;
            else if (qty > byRoom) disabled = roomReason;
        }

        if (disabled is not null)
            return new QuoteTerms(unitPrice, maxStock, maximum, 0, 0, disabled, ImmutableArray<string>.Empty);

        long total;
        try { total = checked(executable * unitPrice); }
        catch (OverflowException) { return new QuoteTerms(unitPrice, maxStock, maximum, 0, 0, ValueOverflow, ImmutableArray<string>.Empty); }
        return new QuoteTerms(unitPrice, maxStock, maximum, executable, total, null, limits);
    }

    private QuoteContext CaptureQuoteContext(in TradeTarget target, long unitPriceCredits, long? maxStock) => new(
        target.Station.InitialMotion.ObjectId,
        target.Station.MarketRevision,
        PlayerCredits,
        target.Station.Credits,
        target.Station.MarketBudgetCredits,
        target.Station.Inventory[target.StationInventoryIndex].StockQuantity,
        unitPriceCredits,
        maxStock,
        target.Ship.IsDocked,
        target.Ship.DockedStationObjectId,
        CanExecuteModuleCommand(target.Module),
        CargoQuantityOf(target.Module, target.ItemTypeIndex),
        FreeCargoKg(target),
        target.Module.FuelAmountKg,
        target.ModuleType.FuelCapacityKg ?? 0);

    private void RememberQuote(TradeQuoteSnapshot quote, QuoteContext context)
    {
        _issuedQuotes[quote.QuoteId] = new IssuedQuote(quote, context);
        _issuedQuoteOrder.Enqueue(quote.QuoteId);
        // FIFO by issue order: only the most recent issues stay executable. Ids already consumed are simply
        // absent from the dictionary, so dequeuing them is a no-op.
        while (_issuedQuoteOrder.Count > QuoteCacheLimit)
            _issuedQuotes.Remove(_issuedQuoteOrder.Dequeue());
    }

    /// <summary>Forget every issued quote and start a new id space. Called by the constructor and every load.</summary>
    private void ResetQuoteSession()
    {
        _issuedQuotes.Clear();
        _issuedQuoteOrder.Clear();
        Span<byte> nonce = stackalloc byte[8];
        RandomNumberGenerator.Fill(nonce);
        _quoteNonce = Convert.ToHexString(nonce);
        _quoteCounter = 0;
    }

    /// <summary>
    /// Check an incoming quote binding against the issued quote and the current world, without mutating anything.
    /// A malformed or foreign binding is <c>invalid_quote</c>; an unknown, consumed, evicted or outdated one is
    /// <c>stale_quote</c>.
    /// </summary>
    private bool TryValidateTradeQuote(
        PlayerCommand command, [NotNullWhen(true)] out TradeQuoteSnapshot? quote, out string reasonCode)
    {
        quote = null;
        if (string.IsNullOrWhiteSpace(command.QuoteId) || command.MarketRevision is not { } revision || revision < 1)
        {
            reasonCode = CommandReasonCodes.InvalidQuote;
            return false;
        }

        if (!_issuedQuotes.TryGetValue(command.QuoteId, out var issued))
        {
            reasonCode = CommandReasonCodes.StaleQuote;
            return false;
        }

        if (!TryResolveTradeTarget(command.ObjectId, command.ModuleId, command.CommandType, command.ItemTypeId,
                command.Quantity, out var target, out reasonCode))
            return false;

        var bound = issued.Quote;
        if (!string.Equals(bound.StationObjectId, target.Station.InitialMotion.ObjectId, StringComparison.Ordinal) ||
            !string.Equals(bound.ObjectId, command.ObjectId, StringComparison.Ordinal) ||
            !string.Equals(bound.ModuleId, command.ModuleId, StringComparison.Ordinal) ||
            !string.Equals(bound.CommandType, command.CommandType, StringComparison.Ordinal) ||
            !string.Equals(bound.ItemTypeId, command.ItemTypeId, StringComparison.Ordinal) ||
            bound.RequestedQuantity != target.Quantity ||
            bound.MarketRevision != revision)
        {
            reasonCode = CommandReasonCodes.InvalidQuote;
            return false;
        }

        var current = CaptureQuoteContext(target, ResolveUnitPriceCredits(target), ResolveMaxStock(target.Station, target.ItemType));
        if (current != issued.Context)
        {
            reasonCode = CommandReasonCodes.StaleQuote;
            return false;
        }

        quote = bound;
        reasonCode = "";
        return true;
    }

    private long NextMarketRevision(SpaceObjectRuntime station) => checked(station.MarketRevision + 1);

    private CommandStartOutcome TryStartQuotedTrade(PlayerCommand command, long gameTimeMs)
    {
        // Every checked step runs before the first runtime assignment, so an overflow here has had no effect.
        try { return PrepareAndCommitQuotedTrade(command, gameTimeMs); }
        catch (OverflowException) { return RejectQuotedTrade(command, ValueOverflow); }
    }

    private CommandStartOutcome PrepareAndCommitQuotedTrade(PlayerCommand command, long gameTimeMs)
    {
        // 1-2. Same addressing and storage rules as the legacy path, then the quote binding itself.
        if (!TryResolveTradeTarget(command.ObjectId, command.ModuleId, command.CommandType, command.ItemTypeId,
                command.Quantity, out var target, out string reasonCode))
            return RejectQuotedTrade(command, reasonCode);
        if (!TryValidateTradeQuote(command, out var quote, out reasonCode))
            return RejectQuotedTrade(command, reasonCode);

        // 3. The quote must describe a transaction this command type allows.
        if (!IsConsistentQuote(quote))
            return RejectQuotedTrade(command, CommandReasonCodes.InvalidQuote);

        // 4. Defensive resource bounds; the price is taken from the quote, never recomputed.
        long executed = quote.ExecutableQuantity;
        long total = quote.TotalCredits;
        if (CheckQuotedResources(target, quote) is { } shortage)
            return RejectQuotedTrade(command, shortage);

        // 5-6. Stage the whole new state and the receipt.
        var station = target.Station;
        var stockItem = station.Inventory[target.StationInventoryIndex];
        long nextRevision = NextMarketRevision(station);
        long updatedPlayerCredits;
        SpaceObjectRuntime updatedStation;
        SpaceObjectRuntime updatedShip;
        if (quote.CommandType == TradeCommandTypes.Sell)
        {
            bool bounded = TryGetMarket(station, out _, out _);
            updatedPlayerCredits = checked(PlayerCredits + total);
            updatedStation = station with
            {
                Credits = checked(station.Credits - total),
                MarketBudgetCredits = bounded ? checked((station.MarketBudgetCredits ?? 0) - total) : station.MarketBudgetCredits,
                Inventory = station.Inventory.SetItem(target.StationInventoryIndex,
                    stockItem with { StockQuantity = checked(stockItem.StockQuantity + executed) }),
                MarketRevision = nextRevision,
            };
            updatedShip = UpdateModule(target.Ship, target.ModuleIndex,
                m => WithCargoDelta(m, target.ModuleType, target.ItemTypeIndex, -executed));
        }
        else
        {
            updatedPlayerCredits = checked(PlayerCredits - total);
            updatedStation = station with
            {
                Credits = checked(station.Credits + total),
                MarketBudgetCredits = ReplenishBudgetFromIncome(station, total),
                Inventory = station.Inventory.SetItem(target.StationInventoryIndex,
                    stockItem with { StockQuantity = checked(stockItem.StockQuantity - executed) }),
                MarketRevision = nextRevision,
            };
            updatedShip = quote.CommandType == TradeCommandTypes.Refuel
                ? UpdateModule(target.Ship, target.ModuleIndex, m => m with { FuelAmountKg = checked(m.FuelAmountKg + executed) })
                : UpdateModule(target.Ship, target.ModuleIndex,
                    m => WithCargoDelta(m, target.ModuleType, target.ItemTypeIndex, executed));
        }

        var receipt = new TradeExecutionReceipt(
            station.InitialMotion.ObjectId, quote.ItemTypeId, quote.QuoteId, quote.MarketRevision, nextRevision,
            quote.RequestedQuantity, executed, total, quote.LimitReasons);

        // 7-9. Commit: plain assignments only, then the result, then the quote is consumed.
        _objects[target.ObjectIndex] = updatedShip;
        _objects[target.StationIndex] = updatedStation;
        PlayerCredits = updatedPlayerCredits;
        RecordCommandResult(command, CommandResultStatus.Executed, gameTimeMs,
            executedQuantity: executed < quote.RequestedQuantity ? executed : null, tradeReceipt: receipt);
        _issuedQuotes.Remove(quote.QuoteId);
        return CommandStartOutcome.Started;
    }

    /// <summary>Curve sums to the executable prefix and total; only Sell may be partial, and only with a named limit.</summary>
    private static bool IsConsistentQuote(TradeQuoteSnapshot quote)
    {
        if (quote.Curve.IsDefaultOrEmpty || quote.LimitReasons.IsDefault) return false;
        long quantity = 0;
        long total = 0;
        foreach (var step in quote.Curve)
        {
            if (step.Quantity <= 0 || step.UnitPriceCredits < 1) return false;
            quantity = checked(quantity + step.Quantity);
            total = checked(total + checked(step.Quantity * step.UnitPriceCredits));
        }

        if (quantity != quote.ExecutableQuantity || total != quote.TotalCredits) return false;
        if (quote.ExecutableQuantity <= 0 || quote.ExecutableQuantity > quote.RequestedQuantity) return false;
        return quote.CommandType == TradeCommandTypes.Sell
            ? quote.ExecutableQuantity == quote.RequestedQuantity || !quote.LimitReasons.IsEmpty
            : quote.ExecutableQuantity == quote.RequestedQuantity;
    }

    private string? CheckQuotedResources(in TradeTarget target, TradeQuoteSnapshot quote)
    {
        long executed = quote.ExecutableQuantity;
        long total = quote.TotalCredits;
        long stock = target.Station.Inventory[target.StationInventoryIndex].StockQuantity;
        if (quote.CommandType == TradeCommandTypes.Sell)
        {
            if (executed > CargoQuantityOf(target.Module, target.ItemTypeIndex))
                return CommandReasonCodes.InsufficientCargoQuantity;
            if (total > SellPurse(target.Station))
                return CommandReasonCodes.StationBudgetExceeded;
            if (ResolveMaxStock(target.Station, target.ItemType) is { } maxStock && checked(stock + executed) > maxStock)
                return CommandReasonCodes.StationCapacityExceeded;
            return null;
        }

        if (total > PlayerCredits)
            return CommandReasonCodes.InsufficientPlayerCredits;
        if (executed > stock)
            return CommandReasonCodes.InsufficientStationStock;
        if (quote.CommandType == TradeCommandTypes.Refuel)
        {
            return checked(target.Module.FuelAmountKg + executed) > (target.ModuleType.FuelCapacityKg ?? 0)
                ? CommandReasonCodes.FuelCapacityExceeded
                : null;
        }

        long addedMassKg = checked(executed * target.ItemType.UnitMassKg);
        return checked(ComputeCargoMassKg(target.Module.Cargo) + addedMassKg) > (target.ModuleType.CargoCapacityKg ?? 0)
            ? CommandReasonCodes.CargoCapacityExceeded
            : null;
    }

    /// <summary>Zero-effect rejection: raw request echoed, known docked station and its unchanged revision.</summary>
    private CommandStartOutcome RejectQuotedTrade(PlayerCommand command, string reasonCode)
    {
        var (stationObjectId, marketRevision) = KnownDockedMarket(command.ObjectId);
        return CommandStartOutcome.Rejected(reasonCode, new TradeExecutionReceipt(
            stationObjectId, command.ItemTypeId, command.QuoteId, command.MarketRevision, marketRevision,
            command.Quantity, 0, 0, ImmutableArray<string>.Empty));
    }

    /// <summary>The station the player ship is docked at, if any — never an id taken from the request.</summary>
    private (string? StationObjectId, long? MarketRevision) KnownDockedMarket(string? objectId)
    {
        if (objectId is null || !string.Equals(objectId, PlayerShipObjectId, StringComparison.Ordinal))
            return (null, null);
        var ship = _objects.Find(o => string.Equals(o.InitialMotion.ObjectId, objectId, StringComparison.Ordinal) &&
            string.Equals(o.ObjectType, "PlayerShip", StringComparison.OrdinalIgnoreCase));
        if (ship is not { IsDocked: true, DockedStationObjectId: { } stationId })
            return (null, null);
        var station = _objects.Find(o => string.Equals(o.InitialMotion.ObjectId, stationId, StringComparison.Ordinal));
        return station is not null && station.ObjectType == SpaceObjectType.Station
            ? (station.InitialMotion.ObjectId, station.MarketRevision)
            : (null, null);
    }

    /// <summary>
    /// Shared addressing for quoted and legacy trade commands, in the legacy rejection order: object, module,
    /// command type, module state, docking, quantity, item, fuel routing, station stock entry.
    /// </summary>
    private bool TryResolveTradeTarget(
        string? objectId, string? moduleId, string? commandType, string? itemTypeId, long? quantity,
        out TradeTarget target, out string reasonCode)
    {
        target = default;
        reasonCode = CommandReasonCodes.UnknownObject;
        if (objectId is null || !string.Equals(objectId, PlayerShipObjectId, StringComparison.Ordinal))
            return false;

        int objectIndex = _objects.FindIndex(o =>
            string.Equals(o.InitialMotion.ObjectId, objectId, StringComparison.Ordinal) &&
            string.Equals(o.ObjectType, "PlayerShip", StringComparison.OrdinalIgnoreCase));
        if (objectIndex < 0)
            return false;

        var obj = _objects[objectIndex];
        int moduleIndex = moduleId is null ? -1 : FindModuleIndex(obj.Modules, moduleId);
        if (moduleIndex < 0)
        {
            reasonCode = CommandReasonCodes.UnknownModule;
            return false;
        }

        var module = obj.Modules[moduleIndex];
        var moduleType = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
        // Whether the addressed module supports this trade command type at all — this is also
        // the "right kind of module" check: Buy/Sell land on module.container.basic, Refuel on
        // module.engine.basic, purely through content wiring (Data\Commands\Container,
        // module-types.json), no separate hardcoded module-type check needed.
        if (commandType is not (TradeCommandTypes.Buy or TradeCommandTypes.Sell or TradeCommandTypes.Refuel) ||
            !moduleType.CommandTypeIds.Contains(commandType, StringComparer.Ordinal))
        {
            reasonCode = CommandReasonCodes.UnknownCommandType;
            return false;
        }

        if (!CanExecuteModuleCommand(module))
        {
            reasonCode = CommandReasonCodes.ModuleUnavailable;
            return false;
        }

        reasonCode = CommandReasonCodes.NotDocked;
        if (!obj.IsDocked)
            return false;

        int stationIndex = _objects.FindIndex(o =>
            string.Equals(o.InitialMotion.ObjectId, obj.DockedStationObjectId, StringComparison.Ordinal));
        if (stationIndex < 0)
            return false;

        if (quantity is not { } qty || qty <= 0)
        {
            reasonCode = CommandReasonCodes.InvalidQuantity;
            return false;
        }

        reasonCode = CommandReasonCodes.UnknownItemType;
        if (string.IsNullOrWhiteSpace(itemTypeId) || !_registry.ItemTypes.Contains(itemTypeId))
            return false;

        int itemTypeIndex = _registry.ItemTypes.GetIndex(itemTypeId);
        var itemType = _registry.ItemTypes.GetDefinition(itemTypeIndex);
        if (CheckFuelRoute(commandType, itemType) is { } fuelReason)
        {
            reasonCode = fuelReason;
            return false;
        }

        var station = _objects[stationIndex];
        int stationInventoryIndex = FindInventoryIndex(station.Inventory, itemTypeIndex);
        if (stationInventoryIndex < 0)
            return false;

        target = new TradeTarget(objectIndex, obj, moduleIndex, module, moduleType, stationIndex, station,
            stationInventoryIndex, itemTypeIndex, itemType, qty);
        reasonCode = "";
        return true;
    }

    /// <summary>Fuel lives in the tank: it is traded only via Refuel, and Refuel accepts nothing else.</summary>
    private static string? CheckFuelRoute(string commandType, ItemTypeDefinition item)
    {
        bool fuel = item.StorageKind == ItemStorageKind.FuelTank;
        if (commandType is TradeCommandTypes.Buy or TradeCommandTypes.Sell)
            return fuel ? CommandReasonCodes.FuelTradeForbidden : null;
        if (commandType == TradeCommandTypes.Refuel)
            return fuel ? null : CommandReasonCodes.FuelTradeForbidden;
        return null;
    }

    private long ResolveUnitPriceCredits(in TradeTarget target) => StationPricing.ComputeUnitPriceCredits(
        target.ItemType.BasePriceCredits ?? 0, ResolveStationPriceFactors(target.Station, target.ItemTypeIndex, target.ItemType));

    /// <summary>Stock cap of a bounded market's cargo item; null where the station has no such cap.</summary>
    private long? ResolveMaxStock(SpaceObjectRuntime station, ItemTypeDefinition itemType) =>
        itemType.StorageKind == ItemStorageKind.Cargo && TryGetMarket(station, out var profile, out var economy) &&
        TryMarketLimits(profile, economy, station.StationSize, itemType.TypeId, out var limits)
            ? limits.MaxStock
            : null;

    /// <summary>A bounded market pays out of its trading budget, an unconfigured one out of its hidden Credits.</summary>
    private long SellPurse(SpaceObjectRuntime station) =>
        TryGetMarket(station, out _, out _) ? station.MarketBudgetCredits ?? 0 : station.Credits;

    private long FreeCargoKg(in TradeTarget target) =>
        Math.Max(0, checked((target.ModuleType.CargoCapacityKg ?? 0) - ComputeCargoMassKg(target.Module.Cargo)));

    private static long CargoQuantityOf(InstalledModuleRuntime module, int itemTypeIndex)
    {
        int stackIndex = FindCargoStackIndex(module.Cargo, itemTypeIndex);
        return stackIndex >= 0 ? module.Cargo[stackIndex].Quantity : 0;
    }

    /// <summary>Add (positive) or remove (negative) units of one item and refresh the stored free capacity.</summary>
    private InstalledModuleRuntime WithCargoDelta(
        InstalledModuleRuntime module, ModuleTypeDefinition moduleType, int itemTypeIndex, long delta)
    {
        int stackIndex = FindCargoStackIndex(module.Cargo, itemTypeIndex);
        long remaining = checked((stackIndex >= 0 ? module.Cargo[stackIndex].Quantity : 0) + delta);
        var cargo = stackIndex < 0
            ? module.Cargo.Add(new CargoStackRuntime(itemTypeIndex, remaining))
            : remaining > 0
                ? module.Cargo.SetItem(stackIndex, module.Cargo[stackIndex] with { Quantity = remaining })
                : module.Cargo.RemoveAt(stackIndex);
        return module with { Cargo = cargo, AvailableCapacityKg = ComputeAvailableCapacityKg(moduleType, cargo) };
    }
}
