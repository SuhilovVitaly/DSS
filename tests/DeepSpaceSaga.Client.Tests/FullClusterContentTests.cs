using System.Text.Json;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public sealed class FullClusterContentTests
{
    [Fact]
    public void FullClusterBoundaryContent()
    {
        var config = EngineContentLoader.LoadSolarSystemGenerationConfig(DefaultSystemContentTests.Settings)!;
        Assert.Equal(3, config.Clusters!.MinClusters); Assert.Equal(5, config.Clusters.MaxClusters);
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(DefaultSystemContentTests.Settings, out _, out _);
        foreach (var name in config.EnabledScenarios)
            foreach (int belts in new[] { 2, 5 })
                foreach (int clusters in new[] { 3, 5 })
                    foreach (int stations in new[] { 10, 12 })
                        foreach (ulong seed in new ulong[] { 1, 2, 42 })
                        {
                            var source = ScenarioLoader.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Scenarios", name, "scenario.json"));
                            using var engine = new SimulationEngine(registry);
                            engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = seed } }, generation: config with
                            { MinBelts = belts, MaxBelts = belts, Clusters = config.Clusters with { MinClusters = clusters, MaxClusters = clusters, MinStations = stations, MaxStations = stations } });
                            var map = engine.CaptureSnapshot().ClusterMap!;
                            Assert.Equal(clusters, map.Clusters.Length);
                            Assert.Equal(clusters * stations, map.Stations.Length);
                            Assert.All(map.Clusters, c => Assert.Equal(5, map.Stations.Where(s => s.ClusterId == c.Id).Select(s => s.MarketProfileId).Distinct().Count()));
                        }
    }

    [Theory]
    [InlineData("Default")]
    [InlineData("MarketProfiles")]
    public void StartClusterRetainsScenarioIdentity(string name)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Scenarios", name, "scenario.json");
        var source = ScenarioLoader.LoadFromFile(path);
        using var engine = SimulationEngine.CreateFromScenarioFile(DefaultSystemContentTests.Settings, path);
        var map = engine.CaptureSnapshot().ClusterMap!;
        var home = map.Clusters.Single(c => c.Id == map.StartClusterId);
        foreach (var obj in source.GameState.SpaceObjects.Where(o => o.ObjectType == "Station"))
        {
            Assert.Contains(obj.ObjectId, home.StationIds);
            Assert.Single(map.Stations, s => s.ObjectId == obj.ObjectId);
        }
        Assert.Equal(map.Stations.Length, map.Stations.Select(s => s.ObjectId).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
}
