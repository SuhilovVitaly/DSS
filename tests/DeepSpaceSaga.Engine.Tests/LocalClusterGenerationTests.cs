using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class LocalClusterGenerationTests
{
    internal static ClusterGenerationConfig Config(int count = 10) => new(1, 1, count, count, 0.5, 2, 3, 7, 15, 35);
    internal static SimulationEngine Create(string name = "Default", int count = 10, ulong seed = 42)
    {
        var engine = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        var source = SeededWorldBootstrapTests.Scenario(name);
        engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = seed } },
            generation: GenerationInputSchemaTests.Config() with { Clusters = Config(count) });
        return engine;
    }

    [Theory]
    [InlineData("Default", 10)]
    [InlineData("Default_500", 12)]
    [InlineData("Docked", 10)]
    [InlineData("Undocked", 12)]
    [InlineData("MarketProfiles", 10)]
    [InlineData("PlayerShipOnly", 12)]
    public void ScenarioStationsCountTowardQuota(string name, int count)
    {
        var original = SeededWorldBootstrapTests.Scenario(name);
        using var engine = Create(name, count);
        var snapshot = engine.CaptureSnapshot();
        Assert.Equal(count, snapshot.ClusterMap!.Stations.Length);
        Assert.Equal(count, snapshot.Objects.Count(o => o.ObjectType == "Station"));
        Assert.Equal(5, snapshot.ClusterMap.Stations.Select(s => s.MarketProfileId).Distinct().Count());
        var saved = engine.CaptureSaveState().GameState.SpaceObjects;
        foreach (var station in original.GameState.SpaceObjects.Where(o => o.ObjectType == "Station"))
        {
            var actual = saved.Single(o => o.ObjectId == station.ObjectId);
            Assert.Equal(station.Name, actual.Name);
            if (station.Inventory is { } inventory)
                foreach (var item in inventory) Assert.Contains(actual.Inventory!, i => i.ItemTypeId == item.ItemTypeId && i.Quantity == item.Quantity);
        }
    }

    [Fact]
    public void LocalGraphHasTwoCyclesAndReturnCargo()
    {
        for (ulong seed = 1; seed <= 16; seed++)
        {
            using var engine = Create(seed: seed);
            var map = engine.CaptureSnapshot().ClusterMap!;
            foreach (var station in map.Stations)
            {
                Assert.True(map.Links.Count(l => l.FromStationId == station.ObjectId) >= 2);
                var seen = new HashSet<string> { station.ObjectId };
                for (int i = 0; i < map.Stations.Length; i++)
                    foreach (var link in map.Links) if (seen.Contains(link.FromStationId)) seen.Add(link.ToStationId);
                Assert.Equal(map.Stations.Length, seen.Count);
                if (station.MarketProfileId == "market.transit")
                    Assert.All(map.Links.Where(l => l.FromStationId == station.ObjectId), l => Assert.Empty(l.ItemTypeIds));
                else Assert.Contains(map.Links, l => l.FromStationId == station.ObjectId && !l.ItemTypeIds.IsEmpty);
            }
            var edges = map.Links.Select(l => string.CompareOrdinal(l.FromStationId, l.ToStationId) < 0 ? (l.FromStationId, l.ToStationId) : (l.ToStationId, l.FromStationId)).Distinct().Count();
            Assert.True(edges - map.Stations.Length + 1 >= 2);
            Assert.Contains(map.Links, l => !l.ItemTypeIds.IsEmpty && map.Links.Any(r => r.FromStationId == l.ToStationId && r.ToStationId == l.FromStationId && !r.ItemTypeIds.IsEmpty));
            using var repeated = Create(seed: seed);
            Assert.Equal(JsonSerializer.Serialize(map), JsonSerializer.Serialize(repeated.CaptureSnapshot().ClusterMap));
        }
    }

    [Fact]
    public void RigidScenarioDistancesAndStartRadius()
    {
        using var engine = Create("MarketProfiles");
        var snapshot = engine.CaptureSnapshot();
        var ship = snapshot.Objects.Single(o => o.ObjectId == snapshot.PlayerShipObjectId);
        Assert.InRange(Math.Sqrt(ship.X * ship.X + ship.Y * ship.Y) / 10 / ship.MaxSpeedKmS!.Value * 300 / 86400, 50, 75);
        var stations = snapshot.Objects.Where(o => o.ObjectType == "Station").ToArray();
        foreach (long time in new long[] { 1000, 28800000, 100000000 })
            foreach (var a in stations)
                foreach (var b in stations)
                {
                    var x = OrbitalMotionMath.At(a, a.Orbit!, time);
                    var y = OrbitalMotionMath.At(b, b.Orbit!, time);
                    Assert.Equal(Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2)), Math.Sqrt(Math.Pow(x.X - y.X, 2) + Math.Pow(x.Y - y.Y, 2)), 6);
                }
    }

    [Fact]
    public void PreservedScenarioNeighbourException()
    {
        var original = SeededWorldBootstrapTests.Scenario("MarketProfiles").GameState.SpaceObjects.Where(o => o.ObjectType == "Station").ToDictionary(o => o.ObjectId);
        using var engine = Create("MarketProfiles");
        var snapshot = engine.CaptureSnapshot();
        var objects = snapshot.Objects.ToDictionary(o => o.ObjectId);
        foreach (var a in original.Values)
            foreach (var b in original.Values)
                Assert.Equal(StationClusterGenerator.Distance(a, b), Distance(objects[a.ObjectId], objects[b.ObjectId]), 6);
        foreach (var link in snapshot.ClusterMap!.Links)
        {
            if (original.ContainsKey(link.FromStationId) && original.ContainsKey(link.ToStationId)) continue;
            Assert.InRange(Distance(objects[link.FromStationId], objects[link.ToStationId]) / 11520, 0.5 - 1e-8, 2 + 1e-8);
        }
    }

    private static double Distance(ObjectMotionSnapshot a, ObjectMotionSnapshot b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
}
