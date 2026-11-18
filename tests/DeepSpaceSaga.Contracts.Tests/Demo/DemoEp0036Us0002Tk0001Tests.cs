using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class DemoEp0036Us0002Tk0001Tests
{
    [Fact]
    public void Claim_RequiresBountyIdNotAcceptedContract()
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(new ClaimBountyCommand("claim", 8, "bounty:pirate:01")));
        Assert.Equal(new[] { "BountyId", "CommandId", "ExpectedRevision" },
            json.RootElement.EnumerateObject().Select(p => p.Name).Order());
    }

    [Fact]
    public void Projection_RoundTripsPostingAndBattleIdentity()
    {
        var due = new BountyPaymentEntry("b", "s", "g", "battle:g", BountyPaymentStates.Claimable, 1500, 0, null, null, null);
        var paid = due with { State = BountyPaymentStates.Paid, DueTokens = 0, PaidTokens = 1500, PostingId = "bounty:b", PaidAtGameTimeMs = 20 };
        var snapshot = new DemoRewardsSnapshot { BountyPayment = new([due, paid]) };
        var loaded = JsonSerializer.Deserialize<DemoRewardsSnapshot>(JsonSerializer.Serialize(snapshot))!;
        Assert.Equal(new[] { due, paid }, loaded.BountyPayment!.Entries);
        Assert.Null(JsonSerializer.Deserialize<DemoRewardsSnapshot>("{}")!.BountyPayment);
    }
}
