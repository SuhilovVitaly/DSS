using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class Ep0029Us0001Tk0004Tests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"dss-ep0029-us0001-tk0004-{Guid.NewGuid():N}");
    private string SettingsPath => Path.Combine(_directory, "settings.json");
    private string ScenarioPath => Path.Combine(_directory, "scenario.json");

    public Ep0029Us0001Tk0004Tests()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "categories.json"),
            """{"moduleTypes":[{"typeId":"module.drone-bay","displayName":"Drone bay","slotSize":1,"commandTypeIds":[]}]}""");
        File.WriteAllText(Path.Combine(_directory, "implementations.json"), """
            {"moduleImplementations":[
              {"typeId":"module.drone-bay.medium","displayName":"M","type":"module.drone-bay","massKg":4500,"structurePointsMax":250,"powerConsumptionW":0,"droneCapacity":2,"footprintWidth":2,"footprintHeight":2},
              {"typeId":"module.drone-bay.small","displayName":"S","type":"module.drone-bay","massKg":2500,"structurePointsMax":150,"powerConsumptionW":0,"droneCapacity":1,"footprintWidth":1,"footprintHeight":2}
            ]}
            """);
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

    private static JsonObject Bay2x2(string moduleId, int ax, int ay, string power = "On",
        string operational = "Ready", int sp = 250, bool destroyed = false)
    {
        var cells = new JsonArray();
        foreach (var (x, y) in new[] { (ax, ay), (ax + 1, ay), (ax, ay + 1), (ax + 1, ay + 1) })
            cells.Add(new JsonObject { ["x"] = x, ["y"] = y });
        return new JsonObject
        {
            ["moduleId"] = moduleId,
            ["moduleTypeId"] = "module.drone-bay.medium",
            ["occupiedCells"] = cells,
            ["structurePoints"] = sp,
            ["powerState"] = power,
            ["operationalState"] = operational,
            ["cargo"] = new JsonArray(),
            ["destroyed"] = destroyed,
        };
    }

    private void WriteScenario(params JsonObject[] modules)
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
            ["modules"] = new JsonArray(modules.Select(m => (JsonNode?)m).ToArray()),
        };
        var scenario = new JsonObject
        {
            ["scenarioMetadata"] = new JsonObject { ["scenarioId"] = "tk0004", ["name"] = "tk0004" },
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
    public void OverlapAndOutsideHullRejected()
    {
        // Two overlapping 2x2 medium bays.
        WriteScenario(Bay2x2("BAY-01", 0, 0), Bay2x2("BAY-02", 0, 0));
        Assert.Throws<ScenarioException>(() => Load());

        // A 2x2 medium bay protruding past the 5x5 hull edge.
        WriteScenario(Bay2x2("BAY-01", 4, 0));
        Assert.Throws<ScenarioException>(() => Load());
    }

    [Fact]
    public void OfflineBayKeepsInventoryButBlocksLaunch()
    {
        WriteScenario(Bay2x2("BAY-ONLINE", 0, 0), Bay2x2("BAY-OFFLINE", 2, 0, power: "Off"));
        using var engine = Load();
        var snapshot = engine.CaptureSnapshot();

        Assert.NotNull(snapshot.CarrierOperations);
        var hangars = snapshot.CarrierOperations!.Hangars;
        Assert.Equal(4, hangars.Length); // two medium bays * two slots each
        Assert.Contains(hangars, h => h.HangarModuleId == "BAY-OFFLINE" && h.SlotIndex == 0);
        Assert.Contains(hangars, h => h.HangarModuleId == "BAY-OFFLINE" && h.SlotIndex == 1);

        // The offline bay keeps its slots (inventory), but is not ready to launch.
        Assert.False(engine.IsHangarReady("BAY-OFFLINE"));
        Assert.True(engine.IsHangarReady("BAY-ONLINE"));
    }

    [Fact]
    public void IndependentHullProjection()
    {
        WriteScenario(Bay2x2("BAY-01", 0, 0));
        using var engine = Load();
        engine.SetCarrierHullCurrentForTests("SPC-0001", 317);
        var snapshot = engine.CaptureSnapshot();

        Assert.NotNull(snapshot.CarrierOperations);
        Assert.Equal(317, snapshot.CarrierOperations!.HullCurrent);
        Assert.Equal(400, snapshot.CarrierOperations.HullMax);
        Assert.Equal("ship.tetrarch", snapshot.CarrierOperations.ShipClassId);
    }
}
