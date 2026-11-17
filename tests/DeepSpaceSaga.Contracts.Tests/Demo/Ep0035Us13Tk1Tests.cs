using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class Ep0035Us13Tk1Tests
{
    [Fact]
    public void SameSideDronesPreserveDistinctOriginCarrierIdentities()
    {
        TacticalUnitSnapshot Unit(string id, string carrier) => new(id, "B", TacticalUnitKind.Drone, "light",
            new(0, 0, 0), 1, 10, 10, 1, 1, TacticalUnitState.Air, null, false, [], null, 0, null, 1, 1)
        { OriginCarrierId = carrier };
        var units = new[] { Unit("d1", "lair1"), Unit("d2", "lair2") };
        var restored = JsonSerializer.Deserialize<TacticalUnitSnapshot[]>(JsonSerializer.Serialize(units))!;
        Assert.Equal(new[] { "lair1", "lair2" }, restored.Select(u => u.OriginCarrierId));
        Assert.All(restored, u => Assert.Equal("B", u.Side));
        Assert.NotEqual(restored[0].Id, restored[1].Id);
    }
}
