using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private const long MealIntervalMs = 12 * GameCalendar.HourMs;
    private long _processedWorldTimeMs;
    private long _processedSimulationTimeMs;

    private void AdvanceWorldTo(long gameTimeMs, long simulationTimeMs)
    {
        long fromCalendar = _processedWorldTimeMs;
        long fromSimulation = _processedSimulationTimeMs;
        // Both timestamps come from the same clock. Map each calendar boundary onto
        // the motion interval so economic events cannot accelerate ship cycles.
        long MotionAt(long calendarTime) => gameTimeMs == fromCalendar ? simulationTimeMs :
            fromSimulation + (long)((decimal)(calendarTime - fromCalendar) *
                (simulationTimeMs - fromSimulation) / (gameTimeMs - fromCalendar));
        // Process (previous, target] in order; repeated snapshots at the same time
        // cannot repeat a meal, including midnight. Loading establishes the cursor.
        while (_processedWorldTimeMs < gameTimeMs)
        {
            // Starting production changes the market at the interval's start. Commit it separately
            // from its completion, regardless of whether a snapshot falls between these two moments.
            CaptureMarketStateBeforeBoundary();
            // A blocked remainder must be handed over before a module may take on a new cycle,
            // so a freed slot is reused on the same pass rather than a later one.
            FlushPendingOutputs();
            StartAvailableProduction(_processedWorldTimeMs);
            CommitChangedMarketRevisions();
            CaptureMarketStateBeforeBoundary();
            long nextMeal = _processedWorldTimeMs - _processedWorldTimeMs % MealIntervalMs;
            nextMeal = nextMeal > long.MaxValue - MealIntervalMs ? long.MaxValue : nextMeal + MealIntervalMs;
            long next = gameTimeMs;
            IncludeBoundary(nextMeal);
            IncludeBoundary(NextPortFeeTime());
            IncludeBoundary(NextContractDeadline());
            IncludeBoundary(NextProductionTime());
            IncludeBoundary(NextMarketHourTime());

            // Only boundaries in (processed, target] may move the cursor. A stale
            // schedule must not rewind motion or replay an already processed time.
            void IncludeBoundary(long boundary)
            {
                if (boundary > _processedWorldTimeMs && boundary < next) next = boundary;
            }

            // Keep equal-time effects ordered: start above, motion, completion, market hour,
            // pending unload, meal, fee, deadline, then commit the calendar cursor.
            AdvanceMotionTo(MotionAt(next));
            CompleteProduction(next);
            if (next != long.MaxValue && next % GameCalendar.HourMs == 0) ApplyMarketHour(next);
            FlushPendingOutputs();
            if (next == nextMeal && next % MealIntervalMs == 0) ConsumeScheduledRations(next);
            RenewPortFees(next);
            ApplyContractDeadlines(next);
            CommitChangedMarketRevisions();
            _processedWorldTimeMs = next;
        }
        AdvanceMotionTo(simulationTimeMs);
        _processedSimulationTimeMs = simulationTimeMs;
    }

    // Market state of every station at the start of the current boundary (object index, stock rows and
    // trading budget); reused across boundaries so the calendar loop allocates nothing extra.
    private readonly List<(int Index, ImmutableArray<StationInventoryItemRuntime> Stock, long? Budget)> _marketStateBeforeBoundary = new();

    private void CaptureMarketStateBeforeBoundary()
    {
        _marketStateBeforeBoundary.Clear();
        for (int i = 0; i < _objects.Count; i++)
        {
            var obj = _objects[i];
            if (obj.ObjectType == SpaceObjectType.Station)
                _marketStateBeforeBoundary.Add((i, obj.Inventory, obj.MarketBudgetCredits));
        }
    }

    /// <summary>
    /// Everything one boundary did to a station's market (production, consumption, pending unload and
    /// budget regeneration, over any number of rows) is one transaction: at most one revision per station.
    /// Station Credits alone (port fees, dialogue payouts) are not market state. A station already at the
    /// maximum revision keeps it, but its quotes are still invalidated.
    /// </summary>
    private void CommitChangedMarketRevisions()
    {
        foreach (var (i, stockBefore, budgetBefore) in _marketStateBeforeBoundary)
        {
            if (i >= _objects.Count) continue;
            var station = _objects[i];
            if (station.ObjectType != SpaceObjectType.Station) continue;
            if (budgetBefore == station.MarketBudgetCredits && SameStock(stockBefore, station.Inventory)) continue;

            if (station.MarketRevision == long.MaxValue)
            {
                OnMarketRevisionCommitted(station.InitialMotion.ObjectId);
                continue;
            }

            long nextRevision = PrepareMarketRevision(i);
            CommitMarketRevision(station.InitialMotion.ObjectId, nextRevision);
        }
    }

    private static bool SameStock(ImmutableArray<StationInventoryItemRuntime> before, ImmutableArray<StationInventoryItemRuntime> after)
    {
        if (before == after || (before.IsDefaultOrEmpty && after.IsDefaultOrEmpty)) return true;
        if (before.IsDefault || after.IsDefault || before.Length != after.Length) return false;
        for (int i = 0; i < before.Length; i++)
        {
            if (before[i].ItemTypeIndex != after[i].ItemTypeIndex || before[i].StockQuantity != after[i].StockQuantity)
                return false;
        }

        return true;
    }

    private void ConsumeScheduledRations(long time)
    {
        _economyTime = _economyTime with { MissingRations = 0 };
        for (int i = 0; i < _objects.Count; i++)
        {
            var ship = _objects[i];
            if (ship.IsDestroyed || ship.ObjectType != SpaceObjectType.PlayerShip) continue;
            long needed = (ship.Crew.IsDefault ? 0 : ship.Crew.Length)
                + (ship.Passengers.IsDefault ? 0 : ship.Passengers.Length);
            if (needed == 0) continue;
            var modules = ship.Modules.ToBuilder();
            if (_registry.ItemTypes.Contains("item.food-rations"))
            {
                int ration = _registry.ItemTypes.GetIndex("item.food-rations");
                for (int m = 0; m < modules.Count && needed > 0; m++)
                {
                    var module = modules[m];
                    var cargo = module.Cargo.ToBuilder();
                    for (int c = cargo.Count - 1; c >= 0 && needed > 0; c--)
                    {
                        if (cargo[c].ItemTypeIndex != ration) continue;
                        long consumed = Math.Min(cargo[c].Quantity, needed);
                        needed -= consumed;
                        long left = cargo[c].Quantity - consumed;
                        if (left == 0) cargo.RemoveAt(c);
                        else cargo[c] = cargo[c] with { Quantity = left };
                    }
                    var remainingCargo = cargo.ToImmutable();
                    modules[m] = module with
                    {
                        Cargo = remainingCargo,
                        AvailableCapacityKg = ComputeAvailableCapacityKg(
                            _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex), remainingCargo)
                    };
                }
            }
            _objects[i] = ship with { Modules = modules.ToImmutable() };
            _economyTime = _economyTime with { MissingRations = _economyTime.MissingRations + needed };
            if (needed > 0)
                RecordShipEvent(ship.InitialMotion.ObjectId, "", ShipEventTypes.RationsShortage,
                    "insufficient_rations", time);
        }
    }
}
