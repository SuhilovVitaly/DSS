using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// Tests for <see cref="SimulationEngine"/> installed module snapshot projection
/// (ТЗ CommandPanelSlim, п. 4.3).
/// </summary>
public class InstalledModuleProjectionTests
{
    private const string PlayerShipId = "SPC-0001";

    [Fact]
    public void Default_scenario_projects_all_modules_with_correct_positions_and_command_type_ids()
    {
        var engine = CreateEngineWithTwoModules();
        var snapshot = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);

        Assert.False(snapshot.InstalledModules.IsDefault);
        Assert.Equal(2, snapshot.InstalledModules.Length);

        // Position = index in the module list (0 = engine, 1 = scanner).
        var engineMod = snapshot.InstalledModules[0];
        Assert.Equal("MOD-ENG-01", engineMod.ModuleId);
        Assert.Equal("module.engine.basic", engineMod.ModuleTypeId);
        Assert.Equal("Engine", engineMod.DisplayName);
        Assert.Equal(0, engineMod.Position);
        Assert.NotEmpty(engineMod.CommandTypeIds);
        Assert.Contains("engine.accelerate", engineMod.CommandTypeIds);
        Assert.Equal("On", engineMod.PowerState);
        Assert.Equal("Ready", engineMod.OperationalState);
        Assert.Equal(100, engineMod.StructurePoints);
        Assert.Null(engineMod.ActiveCommandType);
        Assert.Equal(1000, engineMod.FuelAmountKg);

        var scannerMod = snapshot.InstalledModules[1];
        Assert.Equal("MOD-SCN-01", scannerMod.ModuleId);
        Assert.Equal("module.scanner.mk1", scannerMod.ModuleTypeId);
        Assert.Equal("Scanner MK I", scannerMod.DisplayName);
        Assert.Equal(1, scannerMod.Position);
        Assert.Empty(scannerMod.CommandTypeIds);
        Assert.Equal("On", scannerMod.PowerState);
        Assert.Equal("Ready", scannerMod.OperationalState);
        Assert.Equal(50, scannerMod.StructurePoints);
        Assert.Null(scannerMod.ActiveCommandType);
        Assert.Null(scannerMod.FuelAmountKg);
    }

    [Fact]
    public void Projection_commands_carry_display_names_and_targets()
    {
        var engine = CreateEngineWithTwoModules();
        var snapshot = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);

        var engineMod = snapshot.InstalledModules[0];
        Assert.False(engineMod.Commands.IsDefault);
        Assert.Equal(engineMod.CommandTypeIds.Length, engineMod.Commands.Length);

        Assert.Equal("engine.accelerate", engineMod.Commands[0].CommandTypeId);
        Assert.Equal("Accelerate", engineMod.Commands[0].DisplayName);
        Assert.Equal("none", engineMod.Commands[0].Target);

        Assert.Equal("engine.navigate-to-point", engineMod.Commands[1].CommandTypeId);
        Assert.Equal("Navigate", engineMod.Commands[1].DisplayName);
        Assert.Equal("point", engineMod.Commands[1].Target);
    }

    [Fact]
    public void Projection_commands_for_scanner_and_match_carry_object_target()
    {
        var engine = CreateEngineWithScannerCommands();
        var snapshot = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);

        var scannerMod = Assert.Single(snapshot.InstalledModules);
        Assert.Equal(3, scannerMod.Commands.Length);

        Assert.Equal("scanner.general-scan", scannerMod.Commands[0].CommandTypeId);
        Assert.Equal("General Scan", scannerMod.Commands[0].DisplayName);
        Assert.Equal("object", scannerMod.Commands[0].Target);

        Assert.Equal("scanner.structural-scan", scannerMod.Commands[1].CommandTypeId);
        Assert.Equal("Structural Scan", scannerMod.Commands[1].DisplayName);
        Assert.Equal("object", scannerMod.Commands[1].Target);

        Assert.Equal("engine.match-target-speed", scannerMod.Commands[2].CommandTypeId);
        Assert.Equal("Match Target Speed", scannerMod.Commands[2].DisplayName);
        Assert.Equal("object", scannerMod.Commands[2].Target);
    }

    [Fact]
    public void Ship_without_modules_returns_empty()
    {
        var engine = new SimulationEngine(GameDataRegistry.Empty);
        engine.LoadScenario(Scenario.ScenarioLoader.LoadFromJson($$"""
        {
          "scenarioMetadata": { "scenarioId": "test", "name": "Test" },
          "gameState": {
            "gameTimeMs": 0,
            "currentSpeed": "Speed0",
            "playerShipObjectId": "{{PlayerShipId}}",
            "spaceObjects": [
              { "objectId": "{{PlayerShipId}}", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary" }
            ]
          }
        }
        """));

        var snapshot = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0);
        Assert.True(snapshot.InstalledModules.IsDefault || snapshot.InstalledModules.IsEmpty);
    }

    [Fact]
    public void AddTestObject_with_empty_registry_returns_empty()
    {
        var engine = new SimulationEngine(GameDataRegistry.Empty);
        engine.AddTestObject(new DeepSpaceSaga.Contracts.ObjectMotionSnapshot(
            "OBJ", 0, 0, SpeedKmS: 0, Direction: 0));

        // AddTestObject doesn't set PlayerShipObjectId, and there's no registry.
        var snapshot = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0);
        Assert.True(snapshot.InstalledModules.IsDefault || snapshot.InstalledModules.IsEmpty);
    }

    private static SimulationEngine CreateEngineWithTwoModules()
    {
        var registry = GameDataRegistry.Create(
            moduleTypes:
            [
                new ModuleTypeDefinition(
                    "module.engine.basic", "Engine", SlotSize: 1, MassKg: 5000,
                    StructurePointsMax: 100, PowerConsumptionW: 0,
                    CommandTypeIds: ImmutableArray.Create("engine.accelerate", "engine.navigate-to-point"),
                    MaxSpeedMps: 4000, TurnStepDegrees: 1,
                    LinearInertiaMps2: 40000, AngularInertiaDegPerSec: 4,
                    FuelCapacityKg: 2000),
                new ModuleTypeDefinition(
                    "module.scanner.mk1", "Scanner MK I", SlotSize: 1, MassKg: 1000,
                    StructurePointsMax: 50, PowerConsumptionW: 100,
                    CommandTypeIds: ImmutableArray<string>.Empty)
            ],
            itemTypes: [],
            commandDefinitions:
            [
                new CommandDefinition("engine.accelerate", "Accelerate", Target: "none"),
                new CommandDefinition("engine.navigate-to-point", "Navigate", Target: "point")
            ]);

        var engine = new SimulationEngine(registry);
        engine.LoadScenario(Scenario.ScenarioLoader.LoadFromJson($$"""
        {
          "scenarioMetadata": { "scenarioId": "test", "name": "Test" },
          "gameState": {
            "gameTimeMs": 0,
            "currentSpeed": "Speed1",
            "playerShipObjectId": "{{PlayerShipId}}",
            "spaceObjects": [
              { "objectId": "{{PlayerShipId}}", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "name": "Player Ship",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary",
                "modules": [
                  { "moduleId": "MOD-ENG-01", "moduleTypeId": "module.engine.basic",
                    "platformIndex": 0, "occupiedCells": [0],
                    "powerState": "On", "operationalState": "Ready", "structurePoints": 100,
                    "fuelAmountKg": 1000 },
                  { "moduleId": "MOD-SCN-01", "moduleTypeId": "module.scanner.mk1",
                    "platformIndex": 0, "occupiedCells": [1],
                    "powerState": "On", "operationalState": "Ready", "structurePoints": 50 }
                ]
              }
            ]
          }
        }
        """));

        return engine;
    }

    private static SimulationEngine CreateEngineWithScannerCommands()
    {
        var registry = GameDataRegistry.Create(
            moduleTypes:
            [
                new ModuleTypeDefinition(
                    "module.scanner.mk1", "Scanner MK I", SlotSize: 1, MassKg: 1000,
                    StructurePointsMax: 50, PowerConsumptionW: 100,
                    CommandTypeIds: ImmutableArray.Create(
                        "scanner.general-scan", "scanner.structural-scan", "engine.match-target-speed"))
            ],
            itemTypes: [],
            commandDefinitions:
            [
                new CommandDefinition("scanner.general-scan", "General Scan", Target: "object"),
                new CommandDefinition("scanner.structural-scan", "Structural Scan", Target: "object"),
                new CommandDefinition("engine.match-target-speed", "Match Target Speed", Target: "object")
            ]);

        var engine = new SimulationEngine(registry);
        engine.LoadScenario(Scenario.ScenarioLoader.LoadFromJson($$"""
        {
          "scenarioMetadata": { "scenarioId": "test", "name": "Test" },
          "gameState": {
            "gameTimeMs": 0,
            "currentSpeed": "Speed1",
            "playerShipObjectId": "{{PlayerShipId}}",
            "spaceObjects": [
              { "objectId": "{{PlayerShipId}}", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "name": "Player Ship",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary",
                "modules": [
                  { "moduleId": "MOD-SCN-01", "moduleTypeId": "module.scanner.mk1",
                    "platformIndex": 0, "occupiedCells": [0],
                    "powerState": "On", "operationalState": "Ready", "structurePoints": 50 }
                ]
              }
            ]
          }
        }
        """));

        return engine;
    }
}
