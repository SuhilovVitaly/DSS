using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class DemoEp0033Us0005Tk0001Tests
{
    [Fact]
    public void Payload_RoundTrips()
    {
        var command = new PayCrewDebtCommand("c", 1, 20);
        Assert.Equal(command, JsonSerializer.Deserialize<PayCrewDebtCommand>(JsonSerializer.Serialize(command)));
        var state = new CrewUpkeepSnapshot(100, 80, 20, 86400000, 6, 20, 0, 0);
        Assert.Equal(state, JsonSerializer.Deserialize<CrewUpkeepSnapshot>(JsonSerializer.Serialize(state)));
    }
    [Fact]
    public void LegacyPayload_UsesSafeDefaults() => Assert.Null(JsonSerializer.Deserialize<DemoCompanySnapshot>("{}")!.Upkeep);
}
