using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class DemoEp0033Us0002Tk0001Tests
{
    [Fact]
    public void Payload_RoundTrips()
    {
        var command = new ResupplyDecoyCommand("c", 2, "d", "q", 3);
        Assert.Equal(command, JsonSerializer.Deserialize<ResupplyDecoyCommand>(JsonSerializer.Serialize(command)));
    }
    [Fact]
    public void LegacyPayload_UsesSafeDefaults() => Assert.Null(JsonSerializer.Deserialize<DemoCompanySnapshot>("{}")!.Supply);
    [Fact]
    public void ReasonCodes_AreStable() => Assert.Equal("service_invalid_quote", CompanyServiceReasons.InvalidQuote);
}
