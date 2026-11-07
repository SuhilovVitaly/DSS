using System.Text.Json;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class V2ClusterGraphTests
{
    [Fact]
    public void LegacyIdsHaveNoDistanceExemption()
    {
        using var engine = StationGeographyBootstrapTests.Create(config: GenerationInputSchemaTests.V2Config());
        var snapshot = engine.CaptureSnapshot(); var map = snapshot.ClusterMap!;
        var objects = snapshot.Objects.ToDictionary(o => o.ObjectId);
        var members = map.Stations.ToDictionary(s => s.ObjectId);
        double units = map.Geography!.ReferenceSpeedKmS * 86400 / 300 * 10;
        foreach (var link in map.Links.Where(l => members[l.FromStationId].ClusterId == members[l.ToStationId].ClusterId))
        {
            var a = objects[link.FromStationId]; var b = objects[link.ToStationId];
            Assert.InRange(double.Hypot(a.X - b.X, a.Y - b.Y) / units, 1 - 1e-9, 3 + 1e-9);
        }
        var far = map.Stations.Where(s => s.ObjectId.StartsWith("SPC-", StringComparison.Ordinal)).SelectMany(a =>
            map.Stations.Where(b => b.ObjectId.StartsWith("SPC-", StringComparison.Ordinal) && a.ObjectId != b.ObjectId)
                .Select(b => (A: a.ObjectId, B: b.ObjectId))).Where(p =>
                    double.Hypot(objects[p.A].X - objects[p.B].X, objects[p.A].Y - objects[p.B].Y) / units > 3).ToArray();
        Assert.NotEmpty(far);
        foreach (var pair in far) Assert.DoesNotContain(map.Links, l => l.FromStationId == pair.A && l.ToStationId == pair.B);
    }

    [Fact]
    public void AllStationsHaveOutboundAndReturnPaths()
    {
        foreach (int clusters in new[] { 3, 5 })
            foreach (ulong seed in new ulong[] { 1, 2, 42 })
            {
                var config = GenerationInputSchemaTests.V2Config() with
                { Clusters = GenerationInputSchemaTests.V2Config().Clusters! with { MinClusters = clusters, MaxClusters = clusters } };
                using var engine = StationGeographyBootstrapTests.Create(config: config, seed: seed);
                using var repeat = StationGeographyBootstrapTests.Create(config: config, seed: seed);
                var map = engine.CaptureSnapshot().ClusterMap!;
                Assert.Equal(JsonSerializer.Serialize(map), JsonSerializer.Serialize(repeat.CaptureSnapshot().ClusterMap));
                Assert.Equal(clusters, map.Clusters.Length);
                foreach (var start in map.Stations)
                {
                    Assert.True(map.Links.Count(l => l.FromStationId == start.ObjectId) >= 2);
                    var seen = new HashSet<string> { start.ObjectId };
                    for (int i = 0; i < map.Stations.Length; i++)
                        foreach (var link in map.Links) if (seen.Contains(link.FromStationId)) seen.Add(link.ToStationId);
                    Assert.Equal(map.Stations.Length, seen.Count);
                }
                Assert.NotNull(engine.CaptureClusterVoyageMapForTools());
            }
    }

    [Fact]
    public void CargoFlowsMatchProfilesAfterReposition()
    {
        var registry = SeededWorldBootstrapTests.Registry();
        using var engine = StationGeographyBootstrapTests.Create(config: GenerationInputSchemaTests.V2Config());
        var map = engine.CaptureSnapshot().ClusterMap!;
        var profiles = map.Stations.ToDictionary(s => s.ObjectId,
            s => registry.StationMarketProfiles.GetDefinition(registry.StationMarketProfiles.GetIndex(s.MarketProfileId)));
        foreach (var link in map.Links)
        {
            var expected = profiles[link.FromStationId].SupplyItemTypeIds.Intersect(profiles[link.ToStationId].DemandItemTypeIds)
                .Order(StringComparer.Ordinal).ToArray();
            Assert.Equal(expected, link.ItemTypeIds);
            if (!profiles[link.FromStationId].SupplyItemTypeIds.IsEmpty) Assert.NotEmpty(link.ItemTypeIds);
        }
    }
}
