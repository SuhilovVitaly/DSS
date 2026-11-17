using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class Ep0035Us16Tk1Tests
{
    [Fact]
    public void PendingCheckpointRestartsExactBattleBeforeAnyWorldCanResume()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        engine.LoadScenario(Ep0035Us3Tk2Tests.CapturedScenario(engine), true);
        var campaign = engine.CaptureSnapshot().DemoCampaign!;
        engine.ExecuteDemoCampaign(new DismissSensorContactReportRequest("ack", campaign.Revision, campaign.PendingSensorReport!.BatchId));
        campaign = engine.CaptureSnapshot().DemoCampaign!;
        var begin = new BeginEncounterBattleCommand("begin", campaign.Revision, campaign.WorldEncounter!.Binding.EncounterId);
        var checkpoint = engine.CapturePendingEncounterCheckpoint(begin);
        Assert.False(engine.IsTacticalBattleActive);
        Assert.True(engine.ExecuteDemoCampaign(begin).Applied);
        string original = JsonSerializer.Serialize(engine.GetTacticalBattleState());
        using var loaded = DemoEp0033Us0001Tk0003Tests.Demo();
        loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(checkpoint), true, true), true);
        Assert.True(loaded.IsTacticalBattleActive);
        Assert.Equal(original, JsonSerializer.Serialize(loaded.GetTacticalBattleState()));
        Assert.Equal(SimulationEngine.TacticalBattleActiveReason, loaded.TrySetSpeed(SimulationSpeed.Speed1));
        Assert.Throws<InvalidOperationException>(() => loaded.CaptureSaveState());
        Assert.Throws<InvalidOperationException>(() => loaded.LoadScenario(checkpoint, true));
    }
}
