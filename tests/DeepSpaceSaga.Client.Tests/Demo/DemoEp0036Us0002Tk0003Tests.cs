using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Contracts;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.TacticalBattle;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0036Us0002Tk0003Tests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnacceptedOrCancelledVictory_ClaimThroughSessionAndReopen(bool cancelled)
    {
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(
            Path.Combine(AppContext.BaseDirectory, "Settings.json"), Path.Combine(AppContext.BaseDirectory, "Scenarios/Demo/scenario.json"));
        if (cancelled)
        {
            Assert.True(engine.ExecuteDemoCampaign(new AcceptBountyCommand("track", engine.CaptureSnapshot().DemoCampaign!.Revision, "bounty:pirate:01")).Applied);
            Assert.True(engine.ExecuteDemoCampaign(new CancelBountyCommand("cancel", engine.CaptureSnapshot().DemoCampaign!.Revision, "bounty:pirate:01")).Applied);
        }
        var save = engine.CaptureSaveState();
        var state = save.GameState.DemoCampaign!;
        var world = state.PirateWorld!;
        var group = world.Groups[0];
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(s => s.ObjectId != save.GameState.PlayerShipObjectId ? s : s with
                { PositionX = group.X, PositionY = group.Y + 1500, SpeedMps = 0, Orbit = null, IsDocked = false, DockedStationObjectId = null }).ToArray(),
                DemoCampaign = state with
                {
                    PirateWorld = world with
                    {
                        Groups = world.Groups.SetItem(0, group with
                        { Phase = "captured", CapturedMotionTimeMs = world.LastMotionTimeMs, MotionPlan = null })
                    }
                }
            }
        }, true);
        var campaign = engine.CaptureSnapshot().DemoCampaign!;
        Assert.True(engine.ExecuteDemoCampaign(new DismissSensorContactReportRequest("ack", campaign.Revision, campaign.PendingSensorReport!.BatchId)).Applied);
        campaign = engine.CaptureSnapshot().DemoCampaign!;
        Assert.True(engine.ExecuteDemoCampaign(new BeginEncounterBattleCommand("begin", campaign.Revision, campaign.WorldEncounter!.Binding.EncounterId)).Applied);
        var battle = engine.TacticalBattleSessionForTests!;
        foreach (var drone in battle.Battle.Drones.Where(d => d.Side == "B")) drone.Status = TacticalDroneStatus.Disabled;
        battle.Battle.CheckVictory();
        Assert.True(engine.ExitTacticalBattle(new("finish", battle.Id)).Accepted);
        save = engine.CaptureSaveState();
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(s => s.ObjectId != save.GameState.PlayerShipObjectId ? s : s with
                { IsDocked = true, DockedStationObjectId = "SPC-0002", FirstPortFeeGameTimeMs = 0, NextPortFeeDueGameTimeMs = GameCalendar.DayMs }).ToArray()
            }
        }, true);
        long before = engine.PlayerCredits;
        await using var connection = new LocalGameSessionConnection(engine);
        await using var handle = new GameSessionHandle(connection);
        await WaitFor(() => handle.DemoCampaign?.Rewards?.BountyPayment is not null);
        var screen = new ContractsScreen(handle.Buffer, () => handle.DemoCampaign, c => handle.ExecuteDemoCampaignAsync(c));
        Assert.True(screen.PaymentView("bounty:pirate:01")!.CanClaim);
        var request = screen.TryClaimBounty("bounty:pirate:01");
        Assert.NotNull(request);
        await request!;
        await WaitFor(() => handle.DemoCampaign!.Rewards!.BountyPayment!.Entries.Any(p => p.State == BountyPaymentStates.Paid));
        var reopened = new ContractsScreen(handle.Buffer, () => handle.DemoCampaign, c => handle.ExecuteDemoCampaignAsync(c));
        reopened.OnActivated();
        Assert.False(reopened.PaymentView("bounty:pirate:01")!.CanClaim);
        Assert.Null(reopened.TryClaimBounty("bounty:pirate:01"));
        Assert.Equal(before + 1500, engine.PlayerCredits);
    }

    [Fact]
    public void WrongStationAndPaid_AreNotClaimButtons()
    {
        var due = new BountyPaymentEntry("b", "issuer", "target", "battle", BountyPaymentStates.Claimable,
            1500, 0, null, null, BountyPaymentReasons.WrongStation);
        Assert.False(BountyPaymentPresentation.Build(new([due]), "b")!.CanClaim);
        Assert.False(BountyPaymentPresentation.Build(new([due with { State = BountyPaymentStates.Paid, Reason = null }]), "b")!.CanClaim);
    }

    private static async Task WaitFor(Func<bool> predicate)
    {
        var until = DateTime.UtcNow.AddSeconds(15);
        while (!predicate()) { Assert.True(DateTime.UtcNow < until, "Session projection timeout"); await Task.Delay(20); }
    }
}
