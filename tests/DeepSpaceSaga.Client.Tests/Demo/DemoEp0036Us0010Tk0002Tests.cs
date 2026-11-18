using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0036Us0010Tk0002Tests
{
    [Fact]
    public void SelfDestructLogLineDistinct()
    {
        var entry = new TacticalBattleLogEvent(1, 1, TacticalBattlePhase.Fire, TacticalBattleLogKind.SelfDestructed, TargetId: "enemy",
            Unit: new(TacticalUnitKind.Carrier, 0, 100));
        Assert.Contains("enemy", BattleLogFormatter.Format(entry));
        Assert.NotEqual(BattleLogFormatter.Format(entry), BattleLogFormatter.Format(entry with { Kind = TacticalBattleLogKind.Destroyed }));
    }
    [Fact]
    public void WreckLabelsByFrozenKind()
    {
        Assert.NotEqual(TacticalBattleMapRenderer.LootSourceLabel("DestroyedDrone"), TacticalBattleMapRenderer.LootSourceLabel("SelfDestructCarrier"));
        Assert.DoesNotContain("loot.", TacticalBattleMapRenderer.LootSourceLabel("DestroyedDrone"));
    }
    [Fact]
    public void FinishButtonUsesConfirmFlow()
    {
        var presentation = new BattleLootPresentation(_ => throw new InvalidOperationException());
        presentation.Observe(new("battle", LootPhases.Open, [], true), 0);
        using var window = new BattleLootWindow(presentation);
        Assert.False(window.RequestExit()); Assert.True(window.ConfirmDiscard);
        window.CancelExit(); Assert.False(window.ConfirmDiscard);
        presentation.Observe(new("battle", LootPhases.Closed, [], false), 1);
        Assert.Null(presentation.State);
    }
}
