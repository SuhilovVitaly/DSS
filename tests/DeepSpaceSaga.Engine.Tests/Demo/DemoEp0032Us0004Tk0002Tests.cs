using System.Runtime.CompilerServices;
using System.Text.Json;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0032Us0004Tk0002Tests
{
    // EP-0034-US-0001-TK-0002: read this checkout's content (not a fixed sibling path) and expect the
    // demo-only derived starter manifest; Tutorial/Default keep the shared one.
    private static string ContentRoot([CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    [Fact]
    public void DemoUsesFiveFrozenStationsAndDemoStarter()
    {
        var root = ContentRoot();
        var cluster = DemoCampaignDefinitions.Load(root).Cluster!;
        using var scenario = JsonDocument.Parse(File.ReadAllText(root + "/Scenarios/Demo/scenario.json"));
        Assert.Equal(5, cluster.StationIds.Length);
        Assert.Equal(5, cluster.Roles.Distinct().Count());
        Assert.Equal("Data/Ships/demo-starter-carrier.json", scenario.RootElement.GetProperty("scenarioMetadata").GetProperty("starterCarrierManifest").GetString());
        Assert.Equal(cluster.Seed, scenario.RootElement.GetProperty("gameState").GetProperty("masterSeed").GetUInt64());
    }
}
