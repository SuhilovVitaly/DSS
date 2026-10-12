using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatBootstrapTests
{
    private const string ScenarioJson = """
        {"scenarioMetadata":{"scenarioId":"combat-bootstrap","name":"Combat bootstrap"},"gameState":{
          "masterSeed":42,"gameTimeMs":0,"currentSpeed":"Speed0","playerShipObjectId":"player",
          "spaceObjects":[
            {"objectId":"player","objectType":"PlayerShip","persistenceType":"Permanent",
              "name":"Tetrarch","positionX":100,"positionY":200,"speedMps":0,"directionDegrees":0,
              "movementType":"Stationary","image":"Images/SpaceObjects/Ships/Tetrarch.png","shipClassId":"ship.tetrarch",
              "hullLayout":{"width":5,"height":3,"cells":[{"x":3,"y":2}]},
              "modules":[{"moduleId":"launcher","moduleTypeId":"module.torpedo.launcher.basic",
                "occupiedCells":[{"x":3,"y":2}],"structurePoints":60,"powerState":"On","operationalState":"Ready"}]},
            {"objectId":"pirate","objectType":"NpcShip","persistenceType":"Permanent",
              "positionX":400,"positionY":500,"speedMps":400,"directionDegrees":120,
              "movementType":"Linear","shipClassId":"ship.tetrarch","isKnown":true,
              "hullLayout":{"width":5,"height":3,"cells":[{"x":3,"y":2}]},
              "modules":[{"moduleId":"launcher","moduleTypeId":"module.torpedo.launcher.basic",
                "occupiedCells":[{"x":3,"y":2}],"structurePoints":60,"powerState":"On","operationalState":"Ready"}]}
          ]}}
        """;

    private static GameDataRegistry Registry() => GameDataRegistry.Create(
        moduleCategories: [new("module.torpedo.launcher", "Launcher", 1, [CombatCommandTypes.Fire])],
        moduleTypes: [new("module.torpedo.launcher.basic", "Launcher", 1, 2000, 60, 0, [CombatCommandTypes.Fire],
            TorpedoDamage: 150, TorpedoSpeedKmS: 3, TorpedoTurnRateDegPerSec: 90)],
        itemTypes: [],
        commandDefinitions: [new(CombatCommandTypes.Fire, "Fire", Target: "object", Type: "module.torpedo.launcher")],
        shipClasses: [new("ship.tetrarch")]);

    private static ScenarioFile Scenario() => ScenarioLoader.LoadFromJson(ScenarioJson);
    [Theory]
    [InlineData(450)]
    [InlineData(900)]
    [InlineData(int.MaxValue)]
    public void Legacy_scenario_hull_hp_and_launcher_bootstrap(int maxHp)
    {
        using var engine = new SimulationEngine(Registry());
        var scenario = Scenario();
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o with { HullHitPoints = maxHp, HullHitPointsMax = maxHp }).ToArray()
            }
        };
        engine.LoadScenario(scenario);
        var snapshot = engine.CaptureSnapshot();
        Assert.All(snapshot.Objects, obj => Assert.Null(obj.HullCombat));
        Assert.Equal("ship.tetrarch", engine.CaptureSaveState().GameState.SpaceObjects.Single(obj => obj.ObjectId == "player").ShipClassId);
        var module = Assert.Single(snapshot.InstalledModules);
        Assert.Equal(3, module.LauncherCombat!.SpeedKmS);
        Assert.Equal(90, module.LauncherCombat.TurnRateDegPerSec);
        Assert.Equal(150, module.LauncherCombat.Damage);
        Assert.Empty(module.LauncherCombat.ActiveTorpedoIds);
        Assert.Equal("Ready", module.OperationalState);
        Assert.Null(module.ActiveCommandType);
        Assert.Equal(0, snapshot.PlayerCrewCount);
        Assert.Equal(0.4, snapshot.Objects.Single(o => o.ObjectId == "pirate").SpeedKmS);
        Assert.Equal(120, snapshot.Objects.Single(o => o.ObjectId == "pirate").Direction);
    }

    [Fact]
    public void Unclassified_ship_remains_invulnerable()
    {
        var scenario = Scenario();
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o with { ShipClassId = null }).ToArray()
            }
        };
        using var engine = new SimulationEngine(Registry());
        engine.LoadScenario(scenario);
        Assert.All(engine.CaptureSnapshot().Objects, o => Assert.Null(o.HullCombat));
    }

    [Theory]
    [InlineData("unknown-class")]
    [InlineData("wrong-case-class")]
    [InlineData("blank-class")]
    [InlineData("hp-without-class")]
    [InlineData("hp-zero")]
    [InlineData("hp-negative")]
    [InlineData("hp-overflow")]
    [InlineData("hp-fractional")]
    [InlineData("class-on-station")]
    [InlineData("launcher-on-station")]
    [InlineData("overlapping-launcher")]
    [InlineData("outside-hull")]
    public void Invalid_class_hp_and_overlapping_launcher_are_rejected(string failure)
    {
        using var engine = new SimulationEngine(Registry());
        engine.LoadScenario(Scenario());
        var before = engine.CaptureSnapshot();
        var json = JsonNode.Parse(ScenarioJson)!;
        var player = json["gameState"]!["spaceObjects"]![0]!;
        var pirate = json["gameState"]!["spaceObjects"]![1]!;
        switch (failure)
        {
            case "unknown-class": player["shipClassId"] = "ship.missing"; break;
            case "wrong-case-class": player["shipClassId"] = "SHIP.TETRARCH"; break;
            case "blank-class": player["shipClassId"] = " "; break;
            case "hp-without-class": player["shipClassId"] = null; player["hullHitPoints"] = 300; break;
            case "hp-zero": player["hullHitPoints"] = 0; break;
            case "hp-negative": player["hullHitPoints"] = -1; break;
            case "hp-overflow": player["hullHitPoints"] = (long)int.MaxValue + 1; break;
            case "hp-fractional": player["hullHitPoints"] = 1.5; break;
            case "class-on-station": pirate["objectType"] = "Station"; break;
            case "launcher-on-station": pirate["objectType"] = "Station"; pirate["shipClassId"] = null; break;
            case "overlapping-launcher":
                var duplicate = player["modules"]![0]!.DeepClone();
                duplicate["moduleId"] = "second-launcher";
                player["modules"]!.AsArray().Add(duplicate);
                break;
            case "outside-hull": player["modules"]![0]!["occupiedCells"]![0]!["x"] = 4; break;
            default: throw new ArgumentOutOfRangeException(nameof(failure));
        }

        Assert.Throws<ScenarioException>(() => engine.LoadScenario(ScenarioLoader.LoadFromJson(json.ToJsonString())));
        var after = engine.CaptureSnapshot();
        Assert.Equal(JsonSerializer.Serialize(before.Objects), JsonSerializer.Serialize(after.Objects));
        Assert.Equal(JsonSerializer.Serialize(before.InstalledModules), JsonSerializer.Serialize(after.InstalledModules));
        Assert.Equal(before.GameTimeMs, after.GameTimeMs);
        Assert.Equal(before.PlayerShipObjectId, after.PlayerShipObjectId);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(300)]
    [InlineData(450)]
    public void Legacy_hp_is_not_reemitted_after_file_load(int hp)
    {
        var json = JsonNode.Parse(ScenarioJson)!;
        json["gameState"]!["gameTimeMs"] = 1000;
        json["gameState"]!["spaceObjects"]![0]!["hullHitPoints"] = hp;
        string path = Path.Combine(Path.GetTempPath(), $"combat-bootstrap-{Guid.NewGuid():N}.json");
        try
        {
            using var source = new SimulationEngine(Registry());
            source.LoadScenario(ScenarioLoader.LoadFromJson(json.ToJsonString(), true));
            File.WriteAllText(path, ScenarioLoader.Serialize(source.CaptureSaveState()));
            using var engine = new SimulationEngine(Registry());
            engine.LoadScenario(ScenarioLoader.LoadFromFile(path, allowNonZeroGameTime: true), isSave: true);
            Assert.Null(engine.CaptureSnapshot().Objects.Single(obj => obj.ObjectId == "player").HullCombat);
            Assert.Null(engine.CaptureSaveState().GameState.SpaceObjects.Single(obj => obj.ObjectId == "player").HullHitPoints);
            // Exercise the real capture -> file -> loader -> new engine boundary as well.
            File.WriteAllText(path, JsonSerializer.Serialize(engine.CaptureSaveState()));
            using var restored = new SimulationEngine(Registry());
            restored.LoadScenario(ScenarioLoader.LoadFromFile(path, allowNonZeroGameTime: true), isSave: true);
            Assert.Null(restored.CaptureSaveState().GameState.SpaceObjects.Single(obj => obj.ObjectId == "player").HullHitPoints);
            Assert.Equal("ship.tetrarch", restored.CaptureSaveState().GameState.SpaceObjects.Single(obj => obj.ObjectId == "player").ShipClassId);
            Assert.Equal(Assert.Single(engine.CaptureSnapshot().InstalledModules).LauncherCombat,
                Assert.Single(restored.CaptureSnapshot().InstalledModules).LauncherCombat);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Unknown_ship_masks_class_and_hp_and_new_session_clears_combat()
    {
        var scenario = Scenario();
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o with { IsKnown = false }).ToArray()
            }
        };
        using var engine = new SimulationEngine(Registry());
        engine.LoadScenario(scenario);
        var snapshot = engine.CaptureSnapshot();
        Assert.Null(snapshot.Objects.Single(o => o.ObjectId == "player").HullCombat);
        var pirate = snapshot.Objects.Single(o => o.ObjectId == "pirate");
        Assert.Null(pirate.HullCombat);
        Assert.Null(pirate.ObjectType);

        var legacy = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o with { ShipClassId = null, Modules = null }).ToArray()
            }
        };
        Assert.Contains("incompatible_save_version", Assert.Throws<ScenarioException>(() => engine.LoadScenario(legacy, isSave: true)).Message);
        engine.LoadScenario(legacy); // New scenario clears combat; an old save cannot.
        snapshot = engine.CaptureSnapshot();
        Assert.All(snapshot.Objects, o => Assert.Null(o.HullCombat));
        Assert.Empty(snapshot.InstalledModules);
        engine.LoadScenario(Scenario());
        Assert.Null(engine.CaptureSnapshot().Objects.Single(o => o.ObjectId == "player").HullCombat);
        Assert.Empty(Assert.Single(engine.CaptureSnapshot().InstalledModules).LauncherCombat!.ActiveTorpedoIds);
    }
}
