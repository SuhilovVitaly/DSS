using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Tests;

public class ApproachCommandTests
{
    private const string PlayerShipId = "SHIP";
    private const string EngineModuleId = "ENGINE-1";
    private const string NavModuleId = "NAV-1";
    private const string TargetId = "TARGET";
    private static PlayerCommand ApproachCommand(string? targetObjectId = TargetId, string commandId = "cmd-1") =>
        new(commandId, 1, PlayerShipId, EngineModuleId, NavigationComputerCommandTypes.Approach, TargetObjectId: targetObjectId);
    private static ObjectMotionSnapshot PlayerShipFrom(AuthoritativeSnapshot snapshot) =>
        snapshot.Objects.Single(o => o.ObjectId == PlayerShipId);

    [Theory]
    [InlineData(null, CommandReasonCodes.MissingTarget)]
    [InlineData("missing", CommandReasonCodes.UnknownTarget)]
    public void Invalid_target_is_rejected(string? targetId, string reason)
    {
        var engine = CreateEngine(shipSpeedMps: 1000);
        engine.ReceiveCommand(ApproachCommand(targetId));
        var snapshot = engine.CaptureSnapshotForTests();
        Assert.Equal(reason, Assert.Single(snapshot.CommandResults).ReasonCode);
        Assert.Null(PlayerShipFrom(snapshot).ActiveEngineCommandType);
    }

    [Fact]
    public void Stationary_ship_is_rejected_instead_of_accelerating_or_waiting_forever()
    {
        var engine = CreateEngine();
        engine.ReceiveCommand(ApproachCommand());
        var snapshot = engine.CaptureSnapshotForTests();
        Assert.Equal(CommandReasonCodes.NavigationRequiresMotion, Assert.Single(snapshot.CommandResults).ReasonCode);
        Assert.Null(PlayerShipFrom(snapshot).ActiveEngineCommandType);
        Assert.Equal(0, PlayerShipFrom(snapshot).SpeedKmS);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    [InlineData(3000)]
    [InlineData(5000)]
    public void Route_finishes_behind_live_target_and_never_changes_speed(int targetSpeed)
    {
        var engine = CreateEngine(shipX: 9000, shipY: 10500, shipSpeedMps: 3000, shipDirectionDegrees: 270,
            targetX: 10000, targetY: 10000, targetSpeedMps: targetSpeed, targetDirectionDegrees: 90,
            turnStepDegrees: 1, angularInertiaDegPerSec: 4, trailDistanceKm: 1);
        engine.ReceiveCommand(ApproachCommand());
        var first = PlayerShipFrom(engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0));
        var route = Assert.IsType<ApproachRoute>(first.ApproachRoute);
        Assert.Equal(TargetId, first.NavigationTargetObjectId);
        Assert.Equal(ApproachLineCaptureMath.Phase, first.NavigationPhase);
        // Planning is ready in the first paused snapshot. No per-cycle replanning.
        var predictor = new LinearMotionPredictor();
        long end = (long)Math.Ceiling(route.DurationMs);
        ObjectMotionSnapshot previous = first;
        for (long t = 137; t < end; t += 137)
        {
            var current = PlayerShipFrom(engine.CaptureSnapshotForTests(t, SimulationSpeed.Speed1));
            var expected = predictor.Predict(first, t);
            Assert.Equal(3, current.SpeedKmS);
            Assert.Equal(expected.X, current.X, 7);
            Assert.Equal(expected.Y, current.Y, 7);
            Assert.Equal(expected.Direction, current.Direction, 7);
            Assert.InRange(Math.Abs(ApproachLineCaptureMath.Delta(previous.Direction, current.Direction)), 0, .5480001);
            Assert.Equal(route.X, current.ApproachRoute!.X);
            previous = current;
        }
        var snapshot = engine.CaptureSnapshotForTests(end, SimulationSpeed.Speed1);
        var ship = PlayerShipFrom(snapshot);
        var target = snapshot.Objects.Single(o => o.ObjectId == TargetId);
        Assert.Null(ship.ActiveEngineCommandType);
        Assert.Null(ship.NavigationTargetObjectId);
        Assert.Equal(3, ship.SpeedKmS);
        Assert.InRange(Math.Abs(ApproachLineCaptureMath.Delta(ship.Direction, 90)), 0, 1e-7);
        Assert.InRange(Math.Abs(ship.Y - target.Y), 0, .0001);
        Assert.True(ship.X < target.X);
        Assert.DoesNotContain(snapshot.CommandResults, r => r.Status == CommandResultStatus.Cancelled);
    }

    [Fact]
    public void Already_trailing_ship_reaches_captured_destination_without_a_loop()
    {
        var engine = CreateEngine(shipX: 9800, shipY: 10000, shipSpeedMps: 2000, shipDirectionDegrees: 90,
            targetX: 10000, targetY: 10000, targetSpeedMps: 3000, targetDirectionDegrees: 90, trailDistanceKm: 1);
        engine.ReceiveCommand(ApproachCommand());
        var snapshot = engine.CaptureSnapshotForTests();
        var route = Assert.IsType<ApproachRoute>(PlayerShipFrom(snapshot).ApproachRoute);
        Assert.Equal(NavigationComputerCommandTypes.Approach, PlayerShipFrom(snapshot).ActiveEngineCommandType);
        Assert.Equal(190, route.Length, 6);
        Assert.Equal(0, route.First, 6);
        Assert.Equal(0, route.Third, 6);
        Assert.Empty(snapshot.CommandResults);
        Assert.Equal(9800, PlayerShipFrom(snapshot).X);
        Assert.Equal(2, PlayerShipFrom(snapshot).SpeedKmS);
        var final = engine.CaptureSnapshotForTests((long)route.DurationMs, SimulationSpeed.Speed1);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(final.CommandResults).Status);
        var completed = PlayerShipFrom(final);
        Assert.Null(completed.ActiveEngineCommandType);
        Assert.Equal(9990, completed.X, 6);
        Assert.Equal(10000, completed.Y, 6);
    }

    [Fact]
    public void Cancel_between_cycle_boundaries_preserves_position_heading_and_speed()
    {
        var engine = CreateEngine(shipSpeedMps: 2000);
        engine.ReceiveCommand(ApproachCommand());
        var origin = PlayerShipFrom(engine.CaptureSnapshotForTests());
        var expected = new LinearMotionPredictor().Predict(origin, 137);
        engine.ReceiveCommand(new PlayerCommand("cancel", 2, PlayerShipId, EngineModuleId, ShipEngineCommandTypes.CancelAll));
        var snapshot = engine.CaptureSnapshotForTests(137, SimulationSpeed.Speed1);
        var actual = PlayerShipFrom(snapshot);
        Assert.Equal(expected.X, actual.X, 8);
        Assert.Equal(expected.Y, actual.Y, 8);
        Assert.Equal(expected.Direction, actual.Direction, 8);
        Assert.Equal(2, actual.SpeedKmS);
        Assert.Null(actual.ActiveEngineCommandType);
        var later = PlayerShipFrom(engine.CaptureSnapshotForTests(1137, SimulationSpeed.Speed1));
        Assert.Equal(actual.Direction, later.Direction);
        Assert.Equal(2, later.SpeedKmS);
    }

    [Fact]
    public void Resend_restarts_from_the_actual_pose_without_a_jump()
    {
        var engine = CreateEngine(shipSpeedMps: 2000);
        engine.ReceiveCommand(ApproachCommand());
        var first = PlayerShipFrom(engine.CaptureSnapshotForTests());
        var expected = new LinearMotionPredictor().Predict(first, 137);
        engine.ReceiveCommand(ApproachCommand(commandId: "second"));
        var snapshot = engine.CaptureSnapshotForTests(137, SimulationSpeed.Speed1);
        var actual = PlayerShipFrom(snapshot);
        Assert.Equal(expected.X, actual.X, 8);
        Assert.Equal(expected.Y, actual.Y, 8);
        Assert.Equal(expected.Direction, actual.Direction, 8);
        Assert.Equal(0, actual.ApproachRoute!.ElapsedMs);
        Assert.Contains(snapshot.CommandResults, r => r.CommandId == "cmd-1" && r.Status == CommandResultStatus.Cancelled);
    }

    [Fact]
    public void Target_disappearance_cancels_the_command()
    {
        var engine = CreateEngine(shipSpeedMps: 2000);
        engine.ReceiveCommand(ApproachCommand());
        engine.CaptureSnapshotForTests();
        engine.RemoveObjectForTests(TargetId);
        var snapshot = engine.CaptureSnapshotForTests(1000, SimulationSpeed.Speed1);
        Assert.Equal(CommandReasonCodes.UnknownTarget, Assert.Single(snapshot.CommandResults).ReasonCode);
        Assert.Null(PlayerShipFrom(snapshot).ActiveEngineCommandType);
    }

    [Fact]
    public void Save_between_boundaries_resumes_exactly_the_same_route()
    {
        var engine = CreateEngine(shipSpeedMps: 2000);
        engine.ReceiveCommand(ApproachCommand());
        engine.CaptureSnapshotForTests();
        var save = engine.CaptureSaveStateForTests(137, SimulationSpeed.Speed1);
        var restored = CreateEngine(shipSpeedMps: 2000);
        restored.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), allowNonZeroGameTime: true));
        foreach (long t in new long[] { 137, 251, 1111, 10000, 1000000 })
        {
            var expected = PlayerShipFrom(engine.CaptureSnapshotForTests(t, SimulationSpeed.Speed1));
            var actual = PlayerShipFrom(restored.CaptureSnapshotForTests(t, SimulationSpeed.Speed1));
            Assert.Equal(expected.X, actual.X, 7);
            Assert.Equal(expected.Y, actual.Y, 7);
            Assert.Equal(expected.Direction, actual.Direction, 7);
            Assert.Equal(expected.SpeedKmS, actual.SpeedKmS);
            Assert.Equal(expected.ActiveEngineCommandType, actual.ActiveEngineCommandType);
        }
    }

    [Fact]
    public void Approach_does_not_deduct_fuel()
    {
        var engine = CreateEngine(shipSpeedMps: 2000, fuelAmountKg: 500);
        engine.ReceiveCommand(ApproachCommand());
        engine.CaptureSnapshotForTests();
        engine.CaptureSnapshotForTests(10000, SimulationSpeed.Speed1);
        Assert.Equal(500, engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == PlayerShipId)
            .Modules.Single(m => m.ModuleId == EngineModuleId).FuelAmountKg);
    }

    [Fact]
    public void Target_turn_replans_from_the_current_pose_and_preserves_speed()
    {
        var engine = CreateEngine(shipSpeedMps: 2000, targetSpeedMps: 1000, targetDirectionDegrees: 90, trailDistanceKm: 1);
        engine.ReceiveCommand(ApproachCommand());
        var first = PlayerShipFrom(engine.CaptureSnapshotForTests());
        var expected = new LinearMotionPredictor().Predict(first, 250);
        var save = engine.CaptureSaveStateForTests(137, SimulationSpeed.Speed1);
        var changed = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o =>
                    o.ObjectId == TargetId ? o with { DirectionDegrees = 180 } : o).ToArray()
            }
        };
        var restored = CreateEngine(shipSpeedMps: 2000);
        restored.LoadScenario(changed);
        var replanned = PlayerShipFrom(restored.CaptureSnapshotForTests(250, SimulationSpeed.Speed1));
        Assert.Equal(expected.X, replanned.X, 7);
        Assert.Equal(expected.Y, replanned.Y, 7);
        Assert.Equal(expected.Direction, replanned.Direction, 7);
        Assert.Equal(180, replanned.ApproachRoute!.TargetDirection);
        Assert.Equal(0, replanned.ApproachRoute.ElapsedMs);
        var final = restored.CaptureSnapshotForTests(250 + (long)Math.Ceiling(replanned.ApproachRoute.DurationMs), SimulationSpeed.Speed1);
        var ship = PlayerShipFrom(final);
        var target = final.Objects.Single(o => o.ObjectId == TargetId);
        Assert.Null(ship.ActiveEngineCommandType);
        Assert.Equal(2, ship.SpeedKmS);
        Assert.Equal(180, ship.Direction);
        Assert.InRange(Math.Abs(ship.X - target.X), 0, .001);
        Assert.InRange(ship.Y - target.Y, -10.05, -9.95);
    }

    [Fact]
    public void Legacy_saved_cycle_migrates_to_a_route_on_its_next_boundary()
    {
        var engine = CreateEngine(shipSpeedMps: 2000);
        engine.ReceiveCommand(ApproachCommand());
        engine.CaptureSnapshotForTests();
        var save = engine.CaptureSaveStateForTests(137, SimulationSpeed.Speed1);
        var legacy = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId != PlayerShipId ? o : o with
                {
                    Modules = o.Modules!.Select(m => m.ActiveCycle is null ? m : m with
                    {
                        ActiveCycle = m.ActiveCycle with { ApproachRoute = null, NavigationPhase = ApproachPursuitMath.FinalPhase }
                    }).ToArray()
                }).ToArray()
            }
        };
        var restored = CreateEngine(shipSpeedMps: 2000);
        restored.LoadScenario(legacy);
        var ship = PlayerShipFrom(restored.CaptureSnapshotForTests(250, SimulationSpeed.Speed1));
        Assert.NotNull(ship.ApproachRoute);
        Assert.Equal(ApproachLineCaptureMath.Phase, ship.NavigationPhase);
        Assert.Equal(2, ship.SpeedKmS);
    }
    [Theory]
    [InlineData(1069, 57)]
    [InlineData(1911, 110)]
    public void Speed100_large_ticks_match_speed1_fine_ticks_prediction_and_save_load(int targetSpeed, int targetHeading)
    {
        long normalRealMs = 0, fastRealMs = 0, loadedRealMs = 0;
        SimulationEngine Create(SimulationClock clock) => CreateEngine(shipX: 10000, shipY: 5500,
            shipSpeedMps: 700, shipDirectionDegrees: 0, targetX: 16000, targetY: 9000,
            targetSpeedMps: targetSpeed, targetDirectionDegrees: targetHeading,
            turnStepDegrees: 1, trailDistanceKm: 1, clock: clock);
        using var normal = Create(new SimulationClock(SimulationSpeed.Speed0, () => normalRealMs));
        using var fast = Create(new SimulationClock(SimulationSpeed.Speed0, () => fastRealMs));
        using var loaded = Create(new SimulationClock(SimulationSpeed.Speed0, () => loadedRealMs));
        normal.SetSpeed(SimulationSpeed.Speed1);
        fast.SetSpeed(SimulationSpeed.Speed4);
        normal.ReceiveCommand(ApproachCommand());
        fast.ReceiveCommand(ApproachCommand());
        var origin = PlayerShipFrom(normal.CaptureSnapshot());
        Assert.Equal(origin.ApproachRoute, PlayerShipFrom(fast.CaptureSnapshot()).ApproachRoute);
        long frames = (long)Math.Ceiling(origin.ApproachRoute!.DurationMs / 100_000) + 1;
        var predictor = new LinearMotionPredictor();
        for (int frame = 1; frame <= frames; frame++)
        {
            AuthoritativeSnapshot slowSnapshot = normal.CaptureSnapshot();
            for (int tick = 0; tick < 100; tick++)
            {
                normalRealMs += 1000;
                slowSnapshot = normal.CaptureSnapshot(advanceClock: true);
            }
            fastRealMs += 1000;
            var fastSnapshot = fast.CaptureSnapshot(advanceClock: true);
            var actual = PlayerShipFrom(fastSnapshot);
            Assert.Equal(slowSnapshot.GameTimeMs, fastSnapshot.GameTimeMs);
            Assert.Equal(slowSnapshot.SimulationTimeMs, fastSnapshot.SimulationTimeMs);
            Compare(PlayerShipFrom(slowSnapshot), actual);
            Compare(predictor.Predict(origin, frame * 100_000L), actual);
            if (frame == 1)
            {
                loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(fast.CaptureSaveState()), true));
                Compare(actual, PlayerShipFrom(loaded.CaptureSnapshot()));
            }
            else
            {
                loadedRealMs += 1000;
                Compare(actual, PlayerShipFrom(loaded.CaptureSnapshot(advanceClock: true)));
            }
        }
        Assert.Null(PlayerShipFrom(fast.CaptureSnapshot()).ActiveEngineCommandType);

        static void Compare(ObjectMotionSnapshot expected, ObjectMotionSnapshot actual)
        {
            Assert.Equal(expected.X, actual.X, 6);
            Assert.Equal(expected.Y, actual.Y, 6);
            Assert.Equal(expected.Direction, actual.Direction, 6);
            Assert.Equal(expected.SpeedKmS, actual.SpeedKmS);
            Assert.Equal(expected.ActiveEngineCommandType, actual.ActiveEngineCommandType);
        }
    }
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Saved_old_line_capture_plan_is_replanned_at_boundary_without_a_position_jump(int version)
    {
        using var engine = CreateEngine(shipSpeedMps: 700, targetSpeedMps: 1069, targetDirectionDegrees: 57);
        engine.ReceiveCommand(ApproachCommand());
        engine.CaptureSnapshotForTests();
        var save = engine.CaptureSaveStateForTests(137, SimulationSpeed.Speed1);
        save = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId != PlayerShipId ? o : o with
                {
                    Modules = o.Modules!.Select(m => m.ActiveCycle?.ApproachRoute is not { } route ? m : m with
                    {
                        ActiveCycle = m.ActiveCycle with { ApproachRoute = route with { PlannerVersion = version } }
                    }).ToArray()
                }).ToArray()
            }
        };
        using var restored = CreateEngine();
        restored.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true));
        var initial = PlayerShipFrom(restored.CaptureSnapshot());
        long remaining = initial.TurnStepRemainingMs;
        var predicted = new LinearMotionPredictor().Predict(initial, remaining);
        var actual = PlayerShipFrom(restored.CaptureSnapshotForTests(137 + remaining, SimulationSpeed.Speed1));
        Assert.Equal(predicted.X, actual.X, 7);
        Assert.Equal(predicted.Y, actual.Y, 7);
        Assert.Equal(predicted.Direction, actual.Direction, 7);
        Assert.Equal(ApproachLineCaptureMath.PlannerVersion, actual.ApproachRoute!.PlannerVersion);
        Assert.Equal(0, actual.ApproachRoute.ElapsedMs);
    }
    internal static SimulationEngine CreateEngine(
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
        bool includeNavigationComputer = false, SimulationClock? clock = null)
    {
        var engine = new SimulationEngine(CreateRegistry(turnStepDegrees, angularInertiaDegPerSec, trailDistanceKm), clock: clock);

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
            "playerShipObjectId": "{{PlayerShipId}}", "playerTokens": 1000,
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
                "objectType": "{{targetObjectType}}", "portFeeCreditsPerDay": 100, "securityZoneRadiusKm": 200, "piracyWarningGracePeriodMs": 60000,
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
            ], dialogues: DialogueContentLoader.Load(DialogueTests.ContentPath));
    }
}
