using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Content;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0036Us0007Tk0002Tests
{
    private static TacticalBattleSession Squad(string playerSide = "A")
    {
        var catalog = TacticalBattleContentLoader.Load(DemoEp0033Us0001Tk0003Tests.Root() + "/Data/TacticalBattle");
        catalog = catalog with
        {
            Scenarios = new([("squad", Ep0035Us13Tk2Tests.Scenario())]),
            Pilots = new([("p", catalog.Pilots.Entries[0].Value)])
        };
        return TacticalBattleSession.Create(DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle.Create(catalog, "squad", 42), "squad", playerSide);
    }

    [Fact]
    public void OneCarrierOwnWingDisabled_SelfDestructs()
    {
        var session = Squad();
        var battle = session.Battle;
        battle.Drones.Single(d => d.Id == "bd1").Status = TacticalDroneStatus.Disabled;
        Assert.Null(battle.CheckVictory());
        Assert.Equal(0, battle.Carriers.Single(c => c.Id == "b1").Hull);
        Assert.True(battle.Carriers.Single(c => c.Id == "b2").IsActive);
        Assert.True(battle.Drones.Single(d => d.Id == "bd2").IsActive);
        Assert.Equal(TacticalDroneStatus.Disabled, battle.Drones.Single(d => d.Id == "bd1").Status);
        Assert.Single(battle.SelfDestructedCarrierIds);
        battle.CheckVictory();
        Assert.Single(battle.SelfDestructedCarrierIds);
        battle.Drones.Single(d => d.Id == "bd2").Status = TacticalDroneStatus.Destroyed;
        Assert.NotNull(battle.CheckVictory());
        Assert.Equal(2, battle.SelfDestructedCarrierIds.Count);
    }

    [Fact]
    public void PlayerSideIsExplicit_NotAssumedA()
    {
        var battle = Squad("B").Battle;
        battle.Drones.Single(d => d.Id == "bd1").Status = TacticalDroneStatus.Disabled;
        battle.CheckVictory();
        Assert.True(battle.Carriers.Single(c => c.Id == "b1").IsActive);
        battle.Drones.Single(d => d.Id == "ad").Status = TacticalDroneStatus.Disabled;
        battle.CheckVictory();
        Assert.Contains("a", battle.SelfDestructedCarrierIds);
        Assert.DoesNotContain("b1", battle.SelfDestructedCarrierIds);
    }
}
