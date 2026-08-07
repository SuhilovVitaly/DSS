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
    public void Accelerate_ramps_up_speed_bounded_by_linear_inertia()
    {
        var engine = CreateEngine(linearInertiaMps2: 2000); // 2.0 km/s per game second.

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.Accelerate));
        Assert.Equal(0, PlayerShipFrom(engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1)).SpeedKmS);
        // dt = 1 s → delta = 2.0 km/s.
        Assert.Equal(2.0, PlayerShipFrom(engine.CaptureSnapshotForTests(1000, SimulationSpeed.Speed1)).SpeedKmS);
        // dt = 1 s → 2.0 + 2.0 == MaxSpeedKmS (4.0).
        Assert.Equal(4.0, PlayerShipFrom(engine.CaptureSnapshotForTests(2000, SimulationSpeed.Speed1)).SpeedKmS);
        // Clamped at MaxSpeedKmS, never exceeds it.
        Assert.Equal(4.0, PlayerShipFrom(engine.CaptureSnapshotForTests(3000, SimulationSpeed.Speed1)).SpeedKmS);
    }

    [Fact]
    public void Brake_ramps_down_speed_bounded_by_linear_inertia()
    {
        var engine = CreateEngine(speedMps: 3000, linearInertiaMps2: 2000); // Start 3.0 km/s.

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.Brake));
        Assert.Equal(3.0, PlayerShipFrom(engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1)).SpeedKmS);
        // dt = 1 s → 3.0 - 2.0.
        Assert.Equal(1.0, PlayerShipFrom(engine.CaptureSnapshotForTests(1000, SimulationSpeed.Speed1)).SpeedKmS);
        // 1.0 - 2.0 clamps to 0.
        Assert.Equal(0, PlayerShipFrom(engine.CaptureSnapshotForTests(2000, SimulationSpeed.Speed1)).SpeedKmS);
        // Never goes negative.
        Assert.Equal(0, PlayerShipFrom(engine.CaptureSnapshotForTests(3000, SimulationSpeed.Speed1)).SpeedKmS);
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

        // Genuine pause: SimulationClock's own invariant is that GameTimeMs does not
        // advance at Speed0, so repeated captures at the SAME gameTimeMs must not
        // progress the turn — regardless of how many times a snapshot is captured.
        Assert.Equal(0, PlayerShipFrom(engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0)).Direction);
        Assert.Equal(0, PlayerShipFrom(engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0)).Direction);
    }

    [Fact]
    public void Cycles_due_during_a_running_period_complete_even_if_the_snapshot_reports_paused()
    {
        // Regression test: the engine's snapshot loop yields once per real second
        // regardless of speed. If a pause command lands mid-interval, the FIRST snapshot
        // built after it can carry a GameTimeMs that already includes the running period
        // from before the pause (SimulationClock correctly accumulates that time before
        // switching speed) — even though THIS snapshot's own reported CurrentSpeed is
        // already Speed0. Any turn-cycle step genuinely due during that running portion
        // must still complete; it must not wait for the next non-paused snapshot to catch
        // up (which produces a visible snap once several steps complete at once).
        var engine = CreateEngine(directionDegrees: 0);
        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnRightUntilCancel));
        Assert.Equal(0, PlayerShipFrom(engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1)).Direction);

        var afterPause = PlayerShipFrom(engine.CaptureSnapshotForTests(1000, SimulationSpeed.Speed0));

        Assert.Equal(1, afterPause.Direction);
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
    public void One_shot_command_cancels_active_auto_repeat_cycle_and_executes()
    {
        var engine = CreateEngine(directionDegrees: 12);

        // Start Accelerate (enqueue; applied on next BuildSnapshot tick).
        engine.ReceiveCommand(Command(ShipEngineCommandTypes.Accelerate));
        engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);
        // Accelerate completes after 100 ms of game time.
        var running = PlayerShipFrom(engine.CaptureSnapshotForTests(100, SimulationSpeed.Speed1));
        Assert.Equal(4.0, running.SpeedKmS);
        Assert.Equal(ShipEngineCommandTypes.Accelerate, running.ActiveEngineCommandType);

        // TurnRightStep cancels the auto-repeating Accelerate and executes.
        // Speed stays at the achieved value (not rolled back).
        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnRightStep));
        engine.CaptureSnapshotForTests(200, SimulationSpeed.Speed1); // TurnRightStep starts this tick
        // One-shot turn completes on the next tick.
        var afterTurn = PlayerShipFrom(engine.CaptureSnapshotForTests(300, SimulationSpeed.Speed1));
        Assert.Equal(4.0, afterTurn.SpeedKmS);
        Assert.Equal(13, afterTurn.Direction);
        // One-shot turn completed; no active cycle remains.
        Assert.Null(afterTurn.ActiveEngineCommandType);
    }

    [Fact]
    public void Different_periodic_command_cancels_and_replaces_active_cycle()
    {
        var engine = CreateEngine();

        // Start Accelerate and let it complete once.
        engine.ReceiveCommand(Command(ShipEngineCommandTypes.Accelerate));
        engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);
        var running = PlayerShipFrom(engine.CaptureSnapshotForTests(100, SimulationSpeed.Speed1));
        Assert.Equal(4.0, running.SpeedKmS);
        Assert.Equal(ShipEngineCommandTypes.Accelerate, running.ActiveEngineCommandType);

        // Brake cancels Accelerate and takes over.
        engine.ReceiveCommand(Command(ShipEngineCommandTypes.Brake));
        engine.CaptureSnapshotForTests(200, SimulationSpeed.Speed1); // Brake starts this tick
        var afterBrake = PlayerShipFrom(engine.CaptureSnapshotForTests(300, SimulationSpeed.Speed1));
        Assert.Equal(0, afterBrake.SpeedKmS);
        Assert.Equal(ShipEngineCommandTypes.Brake, afterBrake.ActiveEngineCommandType);
    }

    [Fact]
    public void Snapshot_reports_max_speed_km_s_for_engine_equipped_object()
    {
        var engine = CreateEngine();
        var ship = PlayerShipFrom(engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1));

        // Test registry has MaxSpeedMps: 4000 → 4.0 km/s.
        Assert.Equal(4.0, ship.MaxSpeedKmS);
    }

    [Fact]
    public void Until_cancel_turn_mutual_replacement_is_allowed()
    {
        var engine = CreateEngine(directionDegrees: 0);

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnRightUntilCancel));
        Assert.Equal(0, PlayerShipFrom(engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1)).Direction);
        Assert.Equal(1, PlayerShipFrom(engine.CaptureSnapshotForTests(1000, SimulationSpeed.Speed1)).Direction);

        // TurnLeftUntilCancel replaces TurnRightUntilCancel (mutual replacement).
        // CompleteActiveEngineCycles runs before ApplyPendingCommands, so
        // the existing TurnRightUntilCancel cycle completes one more time (+1°)
        // before TurnLeftUntilCancel takes over.
        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnLeftUntilCancel));
        var afterReplace = PlayerShipFrom(engine.CaptureSnapshotForTests(2000, SimulationSpeed.Speed1));
        Assert.Equal(2, afterReplace.Direction);
        Assert.Equal(ShipEngineCommandTypes.TurnLeftUntilCancel, afterReplace.ActiveEngineCommandType);

        // TurnLeftUntilCancel now takes effect, reversing the direction.
        Assert.Equal(1, PlayerShipFrom(engine.CaptureSnapshotForTests(3000, SimulationSpeed.Speed1)).Direction);
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

    [Fact]
    public void Engine_without_linear_inertia_rejects_engine_commands()
    {
        var engine = CreateEngine(speedMps: 700, directionDegrees: 12, linearInertiaMps2: 0);

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
        string activeCycleJson = "null",
        int linearInertiaMps2 = 40000)
    {
        var engine = new SimulationEngine(CreateRegistry(linearInertiaMps2));
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

    private static GameDataRegistry CreateRegistry(int linearInertiaMps2 = 40000)
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
                    TurnStepDegrees: 1,
                    LinearInertiaMps2: linearInertiaMps2)
            ],
            [],
            commandIds.Select(id => new CommandDefinition(id, id)));
    }
}
