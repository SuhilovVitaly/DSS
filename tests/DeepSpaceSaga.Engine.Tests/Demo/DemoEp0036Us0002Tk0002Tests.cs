using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0036Us0002Tk0002Tests
{
    internal static void Dock(SimulationEngine engine, string station = "SPC-0002", long? credits = null)
    {
        var save = engine.CaptureSaveState();
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                PlayerTokens = credits ?? save.GameState.PlayerTokens,
                SpaceObjects = save.GameState.SpaceObjects.Select(s => s.ObjectId != save.GameState.PlayerShipObjectId ? s
                    : s with { IsDocked = true, DockedStationObjectId = station, FirstPortFeeGameTimeMs = 0, NextPortFeeDueGameTimeMs = GameCalendar.DayMs }).ToArray()
            }
        }, true);
    }

    internal static DemoCampaignResponse Claim(SimulationEngine engine, string id = "claim", string bounty = "bounty:pirate:01") =>
        engine.ExecuteDemoCampaign(new ClaimBountyCommand(id, engine.CaptureSnapshot().DemoCampaign!.Revision, bounty));

    private static SimulationEngine Won()
    {
        var engine = Ep0035Us15Tk3Tests.Begin();
        DemoEp0036Us0001Tk0002Tests.Finish(engine, true);
        Dock(engine);
        return engine;
    }

    [Fact]
    public void UnacceptedVictory_ClaimAtIssuerPaysOnce()
    {
        using var engine = Won();
        long before = engine.PlayerCredits;
        Assert.True(Claim(engine).Applied);
        Assert.Equal(before + 1500, engine.PlayerCredits);
        Assert.False(Claim(engine, "again").Applied);
        Assert.Equal(before + 1500, engine.PlayerCredits);
        var posting = Assert.Single(engine.CaptureSaveState().GameState.DemoCampaign!.BountyPayments);
        Assert.Equal("battle:pirate:01", posting.BattleId);
        Assert.Equal("bounty:bounty:pirate:01", posting.PostingId);
    }

    [Fact]
    public void CancelledThenVictory_ClaimPaysOnce()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        Assert.True(engine.ExecuteDemoCampaign(new AcceptBountyCommand("a", engine.CaptureSnapshot().DemoCampaign!.Revision, "bounty:pirate:01")).Applied);
        Assert.True(engine.ExecuteDemoCampaign(new CancelBountyCommand("c", engine.CaptureSnapshot().DemoCampaign!.Revision, "bounty:pirate:01")).Applied);
        engine.LoadScenario(Ep0035Us3Tk2Tests.CapturedScenario(engine), true);
        var state = engine.CaptureSnapshot().DemoCampaign!;
        Assert.True(engine.ExecuteDemoCampaign(new DismissSensorContactReportRequest("ack", state.Revision, state.PendingSensorReport!.BatchId)).Applied);
        state = engine.CaptureSnapshot().DemoCampaign!;
        Assert.True(engine.ExecuteDemoCampaign(new BeginEncounterBattleCommand("begin", state.Revision, state.WorldEncounter!.Binding.EncounterId)).Applied);
        DemoEp0036Us0001Tk0002Tests.Finish(engine, true);
        Dock(engine);
        Assert.True(Claim(engine).Applied);
        Assert.False(Claim(engine, "again").Applied);
    }

    [Fact]
    public void WrongGroupOrDefeat_NoPayment()
    {
        using var engine = Won();
        long before = engine.PlayerCredits;
        Assert.Equal(BountyPaymentReasons.NoVerifiedVictory, Claim(engine, bounty: "bounty:pirate:02").Reason);
        Assert.Equal(before, engine.PlayerCredits);
        using var timeout = Ep0035Us15Tk3Tests.Begin();
        DemoEp0036Us0001Tk0002Tests.Finish(timeout, false);
        Dock(timeout);
        Assert.Equal(BountyPaymentReasons.NoVerifiedVictory, Claim(timeout).Reason);
        Assert.Empty(timeout.CaptureSaveState().GameState.DemoCampaign!.BountyPayments);
    }

    [Fact]
    public void WrongStation_NoMutation()
    {
        using var engine = Won();
        Dock(engine, "SPC-0006");
        long before = engine.PlayerCredits;
        Assert.Equal(BountyPaymentReasons.WrongStation, Claim(engine).Reason);
        Assert.Equal(before, engine.PlayerCredits);
        Assert.Empty(engine.CaptureSaveState().GameState.DemoCampaign!.BountyPayments);
    }

    [Fact]
    public void ReacceptNewCommandAndLoad_NoSecondPayment()
    {
        using var engine = Won();
        Assert.True(Claim(engine).Applied);
        var save = engine.CaptureSaveState();
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, true), true);
        Assert.True(engine.ExecuteDemoCampaign(new CancelBountyCommand("cancel", engine.CaptureSnapshot().DemoCampaign!.Revision, "bounty:pirate:01")).Applied);
        Assert.False(engine.ExecuteDemoCampaign(new AcceptBountyCommand("accept", engine.CaptureSnapshot().DemoCampaign!.Revision, "bounty:pirate:01")).Applied);
        Assert.Equal(TargetBountyReasons.AlreadyPaid, Claim(engine, "claim-after-load").Reason);
        Assert.Equal(save.GameState.PlayerTokens, engine.PlayerCredits);
    }

    [Fact]
    public void MalformedDuplicatePayment_LoadRejected()
    {
        using var engine = Won();
        Assert.True(Claim(engine).Applied);
        var save = engine.CaptureSaveState();
        var state = save.GameState.DemoCampaign!;
        foreach (var corrupt in new[] { state with { BountyPayments = state.BountyPayments.Add(state.BountyPayments[0]) },
            state with { BountyPayments = [state.BountyPayments[0] with { RewardTokens = 1 }] },
            state with { BountyPayments = [state.BountyPayments[0] with { BattleId = "battle:pirate:02" }] } })
            Assert.Throws<ScenarioException>(() => engine.LoadScenario(save with { GameState = save.GameState with { DemoCampaign = corrupt } }, true));
        Assert.Equal(save.GameState.PlayerTokens, engine.PlayerCredits);
    }

    [Fact]
    public void CreditOverflow_NoPartialPosting()
    {
        using var engine = Won();
        Dock(engine, credits: long.MaxValue);
        Assert.Equal(BountyPaymentReasons.CreditOverflow, Claim(engine).Reason);
        Assert.Equal(long.MaxValue, engine.PlayerCredits);
        Assert.Empty(engine.CaptureSaveState().GameState.DemoCampaign!.BountyPayments);
    }
}
