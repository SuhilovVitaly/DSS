using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine;

/// <summary>
/// Quoted trade execution (EP-0001-US-0003-TK-0002): executes exactly one quote issued by the engine
/// (SimulationEngine.TradeQuotes.cs, EP-0001-US-0015-TK-0004) as one transaction, or rejects it with no effect.
/// Addressing and fuel routing here are shared with the legacy unquoted path and with the issuer.
/// </summary>
public sealed partial class SimulationEngine
{
    private const string ValueOverflow = "value_overflow";

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
        long nextRevision = NextMarketRevision(station.InitialMotion.ObjectId);
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
        CommitMarketRevision(station.InitialMotion.ObjectId, nextRevision);
        RecordCommandResult(command, CommandResultStatus.Executed, gameTimeMs,
            executedQuantity: executed < quote.RequestedQuantity ? executed : null, tradeReceipt: receipt);
        ForgetQuote(quote.QuoteId, string.IsNullOrEmpty(quote.RequestId) ? null : quote.RequestId);
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
