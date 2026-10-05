using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class VoyagePersistenceTests
{
    [Fact]
    public void Active_voyage_survives_json_save_load_without_repeating_undock()
    {
        using var engine = VoyageLifecycleTests.CreateEngine();
        string destination = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0)
            .Voyage!.RouteOptions.First(o => o.IsAvailable).DestinationStationObjectId;
        engine.ReceiveCommand(VoyageLifecycleTests.Undock("persist-voyage", destination));
        var departed = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0);
        var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveState()), true);

        using var restored = new SimulationEngine(QuotedTradeExecutionTests.RealRegistry());
        restored.LoadScenario(save, isSave: true);
        var loaded = restored.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0);
        Assert.Equal(departed.ActiveVoyage, loaded.ActiveVoyage);
        Assert.False(loaded.Objects.Single(o => o.ObjectId == "SPC-0001").IsDocked);
        Assert.Null(loaded.DockedStationTrade);
        Assert.Equal("persist-voyage", save.GameState.VoyageState?.VoyageId);
    }

    [Fact]
    public void Invalid_saved_voyage_does_not_replace_world()
    {
        using var engine = VoyageLifecycleTests.CreateEngine();
        var before = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0);
        var saved = engine.CaptureSaveState();
        var bad = saved with
        {
            GameState = saved.GameState with
            {
                VoyageState = new VoyageStateData(VoyagePhases.InTransit, "bad", "SPC-0002", "missing", 0, 100),
            }
        };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(bad, isSave: true));
        var after = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0);
        Assert.Equal(before.Objects.Select(o => (o.ObjectId, o.X, o.Y, o.IsDocked, o.DockedStationObjectId)),
            after.Objects.Select(o => (o.ObjectId, o.X, o.Y, o.IsDocked, o.DockedStationObjectId)));
        Assert.Equal(before.Voyage?.Phase, after.Voyage?.Phase);
        Assert.Equal(before.Voyage?.BlockReasonCode, after.Voyage?.BlockReasonCode);
        Assert.Equal(before.Voyage?.RouteOptions.ToArray(), after.Voyage?.RouteOptions.ToArray());
    }
    [Theory]
    [InlineData("phase")]
    [InlineData("id")]
    [InlineData("same-station")]
    [InlineData("progress-negative")]
    [InlineData("progress-overflow")]
    [InlineData("distance")]
    [InlineData("start-future")]
    [InlineData("ship-conflict")]
    [InlineData("docking-without-dialogue")]
    [InlineData("no-map")]
    public void Invalid_active_shapes_are_rejected_atomically(string defect)
    {
        using var engine = VoyageLifecycleTests.CreateEngine();
        string destination = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0)
            .Voyage!.RouteOptions.First(o => o.IsAvailable).DestinationStationObjectId;
        engine.ReceiveCommand(VoyageLifecycleTests.Undock("valid-leg", destination));
        var before = ScenarioLoader.Serialize(engine.CaptureSaveState());
        var save = engine.CaptureSaveState();
        var state = save.GameState.VoyageState!;
        state = defect switch
        {
            "phase" => state with { Phase = "Flying" },
            "id" => state with { VoyageId = " " },
            "same-station" => state with { DestinationStationObjectId = state.OriginStationObjectId },
            "progress-negative" => state with { ProgressPermille = -1 },
            "progress-overflow" => state with { ProgressPermille = 1001 },
            "distance" => state with { InitialDistanceWorldUnits = 0 },
            "start-future" => state with { StartedMotionTimeMs = save.GameState.MotionTimeMs + 1 },
            "docking-without-dialogue" => state with { Phase = VoyagePhases.Docking },
            _ => state,
        };
        var bad = save with
        {
            GameState = save.GameState with
            {
                VoyageState = state,
                TradingMap = defect == "no-map" ? null : save.GameState.TradingMap,
                SpaceObjects = defect == "ship-conflict"
                ? save.GameState.SpaceObjects.Select(o => o.ObjectId == "SPC-0001"
                    ? o with { IsDocked = true, DockedStationObjectId = state.OriginStationObjectId } : o).ToArray()
                : save.GameState.SpaceObjects,
            }
        };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(bad, isSave: true));
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }

    [Fact]
    public void In_transit_json_continuation_preserves_next_physical_progress()
    {
        using var voyage = TradingVoyageFixture.Create();
        voyage.Send("MOD-PLAYER-BRIDGE-01", NavigationComputerCommandTypes.Undock, target: voyage.Destination);
        voyage.Advance(1);
        Assert.Equal(VoyagePhases.InTransit, voyage.Snapshot.ActiveVoyage!.Phase);
        var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(voyage.Save()), true);
        using var restored = new SimulationEngine(QuotedTradeExecutionTests.RealRegistry());
        restored.LoadScenario(save, isSave: true);
        var next = voyage.Advance(1000);
        var loaded = restored.CaptureSnapshotForTests(next.GameTimeMs, SimulationSpeed.Speed0, voyage.MotionTime);
        Assert.Equal(next.ActiveVoyage, loaded.ActiveVoyage);
        Assert.Equal(next.Objects.Select(o => (o.ObjectId, o.SpeedKmS)), loaded.Objects.Select(o => (o.ObjectId, o.SpeedKmS)));
        foreach (var expected in next.Objects)
        {
            var actual = loaded.Objects.Single(o => o.ObjectId == expected.ObjectId);
            // Linear binary64 origins rebase on save; sub-millimetre rounding is not physical motion.
            Assert.InRange(Math.Abs(expected.X - actual.X), 0, 1e-6);
            Assert.InRange(Math.Abs(expected.Y - actual.Y), 0, 1e-6);
        }
    }
}
