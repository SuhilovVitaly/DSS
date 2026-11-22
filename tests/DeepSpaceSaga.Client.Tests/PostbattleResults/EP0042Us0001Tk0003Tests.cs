using DeepSpaceSaga.Client.Tests.Demo;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.PostbattleResults;

public sealed class EP0042Us0001Tk0003Tests
{
    [Theory]
    [InlineData(TacticalBattleOutcomeCode.A, "Victory", true)]
    [InlineData(TacticalBattleOutcomeCode.B, "Defeat", false)]
    [InlineData(TacticalBattleOutcomeCode.Draw, "Draw", false)]
    [InlineData(TacticalBattleOutcomeCode.Timeout, "Timeout", false)]
    public void BattleResults_AllOutcomes_RenderCorrectLedger(TacticalBattleOutcomeCode kind, string expected, bool canLoot)
    {
        using var engine = DemoEp0036Us0007Tk0004Tests.Won();
        var battle = engine.GetTacticalBattleState()!;
        var outcome = battle.Outcome! with { Kind = kind, Losses = [new(battle.PlayerSide, 2), new("B", 5)] };
        battle = battle with { Outcome = outcome };
        var current = new BattleResultsSnapshot(battle.BattleId, BattleResultsStage.Open, battle.PlayerSide, outcome, canLoot, false)
        { ConsumedTorpedoes = 3 };
        var stale = new LootSessionSnapshot("another battle", LootPhases.Open, [], true);

        var summary = Assert.IsType<BattleResultsScreen.Summary>(BattleResultsScreen.Summary.From(battle, current, stale));
        Assert.Equal(expected, summary.Outcome);
        Assert.Equal(2, summary.OwnLosses);
        Assert.Equal(5, summary.EnemyLosses);
        Assert.Equal(3, summary.ConsumedTorpedoes);
        Assert.Equal(canLoot, summary.CanLoot);
        Assert.False(BattleResultsScreen.Summary.From(battle, null, stale)!.CanLoot);
    }
}
