using System.Text.Json;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0032Us0004Tk0002Tests
{
    [Fact]
    public void DemoUsesFiveFrozenStationsAndSharedStarter()
    {
        var root = DemoEp0032Us0001Tk0002Tests.ContentRoot;
        var cluster = DemoCampaignDefinitions.Load(root).Cluster!;
        using var scenario = JsonDocument.Parse(File.ReadAllText(root + "/Scenarios/Demo/scenario.json"));
        Assert.Equal(5, cluster.StationIds.Length);
        Assert.Equal(5, cluster.Roles.Distinct().Count());
        Assert.Equal("Data/Ships/starter-carrier.json", scenario.RootElement.GetProperty("scenarioMetadata").GetProperty("starterCarrierManifest").GetString());
        Assert.Equal(cluster.Seed, scenario.RootElement.GetProperty("gameState").GetProperty("masterSeed").GetUInt64());
    }
}
