using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class DemoEp0033Us0004Tk0001Tests
{
    [Fact]
    public void Payload_RoundTrips()
    {
        var command = new HirePilotCommand("c", 1, "offer");
        Assert.Equal(command, JsonSerializer.Deserialize<HirePilotCommand>(JsonSerializer.Serialize(command)));
        var assign = new AssignPilotCommand("a", 2, "pilot", "drone");
        Assert.Equal(assign, JsonSerializer.Deserialize<AssignPilotCommand>(JsonSerializer.Serialize(assign)));
    }
    [Fact]
    public void LegacyPayload_UsesSafeDefaults() => Assert.Null(JsonSerializer.Deserialize<DemoCompanySnapshot>("{}")!.Recruitment);
}
