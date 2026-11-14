using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class Ep0029Us0003Tk0002Tests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"dss-ep0029-us0003-tk0002-{Guid.NewGuid():N}");
    private string SettingsPath => Path.Combine(_directory, "settings.json");
    private string ScenarioPath => Path.Combine(_directory, "scenario.json");

    public Ep0029Us0003Tk0002Tests()
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
    }

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); } catch { /* best effort */ }
    }

    private SimulationEngine Load() => EngineContentLoader.CreateEngineFromSettingsFile(SettingsPath);

    private static JsonObject HullLayout()
    {
        var cells = new JsonArray();
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 5; x++)
                cells.Add(new JsonObject { ["x"] = x, ["y"] = y });
        return new JsonObject { ["width"] = 5, ["height"] = 5, ["cells"] = cells };
    }

    private static JsonObject Bay2x2(string moduleId, int ax, int ay)
    {
        var cells = new JsonArray();
        foreach (var (x, y) in new[] { (ax, ay), (ax + 1, ay), (ax, ay + 1), (ax + 1, ay + 1) })
            cells.Add(new JsonObject { ["x"] = x, ["y"] = y });
        return new JsonObject
        {
            ["moduleId"] = moduleId,
            ["moduleTypeId"] = "module.drone-bay.medium",
            ["occupiedCells"] = cells,
            ["structurePoints"] = 250,
            ["powerState"] = "On",
            ["operationalState"] = "Ready",
            ["cargo"] = new JsonArray(),
        };
    }

    private static JsonObject CrewMember(string id, string name, string role) => new()
    {
        ["crewId"] = id,
        ["displayName"] = name,
        ["role"] = role,
        ["accuracySkill"] = 0,
        ["maneuverabilitySkill"] = 0,
    };

    private static OwnedDroneData Drone(string id, string bay, int slot, string crewId, bool destroyed = false) => new(
        DroneId: id, HangarModuleId: bay, SlotIndex: slot, CrewId: crewId,
        FrameId: "heavy.sword.shield", WeaponId: "weapon.sword", ArmorId: "plate.medium",
        ShieldId: "shield", ShieldCount: 2, ShieldSlots: 2,
        DecoyId: "decoy.t1", DecoyMaxCharges: 2, DecoyCharges: 2,
        CurrentHull: 100, MaxHull: 100, Armor: 50, MaxArmor: 50,
        AmmoCurrent: 12, AmmoCapacity: 12, Destroyed: destroyed);

    private void WriteScenario()
    {
        var ship = new JsonObject
        {
            ["objectId"] = "SPC-0001",
            ["objectType"] = "PlayerShip",
            ["shipClassId"] = "ship.tetrarch",
            ["persistenceType"] = "Permanent",
            ["name"] = "Tetrarch",
            ["positionX"] = 0,
            ["positionY"] = 0,
            ["speedMps"] = 0,
            ["directionDegrees"] = 0,
            ["movementType"] = "Stationary",
            ["isKnown"] = true,
            ["hullLayout"] = HullLayout(),
            ["modules"] = new JsonArray(Bay2x2("BAY-01", 0, 0), Bay2x2("BAY-02", 2, 0)),
            ["crew"] = new JsonArray(
                CrewMember("CHR-0001", "Dunkan Su", "Captain"),
                CrewMember("CHR-PLAYER-DEFENSE", "Irina Volskaya", "Officer"),
                CrewMember("CHR-PILOT-1", "Evelyn Ward", "Pilot"),
                CrewMember("CHR-PILOT-2", "Audrey Bennett", "Pilot"),
                CrewMember("CHR-PILOT-3", "Claire Morgan", "Pilot"),
                CrewMember("CHR-PILOT-4", "Rachel Hayes", "Pilot")),
        };
        var scenario = new JsonObject
        {
            ["scenarioMetadata"] = new JsonObject { ["scenarioId"] = "tk0002", ["name"] = "tk0002" },
            ["gameState"] = new JsonObject
            {
                ["masterSeed"] = 42,
                ["gameTimeMs"] = 0,
                ["currentSpeed"] = "Speed0",
                ["playerShipObjectId"] = "SPC-0001",
                ["spaceObjects"] = new JsonArray(ship),
            },
        };
        File.WriteAllText(ScenarioPath, scenario.ToJsonString());
    }

    [Fact]
    public void FifthDroneRejectedWithFourSlots()
    {
        WriteScenario();
        using var engine = Load();

        var four = new[]
        {
            Drone("DRN-1", "BAY-01", 0, "CHR-PILOT-1"),
            Drone("DRN-2", "BAY-01", 1, "CHR-PILOT-2"),
            Drone("DRN-3", "BAY-02", 0, "CHR-PILOT-3"),
            Drone("DRN-4", "BAY-02", 1, "CHR-PILOT-4"),
        };
        engine.SetOwnedWingForTests("SPC-0001", four);

        var five = four.Concat(new[] { Drone("DRN-5", "BAY-01", 2, "CHR-PILOT-1") }).ToArray();
        Assert.Throws<ScenarioException>(() => engine.SetOwnedWingForTests("SPC-0001", five));
    }

    [Fact]
    public void DuplicatePilotOrSlotRejected()
    {
        WriteScenario();
        using var engine = Load();

        var duplicatePilot = new[]
        {
            Drone("DRN-1", "BAY-01", 0, "CHR-PILOT-1"),
            Drone("DRN-2", "BAY-01", 1, "CHR-PILOT-1"),
        };
        Assert.Throws<ScenarioException>(() => engine.SetOwnedWingForTests("SPC-0001", duplicatePilot));

        var duplicateSlot = new[]
        {
            Drone("DRN-1", "BAY-01", 0, "CHR-PILOT-1"),
            Drone("DRN-2", "BAY-01", 0, "CHR-PILOT-2"),
        };
        Assert.Throws<ScenarioException>(() => engine.SetOwnedWingForTests("SPC-0001", duplicateSlot));
    }

    [Fact]
    public void DestroyedDroneRetainsCrew()
    {
        WriteScenario();
        using var engine = Load();

        var wing = new[]
        {
            Drone("DRN-1", "BAY-01", 0, "CHR-PILOT-1"),
            Drone("DRN-2", "BAY-01", 1, "CHR-PILOT-2", destroyed: true),
            Drone("DRN-3", "BAY-02", 0, "CHR-PILOT-3"),
            Drone("DRN-4", "BAY-02", 1, "CHR-PILOT-4"),
        };
        engine.SetOwnedWingForTests("SPC-0001", wing);

        var snapshot = engine.CaptureSnapshot();
        var carrier = snapshot.CarrierOperations!;
        Assert.Equal(4, carrier.OwnedDrones.Length);
        Assert.Contains(carrier.OwnedDrones, d => d.Id == "DRN-2" && d.ReadinessReasons.Contains("destroyed"));
        Assert.Equal(6, carrier.Crew.Length);
    }
}
