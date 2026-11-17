using System.Collections.Immutable;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Content;
using DeepSpaceSaga.Engine.TacticalBattle.Ai;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class Ep0035Us14Tk1Tests
{
    internal static Battle Squad()
    {
        var catalog = TacticalBattleContentLoader.Load(DemoEp0033Us0001Tk0003Tests.Root() + "/Data/TacticalBattle");
        var scenario = catalog.Scenarios["initial"];
        var enemy = scenario.Sides[1];
        var second = enemy.Carrier with { Id = "second-lair", Y = enemy.Carrier.Y + 300 };
        enemy = enemy with { Carriers = [enemy.Carrier, second], Drones = enemy.Drones.Select((d, i) =>
            d with { OriginCarrierId = i % 2 == 0 ? enemy.Carrier.Id : second.Id }).ToImmutableArray() };
        scenario = scenario with { Sides = scenario.Sides.SetItem(1, enemy) };
        catalog = catalog with { Scenarios = new([("squad", scenario)]) };
        return Battle.Create(catalog, "squad", 123);
    }

    [Fact]
    public void DroneUsesItsLiveOriginThenSurvivingCarrier_EnemySelectionSkipsDestroyed()
    {
        var battle = Squad();
        var drone = battle.Drones.First(d => d.OriginCarrierId == "second-lair");
        Assert.Equal("second-lair", battle.CarrierFor(drone).Id);
        var second = battle.Carriers.Single(c => c.Id == "second-lair");
        second.Hull = 0;
        Assert.NotEqual("second-lair", battle.CarrierFor(drone).Id);
        var own = battle.Drones.First(d => d.Side == "A");
        Assert.True(battle.CarrierFor(own, enemy: true).IsActive);
        var order = TacticalRoles.Striker(battle, own);
        Assert.True(double.IsFinite(order.Speed));
        Assert.True(double.IsFinite(order.Turn));
        Assert.All(TacticalCommander.Threats(battle, "B"), d => Assert.Equal("A", d.Side));
    }
}
