using DeepSpaceSaga.Contracts;
using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class DemoEp0032Us0004Tk0001Tests
{
    [Fact]
    public void DemoScopeHasNoResourceGrant()
    {
        var scope = new DemoStartSnapshot(1, "demo", 32, "home", [new("home", "service")], "shared-manifest");
        var loaded = JsonSerializer.Deserialize<DemoStartSnapshot>(JsonSerializer.Serialize(scope))!;
        Assert.Equal("service", Assert.Single(loaded.Stations).Role);
        Assert.Equal(scope.Seed, loaded.Seed);
        Assert.Null(new DemoCampaignSnapshot(0, 0).Start);
    }
}
