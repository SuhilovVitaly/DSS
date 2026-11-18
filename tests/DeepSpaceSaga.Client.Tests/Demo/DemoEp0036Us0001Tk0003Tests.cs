using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Client.UI.Screens.Contracts;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0036Us0001Tk0003Tests
{
    private static BountyOrder Order(string eligibility, bool tracking = false) =>
        new("bounty:pirate:01", "SPC-0002", "pirate:01", 1500, tracking, eligibility);

    [Fact]
    public void StationBoard_ShowsUntrackedVerifiedVictory()
    {
        var row = Assert.Single(TargetBountyPresentation.Build(new([Order(BountyEligibility.VictoryConfirmed)]), "SPC-0002"));
        Assert.False(row.Order.Tracking);
        Assert.False(row.CanToggle);
        Assert.Contains("1500", row.Headline);
        Assert.Contains("SPC-0002", row.PaymentLocation);
    }

    [Fact]
    public void CancelRefresh_LeavesClaimEligibilityVisible()
    {
        var order = Order(BountyEligibility.VictoryConfirmed, true);
        Assert.True(Assert.Single(TargetBountyPresentation.Build(new([order]), null)).CanToggle);
        var row = Assert.Single(TargetBountyPresentation.Build(new([order with { Tracking = false }]), null));
        Assert.Equal(BountyEligibility.VictoryConfirmed, row.Order.Eligibility);
    }

    [Fact]
    public void UnavailableDeadTarget_NoAccept()
    {
        var row = Assert.Single(TargetBountyPresentation.Build(new([Order(BountyEligibility.Unavailable)]), "SPC-0002"));
        Assert.False(row.CanToggle);
        Assert.Empty(TargetBountyPresentation.Build(new([Order(BountyEligibility.TargetAvailable)]), "SPC-0006"));
    }

    [Fact]
    public async Task PendingAndReopen_DoNotResubmit()
    {
        var campaign = new DemoCampaignSnapshot(2, 2,
            Transport: new() { StationJobs = new([], [], 0, "SPC-0002") },
            Rewards: new() { Bounties = new([Order(BountyEligibility.TargetAvailable)]) });
        var completion = new TaskCompletionSource<DemoCampaignResponse>();
        int calls = 0;
        DemoCampaignCommand? sent = null;
        ValueTask<DemoCampaignResponse> Execute(DemoCampaignCommand command)
        {
            calls++;
            sent = command;
            return new(completion.Task);
        }
        var screen = new ContractsScreen(campaign: () => campaign, execute: Execute);
        screen.OnActivated();
        var pending = screen.TryToggleBounty("bounty:pirate:01");
        Assert.NotNull(pending);
        Assert.True(screen.IsAcceptPending);
        Assert.False(Assert.Single(screen.BountyRows).Order.Tracking);
        Assert.Null(screen.TryToggleBounty("bounty:pirate:01"));
        Assert.IsType<AcceptBountyCommand>(sent);
        completion.SetResult(new(sent!.CommandId, true, null, 3));
        await pending!;
        screen.OnDeactivated();
        new ContractsScreen(campaign: () => campaign, execute: Execute).OnActivated();
        Assert.Equal(1, calls);
    }
}
