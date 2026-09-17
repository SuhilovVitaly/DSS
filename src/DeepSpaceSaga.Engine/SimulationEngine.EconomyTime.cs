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
            StartAvailableProduction(_processedWorldTimeMs);
            long nextMeal = _processedWorldTimeMs - _processedWorldTimeMs % MealIntervalMs;
            nextMeal = nextMeal > long.MaxValue - MealIntervalMs ? long.MaxValue : nextMeal + MealIntervalMs;
            long next = Math.Min(Math.Min(Math.Min(gameTimeMs, nextMeal), NextPortFeeTime()), NextContractDeadline());
            next = Math.Min(next, NextProductionTime());
            AdvanceMotionTo(MotionAt(next));
            CompleteProduction(next);
            if (next == nextMeal && next % MealIntervalMs == 0) ConsumeScheduledRations(next);
            RenewPortFees(next);
            ApplyContractDeadlines(next);
            _processedWorldTimeMs = next;
        }
        AdvanceMotionTo(simulationTimeMs);
        _processedSimulationTimeMs = simulationTimeMs;
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
                    modules[m] = module with { Cargo = remainingCargo,
                        AvailableCapacityKg = ComputeAvailableCapacityKg(
                            _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex), remainingCargo) };
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
