using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public partial class SimulationEngine
{
    private readonly Dictionary<string, ImmutableArray<OwnedDroneData>> _ownedWing = new(StringComparer.Ordinal);

    private ImmutableArray<OwnedDroneData> GetOwnedWing(string shipId) =>
        _ownedWing.TryGetValue(shipId, out var wing) ? wing : ImmutableArray<OwnedDroneData>.Empty;

    /// <summary>Test seam: replace a ship's persistent wing after validating it against the world.</summary>
    internal void SetOwnedWingForTests(string shipId, IReadOnlyList<OwnedDroneData> drones)
    {
        lock (_worldStateLock)
        {
            var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == shipId)
                ?? throw new ScenarioException($"Ship '{shipId}' not found.");
            OwnedWingValidator.Validate(ship, _registry, drones);
            _ownedWing[shipId] = drones.ToImmutableArray();
        }
    }

    private ImmutableArray<OwnedDroneSnapshot> BuildOwnedWingProjection(SpaceObjectRuntime ship)
    {
        var wing = GetOwnedWing(ship.InitialMotion.ObjectId);
        if (wing.IsDefaultOrEmpty)
            return ImmutableArray<OwnedDroneSnapshot>.Empty;

        var result = ImmutableArray.CreateBuilder<OwnedDroneSnapshot>(wing.Length);
        foreach (var drone in wing)
        {
            result.Add(new OwnedDroneSnapshot(
                Id: drone.DroneId,
                ShipId: ship.InitialMotion.ObjectId,
                HangarModuleId: drone.HangarModuleId,
                SlotIndex: drone.SlotIndex,
                CrewId: drone.CrewId,
                FrameId: drone.FrameId,
                WeaponId: drone.WeaponId,
                ArmorId: drone.ArmorId,
                ShieldId: drone.ShieldId,
                ShieldCount: drone.ShieldCount,
                ShieldSlots: drone.ShieldSlots,
                DecoyId: drone.DecoyId,
                DecoyMaxCharges: drone.DecoyMaxCharges,
                DecoyCharges: drone.DecoyCharges,
                CurrentHull: drone.CurrentHull,
                MaxHull: drone.MaxHull,
                Armor: drone.Armor,
                MaxArmor: drone.MaxArmor,
                AmmoCurrent: drone.AmmoCurrent,
                AmmoCapacity: drone.AmmoCapacity,
                Jammed: drone.Jammed,
                EngineIds: (drone.EngineIds ?? []).ToImmutableArray(),
                ReadinessReasons: drone.Destroyed
                    ? ImmutableArray.Create(OwnedWingReadiness.Destroyed)
                    : ImmutableArray<string>.Empty));
        }
        return result.ToImmutable();
    }
}

internal static class OwnedWingReadiness
{
    internal const string Destroyed = "destroyed";
}

internal static class OwnedWingValidator
{
    public static void Validate(SpaceObjectRuntime ship, GameDataRegistry registry, IReadOnlyList<OwnedDroneData> drones)
    {
        var droneIds = new HashSet<string>(StringComparer.Ordinal);
        var slots = new HashSet<(string HangarModuleId, int SlotIndex)>();
        var pilots = new HashSet<string>(StringComparer.Ordinal);

        var hangarCapacity = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var module in ship.Modules)
        {
            var type = registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
            if (type.DroneCapacity is { } capacity && capacity > 0)
                hangarCapacity[module.ModuleId] = capacity;
        }

        var crewIds = ship.Crew.IsDefaultOrEmpty
            ? ImmutableHashSet<string>.Empty
            : ship.Crew.Select(c => c.Id).ToImmutableHashSet(StringComparer.Ordinal);

        foreach (var drone in drones)
        {
            if (string.IsNullOrWhiteSpace(drone.DroneId))
                throw new ScenarioException("Owned drone has empty droneId.");
            if (!droneIds.Add(drone.DroneId))
                throw new ScenarioException($"Duplicate droneId '{drone.DroneId}'.");
            if (!slots.Add((drone.HangarModuleId, drone.SlotIndex)))
                throw new ScenarioException($"Duplicate hangar slot '{drone.HangarModuleId}/{drone.SlotIndex}'.");
            if (!hangarCapacity.TryGetValue(drone.HangarModuleId, out int capacity))
                throw new ScenarioException($"Drone '{drone.DroneId}' references unknown hangar '{drone.HangarModuleId}'.");
            if (drone.SlotIndex < 0 || drone.SlotIndex >= capacity)
                throw new ScenarioException($"Drone '{drone.DroneId}' slot '{drone.SlotIndex}' is outside hangar '{drone.HangarModuleId}' capacity {capacity}.");

            if (!pilots.Add(drone.CrewId))
                throw new ScenarioException($"Duplicate pilot '{drone.CrewId}'.");
            if (!crewIds.Contains(drone.CrewId))
                throw new ScenarioException($"Drone '{drone.DroneId}' pilot '{drone.CrewId}' is not a crew member.");
            var pilot = ship.Crew.First(c => c.Id == drone.CrewId);
            if (string.IsNullOrWhiteSpace(pilot.Role) || !string.Equals(pilot.Role, "Pilot", StringComparison.OrdinalIgnoreCase))
                throw new ScenarioException($"Drone '{drone.DroneId}' pilot '{drone.CrewId}' must have the Pilot role.");

            if (drone.CurrentHull < 0 || drone.MaxHull < 0 || drone.CurrentHull > drone.MaxHull)
                throw new ScenarioException($"Drone '{drone.DroneId}' has invalid hull bounds.");
            if (drone.AmmoCurrent < 0 || drone.AmmoCapacity < 0 || drone.AmmoCurrent > drone.AmmoCapacity)
                throw new ScenarioException($"Drone '{drone.DroneId}' has invalid ammo bounds.");
            if (drone.DecoyCharges < 0 || drone.DecoyMaxCharges < 0 || drone.DecoyCharges > drone.DecoyMaxCharges)
                throw new ScenarioException($"Drone '{drone.DroneId}' has invalid decoy bounds.");
        }
    }
}
