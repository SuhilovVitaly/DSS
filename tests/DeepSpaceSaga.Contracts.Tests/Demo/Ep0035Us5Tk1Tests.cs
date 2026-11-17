using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class Ep0035Us5Tk1Tests
{
    [Fact]
    public void RouteTermsRetainUnknownFuelAndExpiredDeadline()
    {
        var state = new ThreatRoutesSnapshot(17, [new("detour:s", "s", true, [new(1, 2), new(3, 4)],
            9000, null, "unknown", 0, -100, "fuel_efficiency_unavailable")], "detour:s");
        var loaded = JsonSerializer.Deserialize<ThreatRoutesSnapshot>(JsonSerializer.Serialize(state))!;
        Assert.Equal(JsonSerializer.Serialize(state), JsonSerializer.Serialize(loaded));
        Assert.Null(loaded.Options[0].FuelRequiredKg);
        Assert.Equal(-100, loaded.Options[0].DeadlineDeltaGameTimeMs);
        Assert.Equal(2, loaded.Options[0].Waypoints.Length);
    }
}
