using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.Ship;

/// <summary>
/// Read-only model for the Ship overlay, built purely from the authoritative
/// <see cref="CarrierOperationsSnapshot"/>. Formatting and the per-drone max selectable quantity are
/// computed here; eligibility is always the engine's decision — this model never fabricates a wing
/// or grants a carrier surface to a legacy ship.
/// </summary>
public sealed class ShipViewModel
{
    public bool Unsupported { get; private init; }
    public string Name { get; private init; } = "";
    public string ShipClassId { get; private init; } = "";
    public int HullCurrent { get; private init; }
    public int HullMax { get; private init; }
    public int Revision { get; private init; }
    public bool IsDestroyed { get; private init; }
    public ImmutableArray<DroneRow> Drones { get; private init; } = ImmutableArray<DroneRow>.Empty;
    public ImmutableArray<CrewRow> Crew { get; private init; } = ImmutableArray<CrewRow>.Empty;
    public CabinRow Cabins { get; private init; } = new(0, 0, 0, 0);

    public sealed record DroneRow(string Id, int AmmoCurrent, int AmmoCapacity, int CurrentHull, bool Destroyed);
    public sealed record CrewRow(string Id, string DisplayName, CrewRole? Role);
    public sealed record CabinRow(int TotalCabins, int OccupiedCabins, int ReservedCabins, int FreeCabins);

    public static ShipViewModel From(CarrierOperationsSnapshot? carrier)
    {
        if (carrier is null)
            return new ShipViewModel { Unsupported = true };

        return new ShipViewModel
        {
            Name = carrier.Name,
            ShipClassId = carrier.ShipClassId,
            HullCurrent = carrier.HullCurrent,
            HullMax = carrier.HullMax,
            Revision = carrier.Revision,
            IsDestroyed = carrier.IsDestroyed,
            Drones = carrier.OwnedDrones.IsDefaultOrEmpty
                ? ImmutableArray<DroneRow>.Empty
                : carrier.OwnedDrones
                    .Select(d => new DroneRow(d.Id, d.AmmoCurrent, d.AmmoCapacity, d.CurrentHull,
                        !d.ReadinessReasons.IsDefaultOrEmpty && d.ReadinessReasons.Contains("destroyed")))
                    .ToImmutableArray(),
            Crew = carrier.Crew.IsDefaultOrEmpty
                ? ImmutableArray<CrewRow>.Empty
                : carrier.Crew.Select(c => new CrewRow(c.Id, c.DisplayName, c.Role)).ToImmutableArray(),
            Cabins = carrier.CabinReservation is { } cabin
                ? new CabinRow(cabin.TotalCabins, cabin.OccupiedCabins, cabin.ReservedCabins, cabin.FreeCabins)
                : new CabinRow(0, 0, 0, 0),
        };
    }
}
