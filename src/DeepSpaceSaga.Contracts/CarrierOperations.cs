using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

/// <summary>Numeric enum identities are append-only; never renumber existing values.</summary>
public enum CrewGender { Female = 0, Male = 1 }

/// <summary>Numeric enum identities are append-only; never renumber existing values.</summary>
public enum CrewRole { Captain = 0, Officer = 1, Pilot = 2 }

/// <summary>
/// Read-only projection of the player ship's carrier surface: hull, hangar slots,
/// crew and habitation. Produced by the Engine inside <see cref="AuthoritativeSnapshot"/> —
/// the Client only renders it and never mutates the world. Absence of
/// <see cref="AuthoritativeSnapshot.CarrierOperations"/> in a legacy payload means the
/// producer does not support the carrier surface, not that the player owns a free ship.
/// </summary>
public sealed record CarrierOperationsSnapshot(
    string ShipId,
    string ShipClassId,
    string Name,
    int Revision,
    int HullCurrent,
    int HullMax,
    bool IsDestroyed,
    string? AppearanceId = null,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<HangarSlotSnapshot>))]
    ImmutableArray<HangarSlotSnapshot> Hangars = default,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<CrewMemberSnapshot>))]
    ImmutableArray<CrewMemberSnapshot> Crew = default,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<OwnedDroneSnapshot>))]
    ImmutableArray<OwnedDroneSnapshot> OwnedDrones = default,
    CabinReservationSnapshot? CabinReservation = null);

/// <summary>
/// One unique hangar position: a real installed hangar module plus the slot index
/// inside it. Two drones may share the same medium hangar (<see cref="HangarModuleId"/>)
/// as long as their <see cref="SlotIndex"/> differs.
/// </summary>
public sealed record HangarSlotSnapshot(
    string HangarModuleId,
    int SlotIndex,
    string? DroneId = null);

/// <summary>
/// Cabin occupancy across all installed living modules. Reserved cabins are held for
/// future prisoners and never create crew members; occupied/used cabins are the crew.
/// <see cref="ReservedCabins"/> is a held-count, not a prisoner count — <see cref="PrisonerCount"/>
/// stays 0 until capture mechanics exist (explicitly out of scope for EP-0029).
/// </summary>
public sealed record CabinReservationSnapshot(
    int TotalCabins,
    int OccupiedCabins,
    int ReservedCabins,
    int FreeCabins,
    int PrisonerCount);

/// <summary>
/// One crew member aboard the player ship. Single source of truth for name, gender,
/// portrait, role and skills; both the carrier surface and the tactical battle build
/// reference the same <see cref="Id"/> (a CrewId) so identity never diverges on Load.
/// </summary>
public sealed record CrewMemberSnapshot(
    string Id,
    string DisplayName,
    CrewGender Gender,
    string? PortraitAssetId = null,
    CrewRole? Role = null,
    int Accuracy = 0,
    int Maneuver = 0,
    int Initiative = 0);

/// <summary>
/// One owned drone of the player's persistent wing, placed at a unique hangar slot.
/// Technical ids (<see cref="Id"/>, <see cref="ShipId"/>, <see cref="CrewId"/>,
/// <see cref="HangarModuleId"/>, <see cref="FrameId"/>, equipment ids) are stable and never
/// derived from display names or localization.
/// </summary>
public sealed record OwnedDroneSnapshot(
    string Id,
    string ShipId,
    string HangarModuleId,
    int SlotIndex,
    string CrewId,
    string FrameId,
    string WeaponId,
    string ArmorId,
    string? ShieldId = null,
    int ShieldCount = 0,
    int ShieldSlots = 0,
    string? DecoyId = null,
    int DecoyMaxCharges = 0,
    int DecoyCharges = 0,
    int CurrentHull = 0,
    int MaxHull = 0,
    int Armor = 0,
    int MaxArmor = 0,
    int AmmoCurrent = 0,
    int AmmoCapacity = 0,
    bool Jammed = false,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<string>))]
    ImmutableArray<string> EngineIds = default,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<DroneEngineStateSnapshot>))]
    ImmutableArray<DroneEngineStateSnapshot> EngineDamage = default,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<string>))]
    ImmutableArray<string> ReadinessReasons = default);

/// <summary>One engine slot state on an owned drone; State is a stable code (Ok/Jammed/Damaged).</summary>
public sealed record DroneEngineStateSnapshot(int Index, string EngineId, string State);
