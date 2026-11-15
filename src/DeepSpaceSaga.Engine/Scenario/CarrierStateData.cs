using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>
/// Persisted carrier state attached to the world player ship (EP-0029). Schema 1 keeps only
/// the independent runtime hull current; hull max comes from the ship class profile and is
/// never duplicated here. Later stories extend this record with hangar slots, crew roster,
/// owned drones and torpedo magazines without breaking older readers (optional members).
/// </summary>
public sealed record CarrierStateData(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("revision")] int Revision,
    [property: JsonPropertyName("hullCurrent")] int HullCurrent,
    [property: JsonPropertyName("reservedCabins")] int ReservedCabins = 2,
    [property: JsonPropertyName("ownedDrones")] IReadOnlyList<OwnedDroneData>? OwnedDrones = null,
    [property: JsonPropertyName("lastSettlement")] SortieSettlementData? LastSettlement = null)
{
    public const int CurrentSchemaVersion = 1;
}

/// <summary>
/// Durable receipt of the last committed sortie settlement (EP-0029 US-0006/0007). Persisted so a
/// reload never re-applies the settlement delta and so the client's outcome log can be restored.
/// ShipId is implicit (the carrier this is attached to); ExitKind is a stable enum name string.
/// </summary>
public sealed record SortieSettlementData(
    [property: JsonPropertyName("battleId")] string BattleId,
    [property: JsonPropertyName("carrierRevision")] int CarrierRevision,
    [property: JsonPropertyName("shipDestroyed")] bool ShipDestroyed,
    [property: JsonPropertyName("lostDroneIds")] IReadOnlyList<string>? LostDroneIds,
    [property: JsonPropertyName("consumedTorpedoes")] int ConsumedTorpedoes,
    [property: JsonPropertyName("exitKind")] string ExitKind);

/// <summary>
/// One owned drone of the persistent wing (EP-0029 US-0003). Stable ids are never derived
/// from display names. A destroyed drone is kept as a tombstone (not combat-ready) and never
/// frees its pilot or slot; slots are logically freed only by replacement (out of scope here).
/// </summary>
public sealed record OwnedDroneData(
    [property: JsonPropertyName("droneId")] string DroneId,
    [property: JsonPropertyName("hangarModuleId")] string HangarModuleId,
    [property: JsonPropertyName("slotIndex")] int SlotIndex,
    [property: JsonPropertyName("crewId")] string CrewId,
    [property: JsonPropertyName("frameId")] string FrameId,
    [property: JsonPropertyName("weaponId")] string WeaponId,
    [property: JsonPropertyName("armorId")] string ArmorId,
    [property: JsonPropertyName("shieldId")] string? ShieldId = null,
    [property: JsonPropertyName("shieldCount")] int ShieldCount = 0,
    [property: JsonPropertyName("shieldSlots")] int ShieldSlots = 0,
    [property: JsonPropertyName("decoyId")] string? DecoyId = null,
    [property: JsonPropertyName("decoyMaxCharges")] int DecoyMaxCharges = 0,
    [property: JsonPropertyName("decoyCharges")] int DecoyCharges = 0,
    [property: JsonPropertyName("currentHull")] int CurrentHull = 0,
    [property: JsonPropertyName("maxHull")] int MaxHull = 0,
    [property: JsonPropertyName("armor")] int Armor = 0,
    [property: JsonPropertyName("maxArmor")] int MaxArmor = 0,
    [property: JsonPropertyName("ammoCurrent")] int AmmoCurrent = 0,
    [property: JsonPropertyName("ammoCapacity")] int AmmoCapacity = 0,
    [property: JsonPropertyName("jammed")] bool Jammed = false,
    [property: JsonPropertyName("engineIds")] IReadOnlyList<string>? EngineIds = null,
    [property: JsonPropertyName("destroyed")] bool Destroyed = false);
