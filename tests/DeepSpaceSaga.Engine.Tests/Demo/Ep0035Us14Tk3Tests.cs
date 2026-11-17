using System.Text.Json;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class Ep0035Us14Tk3Tests
{
    [Fact]
    public void RepeatedSessionSnapshotsKeepThreeCarrierIdentitiesOwnershipAndRandomState()
    {
        var battle = Ep0035Us14Tk1Tests.Squad();
        var session = TacticalBattleSession.Create(battle, "world-battle", "A");
        long randomCalls = battle.Random.Calls;
        var one = session.Capture();
        Assert.Equal(3, one.Units.Count(u => u.Kind == TacticalUnitKind.Carrier));
        Assert.All(one.Units.Where(u => u.Kind == TacticalUnitKind.Drone), u =>
            Assert.Contains(one.Units, carrier => carrier.Kind == TacticalUnitKind.Carrier && carrier.Id == u.OriginCarrierId && carrier.Side == u.Side));
        string json = JsonSerializer.Serialize(one);
        Assert.Equal(json, JsonSerializer.Serialize(session.Capture()));
        var restored = JsonSerializer.Deserialize<TacticalBattleSnapshot>(json)!;
        Assert.Equal(json, JsonSerializer.Serialize(restored));
        Assert.Equal(randomCalls, battle.Random.Calls);
        battle.Carriers.Single(c => c.Id == "second-lair").Hull -= 10;
        var next = session.Capture();
        Assert.Equal(one.Units.Single(u => u.Id == "second-lair").Hull - 10, next.Units.Single(u => u.Id == "second-lair").Hull);
        Assert.Equal(one.Units.Where(u => u.Kind == TacticalUnitKind.Drone).Select(u => u.AmmoCurrent),
            next.Units.Where(u => u.Kind == TacticalUnitKind.Drone).Select(u => u.AmmoCurrent));
    }
}
