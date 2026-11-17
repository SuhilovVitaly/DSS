using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private const string RearmReasonInvalidRequest = "invalid_request";
    private const string RearmReasonWrongShip = "wrong_ship";
    private const string RearmReasonCarrierNotReady = "carrier_not_ready";
    private const string RearmReasonBattleActive = "battle_active";
    private const string RearmReasonStaleRevision = "stale_revision";
    private const string RearmReasonUnknownDrone = "unknown_drone";
    private const string RearmReasonDestroyedDrone = "destroyed_drone";
    private const string RearmReasonHangarNotReady = "hangar_not_ready";
    private const string RearmReasonInvalidQuantity = "invalid_quantity";
    private const string RearmReasonNoCapacity = "no_capacity";
    private const string RearmReasonNoCargo = "no_cargo";
    private const string RearmReasonDuplicate = "duplicate_command";
    private const string RearmReasonIdentityConflict = "identity_conflict";

    private const string TorpedoItemTypeId = "item.drone-torpedo.t1";

    // CommandId -> request, for idempotency + conflict detection (in-memory). The last committed
    // rearm is persisted via CarrierStateData.LastRearm so a reload never re-spends it.
    private readonly Dictionary<string, RearmDroneRequest> _rearmReceipts = new(StringComparer.Ordinal);
    private RearmReceiptData? _lastRearm;

    public CarrierCommandResponse RearmDrone(RearmDroneRequest request)
    {
        if (request is null) return RejectRearm("", RearmReasonInvalidRequest, 0);
        lock (_worldStateLock)
        {
            int revision = GetCarrierRevision(request.ShipId);
            if (string.IsNullOrWhiteSpace(request.CommandId))
                return RejectRearm(request.CommandId, RearmReasonInvalidRequest, revision);
            if (_rearmReceipts.TryGetValue(request.CommandId, out var prior))
            {
                if (prior != request)
                    return RejectRearm(request.CommandId, RearmReasonIdentityConflict, revision);
                return RejectRearm(request.CommandId, RearmReasonDuplicate, revision);
            }

            var response = ApplyRearm(request);
            if (response.Accepted)
            {
                _rearmReceipts[request.CommandId] = request;
                _lastRearm = new RearmReceiptData(request.CommandId, request.ShipId, request.DroneId,
                    request.Quantity, request.ExpectedRevision, response.Revision, response.Transferred);
            }
            return response;
        }
    }

    private CarrierCommandResponse ApplyRearm(RearmDroneRequest request)
    {
        string shipId = request.ShipId;
        int revision = GetCarrierRevision(shipId);

        if (!string.Equals(shipId, PlayerShipObjectId, StringComparison.Ordinal))
            return RejectRearm(request.CommandId, RearmReasonWrongShip, revision);
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == shipId);
        if (ship is null || ship.IsDestroyed)
            return RejectRearm(request.CommandId, RearmReasonCarrierNotReady, revision);
        if (_tacticalBattleSession is not null || HasCapturedEncounter || _encounterStoragePending)
            return RejectRearm(request.CommandId, RearmReasonBattleActive, revision);
        if (request.ExpectedRevision != revision)
            return RejectRearm(request.CommandId, RearmReasonStaleRevision, revision);
        if (request.Quantity <= 0)
            return RejectRearm(request.CommandId, RearmReasonInvalidQuantity, revision);

        var wing = GetOwnedWing(shipId);
        int droneIndex = -1;
        for (int i = 0; i < wing.Length; i++)
            if (wing[i].DroneId == request.DroneId) { droneIndex = i; break; }
        if (droneIndex < 0)
            return RejectRearm(request.CommandId, RearmReasonUnknownDrone, revision);
        var drone = wing[droneIndex];
        if (drone.Destroyed)
            return RejectRearm(request.CommandId, RearmReasonDestroyedDrone, revision);
        if (!IsHangarReady(drone.HangarModuleId))
            return RejectRearm(request.CommandId, RearmReasonHangarNotReady, revision);

        int capacityRemaining = drone.AmmoCapacity - drone.AmmoCurrent;
        if (request.Quantity > capacityRemaining)
            return RejectRearm(request.CommandId, RearmReasonNoCapacity, revision);

        if (!_registry.ItemTypes.Contains(TorpedoItemTypeId))
            return RejectRearm(request.CommandId, RearmReasonNoCargo, revision);
        int torpedoIndex = _registry.ItemTypes.GetIndex(TorpedoItemTypeId);

        // Containers are drained in ordinal moduleId order; lots inside a container are FIFO.
        long available = 0;
        foreach (var module in ship.Modules)
            foreach (var stack in module.Cargo)
                if (stack.ItemTypeIndex == torpedoIndex)
                    available = checked(available + stack.Quantity);
        if (request.Quantity > available)
            return RejectRearm(request.CommandId, RearmReasonNoCargo, revision);

        // Validate-before-mutate is complete: apply the atomic transfer.
        long remaining = request.Quantity;
        var modules = ship.Modules;
        for (int mi = 0; mi < modules.Length && remaining > 0; mi++)
        {
            var module = modules[mi];
            int stackIndex = FindCargoStackIndex(module.Cargo, torpedoIndex);
            if (stackIndex < 0) continue;
            long take = Math.Min(remaining, module.Cargo[stackIndex].Quantity);
            var (updatedStack, _) = RemoveCargoCost(module.Cargo[stackIndex], take);
            var cargo = updatedStack is null ? module.Cargo.RemoveAt(stackIndex) : module.Cargo.SetItem(stackIndex, updatedStack);
            var moduleType = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
            modules = modules.SetItem(mi, module with
            {
                Cargo = cargo,
                AvailableCapacityKg = ComputeAvailableCapacityKg(moduleType, cargo),
            });
            remaining -= take;
        }

        int newRevision = revision + 1;
        _ownedWing[shipId] = wing.SetItem(droneIndex, drone with { AmmoCurrent = drone.AmmoCurrent + request.Quantity });
        _carrierHull[shipId] = new CarrierHullRuntime(GetCarrierHullCurrent(shipId, GetCarrierHullMax(shipId)), newRevision);
        for (int i = 0; i < _objects.Count; i++)
            if (_objects[i].InitialMotion.ObjectId == shipId)
                _objects[i] = _objects[i] with { Modules = modules };

        return new CarrierCommandResponse(
            Accepted: true,
            Reason: null,
            Revision: newRevision,
            Transferred: request.Quantity,
            Snapshot: BuildCarrierOperationsProjection());
    }

    private static CarrierCommandResponse RejectRearm(string commandId, string reason, int revision) =>
        new(Accepted: false, Reason: reason, Revision: revision);
}
