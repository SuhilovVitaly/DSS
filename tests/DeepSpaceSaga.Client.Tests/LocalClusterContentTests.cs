using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public sealed class LocalClusterContentTests
{
    [Fact]
    public void ClusterContentUsesExistingProfiles()
    {
        var config = EngineContentLoader.LoadSolarSystemGenerationConfig(DefaultSystemContentTests.Settings)!;
        Assert.Equal(0.5, config.Clusters!.NeighbourMinDays);
        Assert.Equal(2, config.Clusters.NeighbourMaxDays);
        Assert.Equal(3, config.Clusters.DiameterMinDays);
        Assert.Equal(7, config.Clusters.DiameterMaxDays);
        foreach (var name in config.EnabledScenarios)
        {
            using var engine = SimulationEngine.CreateFromScenarioFile(DefaultSystemContentTests.Settings,
                Path.Combine(AppContext.BaseDirectory, "Scenarios", name, "scenario.json"));
            var map = engine.CaptureSnapshot().ClusterMap!;
            Assert.InRange(map.Clusters.Length, config.Clusters.MinClusters, config.Clusters.MaxClusters);
            Assert.All(map.Clusters, c => Assert.InRange(c.StationIds.Length, 10, 12));
            Assert.Equal(new[] { "market.hydroponic", "market.industrial", "market.mining", "market.scientific-military", "market.transit" },
                map.Stations.Select(s => s.MarketProfileId).Distinct().Order(StringComparer.Ordinal));
        }
    }

    [Theory]
    [InlineData("Default")]
    [InlineData("MarketProfiles")]
    public void ScenarioMarketsRemainExplicit(string name)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Scenarios", name, "scenario.json");
        var original = ScenarioLoader.LoadFromFile(path);
        using var engine = SimulationEngine.CreateFromScenarioFile(DefaultSystemContentTests.Settings, path);
        var objects = engine.CaptureSaveState().GameState.SpaceObjects;
        foreach (var station in original.GameState.SpaceObjects.Where(o => o.ObjectType == "Station"))
            foreach (var item in station.Inventory ?? [])
                Assert.Contains(objects.Single(o => o.ObjectId == station.ObjectId).Inventory!, i => i.ItemTypeId == item.ItemTypeId && i.Quantity == item.Quantity);
    }
}
