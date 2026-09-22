using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

/// <summary>
/// Bounded station market: hourly production/consumption on the calendar grid, stock caps with a
/// pending-output remainder, and the trading budget (US-0002 TK-0003). Everything here is opt-in —
/// a station only participates when its market profile declares an economy (TK-0002). A station
/// without one keeps the US-0001 bootstrap-only behaviour, which is why every entry point starts
/// from <see cref="TryGetMarket"/>.
/// </summary>
public sealed partial class SimulationEngine
{
    /// <summary>Per-station-size limits of one economy-managed cargo item. MaxStock is always 2x Target (AC-03).</summary>
    private readonly record struct MarketItemLimits(long Target, long MaxStock);

    // SpaceObjectRuntime.MarketBudgetCredits is the station's trading budget: the slice of its
    // Credits currently available to spend on the player's behalf (US-0002 AC-05). It stays null
    // for any station whose profile declares no economy, and that null is exactly what keeps every
    // pre-US-0002 station on its old unbounded path through this whole file.

    private StationMarketProfileDefinition? MarketProfileOf(SpaceObjectRuntime station)
    {
        if (station.MarketProfileId is not { } profileId) return null;
        if (!_registry.StationMarketProfiles.Contains(profileId)) return null;
        return _registry.StationMarketProfiles.GetDefinition(_registry.StationMarketProfiles.GetIndex(profileId));
    }

    /// <summary>True only for a Station whose resolved profile actually declares a bounded economy.</summary>
    private bool TryGetMarket(
        SpaceObjectRuntime station,
        out StationMarketProfileDefinition profile,
        out StationMarketEconomyDefinition economy)
    {
        profile = null!;
        economy = null!;
        if (station.ObjectType != SpaceObjectType.Station) return false;
        if (MarketProfileOf(station) is not { Economy: { } declared } resolved) return false;
        profile = resolved;
        economy = declared;
        return true;
    }

    private bool HasBoundedMarket() => _objects.Any(obj => TryGetMarket(obj, out _, out _));

    private static bool TryMarketLimits(
        StationMarketProfileDefinition profile,
        StationMarketEconomyDefinition economy,
        StationSize size,
        string itemTypeId,
        out MarketItemLimits limits)
    {
        foreach (var target in economy.StockTargets)
        {
            if (!string.Equals(target.ItemTypeId, itemTypeId, StringComparison.Ordinal)) continue;
            long scaled = ScaleProfileValue(target.TargetStock, profile.SizeFactors[size]);
            limits = new MarketItemLimits(scaled, checked(2 * scaled));
            return true;
        }
        limits = default;
        return false;
    }

    private bool TryMarketLimits(
        StationMarketProfileDefinition profile,
        StationMarketEconomyDefinition economy,
        SpaceObjectRuntime station,
        int itemTypeIndex,
        out MarketItemLimits limits) =>
        TryMarketLimits(profile, economy, station.StationSize,
            _registry.ItemTypes.GetDefinition(itemTypeIndex).TypeId, out limits);

    /// <summary>Cap on the station's trading budget: 2x its size-scaled starting Credits (AC-05).</summary>
    private static long MarketMaxBudget(StationMarketProfileDefinition profile, StationSize size) =>
        checked(2 * ScaleProfileValue(profile.InitialCredits, profile.SizeFactors[size]));

    /// <summary>
    /// Shortage/Normal/Surplus from the profile's own permille thresholds. Compared as
    /// 1000x stock against threshold x target so the boundary is exact: strictly below the shortage
    /// threshold is Shortage, strictly above the surplus threshold is Surplus, both boundaries
    /// themselves are Normal.
    /// </summary>
    private static StationMarketStockState MarketBand(long stock, long target, StationMarketEconomyDefinition economy)
    {
        decimal scaled = 1000m * stock;
        if (scaled < (decimal)economy.ShortageThresholdPermille * target) return StationMarketStockState.Shortage;
        if (scaled > (decimal)economy.SurplusThresholdPermille * target) return StationMarketStockState.Surplus;
        return StationMarketStockState.Normal;
    }

    /// <summary>
    /// Next whole game hour strictly after the processed cursor, or <see cref="long.MaxValue"/>
    /// when no station has a bounded economy — the boundary is then never a candidate and the
    /// calendar loop keeps its pre-US-0002 shape exactly.
    /// </summary>
    private long NextMarketHourTime()
    {
        if (!HasBoundedMarket()) return long.MaxValue;
        long floor = _processedWorldTimeMs - _processedWorldTimeMs % GameCalendar.HourMs;
        return floor > long.MaxValue - GameCalendar.HourMs ? long.MaxValue : floor + GameCalendar.HourMs;
    }

    /// <summary>
    /// One hour of bounded market economy, applied once per whole calendar hour regardless of how
    /// often snapshots are taken or how fast the clock runs. Stations are visited in ordinal
    /// ObjectId order and each station's items in ordinal ItemTypeId order, so the same elapsed
    /// game time always produces the same market (AC-04).
    /// </summary>
    private void ApplyMarketHour(long time)
    {
        var stations = Enumerable.Range(0, _objects.Count)
            .Where(index => _objects[index].ObjectType == SpaceObjectType.Station)
            .OrderBy(index => _objects[index].InitialMotion.ObjectId, StringComparer.Ordinal)
            .ToArray();

        foreach (int index in stations)
        {
            var station = _objects[index];
            if (!TryGetMarket(station, out var profile, out var economy)) continue;

            var stock = station.Inventory.IsDefault
                ? ImmutableArray<StationInventoryItemRuntime>.Empty.ToBuilder()
                : station.Inventory.ToBuilder();
            ApplyHourlyConsumption(stock, economy);
            ApplyProfileBatch(stock, station, profile, economy);

            long budget = station.MarketBudgetCredits ?? 0;
            long maxBudget = MarketMaxBudget(profile, station.StationSize);
            long applied = HourlyBudgetGrant(time, budget, maxBudget, economy.BudgetRegenerationDivisorPerDay);
            long newBudget = checked(budget + applied);

            _objects[index] = station with
            {
                Inventory = stock.ToImmutable(),
                MarketBudgetCredits = newBudget,
                // The budget is the spendable slice of Credits, never a separate purse: an existing
                // reserve is made available first and only the shortfall is topped up, so a cap can
                // never destroy revenue the station already earned (AC-05).
                Credits = Math.Max(station.Credits, newBudget),
            };
        }
    }

    /// <summary>Demand that exists independently of any production: each stock drops by min(stock, rate).</summary>
    private void ApplyHourlyConsumption(
        ImmutableArray<StationInventoryItemRuntime>.Builder stock,
        StationMarketEconomyDefinition economy)
    {
        foreach (var rate in economy.HourlyConsumption.OrderBy(entry => entry.ItemTypeId, StringComparer.Ordinal))
        {
            int slot = FindStockSlot(stock, rate.ItemTypeId);
            if (slot < 0) continue;
            long taken = Math.Min(stock[slot].StockQuantity, rate.Quantity);
            if (taken <= 0) continue;
            stock[slot] = stock[slot] with { StockQuantity = stock[slot].StockQuantity - taken };
        }
    }

    /// <summary>
    /// The profile's single hourly batch: every input must be present AND every output must fit,
    /// otherwise nothing at all happens. A skipped batch is never retried or accumulated — a
    /// shortage hour is simply lost (AC-02/03). Stations whose production comes from recipes
    /// (Modules source) never run a batch here.
    /// </summary>
    private void ApplyProfileBatch(
        ImmutableArray<StationInventoryItemRuntime>.Builder stock,
        SpaceObjectRuntime station,
        StationMarketProfileDefinition profile,
        StationMarketEconomyDefinition economy)
    {
        if (economy.ProductionSource != StationMarketProductionSource.Profile) return;
        if (economy.HourlyInputs.IsDefaultOrEmpty && economy.HourlyOutputs.IsDefaultOrEmpty) return;

        foreach (var input in economy.HourlyInputs)
        {
            int slot = FindStockSlot(stock, input.ItemTypeId);
            if (slot < 0 || stock[slot].StockQuantity < input.Quantity) return;
        }
        foreach (var output in economy.HourlyOutputs)
        {
            if (!TryMarketLimits(profile, economy, station.StationSize, output.ItemTypeId, out var limits)) return;
            int slot = FindStockSlot(stock, output.ItemTypeId);
            long have = slot < 0 ? 0 : stock[slot].StockQuantity;
            if (checked(have + output.Quantity) > limits.MaxStock) return;
        }

        // Inputs and outputs are disjoint (TK-0002), so a single pass commits the whole batch.
        foreach (var input in economy.HourlyInputs.OrderBy(entry => entry.ItemTypeId, StringComparer.Ordinal))
        {
            int slot = FindStockSlot(stock, input.ItemTypeId);
            stock[slot] = stock[slot] with { StockQuantity = stock[slot].StockQuantity - input.Quantity };
        }
        foreach (var output in economy.HourlyOutputs.OrderBy(entry => entry.ItemTypeId, StringComparer.Ordinal))
            AddStock(stock, _registry.ItemTypes.GetIndex(output.ItemTypeId), output.Quantity);
    }

    /// <summary>
    /// The hour's slice of one day's replenishment. A full day grants floor(maxBudget/divisor) in
    /// total — NOT that amount every hour: the day's grant is distributed across its 24 hourly
    /// boundaries by integer staircase, so no fractional credits appear and the 24 slices sum back
    /// to exactly the daily figure. Midnight closes the previous day (hour 24). Grants that would
    /// exceed the cap are discarded, never carried over (AC-05).
    /// </summary>
    private static long HourlyBudgetGrant(long time, long budget, long maxBudget, int divisorPerDay)
    {
        long headroom = maxBudget - budget;
        if (headroom <= 0 || divisorPerDay <= 0) return 0;
        long dailyGrant = maxBudget / divisorPerDay;
        if (dailyGrant <= 0) return 0;
        long msInDay = time % GameCalendar.DayMs;
        long hour = msInDay == 0 ? 24 : msInDay / GameCalendar.HourMs;
        long grant = checked(hour * dailyGrant) / 24 - checked((hour - 1) * dailyGrant) / 24;
        return Math.Min(grant, headroom);
    }

    /// <summary>
    /// Hands completed recipe output that did not fit at completion time over to the station as
    /// soon as room appears, exactly once per unit (requirements 3844-3868). Only an Active module
    /// unloads; a disabled one keeps its remainder until it is switched back on.
    /// </summary>
    private void FlushPendingOutputs()
    {
        for (int index = 0; index < _objects.Count; index++)
        {
            var station = _objects[index];
            if (station.ProducingModules.IsDefaultOrEmpty) continue;
            if (!TryGetMarket(station, out var profile, out var economy)) continue;

            var modules = station.ProducingModules.ToBuilder();
            var stock = station.Inventory.IsDefault
                ? ImmutableArray<StationInventoryItemRuntime>.Empty.ToBuilder()
                : station.Inventory.ToBuilder();
            bool changed = false;
            for (int m = 0; m < modules.Count; m++)
            {
                var module = modules[m];
                if (!module.Active || module.PendingOutput.IsDefaultOrEmpty) continue;
                var pending = module.PendingOutput.ToBuilder();
                bool moduleChanged = false;
                for (int p = pending.Count - 1; p >= 0; p--)
                {
                    var remainder = pending[p];
                    if (!TryMarketLimits(profile, economy, station, remainder.ItemTypeIndex, out var limits)) continue;
                    int slot = FindStockSlot(stock, remainder.ItemTypeIndex);
                    long have = slot < 0 ? 0 : stock[slot].StockQuantity;
                    long unloaded = Math.Min(limits.MaxStock - have, remainder.StockQuantity);
                    if (unloaded <= 0) continue;
                    AddStock(stock, remainder.ItemTypeIndex, unloaded);
                    long left = remainder.StockQuantity - unloaded;
                    if (left <= 0) pending.RemoveAt(p);
                    else pending[p] = remainder with { StockQuantity = left };
                    moduleChanged = true;
                }
                if (!moduleChanged) continue;
                modules[m] = module with { PendingOutput = pending.ToImmutable() };
                changed = true;
            }

            if (changed)
                _objects[index] = station with { Inventory = stock.ToImmutable(), ProducingModules = modules.ToImmutable() };
        }
    }

    private int FindStockSlot(ImmutableArray<StationInventoryItemRuntime>.Builder stock, string itemTypeId) =>
        _registry.ItemTypes.Contains(itemTypeId) ? FindStockSlot(stock, _registry.ItemTypes.GetIndex(itemTypeId)) : -1;

    private static int FindStockSlot(ImmutableArray<StationInventoryItemRuntime>.Builder stock, int itemTypeIndex)
    {
        for (int i = 0; i < stock.Count; i++)
            if (stock[i].ItemTypeIndex == itemTypeIndex) return i;
        return -1;
    }

    private static void AddStock(ImmutableArray<StationInventoryItemRuntime>.Builder stock, int itemTypeIndex, long amount)
    {
        int slot = FindStockSlot(stock, itemTypeIndex);
        if (slot < 0) stock.Add(new StationInventoryItemRuntime(itemTypeIndex, amount));
        else stock[slot] = stock[slot] with { StockQuantity = checked(stock[slot].StockQuantity + amount) };
    }

    /// <summary>
    /// Money the station receives from the player (Buy/Refuel) lands in Credits in full and makes
    /// the budget available again only up to its cap. Income that would exceed the cap is kept as
    /// Credits rather than discarded, so port fees and dialogue payouts stay funded (AC-05).
    /// </summary>
    private long? ReplenishBudgetFromIncome(SpaceObjectRuntime station, long income)
    {
        if (!TryGetMarket(station, out var profile, out _)) return station.MarketBudgetCredits;
        long budget = station.MarketBudgetCredits ?? 0;
        long headroom = MarketMaxBudget(profile, station.StationSize) - budget;
        return headroom <= 0 ? budget : checked(budget + Math.Min(headroom, income));
    }

    /// <summary>Recipe outputs collapsed per item type, so a recipe listing an item twice yields one bounded amount.</summary>
    private Dictionary<int, long> AggregateRecipeOutputs(RecipeDefinition recipe)
    {
        var totals = new Dictionary<int, long>();
        foreach (var output in recipe.Outputs)
        {
            int itemTypeIndex = _registry.ItemTypes.GetIndex(output.ItemTypeId);
            totals[itemTypeIndex] = totals.TryGetValue(itemTypeIndex, out long already)
                ? checked(already + output.Count)
                : output.Count;
        }
        return totals;
    }

    /// <summary>
    /// Resolves the station's trading budget while the candidate world is still being built, so an
    /// invalid save throws before anything is replaced. A new game starts at min(Credits, MaxBudget);
    /// a save must carry the value explicitly and within that same range. The field is meaningless
    /// without a configured economy and is rejected there rather than silently ignored.
    /// </summary>
    private static long? ResolveMarketBudget(
        SpaceObjectData obj,
        StationMarketProfileDefinition? profile,
        StationSize stationSize,
        long credits,
        bool loadingSave,
        int saveFormatVersion)
    {
        if (profile?.Economy is null)
        {
            if (obj.MarketBudgetCredits is not null)
                throw new ScenarioException($"Object '{obj.ObjectId}', marketBudgetCredits requires a market profile with a configured economy. Save was not modified.");
            return null;
        }

        long cap = Math.Min(credits, MarketMaxBudget(profile, stationSize));
        if (!loadingSave)
        {
            // A New Game scenario states starting stock and Credits, never a mid-session budget.
            if (obj.MarketBudgetCredits is not null)
                throw new ScenarioException($"Station '{obj.ObjectId}', marketBudgetCredits is only valid in a save. Save was not modified.");
            return cap;
        }

        if (saveFormatVersion < 9)
            throw new ScenarioException($"Station '{obj.ObjectId}', market economy requires save format 9. Save was not modified.");
        if (obj.MarketBudgetCredits is not { } budget)
            throw new ScenarioException($"Station '{obj.ObjectId}', marketBudgetCredits is required for a save. Save was not modified.");
        if (budget < 0 || budget > cap)
            throw new ScenarioException($"Station '{obj.ObjectId}', marketBudgetCredits must be within [0, {cap}]. Save was not modified.");
        return budget;
    }

    /// <summary>
    /// Checks the fully built candidate world before it replaces the running one: stock bounds and
    /// target coverage, a single production source per station, and a well-formed pending-output
    /// remainder. Every failure is a <see cref="ScenarioException"/> naming station, profile and field.
    /// </summary>
    private void ValidateMarketWorld(IReadOnlyList<SpaceObjectRuntime> objects)
    {
        foreach (var station in objects)
        {
            if (!TryGetMarket(station, out var profile, out var economy))
            {
                if (!station.ProducingModules.IsDefaultOrEmpty &&
                    station.ProducingModules.Any(module => !module.PendingOutput.IsDefaultOrEmpty))
                    throw new ScenarioException($"Object '{station.InitialMotion.ObjectId}', pendingOutput requires a market profile with a configured economy. Save was not modified.");
                continue;
            }

            string stationId = station.InitialMotion.ObjectId;
            string profileId = profile.TypeId;

            foreach (var item in station.Inventory.IsDefault ? [] : station.Inventory)
            {
                var itemType = _registry.ItemTypes.GetDefinition(item.ItemTypeIndex);
                // Fuel is sold from its own refuel stock and never takes part in cargo flow.
                if (itemType.StorageKind == ItemStorageKind.FuelTank) continue;
                if (!TryMarketLimits(profile, economy, station.StationSize, itemType.TypeId, out var limits))
                    throw new ScenarioException($"Station '{stationId}', market profile '{profileId}', inventory item '{itemType.TypeId}' has no stockTargets entry. Save was not modified.");
                if (item.StockQuantity < 0 || item.StockQuantity > limits.MaxStock)
                    throw new ScenarioException($"Station '{stationId}', market profile '{profileId}', inventory item '{itemType.TypeId}': stock {item.StockQuantity} is outside [0, {limits.MaxStock}]. Save was not modified.");
            }

            var producingModules = station.ProducingModules.IsDefault
                ? ImmutableArray<StationProducingModuleRuntime>.Empty
                : station.ProducingModules;

            if (economy.ProductionSource == StationMarketProductionSource.Profile)
            {
                // A profile batch is the single source of output: a live producing module would
                // double-count it, so this is reported rather than silently disabled (A-04).
                foreach (var module in producingModules)
                {
                    if (!module.Active && module.NextProductionDueGameTimeMs is null && module.PendingOutput.IsDefaultOrEmpty)
                        continue;
                    string factoryId = _registry.FactoryTypes.GetDefinition(module.FactoryTypeIndex).TypeId;
                    throw new ScenarioException($"Station '{stationId}', market profile '{profileId}', productionSource Profile conflicts with producing module '{factoryId}'. Save was not modified.");
                }
            }
            else
            {
                foreach (var module in producingModules)
                {
                    string factoryId = _registry.FactoryTypes.GetDefinition(module.FactoryTypeIndex).TypeId;
                    var recipe = _registry.FactoryTypes.GetDefinition(module.FactoryTypeIndex).Recipe;
                    foreach (var material in recipe.Inputs.Concat(recipe.Outputs))
                    {
                        var itemType = _registry.ItemTypes.GetDefinition(_registry.ItemTypes.GetIndex(material.ItemTypeId));
                        if (itemType.StorageKind != ItemStorageKind.Cargo)
                            throw new ScenarioException($"Station '{stationId}', market profile '{profileId}', producing module '{factoryId}' references non-cargo item '{material.ItemTypeId}'. Save was not modified.");
                        if (!TryMarketLimits(profile, economy, station.StationSize, material.ItemTypeId, out _))
                            throw new ScenarioException($"Station '{stationId}', market profile '{profileId}', producing module '{factoryId}' item '{material.ItemTypeId}' has no stockTargets entry. Save was not modified.");
                    }
                    // Even an inactive recipe reserves its inputs: letting hourlyConsumption cover the
                    // same item would double-spend it the moment the module is switched on.
                    foreach (var input in recipe.Inputs)
                        if (economy.HourlyConsumption.Any(entry => string.Equals(entry.ItemTypeId, input.ItemTypeId, StringComparison.Ordinal)))
                            throw new ScenarioException($"Station '{stationId}', market profile '{profileId}', hourlyConsumption item '{input.ItemTypeId}' is also an input of producing module '{factoryId}'. Save was not modified.");
                }
            }

            foreach (var module in producingModules)
            {
                if (module.PendingOutput.IsDefaultOrEmpty) continue;
                string factoryId = _registry.FactoryTypes.GetDefinition(module.FactoryTypeIndex).TypeId;
                if (module.NextProductionDueGameTimeMs is not null)
                    throw new ScenarioException($"Station '{stationId}', producing module '{factoryId}' has both pendingOutput and nextProductionDueGameTimeMs. Save was not modified.");
                var batch = AggregateRecipeOutputs(_registry.FactoryTypes.GetDefinition(module.FactoryTypeIndex).Recipe);
                var seen = new HashSet<int>();
                foreach (var remainder in module.PendingOutput)
                {
                    string itemTypeId = _registry.ItemTypes.GetDefinition(remainder.ItemTypeIndex).TypeId;
                    if (remainder.StockQuantity <= 0)
                        throw new ScenarioException($"Station '{stationId}', producing module '{factoryId}', pendingOutput item '{itemTypeId}': quantity must be positive. Save was not modified.");
                    if (!seen.Add(remainder.ItemTypeIndex))
                        throw new ScenarioException($"Station '{stationId}', producing module '{factoryId}', pendingOutput contains duplicate item '{itemTypeId}'. Save was not modified.");
                    if (!batch.TryGetValue(remainder.ItemTypeIndex, out long produced))
                        throw new ScenarioException($"Station '{stationId}', producing module '{factoryId}', pendingOutput item '{itemTypeId}' is not a recipe output. Save was not modified.");
                    if (remainder.StockQuantity > produced)
                        throw new ScenarioException($"Station '{stationId}', producing module '{factoryId}', pendingOutput item '{itemTypeId}' exceeds one recipe batch ({produced}). Save was not modified.");
                }
            }
        }
    }
}
