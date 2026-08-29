using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// Tests for navigation.approach — trailing-pursuit navigation (execution contract
/// story-20260827-083137.md, U3: Engine command lifecycle). Registered under
/// module.engine.basic (game-architect decision #1), auto-repeating (Orbit-style)
/// but re-aiming fresh from the target's live state every completed cycle (never
/// locking a permanent course, unlike engine.orbit) via
/// <see cref="DeepSpaceSaga.Motion.ApproachPursuitMath"/>.
/// </summary>
public class ApproachCommandTests
{
    private const string PlayerShipId = "SHIP";
    private const string EngineModuleId = "ENGINE-1";
    private const string NavModuleId = "NAV-1";
    private const string TargetId = "TARGET";

    private static PlayerCommand ApproachCommand(string? targetObjectId = TargetId, string commandId = "cmd-1") =>
        new(commandId, 1, PlayerShipId, EngineModuleId, NavigationComputerCommandTypes.Approach,
            TargetObjectId: targetObjectId);

    private static ObjectMotionSnapshot PlayerShipFrom(AuthoritativeSnapshot snapshot) =>
        snapshot.Objects.Single(o => o.ObjectId == PlayerShipId);

    [Fact]
    public void Approach_without_target_is_rejected_with_missing_target()
    {
        var engine = CreateEngine();

        engine.ReceiveCommand(ApproachCommand(targetObjectId: null));
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(CommandReasonCodes.MissingTarget, result.ReasonCode);
        Assert.Null(PlayerShipFrom(snapshot).ActiveEngineCommandType);
    }

    [Fact]
    public void Approach_with_unknown_target_is_rejected()
    {
        var engine = CreateEngine();

        engine.ReceiveCommand(ApproachCommand(targetObjectId: "NO-SUCH-OBJECT"));
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(CommandReasonCodes.UnknownTarget, result.ReasonCode);
    }

    [Fact]
    public void Approach_bakes_the_initial_aim_point_and_target_state_immediately_on_start()
    {
        // Regression test for a real user-reported bug: before this fix, the ActiveCycle's
        // navigation fields (aim point, target speed/direction) were only baked when the
        // first ~1s cycle *completed* — so the snapshot captured right after the command
        // starts (gameTimeMs still 0, matching what a paused game would show forever,
        // since paused game time never advances and the first cycle never completes) had
        // ActiveEngineCommandType == navigation.approach but null navigation fields. The
        // client's LinearMotionPredictor.Predict Approach-branch guard requires all of
        // those fields non-null; when they were null it fell through to the generic
        // turn-step fallback, which just spins the ship by TurnStepDegrees every interval
        // forever — the exact "constant right turn instead of a trajectory" symptom
        // reported, and it never self-corrected while paused. This test proves the
        // baked fields are already correct on the very first snapshot, with zero elapsed
        // time — i.e., even a permanently paused game shows the right data immediately.
        var engine = CreateEngine(
            shipX: 8500, shipY: 20000, shipSpeedMps: 0, shipDirectionDegrees: 0,
            targetX: 10000, targetY: 10000, targetSpeedMps: 1000, targetDirectionDegrees: 90,
            trailDistanceKm: 150);

        engine.ReceiveCommand(ApproachCommand());
        var immediateSnapshot = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);
        var ship = PlayerShipFrom(immediateSnapshot);

        Assert.Equal(NavigationComputerCommandTypes.Approach, ship.ActiveEngineCommandType);
        Assert.NotNull(ship.NavigationTargetX);
        Assert.NotNull(ship.NavigationTargetY);
        Assert.Equal(10000, ship.NavigationTargetX!.Value, precision: 6);
        Assert.Equal(10000, ship.NavigationTargetY!.Value, precision: 6);
        Assert.NotNull(ship.NavigationTargetSpeedKmS);
        Assert.Equal(1.0, ship.NavigationTargetSpeedKmS!.Value, precision: 6);
        Assert.NotNull(ship.NavigationTargetDirectionDegrees);
        Assert.Equal(90, ship.NavigationTargetDirectionDegrees!.Value, precision: 6);
        Assert.Equal(ApproachPursuitMath.FlyThroughPendingPhase, ship.NavigationPhase);
        Assert.Equal(1500, ship.NavigationApproachTrailDistanceWorldUnits!.Value, precision: 6);
        Assert.True(ship.NavigationAngularInertiaDegPerSec > 0);
    }

    [Fact]
    public void Moving_target_is_flown_through_on_its_heading_without_changing_speed()
    {
        // Ship and target move right; the ship starts on the 10 km trailing point.
        // Reaching it must switch Approach to Final rather than completing and
        // synchronizing away from the selected object.
        var engine = CreateEngine(
            shipX: 9800, shipY: 10000, shipSpeedMps: 2000, shipDirectionDegrees: 90,
            targetX: 10000, targetY: 10000, targetSpeedMps: 1000, targetDirectionDegrees: 90,
            turnStepDegrees: 1, angularInertiaDegPerSec: 4, trailDistanceKm: 10);

        engine.ReceiveCommand(ApproachCommand());
        engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);

        var completed = PlayerShipFrom(
            engine.CaptureSnapshotForTests(15_000, SimulationSpeed.Speed1));
        Assert.Null(completed.ActiveEngineCommandType);
        Assert.Equal(2.0, completed.SpeedKmS, precision: 6);
        Assert.Equal(90, completed.Direction, precision: 6);
    }

    [Fact]
    public void Default_scenario_paused_at_start_bakes_current_asteroid_pose()
    {
        string scenarioPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "DeepSpaceSaga.Client", "Scenarios", "Default", "scenario.json"));
        string settingsPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "DeepSpaceSaga.Client", "Settings.json"));

        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(settingsPath, out _, out _);
        var engine = new SimulationEngine(registry);
        engine.LoadScenario(ScenarioLoader.LoadFromFile(scenarioPath));
        engine.ReceiveCommand(new PlayerCommand(
            "cmd-default-asteroid", 1, "SPC-0001", "MOD-PLAYER-ENGINE-01",
            NavigationComputerCommandTypes.Approach,
            TargetObjectId: "SPC-0003"));

        var snapshot = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0);
        var ship = snapshot.Objects.Single(o => o.ObjectId == "SPC-0001");
        var asteroid = snapshot.Objects.Single(o => o.ObjectId == "SPC-0003");

        // The planner receives the exact current target pose. It computes the rear
        // entry curve from ship turn radius after the first cycle boundary.
        const double expectedTrailDistance = 10;
        Assert.Equal(expectedTrailDistance,
            ship.NavigationApproachTrailDistanceWorldUnits!.Value, precision: 6);
        Assert.True(ship.NavigationApproachTrailDistanceWorldUnits < 400);

        Assert.Equal(asteroid.X, ship.NavigationTargetX!.Value, precision: 6);
        Assert.Equal(asteroid.Y, ship.NavigationTargetY!.Value, precision: 6);
        Assert.Equal(ApproachPursuitMath.FlyThroughPendingPhase, ship.NavigationPhase);
    }

    [Fact]
    public void Approach_holds_the_current_target_pose_instead_of_extrapolating_it()
    {
        // Target moves at a constant 1.0 km/s along direction 90 (+X). The ship starts
        // far south of the initial aim point with speed 0 (isolating pure steering from
        // any positional drift of the ship itself) and a direction misaligned by ~90°.
        // Approach now shares Orbit's MinTurnIntervalMs cadence (~250 ms at 4°/s —
        // Post-implementation bug fix #2, story-20260827-083137.md; superseded the
        // original ~1000 ms default cadence), so each 250 ms cycle turns by up to
        // turnStepDegrees=10 and must re-read the target's live (moved) position/speed/
        // direction and re-bake a fresh aim point, never a fixed point captured once
        // (unlike Orbit).
        var engine = CreateEngine(
            shipX: 8500, shipY: 20000, shipSpeedMps: 0, shipDirectionDegrees: 90,
            targetX: 10000, targetY: 10000, targetSpeedMps: 1000, targetDirectionDegrees: 90,
            turnStepDegrees: 10, trailDistanceKm: 150);

        engine.ReceiveCommand(ApproachCommand());
        engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1); // cycle #1 starts (StartedGameTimeMs = 0)

        var afterCycle1 = PlayerShipFrom(engine.CaptureSnapshotForTests(250, SimulationSpeed.Speed1));
        Assert.Equal(90, afterCycle1.Direction, precision: 3);
        Assert.Equal(1.0, afterCycle1.NavigationTargetSpeedKmS!.Value, precision: 6);
        Assert.Equal(90, afterCycle1.NavigationTargetDirectionDegrees!.Value, precision: 6);
        double aimX1 = afterCycle1.NavigationTargetX!.Value;

        Assert.StartsWith(ApproachPursuitMath.FlyThroughPhasePrefix, afterCycle1.NavigationPhase);
        var afterCycle2 = PlayerShipFrom(engine.CaptureSnapshotForTests(500, SimulationSpeed.Speed1));
        double aimX2 = afterCycle2.NavigationTargetX!.Value;

        var afterCycle3 = PlayerShipFrom(engine.CaptureSnapshotForTests(750, SimulationSpeed.Speed1));
        double aimX3 = afterCycle3.NavigationTargetX!.Value;

        Assert.Equal(aimX1, aimX2, precision: 6);
        Assert.Equal(aimX1, aimX3, precision: 6);

        // Still cycling — not completed.
        Assert.Equal(NavigationComputerCommandTypes.Approach, afterCycle3.ActiveEngineCommandType);
    }

    [Fact]
    public void Approach_arrives_despite_a_badly_misaligned_initial_heading()
    {
        // Regression for the user-reported "wide ever-widening arc, exits the screen,
        // never closes back toward it" bug (story-20260827-083137.md, Post-implementation
        // bug fix #2). Unlike the other tests here (which mostly start well-aligned or
        // exactly at the aim point), this ship starts with a ~180° initial heading error
        // AND is moving, at a distance from the aim point (~1000 world units) deliberately
        // close relative to the ship's own turn radius (speed / angular velocity ≈ 430 wu
        // at the fixed ~250 ms cadence) — the tight-turn-radius-vs-distance case that most
        // stresses convergence — the true end-to-end regression for the combined cadence
        // recalibration + course-locking fix. Realistic module constants
        // (turnStepDegrees=1, angularInertiaDegPerSec=4, matching module.engine.basic)
        // give the fixed ~250 ms cadence (MinTurnIntervalMs(4)).
        var engine = CreateEngine(
            shipX: 7939, shipY: 11061, shipSpeedMps: 3000, shipDirectionDegrees: 270,
            targetX: 10000, targetY: 10000, targetSpeedMps: 0, targetDirectionDegrees: 45,
            turnStepDegrees: 1, angularInertiaDegPerSec: 4, trailDistanceKm: 150);

        engine.ReceiveCommand(ApproachCommand());
        engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1); // cycle #1 starts

        // Generous but bounded budget: at 3 km/s, a straight-line trip over the ~1000
        // world unit distance alone would take ~33 simulated seconds; even a wide
        // pursuit loop around it (which the fix must avoid needing) stays well inside
        // this 200s budget.
        ObjectMotionSnapshot? ship = null;
        AuthoritativeSnapshot? snapshot = null;
        for (long gameTimeMs = 250; gameTimeMs <= 200_000; gameTimeMs += 250)
        {
            snapshot = engine.CaptureSnapshotForTests(gameTimeMs, SimulationSpeed.Speed1);
            ship = PlayerShipFrom(snapshot);
            if (ship.ActiveEngineCommandType is null)
                break;
        }

        Assert.NotNull(ship);

        // Cycle completed — the ship actually arrived and stopped auto-repeating,
        // instead of still circling/diverging after a generous time budget. Every
        // completed intermediate ~250 ms auto-repeat cycle also emits its own
        // CommandCompleted (pre-existing auto-repeat pipeline behavior, shared by
        // Orbit — out of this fix's scope), so only the LAST one — the actual
        // arrival — is asserted here, not the count.
        Assert.Null(ship.ActiveEngineCommandType);
        Assert.Equal(45, ship.Direction, precision: 6); // exact scalar match with the target
        Assert.Equal(3, ship.SpeedKmS, precision: 6);

        var completed = snapshot!.ShipEvents.Last(e => e.EventType == ShipEventTypes.CommandCompleted);
        Assert.Null(completed.ReasonCode);
    }

    [Fact]
    public void Approach_completes_with_direction_match_and_stops_repeating_without_changing_speed()
    {
        // Ship starts exactly at the (stationary) target's own position — the aim
        // point for a genuinely stationary target (Post-implementation bug fix #3,
        // story-20260827-083137.md) — and arrives on the very first completed cycle.
        var engine = CreateEngine(
            shipX: 10000, shipY: 10000, shipSpeedMps: 0, shipDirectionDegrees: 0,
            targetX: 10000, targetY: 10000, targetSpeedMps: 0, targetDirectionDegrees: 45,
            trailDistanceKm: 150);

        engine.ReceiveCommand(ApproachCommand());
        engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1); // cycle starts (StartedGameTimeMs = 0)
        var snapshot = engine.CaptureSnapshotForTests(1000, SimulationSpeed.Speed1);

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Executed, result.Status);
        Assert.Null(result.ReasonCode);

        var completed = Assert.Single(snapshot.ShipEvents, e => e.EventType == ShipEventTypes.CommandCompleted);
        Assert.Null(completed.ReasonCode);

        var ship = PlayerShipFrom(snapshot);
        Assert.Equal(45, ship.Direction, precision: 9);
        Assert.Equal(0, ship.SpeedKmS, precision: 9);
        Assert.Null(ship.ActiveEngineCommandType); // cycle stopped — no auto-repeat continues

        // No further changes on a later snapshot — the cycle really stopped.
        var later = PlayerShipFrom(engine.CaptureSnapshotForTests(5000, SimulationSpeed.Speed1));
        Assert.Equal(45, later.Direction, precision: 9);
        Assert.Equal(0, later.SpeedKmS, precision: 9);
        Assert.Null(later.ActiveEngineCommandType);
    }

    [Fact]
    public void Stationary_target_produces_an_aim_point_at_its_own_position_and_completes()
    {
        // Same as the completion test above, but explicitly named/asserted per the
        // zero-speed-target fallback (Phase 2b decision #6). Post-implementation bug
        // fix #3 (story-20260827-083137.md) corrected the original reasoning here: a
        // Stationary Station (speed 0) no longer yields a Direction-based OFFSET aim
        // point (its Direction field is an arbitrary placeholder with no physical
        // meaning for something that isn't moving) — instead the aim point is simply
        // the station's own position, so the ship starting there still completes.
        var engine = CreateEngine(
            shipX: 5000, shipY: 5000, shipSpeedMps: 0, shipDirectionDegrees: 0,
            targetX: 5000, targetY: 5000, targetSpeedMps: 0, targetDirectionDegrees: 180,
            targetObjectType: "Station",
            trailDistanceKm: 150);

        engine.ReceiveCommand(ApproachCommand());
        engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1); // cycle starts (StartedGameTimeMs = 0)
        var snapshot = engine.CaptureSnapshotForTests(1000, SimulationSpeed.Speed1);

        Assert.Equal(CommandResultStatus.Executed, Assert.Single(snapshot.CommandResults).Status);
        var ship = PlayerShipFrom(snapshot);
        Assert.Equal(180, ship.Direction, precision: 9);
        Assert.Equal(0, ship.SpeedKmS, precision: 9);
    }

    [Fact]
    public void Approach_to_a_stationary_station_arrives_at_the_station_not_past_it()
    {
        // Regression for the user-reported "long straight line passes well past the
        // visible station marker without ever arriving/converging there" bug
        // (Post-implementation bug fix #3, story-20260827-083137.md), reproduced with
        // real scenario data: the Default scenario's Station SPC-0002 has
        // directionDegrees: 0 — an arbitrary placeholder with no physical meaning
        // (the station isn't "facing" anywhere). Under the old (buggy) behavior, the
        // ship would aim for a point trailDistanceKm (150 km / 1500 world units) away
        // from the station along that meaningless direction — here, since
        // directionDegrees is 0 (=up=-Y), that wrong aim point would sit at
        // (10000, 11500), 1500 world units south of the actual station at
        // (10000, 10000). This ship starts with a badly misaligned heading relative to
        // the station, matching the "issues Approach against a nearby object with a
        // heading that isn't already aimed at it" real-play scenario.
        var engine = CreateEngine(
            shipX: 7939, shipY: 11061, shipSpeedMps: 3000, shipDirectionDegrees: 270,
            targetX: 10000, targetY: 10000, targetSpeedMps: 0, targetDirectionDegrees: 0,
            targetObjectType: "Station",
            turnStepDegrees: 1, angularInertiaDegPerSec: 4, trailDistanceKm: 150);

        engine.ReceiveCommand(ApproachCommand());
        engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1); // cycle #1 starts

        ObjectMotionSnapshot? ship = null;
        for (long gameTimeMs = 250; gameTimeMs <= 200_000; gameTimeMs += 250)
        {
            ship = PlayerShipFrom(engine.CaptureSnapshotForTests(gameTimeMs, SimulationSpeed.Speed1));
            if (ship.ActiveEngineCommandType is null)
                break;
        }

        Assert.NotNull(ship);

        // Cycle completed — the ship actually arrived, not still flying past.
        Assert.Null(ship!.ActiveEngineCommandType);
        Assert.Equal(0, ship.Direction, precision: 6); // exact course match with the station
        Assert.Equal(3, ship.SpeedKmS, precision: 6); // Approach never brakes

        double distanceFromStation = Math.Sqrt(
            (ship.X - 10000) * (ship.X - 10000) + (ship.Y - 10000) * (ship.Y - 10000));
        double distanceFromOldWrongAimPoint = Math.Sqrt(
            (ship.X - 10000) * (ship.X - 10000) + (ship.Y - 11500) * (ship.Y - 11500));

        Assert.True(distanceFromStation <= 10.0,
            $"Ship ended {distanceFromStation:F1} world units from the station at " +
            $"({ship.X:F1},{ship.Y:F1}) — expected to arrive at the station itself.");
        Assert.True(distanceFromOldWrongAimPoint > 1000.0,
            "Ship ended up near the OLD (buggy) direction-offset aim point, not the station.");
    }

    [Fact]
    public void Other_engine_command_cancels_active_approach_cycle()
    {
        var engine = CreateEngine(
            shipX: 0, shipY: 20000, shipSpeedMps: 0, shipDirectionDegrees: 0,
            targetX: 10000, targetY: 10000, targetSpeedMps: 0, targetDirectionDegrees: 0);

        engine.ReceiveCommand(ApproachCommand());
        engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);
        var running = PlayerShipFrom(engine.CaptureSnapshotForTests(500, SimulationSpeed.Speed1));
        Assert.Equal(NavigationComputerCommandTypes.Approach, running.ActiveEngineCommandType);

        engine.ReceiveCommand(new PlayerCommand("cmd-accel", 2, PlayerShipId, EngineModuleId, ShipEngineCommandTypes.Accelerate));
        var snapshot = engine.CaptureSnapshotForTests(600, SimulationSpeed.Speed1);

        var cancelled = snapshot.CommandResults.Single(r => r.CommandId == "cmd-1");
        Assert.Equal(CommandResultStatus.Cancelled, cancelled.Status);
        Assert.Equal(ShipEventReasonCodes.CancelledByCommand, cancelled.ReasonCode);
        Assert.Equal(ShipEngineCommandTypes.Accelerate, PlayerShipFrom(snapshot).ActiveEngineCommandType);
    }

    [Fact]
    public void Resending_approach_always_cancels_and_restarts_even_for_the_same_target()
    {
        // Deliberate THIRD branch, distinct from SpeedSync/DirectionSync (always
        // idempotent) and Orbit (idempotent only same-target): a re-sent Approach
        // ALWAYS cancels-and-restarts, even against the identical target.
        var engine = CreateEngine(
            shipX: 0, shipY: 20000, shipSpeedMps: 0, shipDirectionDegrees: 0,
            targetX: 10000, targetY: 10000, targetSpeedMps: 0, targetDirectionDegrees: 0);

        engine.ReceiveCommand(ApproachCommand(commandId: "cmd-first"));
        engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);
        var firstCycleId = engine.RuntimeObjects
            .Single(o => o.InitialMotion.ObjectId == PlayerShipId).Modules
            .Single(m => m.ModuleId == EngineModuleId).ActiveCycle?.CycleId;

        engine.ReceiveCommand(ApproachCommand(commandId: "cmd-second")); // same target
        var snapshot = engine.CaptureSnapshotForTests(500, SimulationSpeed.Speed1);

        var cancelledFirst = snapshot.CommandResults.Single(r => r.CommandId == "cmd-first");
        Assert.Equal(CommandResultStatus.Cancelled, cancelledFirst.Status);
        Assert.Equal(ShipEventReasonCodes.CancelledByCommand, cancelledFirst.ReasonCode);

        var secondCycleId = engine.RuntimeObjects
            .Single(o => o.InitialMotion.ObjectId == PlayerShipId).Modules
            .Single(m => m.ModuleId == EngineModuleId).ActiveCycle?.CycleId;
        Assert.NotEqual(firstCycleId, secondCycleId);
        Assert.NotNull(secondCycleId);
    }

    [Fact]
    public void Approach_target_that_disappears_mid_cycle_cancels_with_unknown_target()
    {
        var engine = CreateEngine(
            shipX: 0, shipY: 20000, shipSpeedMps: 0, shipDirectionDegrees: 0,
            targetX: 10000, targetY: 10000, targetSpeedMps: 0, targetDirectionDegrees: 0);

        engine.ReceiveCommand(ApproachCommand());
        engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);

        engine.RemoveObjectForTests(TargetId);

        var snapshot = engine.CaptureSnapshotForTests(1000, SimulationSpeed.Speed1);

        var cancelled = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Cancelled, cancelled.Status);
        Assert.Equal(CommandReasonCodes.UnknownTarget, cancelled.ReasonCode);

        var interrupted = Assert.Single(snapshot.ShipEvents, e => e.EventType == ShipEventTypes.CycleInterrupted);
        Assert.Equal(CommandReasonCodes.UnknownTarget, interrupted.ReasonCode);
        Assert.Null(PlayerShipFrom(snapshot).ActiveEngineCommandType);
    }

    [Fact]
    public void Approach_cycle_does_not_deduct_fuel()
    {
        var engine = CreateEngine(
            shipX: 0, shipY: 20000, shipSpeedMps: 0, shipDirectionDegrees: 0,
            targetX: 10000, targetY: 10000, targetSpeedMps: 0, targetDirectionDegrees: 0,
            fuelAmountKg: 500);

        engine.ReceiveCommand(ApproachCommand());
        engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);
        engine.CaptureSnapshotForTests(1000, SimulationSpeed.Speed1);
        engine.CaptureSnapshotForTests(2000, SimulationSpeed.Speed1);

        var module = engine.RuntimeObjects
            .Single(o => o.InitialMotion.ObjectId == PlayerShipId).Modules
            .Single(m => m.ModuleId == EngineModuleId);
        Assert.Equal(500, module.FuelAmountKg);
    }

    [Fact]
    public void Approach_completion_against_a_station_lets_dock_succeed_immediately_after()
    {
        // End-to-end: Approach completes direction-matched to a Station within
        // Dock's <200km range — proves Dock's strict ~1e-6 epsilon check passes right
        // away, no manual sync command needed. Ship starts at the station's own
        // position — the aim point for a genuinely stationary target
        // (Post-implementation bug fix #3, story-20260827-083137.md) — so this also
        // doubles as a proof that Dock's own <200km range check trivially passes,
        // since the ship never has to travel a meaningful distance at all.
        var engine = CreateEngine(
            shipX: 10000, shipY: 10000, shipSpeedMps: 0, shipDirectionDegrees: 0,
            targetX: 10000, targetY: 10000, targetSpeedMps: 0, targetDirectionDegrees: 45,
            targetObjectType: "Station",
            trailDistanceKm: 150,
            includeNavigationComputer: true);

        engine.ReceiveCommand(ApproachCommand());
        engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1); // cycle starts (StartedGameTimeMs = 0)
        var afterApproach = engine.CaptureSnapshotForTests(1000, SimulationSpeed.Speed1);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(afterApproach.CommandResults).Status);
        Assert.Null(PlayerShipFrom(afterApproach).ActiveEngineCommandType);

        engine.ReceiveCommand(new PlayerCommand(
            "cmd-dock", 2, PlayerShipId, NavModuleId, NavigationComputerCommandTypes.Dock,
            TargetObjectId: TargetId));
        var afterDock = engine.CaptureSnapshotForTests(1000, SimulationSpeed.Speed1);

        var dockResult = afterDock.CommandResults.Single(r => r.CommandId == "cmd-dock");
        Assert.Equal(CommandResultStatus.Executed, dockResult.Status);
        Assert.True(PlayerShipFrom(afterDock).IsDocked);
        Assert.Equal(TargetId, PlayerShipFrom(afterDock).DockedStationObjectId);
    }

    /// <summary>
    /// Same trailing-aim-point geometry as <see cref="DeepSpaceSaga.Motion.ApproachPursuitMath.ComputeAimPoint"/>,
    /// duplicated here (rather than referencing it) so the test's expected value is
    /// computed independently of the production formula. Only valid for a genuinely
    /// MOVING target (non-zero speed): since Post-implementation bug fix #3
    /// (story-20260827-083137.md), a genuinely stationary (speed == 0) target's aim
    /// point is simply its own position, not this offset formula — do not call this
    /// helper for a speed == 0 target.
    /// </summary>
    private static (double X, double Y) AimPointBehindStationary(
        double targetX, double targetY, double targetDirectionDegrees, double trailDistanceKm)
    {
        double trailDistanceWorldUnits = trailDistanceKm * 10.0;
        double angleRad = targetDirectionDegrees * Math.PI / 180.0;
        double forwardX = Math.Sin(angleRad);
        double forwardY = -Math.Cos(angleRad);
        return (targetX - trailDistanceWorldUnits * forwardX, targetY - trailDistanceWorldUnits * forwardY);
    }

    private static SimulationEngine CreateEngine(
        double shipX = 0,
        double shipY = 0,
        int shipSpeedMps = 0,
        int shipDirectionDegrees = 0,
        double targetX = 10000,
        double targetY = 10000,
        int targetSpeedMps = 0,
        int targetDirectionDegrees = 0,
        string targetObjectType = "PlayerShip",
        int turnStepDegrees = 10,
        int angularInertiaDegPerSec = 4,
        int trailDistanceKm = 150,
        long fuelAmountKg = 0,
        bool includeNavigationComputer = false)
    {
        var engine = new SimulationEngine(CreateRegistry(turnStepDegrees, angularInertiaDegPerSec, trailDistanceKm));

        string navModuleJson = includeNavigationComputer
            ? $$"""
                ,
                {
                  "moduleId": "{{NavModuleId}}",
                  "moduleTypeId": "module.bridge.navigation.computer.basic",
                  "occupiedCells": [ {"x":1,"y":0} ],
                  "structurePoints": 80,
                  "powerState": "On",
                  "operationalState": "Ready",
                  "activeCycle": null,
                  "cargo": []
                }
                """
            : "";
        string hullLayoutJson = includeNavigationComputer
            ? """{ "width": 2, "height": 1, "cells": [ {"x":0,"y":0}, {"x":1,"y":0} ] }"""
            : """{ "width": 1, "height": 1, "cells": [ {"x":0,"y":0} ] }""";

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
                "positionX": {{shipX}},
                "positionY": {{shipY}},
                "speedMps": {{shipSpeedMps}},
                "directionDegrees": {{shipDirectionDegrees}},
                "movementType": "{{(shipSpeedMps > 0 ? "Linear" : "Stationary")}}",
                "hullLayout": {{hullLayoutJson}},
                "modules": [
                  {
                    "moduleId": "{{EngineModuleId}}",
                    "moduleTypeId": "module.engine.basic",
                    "occupiedCells": [ {"x":0,"y":0} ],
                    "structurePoints": 100,
                    "powerState": "On",
                    "operationalState": "Ready",
                    "activeCycle": null,
                    "fuelAmountKg": {{fuelAmountKg}},
                    "cargo": []
                  }
                  {{navModuleJson}}
                ]
              },
              {
                "objectId": "{{TargetId}}",
                "objectType": "{{targetObjectType}}",
                "persistenceType": "Permanent",
                "positionX": {{targetX}},
                "positionY": {{targetY}},
                "speedMps": {{targetSpeedMps}},
                "directionDegrees": {{targetDirectionDegrees}},
                "movementType": "{{(targetSpeedMps > 0 ? "Linear" : "Stationary")}}"
              }
            ]
          }
        }
        """));

        return engine;
    }

    private static GameDataRegistry CreateRegistry(int turnStepDegrees, int angularInertiaDegPerSec, int trailDistanceKm)
    {
        string[] engineCommandIds =
        [
            ShipEngineCommandTypes.Accelerate,
            ShipEngineCommandTypes.Brake,
            ShipEngineCommandTypes.CancelAll,
            NavigationComputerCommandTypes.Approach
        ];
        string[] navCommandIds = [NavigationComputerCommandTypes.Dock];

        return GameDataRegistry.Create(
            [
                new ModuleCategoryDefinition(
                    "module.engine.basic", "Engine", SlotSize: 1,
                    CommandTypeIds: engineCommandIds.ToImmutableArray()),
                new ModuleCategoryDefinition(
                    "module.bridge.navigation.computer", "Navigation Computer", SlotSize: 1,
                    CommandTypeIds: navCommandIds.ToImmutableArray())
            ],
            [
                new ModuleTypeDefinition(
                    "module.engine.basic",
                    "Engine",
                    SlotSize: 1,
                    MassKg: 5000,
                    StructurePointsMax: 100,
                    PowerConsumptionW: 0,
                    CommandTypeIds: engineCommandIds.ToImmutableArray(),
                    CargoCapacityKg: null,
                    MaxSpeedMps: 4000,
                    TurnStepDegrees: turnStepDegrees,
                    LinearInertiaMps2: 40000,
                    AngularInertiaDegPerSec: angularInertiaDegPerSec,
                    BaseCycleTimeMs: 1000,
                    FuelCapacityKg: 1000),
                new ModuleTypeDefinition(
                    "module.bridge.navigation.computer.basic",
                    "Bridge Navigation Computer",
                    SlotSize: 1,
                    MassKg: 4000,
                    StructurePointsMax: 80,
                    PowerConsumptionW: 0,
                    CommandTypeIds: navCommandIds.ToImmutableArray(),
                    BaseCycleTimeMs: 1000)
            ],
            [],
            [
                new CommandDefinition(
                    ShipEngineCommandTypes.Accelerate, "Accelerate", Type: "module.engine.basic"),
                new CommandDefinition(
                    ShipEngineCommandTypes.Brake, "Brake", Type: "module.engine.basic"),
                new CommandDefinition(
                    ShipEngineCommandTypes.CancelAll, "Cancel All", Type: "module.engine.basic"),
                new CommandDefinition(
                    NavigationComputerCommandTypes.Approach, "Approach",
                    TimeFactor: 1000, Target: "object", Type: "module.engine.basic",
                    TrailDistanceKm: trailDistanceKm),
                new CommandDefinition(
                    NavigationComputerCommandTypes.Dock, "Dock",
                    TimeFactor: 2000, Target: "object", Type: "module.bridge.navigation.computer",
                    RangeKm: 200)
            ]);
    }
}
