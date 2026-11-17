using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class Ep0035Us15Tk3Tests
{
    internal static SimulationEngine Begin()
    {
        var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        engine.LoadScenario(Ep0035Us3Tk2Tests.CapturedScenario(engine), true);
        var state = engine.CaptureSnapshot().DemoCampaign!;
        Assert.True(engine.ExecuteDemoCampaign(new DismissSensorContactReportRequest("ack", state.Revision, state.PendingSensorReport!.BatchId)).Applied);
        state = engine.CaptureSnapshot().DemoCampaign!;
        Assert.True(engine.ExecuteDemoCampaign(new BeginEncounterBattleCommand("begin", state.Revision, state.WorldEncounter!.Binding.EncounterId)).Applied);
        return engine;
    }

    [Fact]
    public void TimeoutRetiresOnlyParticipatingGroupAndPreservesActualDamageExactlyOnce()
    {
        using var engine = Begin();
        var session = engine.TacticalBattleSessionForTests!;
        var enemy = session.Battle.Carriers.First(c => c.Side == "B");
        enemy.Hull -= 27;
        session.Battle.EndWithTimeout(1);
        var response = engine.ExitTacticalBattle(new("finish", session.Id));
        Assert.True(response.Accepted);
        var saved = engine.CaptureSaveState();
        var campaign = saved.GameState.DemoCampaign!;
        var result = Assert.Single(campaign.EncounterResults);
        Assert.False(result.PlayerWon);
        Assert.Equal(enemy.Hull, result.Units.Single(u => u.Id == enemy.Id).Hull);
        Assert.Equal(TacticalBattleOutcomeCode.Timeout, result.Outcome.Kind);
        Assert.Single(campaign.PirateWorld!.Groups.Where(g => g.Retired));
        Assert.All(campaign.PirateWorld.Groups.Where(g => !g.Retired), g => Assert.All(g.Members, m => Assert.False(m.IsDestroyed)));
        Assert.Equal(WorldEncounterPhases.Finished, campaign.WorldEncounter!.Phase);
        Assert.Equal(response, engine.ExitTacticalBattle(new("finish", session.Id)));
        Assert.Single(engine.CaptureSaveState().GameState.DemoCampaign!.EncounterResults);
        Assert.False(response.Settlement!.ShipDestroyed);
    }
}
