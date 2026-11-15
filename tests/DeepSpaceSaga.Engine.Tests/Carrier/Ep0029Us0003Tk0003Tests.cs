using System.Collections.Immutable;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Content;
using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class Ep0029Us0003Tk0003Tests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"dss-ep0029-us0003-tk0003-{Guid.NewGuid():N}");
    private string SettingsPath => Path.Combine(_directory, "settings.json");

    public Ep0029Us0003Tk0003Tests()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "categories.json"),
            """{"moduleTypes":[{"typeId":"module.drone-bay","displayName":"Drone bay","slotSize":1,"commandTypeIds":[]}]}""");
        File.WriteAllText(Path.Combine(_directory, "implementations.json"),
            """{"moduleImplementations":[{"typeId":"module.drone-bay.medium","displayName":"M","type":"module.drone-bay","massKg":4500,"structurePointsMax":250,"powerConsumptionW":0,"droneCapacity":2,"footprintWidth":2,"footprintHeight":2}]}""");
        File.WriteAllText(Path.Combine(_directory, "commands.json"), """{"commandDefinitions":[]}""");
        File.WriteAllText(Path.Combine(_directory, "items.json"),
            """{"itemTypes":[{"typeId":"item.ice","displayName":"Ice","unitMassKg":1}]}""");
        File.WriteAllText(Path.Combine(_directory, "ships.json"),
            """{"shipClasses":[{"typeId":"ship.tetrarch","hullMax":400}]}""");
        File.WriteAllText(SettingsPath, """
            {"typeData":{"moduleTypes":"categories.json","moduleImplementations":"implementations.json",
              "commandDefinitions":"commands.json","itemTypes":"items.json","shipClasses":"ships.json"},
              "defaultScenario":"scenario.json"}
            """);
        File.WriteAllText(Path.Combine(_directory, "scenario.json"), """
            {"scenarioMetadata":{"scenarioId":"tk0003","name":"tk0003"},"gameState":{
              "masterSeed":42,"gameTimeMs":0,"currentSpeed":"Speed0","playerShipObjectId":"SPC-0001",
              "spaceObjects":[{"objectId":"SPC-0001","objectType":"PlayerShip","shipClassId":"ship.tetrarch",
                "persistenceType":"Permanent","name":"Tetrarch","positionX":0,"positionY":0,"speedMps":0,
                "directionDegrees":0,"movementType":"Stationary"}]}}
            """);
    }

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); } catch { /* best effort */ }
    }

    private SimulationEngine Load() => EngineContentLoader.CreateEngineFromSettingsFile(SettingsPath);

    private static OwnedDroneData Drone(int ammo) => new(
        DroneId: "DRN-1", HangarModuleId: "BAY-01", SlotIndex: 0, CrewId: "CHR-PILOT-1",
        FrameId: "heavy.sword.shield", WeaponId: "weapon.sword", ArmorId: "plate.medium",
        ShieldId: "shield", ShieldCount: 2, ShieldSlots: 2,
        DecoyId: "decoy.t1", DecoyMaxCharges: 2, DecoyCharges: 2,
        CurrentHull: 100, MaxHull: 100, Armor: 50, MaxArmor: 50,
        AmmoCurrent: ammo, AmmoCapacity: 12);

    [Fact]
    public void RearmTransfersMassWithoutDuplication()
    {
        using var engine = Load();
        double before = engine.ComputeOwnedDroneMassKg(Drone(ammo: 6)) + 5 * SimulationEngine.TorpedoUnitMassKg; // cargo holds 5
        double after = engine.ComputeOwnedDroneMassKg(Drone(ammo: 11)) + 0 * SimulationEngine.TorpedoUnitMassKg; // cargo empty
        Assert.Equal(before, after, 6);
    }

    [Fact]
    public void ShotReducesDroneMassByOne()
    {
        using var engine = Load();
        double mass6 = engine.ComputeOwnedDroneMassKg(Drone(ammo: 6));
        double mass5 = engine.ComputeOwnedDroneMassKg(Drone(ammo: 5));
        Assert.Equal(1.0, mass6 - mass5, 6);
    }

    [Fact]
    public void MovementDoesNotJumpAfterShot()
    {
        var frame = new BattleFrame("frame", "Frame", 100, FrameMassKg: 200, ReferenceMassKg: 200,
            MinSpeed: 10, MaxSpeedCap: 100, TurnCapDeg: 60, EngineSlots: 2, ShieldSlots: 1);
        var layout = new BattleLayout("layout", "frame", "armor", "weapon", "shield",
            ImmutableArray.Create("eng1", "eng2"), Hull: 100, ShieldCount: 1);
        var weapon = new BattleWeapon("weapon", "Weapon", 1, 30, 100, 1, 2, 0, 0, 0, MassKg: 10);
        var plates = new BattleArmor("armor", "Armor", 1, 50, MassKg: 10);
        var shield = new BattleShield("shield", "Shield", 1, 1, MassKg: 5);
        var engines = ImmutableArray.Create(
            new TacticalEngineState(new BattleEngine("eng1", "E1", 10, 5, 5, 5)),
            new TacticalEngineState(new BattleEngine("eng2", "E2", 10, 5, 5, 5)));
        var decoy = new BattleDecoy("decoy", "Decoy", 1, 2, 1, 2);
        var drone = new TacticalDrone("d1", "A", new TacticalPose(100, 200, 45), 100, 50, 50, 55, "pilot",
            layout, frame, weapon, plates, shield, 1, engines, 30, decoy)
        {
            AmmoCurrent = 6,
            AmmoCapacity = 6,
            AmmoUnitMassKg = 1.0,
        };

        var poseBefore = drone.Pose;
        double speedBefore = drone.Speed;
        double massBefore = drone.Limits.Mass;

        drone.AmmoCurrent = 5; // one shot fired

        Assert.Equal(poseBefore, drone.Pose);
        Assert.Equal(speedBefore, drone.Speed);
        Assert.Equal(massBefore - 1.0, drone.Limits.Mass, 6);
    }
}
