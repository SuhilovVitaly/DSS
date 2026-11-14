using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

public partial class SimulationEngine
{
    // Carrier surface state keyed by ship object id. Minimal for US-0001: hull current + revision.
    // Absence of an entry means the hull is full (derived from the ship class profile), never a
    // fabricated carrier. Later stories extend this with hangar drones, crew and magazines.
    private readonly Dictionary<string, CarrierHullRuntime> _carrierHull = new(StringComparer.Ordinal);

    private sealed record CarrierHullRuntime(int HullCurrent, int Revision);

    private CarrierOperationsSnapshot? BuildCarrierOperationsProjection()
    {
        if (string.IsNullOrWhiteSpace(PlayerShipObjectId))
            return null;

        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (ship is null || ship.ShipClassId is null || !_registry.ShipClasses.Contains(ship.ShipClassId))
            return null;

        var shipClass = _registry.ShipClasses.GetDefinition(_registry.ShipClasses.GetIndex(ship.ShipClassId));
        if (shipClass.HullMax is not { } hullMax)
            return null; // class carries no carrier profile -> unsupported, not a free carrier

        var hull = _carrierHull.TryGetValue(ship.InitialMotion.ObjectId, out var stored)
            ? stored
            : new CarrierHullRuntime(hullMax, Revision: 0);

        return new CarrierOperationsSnapshot(
            ShipId: ship.InitialMotion.ObjectId,
            ShipClassId: ship.ShipClassId,
            Name: ship.Name ?? ship.CaptainDisplayName ?? string.Empty,
            Revision: hull.Revision,
            HullCurrent: hull.HullCurrent,
            HullMax: hullMax,
            IsDestroyed: ship.IsDestroyed,
            AppearanceId: shipClass.AppearanceId,
            Hangars: BuildHangarSlots(ship),
            Habitation: null); // US-0002 fills this
    }

    private ImmutableArray<HangarSlotSnapshot> BuildHangarSlots(SpaceObjectRuntime ship)
    {
        var slots = ImmutableArray.CreateBuilder<HangarSlotSnapshot>();
        foreach (var module in ship.Modules)
        {
            var moduleType = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
            if (moduleType.DroneCapacity is not { } capacity || capacity <= 0)
                continue;
            for (int i = 0; i < capacity; i++)
                slots.Add(new HangarSlotSnapshot(module.ModuleId, i)); // DroneId arrives in US-0003
        }
        return slots.ToImmutable();
    }

    /// <summary>
    /// A hangar bay is ready to launch when it is structurally intact, powered on and
    /// operationally ready. Losing readiness never removes placed drones — it only blocks a
    /// new sortie from that bay.
    /// </summary>
    internal bool IsHangarReady(string moduleId)
    {
        if (string.IsNullOrWhiteSpace(PlayerShipObjectId))
            return false;

        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (ship is null)
            return false;

        var module = ship.Modules.FirstOrDefault(m => m.ModuleId == moduleId);
        return module is not null && IsHangarReady(module);
    }

    private bool IsHangarReady(InstalledModuleRuntime module)
    {
        var moduleType = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
        if (moduleType.DroneCapacity is not { } capacity || capacity <= 0)
            return false;

        return module.StructurePoints > 0 &&
               !module.Destroyed &&
               string.Equals(module.PowerState, "On", StringComparison.Ordinal) &&
               string.Equals(module.OperationalState, "Ready", StringComparison.Ordinal);
    }

    /// <summary>Test seam: set a ship's carrier hull current (US-0001 independent-hull projection).</summary>
    internal void SetCarrierHullCurrentForTests(string objectId, int hullCurrent)
    {
        lock (_worldStateLock)
        {
            _carrierHull[objectId] = new CarrierHullRuntime(hullCurrent, Revision: 0);
        }
    }
}
