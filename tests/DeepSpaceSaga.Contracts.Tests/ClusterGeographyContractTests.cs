using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public sealed class ClusterGeographyContractTests
{
    [Fact]
    public void ClusterSnapshotRoundTrip()
    {
        var map = new StationClusterMapSnapshot(1, "cluster-1",
            [new("cluster-1", "Home", "belt-1", ["station-1", "station-2"], "mining")],
            [new("station-1", "cluster-1", "market.mining"), new("station-2", "cluster-1", "market.transit")],
            [new("link-1", "station-1", "station-2", ["item.ice"])]);
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, [], ClusterMap: map);
        var json = JsonSerializer.Serialize(snapshot);
        using var document = JsonDocument.Parse(json);
        Assert.Equal("cluster-1", document.RootElement.GetProperty("clusterMap").GetProperty("startClusterId").GetString());
        var restored = JsonSerializer.Deserialize<AuthoritativeSnapshot>(json)!;
        Assert.Equal(json, JsonSerializer.Serialize(restored));
        Assert.Equal("market.transit", restored.ClusterMap!.Stations[1].MarketProfileId);
        Assert.Equal("item.ice", restored.ClusterMap.Links[0].ItemTypeIds[0]);

        var empty = new StationClusterMapSnapshot(1, "", default, default, default);
        var defaults = JsonSerializer.Deserialize<StationClusterMapSnapshot>(JsonSerializer.Serialize(empty))!;
        Assert.True(defaults.Clusters.IsEmpty);
        Assert.True(defaults.Stations.IsEmpty);
        Assert.True(defaults.Links.IsEmpty);
        var cluster = JsonSerializer.Deserialize<StationClusterData>(JsonSerializer.Serialize(new StationClusterData("c", "n", "b", default, "s")))!;
        Assert.True(cluster.StationIds.IsEmpty);
        var link = JsonSerializer.Deserialize<ClusterTradeLink>(JsonSerializer.Serialize(new ClusterTradeLink("l", "a", "b", default)))!;
        Assert.True(link.ItemTypeIds.IsEmpty);
    }

    [Fact]
    public void LegacySnapshotHasNoCluster()
    {
        const string legacy = """
            {"SnapshotSequence":1,"GameTimeMs":0,"CurrentSpeed":0,"Objects":[]}
            """;
        Assert.Null(JsonSerializer.Deserialize<AuthoritativeSnapshot>(legacy)!.ClusterMap);
        Assert.Null(new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, []).ClusterMap);
        Assert.DoesNotContain(typeof(ClusterTradeLink).GetProperties(), p => p.Name.Contains("Eta", StringComparison.OrdinalIgnoreCase) || p.Name.Contains("Quote", StringComparison.OrdinalIgnoreCase));
    }
}
