using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// Save/load continuity coverage for requirements §3990-4021: after a CaptureSaveState →
/// LoadScenario round trip at the same GameTimeMs, further ticks must produce the same
/// authoritative result as if the original engine had simply continued running.
/// </summary>
public class SaveLoadContinuityTests
{
    private const string PlayerShipId = "SHIP";
    private const string EngineModuleId = "ENGINE-1";
    private const string CargoModuleId = "CARGO-1";
    private const string StationId = "STATION-1";
    private const string AsteroidId = "AST-1";

    [Fact]
    public void LoadScenario_stamps_StartGameTimeMs_from_scenario_gameTimeMs_not_zero()
    {
        // P1 regression (external PR review): LoadScenario used to always stamp every
        // object's StartGameTimeMs = 0, regardless of gs.GameTimeMs. The correct value is
        // only re-stamped later, inside RunAsync's prologue — which runs asynchronously via
        // Task.Run from LocalGameSessionConnection's constructor. CaptureSaveState() (and
        // BuildSnapshot) are reachable in the window between construction and RunAsync's
        // first iteration — e.g. F5 fired right after F9, or CaptureSaveState() called
        // directly on a freshly bootstrapped engine, as reproduced here via LoadScenario
        // with NO RunAsync call at all. With the bug, elapsed = gameTimeMs - 0 double-counted
        // the save's own gameTimeMs as extra travel from the saved position, instead of the
        // correct elapsed = 0 (no time has passed since the save was captured).
        const int speedMps = 500; // nonzero — position visibly drifts under the bug
        const long gameTimeMs = 12345;

        string saveJson = $$"""
        {
          "scenarioMetadata": { "scenarioId": "quicksave", "name": "Quicksave" },
          "saveFormatVersion": 1,
          "gameState": {
            "gameTimeMs": {{gameTimeMs}},
            "currentSpeed": "Speed1",
            "playerShipObjectId": "{{PlayerShipId}}",
            "spaceObjects": [
              {
                "objectId": "{{PlayerShipId}}", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 1000, "positionY": 2000,
                "speedMps": {{speedMps}}, "directionDegrees": 0, "movementType": "Linear"
              }
            ]
          }
        }
        """;

        var save = ScenarioLoader.LoadFromJson(saveJson, allowNonZeroGameTime: true);

        var engine = new SimulationEngine();
        engine.LoadScenario(save); // deliberately no RunAsync — reproduces the vulnerable window

        // Capturing immediately at the SAME gameTimeMs the save claims must reproduce
        // exactly the same position: zero elapsed time, not gameTimeMs worth of travel.
        var recaptured = engine.CaptureSaveStateForTests(gameTimeMs, SimulationSpeed.Speed1);
        var ship = recaptured.GameState.SpaceObjects.Single(o => o.ObjectId == PlayerShipId);

        Assert.Equal(1000, ship.PositionX, precision: 6);
        Assert.Equal(2000, ship.PositionY, precision: 6);
    }

    [Fact]
    public async Task Round_trip_continues_active_turn_cycle_identically()
    {
        var engine = CreateEngine(speedMps: 100, directionDegrees: 0);

        engine.ReceiveCommand(new PlayerCommand(
            "cmd-1", 1, PlayerShipId, EngineModuleId, ShipEngineCommandTypes.TurnLeftUntilCancel));

        // A couple of 1 Hz-shaped ticks get the auto-repeating cycle properly under way
        // (first tick starts it, second tick completes/renews it once) before we save
        // mid-way through the next 1000 ms turn-step window.
        engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1);
        engine.CaptureSnapshotForTests(1000, SimulationSpeed.Speed1);

        var saveState = engine.CaptureSaveStateForTests(1500, SimulationSpeed.Speed1);

        var loadedEngine = new SimulationEngine(CreateRegistry());
        loadedEngine.LoadScenario(saveState);

        // Mirror the real F9 bootstrap: LoadScenario alone leaves every object's
        // StartGameTimeMs at 0 by design (see the "startGameTime stamped at RunAsync"
        // comment in SimulationEngine.LoadScenario) — only RunAsync's prologue re-stamps
        // it from the (now-restored) clock, exactly as LocalGameSessionConnection's
        // background engine loop does in production. Consuming just the first
        // (immediately-yielded, no delay) snapshot triggers that stamp without waiting
        // on the 1 Hz loop.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await foreach (var _ in loadedEngine.RunAsync(cts.Token))
            break;

        // Continue both engines to the same future GameTimeMs — several more turn steps
        // and travel segments — and compare.
        var originalContinued = PlayerShipFrom(engine.CaptureSnapshotForTests(4000, SimulationSpeed.Speed1));
        var loadedContinued = PlayerShipFrom(loadedEngine.CaptureSnapshotForTests(4000, SimulationSpeed.Speed1));

        Assert.Equal(originalContinued.Direction, loadedContinued.Direction, precision: 6);
        Assert.Equal(originalContinued.X, loadedContinued.X, precision: 6);
        Assert.Equal(originalContinued.Y, loadedContinued.Y, precision: 6);
        Assert.Equal(originalContinued.SpeedKmS, loadedContinued.SpeedKmS, precision: 6);
        Assert.Equal(originalContinued.ActiveEngineCommandType, loadedContinued.ActiveEngineCommandType);
        Assert.Equal(ShipEngineCommandTypes.TurnLeftUntilCancel, loadedContinued.ActiveEngineCommandType);
    }

    [Fact]
    public void Save_captures_a_command_sent_just_before_it_instead_of_losing_it()
    {
        // Phase 4 review finding (medium): CaptureSaveStateCore did not call
        // ApplyPendingCommands, unlike BuildSnapshot. A command sent in the narrow window
        // between the last 1 Hz tick and F5 (before the background loop ever ticks again)
        // sat only in the in-memory _pendingCommands queue — captured nowhere in the save,
        // and gone for good once F9 disposes the old session/engine. Fixed by mirroring
        // BuildSnapshot's own order (CompleteActiveEngineCycles, then ApplyPendingCommands)
        // inside CaptureSaveStateCore.
        var engine = CreateEngine(speedMps: 0, directionDegrees: 0);

        // Command sent but the engine has not ticked/snapshotted since it was queued —
        // simulates F5 landing before the next regular 1 Hz BuildSnapshot.
        engine.ReceiveCommand(new PlayerCommand(
            "cmd-1", 1, PlayerShipId, EngineModuleId, ShipEngineCommandTypes.TurnRightStep));

        var saveState = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed1);

        // The command must have been started (not silently dropped): the save carries a
        // fresh one-shot ActiveCycle for it, even though — exactly like a normal
        // BuildSnapshot at the same instant — the turn itself hasn't completed yet
        // (zero-duration one-shot cycles resolve on the NEXT tick, not the one that starts
        // them; see EngineCommandTests.Rapid_one_shot_commands_are_deferred_until_the_module_is_free).
        var savedShip = saveState.GameState.SpaceObjects.Single(o => o.ObjectId == PlayerShipId);
        var savedEngineModule = savedShip.Modules!.Single(m => m.ModuleId == EngineModuleId);
        Assert.NotNull(savedEngineModule.ActiveCycle);
        Assert.Equal(ShipEngineCommandTypes.TurnRightStep, savedEngineModule.ActiveCycle!.CommandType);
        Assert.Equal(0, savedShip.DirectionDegrees);

        // After F9: load into a fresh engine and let the pending turn resolve on its next
        // tick, exactly as it would have if the original session had never been saved.
        var loadedEngine = new SimulationEngine(CreateRegistry());
        loadedEngine.LoadScenario(saveState);
        var loadedShip = PlayerShipFrom(loadedEngine.CaptureSnapshotForTests(100, SimulationSpeed.Speed1));

        Assert.Equal(1, loadedShip.Direction);
    }

    [Fact]
    public void Round_trip_preserves_module_and_object_state_including_isKnown()
    {
        // Covers module runtime fields (PowerState/OperationalState/StructurePoints/Cargo)
        // for a non-engine module, plus IsKnown and PersistenceType/MassKg/CompositionType
        // for non-player objects (a known Station and an unknown Temporary asteroid).
        var engine = CreateEngine(speedMps: 0, directionDegrees: 0);

        var originalSave = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed1);

        var loadedEngine = new SimulationEngine(CreateRegistry());
        loadedEngine.LoadScenario(originalSave);
        var reloadedSave = loadedEngine.CaptureSaveStateForTests(0, SimulationSpeed.Speed1);

        Assert.Equal(
            originalSave.GameState.SpaceObjects.Count,
            reloadedSave.GameState.SpaceObjects.Count);

        foreach (var expected in originalSave.GameState.SpaceObjects)
        {
            var actual = reloadedSave.GameState.SpaceObjects.Single(o => o.ObjectId == expected.ObjectId);

            Assert.Equal(expected.ObjectType, actual.ObjectType);
            Assert.Equal(expected.PersistenceType, actual.PersistenceType);
            Assert.Equal(expected.MassKg, actual.MassKg);
            Assert.Equal(expected.CompositionType, actual.CompositionType);
            Assert.Equal(expected.IsKnown, actual.IsKnown);
            Assert.Equal(expected.PositionX, actual.PositionX, precision: 6);
            Assert.Equal(expected.PositionY, actual.PositionY, precision: 6);
            Assert.Equal(expected.SpeedMps, actual.SpeedMps);
            Assert.Equal(expected.DirectionDegrees, actual.DirectionDegrees);

            var expectedModules = expected.Modules ?? Array.Empty<ShipModuleData>();
            var actualModules = actual.Modules ?? Array.Empty<ShipModuleData>();
            Assert.Equal(expectedModules.Count, actualModules.Count);

            foreach (var expectedModule in expectedModules)
            {
                var actualModule = actualModules.Single(m => m.ModuleId == expectedModule.ModuleId);

                Assert.Equal(expectedModule.ModuleTypeId, actualModule.ModuleTypeId);
                Assert.Equal(expectedModule.OccupiedCells, actualModule.OccupiedCells);
                Assert.Equal(expectedModule.StructurePoints, actualModule.StructurePoints);
                Assert.Equal(expectedModule.PowerState, actualModule.PowerState);
                Assert.Equal(expectedModule.OperationalState, actualModule.OperationalState);
                Assert.Equal(expectedModule.ActiveCycle, actualModule.ActiveCycle);

                var expectedCargo = expectedModule.Cargo ?? Array.Empty<CargoStackData>();
                var actualCargo = actualModule.Cargo ?? Array.Empty<CargoStackData>();
                Assert.Equal(expectedCargo.Count, actualCargo.Count);

                foreach (var expectedStack in expectedCargo)
                {
                    var actualStack = actualCargo.Single(c => c.ItemTypeId == expectedStack.ItemTypeId);
                    Assert.Equal(expectedStack.Quantity, actualStack.Quantity);
                }
            }
        }

        // Specifically pin down the two infrastructure fields this iteration adds.
        var station = reloadedSave.GameState.SpaceObjects.Single(o => o.ObjectId == StationId);
        Assert.True(station.IsKnown);
        var asteroid = reloadedSave.GameState.SpaceObjects.Single(o => o.ObjectId == AsteroidId);
        Assert.False(asteroid.IsKnown);
        Assert.Equal("Temporary", asteroid.PersistenceType);
        Assert.Equal(2_000_000, asteroid.MassKg);
        Assert.Equal("Silicate", asteroid.CompositionType);

        var cargoModule = reloadedSave.GameState.SpaceObjects
            .Single(o => o.ObjectId == PlayerShipId).Modules!.Single(m => m.ModuleId == CargoModuleId);
        Assert.Equal("Damaged", cargoModule.OperationalState);
        Assert.Equal("Off", cargoModule.PowerState);
        Assert.Equal(250, cargoModule.StructurePoints);
        var cargoStack = Assert.Single(cargoModule.Cargo!);
        Assert.Equal("item.energy-cells", cargoStack.ItemTypeId);
        Assert.Equal(750, cargoStack.Quantity);

        // story-20260901-112254 (Batch A, U2): crew round-trips through save/load.
        var playerShip = reloadedSave.GameState.SpaceObjects.Single(o => o.ObjectId == PlayerShipId);
        var crewMember = Assert.Single(playerShip.Crew!);
        Assert.Equal("CHR-0001", crewMember.CrewId);
        Assert.Equal("Dunkan Su", crewMember.DisplayName);
    }

    private static ObjectMotionSnapshot PlayerShipFrom(AuthoritativeSnapshot snapshot)
    {
        return snapshot.Objects.Single(o => o.ObjectId == PlayerShipId);
    }

    private static SimulationEngine CreateEngine(int speedMps, int directionDegrees)
    {
        var engine = new SimulationEngine(CreateRegistry());
        engine.LoadScenario(ScenarioLoader.LoadFromJson($$"""
        {
          "scenarioMetadata": { "scenarioId": "test", "name": "Test" },
          "gameState": {
            "gameTimeMs": 0,
            "currentSpeed": "Speed1",
            "playerShipObjectId": "{{PlayerShipId}}",
            "spaceObjects": [
              {
                "objectId": "{{PlayerShipId}}",
                "objectType": "PlayerShip",
                "persistenceType": "Permanent",
                "name": "Player Ship",
                "positionX": 1000,
                "positionY": 2000,
                "speedMps": {{speedMps}},
                "directionDegrees": {{directionDegrees}},
                "movementType": "Linear",
                "hullLayout": {
                  "width": 3, "height": 2,
                  "cells": [
                    {"x":0,"y":0},
                    {"x":1,"y":0},{"x":2,"y":0},
                    {"x":1,"y":1},{"x":2,"y":1}
                  ]
                },
                "modules": [
                  {
                    "moduleId": "{{EngineModuleId}}",
                    "moduleTypeId": "module.engine.basic",
                    "occupiedCells": [ {"x":0,"y":0} ],
                    "structurePoints": 100,
                    "powerState": "On",
                    "operationalState": "Ready",
                    "activeCycle": null,
                    "cargo": []
                  },
                  {
                    "moduleId": "{{CargoModuleId}}",
                    "moduleTypeId": "module.container.basic",
                    "occupiedCells": [ {"x":1,"y":0},{"x":2,"y":0},{"x":1,"y":1},{"x":2,"y":1} ],
                    "structurePoints": 250,
                    "powerState": "Off",
                    "operationalState": "Damaged",
                    "activeCycle": null,
                    "cargo": [
                      { "itemTypeId": "item.energy-cells", "quantity": 750 }
                    ]
                  }
                ],
                "crew": [
                  { "crewId": "CHR-0001", "displayName": "Dunkan Su" }
                ]
              },
              {
                "objectId": "{{StationId}}",
                "objectType": "Station",
                "persistenceType": "Permanent",
                "name": "Known Station",
                "positionX": 5000,
                "positionY": 5000,
                "speedMps": 0,
                "directionDegrees": 0,
                "movementType": "Stationary",
                "isKnown": true
              },
              {
                "objectId": "{{AsteroidId}}",
                "objectType": "Asteroid",
                "persistenceType": "Temporary",
                "positionX": 7000,
                "positionY": 7000,
                "speedMps": 300,
                "directionDegrees": 45,
                "movementType": "Linear",
                "massKg": 2000000,
                "compositionType": "Silicate",
                "isKnown": false
              }
            ]
          }
        }
        """));

        return engine;
    }

    private static GameDataRegistry CreateRegistry()
    {
        string[] engineCommandIds =
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
                new ModuleCategoryDefinition(
                    "module.engine.basic",
                    "Engine",
                    SlotSize: 1,
                    CommandTypeIds: engineCommandIds.ToImmutableArray()),
                new ModuleCategoryDefinition(
                    "module.container.basic",
                    "Container",
                    SlotSize: 4,
                    CommandTypeIds: ImmutableArray<string>.Empty)
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
                    TurnStepDegrees: 1,
                    LinearInertiaMps2: 400,
                    AngularInertiaDegPerSec: 4,
                    BaseCycleTimeMs: 1000),
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
                new ItemTypeDefinition("item.energy-cells", "Energy Cells", UnitMassKg: 10)
            ],
            engineCommandIds.Select(id => new CommandDefinition(
                id,
                id,
                TimeFactor: id is ShipEngineCommandTypes.TurnLeftUntilCancel
                    or ShipEngineCommandTypes.TurnRightUntilCancel
                    ? 1000
                    : 0,
                Type: "module.engine.basic")));
    }
}
