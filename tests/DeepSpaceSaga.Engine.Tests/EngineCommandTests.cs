using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Tests;

public class EngineCommandTests
{
    private const string PlayerShipId = "SHIP";
    private const string EngineModuleId = "ENGINE-1";

    [Fact]
    public void Accelerate_completes_on_the_next_authoritative_tick()
    {
        var engine = CreateEngine();

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.Accelerate));
        Assert.Equal(0, PlayerShipFrom(engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1)).SpeedKmS);
        var ship = PlayerShipFrom(engine.CaptureSnapshotForTests(100, SimulationSpeed.Speed1));

        Assert.Equal(4.0, ship.SpeedKmS);
    }

    [Fact]
    public void Brake_completes_on_the_next_authoritative_tick()
    {
        var engine = CreateEngine(speedMps: 700);

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.Brake));
        Assert.Equal(0.7, PlayerShipFrom(engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1)).SpeedKmS);
        var ship = PlayerShipFrom(engine.CaptureSnapshotForTests(100, SimulationSpeed.Speed1));

        Assert.Equal(0, ship.SpeedKmS);
    }

    [Theory]
    [InlineData(ShipEngineCommandTypes.TurnLeftStep, 0, 359)]
    [InlineData(ShipEngineCommandTypes.TurnRightStep, 359, 0)]
    public void Step_turns_change_direction_with_normalization(
        string commandType,
        int initialDirection,
        int expectedDirection)
    {
        var engine = CreateEngine(directionDegrees: initialDirection);

        engine.ReceiveCommand(Command(commandType));
        Assert.Equal(initialDirection, PlayerShipFrom(engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1)).Direction);
        var ship = PlayerShipFrom(engine.CaptureSnapshotForTests(100, SimulationSpeed.Speed1));

        Assert.Equal(expectedDirection, ship.Direction);
    }

    [Fact]
    public void Until_cancel_turn_repeats_until_cancel_all()
    {
        var engine = CreateEngine(directionDegrees: 0);

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnRightUntilCancel));
        Assert.Equal(0, PlayerShipFrom(engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1)).Direction);
        Assert.Equal(0, PlayerShipFrom(engine.CaptureSnapshotForTests(999, SimulationSpeed.Speed1)).Direction);
        Assert.Equal(1, PlayerShipFrom(engine.CaptureSnapshotForTests(1100, SimulationSpeed.Speed1)).Direction);
        Assert.Equal(2, PlayerShipFrom(engine.CaptureSnapshotForTests(2000, SimulationSpeed.Speed1)).Direction);

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.CancelAll));
        Assert.Equal(2, PlayerShipFrom(engine.CaptureSnapshotForTests(2000, SimulationSpeed.Speed1)).Direction);
        Assert.Equal(2, PlayerShipFrom(engine.CaptureSnapshotForTests(3000, SimulationSpeed.Speed1)).Direction);
        Assert.Equal(2, PlayerShipFrom(engine.CaptureSnapshotForTests(4000, SimulationSpeed.Speed1)).Direction);
    }

    [Fact]
    public void Opposite_until_cancel_replaces_current_repeating_turn()
    {
        var engine = CreateEngine(directionDegrees: 0);

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnLeftUntilCancel));
        Assert.Equal(0, PlayerShipFrom(engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1)).Direction);
        Assert.Equal(359, PlayerShipFrom(engine.CaptureSnapshotForTests(1000, SimulationSpeed.Speed1)).Direction);

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnRightUntilCancel));
        Assert.Equal(358, PlayerShipFrom(engine.CaptureSnapshotForTests(2000, SimulationSpeed.Speed1)).Direction);
        Assert.Equal(359, PlayerShipFrom(engine.CaptureSnapshotForTests(3000, SimulationSpeed.Speed1)).Direction);
    }

    [Fact]
    public void Until_cancel_turn_does_not_repeat_when_game_time_is_paused()
    {
        var engine = CreateEngine(directionDegrees: 0);

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnRightUntilCancel));
        Assert.Equal(0, PlayerShipFrom(engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0)).Direction);

        Assert.Equal(0, PlayerShipFrom(engine.CaptureSnapshotForTests(1000, SimulationSpeed.Speed0)).Direction);
        Assert.Equal(1, PlayerShipFrom(engine.CaptureSnapshotForTests(1000, SimulationSpeed.Speed1)).Direction);
    }

    [Fact]
    public void Until_cancel_turn_applies_one_degree_per_elapsed_game_second()
    {
        var engine = CreateEngine(directionDegrees: 0);

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnRightUntilCancel));
        Assert.Equal(0, PlayerShipFrom(engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1)).Direction);

        Assert.Equal(5, PlayerShipFrom(engine.CaptureSnapshotForTests(5000, SimulationSpeed.Speed2)).Direction);
        Assert.Equal(5, PlayerShipFrom(engine.CaptureSnapshotForTests(5500, SimulationSpeed.Speed2)).Direction);
        Assert.Equal(6, PlayerShipFrom(engine.CaptureSnapshotForTests(6000, SimulationSpeed.Speed2)).Direction);
    }

    [Fact]
    public void Until_cancel_turn_publishes_discrete_cycle_timing_for_client_prediction()
    {
        var engine = CreateEngine(directionDegrees: 12);

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnRightUntilCancel));
        var ship = PlayerShipFrom(engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1));

        Assert.Equal(12, ship.Direction);
        Assert.Equal(ShipEngineCommandTypes.TurnRightUntilCancel, ship.ActiveEngineCommandType);
        Assert.Equal(1, ship.TurnStepDegrees);
        Assert.Equal(1_000, ship.TurnStepRemainingMs);
        Assert.Equal(1_000, ship.TurnStepIntervalMs);
    }

    [Fact]
    public void Repeated_same_until_cancel_command_preserves_cycle_progress()
    {
        var engine = CreateEngine(directionDegrees: 0);

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnRightUntilCancel));
        engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);
        engine.CaptureSnapshotForTests(500, SimulationSpeed.Speed1);

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnRightUntilCancel));
        Assert.Equal(1, PlayerShipFrom(engine.CaptureSnapshotForTests(1000, SimulationSpeed.Speed1)).Direction);
        Assert.Equal(2, PlayerShipFrom(engine.CaptureSnapshotForTests(2000, SimulationSpeed.Speed1)).Direction);
    }

    [Fact]
    public void Client_prediction_matches_authoritative_discrete_repeat_turn_trajectory()
    {
        var engine = CreateEngine(speedMps: 700, directionDegrees: 0);
        var predictor = new LinearMotionPredictor();

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnRightUntilCancel));
        var initial = PlayerShipFrom(engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1));
        var predicted = predictor.Predict(initial, 5_000);
        var authoritative = PlayerShipFrom(engine.CaptureSnapshotForTests(5_000, SimulationSpeed.Speed2));

        Assert.Equal(authoritative.X, predicted.X, precision: 8);
        Assert.Equal(authoritative.Y, predicted.Y, precision: 8);
        Assert.Equal(authoritative.Direction, predicted.Direction, precision: 8);
    }

    [Fact]
    public void Loaded_engine_cycle_id_is_not_reused()
    {
        var engine = CreateEngine(activeCycleJson: """
        { "cycleId": "CYC-ENGINE-000001", "startedGameTimeMs": 0, "durationMs": 0,
          "commandType": "engine.accelerate", "isAutoRepeat": false }
        """);

        engine.CaptureSnapshotForTests(100, SimulationSpeed.Speed1);
        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnRightStep));
        engine.CaptureSnapshotForTests(100, SimulationSpeed.Speed1);

        var module = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == PlayerShipId).Modules.Single();
        Assert.Equal("CYC-ENGINE-000002", module.ActiveCycle?.CycleId);
    }

    [Fact]
    public void New_engine_command_replaces_active_cyclic_command()
    {
        var engine = CreateEngine(directionDegrees: 12);

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.Accelerate));
        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnRightStep));

        var initial = PlayerShipFrom(engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1));
        Assert.Equal(0, initial.SpeedKmS);
        Assert.Equal(12, initial.Direction);

        var completed = PlayerShipFrom(engine.CaptureSnapshotForTests(100, SimulationSpeed.Speed1));
        Assert.Equal(0, completed.SpeedKmS);
        Assert.Equal(13, completed.Direction);
    }

    [Fact]
    public void Repeated_same_cyclic_command_is_a_server_no_op()
    {
        var engine = CreateEngine();

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.Accelerate));
        engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);
        var playerShip = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == PlayerShipId);
        string? cycleId = playerShip.Modules.Single().ActiveCycle?.CycleId;

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.Accelerate));
        engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);

        playerShip = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == PlayerShipId);
        Assert.Equal(cycleId, playerShip.Modules.Single().ActiveCycle?.CycleId);
    }

    [Fact]
    public void Rapid_one_shot_commands_are_deferred_until_the_module_is_free()
    {
        var engine = CreateEngine(directionDegrees: 0);

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnRightStep));
        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnRightStep));

        Assert.Equal(0, PlayerShipFrom(engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1)).Direction);
        Assert.Equal(1, PlayerShipFrom(engine.CaptureSnapshotForTests(100, SimulationSpeed.Speed1)).Direction);
        Assert.Equal(2, PlayerShipFrom(engine.CaptureSnapshotForTests(200, SimulationSpeed.Speed1)).Direction);
    }

    [Fact]
    public void One_shot_engine_command_does_not_change_motion_while_paused()
    {
        var engine = CreateEngine(speedMps: 700, directionDegrees: 12);

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnRightStep));
        var paused = PlayerShipFrom(engine.CaptureSnapshotForTests(1000, SimulationSpeed.Speed0));

        Assert.Equal(0.7, paused.SpeedKmS);
        Assert.Equal(12, paused.Direction);
        Assert.Equal(13, PlayerShipFrom(engine.CaptureSnapshotForTests(1100, SimulationSpeed.Speed1)).Direction);
    }

    [Theory]
    [InlineData("OTHER", EngineModuleId, ShipEngineCommandTypes.Accelerate)]
    [InlineData(PlayerShipId, "MISSING", ShipEngineCommandTypes.Accelerate)]
    [InlineData(PlayerShipId, EngineModuleId, "engine.unknown")]
    public void Invalid_commands_do_not_change_authoritative_motion(
        string objectId,
        string moduleId,
        string commandType)
    {
        var engine = CreateEngine(speedMps: 700, directionDegrees: 12);

        engine.ReceiveCommand(new PlayerCommand("cmd-1", 1, objectId, moduleId, commandType));
        var ship = PlayerShipFrom(engine.CaptureSnapshotForTests());

        Assert.Equal(0.7, ship.SpeedKmS);
        Assert.Equal(12, ship.Direction);
    }

    [Theory]
    [InlineData("Off", "Ready", 100)]
    [InlineData("On", "Disabled", 100)]
    [InlineData("On", "Ready", 0)]
    public void Unavailable_engine_module_rejects_commands_without_state_change(
        string powerState,
        string operationalState,
        int structurePoints)
    {
        var engine = CreateEngine(
            speedMps: 700,
            directionDegrees: 12,
            powerState: powerState,
            operationalState: operationalState,
            structurePoints: structurePoints);

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.Accelerate));
        var ship = PlayerShipFrom(engine.CaptureSnapshotForTests());

        Assert.Equal(0.7, ship.SpeedKmS);
        Assert.Equal(12, ship.Direction);
    }

    private static PlayerCommand Command(string commandType)
    {
        return new PlayerCommand("cmd-1", 1, PlayerShipId, EngineModuleId, commandType);
    }

    private static ObjectMotionSnapshot PlayerShipFrom(AuthoritativeSnapshot snapshot)
    {
        return snapshot.Objects.Single(o => o.ObjectId == PlayerShipId);
    }

    private static SimulationEngine CreateEngine(
        int speedMps = 0,
        int directionDegrees = 0,
        string powerState = "On",
        string operationalState = "Ready",
        int structurePoints = 100,
        string activeCycleJson = "null")
    {
        var engine = new SimulationEngine(CreateRegistry());
        engine.LoadScenario(ScenarioLoader.LoadFromJson($$"""
        {
          "scenarioMetadata": { "scenarioId": "test", "name": "Test" },
          "gameState": {
            "gameTimeMs": 0,
            "currentSpeed": "Speed0",
            "playerShipObjectId": "{{PlayerShipId}}",
            "spaceObjects": [
              {
                "objectId": "{{PlayerShipId}}",
                "objectType": "PlayerShip",
                "persistenceType": "Permanent",
                "positionX": 0,
                "positionY": 0,
                "speedMps": {{speedMps}},
                "directionDegrees": {{directionDegrees}},
                "movementType": "Linear",
                "modules": [
                  {
                    "moduleId": "{{EngineModuleId}}",
                    "moduleTypeId": "module.engine.basic",
                    "platformIndex": 0,
                    "occupiedCells": [0],
                    "structurePoints": {{structurePoints}},
                    "powerState": "{{powerState}}",
                    "operationalState": "{{operationalState}}",
                    "activeCycle": {{activeCycleJson}},
                    "cargo": []
                  }
                ]
              },
              {
                "objectId": "OTHER",
                "objectType": "PlayerShip",
                "persistenceType": "Permanent",
                "positionX": 0,
                "positionY": 0,
                "speedMps": 0,
                "directionDegrees": 0,
                "movementType": "Stationary",
                "modules": [
                  {
                    "moduleId": "{{EngineModuleId}}",
                    "moduleTypeId": "module.engine.basic",
                    "platformIndex": 0,
                    "occupiedCells": [0],
                    "structurePoints": 100,
                    "powerState": "On",
                    "operationalState": "Ready",
                    "activeCycle": null,
                    "cargo": []
                  }
                ]
              }
            ]
          }
        }
        """));

        return engine;
    }

    private static GameDataRegistry CreateRegistry()
    {
        string[] commandIds =
        [
            ShipEngineCommandTypes.Accelerate,
            ShipEngineCommandTypes.Brake,
            ShipEngineCommandTypes.TurnLeftStep,
            ShipEngineCommandTypes.TurnRightStep,
            ShipEngineCommandTypes.TurnLeftUntilCancel,
            ShipEngineCommandTypes.TurnRightUntilCancel,
            ShipEngineCommandTypes.CancelAll
        ];

        return GameDataRegistry.Create(
            [
                new ModuleTypeDefinition(
                    "module.engine.basic",
                    "Engine",
                    SlotSize: 1,
                    MassKg: 5000,
                    StructurePointsMax: 100,
                    PowerConsumptionW: 0,
                    CommandTypeIds: commandIds.ToImmutableArray(),
                    CargoCapacityKg: null,
                    MaxSpeedMps: 4000,
                    TurnStepDegrees: 1)
            ],
            [],
            commandIds.Select(id => new CommandDefinition(id, id)));
    }
}
