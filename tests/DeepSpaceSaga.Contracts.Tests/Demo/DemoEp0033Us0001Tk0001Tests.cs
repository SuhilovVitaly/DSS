using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class DemoEp0033Us0001Tk0001Tests
{
    [Fact]
    public void Payload_RoundTrips()
    {
        var command = new RepairCommand("repair-1", 7, "quote-1", "drone-1");
        Assert.Equal(command, JsonSerializer.Deserialize<RepairCommand>(JsonSerializer.Serialize(command)));
        var quote = new RepairQuote("q", "s", "d", 8, 60000, 20,
            [new("hull", 2, 10, 20)], new(8, 0, 0, false));
        var restored = JsonSerializer.Deserialize<RepairQuote>(JsonSerializer.Serialize(quote))!;
        Assert.Equal(quote.Items[0], restored.Items[0]);
        Assert.Equal(quote.Before, restored.Before);
        Assert.Equal(20, restored.Total);
    }

    [Fact]
    public void LegacyPayload_UsesSafeDefaults()
    {
        Assert.Null(JsonSerializer.Deserialize<DemoCompanySnapshot>("{}")!.StationRepair);
        Assert.Null(new StationRepairSnapshot().Quote);
    }

    [Fact]
    public void ReasonCodes_AreStable()
    {
        Assert.Equal("service_invalid_quote", CompanyServiceReasons.InvalidQuote);
        Assert.Equal("service_insufficient_funds", CompanyServiceReasons.InsufficientFunds);
    }
}
