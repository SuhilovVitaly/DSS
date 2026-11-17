using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class DemoEp0035Us0001Tk0001Tests
{
    [Fact]
    public void Payload_RoundTrips()
    {
        var group = new PirateGroupSnapshot("group-1", 10, 20, 3, -4, 1000, true, PirateGroupStatuses.Observed, 7);
        var campaign = new DemoCampaignSnapshot(2, 3, Encounters: new() { Groups = [group] });
        var restored = JsonSerializer.Deserialize<DemoCampaignSnapshot>(JsonSerializer.Serialize(campaign));
        Assert.Equal(JsonSerializer.Serialize(group), JsonSerializer.Serialize(Assert.Single(restored!.Encounters!.Groups)));
    }

    [Fact]
    public void LegacyPayload_UsesSafeDefaults()
    {
        Assert.Empty(JsonSerializer.Deserialize<DemoEncountersSnapshot>("{}")!.Groups);
        Assert.Null(JsonSerializer.Deserialize<DemoCampaignSnapshot>("{}")!.Encounters);
    }

    [Fact]
    public void ReasonCodes_AreStable()
    {
        Assert.Equal("observed", PirateGroupStatuses.Observed);
        Assert.Equal("debug_visible", PirateGroupStatuses.DebugVisible);
        Assert.Equal("captured", PirateGroupStatuses.Captured);
    }
}
