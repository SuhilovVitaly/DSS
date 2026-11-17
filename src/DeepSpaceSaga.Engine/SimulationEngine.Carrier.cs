using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public partial class SimulationEngine
{
    // Carrier surface state keyed by ship object id. Minimal for US-0001: hull current + revision.
    // Absence of an entry means the hull is full (derived from the ship class profile), never a
    // fabricated carrier. Later stories extend this with hangar drones, crew and magazines.
    private readonly Dictionary<string, CarrierHullRuntime> _carrierHull = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SortieSettlementData> _carrierSettlement = new(StringComparer.Ordinal);

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
            Crew: BuildCrewProjection(ship),
            OwnedDrones: BuildOwnedWingProjection(ship),
            CabinReservation: BuildCabinReservationProjection(ship));
    }

    /// <summary>
    /// Persisted carrier surface for the player ship (EP-0029 US-0007). Null for a ship without a
    /// carrier hull profile — a legacy ship is never granted a fabricated carrier. Owned wing is the
    /// authoritative persistent wing (tombstones included); the last settlement receipt is kept so a
    /// reload never re-applies its delta.
    /// </summary>
    private CarrierStateData? BuildCarrierStateData(SpaceObjectRuntime ship)
    {
        if (ship.ShipClassId is null || !_registry.ShipClasses.Contains(ship.ShipClassId))
            return null;
        var shipClass = _registry.ShipClasses.GetDefinition(_registry.ShipClasses.GetIndex(ship.ShipClassId));
        if (shipClass.HullMax is not { } hullMax)
            return null;

        string shipId = ship.InitialMotion.ObjectId;
        var hull = _carrierHull.TryGetValue(shipId, out var stored) ? stored : new CarrierHullRuntime(hullMax, Revision: 0);
        var wing = GetOwnedWing(shipId);
        return new CarrierStateData(
            SchemaVersion: CarrierStateData.CurrentSchemaVersion,
            Revision: hull.Revision,
            HullCurrent: hull.HullCurrent,
            ReservedCabins: ReservedPrisonerCabins,
            OwnedDrones: wing.IsDefaultOrEmpty ? null : wing.ToArray(),
            LastSettlement: _carrierSettlement.TryGetValue(shipId, out var settlement) ? settlement : null,
            LastRearm: _lastRearm);
    }

    private ImmutableArray<HangarSlotSnapshot> BuildHangarSlots(SpaceObjectRuntime ship)
    {
        var wing = GetOwnedWing(ship.InitialMotion.ObjectId);
        var droneBySlot = wing.Where(d => !d.Destroyed).ToDictionary(d => (d.HangarModuleId, d.SlotIndex), d => d.DroneId);

        var slots = ImmutableArray.CreateBuilder<HangarSlotSnapshot>();
        foreach (var module in ship.Modules)
        {
            var moduleType = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
            if (moduleType.DroneCapacity is not { } capacity || capacity <= 0)
                continue;
            for (int i = 0; i < capacity; i++)
            {
                droneBySlot.TryGetValue((module.ModuleId, i), out var droneId);
                slots.Add(new HangarSlotSnapshot(module.ModuleId, i, droneId));
            }
        }
        return slots.ToImmutable();
    }

    private const int ReservedPrisonerCabins = 2;

    private ImmutableArray<CrewMemberSnapshot> BuildCrewProjection(SpaceObjectRuntime ship)
    {
        if (ship.Crew.IsDefaultOrEmpty)
            return ImmutableArray<CrewMemberSnapshot>.Empty;

        var crew = ImmutableArray.CreateBuilder<CrewMemberSnapshot>(ship.Crew.Length);
        foreach (var member in ship.Crew)
        {
            crew.Add(new CrewMemberSnapshot(
                Id: member.Id,
                DisplayName: member.DisplayName,
                Gender: ParseCrewGender(member.Gender),
                PortraitAssetId: member.PortraitAssetId,
                Role: ParseCrewRole(member.Role),
                Accuracy: member.AccuracySkill,
                Maneuver: member.ManeuverabilitySkill,
                Initiative: member.Initiative,
                SensorRangeBonusPercent: member.SensorRangeBonusPercent));
        }
        return crew.ToImmutable();
    }

    private CabinReservationSnapshot BuildCabinReservationProjection(SpaceObjectRuntime ship)
    {
        int operationalCabins = 0;
        foreach (var module in ship.Modules)
        {
            var moduleType = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
            if (moduleType.CabinesCount is { } cabins && cabins > 0 &&
                module.StructurePoints > 0 && !module.Destroyed)
            {
                operationalCabins += cabins;
            }
        }

        int occupied = (ship.Crew.IsDefaultOrEmpty ? 0 : ship.Crew.Length) + (ship.Passengers.IsDefault ? 0 : ship.Passengers.Length);
        int reserved = ReservedPrisonerCabins;
        return new CabinReservationSnapshot(
            TotalCabins: operationalCabins,
            OccupiedCabins: occupied,
            ReservedCabins: reserved,
            FreeCabins: Math.Max(0, operationalCabins - occupied - reserved),
            PrisonerCount: 0);
    }

    private static CrewGender ParseCrewGender(string? value) =>
        Enum.TryParse<CrewGender>(value, ignoreCase: true, out var gender) ? gender : CrewGender.Female;

    private static CrewRole? ParseCrewRole(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : Enum.TryParse<CrewRole>(value, ignoreCase: true, out var role) ? role : null;

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

    internal int GetCarrierRevision(string shipId) =>
        _carrierHull.TryGetValue(shipId, out var stored) ? stored.Revision : 0;

    internal int GetCarrierHullCurrent(string shipId, int hullMax) =>
        _carrierHull.TryGetValue(shipId, out var stored) ? stored.HullCurrent : hullMax;

    internal int GetCarrierHullMax(string shipId)
    {
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == shipId);
        if (ship?.ShipClassId is null || !_registry.ShipClasses.Contains(ship.ShipClassId)) return 0;
        return _registry.ShipClasses.GetDefinition(_registry.ShipClasses.GetIndex(ship.ShipClassId)).HullMax ?? 0;
    }

    internal int GetShipManeuver(string shipId)
    {
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == shipId);
        if (ship?.ShipClassId is null || !_registry.ShipClasses.Contains(ship.ShipClassId)) return 0;
        return _registry.ShipClasses.GetDefinition(_registry.ShipClasses.GetIndex(ship.ShipClassId)).TacticalManeuver ?? 0;
    }

    /// <summary>
    /// Validate the stored carrier surface against the candidate world before the running session is
    /// replaced (AC-03). Rejects an unknown schema, a hull outside the class profile, or an invalid
    /// persistent wing (duplicate ids/slots, unknown hangar, non-pilot, bad hull/ammo/decoy bounds).
    /// </summary>
    private void ValidateCarrierStateForLoad(GameStateData gs, IReadOnlyList<SpaceObjectRuntime> runtimeObjects)
    {
        var data = gs.SpaceObjects.FirstOrDefault(o =>
            string.Equals(o.ObjectId, gs.PlayerShipObjectId, StringComparison.OrdinalIgnoreCase));
        if (data?.CarrierState is not { } carrier)
            return;

        if (carrier.SchemaVersion != CarrierStateData.CurrentSchemaVersion)
            throw new ScenarioException($"Unsupported carrierState schemaVersion {carrier.SchemaVersion}.");
        if (carrier.Revision < 0)
            throw new ScenarioException("Carrier revision must be nonnegative.");
        if (carrier.LastSettlement is { } settlement && settlement.ConsumedTorpedoes < 0)
            throw new ScenarioException("Settlement consumedTorpedoes must be nonnegative.");

        var ship = runtimeObjects.FirstOrDefault(o =>
            string.Equals(o.InitialMotion.ObjectId, gs.PlayerShipObjectId, StringComparison.OrdinalIgnoreCase));
        if (ship is null)
            throw new ScenarioException("Player ship not found for carrier state.");
        int hullMax = ShipHullMax(ship);
        if (hullMax <= 0)
            throw new ScenarioException("Carrier state is attached to a ship without a carrier hull profile.");
        if (carrier.HullCurrent < 0 || carrier.HullCurrent > hullMax)
            throw new ScenarioException($"Carrier hullCurrent {carrier.HullCurrent} outside 0..{hullMax}.");

        OwnedWingValidator.Validate(ship, _registry, carrier.OwnedDrones ?? Array.Empty<OwnedDroneData>());
    }

    /// <summary>Restore the carrier surface from the loaded save; call with the world lock held.</summary>
    private void RestoreCarrierState(GameStateData gs)
    {
        _carrierHull.Clear();
        _ownedWing.Clear();
        _carrierSettlement.Clear();
        _rearmReceipts.Clear();
        _lastRearm = null;

        var data = gs.SpaceObjects.FirstOrDefault(o =>
            string.Equals(o.ObjectId, gs.PlayerShipObjectId, StringComparison.OrdinalIgnoreCase));
        if (data?.CarrierState is not { } carrier)
            return;

        string shipId = gs.PlayerShipObjectId;
        _carrierHull[shipId] = new CarrierHullRuntime(carrier.HullCurrent, carrier.Revision);
        if (carrier.OwnedDrones is { Count: > 0 } drones)
            _ownedWing[shipId] = drones.ToImmutableArray();
        if (carrier.LastSettlement is { } settlement)
            _carrierSettlement[shipId] = settlement;
        if (carrier.LastRearm is { } lastRearm)
        {
            _lastRearm = lastRearm;
            _rearmReceipts[lastRearm.CommandId] = new RearmDroneRequest(
                lastRearm.CommandId, lastRearm.ShipId, lastRearm.DroneId, lastRearm.Quantity, lastRearm.ExpectedRevision);
        }
    }

    private int ShipHullMax(SpaceObjectRuntime ship)
    {
        if (ship.ShipClassId is null || !_registry.ShipClasses.Contains(ship.ShipClassId)) return 0;
        return _registry.ShipClasses.GetDefinition(_registry.ShipClasses.GetIndex(ship.ShipClassId)).HullMax ?? 0;
    }
}
