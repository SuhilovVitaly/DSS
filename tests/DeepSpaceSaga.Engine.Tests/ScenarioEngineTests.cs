using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public class ScenarioEngineTests
{
    private const string DefaultScenarioJson = """
    {
      "scenarioMetadata": { "scenarioId": "default", "name": "Default Scenario" },
      "gameState": {
        "gameTimeMs": 0, "currentSpeed": "Speed0",
        "playerShipObjectId": "SPC-0001",
        "focus": { "mode": "Attached", "objectId": "SPC-0001" },
        "spaceObjects": [
          { "objectId": "SPC-0001", "objectType": "PlayerShip", "persistenceType": "Permanent",
            "positionX": 10000, "positionY": 10000, "speedMps": 0, "directionDegrees": 0,
            "movementType": "Stationary" },
          { "objectId": "SPC-0002", "objectType": "Station", "persistenceType": "Permanent",
            "positionX": 11000, "positionY": 11000, "speedMps": 0, "directionDegrees": 0,
            "movementType": "Stationary" },
          { "objectId": "SPC-0003", "objectType": "Asteroid", "persistenceType": "Temporary",
            "positionX": 10400, "positionY": 10000, "speedMps": 100, "directionDegrees": 270,
            "movementType": "Linear", "massKg": 1000000, "compositionType": "Silicate" },
          { "objectId": "SPC-0004", "objectType": "Asteroid", "persistenceType": "Temporary",
            "positionX": 10000, "positionY": 10450, "speedMps": 200, "directionDegrees": 0,
            "movementType": "Linear", "massKg": 1000000, "compositionType": "Ice" }
        ]
      }
    }
    """;

    [Fact]
    public void Engine_loads_scenario_with_4_objects()
    {
        var scenario = ScenarioLoader.LoadFromJson(DefaultScenarioJson);
        var engine = new SimulationEngine();
        engine.LoadScenario(scenario);

        Assert.Equal("SPC-0001", engine.PlayerShipObjectId);
        Assert.Equal(SimulationSpeed.Speed0, engine.CurrentSpeed);
    }

    [Fact]
    public async Task First_snapshot_has_scenario_objects()
    {
        var scenario = ScenarioLoader.LoadFromJson(DefaultScenarioJson);
        var engine = new SimulationEngine();
        engine.LoadScenario(scenario);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        AuthoritativeSnapshot? first = null;

        await foreach (var snapshot in engine.RunAsync(cts.Token))
        {
            first = snapshot;
            break;
        }

        Assert.NotNull(first);
        Assert.Equal(4, first.Objects.Length);
        Assert.Equal("SPC-0001", first.PlayerShipObjectId);

        // Objects at expected positions (gameTime=0, Speed0, so no movement)
        Assert.Contains(first.Objects, o => o.ObjectId == "SPC-0001" && o.X == 10000 && o.Y == 10000);
        Assert.Contains(first.Objects, o => o.ObjectId == "SPC-0002" && o.X == 11000 && o.Y == 11000);
        Assert.Contains(first.Objects, o => o.ObjectId == "SPC-0003" && o.X == 10400 && o.Y == 10000);
        Assert.Contains(first.Objects, o => o.ObjectId == "SPC-0004" && o.X == 10000 && o.Y == 10450);
    }

    [Fact]
    public async Task First_snapshot_has_GameTime_0_and_Speed0()
    {
        var scenario = ScenarioLoader.LoadFromJson(DefaultScenarioJson);
        var engine = new SimulationEngine();
        engine.LoadScenario(scenario);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        AuthoritativeSnapshot? first = null;

        await foreach (var snapshot in engine.RunAsync(cts.Token))
        {
            first = snapshot;
            break;
        }

        Assert.NotNull(first);
        Assert.Equal(0, first.GameTimeMs);
        Assert.Equal(SimulationSpeed.Speed0, first.CurrentSpeed);
    }

    [Fact]
    public async Task Probe_1_is_not_in_first_snapshot()
    {
        var scenario = ScenarioLoader.LoadFromJson(DefaultScenarioJson);
        var engine = new SimulationEngine();
        engine.LoadScenario(scenario);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        AuthoritativeSnapshot? first = null;

        await foreach (var snapshot in engine.RunAsync(cts.Token))
        {
            first = snapshot;
            break;
        }

        Assert.NotNull(first);
        Assert.DoesNotContain(first.Objects, o => o.ObjectId == "probe-1");
    }

    [Fact]
    public async Task First_snapshot_is_yielded_immediately()
    {
        // The initial snapshot must arrive without the 1-second delay.
        // We verify by timing: if it takes < 500ms, it's immediate.
        var scenario = ScenarioLoader.LoadFromJson(DefaultScenarioJson);
        var engine = new SimulationEngine();
        engine.LoadScenario(scenario);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var sw = System.Diagnostics.Stopwatch.StartNew();

        await foreach (var snapshot in engine.RunAsync(cts.Token))
        {
            sw.Stop();
            Assert.True(sw.ElapsedMilliseconds < 500,
                $"First snapshot took {sw.ElapsedMilliseconds}ms, expected < 500ms (immediate)");
            break;
        }
    }

    [Fact]
    public void Can_load_real_scenario_file()
    {
        // Walk up from the test output directory to find the repo root,
        // then into the Client project where the scenario lives.
        string scenarioPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "DeepSpaceSaga.Client", "Scenarios", "Default", "scenario.json"));

        // Fallback for environments where the output structure differs (CI / dotnet test from sln dir)
        if (!File.Exists(scenarioPath))
        {
            scenarioPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, "Scenarios", "Default", "scenario.json"));
        }

        Assert.True(File.Exists(scenarioPath),
            $"Real scenario file not found at '{scenarioPath}'. " +
            "Ensure it is copied to the output directory or the test is running from the repo root.");

        var scenario = ScenarioLoader.LoadFromFile(scenarioPath);
        Assert.Equal("default", scenario.Metadata.ScenarioId);
        Assert.Equal("SPC-0001", scenario.GameState.PlayerShipObjectId);
        Assert.Equal(4, scenario.GameState.SpaceObjects.Count);

        var playerShip = scenario.GameState.SpaceObjects
            .Single(o => o.ObjectId == scenario.GameState.PlayerShipObjectId);
        Assert.Equal(2, playerShip.Modules?.Count);
        var cargoModule = Assert.Single(playerShip.Modules ?? [], m => m.ModuleId == "MOD-PLAYER-CARGO-01");
        Assert.Equal("module.container.basic", cargoModule.ModuleTypeId);
        var engineModule = Assert.Single(playerShip.Modules ?? [], m => m.ModuleId == "MOD-PLAYER-ENGINE-01");
        Assert.Equal("module.engine.basic", engineModule.ModuleTypeId);
        Assert.Equal([0], engineModule.OccupiedCells);
        Assert.Equal("On", engineModule.PowerState);
        Assert.Equal("Ready", engineModule.OperationalState);

        var energyCells = Assert.Single(cargoModule.Cargo ?? []);
        Assert.Equal("item.energy-cells", energyCells.ItemTypeId);
        Assert.Equal(1_000, energyCells.Quantity);
    }

    [Fact]
    public void Can_create_engine_from_real_settings_file()
    {
        string settingsPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "DeepSpaceSaga.Client", "Settings.json"));

        if (!File.Exists(settingsPath))
        {
            settingsPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, "Settings.json"));
        }

        var engine = EngineContentLoader.CreateEngineFromSettingsFile(settingsPath);

        Assert.Equal("SPC-0001", engine.PlayerShipObjectId);
        var playerShip = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == "SPC-0001");
        Assert.Equal(2, playerShip.Modules.Length);
        var cargoModule = Assert.Single(playerShip.Modules, m => m.ModuleId == "MOD-PLAYER-CARGO-01");
        var engineModule = Assert.Single(playerShip.Modules, m => m.ModuleId == "MOD-PLAYER-ENGINE-01");
        Assert.Equal(0, cargoModule.ModuleTypeIndex);
        Assert.Equal(1, engineModule.ModuleTypeIndex);
        Assert.Null(engineModule.ActiveCycle);
        var cargo = Assert.Single(cargoModule.Cargo);
        Assert.Equal(0, cargo.ItemTypeIndex);
    }

    [Theory]
    [InlineData("\"turnStepDegrees\": 1,")]
    [InlineData("\"maxSpeedMps\": null, \"turnStepDegrees\": 1,")]
    [InlineData("\"maxSpeedMps\": 0, \"turnStepDegrees\": 1,")]
    [InlineData("\"maxSpeedMps\": -1, \"turnStepDegrees\": 1,")]
    [InlineData("\"maxSpeedMps\": 4000,")]
    [InlineData("\"maxSpeedMps\": 4000, \"turnStepDegrees\": 0,")]
    [InlineData("\"maxSpeedMps\": 4000, \"turnStepDegrees\": -1,")]
    public void Engine_content_requires_positive_engine_motion_parameters(string engineParameters)
    {
        string directory = Path.Combine(Path.GetTempPath(), $"dss-content-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "Settings.json"), """
            { "typeData": { "moduleTypes": "module-types.json", "itemTypes": "item-types.json", "commandDefinitions": "command-definitions.json" }, "defaultScenario": "scenario.json" }
            """);
            File.WriteAllText(Path.Combine(directory, "command-definitions.json"), """{ "commandDefinitions": [] }""");
            File.WriteAllText(Path.Combine(directory, "module-types.json"), $$"""
            {
              "moduleTypes": [
                {
                  "typeId": "module.engine.basic",
                  "displayName": "Engine",
                  "slotSize": 1,
                  "massKg": 1,
                  "structurePointsMax": 1,
                  "powerConsumptionW": 0,
                  "cargoCapacityKg": null,
                  {{engineParameters}}
                  "commandTypeIds": []
                }
              ]
            }
            """);

            var exception = Assert.Throws<ContentException>(() => EngineContentLoader.CreateEngineFromSettingsFile(
                Path.Combine(directory, "Settings.json")));
            Assert.Contains("requires", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LoadScenario_rejects_module_cell_count_that_differs_from_type_slot_size()
    {
        var scenario = ScenarioLoader.LoadFromJson(ScenarioWithModule(occupiedCells: "1"));
        var engine = CreateEngineWithBasicTypes();

        Assert.Throws<ScenarioException>(() => engine.LoadScenario(scenario));
    }

    [Fact]
    public void LoadScenario_rejects_duplicate_module_cells()
    {
        var scenario = ScenarioLoader.LoadFromJson(ScenarioWithModule(occupiedCells: "1, 1, 2, 3"));
        var engine = CreateEngineWithBasicTypes();

        Assert.Throws<ScenarioException>(() => engine.LoadScenario(scenario));
    }

    [Fact]
    public void LoadScenario_rejects_modules_overlapping_on_same_platform()
    {
        const string extraModule = """
        ,
                  {
                    "moduleId": "MOD-2",
                    "moduleTypeId": "module.container.basic",
                    "platformIndex": 0,
                    "occupiedCells": [4, 5, 6, 7],
                    "structurePoints": 400,
                    "powerState": "On",
                    "operationalState": "Ready",
                    "cargo": []
                  }
        """;

        var scenario = ScenarioLoader.LoadFromJson(ScenarioWithModule(extraModules: extraModule));
        var engine = CreateEngineWithBasicTypes();

        Assert.Throws<ScenarioException>(() => engine.LoadScenario(scenario));
    }

    [Fact]
    public void LoadScenario_resolves_typeIds_case_sensitively()
    {
        var scenario = ScenarioLoader.LoadFromJson(
            ScenarioWithModule(moduleTypeId: "Module.Container.Basic"));
        var engine = CreateEngineWithBasicTypes();

        Assert.Throws<ContentException>(() => engine.LoadScenario(scenario));
    }

    [Fact]
    public void Public_engine_without_registry_rejects_scenarios_with_modules_explicitly()
    {
        var scenario = ScenarioLoader.LoadFromJson(ScenarioWithModule());
        var engine = new SimulationEngine();

        var ex = Assert.Throws<ScenarioException>(() => engine.LoadScenario(scenario));
        Assert.Contains("SimulationEngine.CreateFromSettingsFile", ex.Message);
    }

    [Fact]
    public void Failed_LoadScenario_keeps_existing_engine_world()
    {
        var engine = CreateEngineWithBasicTypes();
        var oldScenario = ScenarioLoader.LoadFromJson(ScenarioWithModule(
            objectId: "OLD-SHIP",
            currentSpeed: "Speed2"));
        engine.LoadScenario(oldScenario);

        var invalidScenario = ScenarioLoader.LoadFromJson(ScenarioWithModule(
            objectId: "NEW-SHIP",
            currentSpeed: "Speed3",
            occupiedCells: "1"));

        Assert.Throws<ScenarioException>(() => engine.LoadScenario(invalidScenario));

        Assert.Equal("OLD-SHIP", engine.PlayerShipObjectId);
        Assert.Equal(SimulationSpeed.Speed2, engine.CurrentSpeed);
        var obj = Assert.Single(engine.RuntimeObjects);
        Assert.Equal("OLD-SHIP", obj.InitialMotion.ObjectId);
        Assert.Single(obj.Modules);
    }

    [Fact]
    public void Public_settings_factory_loads_scenario_with_modules()
    {
        string settingsPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "DeepSpaceSaga.Client", "Settings.json"));

        if (!File.Exists(settingsPath))
        {
            settingsPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, "Settings.json"));
        }

        var engine = SimulationEngine.CreateFromSettingsFile(settingsPath);

        Assert.Equal("SPC-0001", engine.PlayerShipObjectId);
        Assert.Contains(engine.RuntimeObjects, o => o.Modules.Length > 0);
    }

    [Fact]
    public void LoadScenario_sets_speed_from_scenario()
    {
        var json = """
        {
          "scenarioMetadata": { "scenarioId": "x", "name": "x" },
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed2",
            "playerShipObjectId": "SHIP",
            "spaceObjects": [
              { "objectId": "SHIP", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary" }
            ]
          }
        }
        """;

        var scenario = ScenarioLoader.LoadFromJson(json);
        var engine = new SimulationEngine();
        engine.LoadScenario(scenario);

        Assert.Equal(SimulationSpeed.Speed2, engine.CurrentSpeed);
    }

    private static SimulationEngine CreateEngineWithBasicTypes()
    {
        var registry = GameDataRegistry.Create(
            [
                new ModuleTypeDefinition(
                    "module.container.basic",
                    "Container",
                    SlotSize: 4,
                    MassKg: 20000,
                    StructurePointsMax: 400,
                    PowerConsumptionW: 0,
                    CommandTypeIds: ImmutableArray<string>.Empty,
                    CargoCapacityKg: 100000,
                    MaxSpeedMps: null,
                    TurnStepDegrees: null)
            ],
            [
                new ItemTypeDefinition(
                    "item.energy-cells",
                    "Energy Cells",
                    UnitMassKg: 10)
            ],
            []);

        return new SimulationEngine(registry);
    }

    private static string ScenarioWithModule(
        string objectId = "SHIP",
        string currentSpeed = "Speed1",
        string moduleTypeId = "module.container.basic",
        string occupiedCells = "1, 2, 3, 4",
        string extraModules = "")
    {
        return $$"""
        {
          "scenarioMetadata": { "scenarioId": "x", "name": "x" },
          "gameState": {
            "gameTimeMs": 0,
            "currentSpeed": "{{currentSpeed}}",
            "playerShipObjectId": "{{objectId}}",
            "spaceObjects": [
              {
                "objectId": "{{objectId}}",
                "objectType": "PlayerShip",
                "persistenceType": "Permanent",
                "positionX": 0,
                "positionY": 0,
                "speedMps": 0,
                "directionDegrees": 0,
                "movementType": "Stationary",
                "modules": [
                  {
                    "moduleId": "MOD-1",
                    "moduleTypeId": "{{moduleTypeId}}",
                    "platformIndex": 0,
                    "occupiedCells": [{{occupiedCells}}],
                    "structurePoints": 400,
                    "powerState": "On",
                    "operationalState": "Ready",
                    "cargo": [
                      { "itemTypeId": "item.energy-cells", "quantity": 1 }
                    ]
                  }{{extraModules}}
                ]
              }
            ]
          }
        }
        """;
    }
}
