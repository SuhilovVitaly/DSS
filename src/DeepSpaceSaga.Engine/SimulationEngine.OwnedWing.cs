using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Content;

namespace DeepSpaceSaga.Engine;

public partial class SimulationEngine
{
    private readonly Dictionary<string, ImmutableArray<OwnedDroneData>> _ownedWing = new(StringComparer.Ordinal);

    private ImmutableArray<OwnedDroneData> GetOwnedWing(string shipId) =>
        _ownedWing.TryGetValue(shipId, out var wing) ? wing : ImmutableArray<OwnedDroneData>.Empty;

    /// <summary>Torpedo unit mass (kg) — A-02 technical catalog value, not an approved balance promise.</summary>
    internal const double TorpedoUnitMassKg = 1.0;

    /// <summary>
    /// Mass of one owned drone: catalogue components resolved from its layout plus loaded
    /// torpedoes (AmmoCurrent × <see cref="TorpedoUnitMassKg"/>). Unknown components contribute
    /// nothing — the engine never fabricates a full tonnage.
    /// </summary>
    internal double ComputeOwnedDroneMassKg(OwnedDroneData drone)
    {
        double mass = drone.AmmoCurrent * TorpedoUnitMassKg;
        if (_registry.TacticalBattle.Catalog is not { } catalog || !catalog.Layouts.Contains(drone.FrameId))
            return mass;

        var layout = catalog.Layouts[drone.FrameId];
        if (catalog.Frames.Contains(layout.Class))
            mass += catalog.Frames[layout.Class].FrameMassKg;
        if (catalog.Armor.Contains(layout.Armor))
            mass += catalog.Armor[layout.Armor].MassKg;
        if (layout.Weapon is { } weaponId && catalog.Weapons.Contains(weaponId))
            mass += catalog.Weapons[weaponId].MassKg;
        if (layout.Shield is { } shieldId && catalog.Shields.Contains(shieldId))
            mass += catalog.Shields[shieldId].MassKg * drone.ShieldCount;
        foreach (string engineId in layout.Engines)
            if (catalog.Engines.Contains(engineId))
                mass += catalog.Engines[engineId].MassKg;
        if (layout.Module is { } decoyId && catalog.Decoys.Contains(decoyId))
            mass += catalog.Decoys[decoyId].MassKg;
        return mass;
    }

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
            var engineIds = (drone.EngineIds ?? []).ToImmutableArray();
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
                EngineIds: engineIds,
                EngineDamage: BuildEngineDamage(engineIds, drone.EngineFactors),
                ReadinessReasons: BuildReadinessReasons(drone)));
        }
        return result.ToImmutable();
    }

    private static ImmutableArray<DroneEngineStateSnapshot> BuildEngineDamage(ImmutableArray<string> engineIds,
        IReadOnlyList<double>? factors)
    {
        if (factors is not { Count: > 0 } || engineIds.IsDefaultOrEmpty)
            return ImmutableArray<DroneEngineStateSnapshot>.Empty;

        var damage = ImmutableArray.CreateBuilder<DroneEngineStateSnapshot>(engineIds.Length);
        for (int i = 0; i < engineIds.Length; i++)
        {
            double factor = i < factors.Count ? factors[i] : TacticalBattleConstants.IntactEngineFactor;
            damage.Add(new DroneEngineStateSnapshot(i, engineIds[i],
                factor >= TacticalBattleConstants.IntactEngineFactor
                    ? OwnedWingEngineState.Ok
                    : OwnedWingEngineState.Damaged));
        }
        return damage.ToImmutable();
    }

    private static ImmutableArray<string> BuildReadinessReasons(OwnedDroneData drone) =>
        drone.Destroyed ? ImmutableArray.Create(OwnedWingReadiness.Destroyed)
        : drone.Disabled ? ImmutableArray.Create(OwnedWingReadiness.Disabled)
        : ImmutableArray<string>.Empty;
}

internal static class OwnedWingReadiness
{
    internal const string Destroyed = "destroyed";
    internal const string Disabled = "disabled";
}

internal static class OwnedWingEngineState
{
    internal const string Ok = "Ok";
    internal const string Damaged = "Damaged";
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
            // A null/blank hangar id would otherwise surface as ArgumentNullException from the
            // capacity dictionary probe instead of a clean scenario rejection.
            if (string.IsNullOrWhiteSpace(drone.HangarModuleId))
                throw new ScenarioException($"Drone '{drone.DroneId}' has empty hangarModuleId.");
            if (!slots.Add((drone.HangarModuleId, drone.SlotIndex)))
                throw new ScenarioException($"Duplicate hangar slot '{drone.HangarModuleId}/{drone.SlotIndex}'.");
            if (!hangarCapacity.TryGetValue(drone.HangarModuleId, out int capacity))
                throw new ScenarioException($"Drone '{drone.DroneId}' references unknown hangar '{drone.HangarModuleId}'.");
            if (drone.SlotIndex < 0 || drone.SlotIndex >= capacity)
                throw new ScenarioException($"Drone '{drone.DroneId}' slot '{drone.SlotIndex}' is outside hangar '{drone.HangarModuleId}' capacity {capacity}.");

            if (!(drone.Destroyed && drone.CrewId == ""))
            {
                if (!pilots.Add(drone.CrewId))
                    throw new ScenarioException($"Duplicate pilot '{drone.CrewId}'.");
                if (!crewIds.Contains(drone.CrewId))
                    throw new ScenarioException($"Drone '{drone.DroneId}' pilot '{drone.CrewId}' is not a crew member.");
                var pilot = ship.Crew.First(c => c.Id == drone.CrewId);
                if (string.IsNullOrWhiteSpace(pilot.Role) || !string.Equals(pilot.Role, "Pilot", StringComparison.OrdinalIgnoreCase))
                    throw new ScenarioException($"Drone '{drone.DroneId}' pilot '{drone.CrewId}' must have the Pilot role.");
            }

            if (drone.CurrentHull < 0 || drone.MaxHull < 0 || drone.CurrentHull > drone.MaxHull)
                throw new ScenarioException($"Drone '{drone.DroneId}' has invalid hull bounds.");
            if (drone.AmmoCurrent < 0 || drone.AmmoCapacity < 0 || drone.AmmoCurrent > drone.AmmoCapacity)
                throw new ScenarioException($"Drone '{drone.DroneId}' has invalid ammo bounds.");
            if (drone.DecoyCharges < 0 || drone.DecoyMaxCharges < 0 || drone.DecoyCharges > drone.DecoyMaxCharges)
                throw new ScenarioException($"Drone '{drone.DroneId}' has invalid decoy bounds.");

            // Catalogue-reference and slot-capacity validation (US-0003 TK-0002): numeric bounds alone
            // would accept a wing that cannot be assembled from real tactical content. A registry
            // without tactical content keeps the legacy numeric-only behaviour.
            if (registry.TacticalBattle.Catalog is { } catalog)
                ValidateCatalogReferences(drone, catalog);
        }
    }

    private static void ValidateCatalogReferences(OwnedDroneData drone, TacticalBattleCatalog catalog)
    {
        // Guard every id before a catalogue lookup: ImmutableDictionary.ContainsKey(null) throws
        // ArgumentNullException, so a malformed save/scenario must be rejected as ScenarioException.
        if (string.IsNullOrWhiteSpace(drone.FrameId))
            throw new ScenarioException($"Drone '{drone.DroneId}' has empty frameId.");
        if (string.IsNullOrWhiteSpace(drone.WeaponId))
            throw new ScenarioException($"Drone '{drone.DroneId}' has empty weaponId.");
        if (string.IsNullOrWhiteSpace(drone.ArmorId))
            throw new ScenarioException($"Drone '{drone.DroneId}' has empty armorId.");
        if (drone.ShieldId is not null && string.IsNullOrWhiteSpace(drone.ShieldId))
            throw new ScenarioException($"Drone '{drone.DroneId}' has blank shieldId.");
        if (drone.DecoyId is not null && string.IsNullOrWhiteSpace(drone.DecoyId))
            throw new ScenarioException($"Drone '{drone.DroneId}' has blank decoyId.");

        var engineIds = drone.EngineIds ?? [];
        foreach (string engineId in engineIds)
            if (string.IsNullOrWhiteSpace(engineId))
                throw new ScenarioException($"Drone '{drone.DroneId}' references an empty engine id.");

        if (!catalog.Layouts.Contains(drone.FrameId))
            throw new ScenarioException($"Drone '{drone.DroneId}' references unknown layout '{drone.FrameId}'.");
        var layout = catalog.Layouts[drone.FrameId];
        if (!catalog.Frames.Contains(layout.Class))
            throw new ScenarioException(
                $"Drone '{drone.DroneId}' layout '{drone.FrameId}' references unknown frame '{layout.Class}'.");
        var frame = catalog.Frames[layout.Class];

        if (!catalog.Weapons.Contains(drone.WeaponId))
            throw new ScenarioException($"Drone '{drone.DroneId}' references unknown weapon '{drone.WeaponId}'.");
        if (!catalog.Armor.Contains(drone.ArmorId))
            throw new ScenarioException($"Drone '{drone.DroneId}' references unknown armor '{drone.ArmorId}'.");
        if (drone.ShieldId is { } shieldId && !catalog.Shields.Contains(shieldId))
            throw new ScenarioException($"Drone '{drone.DroneId}' references unknown shield '{shieldId}'.");
        if (drone.DecoyId is { } decoyId && !catalog.Decoys.Contains(decoyId))
            throw new ScenarioException($"Drone '{drone.DroneId}' references unknown decoy '{decoyId}'.");

        foreach (string engineId in engineIds)
            if (!catalog.Engines.Contains(engineId))
                throw new ScenarioException($"Drone '{drone.DroneId}' references unknown engine '{engineId}'.");
        if (engineIds.Count > frame.EngineSlots)
            throw new ScenarioException(
                $"Drone '{drone.DroneId}' mounts {engineIds.Count} engines but frame '{frame.Id}' has {frame.EngineSlots} engine slots.");
        if (drone.ShieldCount > frame.ShieldSlots)
            throw new ScenarioException(
                $"Drone '{drone.DroneId}' mounts {drone.ShieldCount} shields but frame '{frame.Id}' has {frame.ShieldSlots} shield slots.");
        if (drone.ShieldSlots > frame.ShieldSlots)
            throw new ScenarioException(
                $"Drone '{drone.DroneId}' declares {drone.ShieldSlots} shield slots but frame '{frame.Id}' has {frame.ShieldSlots}.");
        if (drone.DecoyId is { } decoy && drone.DecoyMaxCharges > catalog.Decoys[decoy].Charges)
            throw new ScenarioException(
                $"Drone '{drone.DroneId}' decoy '{decoy}' carries {drone.DecoyMaxCharges} charges but the catalogue allows {catalog.Decoys[decoy].Charges}.");
        if (drone.AmmoCapacity > frame.AmmoCapacity)
            throw new ScenarioException(
                $"Drone '{drone.DroneId}' magazine {drone.AmmoCapacity} exceeds frame '{frame.Id}' capacity {frame.AmmoCapacity}.");

        // Persisted engine damage (US-0006): one factor per installed engine, each in (0, 1].
        if (drone.EngineFactors is { } factors)
        {
            if (factors.Count > engineIds.Count)
                throw new ScenarioException(
                    $"Drone '{drone.DroneId}' has {factors.Count} engine factors but {engineIds.Count} engines.");
            foreach (double factor in factors)
                if (!(factor > 0 && factor <= 1))
                    throw new ScenarioException(
                        $"Drone '{drone.DroneId}' engine factor {factor} must be in (0,1].");
        }
    }
}
