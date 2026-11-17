using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0032Us0004Tk0003Tests
{
    [Fact]
    public void DemoMaterializesOnceAndLoadRetainsFiveStationsAndStarter()
    {
        string root = DemoEp0032Us0001Tk0002Tests.ContentRoot;
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(root + "/Settings.json", root + "/Scenarios/Demo/scenario.json");
        var before = engine.CaptureSnapshot();
        Assert.Equal(5, before.Objects.Count(o => o.ObjectType == Contracts.SpaceObjectType.Station));
        Assert.Equal(5, before.DemoCampaign!.Start!.Stations.Length);
        Assert.Equal(4, before.CarrierOperations!.OwnedDrones.Length);
        var start = DemoEp0032Us0001Tk0002Tests.Start(engine, "demo-tutorial");
        engine.ExitTacticalBattle(new("exit", start.State!.BattleId));
        string directory = "D:/DeepSpaceSaga/artifacts/validation/ep-0032/demo-load-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "save.json");
            File.WriteAllText(path, ScenarioLoader.Serialize(engine.CaptureSaveState()));
            using var restored = EngineContentLoader.CreateEngineFromSaveFile(root + "/Settings.json", path);
            var after = restored.CaptureSnapshot();
            Assert.Equal(before.Objects.Where(o => o.ObjectType == Contracts.SpaceObjectType.Station).Select(o => (o.ObjectId, o.X, o.Y)),
                after.Objects.Where(o => o.ObjectType == Contracts.SpaceObjectType.Station).Select(o => (o.ObjectId, o.X, o.Y)));
            Assert.Equal(before.CarrierOperations.Crew.Select(p => p.Id), after.CarrierOperations!.Crew.Select(p => p.Id));
            Assert.Equal("demo-tutorial", after.DemoCampaign!.Company!.Roundtrip!.LastBattleId);
            Assert.Equal(before.DemoCampaign.Start.Seed, after.DemoCampaign.Start!.Seed);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
