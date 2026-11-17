using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class DemoEp0036Us0001Tk0001Tests
{
    [Fact]
    public void Commands_HaveNoClientPriceOrOutcome()
    {
        DemoCampaignCommand[] commands = [new AcceptBountyCommand("accept", 3, "bounty:pirate:01"),
            new CancelBountyCommand("cancel", 4, "bounty:pirate:01")];
        foreach (var command in commands)
        {
            using var json = JsonDocument.Parse(JsonSerializer.Serialize(command, command.GetType()));
            Assert.Equal(new[] { "BountyId", "CommandId", "ExpectedRevision" },
                json.RootElement.EnumerateObject().Select(p => p.Name).Order().ToArray());
        }
    }

    [Fact]
    public void Tracking_IndependentFromEligibility()
    {
        var claimable = new BountyOrder("bounty:pirate:01", "SPC-0002", "pirate:01", 1500,
            false, BountyEligibility.VictoryConfirmed, "battle:pirate:01");
        var tracking = claimable with { Tracking = true };
        Assert.Equal(claimable.Eligibility, tracking.Eligibility);
        Assert.Equal(claimable.VictoryBattleId, tracking.VictoryBattleId);
        Assert.Null(JsonSerializer.Deserialize<DemoRewardsSnapshot>("{}")!.Bounties);
    }

    [Fact]
    public void Snapshot_RoundTripsIssuerTargetAndFrozenReward()
    {
        var order = new BountyOrder("bounty:pirate:01", "SPC-0002", "pirate:01", 1500,
            true, BountyEligibility.TargetAvailable, SearchRegion: "region.demo");
        var snapshot = new DemoCampaignSnapshot(7, 9,
            Rewards: new DemoRewardsSnapshot { Bounties = new([order]) });
        var loaded = JsonSerializer.Deserialize<DemoCampaignSnapshot>(JsonSerializer.Serialize(snapshot))!;
        Assert.Equal(order, Assert.Single(loaded.Rewards!.Bounties!.Orders));
        Assert.Equal(7, loaded.Revision);
    }
}
