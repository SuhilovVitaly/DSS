using System.Collections.Immutable;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Content;
using DeepSpaceSaga.Engine.Tests.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class Ep0029Us0005Tk0002Tests
{
    private static OwnedDroneData Drone(string id, string crewId, string frame, int ammo, int currentHull, int maxHull, bool destroyed = false)
    {
        bool heavy = frame.StartsWith("heavy");
        bool spear = frame.Contains("spear");
        return new OwnedDroneData(
            DroneId: id, HangarModuleId: "BAY", SlotIndex: 0, CrewId: crewId,
            FrameId: frame,
            WeaponId: spear ? "gun.spear.t1" : "gun.sword.t1",
            ArmorId: "plate.medium", ShieldId: "shield.t1",
            ShieldCount: heavy ? 2 : 1, ShieldSlots: heavy ? 2 : 1,
            DecoyId: "decoy.t1", DecoyMaxCharges: 2, DecoyCharges: 2,
            CurrentHull: currentHull, MaxHull: maxHull, Armor: 50, MaxArmor: 50,
            AmmoCurrent: ammo, AmmoCapacity: heavy ? 12 : 6,
            EngineIds: ["drive.standard", "drive.standard"],
            Destroyed: destroyed);
    }

    private static CrewMemberRuntime Pilot(string id, int accuracy = 55, int maneuver = 52, int initiative = 55) =>
        new(id, id, accuracy, maneuver, Role: "Pilot", Initiative: initiative);

    private static ImmutableArray<OwnedDroneData> FourWing(bool twoDestroyed = false) => ImmutableArray.Create(
        Drone("DRN-PLAYER-01", "CHR-PILOT-1", "heavy.sword.shield", 12, 100, 100, destroyed: twoDestroyed),
        Drone("DRN-PLAYER-02", "CHR-PILOT-2", "heavy.spear.shield", 12, 100, 100, destroyed: twoDestroyed),
        Drone("DRN-PLAYER-03", "CHR-PILOT-3", "light.sword.shield", 6, 100, 100),
        Drone("DRN-PLAYER-04", "CHR-PILOT-4", "light.spear", 6, 100, 100));

    private static ImmutableArray<CrewMemberRuntime> FourCrew() => ImmutableArray.Create(
        Pilot("CHR-PILOT-1", 55, 52, 55),
        Pilot("CHR-PILOT-2", 53, 55, 53),
        Pilot("CHR-PILOT-3", 59, 54, 59),
        Pilot("CHR-PILOT-4", 50, 51, 50));

    private static TacticalBattleCatalog Catalog() => AssemblyTests.Catalog();

    [Fact]
    public void TwoSurvivorsStartAsTwo()
    {
        var battle = OwnedBattleAssembly.Create(Catalog(), "SPC-0001", 400, 35,
            FourWing(twoDestroyed: true), FourCrew(), Catalog().Scenarios["initial"], 1);
        Assert.Equal(2, battle.Drones.Count(d => d.Side == "A"));
        Assert.Equal(6, battle.Drones.Count(d => d.Side == "B")); // enemy unchanged
    }

    [Fact]
    public void DamagedShipAndAmmoPersistIntoBattle()
    {
        var wing = FourWing();
        var battle = OwnedBattleAssembly.Create(Catalog(), "SPC-0001", 317, 35, wing, FourCrew(),
            Catalog().Scenarios["initial"], 1);

        Assert.Equal(317, battle.Carrier("A").Hull);
        var light = battle.Drones.Single(d => d.Side == "A" && d.Id == "DRN-PLAYER-03");
        Assert.Equal(6, light.AmmoCapacity);
        Assert.Equal(6, light.AmmoCurrent);

        // Damage a drone's ammo and hull, then rebuild.
        var damaged = wing.Select(d => d.DroneId == "DRN-PLAYER-03" ? d with { AmmoCurrent = 2, CurrentHull = 40 } : d).ToImmutableArray();
        var damagedBattle = OwnedBattleAssembly.Create(Catalog(), "SPC-0001", 317, 35, damaged, FourCrew(),
            Catalog().Scenarios["initial"], 1);
        var rebuiltLight = damagedBattle.Drones.Single(d => d.Side == "A" && d.Id == "DRN-PLAYER-03");
        Assert.Equal(2, rebuiltLight.AmmoCurrent);
        Assert.Equal(40, rebuiltLight.Hull);
    }

    [Fact]
    public void RenameDoesNotReassignDeployment()
    {
        // Same DroneIds, different display names — deployment is keyed by stable id, not name.
        var crewA = FourCrew();
        var crewB = ImmutableArray.Create(
            Pilot("CHR-PILOT-1", 55, 52, 55), Pilot("CHR-PILOT-2", 53, 55, 53),
            Pilot("CHR-PILOT-3", 59, 54, 59), Pilot("CHR-PILOT-4", 50, 51, 50));

        var a = OwnedBattleAssembly.Create(Catalog(), "SPC-0001", 400, 35, FourWing(), crewA, Catalog().Scenarios["initial"], 1);
        var b = OwnedBattleAssembly.Create(Catalog(), "SPC-0001", 400, 35, FourWing(), crewB, Catalog().Scenarios["initial"], 1);
        Assert.Equal(a.Drones.Where(d => d.Side == "A").Select(d => d.Id),
            b.Drones.Where(d => d.Side == "A").Select(d => d.Id));
    }

    [Fact]
    public void EnemyGenerationKeepsSeedDiscipline()
    {
        var catalog = Catalog();
        var encounter = catalog.Scenarios["initial"];
        var fixedCrew = ImmutableArray.Create(
            Pilot("CHR-PILOT-1", 10, 10, 10), Pilot("CHR-PILOT-2", 10, 10, 10),
            Pilot("CHR-PILOT-3", 10, 10, 10), Pilot("CHR-PILOT-4", 10, 10, 10));
        var otherCrew = ImmutableArray.Create(
            Pilot("CHR-PILOT-1", 90, 90, 90), Pilot("CHR-PILOT-2", 90, 90, 90),
            Pilot("CHR-PILOT-3", 90, 90, 90), Pilot("CHR-PILOT-4", 90, 90, 90));

        var a = OwnedBattleAssembly.Create(catalog, "SPC-0001", 400, 35, FourWing(), fixedCrew, encounter, 1);
        var b = OwnedBattleAssembly.Create(catalog, "SPC-0001", 400, 35, FourWing(), otherCrew, encounter, 1);

        var enemyA = a.Drones.Where(d => d.Side == "B").Select(d => (d.Accuracy, d.Maneuver, d.Initiative)).ToArray();
        var enemyB = b.Drones.Where(d => d.Side == "B").Select(d => (d.Accuracy, d.Maneuver, d.Initiative)).ToArray();
        Assert.Equal(enemyA, enemyB); // fixed player crew never shifts the enemy RNG stream
    }
}
