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
        Assert.Equal(9, playerShip.Modules?.Count);
        var cargoModule = Assert.Single(playerShip.Modules ?? [], m => m.ModuleId == "MOD-PLAYER-CARGO-01");
        Assert.Equal("module.container.basic", cargoModule.ModuleTypeId);
        Assert.Equal(2, cargoModule.PlatformIndex);
        Assert.Equal([0, 1, 2, 3], cargoModule.OccupiedCells);
        var engineModule = Assert.Single(playerShip.Modules ?? [], m => m.ModuleId == "MOD-PLAYER-ENGINE-01");
        Assert.Equal("module.engine.basic", engineModule.ModuleTypeId);
        Assert.Equal(1, engineModule.PlatformIndex);
        Assert.Equal([1], engineModule.OccupiedCells);
        Assert.Equal("On", engineModule.PowerState);
        Assert.Equal("Ready", engineModule.OperationalState);

        var bridgeModule = Assert.Single(playerShip.Modules ?? [], m => m.ModuleId == "MOD-PLAYER-BRIDGE-01");
        Assert.Equal("module.bridge-navigation-computer.basic", bridgeModule.ModuleTypeId);
        Assert.Equal(1, bridgeModule.PlatformIndex);
        var generatorModule = Assert.Single(playerShip.Modules ?? [], m => m.ModuleId == "MOD-PLAYER-GENERATOR-01");
        Assert.Equal("module.generator.basic", generatorModule.ModuleTypeId);
        Assert.Equal(1, generatorModule.PlatformIndex);
        var batteryModule = Assert.Single(playerShip.Modules ?? [], m => m.ModuleId == "MOD-PLAYER-BATTERY-01");
        Assert.Equal("module.battery.basic", batteryModule.ModuleTypeId);
        Assert.Equal(1, batteryModule.PlatformIndex);
        var drillingModule = Assert.Single(playerShip.Modules ?? [], m => m.ModuleId == "MOD-PLAYER-DRILLING-01");
        Assert.Equal("module.drilling-unit.basic", drillingModule.ModuleTypeId);
        Assert.Equal(3, drillingModule.PlatformIndex);
        var scannerModule = Assert.Single(playerShip.Modules ?? [], m => m.ModuleId == "MOD-PLAYER-SCANNER-01");
        Assert.Equal("module.scanner.mk1", scannerModule.ModuleTypeId);
        Assert.Equal(3, scannerModule.PlatformIndex);
        var habitationModule = Assert.Single(playerShip.Modules ?? [], m => m.ModuleId == "MOD-PLAYER-HABITATION-01");
        Assert.Equal("module.habitation.basic", habitationModule.ModuleTypeId);
        Assert.Equal(3, habitationModule.PlatformIndex);
        var combatLaserModule = Assert.Single(playerShip.Modules ?? [], m => m.ModuleId == "MOD-PLAYER-COMBAT-LASER-01");
        Assert.Equal("module.combat-laser.basic", combatLaserModule.ModuleTypeId);
        Assert.Equal(3, combatLaserModule.PlatformIndex);

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
        Assert.Equal(9, playerShip.Modules.Length);
        var cargoModule = Assert.Single(playerShip.Modules, m => m.ModuleId == "MOD-PLAYER-CARGO-01");
        var engineModule = Assert.Single(playerShip.Modules, m => m.ModuleId == "MOD-PLAYER-ENGINE-01");
        Assert.Equal(4, cargoModule.ModuleTypeIndex);
        Assert.Equal(1, engineModule.ModuleTypeIndex);
        Assert.Null(engineModule.ActiveCycle);
        Assert.Single(playerShip.Modules, m => m.ModuleId == "MOD-PLAYER-BRIDGE-01");
        Assert.Single(playerShip.Modules, m => m.ModuleId == "MOD-PLAYER-GENERATOR-01");
        Assert.Single(playerShip.Modules, m => m.ModuleId == "MOD-PLAYER-BATTERY-01");
        Assert.Single(playerShip.Modules, m => m.ModuleId == "MOD-PLAYER-DRILLING-01");
        Assert.Single(playerShip.Modules, m => m.ModuleId == "MOD-PLAYER-SCANNER-01");
        Assert.Single(playerShip.Modules, m => m.ModuleId == "MOD-PLAYER-HABITATION-01");
        Assert.Single(playerShip.Modules, m => m.ModuleId == "MOD-PLAYER-COMBAT-LASER-01");
        var cargo = Assert.Single(cargoModule.Cargo);
        Assert.Equal(0, cargo.ItemTypeIndex);
    }

    [Fact]
    public void Real_default_scenario_occupies_12_cells_on_3_platforms()
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

        var playerShip = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == "SPC-0001");
        Assert.Equal(9, playerShip.Modules.Length);
        Assert.Equal([1, 2, 3], playerShip.Modules.Select(m => m.PlatformIndex).Distinct().OrderBy(x => x));

        int totalCells = playerShip.Modules.Sum(m => m.OccupiedCells.Length);
        Assert.Equal(12, totalCells);

        // Each of the 3 platforms has exactly 4 distinct cells occupied:
        // no overlaps within a platform and no free cells (§50.9).
        foreach (var platformGroup in playerShip.Modules.GroupBy(m => m.PlatformIndex))
        {
            int distinctCells = platformGroup.SelectMany(m => m.OccupiedCells).Distinct().Count();
            Assert.Equal(4, distinctCells);
        }
    }

    [Fact]
    public void Real_default_scenario_active_module_types_have_valid_command_type_ids()
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

        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(settingsPath, out _, out _);

        var activeTypes = Enumerable.Range(0, registry.ModuleTypes.Count)
            .Select(i => registry.ModuleTypes.GetDefinition(i))
            .Where(t => t.CommandTypeIds.Length > 0)
            .ToArray();

        Assert.Equal(2, activeTypes.Length); // engine + scanner

        var engineType = Assert.Single(activeTypes, t => t.TypeId == "module.engine.basic");
        Assert.Equal(12, engineType.CommandTypeIds.Length); // 11 + engine.cancel-all
        Assert.Equal(100, engineType.BaseSuccessChancePercent);
        foreach (string commandTypeId in engineType.CommandTypeIds)
        {
            Assert.True(registry.CommandDefinitions.Contains(commandTypeId),
                $"Command '{commandTypeId}' of 'module.engine.basic' is missing from the command definitions registry.");
        }

        var scannerType = Assert.Single(activeTypes, t => t.TypeId == "module.scanner.mk1");
        Assert.Equal(3, scannerType.CommandTypeIds.Length); // deep-scan, common-scan, direct-scan
        Assert.Equal(100, scannerType.BaseSuccessChancePercent);
        foreach (string commandTypeId in scannerType.CommandTypeIds)
        {
            Assert.True(registry.CommandDefinitions.Contains(commandTypeId),
                $"Command '{commandTypeId}' of 'module.scanner.mk1' is missing from the command definitions registry.");
        }

        int passiveTypes = Enumerable.Range(0, registry.ModuleTypes.Count)
            .Count(i => registry.ModuleTypes.GetDefinition(i).CommandTypeIds.Length == 0);
        Assert.Equal(7, passiveTypes);
    }

    [Theory]
    [InlineData(null, 100)]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(99, 99)]
    [InlineData(100, 100)]
    public void Module_type_base_success_chance_percent_loads_expected_value(int? jsonValue, int expected)
    {
        WithContentDirectory(directory =>
        {
            WriteSettings(directory);
            WriteMinimalContent(directory);
            string field = jsonValue is null ? "" : $"\"baseSuccessChancePercent\": {jsonValue.Value},";
            File.WriteAllText(Path.Combine(directory, "module-types.json"), $$"""
            {
              "moduleTypes": [
                {
                  "typeId": "module.test.passive",
                  "displayName": "Test",
                  "slotSize": 1,
                  "massKg": 1,
                  "structurePointsMax": 1,
                  "powerConsumptionW": 0,
                  "commandTypeIds": [],
                  {{field}}
                  "baseCycleTimeMs": 0
                }
              ]
            }
            """);

            var registry = EngineContentLoader.LoadRegistryFromSettingsFile(
                Path.Combine(directory, "Settings.json"), out _, out _);

            Assert.Equal(expected, registry.ModuleTypes.GetDefinition(0).BaseSuccessChancePercent);
        });
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Module_type_base_success_chance_percent_out_of_range_throws(int invalidValue)
    {
        WithContentDirectory(directory =>
        {
            WriteSettings(directory);
            WriteMinimalContent(directory);
            File.WriteAllText(Path.Combine(directory, "module-types.json"), $$"""
            {
              "moduleTypes": [
                {
                  "typeId": "module.test.passive",
                  "displayName": "Test",
                  "slotSize": 1,
                  "massKg": 1,
                  "structurePointsMax": 1,
                  "powerConsumptionW": 0,
                  "commandTypeIds": [],
                  "baseSuccessChancePercent": {{invalidValue}},
                  "baseCycleTimeMs": 0
                }
              ]
            }
            """);

            var exception = Assert.Throws<ContentException>(() =>
                EngineContentLoader.LoadRegistryFromSettingsFile(
                    Path.Combine(directory, "Settings.json"), out _, out _));
            Assert.Contains("module.test.passive", exception.Message, StringComparison.Ordinal);
            Assert.Contains("baseSuccessChancePercent", exception.Message, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Module_type_base_success_chance_percent_rejects_fractional_value()
    {
        WithContentDirectory(directory =>
        {
            WriteSettings(directory);
            WriteMinimalContent(directory);
            File.WriteAllText(Path.Combine(directory, "module-types.json"), """
            {
              "moduleTypes": [
                {
                  "typeId": "module.test.passive",
                  "displayName": "Test",
                  "slotSize": 1,
                  "massKg": 1,
                  "structurePointsMax": 1,
                  "powerConsumptionW": 0,
                  "commandTypeIds": [],
                  "baseSuccessChancePercent": 99.5,
                  "baseCycleTimeMs": 0
                }
              ]
            }
            """);

            Assert.Throws<ContentException>(() =>
                EngineContentLoader.LoadRegistryFromSettingsFile(
                    Path.Combine(directory, "Settings.json"), out _, out _));
        });
    }

    [Theory]
    [InlineData("\"turnStepDegrees\": 1,")]
    [InlineData("\"maxSpeedMps\": null, \"turnStepDegrees\": 1,")]
    [InlineData("\"maxSpeedMps\": 0, \"turnStepDegrees\": 1,")]
    [InlineData("\"maxSpeedMps\": -1, \"turnStepDegrees\": 1,")]
    [InlineData("\"maxSpeedMps\": 4000,")]
    [InlineData("\"maxSpeedMps\": 4000, \"turnStepDegrees\": 0,")]
    [InlineData("\"maxSpeedMps\": 4000, \"turnStepDegrees\": -1,")]
    [InlineData("\"maxSpeedMps\": 4000, \"turnStepDegrees\": 1,")]
    [InlineData("\"maxSpeedMps\": 4000, \"turnStepDegrees\": 1, \"linearInertiaMps2\": 0,")]
    [InlineData("\"maxSpeedMps\": 4000, \"turnStepDegrees\": 1, \"linearInertiaMps2\": -1,")]
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
    public void LoadScenario_rejects_cell_outside_2x2_grid()
    {
        // Container occupies exactly 4 cells (slotSize 4), so the count check passes;
        // cell 4 is outside the 0..3 grid of a 2x2 platform and must be rejected.
        var scenario = ScenarioLoader.LoadFromJson(ScenarioWithModule(occupiedCells: "0, 1, 2, 4"));
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

    [Fact]
    public void Empty_factory_and_recipe_files_load_successfully()
    {
        WithContentDirectory(directory =>
        {
            WriteSettings(directory, factoryTypesPath: "factory-types.json", recipesPath: "recipes.json");
            WriteMinimalContent(directory);
            File.WriteAllText(Path.Combine(directory, "factory-types.json"), """{ "factoryTypes": [] }""");
            File.WriteAllText(Path.Combine(directory, "recipes.json"), """{ "recipes": [] }""");

            string settingsPath = Path.Combine(directory, "Settings.json");
            Assert.NotNull(EngineContentLoader.CreateEngineFromSettingsFile(settingsPath));

            var registry = EngineContentLoader.LoadRegistryFromSettingsFile(settingsPath, out _, out _);
            Assert.Equal(0, registry.FactoryTypes.Count);
            Assert.Equal(0, registry.Recipes.Count);
        });
    }

    [Fact]
    public void Missing_declared_factory_types_file_throws()
    {
        WithContentDirectory(directory =>
        {
            WriteSettings(directory, factoryTypesPath: "missing-factory-types.json");
            WriteMinimalContent(directory);

            var exception = Assert.Throws<ContentException>(() =>
                EngineContentLoader.CreateEngineFromSettingsFile(Path.Combine(directory, "Settings.json")));
            Assert.Contains("factory types", exception.Message, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Missing_declared_recipes_file_throws()
    {
        WithContentDirectory(directory =>
        {
            WriteSettings(directory, recipesPath: "missing-recipes.json");
            WriteMinimalContent(directory);

            var exception = Assert.Throws<ContentException>(() =>
                EngineContentLoader.CreateEngineFromSettingsFile(Path.Combine(directory, "Settings.json")));
            Assert.Contains("recipes", exception.Message, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Extra_field_in_recipes_file_throws()
    {
        WithContentDirectory(directory =>
        {
            WriteSettings(directory, recipesPath: "recipes.json");
            WriteMinimalContent(directory);
            File.WriteAllText(Path.Combine(directory, "recipes.json"), """{ "recipes": [], "unexpected": 1 }""");

            Assert.Throws<ContentException>(() =>
                EngineContentLoader.CreateEngineFromSettingsFile(Path.Combine(directory, "Settings.json")));
        });
    }

    [Fact]
    public void Extra_field_inside_recipe_entry_throws()
    {
        WithContentDirectory(directory =>
        {
            WriteSettings(directory, recipesPath: "recipes.json");
            WriteMinimalContent(directory);
            File.WriteAllText(Path.Combine(directory, "recipes.json"), """
            {
              "recipes": [
                { "typeId": "r1", "displayName": "R", "inputs": [], "outputs": [], "cycleDurationMs": 100, "bogus": 0 }
              ]
            }
            """);

            Assert.Throws<ContentException>(() =>
                EngineContentLoader.CreateEngineFromSettingsFile(Path.Combine(directory, "Settings.json")));
        });
    }

    [Fact]
    public void Factory_and_recipe_content_lands_in_registry()
    {
        WithContentDirectory(directory =>
        {
            WriteSettings(directory, factoryTypesPath: "factory-types.json", recipesPath: "recipes.json");
            WriteMinimalContent(directory);
            File.WriteAllText(Path.Combine(directory, "factory-types.json"), """
            {
              "factoryTypes": [
                {
                  "typeId": "fac-1",
                  "displayName": "Water Factory",
                  "recipe": {
                    "inputs": [ { "itemTypeId": "item.energy-cells", "count": 5 } ],
                    "outputs": [ { "itemTypeId": "item.water", "count": 95 } ],
                    "cycleTimeMs": 1000
                  }
                }
              ]
            }
            """);
            File.WriteAllText(Path.Combine(directory, "recipes.json"), """
            {
              "recipes": [
                { "typeId": "rec-1", "displayName": "Recipe", "inputs": [], "outputs": [], "cycleDurationMs": 2000 }
              ]
            }
            """);

            var registry = EngineContentLoader.LoadRegistryFromSettingsFile(
                Path.Combine(directory, "Settings.json"), out _, out _);

            Assert.Equal(1, registry.FactoryTypes.Count);
            Assert.Equal(1, registry.Recipes.Count);
            Assert.Equal("item.energy-cells", registry.FactoryTypes.GetDefinition(0).Recipe.Inputs[0].ItemTypeId);
            Assert.Equal("item.water", registry.FactoryTypes.GetDefinition(0).Recipe.Outputs[0].ItemTypeId);
            Assert.Equal(1000, registry.FactoryTypes.GetDefinition(0).Recipe.CycleDurationMs);
            Assert.Equal(2000, registry.Recipes.GetDefinition(0).CycleDurationMs);
        });
    }

    [Fact]
    public void Duplicate_factory_type_id_is_rejected()
    {
        WithContentDirectory(directory =>
        {
            WriteSettings(directory, factoryTypesPath: "factory-types.json");
            WriteMinimalContent(directory);
            File.WriteAllText(Path.Combine(directory, "factory-types.json"), """
            {
              "factoryTypes": [
                { "typeId": "fac-1", "displayName": "A", "recipe": { "inputs": [], "outputs": [], "cycleTimeMs": 100 } },
                { "typeId": "fac-1", "displayName": "B", "recipe": { "inputs": [], "outputs": [], "cycleTimeMs": 200 } }
              ]
            }
            """);

            var exception = Assert.Throws<ContentException>(() =>
                EngineContentLoader.CreateEngineFromSettingsFile(Path.Combine(directory, "Settings.json")));
            Assert.Contains("duplicate typeId 'fac-1'", exception.Message, StringComparison.Ordinal);
        });
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
                    TurnStepDegrees: null,
                    LinearInertiaMps2: null)
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
        string occupiedCells = "0, 1, 2, 3",
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

    private static void WithContentDirectory(Action<string> action)
    {
        string directory = Path.Combine(Path.GetTempPath(), $"dss-content-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            action(directory);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static void WriteSettings(string directory, string? factoryTypesPath = null, string? recipesPath = null)
    {
        string factoryTypesEntry = factoryTypesPath is null ? "" : ", \"factoryTypes\": \"" + factoryTypesPath + "\"";
        string recipesEntry = recipesPath is null ? "" : ", \"recipes\": \"" + recipesPath + "\"";
        File.WriteAllText(Path.Combine(directory, "Settings.json"), $$"""
        { "typeData": { "moduleTypes": "module-types.json", "itemTypes": "item-types.json", "commandDefinitions": "command-definitions.json"{{factoryTypesEntry}}{{recipesEntry}} }, "defaultScenario": "scenario.json" }
        """);
    }

    private static void WriteMinimalContent(string directory)
    {
        File.WriteAllText(Path.Combine(directory, "command-definitions.json"), """{ "commandDefinitions": [] }""");
        File.WriteAllText(Path.Combine(directory, "module-types.json"), """{ "moduleTypes": [] }""");
        File.WriteAllText(Path.Combine(directory, "item-types.json"), """{ "itemTypes": [] }""");
        File.WriteAllText(Path.Combine(directory, "scenario.json"), DefaultScenarioJson);
    }
}
