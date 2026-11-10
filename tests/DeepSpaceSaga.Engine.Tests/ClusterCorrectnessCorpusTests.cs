using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ClusterCorrectnessCorpusTests
{
    internal static (double Minimum, double NearestMinimum, double NearestMaximum, double LaneMinimum) VerifyV2Geography(ScenarioFile world, double scale = 1)
    {
        var state = world.GameState; var map = state.ClusterMap!;
        Assert.Equal(2, map.RulesVersion);
        var rules = map.Geography!; Assert.Equal(scale, rules.MinimumStationDays);
        double units = rules.ReferenceSpeedKmS * 86400 / 300 * 10 * scale;
        Assert.True(double.IsFinite(units) && units > 0);
        var objects = state.SpaceObjects.ToDictionary(o => o.ObjectId);
        var lanes = map.Clusters.Select(cluster =>
        {
            var first = objects[cluster.StationIds[0]].Orbit!;
            foreach (string id in cluster.StationIds)
            {
                var orbit = objects[id].Orbit!;
                Assert.Equal(orbit.SemiMajorAxis, orbit.SemiMinorAxis, 6);
                Assert.Equal(first.OrbitalPeriodMs, orbit.OrbitalPeriodMs);
                Assert.Equal(first.EpochSimulationTimeMs, orbit.EpochSimulationTimeMs);
                Assert.Equal(first.OrbitDirection, orbit.OrbitDirection);
            }
            var all = cluster.StationIds.Concat(map.ResourceBindings.Where(b => b.ClusterId == cluster.Id).Select(b => b.FieldId)).ToArray();
            double min = all.Min(id => objects[id].Orbit!.SemiMinorAxis), max = all.Max(id => objects[id].Orbit!.SemiMajorAxis);
            var belt = state.SolarSystem!.Belts.Single(b => b.Id == cluster.BeltId);
            Assert.InRange(min, belt.InnerRadius - 1e-6, belt.OuterRadius + 1e-6);
            Assert.InRange(max, belt.InnerRadius - 1e-6, belt.OuterRadius + 1e-6);
            return (Minimum: min, Maximum: max);
        }).OrderBy(l => l.Minimum).ToArray();
        double laneMinimum = double.PositiveInfinity;
        for (int i = 1; i < lanes.Length; i++)
        {
            // |r2-r1| is a lower bound at every relative phase, not a sampled claim.
            double gap = (lanes[i].Minimum - lanes[i - 1].Maximum) / units;
            Assert.True(gap >= 1 - 1e-9);
            laneMinimum = Math.Min(laneMinimum, gap);
        }
        double minimum = double.PositiveInfinity, nearestMinimum = double.PositiveInfinity, nearestMaximum = 0;
        foreach (long time in new[] { 0L, 288000, 30 * 288000L, 365 * 288000L })
        {
            var poses = objects.ToDictionary(p => p.Key, p => FullMapCorrectnessCorpusTests.Position(p.Value, time));
            double Distance(string a, string b) => double.Hypot(poses[a].X - poses[b].X, poses[a].Y - poses[b].Y) / units;
            for (int i = 0; i < map.Stations.Length; i++)
                for (int j = i + 1; j < map.Stations.Length; j++)
                {
                    double distance = Distance(map.Stations[i].ObjectId, map.Stations[j].ObjectId);
                    Assert.True(distance >= 1 - 1e-8, $"pair={map.Stations[i].ObjectId}/{map.Stations[j].ObjectId};motion={time};days={distance:R}");
                    minimum = Math.Min(minimum, distance);
                }
            foreach (var cluster in map.Clusters)
            {
                double diameter = 0;
                foreach (string id in cluster.StationIds)
                {
                    var distances = cluster.StationIds.Where(other => other != id).Select(other => Distance(id, other)).ToArray();
                    double nearest = distances.Min();
                    Assert.InRange(nearest, 1 - 1e-8, 3 + 1e-8);
                    nearestMinimum = Math.Min(nearestMinimum, nearest); nearestMaximum = Math.Max(nearestMaximum, nearest);
                    diameter = Math.Max(diameter, distances.Max());
                }
                Assert.InRange(diameter, 10 - 1e-8, 16 + 1e-8);
            }
            foreach (var binding in map.ResourceBindings)
            {
                double km = double.Hypot(poses[binding.FieldId].X - poses[binding.AnchorStationId].X,
                    poses[binding.FieldId].Y - poses[binding.AnchorStationId].Y) / 10;
                Assert.InRange(km, 2.5 * scale - 1e-6, 4.5 * scale + 1e-6);
            }
        }
        return (minimum, nearestMinimum, nearestMaximum, laneMinimum);
    }

    [Theory]
    [MemberData(nameof(SystemCorrectnessCorpusTests.V2Ranges), MemberType = typeof(SystemCorrectnessCorpusTests))]
    public void V2EveryPairAndNeighbourIndependentOracle(string scenario, string range, int seedStart)
    {
        Assert.Equal($"{seedStart:000}-{seedStart + 19:000}", range);
        for (ulong seed = (ulong)seedStart; seed < (ulong)seedStart + 20; seed++)
        {
            var config = SystemCorrectnessCorpusTests.V2BoundaryConfig(seed); ScenarioFile? world = null;
            try
            {
                using var engine = SystemCorrectnessCorpusTests.V2Create(scenario, seed, config);
                world = engine.CaptureSaveState(); VerifyV2Geography(world, config.DistanceScale);
                var map = world.GameState.ClusterMap!;
                foreach (var member in map.Stations)
                {
                    Assert.True(map.Links.Count(l => l.FromStationId == member.ObjectId) >= 2);
                    var seen = new HashSet<string> { member.ObjectId }; var queue = new Queue<string>(); queue.Enqueue(member.ObjectId);
                    while (queue.TryDequeue(out var id))
                        foreach (var link in map.Links.Where(l => l.FromStationId == id)) if (seen.Add(link.ToStationId)) queue.Enqueue(link.ToStationId);
                    Assert.Equal(map.Stations.Length, seen.Count);
                }
            }
            catch (Exception error) { SystemCorrectnessCorpusTests.CorpusFailure(error, scenario, seed, config, "cluster-oracle", world); }
        }
    }

    public static IEnumerable<object[]> Cases()
    {
        foreach (string scenario in new[] { "Default", "Default_500", "Docked", "Undocked", "MarketProfiles", "PlayerShipOnly" })
            foreach (int clusters in new[] { 3, 5 }) foreach (int stations in new[] { 10, 12 }) foreach (int belts in new[] { 2, 5 })
                foreach (int start in new[] { 1, 21, 41, 61, 81 }) yield return [scenario, clusters, stations, belts, $"{start:000}-{start + 19:000}", start];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void HundredSeedClusterBoundaryMatrix(string scenario, int clusters, int stations, int belts, string range, int seedStart)
    {
        Assert.Equal($"{seedStart:000}-{seedStart + 19:000}", range);
        var registry = SeededWorldBootstrapTests.Registry();
        var source = SeededWorldBootstrapTests.Scenario(scenario);
        var config = SystemCorrectnessCorpusTests.LegacyConfig();
        config = config with { MinBelts = belts, MaxBelts = belts, Clusters = config.Clusters! with { MinClusters = clusters, MaxClusters = clusters, MinStations = stations, MaxStations = stations } };
        var fields = JsonSerializer.Deserialize<StationResourceFieldConfig>(File.ReadAllText(Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Data/World/station-resource-fields.json")))!;
        for (ulong seed = (ulong)seedStart; seed < (ulong)seedStart + 20; seed++)
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
                var expected = engine.CaptureSnapshotForTests(15000 * 300, SimulationSpeed.Speed0, 15000);
                GeneratedWorldPersistenceTests.EqualWorld(expected,
                    loaded.CaptureSnapshotForTests(15000 * 300, SimulationSpeed.Speed0, 15000));
            }
            catch (Exception e) { SystemCorrectnessCorpusTests.CorpusFailure(e, scenario, seed, config, "legacy-cluster"); }
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
