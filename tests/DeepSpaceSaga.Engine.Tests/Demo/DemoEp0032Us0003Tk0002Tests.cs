using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0032Us0003Tk0002Tests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OutcomeSurvivesSaveAndDistinguishesEmptyWingFromDestroyedCarrier(bool destroyCarrier)
    {
        using var engine = DemoEp0032Us0001Tk0002Tests.Tutorial();
        var start = DemoEp0032Us0001Tk0002Tests.Start(engine, "outcome");
        var session = engine.TacticalBattleSessionForTests!;
        foreach (var drone in session.Battle.Drones.Where(d => d.Side == session.PlayerSide)) drone.Status = TacticalDroneStatus.Destroyed;
        if (destroyCarrier) session.Battle.Carriers.Single(c => c.Side == session.PlayerSide).Hull = 0;
        engine.ExitTacticalBattle(new("exit", start.State!.BattleId));
        var before = engine.CaptureSnapshot();
        var outcome = before.DemoCampaign!.Company!.Outcome!;
        Assert.Equal(!destroyCarrier, outcome.ContinuationAvailable);
        Assert.Equal(destroyCarrier ? "carrier_destroyed" : "no_ready_wing", outcome.RecoveryReason);
        Assert.Equal(4, before.DemoCampaign.Company.PilotLoss!.Memorial.Length);
        var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveState()), true, true);
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(DemoEp0032Us0001Tk0002Tests.ContentRoot + "/Settings.json", out _, out _);
        using var restored = new SimulationEngine(registry);
        restored.LoadScenario(save, isSave: true);
        Assert.Equal(outcome, restored.CaptureSnapshot().DemoCampaign!.Company!.Outcome);
        Assert.Equal(before.CarrierOperations!.HullCurrent, restored.CaptureSnapshot().CarrierOperations!.HullCurrent);
        Assert.All(restored.CaptureSnapshot().CarrierOperations!.OwnedDrones, d => Assert.Contains("destroyed", d.ReadinessReasons));
        Assert.Equal(4, restored.CaptureSnapshot().DemoCampaign!.Company!.PilotLoss!.Memorial.Length);
        var revision = restored.CaptureSnapshot().DemoCampaign!.Revision;
        Assert.Equal(destroyCarrier ? "carrier_destroyed" : "unsupported", restored.ExecuteDemoCampaign(new Probe("probe", revision)).Reason);
    }

    private sealed record Probe(string CommandId, long ExpectedRevision) : DemoCampaignCommand(CommandId, ExpectedRevision);
}
