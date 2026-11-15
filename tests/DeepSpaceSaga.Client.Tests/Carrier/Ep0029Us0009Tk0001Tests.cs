using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.Ship;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests;

public sealed class Ep0029Us0009Tk0001Tests
{
    private static CarrierOperationsSnapshot Carrier(int hull, int ammo) =>
        new("SPC-0001", "ship.tetrarch", "Tetrarch", 1, hull, 400, false,
            OwnedDrones:
            [
                new OwnedDroneSnapshot("DRN-PLAYER-01", "SPC-0001", "BAY-01", 0, "CHR-PILOT-1",
                    "heavy.sword.shield", "gun.sword.t1", "plate.medium",
                    AmmoCurrent: ammo, AmmoCapacity: 12),
            ],
            CabinReservation: new CabinReservationSnapshot(8, 6, 2, 0, PrisonerCount: 0));

    [Fact]
    public void ShipViewUsesSnapshotNotDefaults()
    {
        var model = ShipViewModel.From(Carrier(hull: 317, ammo: 2));
        Assert.False(model.Unsupported);
        Assert.Equal(317, model.HullCurrent);
        Assert.Equal(400, model.HullMax);
        Assert.Equal(2, model.Drones.Single().AmmoCurrent);
        Assert.Equal(12, model.Drones.Single().AmmoCapacity);
    }

    [Fact]
    public void LegacyShipShowsNoWing()
    {
        var model = ShipViewModel.From(null);
        Assert.True(model.Unsupported);
        Assert.True(model.Drones.IsEmpty); // no four gifted drones
        Assert.True(model.Crew.IsEmpty);
    }

    [Fact]
    public void ReservedIsSeparateFromOccupied()
    {
        var model = ShipViewModel.From(Carrier(hull: 400, ammo: 12));
        Assert.Equal(6, model.Cabins.OccupiedCabins);
        Assert.Equal(2, model.Cabins.ReservedCabins);
        Assert.Equal(8, model.Cabins.TotalCabins);
        Assert.Equal(0, model.Cabins.FreeCabins);
    }
}
