using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0032Us0001Tk0002Tests
{
    internal const string ContentRoot = "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client";
    internal static SimulationEngine Tutorial() => EngineContentLoader.CreateEngineFromScenarioFile(
        ContentRoot + "/Settings.json", ContentRoot + "/Scenarios/Tutorial/scenario.json");

    internal static TacticalBattleResponse Start(SimulationEngine engine, string identity)
    {
        engine.SetSpeed(SimulationSpeed.Speed0);
        var carrier = engine.CaptureSnapshot().CarrierOperations!;
        var response = engine.StartTacticalBattle(new(identity, 1, "initial")
        { SourceShipId = carrier.ShipId, ExpectedCarrierRevision = carrier.Revision });
        Assert.True(response.Accepted, response.Code.ToString());
        return response;
    }

    [Fact]
    public void HappyPath_MatchesStory()
    {
        using var engine = Tutorial();
        var before = engine.CaptureSnapshot().CarrierOperations!;
        var start = Start(engine, "tutorial");
        var session = engine.TacticalBattleSessionForTests!;
        var drone = session.Battle.Drones.First(d => d.Side == session.PlayerSide);
        string droneId = drone.Id;
        drone.AmmoCurrent -= 2;
        drone.Hull -= 1;
        var exit = engine.ExitTacticalBattle(new("exit", start.State!.BattleId));
        var world = engine.CaptureSnapshot();
        Assert.Equal(exit.Settlement, world.DemoCampaign!.Company!.Roundtrip!.Settlement);
        Assert.Equal(2, world.DemoCampaign.Company.Roundtrip.ConsumedTorpedoes);
        var after = world.CarrierOperations!;
        Assert.Equal(before.Crew.Select(c => c.CrewId), after.Crew.Select(c => c.CrewId));
        Assert.Equal(before.OwnedDrones.Select(d => d.Id), after.OwnedDrones.Select(d => d.Id));
        var rearm = engine.RearmDrone(new("rearm", before.ShipId, droneId, 1, after.Revision));
        Assert.True(rearm.Accepted, rearm.Reason);
        Start(engine, "second");
        var next = engine.TacticalBattleSessionForTests!.Battle.Drones.Single(d => d.Id == droneId);
        Assert.Equal(drone.Hull, next.Hull);
        Assert.Equal(drone.AmmoCurrent + 1, next.AmmoCurrent);
    }

    [Fact]
    public void InvalidOrDuplicate_NoPartialMutation()
    {
        using var engine = Tutorial();
        var start = Start(engine, "first");
        var request = new ExitTacticalBattleRequest("exit", start.State!.BattleId);
        var first = engine.ExitTacticalBattle(request);
        var before = ScenarioLoader.Serialize(engine.CaptureSaveState());
        Assert.Equal(first, engine.ExitTacticalBattle(request));
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }

    [Fact]
    public void SaveLoad_PreservesOutcome()
    {
        using var engine = Tutorial();
        var start = Start(engine, "saved");
        engine.ExitTacticalBattle(new("exit", start.State!.BattleId));
        var before = engine.CaptureSnapshot();
        var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveState()), true, true);
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(ContentRoot + "/Settings.json", out _, out _);
        using var restored = new SimulationEngine(registry);
        restored.LoadScenario(save, isSave: true);
        var after = restored.CaptureSnapshot();
        Assert.Equal(before.DemoCampaign!.CommitSequence, after.DemoCampaign!.CommitSequence);
        Assert.Equal(before.DemoCampaign.Company!.Roundtrip!.LastBattleId, after.DemoCampaign.Company!.Roundtrip!.LastBattleId);
        Assert.Equal(before.CarrierOperations!.Revision, after.CarrierOperations!.Revision);
        Assert.False(restored.ExitTacticalBattle(new("again", start.State.BattleId)).Accepted);
        Assert.Equal(after.DemoCampaign.CommitSequence, restored.CaptureSnapshot().DemoCampaign!.CommitSequence);
    }
}
