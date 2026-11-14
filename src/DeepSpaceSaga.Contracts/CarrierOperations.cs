using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Read-only projection of the player ship's carrier surface: hull, hangar slots
/// and habitation. Produced by the Engine inside <see cref="AuthoritativeSnapshot"/> —
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
    HabitationSnapshot? Habitation = null);

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
/// </summary>
public sealed record HabitationSnapshot(
    int TotalCabins,
    int OccupiedCabins,
    int ReservedCabins,
    int FreeCabins,
    int PrisonerCount);
