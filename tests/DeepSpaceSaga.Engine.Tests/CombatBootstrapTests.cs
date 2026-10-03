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

    private static GameDataRegistry Registry(int maxHp = 450) => GameDataRegistry.Create(
        moduleCategories: [new("module.torpedo.launcher", "Launcher", 1, [CombatCommandTypes.Fire])],
        moduleTypes: [new("module.torpedo.launcher.basic", "Launcher", 1, 2000, 60, 0, [CombatCommandTypes.Fire],
            TorpedoDamage: 150, TorpedoSpeedKmS: 3, TorpedoTurnRateDegPerSec: 90)],
        itemTypes: [],
        commandDefinitions: [new(CombatCommandTypes.Fire, "Fire", Target: "object", Type: "module.torpedo.launcher")],
        shipClasses: [new("ship.tetrarch", maxHp)]);

    private static ScenarioFile Scenario() => ScenarioLoader.LoadFromJson(ScenarioJson);
    private static HullCombatSnapshot Hull(AuthoritativeSnapshot snapshot, string id = "player") =>
        Assert.IsType<HullCombatSnapshot>(snapshot.Objects.Single(o => o.ObjectId == id).HullCombat);

    [Theory]
    [InlineData(450)]
    [InlineData(900)]
    [InlineData(int.MaxValue)]
    public void Tetrarch_starts_at_configured_hp_and_launcher_ready(int maxHp)
    {
        using var engine = new SimulationEngine(Registry(maxHp));
        engine.LoadScenario(Scenario());
        var snapshot = engine.CaptureSnapshot();
        Assert.Equal(new HullCombatSnapshot("ship.tetrarch", maxHp, maxHp), Hull(snapshot));
        Assert.Equal(Hull(snapshot), Hull(snapshot, "pirate"));
        var module = Assert.Single(snapshot.InstalledModules);
        Assert.Equal(new LauncherCombatSnapshot(null, 3, 90, 150), module.LauncherCombat);
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
    [InlineData("hp-above-max")]
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
            case "hp-above-max": player["hullHitPoints"] = 451; break;
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
    public void Explicit_hp_survives_file_load_without_refilling(int hp)
    {
        var json = JsonNode.Parse(ScenarioJson)!;
        json["gameState"]!["gameTimeMs"] = 1000;
        json["gameState"]!["spaceObjects"]![0]!["hullHitPoints"] = hp;
        string path = Path.Combine(Path.GetTempPath(), $"combat-bootstrap-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, json.ToJsonString());
            using var engine = new SimulationEngine(Registry());
            engine.LoadScenario(ScenarioLoader.LoadFromFile(path, allowNonZeroGameTime: true), isSave: true);
            Assert.Equal(new HullCombatSnapshot("ship.tetrarch", hp, 450), Hull(engine.CaptureSnapshot()));
            // Exercise the real capture -> file -> loader -> new engine boundary as well.
            File.WriteAllText(path, JsonSerializer.Serialize(engine.CaptureSaveState()));
            using var restored = new SimulationEngine(Registry());
            restored.LoadScenario(ScenarioLoader.LoadFromFile(path, allowNonZeroGameTime: true), isSave: true);
            Assert.Equal(Hull(engine.CaptureSnapshot()), Hull(restored.CaptureSnapshot()));
            Assert.Equal(Hull(engine.CaptureSnapshot(), "pirate"), Hull(restored.CaptureSnapshot(), "pirate"));
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
        Assert.NotNull(Hull(snapshot)); // The controlled ship is always known.
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
        engine.LoadScenario(legacy, isSave: true);
        snapshot = engine.CaptureSnapshot();
        Assert.All(snapshot.Objects, o => Assert.Null(o.HullCombat));
        Assert.Empty(snapshot.InstalledModules);
        engine.LoadScenario(Scenario());
        Assert.Equal(450, Hull(engine.CaptureSnapshot()).CurrentHp);
        Assert.Null(Assert.Single(engine.CaptureSnapshot().InstalledModules).LauncherCombat!.ActiveTorpedoObjectId);
    }
}
