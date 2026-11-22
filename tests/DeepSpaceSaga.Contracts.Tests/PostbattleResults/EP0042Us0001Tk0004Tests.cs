using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public sealed class EP0042Us0001Tk0004Tests
{
    [Theory]
    [InlineData(TacticalBattleOutcomeCode.A, true, false)]
    [InlineData(TacticalBattleOutcomeCode.B, false, true)]
    [InlineData(TacticalBattleOutcomeCode.Draw, false, null)]
    [InlineData(TacticalBattleOutcomeCode.Timeout, false, false)]
    public void BattleResultsContract_FourOutcomesAndLegacyDefaults(TacticalBattleOutcomeCode kind,
        bool canLoot, bool? playerCarrierDestroyed)
    {
        var legacy = JsonSerializer.Deserialize<DemoCampaignSnapshot>("""{"Revision":4,"CommitSequence":2}""")!;
        Assert.Null(legacy.BattleResults);

        var outcome = new TacticalOutcomeSnapshot(kind, [], 8, [new("A", 2)],
            new(3, 2, 0, 0, 0, 0, 0, 0, 0, null, 0, []), 4);
        var results = new BattleResultsSnapshot("battle:1", BattleResultsStage.Open, "A", outcome, canLoot, playerCarrierDestroyed);
        var current = legacy with { BattleResults = results };
        var roundtrip = JsonSerializer.Deserialize<DemoCampaignSnapshot>(JsonSerializer.Serialize(current))!;

        var restored = Assert.IsType<BattleResultsSnapshot>(roundtrip.BattleResults);
        Assert.Equal(results.BattleId, restored.BattleId);
        Assert.Equal(BattleResultsStage.Open, restored.Stage);
        Assert.Equal(kind, restored.Outcome.Kind);
        Assert.Equal(new TacticalLossCount("A", 2), Assert.Single(restored.Outcome.Losses));
        Assert.Equal(canLoot, restored.CanLoot);
        Assert.Equal(playerCarrierDestroyed, restored.PlayerCarrierDestroyed);
        Assert.Null(roundtrip.Rewards);
    }
}
