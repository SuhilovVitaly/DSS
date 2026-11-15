using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

/// <summary>
/// EP-0029 US-0003 TK-0001 — owned drone contract.
/// </summary>
public class Ep0029Us0003Tk0001Tests
{
    [Fact]
    public void DroneIdentitySurvivesProjection()
    {
        var drone = new OwnedDroneSnapshot(
            Id: "DRN-PLAYER-01", ShipId: "SPC-0001", HangarModuleId: "MOD-PLAYER-BAY-01", SlotIndex: 0,
            CrewId: "CHR-PLAYER-EVELYN", FrameId: "heavy.sword.shield", WeaponId: "weapon.sword",
            ArmorId: "plate.medium", ShieldId: "shield", ShieldCount: 2, ShieldSlots: 2,
            DecoyId: "decoy.t1", DecoyMaxCharges: 2, DecoyCharges: 2,
            CurrentHull: 100, MaxHull: 100, Armor: 50, MaxArmor: 50, AmmoCurrent: 12, AmmoCapacity: 12);

        var copy = JsonSerializer.Deserialize<OwnedDroneSnapshot>(JsonSerializer.Serialize(drone))!;
        // Technical IDs are stable and independent of display names/localization.
        Assert.Equal("DRN-PLAYER-01", copy.Id);
        Assert.Equal("SPC-0001", copy.ShipId);
        Assert.Equal("CHR-PLAYER-EVELYN", copy.CrewId);
        Assert.Equal("MOD-PLAYER-BAY-01", copy.HangarModuleId);
        Assert.Equal("heavy.sword.shield", copy.FrameId);
        Assert.Equal("weapon.sword", copy.WeaponId);
        Assert.Equal("plate.medium", copy.ArmorId);
        Assert.Equal(12, copy.AmmoCurrent);
        Assert.Equal(12, copy.AmmoCapacity);
    }

    [Fact]
    public void ImmutableEquipmentArraysRoundTrip()
    {
        var drone = new OwnedDroneSnapshot(
            Id: "DRN-PLAYER-01", ShipId: "SPC-0001", HangarModuleId: "MOD-PLAYER-BAY-01", SlotIndex: 0,
            CrewId: "CHR-PLAYER-EVELYN", FrameId: "heavy.sword.shield", WeaponId: "weapon.sword",
            ArmorId: "plate.medium", ShieldId: "shield", ShieldCount: 2, ShieldSlots: 2,
            EngineIds: ImmutableArray.Create("drive.standard", "drive.standard"),
            EngineDamage: ImmutableArray.Create(
                new DroneEngineStateSnapshot(0, "drive.standard", "Ok"),
                new DroneEngineStateSnapshot(1, "drive.standard", "Damaged")));

        var copy = JsonSerializer.Deserialize<OwnedDroneSnapshot>(JsonSerializer.Serialize(drone))!;
        Assert.False(copy.EngineIds.IsDefault);
        Assert.Equal(2, copy.EngineIds.Length);
        Assert.Equal("drive.standard", copy.EngineIds[0]);
        Assert.Equal("drive.standard", copy.EngineIds[1]);

        Assert.Equal(2, copy.EngineDamage.Length);
        Assert.Equal("Ok", copy.EngineDamage[0].State);
        Assert.Equal("Damaged", copy.EngineDamage[1].State);

        Assert.Equal(2, copy.ShieldCount);
        Assert.Equal(2, copy.ShieldSlots);
    }
}
