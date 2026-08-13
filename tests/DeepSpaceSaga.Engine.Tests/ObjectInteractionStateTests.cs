using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// ТЗ: ActiveObject and SelectedObject — authoritative Engine-side state, validation,
/// and snapshot projection for ActiveObjectId/SelectedObjectId.
/// </summary>
public class ObjectInteractionStateTests
{
    [Fact]
    public void New_engine_has_null_active_and_selected_object_ids()
    {
        var engine = new SimulationEngine();

        Assert.Null(engine.ActiveObjectId);
        Assert.Null(engine.SelectedObjectId);
    }

    [Fact]
    public void SetObjectInteractionState_accepts_existing_object_ids()
    {
        var engine = new SimulationEngine();
        engine.AddTestObject(new ObjectMotionSnapshot("obj-1", 0, 0, SpeedKmS: 0, Direction: 0));
        engine.AddTestObject(new ObjectMotionSnapshot("obj-2", 0, 0, SpeedKmS: 0, Direction: 0));

        engine.SetObjectInteractionState("obj-1", "obj-2");

        Assert.Equal("obj-1", engine.ActiveObjectId);
        Assert.Equal("obj-2", engine.SelectedObjectId);
    }

    [Fact]
    public void SetObjectInteractionState_normalizes_unknown_ids_to_null()
    {
        var engine = new SimulationEngine();
        engine.AddTestObject(new ObjectMotionSnapshot("obj-1", 0, 0, SpeedKmS: 0, Direction: 0));

        engine.SetObjectInteractionState("does-not-exist", "also-missing");

        Assert.Null(engine.ActiveObjectId);
        Assert.Null(engine.SelectedObjectId);
    }

    [Fact]
    public void SetObjectInteractionState_validates_active_and_selected_independently()
    {
        var engine = new SimulationEngine();
        engine.AddTestObject(new ObjectMotionSnapshot("obj-1", 0, 0, SpeedKmS: 0, Direction: 0));

        // Active is a real object, Selected is not — each id must be normalized on
        // its own; one being unknown must not null out the other.
        engine.SetObjectInteractionState("obj-1", "unknown");

        Assert.Equal("obj-1", engine.ActiveObjectId);
        Assert.Null(engine.SelectedObjectId);
    }

    [Fact]
    public void SetObjectInteractionState_accepts_explicit_null_for_both()
    {
        var engine = new SimulationEngine();
        engine.AddTestObject(new ObjectMotionSnapshot("obj-1", 0, 0, SpeedKmS: 0, Direction: 0));
        engine.SetObjectInteractionState("obj-1", "obj-1");

        engine.SetObjectInteractionState(null, null);

        Assert.Null(engine.ActiveObjectId);
        Assert.Null(engine.SelectedObjectId);
    }

    [Fact]
    public void SetObjectInteractionState_works_at_Speed0()
    {
        var engine = new SimulationEngine();
        engine.AddTestObject(new ObjectMotionSnapshot("obj-1", 0, 0, SpeedKmS: 0, Direction: 0));
        engine.SetSpeed(SimulationSpeed.Speed0);

        // Pause stops simulation, not session-control state.
        engine.SetObjectInteractionState("obj-1", "obj-1");

        Assert.Equal("obj-1", engine.ActiveObjectId);
        Assert.Equal("obj-1", engine.SelectedObjectId);
    }

    [Fact]
    public void BuildSnapshot_carries_authoritative_active_and_selected_object_ids()
    {
        var engine = new SimulationEngine();
        engine.AddTestObject(new ObjectMotionSnapshot("obj-1", 0, 0, SpeedKmS: 0, Direction: 0));
        engine.SetObjectInteractionState("obj-1", "obj-1");

        var snapshot = engine.CaptureSnapshotForTests();

        Assert.Equal("obj-1", snapshot.ActiveObjectId);
        Assert.Equal("obj-1", snapshot.SelectedObjectId);
    }

    [Fact]
    public void New_session_snapshot_defaults_active_and_selected_object_ids_to_null()
    {
        var engine = new SimulationEngine();
        engine.AddTestObject(new ObjectMotionSnapshot("obj-1", 0, 0, SpeedKmS: 0, Direction: 0));

        var snapshot = engine.CaptureSnapshotForTests();

        Assert.Null(snapshot.ActiveObjectId);
        Assert.Null(snapshot.SelectedObjectId);
    }

    [Fact]
    public void LoadScenario_resets_active_and_selected_object_ids_to_null()
    {
        var engine = new SimulationEngine();
        engine.AddTestObject(new ObjectMotionSnapshot("obj-1", 0, 0, SpeedKmS: 0, Direction: 0));
        engine.SetObjectInteractionState("obj-1", "obj-1");
        Assert.Equal("obj-1", engine.ActiveObjectId);

        // New Game / Quick Load both go through LoadScenario replacing the whole
        // world — session-interaction state must never survive that (§54), even
        // when the new world happens to contain an object with the same id.
        var scenario = ScenarioLoader.LoadFromJson("""
        {
          "scenarioMetadata": { "scenarioId": "x", "name": "x" },
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed1",
            "playerShipObjectId": "SHIP",
            "spaceObjects": [
              { "objectId": "SHIP", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary" }
            ]
          }
        }
        """);
        engine.LoadScenario(scenario);

        Assert.Null(engine.ActiveObjectId);
        Assert.Null(engine.SelectedObjectId);

        var snapshot = engine.CaptureSnapshotForTests();
        Assert.Null(snapshot.ActiveObjectId);
        Assert.Null(snapshot.SelectedObjectId);
    }

    [Fact]
    public void Selection_does_not_start_simulation_events_or_affect_object_motion()
    {
        var engine = new SimulationEngine();
        engine.AddTestObject(new ObjectMotionSnapshot("obj-1", X: 0, Y: 0, SpeedKmS: 5, Direction: 90));

        var before = engine.CaptureSnapshotForTests(gameTimeMs: 1000);
        engine.SetObjectInteractionState("obj-1", "obj-1");
        var after = engine.CaptureSnapshotForTests(gameTimeMs: 1000);

        Assert.Equal(before.Objects[0].X, after.Objects[0].X);
        Assert.Equal(before.Objects[0].Y, after.Objects[0].Y);
        Assert.Empty(after.ShipEvents.IsDefault ? [] : after.ShipEvents);
    }
}
