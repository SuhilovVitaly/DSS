using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class MultiClusterPlacementTests
{
    [Fact]
    public void V2SweptLanesProveAllPhaseMinimum()
    {
        var candidate = V2Candidate(GenerationInputSchemaTests.V2Config() with
        { Clusters = GenerationInputSchemaTests.V2Config().Clusters! with { MinClusters = 5, MaxClusters = 5, MinStations = 12, MaxStations = 12 } });
        var state = candidate.World.GameState; var map = candidate.Map;
        double units = map.Geography!.ReferenceSpeedKmS * 86400 / 300 * 10;
        var radii = map.Clusters.Select(c => state.SpaceObjects.Single(o => o.ObjectId == c.StationIds[0]).Orbit!.SemiMajorAxis).Order().ToArray();
        for (int i = 1; i < radii.Length; i++) Assert.True(radii[i] - radii[i - 1] >= units);
        // Reverse triangle inequality: |p-q| >= ||p|-|q|| >= one day for all relative phases.
        foreach (int days in new[] { 0, 1, 30, 365 })
        {
            var stations = state.SpaceObjects.Where(o => map.Stations.Any(s => s.ObjectId == o.ObjectId))
                .Select(o => OrbitalMotionMath.At(new(o.ObjectId, o.PositionX, o.PositionY, 0, 0), o.Orbit!, days * 86400000L / 300)).ToArray();
            for (int i = 0; i < stations.Length; i++)
                for (int j = i + 1; j < stations.Length; j++) Assert.True(Distance(stations[i], stations[j]) / units >= 1 - 1e-9);
        }
    }

    [Fact]
    public void V2BoundaryClusterCountsKeepLocalNeighbours()
    {
        foreach (int count in new[] { 3, 5 })
            foreach (int stationCount in new[] { 10, 12 })
            {
                var config = GenerationInputSchemaTests.V2Config() with
                { Clusters = GenerationInputSchemaTests.V2Config().Clusters! with { MinClusters = count, MaxClusters = count, MinStations = stationCount, MaxStations = stationCount } };
                var candidate = V2Candidate(config);
                var state = candidate.World.GameState; var map = candidate.Map;
                double units = map.Geography!.ReferenceSpeedKmS * 86400 / 300 * 10;
                Assert.Equal(count, map.Clusters.Length);
                Assert.Equal(JsonSerializer.Serialize(candidate), JsonSerializer.Serialize(V2Candidate(config)));
                var centers = map.Clusters.Select(c =>
                {
                    var rows = state.SpaceObjects.Where(o => c.StationIds.Contains(o.ObjectId)).ToArray();
                    return (X: rows.Average(o => o.PositionX), Y: rows.Average(o => o.PositionY));
                }).ToArray();
                for (int i = 0; i < centers.Length; i++) Assert.InRange(centers.Where((_, j) => i != j)
                    .Min(c => double.Hypot(c.X - centers[i].X, c.Y - centers[i].Y)) / units, 30, 50);
                foreach (var cluster in map.Clusters)
                {
                    var stations = state.SpaceObjects.Where(o => cluster.StationIds.Contains(o.ObjectId)).ToArray();
                    Assert.Equal(stationCount, stations.Length);
                    Assert.Single(stations.Select(o => o.Orbit!.SemiMajorAxis).Distinct());
                    Assert.Single(stations.Select(o => o.Orbit!.OrbitalPeriodMs).Distinct());
                    foreach (var a in stations) Assert.InRange(stations.Where(b => b.ObjectId != a.ObjectId)
                        .Min(b => double.Hypot(a.PositionX - b.PositionX, a.PositionY - b.PositionY)) / units, 1, 3);
                    var belt = state.SolarSystem!.Belts.Single(b => b.Id == cluster.BeltId);
                    Assert.All(stations, s => Assert.InRange(s.Orbit!.SemiMajorAxis, belt.InnerRadius + 45, belt.OuterRadius - 45));
                }
            }
    }

    private static ClusterGenerationResult V2Candidate(SolarSystemGenerationConfig config)
    {
        var source = SeededWorldBootstrapTests.Scenario();
        var registry = SeededWorldBootstrapTests.Registry();
        var solar = SolarSystemGenerator.Generate(source, config, registry, 42);
        var candidate = StationClusterGenerator.Generate(solar, config.Clusters!, registry, 42);
        return StationGeographyV2.CompleteBootstrap(source, candidate, config, registry, 42);
    }

    [Fact]
    public void V2NarrowBeltFailsWithinAttemptBudget()
    {
        using var engine = StationGeographyBootstrapTests.Create();
        string before = ScenarioLoader.Serialize(engine.CaptureSaveState());
        var source = SeededWorldBootstrapTests.Scenario("PlayerShipOnly");
        var config = GenerationInputSchemaTests.V2Config() with { BeltWidthFraction = 0.0001 };
        var error = Assert.Throws<ScenarioException>(() => engine.LoadScenario(source, generation: config));
        Assert.Contains("attempts=128", error.Message);
        Assert.Contains("radial lanes", error.Message);
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }

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
        foreach (var a in centers) Assert.InRange(centers.Where(b => b != a).Min(b => Distance(a, b)) / 17280, 15, 35);
        string before = JsonSerializer.Serialize(snapshot);
        var source = SeededWorldBootstrapTests.Scenario();
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(source, generation: GenerationInputSchemaTests.Config() with
        { Clusters = LocalClusterGenerationTests.Config() with { MinClusters = 5, MaxClusters = 5, InterclusterMinDays = 1000, InterclusterMaxDays = 1001 } }));
        Assert.Equal(before, JsonSerializer.Serialize(engine.CaptureSnapshot() with { SnapshotSequence = snapshot.SnapshotSequence }));
    }

    private static double Distance(ObjectMotionSnapshot a, ObjectMotionSnapshot b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
}
