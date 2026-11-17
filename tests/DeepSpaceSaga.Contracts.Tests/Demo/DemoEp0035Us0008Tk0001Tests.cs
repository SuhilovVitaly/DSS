using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class DemoEp0035Us0008Tk0001Tests
{
    [Fact]
    public void NormalDefault_IsTrueAndLegacyHasNoOverride()
    {
        Assert.True(new FogOfWarSnapshot().Enabled);
        Assert.Null(JsonSerializer.Deserialize<DemoCampaignSnapshot>("{}")!.FogOfWar);
        var request = new SetFogOfWarRequest("fog-1", 3, false);
        Assert.Equal(request, JsonSerializer.Deserialize<SetFogOfWarRequest>(JsonSerializer.Serialize(request)));
        var group = new PirateGroupSnapshot("g", 0, 0, 0, 0, 0, false, PirateGroupStatuses.DebugVisible);
        Assert.True(group.IsDebugVisible);
        Assert.False((group with { IsDetected = true }).IsDebugVisible);
    }
}
