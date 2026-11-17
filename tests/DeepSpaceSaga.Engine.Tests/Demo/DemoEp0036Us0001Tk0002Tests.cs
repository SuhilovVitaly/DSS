using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0036Us0001Tk0002Tests
{
    internal static BountyOrder Order(SimulationEngine engine) => engine.CaptureSnapshot()
        .DemoCampaign!.Rewards!.Bounties!.Orders.Single(b => b.TargetGroupId == "pirate:01");

    internal static void Finish(SimulationEngine engine, bool victory)
    {
        var session = engine.TacticalBattleSessionForTests!;
        if (victory)
        {
            foreach (var drone in session.Battle.Drones.Where(d => d.Side == "B"))
                drone.Status = TacticalDroneStatus.Disabled;
            session.Battle.CheckVictory();
        }
        else session.Battle.EndWithTimeout(1);
        Assert.True(engine.ExitTacticalBattle(new("finish", session.Id)).Accepted);
    }

    [Fact]
    public void Cancel_RemovesTrackingButKeepsEntitlement()
    {
        using var engine = Ep0035Us15Tk3Tests.Begin();
        Finish(engine, true);
        var before = Order(engine);
        Assert.True(engine.ExecuteDemoCampaign(new CancelBountyCommand("cancel", engine.CaptureSnapshot().DemoCampaign!.Revision, before.BountyId)).Applied);
        var after = Order(engine);
        Assert.False(after.Tracking);
        Assert.Equal(BountyEligibility.VictoryConfirmed, after.Eligibility);
        Assert.Equal(before.VictoryBattleId, after.VictoryBattleId);
    }

    [Fact]
    public void VictoryBeforeAccept_IsClaimable()
    {
        using var engine = Ep0035Us15Tk3Tests.Begin();
        Finish(engine, true);
        Assert.False(Order(engine).Tracking);
        Assert.Equal(BountyEligibility.VictoryConfirmed, Order(engine).Eligibility);
        var save = engine.CaptureSaveState();
        // An old Save22 has no bounty section. Migration matches the existing validated victory.
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                DemoCampaign = save.GameState.DemoCampaign! with
                { BountyOrders = [], BountyCatalogFingerprint = null }
            }
        }, true);
        Assert.Equal(BountyEligibility.VictoryConfirmed, Order(engine).Eligibility);
    }

    [Fact]
    public void DeadTargetWithoutPlayerVictory_NotActive()
    {
        using var engine = Ep0035Us15Tk3Tests.Begin();
        Finish(engine, false);
        Assert.Equal(BountyEligibility.Unavailable, Order(engine).Eligibility);
        Assert.False(engine.ExecuteDemoCampaign(new AcceptBountyCommand("track", engine.CaptureSnapshot().DemoCampaign!.Revision, Order(engine).BountyId)).Applied);
    }

    [Fact]
    public void RepeatedAcceptCancel_NoCopiesOrMoney()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var money = engine.CaptureSaveState().GameState.PlayerTokens;
        for (int i = 0; i < 4; i++)
        {
            DemoCampaignCommand command = i % 2 == 0
                ? new AcceptBountyCommand("track:" + i, engine.CaptureSnapshot().DemoCampaign!.Revision, Order(engine).BountyId)
                : new CancelBountyCommand("track:" + i, engine.CaptureSnapshot().DemoCampaign!.Revision, Order(engine).BountyId);
            var response = engine.ExecuteDemoCampaign(command);
            Assert.True(response.Applied);
            Assert.Equal(response, engine.ExecuteDemoCampaign(command));
        }
        Assert.Equal(2, engine.CaptureSaveState().GameState.DemoCampaign!.BountyOrders.Length);
        Assert.Equal(money, engine.CaptureSaveState().GameState.PlayerTokens);
    }

    [Fact]
    public void SaveLoad_FreezesIssuerTargetRewardAndTracking()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        Assert.True(engine.ExecuteDemoCampaign(new AcceptBountyCommand("track", engine.CaptureSnapshot().DemoCampaign!.Revision, Order(engine).BountyId)).Applied);
        var before = Order(engine);
        var save = engine.CaptureSaveState();
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, true), true);
        Assert.Equal(before, Order(engine));
        var state = save.GameState.DemoCampaign!;
        var corrupt = save with
        {
            GameState = save.GameState with
            {
                DemoCampaign = state with
                { BountyOrders = state.BountyOrders.SetItem(0, state.BountyOrders[0] with { IssuerStationId = "SPC-0006" }) }
            }
        };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(corrupt, true));
        Assert.Equal(before, Order(engine));
    }
}
