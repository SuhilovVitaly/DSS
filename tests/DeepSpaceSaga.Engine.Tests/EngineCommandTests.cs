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

    [Fact]
    public void Executed_command_appears_in_next_snapshot_with_full_details()
    {
        var engine = CreateEngine();

        engine.ReceiveCommand(new PlayerCommand("cmd-1", 1, PlayerShipId, EngineModuleId, ShipEngineCommandTypes.Accelerate));

        // t=0: cycle started (zero-duration guard prevents completion in the starting tick).
        // No CommandResult yet — Executed appears on completion (§56.5).
        var snapshot0 = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);
        Assert.Empty(snapshot0.CommandResults);

        // t=100: cycle completes → CommandResult(Executed).
        var snapshot = engine.CaptureSnapshotForTests(100, SimulationSpeed.Speed1);

        // AC1: exactly one result, Executed, all fields match, no reason code,
        // effective game time equals the completion time.
        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Executed, result.Status);
        Assert.Equal("cmd-1", result.CommandId);
        Assert.Equal(PlayerShipId, result.ObjectId);
        Assert.Equal(EngineModuleId, result.ModuleId);
        Assert.Equal(ShipEngineCommandTypes.Accelerate, result.CommandType);
        Assert.Null(result.ReasonCode);
        Assert.Equal(100, result.EffectiveGameTimeMs);
    }

    [Theory]
    [InlineData("OTHER", EngineModuleId, ShipEngineCommandTypes.Accelerate, CommandReasonCodes.UnknownObject)]
    [InlineData(PlayerShipId, "MISSING", ShipEngineCommandTypes.Accelerate, CommandReasonCodes.UnknownModule)]
    [InlineData(PlayerShipId, EngineModuleId, "engine.unknown", CommandReasonCodes.UnknownCommandType)]
    public void Invalid_commands_are_rejected_with_machine_readable_reason_codes(
        string objectId,
        string moduleId,
        string commandType,
        string expectedReason)
    {
        var engine = CreateEngine();

        engine.ReceiveCommand(new PlayerCommand("cmd-1", 1, objectId, moduleId, commandType));
        var snapshot = engine.CaptureSnapshotForTests();

        // AC2: wrong object / wrong module / wrong type → Rejected with the
        // matching snake_case reason code.
        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(expectedReason, result.ReasonCode);
        Assert.Equal(commandType, result.CommandType);
    }

    [Fact]
    public void Deferred_command_is_reported_once_and_executed_on_the_next_snapshot()
    {
        var engine = CreateEngine(directionDegrees: 0);

        // AC3: two one-shot TurnRightStep commands in the same tick — the module
        // is busy for the second one, which is genuinely requeued.
        engine.ReceiveCommand(new PlayerCommand("cmd-1", 1, PlayerShipId, EngineModuleId, ShipEngineCommandTypes.TurnRightStep));
        engine.ReceiveCommand(new PlayerCommand("cmd-2", 2, PlayerShipId, EngineModuleId, ShipEngineCommandTypes.TurnRightStep));

        // t=0: cmd-1 starts a zero-duration cycle (no CommandResult at start per §56.5);
        // cmd-2 is Deferred. Zero-duration guard prevents cmd-1 from completing in its
        // starting tick, so only the Deferred result is visible.
        var first = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);
        var cmd2Deferred = Assert.Single(first.CommandResults);
        Assert.Equal("cmd-2", cmd2Deferred.CommandId);
        Assert.Equal(CommandResultStatus.Deferred, cmd2Deferred.Status);
        Assert.Equal(CommandReasonCodes.Busy, cmd2Deferred.ReasonCode);

        // t=100: cmd-1's zero-duration cycle completes → Executed. cmd-2 starts from
        // the deferred queue and gets its own zero-duration cycle (no result yet).
        var second = engine.CaptureSnapshotForTests(100, SimulationSpeed.Speed1);
        Assert.Equal(1, PlayerShipFrom(second).Direction);
        var cmd1Executed = Assert.Single(second.CommandResults);
        Assert.Equal("cmd-1", cmd1Executed.CommandId);
        Assert.Equal(CommandResultStatus.Executed, cmd1Executed.Status);
        Assert.Null(cmd1Executed.ReasonCode);

        // t=200: cmd-2's zero-duration cycle completes → Executed. No more pending.
        var third = engine.CaptureSnapshotForTests(200, SimulationSpeed.Speed1);
        Assert.Equal(2, PlayerShipFrom(third).Direction);
        var cmd2Executed = Assert.Single(third.CommandResults);
        Assert.Equal("cmd-2", cmd2Executed.CommandId);
        Assert.Equal(CommandResultStatus.Executed, cmd2Executed.Status);
        Assert.Null(cmd2Executed.ReasonCode);
    }

    [Fact]
    public void Command_results_are_immutable_after_snapshot_publication()
    {
        var engine = CreateEngine();

        // cmd-1 (Accelerate) starts a zero-duration cycle at t=0. No CommandResult at start
        // per §56.5 — Executed appears on cycle completion.
        engine.ReceiveCommand(new PlayerCommand("cmd-1", 1, PlayerShipId, EngineModuleId, ShipEngineCommandTypes.Accelerate));
        var snapshotA = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);
        Assert.Empty(snapshotA.CommandResults);

        // Process another command; the previous cycle (cmd-1) completes at t=100.
        // cmd-2 (Brake) starts a new zero-duration cycle — no CommandResult at start.
        engine.ReceiveCommand(new PlayerCommand("cmd-2", 2, PlayerShipId, EngineModuleId, ShipEngineCommandTypes.Brake));
        var snapshotB = engine.CaptureSnapshotForTests(100, SimulationSpeed.Speed1);

        // AC4: snapshot A is not mutated by later processing.
        Assert.Empty(snapshotA.CommandResults);

        // snapshot B: cmd-1's auto-repeat Accelerate completed one step at t=100
        // (Executed), then Brake implicitly cancelled the renewed cycle (Cancelled).
        // Dedup by CommandId keeps the last disposition → Cancelled. cmd-2 has no
        // result yet — its cycle completes at t=200.
        var resultB = Assert.Single(snapshotB.CommandResults);
        Assert.Equal("cmd-1", resultB.CommandId);
        Assert.Equal(CommandResultStatus.Cancelled, resultB.Status);
        Assert.Equal(ShipEventReasonCodes.CancelledByCommand, resultB.ReasonCode);
    }

    [Theory]
    [InlineData("Off", "Ready", 100)]
    [InlineData("On", "Disabled", 100)]
    [InlineData("On", "Ready", 0)]
    public void Unavailable_engine_module_rejects_with_module_unavailable_reason(
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
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(CommandReasonCodes.ModuleUnavailable, result.ReasonCode);
    }

    [Fact]
    public void Save_in_deferred_window_does_not_duplicate_command_results()
    {
        // Regression test for review finding 7.1:
        // CaptureSaveStateCore also calls ApplyPendingCommands — the drain in
        // BuildSnapshot must deduplicate by CommandId so a command processed
        // twice in one drain window (once by BuildSnapshot, once by
        // CaptureSaveStateCore) appears exactly once in the snapshot.
        var engine = CreateEngine(speedMps: 700, directionDegrees: 12);

        // Two one-shot commands on the same tick: first starts, second is deferred (busy).
        engine.ReceiveCommand(new PlayerCommand("cmd-1", 1, PlayerShipId, EngineModuleId, ShipEngineCommandTypes.Accelerate));
        engine.ReceiveCommand(new PlayerCommand("cmd-2", 2, PlayerShipId, EngineModuleId, ShipEngineCommandTypes.Accelerate));

        // Simulate F5 save right after the commands, inside the deferred window.
        // This calls CaptureSaveStateCore → ApplyPendingCommands → RecordCommandResult
        // for both commands, populating _commandResults with Deferred for cmd-2.
        engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed1);

        // Advance time so the deferred command executes.
        var snapshot = engine.CaptureSnapshotForTests(100, SimulationSpeed.Speed1);

        // No duplicates: at most one CommandResult per CommandId.
        var byId = snapshot.CommandResults.GroupBy(r => r.CommandId).ToList();
        foreach (var group in byId)
            Assert.Single(group);

        // Verify the content is correct (not just "no duplicates").
        Assert.Contains(snapshot.CommandResults, r => r.CommandId == "cmd-1" && r.Status == CommandResultStatus.Executed);
        Assert.Contains(snapshot.CommandResults, r => r.CommandId == "cmd-2");
    }

    // ── ShipEvent tests (ТЗ-02) ────────────────────────────────────

    [Fact]
    public void TurnRightStep_completion_publishes_command_completed_event()
    {
        // AC1: a completed one-shot command publishes a command_completed ShipEvent
        // with the completion game time (first tick where gameTimeMs > StartedGameTimeMs).
        var engine = CreateEngine();
        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnRightStep));

        // t=0: zero-duration cycle guard prevents completion in the starting tick.
        var snapshot0 = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);
        Assert.Empty(snapshot0.ShipEvents);

        // t=100: duration 0 cycle completes at gameTimeMs=100.
        var snapshot100 = engine.CaptureSnapshotForTests(100, SimulationSpeed.Speed1);
        var e = Assert.Single(snapshot100.ShipEvents);
        Assert.Equal("EVE-000001", e.EventId);
        Assert.Equal(PlayerShipId, e.ObjectId);
        Assert.Equal(EngineModuleId, e.ModuleId);
        Assert.Equal(ShipEventTypes.CommandCompleted, e.EventType);
        Assert.Null(e.ReasonCode);
        Assert.Equal(100, e.GameTimeMs);

        // t=200: events drained — nothing new.
        var snapshot200 = engine.CaptureSnapshotForTests(200, SimulationSpeed.Speed1);
        Assert.Empty(snapshot200.ShipEvents);
    }

    [Fact]
    public void Auto_repeat_publishes_command_completed_per_step()
    {
        var engine = CreateEngine();
        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnRightUntilCancel));

        // t=0: guard prevents zero-duration completion.
        var snapshot0 = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);
        Assert.Empty(snapshot0.ShipEvents);

        // t=1000: first step completes (1000 ms duration).
        var snapshot1000 = engine.CaptureSnapshotForTests(1000, SimulationSpeed.Speed1);
        var e1 = Assert.Single(snapshot1000.ShipEvents);
        Assert.Equal("EVE-000001", e1.EventId);
        Assert.Equal(ShipEventTypes.CommandCompleted, e1.EventType);
        Assert.Equal(1000, e1.GameTimeMs);

        // t=2000: second step completes.
        var snapshot2000 = engine.CaptureSnapshotForTests(2000, SimulationSpeed.Speed1);
        var e2 = Assert.Single(snapshot2000.ShipEvents);
        Assert.Equal("EVE-000002", e2.EventId);
        Assert.Equal(ShipEventTypes.CommandCompleted, e2.EventType);
        Assert.Equal(2000, e2.GameTimeMs);
    }

    [Fact]
    public void CancelAll_publishes_cycle_cancelled_event()
    {
        // AC2: CancelAll cancels an active auto-repeat cycle and publishes
        // a cycle_cancelled event with cancelled_by_command reason.
        var engine = CreateEngine();
        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnRightUntilCancel));
        engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.CancelAll));
        var snapshot = engine.CaptureSnapshotForTests(500, SimulationSpeed.Speed1);

        // cycle_cancelled at t=500 (the tick CancelAll was processed).
        var cancelled = Assert.Single(snapshot.ShipEvents);
        Assert.Equal(ShipEventTypes.CycleCancelled, cancelled.EventType);
        Assert.Equal(ShipEventReasonCodes.CancelledByCommand, cancelled.ReasonCode);
        Assert.Equal(500, cancelled.GameTimeMs);

        // No completion event — the cycle was cancelled before its duration elapsed
        // (500 < 1000 ms TurnRightUntilCancel duration).
        Assert.DoesNotContain(snapshot.ShipEvents,
            e => e.EventType == ShipEventTypes.CommandCompleted);

        // Later snapshot: no stale events.
        var snapshot1500 = engine.CaptureSnapshotForTests(1500, SimulationSpeed.Speed1);
        Assert.Empty(snapshot1500.ShipEvents);
    }

    [Fact]
    public void Implicit_cancel_publishes_cycle_cancelled_and_then_completed()
    {
        // AC2: sending a one-shot command of a different type while an auto-repeat
        // cycle is running implicitly cancels the auto-repeat and starts the one-shot.
        var engine = CreateEngine();
        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnRightUntilCancel));
        engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnLeftStep));
        var snapshot = engine.CaptureSnapshotForTests(500, SimulationSpeed.Speed1);

        // t=500: cycle_cancelled (implicit cancel processed at this tick).
        var cancelled = Assert.Single(snapshot.ShipEvents, e => e.EventType == ShipEventTypes.CycleCancelled);
        Assert.Equal(ShipEventReasonCodes.CancelledByCommand, cancelled.ReasonCode);
        Assert.Equal(500, cancelled.GameTimeMs);

        // t=600: TurnLeftStep completes (500 + 100 ms duration).
        var snapshot600 = engine.CaptureSnapshotForTests(600, SimulationSpeed.Speed1);
        var completed = Assert.Single(snapshot600.ShipEvents, e => e.EventType == ShipEventTypes.CommandCompleted);
        Assert.Null(completed.ReasonCode);
        Assert.Equal(600, completed.GameTimeMs);
    }

    [Theory]
    [InlineData("Off", "Ready", 100, "power_off")]
    [InlineData("On", "Disabled", 100, "module_disabled")]
    [InlineData("On", "Ready", 0, "module_destroyed")]
    public void Interruption_publishes_cycle_interrupted_with_correct_reason(
        string powerState, string operationalState, int structurePoints, string expectedReason)
    {
        // Load a scenario with an active auto-repeat cycle on an unavailable module.
        // The cycle reaches its completion time at startedGameTimeMs + durationMs = 1000.
        string activeCycleJson = $$"""
        {
          "cycleId": "CYC-ENGINE-000001",
          "commandType": "engine.turn_right_until_cancel",
          "startedGameTimeMs": 0,
          "durationMs": 1000,
          "isAutoRepeat": true
        }
        """;

        var engine = CreateEngine(
            powerState: powerState,
            operationalState: operationalState,
            structurePoints: structurePoints,
            activeCycleJson: activeCycleJson);

        var snapshot = engine.CaptureSnapshotForTests(1000, SimulationSpeed.Speed1);

        var interrupted = Assert.Single(snapshot.ShipEvents);
        Assert.Equal(ShipEventTypes.CycleInterrupted, interrupted.EventType);
        Assert.Equal(expectedReason, interrupted.ReasonCode);
        Assert.Equal(1000, interrupted.GameTimeMs);

        // No completion event — cycle was interrupted before completing.
        Assert.DoesNotContain(snapshot.ShipEvents,
            e => e.EventType == ShipEventTypes.CommandCompleted);
    }

    [Fact]
    public void Save_in_cancel_window_does_not_duplicate_ship_events()
    {
        // Regression: CaptureSaveStateCore calls ApplyPendingCommands which may
        // process CancelAll — the drain must not duplicate ShipEvents.
        var engine = CreateEngine();
        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnRightUntilCancel));
        engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.CancelAll));
        engine.CaptureSaveStateForTests(500, SimulationSpeed.Speed1);

        var snapshot = engine.CaptureSnapshotForTests(500, SimulationSpeed.Speed1);

        // Each EventId appears at most once.
        var byId = snapshot.ShipEvents.GroupBy(e => e.EventId).ToList();
        foreach (var group in byId)
            Assert.Single(group);

        Assert.Single(snapshot.ShipEvents,
            e => e.EventType == ShipEventTypes.CycleCancelled);
    }

    [Fact]
    public void Ship_events_are_immutable_after_snapshot_publication()
    {
        var engine = CreateEngine();
        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnRightStep));

        // t=100: ApplyPendingCommands creates the cycle (after CompleteActiveEngineCycles).
        engine.CaptureSnapshotForTests(100, SimulationSpeed.Speed1);

        // t=200: CompleteActiveEngineCycles finds the cycle and completes it → ShipEvent.
        var snapshotA = engine.CaptureSnapshotForTests(200, SimulationSpeed.Speed1);
        Assert.Single(snapshotA.ShipEvents);

        // Process another command; snapshot A must not be mutated.
        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnLeftStep));
        engine.CaptureSnapshotForTests(300, SimulationSpeed.Speed1);

        Assert.Single(snapshotA.ShipEvents);
        Assert.Equal("EVE-000001", snapshotA.ShipEvents[0].EventId);
        Assert.Equal(ShipEventTypes.CommandCompleted, snapshotA.ShipEvents[0].EventType);
    }

    // ── Match command tests (ТЗ-04) ───────────────────────────────

    [Fact]
    public void MatchTargetSpeed_without_target_publishes_rejected()
    {
        // AC1: a match command without targetObjectId is rejected with missing_target
        // (§56.9) — before any module-state check. Ship motion is untouched, no cycle starts.
        var engine = CreateEngine();

        engine.ReceiveCommand(new PlayerCommand(
            "cmd-1", 1, PlayerShipId, EngineModuleId, ShipEngineCommandTypes.MatchTargetSpeed));

        var snapshot = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(CommandReasonCodes.MissingTarget, result.ReasonCode);

        var ship = PlayerShipFrom(snapshot);
        Assert.Equal(0, ship.SpeedKmS);
        Assert.Equal(0, ship.Direction);
        Assert.Null(ship.ActiveEngineCommandType);
    }

    [Fact]
    public void MatchTargetCourse_with_unknown_target_publishes_rejected()
    {
        // AC2: a match command referencing an object that does not exist in the world
        // is rejected with unknown_target (§56.9).
        var engine = CreateEngine();

        engine.ReceiveCommand(new PlayerCommand(
            "cmd-1", 1, PlayerShipId, EngineModuleId, ShipEngineCommandTypes.MatchTargetCourse,
            TargetObjectId: "GHOST"));

        var snapshot = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(CommandReasonCodes.UnknownTarget, result.ReasonCode);
    }

    [Fact]
    public void MatchTargetSpeed_captures_target_speed_at_cycle_start()
    {
        // AC3: at cycle start the ActiveCycle stores TargetObjectId and the captured
        // scalar speed; completion applies ONLY the captured value, so later target
        // changes or the target disappearing do not affect the result (§56.9).
        // Course is not captured and not changed.
        var engine = CreateEngine(targetSpeedMps: 2500); // OTHER = 2.5 km/s, ship = 0.

        engine.ReceiveCommand(new PlayerCommand(
            "cmd-1", 1, PlayerShipId, EngineModuleId, ShipEngineCommandTypes.MatchTargetSpeed,
            TargetObjectId: "OTHER"));

        // t=0: cycle starts. No CommandResult at start per §56.5 — Executed appears
        // on completion. A zero-duration one-shot does not complete in its starting
        // tick (guard gameTimeMs > StartedGameTimeMs).
        var snapshot0 = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);
        Assert.Empty(snapshot0.CommandResults);

        var module = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == PlayerShipId).Modules.Single();
        Assert.Equal("OTHER", module.ActiveCycle!.TargetObjectId);
        Assert.Equal(2.5, module.ActiveCycle!.CapturedTargetSpeedKmS);
        Assert.Null(module.ActiveCycle!.CapturedTargetCourseDegrees);

        // t=100: cycle completes — ship speed matches the captured target speed,
        // direction unchanged (MatchTargetSpeed changes only scalar speed).
        var ship = PlayerShipFrom(engine.CaptureSnapshotForTests(100, SimulationSpeed.Speed1));
        Assert.Equal(2.5, ship.SpeedKmS);
        Assert.Equal(0, ship.Direction);
    }

    [Fact]
    public void MatchTargetCourse_captures_target_course_at_cycle_start()
    {
        // AC3: MatchTargetCourse captures the target course only; speed is not
        // captured and not changed (MatchTargetCourse changes only course).
        var engine = CreateEngine(speedMps: 700, directionDegrees: 12, targetDirectionDegrees: 180);

        engine.ReceiveCommand(new PlayerCommand(
            "cmd-1", 1, PlayerShipId, EngineModuleId, ShipEngineCommandTypes.MatchTargetCourse,
            TargetObjectId: "OTHER"));

        var snapshot0 = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);
        Assert.Empty(snapshot0.CommandResults);

        var module = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == PlayerShipId).Modules.Single();
        Assert.Equal("OTHER", module.ActiveCycle!.TargetObjectId);
        Assert.Equal(180, module.ActiveCycle!.CapturedTargetCourseDegrees);
        Assert.Null(module.ActiveCycle!.CapturedTargetSpeedKmS);

        var ship = PlayerShipFrom(engine.CaptureSnapshotForTests(100, SimulationSpeed.Speed1));
        Assert.Equal(180, ship.Direction);
        Assert.Equal(0.7, ship.SpeedKmS);
    }

    [Fact]
    public void Match_cycle_captured_scalars_survive_save_load()
    {
        // AC3 + §1253-1298: the captured scalar is persisted in the save and restored
        // on load — completion after F9 uses the restored captured value and does not
        // depend on the original target object.
        var engine = CreateEngine(targetSpeedMps: 2500);

        engine.ReceiveCommand(new PlayerCommand(
            "cmd-1", 1, PlayerShipId, EngineModuleId, ShipEngineCommandTypes.MatchTargetSpeed,
            TargetObjectId: "OTHER"));

        // Save in the active-cycle window (cycle started, not yet completed).
        var saveState = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed1);

        // Continue in a fresh engine (SaveLoadContinuityTests pattern): the save's
        // gameTimeMs is 0, so LoadScenario's StartGameTimeMs stamp is already correct.
        var loadedEngine = new SimulationEngine(CreateRegistry());
        loadedEngine.LoadScenario(saveState);

        var ship = PlayerShipFrom(loadedEngine.CaptureSnapshotForTests(100, SimulationSpeed.Speed1));
        Assert.Equal(2.5, ship.SpeedKmS);
        Assert.Equal(0, ship.Direction);
    }

    // ── Factor model tests (ТЗ-06) ────────────────────────────────

    [Fact]
    public void TimeFactor_is_stored_as_fixed_point_int()
    {
        // AC1: timeFactor 1.2 in JSON → 1200 fixed-point.
        string directory = Path.Combine(Path.GetTempPath(), $"dss-factor-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "Settings.json"), """
            { "typeData": { "moduleTypes": "module-types.json", "itemTypes": "item-types.json", "commandDefinitions": "command-definitions.json" }, "defaultScenario": "scenario.json" }
            """);
            File.WriteAllText(Path.Combine(directory, "module-types.json"), """
            { "moduleTypes": [ { "typeId": "module.engine.basic", "displayName": "E", "slotSize": 1, "massKg": 1, "structurePointsMax": 1, "powerConsumptionW": 0, "commandTypeIds": [], "cargoCapacityKg": null, "baseCycleTimeMs": 1000, "maxSpeedMps": 4000, "turnStepDegrees": 1, "linearInertiaMps2": 400, "fuelCapacityKg": 1 } ] }
            """);
            File.WriteAllText(Path.Combine(directory, "item-types.json"), """{ "itemTypes": [] }""");
            File.WriteAllText(Path.Combine(directory, "command-definitions.json"), """
            { "commandDefinitions": [ { "typeId": "engine.accelerate", "displayName": "A", "timeFactor": 1.2 } ] }
            """);
            File.WriteAllText(Path.Combine(directory, "scenario.json"), DefaultScenarioJson);

            var registry = EngineContentLoader.LoadRegistryFromSettingsFile(
                Path.Combine(directory, "Settings.json"), out _, out _);

            var cmd = registry.CommandDefinitions.GetDefinition(0);
            Assert.Equal(1200, cmd.TimeFactor);
            Assert.Equal(1000, cmd.ComplexityFactor); // default
            Assert.Equal(1000, cmd.ConsumptionFactor); // default
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ComplexityFactor_is_stored_as_fixed_point_int()
    {
        // AC2: complexityFactor 0.75 in JSON → 750 fixed-point.
        string directory = Path.Combine(Path.GetTempPath(), $"dss-factor-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "Settings.json"), """
            { "typeData": { "moduleTypes": "module-types.json", "itemTypes": "item-types.json", "commandDefinitions": "command-definitions.json" }, "defaultScenario": "scenario.json" }
            """);
            File.WriteAllText(Path.Combine(directory, "module-types.json"), """
            { "moduleTypes": [ { "typeId": "module.engine.basic", "displayName": "E", "slotSize": 1, "massKg": 1, "structurePointsMax": 1, "powerConsumptionW": 0, "commandTypeIds": [], "cargoCapacityKg": null, "baseCycleTimeMs": 1000, "maxSpeedMps": 4000, "turnStepDegrees": 1, "linearInertiaMps2": 400, "fuelCapacityKg": 1 } ] }
            """);
            File.WriteAllText(Path.Combine(directory, "item-types.json"), """{ "itemTypes": [] }""");
            File.WriteAllText(Path.Combine(directory, "command-definitions.json"), """
            { "commandDefinitions": [ { "typeId": "engine.accelerate", "displayName": "A", "timeFactor": 1.0, "complexityFactor": 0.75 } ] }
            """);
            File.WriteAllText(Path.Combine(directory, "scenario.json"), DefaultScenarioJson);

            var registry = EngineContentLoader.LoadRegistryFromSettingsFile(
                Path.Combine(directory, "Settings.json"), out _, out _);

            var cmd = registry.CommandDefinitions.GetDefinition(0);
            Assert.Equal(750, cmd.ComplexityFactor);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Effective_cycle_time_comes_from_content_data_not_hardcode()
    {
        // AC3: EffectiveCycleTimeMs is computed from ModuleTypeDefinition.BaseCycleTimeMs
        // and CommandDefinition.TimeFactor, not from a hardcoded IsUntilCancelTurn switch.
        // Give TurnRightUntilCancel a timeFactor of 2.0 → EffectiveCycleTimeMs = 2000.
        string[] commandIds =
        [
            ShipEngineCommandTypes.TurnRightUntilCancel
        ];

        var registry = GameDataRegistry.Create(
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
                    LinearInertiaMps2: 400,
                    BaseCycleTimeMs: 1000)
            ],
            [],
            [
                new CommandDefinition(
                    ShipEngineCommandTypes.TurnRightUntilCancel,
                    "Turn Right Until Cancel",
                    TimeFactor: 2000) // 2.0 → Ceil(1000 * 2000 / 1000) = 2000 ms
            ]);

        var engine = new SimulationEngine(registry);
        engine.LoadScenario(ScenarioLoader.LoadFromJson($$"""
        {
          "scenarioMetadata": { "scenarioId": "test", "name": "Test" },
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed0",
            "playerShipObjectId": "{{PlayerShipId}}",
            "spaceObjects": [
              { "objectId": "{{PlayerShipId}}", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary",
                "modules": [
                  { "moduleId": "{{EngineModuleId}}", "moduleTypeId": "module.engine.basic", "platformIndex": 0,
                    "occupiedCells": [0], "structurePoints": 100, "powerState": "On", "operationalState": "Ready",
                    "activeCycle": null, "cargo": [] }
                ]
              }
            ]
          }
        }
        """));

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnRightUntilCancel));

        // t=0: cycle starts, direction unchanged (zero-duration guard).
        Assert.Equal(0, PlayerShipFrom(engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1)).Direction);

        // t=1000: old hardcode would complete here. With timeFactor 2.0, duration is 2000 ms →
        // the cycle should NOT complete at t=1000.
        Assert.Equal(0, PlayerShipFrom(engine.CaptureSnapshotForTests(1000, SimulationSpeed.Speed1)).Direction);

        // t=2000: cycle should complete now.
        Assert.Equal(1, PlayerShipFrom(engine.CaptureSnapshotForTests(2000, SimulationSpeed.Speed1)).Direction);

        // Verify the active cycle's duration was stored as 2000.
        var module = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == PlayerShipId).Modules.Single();
        Assert.Equal(2000, module.ActiveCycle!.DurationMs);
    }

    [Fact]
    public void Missing_factor_defaults_to_neutral_1000()
    {
        // AC2 (partial): a command definition without timeFactor → TimeFactor = 1000 (1.0).
        string directory = Path.Combine(Path.GetTempPath(), $"dss-factor-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "Settings.json"), """
            { "typeData": { "moduleTypes": "module-types.json", "itemTypes": "item-types.json", "commandDefinitions": "command-definitions.json" }, "defaultScenario": "scenario.json" }
            """);
            File.WriteAllText(Path.Combine(directory, "module-types.json"), """
            { "moduleTypes": [ { "typeId": "module.engine.basic", "displayName": "E", "slotSize": 1, "massKg": 1, "structurePointsMax": 1, "powerConsumptionW": 0, "commandTypeIds": [], "cargoCapacityKg": null, "baseCycleTimeMs": 1000, "maxSpeedMps": 4000, "turnStepDegrees": 1, "linearInertiaMps2": 400, "fuelCapacityKg": 1 } ] }
            """);
            File.WriteAllText(Path.Combine(directory, "item-types.json"), """{ "itemTypes": [] }""");
            File.WriteAllText(Path.Combine(directory, "command-definitions.json"), """
            { "commandDefinitions": [ { "typeId": "engine.accelerate", "displayName": "A" } ] }
            """);
            File.WriteAllText(Path.Combine(directory, "scenario.json"), DefaultScenarioJson);

            var registry = EngineContentLoader.LoadRegistryFromSettingsFile(
                Path.Combine(directory, "Settings.json"), out _, out _);

            var cmd = registry.CommandDefinitions.GetDefinition(0);
            Assert.Equal(1000, cmd.TimeFactor);
            Assert.Equal(1000, cmd.ComplexityFactor);
            Assert.Equal(1000, cmd.ConsumptionFactor);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Active_module_type_without_base_cycle_time_throws()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"dss-factor-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "Settings.json"), """
            { "typeData": { "moduleTypes": "module-types.json", "itemTypes": "item-types.json", "commandDefinitions": "command-definitions.json" }, "defaultScenario": "scenario.json" }
            """);
            // Active module (has commandTypeIds) but no baseCycleTimeMs.
            File.WriteAllText(Path.Combine(directory, "module-types.json"), """
            { "moduleTypes": [ { "typeId": "module.engine.basic", "displayName": "E", "slotSize": 1, "massKg": 1, "structurePointsMax": 1, "powerConsumptionW": 0, "commandTypeIds": ["engine.accelerate"], "cargoCapacityKg": null, "maxSpeedMps": 4000, "turnStepDegrees": 1, "linearInertiaMps2": 400, "fuelCapacityKg": 1 } ] }
            """);
            File.WriteAllText(Path.Combine(directory, "item-types.json"), """{ "itemTypes": [] }""");
            File.WriteAllText(Path.Combine(directory, "command-definitions.json"), """
            { "commandDefinitions": [ { "typeId": "engine.accelerate", "displayName": "A" } ] }
            """);
            File.WriteAllText(Path.Combine(directory, "scenario.json"), DefaultScenarioJson);

            var ex = Assert.Throws<ContentException>(() =>
                EngineContentLoader.LoadRegistryFromSettingsFile(
                    Path.Combine(directory, "Settings.json"), out _, out _));
            Assert.Contains("baseCycleTimeMs", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Zero_duration_cycle_completes_no_earlier_than_next_tick()
    {
        // AC4: a zero-duration cycle started at gameTimeMs=X must complete at
        // gameTimeMs > X, never at gameTimeMs == X.
        var engine = CreateEngine(directionDegrees: 0);

        engine.ReceiveCommand(Command(ShipEngineCommandTypes.TurnRightStep));

        // t=0: guard prevents zero-duration cycle from completing in its starting tick.
        var snapshot0 = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);
        Assert.Equal(0, PlayerShipFrom(snapshot0).Direction);

        // t=50: cycle completes (50 > 0 and duration=0, so 50-0 >= 0).
        var snapshot50 = engine.CaptureSnapshotForTests(50, SimulationSpeed.Speed1);
        Assert.Equal(1, PlayerShipFrom(snapshot50).Direction);
    }

    // ── Fuel state tests (ТЗ-07) ──────────────────────────────────

    [Fact]
    public void Engine_module_type_without_fuel_capacity_throws()
    {
        // AC1: engine module type without valid FuelCapacityKg is rejected on load.
        string directory = Path.Combine(Path.GetTempPath(), $"dss-fuel-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "Settings.json"), """
            { "typeData": { "moduleTypes": "module-types.json", "itemTypes": "item-types.json", "commandDefinitions": "command-definitions.json" }, "defaultScenario": "scenario.json" }
            """);
            File.WriteAllText(Path.Combine(directory, "command-definitions.json"), """{ "commandDefinitions": [] }""");
            File.WriteAllText(Path.Combine(directory, "module-types.json"), """
            { "moduleTypes": [ { "typeId": "module.engine.basic", "displayName": "E", "slotSize": 1, "massKg": 1, "structurePointsMax": 1, "powerConsumptionW": 0, "commandTypeIds": [], "cargoCapacityKg": null, "maxSpeedMps": 4000, "turnStepDegrees": 1, "linearInertiaMps2": 400 } ] }
            """);
            File.WriteAllText(Path.Combine(directory, "item-types.json"), """{ "itemTypes": [] }""");
            File.WriteAllText(Path.Combine(directory, "scenario.json"), DefaultScenarioJson);

            var ex = Assert.Throws<ContentException>(() =>
                EngineContentLoader.LoadRegistryFromSettingsFile(
                    Path.Combine(directory, "Settings.json"), out _, out _));
            Assert.Contains("fuelCapacityKg", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Installed_engine_with_fuel_out_of_range_throws()
    {
        // AC2: installed engine with FuelAmountKg outside 0..FuelCapacityKg is rejected.
        var registry = GameDataRegistry.Create(
            [
                new ModuleTypeDefinition(
                    "module.engine.basic",
                    "Engine",
                    SlotSize: 1,
                    MassKg: 5000,
                    StructurePointsMax: 100,
                    PowerConsumptionW: 0,
                    CommandTypeIds: ImmutableArray<string>.Empty,
                    CargoCapacityKg: null,
                    MaxSpeedMps: 4000,
                    TurnStepDegrees: 1,
                    LinearInertiaMps2: 400,
                    FuelCapacityKg: 1000)
            ],
            [],
            []);

        var engine = new SimulationEngine(registry);
        var ex = Assert.Throws<ScenarioException>(() =>
            engine.LoadScenario(ScenarioLoader.LoadFromJson($$"""
            {
              "scenarioMetadata": { "scenarioId": "test", "name": "Test" },
              "gameState": {
                "gameTimeMs": 0, "currentSpeed": "Speed0",
                "playerShipObjectId": "{{PlayerShipId}}",
                "spaceObjects": [
                  { "objectId": "{{PlayerShipId}}", "objectType": "PlayerShip", "persistenceType": "Permanent",
                    "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                    "movementType": "Stationary",
                    "modules": [
                      { "moduleId": "{{EngineModuleId}}", "moduleTypeId": "module.engine.basic", "platformIndex": 0,
                        "occupiedCells": [0], "structurePoints": 100, "powerState": "On", "operationalState": "Ready",
                        "activeCycle": null, "cargo": [], "fuelAmountKg": 2000 }
                    ]
                  }
                ]
              }
            }
            """)));
        Assert.Contains("fuelAmountKg", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Default_scenario_engine_starts_with_half_tank()
    {
        // AC3: the default scenario file has an engine with fuelAmountKg exactly half of fuelCapacityKg.
        string scenarioPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "DeepSpaceSaga.Client", "Scenarios", "Default", "scenario.json"));

        if (!File.Exists(scenarioPath))
        {
            scenarioPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, "Scenarios", "Default", "scenario.json"));
        }

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
        var engineModuleType = registry.ModuleTypes.GetDefinition(
            registry.ModuleTypes.GetIndex("module.engine.basic"));
        Assert.True(engineModuleType.FuelCapacityKg is > 0);

        var scenario = ScenarioLoader.LoadFromFile(scenarioPath);
        var engine = new SimulationEngine(registry);
        engine.LoadScenario(scenario);

        var playerShip = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == "SPC-0001");
        var engineModule = Assert.Single(playerShip.Modules, m => m.ModuleId == "MOD-PLAYER-ENGINE-01");
        long expectedHalf = engineModuleType.FuelCapacityKg!.Value / 2;
        Assert.Equal(expectedHalf, engineModule.FuelAmountKg);
    }

    [Fact]
    public void Save_load_preserves_fuel_amount()
    {
        // AC4: save/load round-trip preserves FuelAmountKg.
        var registry = GameDataRegistry.Create(
            [
                new ModuleTypeDefinition(
                    "module.engine.basic",
                    "Engine",
                    SlotSize: 1,
                    MassKg: 5000,
                    StructurePointsMax: 100,
                    PowerConsumptionW: 0,
                    CommandTypeIds: ImmutableArray<string>.Empty,
                    CargoCapacityKg: null,
                    MaxSpeedMps: 4000,
                    TurnStepDegrees: 1,
                    LinearInertiaMps2: 400,
                    FuelCapacityKg: 10000)
            ],
            [],
            []);

        var engine = new SimulationEngine(registry);
        engine.LoadScenario(ScenarioLoader.LoadFromJson($$"""
        {
          "scenarioMetadata": { "scenarioId": "test", "name": "Test" },
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed0",
            "playerShipObjectId": "{{PlayerShipId}}",
            "spaceObjects": [
              { "objectId": "{{PlayerShipId}}", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary",
                "modules": [
                  { "moduleId": "{{EngineModuleId}}", "moduleTypeId": "module.engine.basic", "platformIndex": 0,
                    "occupiedCells": [0], "structurePoints": 100, "powerState": "On", "operationalState": "Ready",
                    "activeCycle": null, "cargo": [], "fuelAmountKg": 7500 }
                ]
              }
            ]
          }
        }
        """));

        // Verify initial state.
        var initialModule = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == PlayerShipId).Modules.Single();
        Assert.Equal(7500, initialModule.FuelAmountKg);

        // Save and reload into a fresh engine.
        var saveState = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed1);
        var loadedEngine = new SimulationEngine(registry);
        loadedEngine.LoadScenario(saveState);

        var restoredModule = loadedEngine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == PlayerShipId).Modules.Single();
        Assert.Equal(7500, restoredModule.FuelAmountKg);
    }

    [Fact]
    public void Non_engine_module_ignores_fuel_amount()
    {
        // Container modules don't have FuelCapacityKg → FuelAmountKg stays 0, no validation.
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
                    CargoCapacityKg: 100000)
            ],
            [],
            []);

        var engine = new SimulationEngine(registry);
        engine.LoadScenario(ScenarioLoader.LoadFromJson($$"""
        {
          "scenarioMetadata": { "scenarioId": "test", "name": "Test" },
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed0",
            "playerShipObjectId": "{{PlayerShipId}}",
            "spaceObjects": [
              { "objectId": "{{PlayerShipId}}", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary",
                "modules": [
                  { "moduleId": "MOD-CONTAINER", "moduleTypeId": "module.container.basic", "platformIndex": 0,
                    "occupiedCells": [0, 1, 2, 3], "structurePoints": 400, "powerState": "On", "operationalState": "Ready",
                    "activeCycle": null, "cargo": [] }
                ]
              }
            ]
          }
        }
        """));

        var module = engine.RuntimeObjects.Single().Modules.Single();
        Assert.Equal(0, module.FuelAmountKg);
    }

    private const string DefaultScenarioJson = """
    {
      "scenarioMetadata": { "scenarioId": "default", "name": "Default Scenario" },
      "gameState": {
        "gameTimeMs": 0, "currentSpeed": "Speed0",
        "playerShipObjectId": "SPC-0001",
        "spaceObjects": [
          { "objectId": "SPC-0001", "objectType": "PlayerShip", "persistenceType": "Permanent",
            "positionX": 10000, "positionY": 10000, "speedMps": 0, "directionDegrees": 0,
            "movementType": "Stationary" }
        ]
      }
    }
    """;

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
        int linearInertiaMps2 = 40000,
        int targetSpeedMps = 0,
        int targetDirectionDegrees = 0)
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
                "speedMps": {{targetSpeedMps}},
                "directionDegrees": {{targetDirectionDegrees}},
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
            ShipEngineCommandTypes.MatchTargetSpeed,
            ShipEngineCommandTypes.MatchTargetCourse,
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
                    LinearInertiaMps2: linearInertiaMps2,
                    BaseCycleTimeMs: 1000)
            ],
            [],
            commandIds.Select(id => new CommandDefinition(
                id,
                id,
                TimeFactor: id is ShipEngineCommandTypes.TurnLeftUntilCancel
                    or ShipEngineCommandTypes.TurnRightUntilCancel
                    ? 1000
                    : 0)));
    }
}
