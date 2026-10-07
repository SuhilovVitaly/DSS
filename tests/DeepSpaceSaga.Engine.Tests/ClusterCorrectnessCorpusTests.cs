using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ClusterCorrectnessCorpusTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (string scenario in new[] { "Default", "Default_500", "Docked", "Undocked", "MarketProfiles", "PlayerShipOnly" })
            foreach (int clusters in new[] { 3, 5 }) foreach (int stations in new[] { 10, 12 }) foreach (int belts in new[] { 2, 5 })
                yield return [scenario, clusters, stations, belts];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void HundredSeedClusterBoundaryMatrix(string scenario, int clusters, int stations, int belts)
    {
        var registry = SeededWorldBootstrapTests.Registry();
        var source = SeededWorldBootstrapTests.Scenario(scenario);
        var config = EngineContentLoader.LoadSolarSystemGenerationConfig(Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Settings.json"))!;
        config = config with { MinBelts = belts, MaxBelts = belts, Clusters = config.Clusters! with { MinClusters = clusters, MaxClusters = clusters, MinStations = stations, MaxStations = stations } };
        var fields = JsonSerializer.Deserialize<StationResourceFieldConfig>(File.ReadAllText(Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Data/World/station-resource-fields.json")))!;
        for (ulong seed = 1; seed <= 100; seed++)
        {
            try
            {
                using var engine = new SimulationEngine(registry); engine.ConfigureStationResourceFields(fields);
                engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = seed, CurrentSpeed = "Speed0" } }, generation: config);
                var snapshot = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0); var map = snapshot.ClusterMap!;
                Assert.Equal(clusters, map.Clusters.Length); Assert.Equal(clusters * stations, map.Stations.Length);
                Assert.Equal(belts, snapshot.SolarSystemMap!.Belts.Length);
                Assert.Equal(snapshot.Objects.Length, snapshot.Objects.Select(o => o.ObjectId).Distinct(StringComparer.OrdinalIgnoreCase).Count());
                foreach (var c in map.Clusters)
                {
                    Assert.Equal(stations, c.StationIds.Length);
                    Assert.Equal(5, map.Stations.Where(s => s.ClusterId == c.Id).Select(s => s.MarketProfileId).Distinct().Count());
                    Assert.True(map.Links.Any(l => c.StationIds.Contains(l.FromStationId) && !c.StationIds.Contains(l.ToStationId)));
                    foreach (string start in c.StationIds)
                    {
                        var seen = new HashSet<string> { start }; var queue = new Queue<string>(); queue.Enqueue(start);
                        while (queue.TryDequeue(out var id)) foreach (var link in map.Links.Where(l => l.FromStationId == id)) if (seen.Add(link.ToStationId)) queue.Enqueue(link.ToStationId);
                        Assert.Equal(map.Stations.Length, seen.Count);
                        Assert.True(map.Links.Count(l => l.FromStationId == start && c.StationIds.Contains(l.ToStationId)) >= 2);
                    }
                }
                foreach (var link in map.Links)
                {
                    var from = map.Stations.Single(s => s.ObjectId == link.FromStationId); var to = map.Stations.Single(s => s.ObjectId == link.ToStationId);
                    var supply = registry.StationMarketProfiles.GetDefinition(registry.StationMarketProfiles.GetIndex(from.MarketProfileId)).SupplyItemTypeIds;
                    var demand = registry.StationMarketProfiles.GetDefinition(registry.StationMarketProfiles.GetIndex(to.MarketProfileId)).DemandItemTypeIds;
                    Assert.All(link.ItemTypeIds, id => { Assert.Contains(id, supply); Assert.Contains(id, demand); });
                    if (supply.Length > 0) Assert.NotEmpty(link.ItemTypeIds);
                }
                CheckGeometry(snapshot);
                var saved = engine.CaptureSaveStateForTests(12300 * 300, SimulationSpeed.Speed0, 12300);
                using var loaded = new SimulationEngine(registry); loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(saved), true), true);
                Assert.Equal(JsonSerializer.Serialize(map), JsonSerializer.Serialize(loaded.CaptureSnapshotForTests(12300 * 300, SimulationSpeed.Speed0, 12300).ClusterMap));
                GeneratedWorldPersistenceTests.EqualWorld(engine.CaptureSnapshotForTests(15000 * 300, SimulationSpeed.Speed0, 15000), loaded.CaptureSnapshotForTests(15000 * 300, SimulationSpeed.Speed0, 15000));
            }
            catch (Exception e) { throw new Xunit.Sdk.XunitException($"seed={seed};scenario={scenario};clusters={clusters};stations={stations};belts={belts};config={JsonSerializer.Serialize(config)}: {e}"); }
        }
    }

    private static void CheckGeometry(AuthoritativeSnapshot s)
    {
        var map = s.ClusterMap!; var objects = s.Objects.ToDictionary(o => o.ObjectId);
        static double Distance(ObjectMotionSnapshot a, ObjectMotionSnapshot b) => double.Hypot(a.X - b.X, a.Y - b.Y);
        ObjectMotionSnapshot At(string id, long time) => OrbitalMotionMath.At(objects[id], objects[id].Orbit!, time);
        var groups = map.Clusters.Select(c =>
        {
            var ids = c.StationIds.Concat(map.ResourceBindings.Where(b => b.ClusterId == c.Id).Select(b => b.FieldId)).ToArray();
            return (Cluster: c, Min: ids.Min(id => objects[id].Orbit!.SemiMinorAxis), Max: ids.Max(id => objects[id].Orbit!.SemiMajorAxis));
        }).OrderBy(g => g.Min).ToArray();
        for (int i = 1; i < groups.Length; i++) Assert.True(groups[i].Min > groups[i - 1].Max, "Station/resource swept envelopes overlap");
        foreach (var c in map.Clusters)
            foreach (long time in new[] { 0, 1, 7, 30, 100, 365 }.Select(d => d * 288000L))
            {
                string anchor = c.StationIds[0];
                foreach (string id in c.StationIds) Assert.InRange(Math.Abs(Distance(objects[anchor], objects[id]) - Distance(At(anchor, time), At(id, time))), 0, 0.000001);
                foreach (var b in map.ResourceBindings.Where(b => b.ClusterId == c.Id)) Assert.InRange(Math.Abs(double.Hypot(b.OffsetX, b.OffsetY) - Distance(At(b.FieldId, time), At(b.AnchorStationId, time))), 0, 0.000001);
            }
        // Solve the first conjunction independently. Disjoint annuli additionally prove
        // all other relative phases, including extrema beyond the sampled 365 days.
        for (int i = 1; i < groups.Length; i++)
        {
            string a = groups[i - 1].Cluster.StationIds[0], b = groups[i].Cluster.StationIds[0];
            var oa = objects[a].Orbit!; var ob = objects[b].Orbit!;
            double wa = (oa.OrbitDirection == "clockwise" ? 1 : -1) * Math.Tau * 300 / oa.OrbitalPeriodMs;
            double wb = (ob.OrbitDirection == "clockwise" ? 1 : -1) * Math.Tau * 300 / ob.OrbitalPeriodMs;
            double phase = (oa.InitialPhase + oa.PhaseOffsetDegrees - ob.InitialPhase - ob.PhaseOffsetDegrees) * Math.PI / 180;
            double period = Math.Tau / Math.Abs(wa - wb), conjunction = ((-phase / (wa - wb)) % period + period) % period;
            Assert.True(double.IsFinite(conjunction) && conjunction <= long.MaxValue);
            Assert.True(Distance(At(a, (long)conjunction), At(b, (long)conjunction)) >= groups[i].Min - groups[i - 1].Max);
        }
    }

    [Fact]
    public void TradeGeographyHandoffIsComplete()
    {
        using var engine = MultiClusterPlacementTests.Create(5, 12);
        var s = engine.CaptureSnapshot(); var map = s.ClusterMap!;
        Assert.Contains(map.Clusters, c => c.Id == map.StartClusterId);
        Assert.All(map.Stations, m => Assert.Contains(s.Objects, o => o.ObjectId == m.ObjectId && o.ObjectType == "Station"));
        Assert.All(map.Links, l => { Assert.Contains(map.Stations, m => m.ObjectId == l.FromStationId); Assert.Contains(map.Stations, m => m.ObjectId == l.ToStationId); });
        var json = JsonSerializer.Serialize(map); Assert.DoesNotContain("travelTime", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("distance", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InvalidClusterPlacementIsAtomic() => new MultiClusterPlacementTests().InitialNeighbourDistancesAndImpossiblePlacement();
}
