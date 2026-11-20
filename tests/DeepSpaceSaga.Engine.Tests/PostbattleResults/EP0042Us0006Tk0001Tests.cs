using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.Tests.Demo;

namespace DeepSpaceSaga.Engine.Tests.PostbattleResults;

[Collection("Demo process debug")]
public sealed class EP0042Us0006Tk0001Tests
{
    [Fact]
    public void BattleResultsSave_LegacyOpenClosedAndExtractedWaiting()
    {
        SimulationEngine.SetLootDebug(new("success", 0, Attempts: "success"));
        try
        {
            using var engine = DemoEp0036Us0007Tk0003Tests.Won();
            long time = 100;
            engine.LootClockMilliseconds = () => time;
            var legacy = engine.CaptureSaveState();
            Assert.Null(legacy.GameState.DemoCampaign!.BattleResults);
            engine.LoadScenario(RoundTrip(legacy), true);
            Assert.NotNull(engine.GetTacticalBattleState());
            EP0042Us0004Tk0001Tests.FillCargoLeaving(engine, 0);
            var loot = Current().Rewards!.Loot!;
            var wreck = loot.Objects.First(source => source.Scrap is not null);
            var battleId = loot.BattleId;
            Assert.True(engine.ExecuteDemoCampaign(new OpenBattleResultsCommand("open", Current().Revision, battleId)).Applied);
            var target = new LootRowKey(wreck.ObjectId, LootRowKind.Scrap, wreck.Scrap!.ItemTypeId);
            Assert.True(engine.ExecuteDemoCampaign(new StartLootOperationsCommand("start", Current().Revision, battleId, target)).Applied);
            Assert.Throws<InvalidOperationException>(() => engine.CaptureSaveState());
            Assert.Throws<InvalidOperationException>(() => engine.LoadScenario(legacy, true));
            time += 10000;
            Assert.True(engine.ExecuteDemoCampaign(new AdvanceLootOperationsCommand("complete", Current().Revision, battleId)).Applied);

            var before = engine.CaptureSaveState();
            var campaign = before.GameState.DemoCampaign!;
            Assert.Null(campaign.BattleResults!.Map);
            Assert.Equal(BattleResultsStage.Open, campaign.BattleResults.Stage);
            engine.LoadScenario(RoundTrip(before), true);
            Assert.Equal(LootAttemptStates.ExtractedWaiting, Current().Rewards!.Loot!.Objects.Single(source => source.ObjectId == wreck.ObjectId).Scrap!.State);
            Assert.Equal(campaign.LootSession!.DrawCount, engine.CaptureSaveState().GameState.DemoCampaign!.LootSession!.DrawCount);
            Assert.Empty(Current().Rewards!.Loot!.Operations);
            Assert.Equal(BattleResultsStage.Open, Current().BattleResults!.Stage);
            Assert.True(engine.ExecuteDemoCampaign(new AcknowledgeBattleResultsCommand("leave", Current().Revision, battleId, true)).Applied);
            var closed = engine.CaptureSaveState();
            Assert.Null(closed.GameState.DemoCampaign!.BattleResults);
            engine.LoadScenario(RoundTrip(closed), true);
            Assert.Null(engine.GetTacticalBattleState());

            DemoCampaignSnapshot Current() => engine.CaptureSnapshot().DemoCampaign!;
        }
        finally { SimulationEngine.SetLootDebug(new("reset", 0)); }
    }

    [Fact]
    public void BattleResultsSave_TimeoutAndInvalidBinding()
    {
        using var engine = EP0042Us0001Tk0005Tests.BeginEncounter();
        var battle = engine.TacticalBattleSessionForTests!;
        battle.Battle.EndWithTimeout(8);
        Assert.Equal(TacticalBattleRejectionCode.WrongPhase, engine.ExitTacticalBattle(new("blocked", battle.Id)).Code);
        var snapshot = engine.CaptureSnapshot().DemoCampaign!;
        Assert.True(engine.ExecuteDemoCampaign(new OpenBattleResultsCommand("open", snapshot.Revision, battle.Id)).Applied);
        var good = engine.CaptureSaveState();
        var campaign = good.GameState.DemoCampaign!;
        Assert.NotNull(campaign.BattleResults!.Map);
        engine.LoadScenario(RoundTrip(good), true);
        Assert.False(engine.IsTacticalBattleActive);
        Assert.Equal(TacticalBattleOutcomeCode.Timeout, engine.GetTacticalBattleState()!.Outcome!.Kind);
        var bad = good with
        {
            GameState = good.GameState with
            {
                DemoCampaign = campaign with { BattleResults = campaign.BattleResults with { BattleId = "wrong" } }
            }
        };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(bad, true));
        Assert.Equal(battle.Id, engine.GetTacticalBattleState()!.BattleId);
        Assert.Equal(BattleResultsStage.Open, engine.CaptureSnapshot().DemoCampaign!.BattleResults!.Stage);
    }

    private static ScenarioFile RoundTrip(ScenarioFile save) => ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, true);
}
