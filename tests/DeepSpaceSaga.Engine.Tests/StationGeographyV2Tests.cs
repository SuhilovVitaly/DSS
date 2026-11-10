using System.Text.Json;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class StationGeographyV2Tests
{
    internal static ClusterGenerationResult Create(ulong seed = 42, string scenario = "Default", int count = 10)
    {
        var registry = SeededWorldBootstrapTests.Registry();
        var config = GenerationInputSchemaTests.V2Config() with
        { Clusters = GenerationInputSchemaTests.V2Config().Clusters! with { MinClusters = 1, MaxClusters = 1, MinStations = count, MaxStations = count } };
        var source = SeededWorldBootstrapTests.Scenario(scenario);
        var solar = SolarSystemGenerator.Generate(source, config, registry, seed);
        return StationClusterGenerator.Generate(solar, config.Clusters!, registry, seed);
    }

    [Fact]
    public void LegacyAndGeneratedStationsShareMinimumDays()
    {
        var result = Create();
        var stations = result.World.GameState.SpaceObjects.Where(o => result.Map.Stations.Any(s => s.ObjectId == o.ObjectId)).ToArray();
        Assert.Equal(10, stations.Length);
        Assert.Equal(2, result.Map.RulesVersion);
        Assert.Equal(6, result.Map.Geography!.ReferenceSpeedKmS);
        foreach (string id in new[] { "SPC-0002", "SPC-0005", "SPC-0006", "SPC-0007", "SPC-0008" })
            Assert.Contains(stations, s => s.ObjectId == id);
        foreach (var a in stations)
            foreach (var b in stations.Where(s => s.ObjectId != a.ObjectId))
                Assert.True(double.Hypot(a.PositionX - b.PositionX, a.PositionY - b.PositionY) / 17280 >= 1 - 1e-10);
        var original = SeededWorldBootstrapTests.Scenario().GameState.SpaceObjects.Where(o => o.ObjectType == "Station");
        foreach (var old in original)
        {
            var moved = stations.Single(s => s.ObjectId == old.ObjectId);
            Assert.Equal(old.Name, moved.Name);
            Assert.Equal(JsonSerializer.Serialize(old.Inventory), JsonSerializer.Serialize(moved.Inventory));
            Assert.NotEqual((old.PositionX, old.PositionY), (moved.PositionX, moved.PositionY));
        }
    }

    [Fact]
    public void EveryMemberHasOneToThreeDayNeighbour()
    {
        foreach (string scenario in new[] { "Default", "Docked", "Undocked", "MarketProfiles", "PlayerShipOnly" })
            foreach (int count in new[] { 10, 12 })
            {
                var result = Create(scenario: scenario, count: count);
                var stations = result.World.GameState.SpaceObjects.Where(o => result.Map.Stations.Any(s => s.ObjectId == o.ObjectId)).ToArray();
                double units = result.Map.Geography!.ReferenceSpeedKmS * 86400 / 300 * 10;
                Assert.Equal(5, stations.Select(s => s.MarketProfileId).Distinct().Count());
                Assert.All(stations, a => Assert.InRange(stations.Where(b => a.ObjectId != b.ObjectId)
                    .Min(b => double.Hypot(a.PositionX - b.PositionX, a.PositionY - b.PositionY) / units), 1, 3));
                var radii = stations.Select(s => s.Orbit!.SemiMajorAxis).Distinct().ToArray();
                Assert.Equal(count, radii.Length);
                Assert.InRange((radii.Max() - radii.Min()) / units, 7.5, 7.7);
                double phase = Math.Atan2(stations.Average(s => s.PositionX), -stations.Average(s => s.PositionY));
                var tangents = stations.Select(s => s.PositionX * Math.Cos(phase) + s.PositionY * Math.Sin(phase)).ToArray();
                Assert.InRange((tangents.Max() - tangents.Min()) / units, 7.0, 7.5);
                Assert.Single(stations.Select(s => s.Orbit!.OrbitalPeriodMs).Distinct());
                double diameter = stations.SelectMany(a => stations.Select(b => double.Hypot(a.PositionX - b.PositionX, a.PositionY - b.PositionY))).Max() / units;
                Assert.InRange(diameter, 10 - 1e-9, 16 + 1e-9);
            }
    }

    [Fact]
    public void SeedAndFailureAreDeterministic()
    {
        for (ulong seed = 1; seed <= 12; seed++)
        {
            var a = Create(seed);
            Assert.Equal(JsonSerializer.Serialize(a), JsonSerializer.Serialize(Create(seed)));
        }
        var registry = SeededWorldBootstrapTests.Registry();
        var config = GenerationInputSchemaTests.V2Config();
        var solar = SolarSystemGenerator.Generate(SeededWorldBootstrapTests.Scenario(), config, registry, 42);
        string before = JsonSerializer.Serialize(solar);
        var impossible = config.Clusters! with { DiameterMinDays = 1000, DiameterMaxDays = 2000 };
        string first = Assert.Throws<ScenarioException>(() => StationClusterGenerator.Generate(solar, impossible, registry, 42)).Message;
        Assert.Contains("attempts=128", first);
        Assert.Equal(first, Assert.Throws<ScenarioException>(() => StationClusterGenerator.Generate(solar, impossible, registry, 42)).Message);
        Assert.Equal(before, JsonSerializer.Serialize(solar));
        var collision = solar with
        {
            GameState = solar.GameState with
            {
                SpaceObjects = [.. solar.GameState.SpaceObjects,
                    solar.GameState.SpaceObjects[0] with { ObjectId = "SPC-0005", ObjectType = "Asteroid" }]
            }
        };
        Assert.Contains("non-station", Assert.Throws<ScenarioException>(() =>
            StationClusterGenerator.Generate(collision, config.Clusters!, registry, 42)).Message);
    }
}
