using System.Text.Json;
using DeepSpaceSaga.Engine.Scenario;
using Xunit.Abstractions;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class StationGeographyBootstrapTests(ITestOutputHelper output)
{
    internal static SimulationEngine Create(string scenario = "Default", SolarSystemGenerationConfig? config = null, ulong seed = 42)
    {
        var engine = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        engine.ConfigureStationResourceFields(JsonSerializer.Deserialize<StationResourceFieldConfig>(File.ReadAllText(
            Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Data/World/station-resource-fields.json")))!);
        config ??= GenerationInputSchemaTests.V2Config() with
        { Clusters = GenerationInputSchemaTests.V2Config().Clusters! with { MinClusters = 1, MaxClusters = 1 } };
        var source = SeededWorldBootstrapTests.Scenario(scenario);
        try { engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = seed } }, generation: config); }
        catch { engine.Dispose(); throw; }
        return engine;
    }

    [Fact]
    public void DefaultPublishesOnlyCompleteV2World()
    {
        using var engine = Create();
        var save = engine.CaptureSaveState(); var snapshot = engine.CaptureSnapshot();
        Assert.Equal(2, snapshot.SolarSystemMap!.GeneratorVersion);
        Assert.Equal(2, snapshot.ClusterMap!.RulesVersion);
        Assert.Null(save.GameState.TradingMapGeneration);
        Assert.NotNull(save.GameState.TradingMap);
        var objects = save.GameState.SpaceObjects.ToDictionary(o => o.ObjectId);
        double units = snapshot.ClusterMap.Geography!.ReferenceSpeedKmS * 86400 / 300 * 10;
        var player = objects[save.GameState.PlayerShipObjectId];
        Assert.InRange(double.Hypot(player.PositionX, player.PositionY) / units, 50, 75);
        foreach (var a in snapshot.ClusterMap.Stations)
            foreach (var b in snapshot.ClusterMap.Stations.Where(b => b.ObjectId != a.ObjectId))
                Assert.True(double.Hypot(objects[a.ObjectId].PositionX - objects[b.ObjectId].PositionX,
                    objects[a.ObjectId].PositionY - objects[b.ObjectId].PositionY) / units >= 1 - 1e-10);
        Assert.NotEmpty(save.GameState.StationResourceFields!.Asteroids);
        foreach (var resource in save.GameState.StationResourceFields.Asteroids)
        {
            var asteroid = objects[resource.ObjectId]; var owner = objects[resource.StationObjectId];
            Assert.InRange(double.Hypot(asteroid.PositionX - owner.PositionX, asteroid.PositionY - owner.PositionY) / 10, 2.5 - 1e-8, 4.5 + 1e-8);
        }
        Assert.Equal(save.GameState.StationResourceFields.Asteroids.Count, snapshot.ClusterMap.ResourceBindings.Length);
        output.WriteLine("Default seed=42; pair distances and nearest are calendar days at frozen reference speed.");
        foreach (var a in snapshot.ClusterMap.Stations)
        {
            var distances = snapshot.ClusterMap.Stations.Where(b => b.ObjectId != a.ObjectId).Select(b =>
                (Id: b.ObjectId, Days: double.Hypot(objects[a.ObjectId].PositionX - objects[b.ObjectId].PositionX,
                    objects[a.ObjectId].PositionY - objects[b.ObjectId].PositionY) / units)).ToArray();
            output.WriteLine($"nearest {a.ObjectId}: {distances.Min(d => d.Days):R}");
            foreach (var b in distances.Where(b => string.CompareOrdinal(a.ObjectId, b.Id) < 0))
                output.WriteLine($"pair {a.ObjectId}/{b.Id}: {b.Days:R}");
        }
        output.WriteLine("Authoritative snapshot: " + JsonSerializer.Serialize(snapshot));
    }

    [Fact]
    public void InvalidV2WorldLeavesRunningSessionUntouched()
    {
        using var engine = Create();
        string before = ScenarioLoader.Serialize(engine.CaptureSaveState());
        var config = GenerationInputSchemaTests.V2Config() with { OrbitClearanceWorld = 1e30, MaxPlacementAttempts = 2 };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(SeededWorldBootstrapTests.Scenario(), generation: config));
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }

    [Fact]
    public void PreservedStationPayloadSurvivesReposition()
    {
        var original = SeededWorldBootstrapTests.Scenario("MarketProfiles");
        using var engine = Create("MarketProfiles");
        var state = engine.CaptureSaveState().GameState;
        foreach (var old in original.GameState.SpaceObjects.Where(o => o.ObjectType == "Station"))
        {
            var current = state.SpaceObjects.Single(o => o.ObjectId == old.ObjectId);
            Assert.Equal(old.Name, current.Name); Assert.Equal(old.MarketProfileId, current.MarketProfileId);
            foreach (var item in old.Inventory ?? []) Assert.Contains(current.Inventory!, i => i.ItemTypeId == item.ItemTypeId && i.Quantity == item.Quantity);
            Assert.Equal(old.PortFeeCreditsPerDay, current.PortFeeCreditsPerDay);
        }
        var oldShip = original.GameState.SpaceObjects.Single(o => o.ObjectId == original.GameState.PlayerShipObjectId);
        var ship = state.SpaceObjects.Single(o => o.ObjectId == state.PlayerShipObjectId);
        Assert.Equal(oldShip.IsDocked, ship.IsDocked);
        Assert.Equal(oldShip.DockedStationObjectId, ship.DockedStationObjectId);
    }
}
