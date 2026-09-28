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
        var bad = saved with { GameState = saved.GameState with
        {
            VoyageState = new VoyageStateData(VoyagePhases.InTransit, "bad", "SPC-0002", "missing", 0, 100),
        } };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(bad, isSave: true));
        var after = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0);
        Assert.Equal(before.Objects.Select(o => (o.ObjectId, o.X, o.Y, o.IsDocked, o.DockedStationObjectId)),
            after.Objects.Select(o => (o.ObjectId, o.X, o.Y, o.IsDocked, o.DockedStationObjectId)));
        Assert.Equal(before.Voyage?.Phase, after.Voyage?.Phase);
        Assert.Equal(before.Voyage?.BlockReasonCode, after.Voyage?.BlockReasonCode);
        Assert.Equal(before.Voyage?.RouteOptions.ToArray(), after.Voyage?.RouteOptions.ToArray());
    }
}
