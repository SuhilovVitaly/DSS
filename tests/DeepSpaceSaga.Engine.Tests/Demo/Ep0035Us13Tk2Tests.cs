using DeepSpaceSaga.Engine.TacticalBattle.Content;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class Ep0035Us13Tk2Tests
{
    internal static BattleScenario Scenario() => new("squad", "test", [
        new("A", "standard", new("a", "carrier.standard", -100, 0, 0), [new("ad", "p", "light.spear", 0, 0, 0, 1)]),
        new("B", "standard", null!, [new("bd1", "p", "light.spear", 0, 0, 0, 1, "b1"), new("bd2", "p", "light.spear", 0, 0, 0, 1, "b2")],
            [new("b1", "carrier.standard", 100, -20, 0), new("b2", "carrier.standard", 100, 20, 0)])]);

    [Fact]
    public void NormalizeLegacySingletonAndExplicitSquadOwnership()
    {
        var normalized = TacticalBattleContentLoader.NormalizeScenario(Scenario());
        Assert.Single(normalized.Sides[0].Carriers);
        Assert.Equal("a", normalized.Sides[0].Drones[0].OriginCarrierId);
        Assert.Equal(2, normalized.Sides[1].Carriers.Length);
        Assert.Equal(new[] { "b1", "b2" }, normalized.Sides[1].Drones.Select(d => d.OriginCarrierId));
        var side = normalized.Sides[1];
        foreach (string? origin in new[] { null, "a", "missing" })
            Assert.Throws<TacticalBattleContentException>(() => TacticalBattleContentLoader.NormalizeScenario(normalized with
            { Sides = normalized.Sides.SetItem(1, side with { Drones = side.Drones.SetItem(0, side.Drones[0] with { OriginCarrierId = origin }) }) }));
        Assert.Throws<TacticalBattleContentException>(() => TacticalBattleContentLoader.NormalizeScenario(normalized with
        { Sides = normalized.Sides.SetItem(1, side with { Carriers = [side.Carriers[0], side.Carriers[0]] }) }));
    }
}
