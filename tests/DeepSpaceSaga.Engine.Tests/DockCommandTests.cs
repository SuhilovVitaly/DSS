using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// Tests for navigation.dock — the docking MVP (requirements Docking.md, Station.md):
/// an immediate one-shot authoritative action validated by target/range/synchronization,
/// distinct from the Engine module's timed ActiveCycle commands covered by
/// <see cref="EngineCommandTests"/>.
/// </summary>
public class DockCommandTests
{
    private const string PlayerShipId = "SPC-0001";
    private const string NavModuleId = "MOD-NAV-01";
    private const string StationId = "STATION-01";

    private static PlayerCommand DockCommand(string? targetObjectId = StationId) =>
        new("cmd-dock", 1, PlayerShipId, NavModuleId, NavigationComputerCommandTypes.Dock,
            TargetObjectId: targetObjectId);

    private static ObjectMotionSnapshot PlayerShipFrom(AuthoritativeSnapshot snapshot) =>
        snapshot.Objects.Single(o => o.ObjectId == PlayerShipId);

    /// <param name="shipX">Player ship starting X (world units).</param>
    /// <param name="shipY">Player ship starting Y (world units).</param>
    /// <param name="shipSpeedMps">Player ship starting speed (m/s).</param>
    /// <param name="stationX">Station X (world units).</param>
    /// <param name="stationSpeedMps">Station speed (m/s) — 0 for the usual Stationary station.</param>
    /// <param name="stationObjectType">Object type of the "STATION-01" object — Station by default; a test can override to a non-Station type.</param>
    /// <param name="rangeKm">navigation.dock's configured range, world-units-converted for the check.</param>
    private static SimulationEngine CreateEngine(
        double shipX = 10000,
        double shipY = 10000,
        int shipSpeedMps = 0,
        double stationX = 10001,
        int stationSpeedMps = 0,
        string stationObjectType = "Station",
        int rangeKm = 200,
        string powerState = "On",
        string operationalState = "Ready",
        int structurePoints = 80)
    {
        var engine = new SimulationEngine(CreateRegistry(rangeKm));
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
                "directionDegrees": 0,
                "movementType": "{{(shipSpeedMps > 0 ? "Linear" : "Stationary")}}",
                "hullLayout": { "width": 1, "height": 1, "cells": [ {"x":0,"y":0} ] },
                "modules": [
                  {
                    "moduleId": "{{NavModuleId}}",
                    "moduleTypeId": "module.bridge.navigation.computer.basic",
                    "occupiedCells": [ {"x":0,"y":0} ],
                    "structurePoints": {{structurePoints}},
                    "powerState": "{{powerState}}",
                    "operationalState": "{{operationalState}}",
                    "activeCycle": null,
                    "cargo": []
                  }
                ]
              },
              {
                "objectId": "{{StationId}}",
                "objectType": "{{stationObjectType}}",
                "persistenceType": "Permanent",
                "positionX": {{stationX}},
                "positionY": 10000,
                "speedMps": {{stationSpeedMps}},
                "directionDegrees": 0,
                "movementType": "{{(stationSpeedMps > 0 ? "Linear" : "Stationary")}}"
              }
            ]
          }
        }
        """));

        return engine;
    }

    private static GameDataRegistry CreateRegistry(int rangeKm)
    {
        string[] commandIds = [NavigationComputerCommandTypes.Dock, NavigationComputerCommandTypes.StationsList];

        return GameDataRegistry.Create(
            [
                new ModuleCategoryDefinition(
                    "module.bridge.navigation.computer",
                    "Navigation Computer",
                    SlotSize: 1,
                    CommandTypeIds: commandIds.ToImmutableArray())
            ],
            [
                new ModuleTypeDefinition(
                    "module.bridge.navigation.computer.basic",
                    "Bridge Navigation Computer",
                    SlotSize: 1,
                    MassKg: 4000,
                    StructurePointsMax: 80,
                    PowerConsumptionW: 0,
                    CommandTypeIds: commandIds.ToImmutableArray(),
                    BaseCycleTimeMs: 1000)
            ],
            [],
            [
                new CommandDefinition(
                    NavigationComputerCommandTypes.Dock, "Dock",
                    TimeFactor: 2000, Target: "object", Type: "module.bridge.navigation.computer",
                    RangeKm: rangeKm),
                new CommandDefinition(
                    NavigationComputerCommandTypes.StationsList, "Stations List",
                    Target: "none", Type: "module.bridge.navigation.computer")
            ]);
    }

    [Fact]
    public void Dock_in_range_and_synchronized_succeeds_and_snaps_ship_onto_station()
    {
        var engine = CreateEngine(shipX: 10000, shipY: 10000, shipSpeedMps: 0, stationX: 10001, stationSpeedMps: 0);

        engine.ReceiveCommand(DockCommand());
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Executed, result.Status);

        var ship = PlayerShipFrom(snapshot);
        Assert.True(ship.IsDocked);
        Assert.Equal(StationId, ship.DockedStationObjectId);
        Assert.Equal(10001 + 1.0, ship.X, precision: 6);
        Assert.Equal(10000 + 1.0, ship.Y, precision: 6);
        Assert.Equal(0, ship.SpeedKmS, precision: 6);
    }

    [Fact]
    public void Dock_without_target_is_rejected_with_missing_target()
    {
        var engine = CreateEngine();

        engine.ReceiveCommand(DockCommand(targetObjectId: null));
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(CommandReasonCodes.MissingTarget, result.ReasonCode);
        Assert.False(PlayerShipFrom(snapshot).IsDocked);
    }

    [Fact]
    public void Dock_with_unknown_target_is_rejected()
    {
        var engine = CreateEngine();

        engine.ReceiveCommand(DockCommand(targetObjectId: "NO-SUCH-OBJECT"));
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(CommandReasonCodes.UnknownTarget, result.ReasonCode);
    }

    [Fact]
    public void Dock_target_that_is_not_a_station_is_rejected()
    {
        var engine = CreateEngine(stationObjectType: "Asteroid");

        engine.ReceiveCommand(DockCommand());
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(CommandReasonCodes.DockTargetNotStation, result.ReasonCode);
        Assert.False(PlayerShipFrom(snapshot).IsDocked);
    }

    [Fact]
    public void Dock_out_of_range_is_rejected()
    {
        // 3000 world units = 300 km, beyond the 200 km configured range.
        var engine = CreateEngine(shipX: 10000, stationX: 13000);

        engine.ReceiveCommand(DockCommand());
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(CommandReasonCodes.DockOutOfRange, result.ReasonCode);
        Assert.False(PlayerShipFrom(snapshot).IsDocked);
    }

    [Fact]
    public void Dock_at_exactly_configured_range_succeeds()
    {
        // 2000 world units = 200 km, exactly the configured range — must not be rejected.
        var engine = CreateEngine(shipX: 10000, stationX: 12000, rangeKm: 200);

        engine.ReceiveCommand(DockCommand());
        var snapshot = engine.CaptureSnapshotForTests();

        Assert.Equal(CommandResultStatus.Executed, Assert.Single(snapshot.CommandResults).Status);
        Assert.True(PlayerShipFrom(snapshot).IsDocked);
    }

    [Fact]
    public void Dock_with_unsynchronized_speed_is_rejected()
    {
        var engine = CreateEngine(shipSpeedMps: 500, stationSpeedMps: 0);

        engine.ReceiveCommand(DockCommand());
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(CommandReasonCodes.DockNotSynchronized, result.ReasonCode);
        Assert.False(PlayerShipFrom(snapshot).IsDocked);
    }

    [Fact]
    public void Dock_when_navigation_computer_module_is_powered_off_is_rejected()
    {
        var engine = CreateEngine(powerState: "Off");

        engine.ReceiveCommand(DockCommand());
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(CommandReasonCodes.ModuleUnavailable, result.ReasonCode);
    }

    [Fact]
    public void Docked_state_survives_save_and_load()
    {
        var engine = CreateEngine();
        engine.ReceiveCommand(DockCommand());
        engine.CaptureSnapshotForTests(); // drain the command so it's applied before capture below

        var saveState = engine.CaptureSaveStateForTests(gameTimeMs: 0, DeepSpaceSaga.Contracts.SimulationSpeed.Speed0);

        var reloaded = new SimulationEngine(CreateRegistry(200));
        reloaded.LoadScenario(saveState);
        var snapshot = reloaded.CaptureSnapshotForTests();

        var ship = PlayerShipFrom(snapshot);
        Assert.True(ship.IsDocked);
        Assert.Equal(StationId, ship.DockedStationObjectId);
    }
}
