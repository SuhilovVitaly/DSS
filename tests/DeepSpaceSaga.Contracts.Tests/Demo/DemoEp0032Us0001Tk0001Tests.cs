using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class DemoEp0032Us0001Tk0001Tests
{
    [Fact]
    public void Summary_ReusesSettlementAndSurvivesSerialization()
    {
        var receipt = new SortieSettlementSnapshot("battle", "ship", 3, false, ["lost"], 7, SortieExitKind.Aborted);
        var summary = new CampaignRoundtripSnapshot(receipt);
        Assert.Same(receipt, summary.Settlement);
        var loaded = JsonSerializer.Deserialize<CampaignRoundtripSnapshot>(JsonSerializer.Serialize(summary))!;
        Assert.Equal("battle", loaded.LastBattleId);
        Assert.Equal(3, loaded.SettlementRevision);
        Assert.False(loaded.CarrierDestroyed);
        Assert.Equal("lost", Assert.Single(loaded.LostDroneIds));
        Assert.Equal(7, loaded.ConsumedTorpedoes);
        Assert.Null(new DemoCompanySnapshot().Roundtrip);
    }
}
