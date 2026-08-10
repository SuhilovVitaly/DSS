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

        var scannerMod = snapshot.InstalledModules[1];
        Assert.Equal("MOD-SCN-01", scannerMod.ModuleId);
        Assert.Equal("module.scanner.mk1", scannerMod.ModuleTypeId);
        Assert.Equal("Scanner MK I", scannerMod.DisplayName);
        Assert.Equal(1, scannerMod.Position);
        Assert.Empty(scannerMod.CommandTypeIds);
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
                    LinearInertiaMps2: 40000, AngularInertiaDegPerSec: 4),
                new ModuleTypeDefinition(
                    "module.scanner.mk1", "Scanner MK I", SlotSize: 1, MassKg: 1000,
                    StructurePointsMax: 50, PowerConsumptionW: 100,
                    CommandTypeIds: ImmutableArray<string>.Empty)
            ],
            itemTypes: [],
            commandDefinitions:
            [
                new CommandDefinition("engine.accelerate", "Accelerate"),
                new CommandDefinition("engine.navigate-to-point", "Navigate")
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
}
