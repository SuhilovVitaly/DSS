using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

/// <summary>
/// EP-0034 US-0001: passenger capacity of the demo carrier. The split is derived on every projection from
/// the operational living quarters, the live crew, the people aboard and the hold rations; it is never
/// persisted, so a Load or another scenario cannot receive extra cabins from it.
/// </summary>
public sealed partial class SimulationEngine
{
    private const string FoodRationsItemTypeId = "item.food-rations";

    /// <summary>Persisted transport state plus the derived capacity; only demo sessions get the capacity.</summary>
    private DemoTransportSnapshot? BuildDemoTransportProjection()
    {
        var persisted = _demoCampaign.Transport;
        if (_demoCampaign.CatalogFingerprint is null)
            return persisted;
        var capacity = BuildPassengerCapacityProjection();
        var jobs = BuildStationJobsProjection(); // EP-0034 US-0006
        return capacity is null && jobs is null ? persisted
            : (persisted ?? new DemoTransportSnapshot()) with { PassengerCapacity = capacity, StationJobs = jobs };
    }

    private PassengerCapacitySnapshot? BuildPassengerCapacityProjection()
    {
        if (string.IsNullOrWhiteSpace(PlayerShipObjectId))
            return null;
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (ship is null || ship.IsDestroyed || ship.ShipClassId is null || !_registry.ShipClasses.Contains(ship.ShipClassId) ||
            _registry.ShipClasses.GetDefinition(_registry.ShipClasses.GetIndex(ship.ShipClassId)).HullMax is null)
            return null; // no carrier profile -> no capacity, never a fabricated one

        // Same operational rule as the carrier cabin reservation: intact, non-destroyed living modules.
        int total = 0;
        foreach (var module in ship.Modules)
        {
            var moduleType = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
            if (moduleType.CabinesCount is > 0 && module.StructurePoints > 0 && !module.Destroyed)
                total = checked(total + moduleType.CabinesCount.Value);
        }

        int crew = ship.Crew.IsDefault ? 0 : ship.Crew.Length;
        int passengers = ship.Passengers.IsDefault ? 0 : ship.Passengers.Length;
        int reserved = ReservedPrisonerCabins;
        long committed = (long)crew + passengers + reserved;
        int free = (int)Math.Max(0, total - committed);

        string? reason = null;
        if (committed > total)
        {
            reason = PassengerCapacityReasons.CabinsOvercommitted;
        }
        else
        {
            // Life support: the hold must feed everyone aboard plus every free cabin for one calendar day.
            long mealsPerDay = GameCalendar.DayMs / MealIntervalMs;
            long needed = checked(((long)crew + passengers + free) * mealsPerDay);
            if (RationsAboard(ship) < needed)
                reason = PassengerCapacityReasons.InsufficientFoodRations;
        }

        return new PassengerCapacitySnapshot(total, crew, passengers, reserved, free, reason);
    }

    private long RationsAboard(SpaceObjectRuntime ship)
    {
        if (!_registry.ItemTypes.Contains(FoodRationsItemTypeId))
            return 0;
        int ration = _registry.ItemTypes.GetIndex(FoodRationsItemTypeId);
        long total = 0;
        foreach (var module in ship.Modules)
            foreach (var stack in module.Cargo.IsDefault ? [] : module.Cargo)
                if (stack.ItemTypeIndex == ration)
                    total = checked(total + stack.Quantity);
        return total;
    }
}
