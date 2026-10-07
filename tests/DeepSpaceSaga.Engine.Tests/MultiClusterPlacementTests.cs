using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class MultiClusterPlacementTests
{
    internal static SimulationEngine Create(int clusters = 3, int stations = 10, ulong seed = 42)
    {
        var engine = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        var source = SeededWorldBootstrapTests.Scenario("MarketProfiles");
        engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = seed } }, generation:
            GenerationInputSchemaTests.Config() with { Clusters = LocalClusterGenerationTests.Config(stations) with { MinClusters = clusters, MaxClusters = clusters } });
        return engine;
    }

    [Theory]
    [InlineData(3, 10)]
    [InlineData(5, 12)]
    public void SameSeedDifferentSpecializations(int clusters, int stations)
    {
        foreach (ulong seed in new ulong[] { 1, 2, 42 })
        {
            using var a = Create(clusters, stations, seed); using var b = Create(clusters, stations, seed);
            var snapshot = a.CaptureSnapshot();
            Assert.Equal(JsonSerializer.Serialize(snapshot), JsonSerializer.Serialize(b.CaptureSnapshot()));
            Assert.Equal(clusters, snapshot.ClusterMap!.Clusters.Length);
            Assert.True(snapshot.ClusterMap.Clusters.Select(c => c.Specialization).Distinct().Count() >= 2);
            foreach (var cluster in snapshot.ClusterMap.Clusters)
            {
                Assert.Equal(stations, cluster.StationIds.Length);
                Assert.Equal(5, snapshot.ClusterMap.Stations.Where(s => s.ClusterId == cluster.Id).Select(s => s.MarketProfileId).Distinct().Count());
            }
        }
    }

    [Fact]
    public void RigidGroupsFor365Days()
    {
        using var engine = Create(5, 12);
        var snapshot = engine.CaptureSnapshot();
        foreach (int days in new[] { 0, 1, 7, 30, 100, 365 })
            foreach (var cluster in snapshot.ClusterMap!.Clusters)
            {
                var members = snapshot.Objects.Where(o => cluster.StationIds.Contains(o.ObjectId)).ToArray();
                foreach (var a in members)
                    foreach (var b in members)
                    {
                        long time = days * 86400000L / 300;
                        Assert.Equal(Distance(a, b), Distance(OrbitalMotionMath.At(a, a.Orbit!, time), OrbitalMotionMath.At(b, b.Orbit!, time)), 6);
                    }
            }
        var envelopes = snapshot.ClusterMap!.Clusters.Select(c =>
        {
            var members = snapshot.Objects.Where(o => c.StationIds.Contains(o.ObjectId)).ToArray();
            return (Min: members.Min(o => o.Orbit!.SemiMinorAxis), Max: members.Max(o => o.Orbit!.SemiMajorAxis));
        }).OrderBy(c => c.Min).ToArray();
        for (int i = 1; i < envelopes.Length; i++) Assert.True(envelopes[i].Min > envelopes[i - 1].Max);
        // Disjoint radial envelopes prove separation for every relative phase, including conjunction.
        Assert.True(snapshot.ClusterMap.Clusters.Select(c => snapshot.Objects.First(o => c.StationIds.Contains(o.ObjectId)).Orbit!.OrbitalPeriodMs).Distinct().Count() > 1);
    }

    [Fact]
    public void InitialNeighbourDistancesAndImpossiblePlacement()
    {
        using var engine = Create(5, 10);
        var snapshot = engine.CaptureSnapshot();
        var centers = snapshot.ClusterMap!.Clusters.Select(c =>
        {
            var objects = snapshot.Objects.Where(o => c.StationIds.Contains(o.ObjectId)).ToArray();
            return new ObjectMotionSnapshot(c.Id, objects.Average(o => o.X), objects.Average(o => o.Y), 0, 0);
        }).ToArray();
        foreach (var a in centers) Assert.InRange(centers.Where(b => b != a).Min(b => Distance(a, b)) / 11520, 15, 35);
        string before = JsonSerializer.Serialize(snapshot);
        var source = SeededWorldBootstrapTests.Scenario();
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(source, generation: GenerationInputSchemaTests.Config() with
        { Clusters = LocalClusterGenerationTests.Config() with { MinClusters = 5, MaxClusters = 5, InterclusterMinDays = 1000, InterclusterMaxDays = 1001 } }));
        Assert.Equal(before, JsonSerializer.Serialize(engine.CaptureSnapshot() with { SnapshotSequence = snapshot.SnapshotSequence }));
    }

    private static double Distance(ObjectMotionSnapshot a, ObjectMotionSnapshot b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
}
