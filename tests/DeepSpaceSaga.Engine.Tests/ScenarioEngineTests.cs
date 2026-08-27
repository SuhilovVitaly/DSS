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

        // Tetrarch-class hull (requirements §57): 9x9 grid, 10 structural cells.
        Assert.NotNull(playerShip.HullLayout);
        Assert.Equal(9, playerShip.HullLayout!.Width);
        Assert.Equal(9, playerShip.HullLayout.Height);
        Assert.Equal(10, playerShip.HullLayout.Cells.Count);

        Assert.Equal(6, playerShip.Modules?.Count);
        var cargoModule = Assert.Single(playerShip.Modules ?? [], m => m.ModuleId == "MOD-PLAYER-CARGO-01");
        Assert.Equal("module.container.basic", cargoModule.ModuleTypeId);
        Assert.Equal([new HullCellCoordinate(4, 2)], cargoModule.OccupiedCells);
        var engineModule = Assert.Single(playerShip.Modules ?? [], m => m.ModuleId == "MOD-PLAYER-ENGINE-01");
        Assert.Equal("module.engine.basic", engineModule.ModuleTypeId);
        Assert.Equal([new HullCellCoordinate(4, 5)], engineModule.OccupiedCells);
        Assert.Equal("On", engineModule.PowerState);
        Assert.Equal("Ready", engineModule.OperationalState);

        var bridgeModule = Assert.Single(playerShip.Modules ?? [], m => m.ModuleId == "MOD-PLAYER-BRIDGE-01");
        Assert.Equal("module.bridge.navigation.computer.basic", bridgeModule.ModuleTypeId);
        Assert.Equal([new HullCellCoordinate(4, 0)], bridgeModule.OccupiedCells);
        var livingQuartersModule = Assert.Single(playerShip.Modules ?? [], m => m.ModuleId == "MOD-PLAYER-LIVING-QUARTERS-01");
        Assert.Equal("living.quarters.mk1", livingQuartersModule.ModuleTypeId);
        Assert.Equal([new HullCellCoordinate(4, 1)], livingQuartersModule.OccupiedCells);
        var generatorModule = Assert.Single(playerShip.Modules ?? [], m => m.ModuleId == "MOD-PLAYER-GENERATOR-01");
        Assert.Equal("module.generator.basic", generatorModule.ModuleTypeId);
        Assert.Equal([new HullCellCoordinate(4, 4)], generatorModule.OccupiedCells);
        var scannerModule = Assert.Single(playerShip.Modules ?? [], m => m.ModuleId == "MOD-PLAYER-SCANNER-01");
        Assert.Equal("module.scanner.mk1", scannerModule.ModuleTypeId);
        Assert.Equal([new HullCellCoordinate(4, 3)], scannerModule.OccupiedCells);

        // Battery/Drilling Unit/Habitation/Combat Laser were removed from the loadout (requirements §57/§50).
        Assert.DoesNotContain(playerShip.Modules ?? [], m => m.ModuleId == "MOD-PLAYER-BATTERY-01");
        Assert.DoesNotContain(playerShip.Modules ?? [], m => m.ModuleId == "MOD-PLAYER-DRILLING-01");
        Assert.DoesNotContain(playerShip.Modules ?? [], m => m.ModuleId == "MOD-PLAYER-HABITATION-01");
        Assert.DoesNotContain(playerShip.Modules ?? [], m => m.ModuleId == "MOD-PLAYER-COMBAT-LASER-01");

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
        Assert.Equal(6, playerShip.Modules.Length);
        var cargoModule = Assert.Single(playerShip.Modules, m => m.ModuleId == "MOD-PLAYER-CARGO-01");
        var engineModule = Assert.Single(playerShip.Modules, m => m.ModuleId == "MOD-PLAYER-ENGINE-01");
        // Module type registry order follows the deterministic (ordinal) sort of
        // Data/Modules/**/*.json file paths, not the historical flat module-types.json order.
        Assert.Equal(2, cargoModule.ModuleTypeIndex);
        Assert.Equal(4, engineModule.ModuleTypeIndex);
        Assert.Null(engineModule.ActiveCycle);
        Assert.Single(playerShip.Modules, m => m.ModuleId == "MOD-PLAYER-BRIDGE-01");
        Assert.Single(playerShip.Modules, m => m.ModuleId == "MOD-PLAYER-LIVING-QUARTERS-01");
        Assert.Single(playerShip.Modules, m => m.ModuleId == "MOD-PLAYER-GENERATOR-01");
        Assert.Single(playerShip.Modules, m => m.ModuleId == "MOD-PLAYER-SCANNER-01");
        var cargo = Assert.Single(cargoModule.Cargo);
        // item.energy-cells is index 2 in the real 10-entry catalog. Item-catalog registry order
        // follows the deterministic (ordinal) sort of Data/Items/<Category>/*.json file paths
        // (Good < Resource alphabetically), not the historical flat item-types.json order — so
        // Good entries (Water/Steel/Energy Cells/Fuel/Protein mass/Food Rations) sort first,
        // then Resource (Ice/Iron Ore/Silicon/Magnesium Ore).
        Assert.Equal(2, cargo.ItemTypeIndex);
    }

    [Fact]
    public void Real_default_scenario_occupies_6_of_10_hull_cells()
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
        Assert.Equal(6, playerShip.Modules.Length);
        Assert.NotNull(playerShip.HullLayout);
        Assert.Equal(10, playerShip.HullLayout!.Cells.Count);

        // Each module occupies exactly 1 hull cell (requirements §57: all real module
        // types are slotSize 1), and no two modules share a cell.
        var occupiedCells = playerShip.Modules.SelectMany(m => m.OccupiedCells).ToArray();
        Assert.Equal(6, occupiedCells.Length);
        Assert.Equal(6, occupiedCells.Distinct().Count());

        // Every occupied cell must belong to the object's hull layout.
        var hullCells = playerShip.HullLayout.Cells.Select(c => (c.X, c.Y)).ToHashSet();
        Assert.All(occupiedCells, cell => Assert.Contains(cell, hullCells));

        // 4 of the 10 structural cells (the Y1/Y5 side wings) remain unoccupied.
        int freeCells = hullCells.Count - occupiedCells.Distinct().Count();
        Assert.Equal(4, freeCells);
    }

    [Fact]
    public void CreateFromScenarioFile_loads_an_explicitly_chosen_scenario_instead_of_the_settings_default()
    {
        // The New Game -> scenario picker path: SimulationEngine.CreateFromScenarioFile
        // must read the given scenario file, not settings.json's defaultScenario, while
        // still using the settings file's type registry (module/item/command definitions).
        string settingsPath = ResolveRealSettingsPath();
        string dockedScenarioPath = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(settingsPath)!, "Scenarios", "Docked", "scenario.json"));

        var engine = SimulationEngine.CreateFromScenarioFile(settingsPath, dockedScenarioPath);

        var playerShip = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == "SPC-0001");
        var station = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == "SPC-0002");

        // (1, 1) world-unit offset — matches exactly what a real successful navigation.dock
        // command produces (SimulationEngine.TryStartNavigationCommand), not the station's
        // own coordinates: two objects at literally identical coordinates are unselectable
        // apart from each other on the tactical map (FindNearestObjectId's tie-break always
        // picks the lexicographically smaller object id — the ship, "SPC-0001" < "SPC-0002").
        Assert.Equal(station.InitialMotion.X + 1.0, playerShip.InitialMotion.X);
        Assert.Equal(station.InitialMotion.Y + 1.0, playerShip.InitialMotion.Y);
        Assert.Equal(0, playerShip.InitialMotion.SpeedKmS);
        Assert.True(playerShip.IsDocked);
        Assert.Equal("SPC-0002", playerShip.DockedStationObjectId);
    }

    private static string ResolveRealSettingsPath()
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

        return settingsPath;
    }

    /// <summary>
    /// Loads from DeepSpaceSaga.Client's own *build output* directory (bin/Debug or
    /// bin/Release), not the source tree ResolveRealSettingsPath uses. Every other
    /// "Real_..." test in this file reads Settings.json straight from source, so a
    /// content file that exists in source but isn't wired into a
    /// "&lt;None Update="Data\X\**\*.json"&gt;&lt;CopyToOutputDirectory&gt;" rule in
    /// DeepSpaceSaga.Client.csproj still passes every one of them while the actual
    /// shipped client throws ContentException on startup (this happened for real: the
    /// item-catalog's Data/Items split added the files in source but not the csproj
    /// copy rule). This test is the only one in the suite that would have caught it.
    /// </summary>
    private static string ResolveClientBuildOutputSettingsPath()
    {
        string repoRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string clientBinRoot = Path.Combine(repoRoot, "src", "DeepSpaceSaga.Client", "bin");

        foreach (string configuration in new[] { "Debug", "Release" })
        {
            string candidate = Path.Combine(clientBinRoot, configuration, "net8.0", "Settings.json");
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException(
            "DeepSpaceSaga.Client build output not found under " +
            $"'{clientBinRoot}' (Debug or Release, net8.0). Build the Client project first.");
    }

    [Fact]
    public void Real_default_scenario_loads_from_the_client_build_output_directory()
    {
        string settingsPath = ResolveClientBuildOutputSettingsPath();

        var engine = EngineContentLoader.CreateEngineFromSettingsFile(settingsPath);

        var station = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == "SPC-0002");
        Assert.NotNull(station);
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

        Assert.Equal(5, activeTypes.Length); // engine + scanner + bridge-navigation-computer + drilling unit + container

        var engineType = Assert.Single(activeTypes, t => t.TypeId == "module.engine.basic");
        Assert.Equal(13, engineType.CommandTypeIds.Length); // + navigation.approach (story-20260827-083137, Batch 3)
        Assert.Equal(100, engineType.BaseSuccessChancePercent);
        foreach (string commandTypeId in engineType.CommandTypeIds)
        {
            Assert.True(registry.CommandDefinitions.Contains(commandTypeId),
                $"Command '{commandTypeId}' of 'module.engine.basic' is missing from the command definitions registry.");
        }

        var scannerType = Assert.Single(activeTypes, t => t.TypeId == "module.scanner.mk1");
        Assert.Equal(3, scannerType.CommandTypeIds.Length);
        Assert.Equal(100, scannerType.BaseSuccessChancePercent);
        Assert.Contains("scanner.generalScan", scannerType.CommandTypeIds);
        Assert.Contains("scanner.structuralScan", scannerType.CommandTypeIds);
        Assert.Contains("scanner.nearbySignatures", scannerType.CommandTypeIds);
        foreach (string commandTypeId in scannerType.CommandTypeIds)
        {
            Assert.True(registry.CommandDefinitions.Contains(commandTypeId),
                $"Command '{commandTypeId}' of 'module.scanner.mk1' is missing from the command definitions registry.");
        }

        var navigationComputerType = Assert.Single(activeTypes, t => t.TypeId == "module.bridge.navigation.computer.basic");
        Assert.Equal(2, navigationComputerType.CommandTypeIds.Length);
        Assert.Contains("navigation.dock", navigationComputerType.CommandTypeIds);
        Assert.Contains("navigation.stationsList", navigationComputerType.CommandTypeIds);
        foreach (string commandTypeId in navigationComputerType.CommandTypeIds)
        {
            Assert.True(registry.CommandDefinitions.Contains(commandTypeId),
                $"Command '{commandTypeId}' of 'module.bridge.navigation.computer.basic' is missing from the command definitions registry.");
        }


        var drillingType = Assert.Single(activeTypes, t => t.TypeId == "module.drilling.unit.basic");
        Assert.Equal(2, drillingType.CommandTypeIds.Length);
        Assert.Contains("mining.extractIce", drillingType.CommandTypeIds);
        Assert.Contains("mining.stopExtraction", drillingType.CommandTypeIds);
        foreach (string commandTypeId in drillingType.CommandTypeIds)
        {
            Assert.True(registry.CommandDefinitions.Contains(commandTypeId),
                $"Command '{commandTypeId}' of 'module.drilling.unit.basic' is missing from the command definitions registry.");
        }

        var containerType = Assert.Single(activeTypes, t => t.TypeId == "module.container.basic");
        Assert.Equal(2, containerType.CommandTypeIds.Length);
        Assert.Contains("trade.buy", containerType.CommandTypeIds);
        Assert.Contains("trade.sell", containerType.CommandTypeIds);
        foreach (string commandTypeId in containerType.CommandTypeIds)
        {
            Assert.True(registry.CommandDefinitions.Contains(commandTypeId),
                $"Command '{commandTypeId}' of 'module.container.basic' is missing from the command definitions registry.");
        }

        int passiveTypes = Enumerable.Range(0, registry.ModuleTypes.Count)
            .Count(i => registry.ModuleTypes.GetDefinition(i).CommandTypeIds.Length == 0);
        Assert.Equal(5, passiveTypes); // includes living.quarters.mk1 (requirements §57); container moved to active (story-20260822-193700, Batch 3)
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
            File.WriteAllText(Path.Combine(directory, "module-types.json"), """
            {
              "moduleTypes": [
                {
                  "typeId": "module.test.passive",
                  "displayName": "Test",
                  "slotSize": 1,
                  "commandTypeIds": []
                }
              ]
            }
            """);
            File.WriteAllText(Path.Combine(directory, "modules.json"), $$"""
            {
              "moduleImplementations": [
                {
                  "typeId": "module.test.passive",
                  "displayName": "Test",
                  "type": "module.test.passive",
                  "massKg": 1,
                  "structurePointsMax": 1,
                  "powerConsumptionW": 0,
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
            File.WriteAllText(Path.Combine(directory, "module-types.json"), """
            {
              "moduleTypes": [
                {
                  "typeId": "module.test.passive",
                  "displayName": "Test",
                  "slotSize": 1,
                  "commandTypeIds": []
                }
              ]
            }
            """);
            File.WriteAllText(Path.Combine(directory, "modules.json"), $$"""
            {
              "moduleImplementations": [
                {
                  "typeId": "module.test.passive",
                  "displayName": "Test",
                  "type": "module.test.passive",
                  "massKg": 1,
                  "structurePointsMax": 1,
                  "powerConsumptionW": 0,
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
                  "commandTypeIds": []
                }
              ]
            }
            """);
            File.WriteAllText(Path.Combine(directory, "modules.json"), """
            {
              "moduleImplementations": [
                {
                  "typeId": "module.test.passive",
                  "displayName": "Test",
                  "type": "module.test.passive",
                  "massKg": 1,
                  "structurePointsMax": 1,
                  "powerConsumptionW": 0,
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
            { "typeData": { "moduleTypes": "module-types.json", "moduleImplementations": "modules.json", "itemTypes": "item-types.json", "commandDefinitions": "command-definitions.json" }, "defaultScenario": "scenario.json" }
            """);
            File.WriteAllText(Path.Combine(directory, "command-definitions.json"), """{ "commandDefinitions": [] }""");
            File.WriteAllText(Path.Combine(directory, "module-types.json"), """
            {
              "moduleTypes": [
                {
                  "typeId": "module.engine",
                  "displayName": "Engine",
                  "slotSize": 1,
                  "commandTypeIds": []
                }
              ]
            }
            """);
            File.WriteAllText(Path.Combine(directory, "modules.json"), $$"""
            {
              "moduleImplementations": [
                {
                  "typeId": "module.engine.basic",
                  "displayName": "Engine",
                  "type": "module.engine",
                  "massKg": 1,
                  "structurePointsMax": 1,
                  "powerConsumptionW": 0,
                  "cargoCapacityKg": null,
                  {{engineParameters}}
                  "baseCycleTimeMs": 1
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
        // Only 1 cell for a module type whose slotSize is 4.
        var scenario = ScenarioLoader.LoadFromJson(ScenarioWithModule(occupiedCells: [(0, 0)]));
        var engine = CreateEngineWithBasicTypes();

        Assert.Throws<ScenarioException>(() => engine.LoadScenario(scenario));
    }

    [Fact]
    public void LoadScenario_rejects_duplicate_module_cells()
    {
        var scenario = ScenarioLoader.LoadFromJson(
            ScenarioWithModule(occupiedCells: [(0, 0), (0, 0), (1, 0), (0, 1)]));
        var engine = CreateEngineWithBasicTypes();

        Assert.Throws<ScenarioException>(() => engine.LoadScenario(scenario));
    }

    [Fact]
    public void LoadScenario_rejects_modules_overlapping_hull_cells()
    {
        // MOD-1 (default cells) occupies (0,0),(1,0),(0,1),(1,1); MOD-2 overlaps at (1,1).
        const string extraModule = """
        ,
                  {
                    "moduleId": "MOD-2",
                    "moduleTypeId": "module.container.basic",
                    "occupiedCells": [ { "x": 1, "y": 1 }, { "x": 2, "y": 0 }, { "x": 0, "y": 2 }, { "x": 2, "y": 2 } ],
                    "structurePoints": 400,
                    "powerState": "On",
                    "operationalState": "Ready",
                    "cargo": []
                  }
        """;

        var scenario = ScenarioLoader.LoadFromJson(ScenarioWithModule(extraModules: extraModule));
        var engine = CreateEngineWithBasicTypes();

        var ex = Assert.Throws<ScenarioException>(() => engine.LoadScenario(scenario));
        Assert.Contains("overlaps occupied cell", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadScenario_rejects_cell_outside_hull_layout()
    {
        // Container occupies exactly 4 cells (slotSize 4), so the count check passes;
        // (9,9) is outside the default 3x3 hull layout and must be rejected.
        var scenario = ScenarioLoader.LoadFromJson(
            ScenarioWithModule(occupiedCells: [(0, 0), (1, 0), (0, 1), (9, 9)]));
        var engine = CreateEngineWithBasicTypes();

        var ex = Assert.Throws<ScenarioException>(() => engine.LoadScenario(scenario));
        Assert.Contains("outside the object's hull layout", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadScenario_rejects_module_with_no_hull_layout_on_object()
    {
        // requirements §57: an object with modules must declare a hullLayout.
        var scenario = ScenarioLoader.LoadFromJson(ScenarioWithModule(includeHullLayout: false));
        var engine = CreateEngineWithBasicTypes();

        var ex = Assert.Throws<ScenarioException>(() => engine.LoadScenario(scenario));
        Assert.Contains("hullLayout", ex.Message, StringComparison.Ordinal);
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
            occupiedCells: [(0, 0)]));

        Assert.Throws<ScenarioException>(() => engine.LoadScenario(invalidScenario));

        Assert.Equal("OLD-SHIP", engine.PlayerShipObjectId);
        Assert.Equal(SimulationSpeed.Speed2, engine.CurrentSpeed);
        var obj = Assert.Single(engine.RuntimeObjects);
        Assert.Equal("OLD-SHIP", obj.InitialMotion.ObjectId);
        Assert.Single(obj.Modules);
    }

    // --- requirements §57 domain invariants (SimulationEngine.ValidateModulePlacement) ---
    // Minimal synthetic fixtures, independent of ScenarioWithModule, per plan Batch D item 1.

    [Fact]
    public void Module_placed_outside_object_hull_layout_throws_ScenarioException()
    {
        var registry = CreateSingleCellModuleTypeRegistry();
        var engine = new SimulationEngine(registry);

        const string json = """
        {
          "scenarioMetadata": { "scenarioId": "x", "name": "x" },
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed1",
            "playerShipObjectId": "SHIP",
            "spaceObjects": [
              {
                "objectId": "SHIP", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary",
                "hullLayout": { "width": 1, "height": 1, "cells": [ { "x": 0, "y": 0 } ] },
                "modules": [
                  {
                    "moduleId": "MOD-1",
                    "moduleTypeId": "module.test.cell",
                    "occupiedCells": [ { "x": 1, "y": 0 } ],
                    "structurePoints": 10,
                    "powerState": "On",
                    "operationalState": "Ready",
                    "cargo": []
                  }
                ]
              }
            ]
          }
        }
        """;

        var scenario = ScenarioLoader.LoadFromJson(json);
        var ex = Assert.Throws<ScenarioException>(() => engine.LoadScenario(scenario));
        Assert.Contains("outside the object's hull layout", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Two_modules_sharing_a_hull_cell_throw_ScenarioException()
    {
        var registry = CreateSingleCellModuleTypeRegistry();
        var engine = new SimulationEngine(registry);

        const string json = """
        {
          "scenarioMetadata": { "scenarioId": "x", "name": "x" },
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed1",
            "playerShipObjectId": "SHIP",
            "spaceObjects": [
              {
                "objectId": "SHIP", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary",
                "hullLayout": { "width": 2, "height": 1, "cells": [ { "x": 0, "y": 0 }, { "x": 1, "y": 0 } ] },
                "modules": [
                  {
                    "moduleId": "MOD-1",
                    "moduleTypeId": "module.test.cell",
                    "occupiedCells": [ { "x": 0, "y": 0 } ],
                    "structurePoints": 10,
                    "powerState": "On",
                    "operationalState": "Ready",
                    "cargo": []
                  },
                  {
                    "moduleId": "MOD-2",
                    "moduleTypeId": "module.test.cell",
                    "occupiedCells": [ { "x": 0, "y": 0 } ],
                    "structurePoints": 10,
                    "powerState": "On",
                    "operationalState": "Ready",
                    "cargo": []
                  }
                ]
              }
            ]
          }
        }
        """;

        var scenario = ScenarioLoader.LoadFromJson(json);
        var ex = Assert.Throws<ScenarioException>(() => engine.LoadScenario(scenario));
        Assert.Contains("overlaps occupied cell", ex.Message, StringComparison.Ordinal);
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
                new ModuleCategoryDefinition(
                    "module.container.basic",
                    "Container",
                    SlotSize: 4,
                    CommandTypeIds: ImmutableArray<string>.Empty)
            ],
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

    /// <summary>Minimal single-cell passive module type, used by the two hull-invariant Facts.</summary>
    private static GameDataRegistry CreateSingleCellModuleTypeRegistry()
    {
        return GameDataRegistry.Create(
            [
                new ModuleCategoryDefinition(
                    "module.test.cell",
                    "Test Cell Module",
                    SlotSize: 1,
                    CommandTypeIds: ImmutableArray<string>.Empty)
            ],
            [
                new ModuleTypeDefinition(
                    "module.test.cell",
                    "Test Cell Module",
                    SlotSize: 1,
                    MassKg: 1000,
                    StructurePointsMax: 10,
                    PowerConsumptionW: 0,
                    CommandTypeIds: ImmutableArray<string>.Empty)
            ],
            [],
            []);
    }

    // Default hull layout for ScenarioWithModule: a 3x3 block, (0,0)..(2,2).
    private static readonly (int X, int Y)[] DefaultHullCells =
    [
        (0, 0), (1, 0), (2, 0),
        (0, 1), (1, 1), (2, 1),
        (0, 2), (1, 2), (2, 2)
    ];

    // Default module cells: a 2x2 corner of the default hull, matching container.basic's SlotSize 4.
    private static readonly (int X, int Y)[] DefaultModuleCells = [(0, 0), (1, 0), (0, 1), (1, 1)];

    private static string CellsJson(IEnumerable<(int X, int Y)> cells) =>
        string.Join(", ", cells.Select(c => $$"""{ "x": {{c.X}}, "y": {{c.Y}} }"""));

    private static string ScenarioWithModule(
        string objectId = "SHIP",
        string currentSpeed = "Speed1",
        string moduleTypeId = "module.container.basic",
        (int X, int Y)[]? occupiedCells = null,
        bool includeHullLayout = true,
        string extraModules = "")
    {
        string hullLayoutJson = includeHullLayout
            ? "\"hullLayout\": { \"width\": 3, \"height\": 3, \"cells\": [ " + CellsJson(DefaultHullCells) + " ] },"
            : "";

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
                {{hullLayoutJson}}
                "modules": [
                  {
                    "moduleId": "MOD-1",
                    "moduleTypeId": "{{moduleTypeId}}",
                    "occupiedCells": [ {{CellsJson(occupiedCells ?? DefaultModuleCells)}} ],
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
        { "typeData": { "moduleTypes": "module-types.json", "moduleImplementations": "modules.json", "itemTypes": "item-types.json", "commandDefinitions": "command-definitions.json"{{factoryTypesEntry}}{{recipesEntry}} }, "defaultScenario": "scenario.json" }
        """);
    }

    private static void WriteMinimalContent(string directory)
    {
        File.WriteAllText(Path.Combine(directory, "command-definitions.json"), """{ "commandDefinitions": [] }""");
        File.WriteAllText(Path.Combine(directory, "module-types.json"), """{ "moduleTypes": [] }""");
        File.WriteAllText(Path.Combine(directory, "modules.json"), """{ "moduleImplementations": [] }""");
        File.WriteAllText(Path.Combine(directory, "item-types.json"), """{ "itemTypes": [] }""");
        File.WriteAllText(Path.Combine(directory, "scenario.json"), DefaultScenarioJson);
    }
}
