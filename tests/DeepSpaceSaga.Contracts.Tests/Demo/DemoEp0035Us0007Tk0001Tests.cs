using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class DemoEp0035Us0007Tk0001Tests
{
    [Fact]
    public void Roundtrip_PreservesSensor()
    {
        var source = new DemoCampaignSnapshot(3, 8, Sensor: new("sensor", 2400, true));
        var restored = JsonSerializer.Deserialize<DemoCampaignSnapshot>(JsonSerializer.Serialize(source));
        Assert.Equal(source, restored);
        var contact = new SensorContactSnapshot("player", "pirate", true, 1234);
        Assert.Equal(contact, JsonSerializer.Deserialize<SensorContactSnapshot>(JsonSerializer.Serialize(contact)));
    }

    [Fact]
    public void MissingSection_IsNotInfiniteRange()
    {
        var legacy = JsonSerializer.Deserialize<DemoCampaignSnapshot>("{\"Revision\":1,\"CommitSequence\":2}");
        Assert.Null(legacy!.Sensor);
        Assert.Equal(0, legacy.Sensor?.RadiusWorldUnits ?? 0);
    }
}
