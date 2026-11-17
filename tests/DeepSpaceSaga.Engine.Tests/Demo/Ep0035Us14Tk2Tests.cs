using DeepSpaceSaga.Engine.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class Ep0035Us14Tk2Tests
{
    [Fact]
    public void FirstCarrierLossKeepsSideAlive_LastDisablesSurvivorsOnceWithoutHullDamage()
    {
        var battle = Ep0035Us14Tk1Tests.Squad();
        var carriers = battle.Carriers.Where(c => c.Side == "B").ToArray();
        var enemy = battle.Drones.Where(d => d.Side == "B").ToArray();
        enemy[0].Hull = 0; enemy[0].Status = TacticalDroneStatus.Destroyed;
        var hull = enemy.Skip(1).Select(d => d.Hull).ToArray();
        carriers[0].Hull = 0;
        Assert.Null(battle.CheckVictory());
        Assert.All(enemy.Skip(1), d => Assert.Equal(TacticalDroneStatus.Air, d.Status));
        carriers[1].Hull = 0;
        var result = Assert.IsType<TacticalBattleResult>(battle.CheckVictory());
        Assert.Equal(TacticalOutcomeKind.A, result.Kind);
        Assert.Equal(2, result.Reasons.OfType<TacticalCarrierDestroyed>().Count());
        Assert.All(enemy.Skip(1), d => { Assert.Equal(TacticalDroneStatus.Disabled, d.Status); Assert.Equal(0, d.Speed); });
        Assert.Equal(hull, enemy.Skip(1).Select(d => d.Hull));
        Assert.Equal(TacticalDroneStatus.Destroyed, enemy[0].Status);
        long disabled = battle.Statistics.Disabled;
        Assert.Same(result, battle.CheckVictory());
        Assert.Equal(disabled, battle.Statistics.Disabled);
    }

    [Fact]
    public void PlayerCarrierLossUsesTheSameDisableRule()
    {
        var battle = Ep0035Us14Tk1Tests.Squad();
        battle.Carrier("A").Hull = 0;
        Assert.Equal(TacticalOutcomeKind.B, battle.CheckVictory()!.Kind);
        Assert.All(battle.Drones.Where(d => d.Side == "A"), d =>
        { Assert.Equal(TacticalDroneStatus.Disabled, d.Status); Assert.True(d.Hull > 0); Assert.Equal(0, d.Speed); });
    }
}
