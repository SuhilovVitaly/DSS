using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.Tests.Demo;

namespace DeepSpaceSaga.Engine.Tests.PostbattleResults;

public sealed class EP0042Us0001Tk0005Tests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BattleResultsLifecycle_SettlesOnceAndNeverAwardsDefeat(bool victory)
    {
        using var engine = BeginEncounter();
        var battle = engine.TacticalBattleSessionForTests!;
        var lostSide = victory ? "B" : "A";
        foreach (var drone in battle.Battle.Drones.Where(drone => drone.Side == lostSide))
            drone.Status = TacticalDroneStatus.Disabled;
        battle.Battle.CheckVictory();

        var attemptedExit = engine.ExitTacticalBattle(new("blocked-before-results", battle.Id));
        Assert.Equal(TacticalBattleRejectionCode.WrongPhase, attemptedExit.Code);
        Assert.False(engine.IsTacticalBattleActive);
        var awaiting = Assert.IsType<BattleResultsSnapshot>(Current(engine).BattleResults);
        Assert.Equal(BattleResultsStage.AwaitingOpen, awaiting.Stage);
        Assert.Equal(victory, awaiting.CanLoot);
        Assert.Equal(victory, Current(engine).Rewards?.Loot?.Phase == LootPhases.Open);
        Assert.Equal(victory ? TacticalBattleOutcomeCode.A : TacticalBattleOutcomeCode.B, awaiting.Outcome.Kind);
        Assert.Single(engine.CaptureSaveState().GameState.DemoCampaign!.EncounterResults);

        var premature = engine.ExecuteDemoCampaign(new AcknowledgeBattleResultsCommand("premature", Current(engine).Revision, battle.Id, true));
        Assert.False(premature.Applied);
        var open = new OpenBattleResultsCommand("open", Current(engine).Revision, battle.Id);
        var opened = engine.ExecuteDemoCampaign(open);
        Assert.True(opened.Applied);
        Assert.Equal(BattleResultsStage.Open, Current(engine).BattleResults?.Stage);
        Assert.Equal(opened, engine.ExecuteDemoCampaign(open));
        Assert.Equal(TacticalBattleRejectionCode.WrongPhase, engine.ExitTacticalBattle(new("blocked-in-results", battle.Id)).Code);
        Assert.Equal(DemoCampaignReasons.BattleActive, engine.ExecuteDemoCampaign(
            new FinishBattleLootCommand("legacy-finish", Current(engine).Revision, battle.Id, true)).Reason);

        var acknowledge = new AcknowledgeBattleResultsCommand("acknowledge", Current(engine).Revision, battle.Id, true);
        var acknowledged = engine.ExecuteDemoCampaign(acknowledge);
        Assert.True(acknowledged.Applied);
        Assert.Equal(acknowledged, engine.ExecuteDemoCampaign(acknowledge));
        Assert.Equal(BattleResultsStage.Acknowledged, Current(engine).BattleResults?.Stage);
        Assert.Null(engine.GetTacticalBattleState());
        Assert.Single(engine.CaptureSaveState().GameState.DemoCampaign!.EncounterResults);
    }

    private static SimulationEngine BeginEncounter()
    {
        var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        engine.LoadScenario(Ep0035Us3Tk2Tests.CapturedScenario(engine), true);
        var state = Current(engine);
        Assert.True(engine.ExecuteDemoCampaign(new DismissSensorContactReportRequest("report", state.Revision, state.PendingSensorReport!.BatchId)).Applied);
        state = Current(engine);
        Assert.True(engine.ExecuteDemoCampaign(new BeginEncounterBattleCommand("begin", state.Revision, state.WorldEncounter!.Binding.EncounterId)
        { RequireBattleResults = true }).Applied);
        return engine;
    }

    private static DemoCampaignSnapshot Current(SimulationEngine engine) => engine.CaptureSnapshot().DemoCampaign!;
}
