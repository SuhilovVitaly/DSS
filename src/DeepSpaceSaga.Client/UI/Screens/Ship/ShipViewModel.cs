using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.Ship;

/// <summary>
/// Read-only model for the Ship overlay, built purely from the authoritative
/// <see cref="CarrierOperationsSnapshot"/>. Formatting and the per-drone max selectable quantity are
/// computed here; eligibility is always the engine's decision â€” this model never fabricates a wing
/// or grants a carrier surface to a legacy ship.
/// </summary>
public sealed class ShipViewModel
{
    public bool Unsupported { get; private init; }
    public string ShipId { get; private init; } = "";
    public string Name { get; private init; } = "";
    public string ShipClassId { get; private init; } = "";
    public int HullCurrent { get; private init; }
    public int HullMax { get; private init; }
    public int Revision { get; private init; }
    public bool IsDestroyed { get; private init; }
    public ImmutableArray<DroneRow> Drones { get; private init; } = ImmutableArray<DroneRow>.Empty;
    public ImmutableArray<CrewRow> Crew { get; private init; } = ImmutableArray<CrewRow>.Empty;
    public SensorOperatorSnapshot? SensorOperator { get; private init; }
    public CabinRow Cabins { get; private init; } = new(0, 0, 0, 0);

    public sealed record DroneRow(string Id, int AmmoCurrent, int AmmoCapacity, int CurrentHull, bool Destroyed,
        bool Disabled = false, bool Uncrewed = false, ImmutableArray<string> Equipment = default);
    public sealed record CrewRow(string Id, string DisplayName, CrewRole? Role, bool IsSensorOperator = false);
    public sealed record CabinRow(int TotalCabins, int OccupiedCabins, int ReservedCabins, int FreeCabins);

    public static ShipViewModel From(CarrierOperationsSnapshot? carrier, SensorOperatorSnapshot? sensorOperator = null)
    {
        if (carrier is null)
            return new ShipViewModel { Unsupported = true };

        return new ShipViewModel
        {
            ShipId = carrier.ShipId,
            Name = carrier.Name,
            ShipClassId = carrier.ShipClassId,
            HullCurrent = carrier.HullCurrent,
            HullMax = carrier.HullMax,
            Revision = carrier.Revision,
            IsDestroyed = carrier.IsDestroyed,
            SensorOperator = sensorOperator,
            Drones = carrier.OwnedDrones.IsDefaultOrEmpty
                ? ImmutableArray<DroneRow>.Empty
                : carrier.OwnedDrones
                    .Select(d => new DroneRow(d.Id, d.AmmoCurrent, d.AmmoCapacity, d.CurrentHull,
                        !d.ReadinessReasons.IsDefaultOrEmpty && d.ReadinessReasons.Contains("destroyed"),
                        !d.ReadinessReasons.IsDefaultOrEmpty && d.ReadinessReasons.Contains("disabled"),
                        !d.ReadinessReasons.IsDefaultOrEmpty && d.ReadinessReasons.Contains("no_pilot"),
                        new[] { d.FrameId, d.WeaponId, d.ArmorId, d.DecoyId }
                            .Where(id => !string.IsNullOrEmpty(id)).Select(id => id!)
                            .Concat(d.ShieldId is { } shield ? Enumerable.Repeat(shield, Math.Max(0, d.ShieldCount)) : [])
                            .Concat(d.EngineIds.IsDefault ? [] : d.EngineIds).ToImmutableArray()))
                    .ToImmutableArray(),
            Crew = carrier.Crew.IsDefaultOrEmpty
                ? ImmutableArray<CrewRow>.Empty
                : carrier.Crew.Select(c => new CrewRow(c.Id, c.DisplayName, c.Role, c.Id == sensorOperator?.CrewId)).ToImmutableArray(),
            Cabins = carrier.CabinReservation is { } cabin
                ? new CabinRow(cabin.TotalCabins, cabin.OccupiedCabins, cabin.ReservedCabins, cabin.FreeCabins)
                : new CabinRow(0, 0, 0, 0),
        };
    }
}
