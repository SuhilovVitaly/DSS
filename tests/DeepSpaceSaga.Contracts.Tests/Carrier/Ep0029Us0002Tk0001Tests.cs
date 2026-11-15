using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

/// <summary>
/// EP-0029 US-0002 TK-0001 — shared crew identity and cabin reservation contract.
/// </summary>
public class Ep0029Us0002Tk0001Tests
{
    [Fact]
    public void CrewIdentityRoundTrip()
    {
        var crew = new CrewMemberSnapshot(
            Id: "CHR-PLAYER-EVELYN",
            DisplayName: "Evelyn Ward",
            Gender: CrewGender.Female,
            PortraitAssetId: "Images/Persons/W4/Portraits/CHR-20260915-102445-3C5C5D.png",
            Role: CrewRole.Pilot,
            Accuracy: 55, Maneuver: 52, Initiative: 55);

        var carrier = new CarrierOperationsSnapshot(
            ShipId: "SPC-0001", ShipClassId: "ship.tetrarch", Name: "Skitalets",
            Revision: 1, HullCurrent: 400, HullMax: 400, IsDestroyed: false,
            Crew: ImmutableArray.Create(crew));

        var copy = JsonSerializer.Deserialize<CarrierOperationsSnapshot>(JsonSerializer.Serialize(carrier))!;
        var pilot = copy.Crew[0];
        Assert.Equal("Evelyn Ward", pilot.DisplayName);
        Assert.Equal(CrewGender.Female, pilot.Gender);
        Assert.Equal("CHR-PLAYER-EVELYN", pilot.Id);
        Assert.Equal(CrewRole.Pilot, pilot.Role);

        // The tactical build references the same CrewId and carries the same display fields.
        var build = new TacticalBuildSnapshot(
            FrameName: "heavy.sword.shield", PilotId: pilot.Id, PilotName: pilot.DisplayName,
            WeaponId: "weapon", WeaponName: "Weapon", DamageMin: 1, DamageMax: 2, Piercing: 0,
            HitBonus: 0, Range: 10, ArcDegrees: 30, RangeBonus: null, RearKillChance: 0,
            ArmorId: "armor", ArmorName: "Armor", ShieldId: "shield", ShieldName: "Shield",
            ShieldDefense: 1, ShieldCount: 1, ShieldSlots: 1, FrameMass: 100, ReferenceMass: 100,
            WeaponMass: 10, ArmorMass: 10, ShieldMass: 5)
        {
            PilotGender = pilot.Gender,
            PilotPortraitAssetId = pilot.PortraitAssetId,
        };

        Assert.Equal("Evelyn Ward", build.PilotName);
        Assert.Equal(CrewGender.Female, build.PilotGender);
        Assert.Equal(pilot.PortraitAssetId, build.PilotPortraitAssetId);
    }

    [Fact]
    public void ReservationIsNotOccupant()
    {
        var reservation = new CabinReservationSnapshot(
            TotalCabins: 8, OccupiedCabins: 6, ReservedCabins: 2, FreeCabins: 0, PrisonerCount: 0);

        var copy = JsonSerializer.Deserialize<CabinReservationSnapshot>(JsonSerializer.Serialize(reservation))!;
        Assert.Equal(8, copy.TotalCabins);
        Assert.Equal(6, copy.OccupiedCabins);
        Assert.Equal(2, copy.ReservedCabins);
        Assert.Equal(0, copy.FreeCabins);
        Assert.Equal(0, copy.PrisonerCount);
        // Reserved cabins are a held count, not occupants and not actual prisoners.
        Assert.NotEqual(copy.ReservedCabins, copy.OccupiedCabins);
    }
}
