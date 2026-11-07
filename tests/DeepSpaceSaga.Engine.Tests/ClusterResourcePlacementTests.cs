using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Motion;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ClusterResourcePlacementTests
{
    [Fact]
    public void V2ResourcesFollowFinalStationCoordinates()
    {
        using var engine = StationGeographyBootstrapTests.Create(config: GenerationInputSchemaTests.V2Config());
        var state = engine.CaptureSaveState().GameState; var snapshot = engine.CaptureSnapshot();
        var objects = state.SpaceObjects.ToDictionary(o => o.ObjectId);
        foreach (var resource in state.StationResourceFields!.Asteroids)
        {
            var a = objects[resource.ObjectId]; var owner = objects[resource.StationObjectId];
            Assert.InRange(double.Hypot(a.PositionX - owner.PositionX, a.PositionY - owner.PositionY) / 10, 2.5 - 1e-8, 4.5 + 1e-8);
            var shown = snapshot.Objects.Single(o => o.ObjectId == resource.ObjectId).Survey!;
            Assert.False(shown.CompositionKnown); Assert.Null(shown.CompositionType); Assert.Empty(shown.Resources);
        }
    }

    [Fact]
    public void V2ResourceManifestIsNotGeneratedTwice()
    {
        var registry = SeededWorldBootstrapTests.Registry();
        var rules = JsonSerializer.Deserialize<StationResourceFieldConfig>(File.ReadAllText(Path.Combine(
            SeededWorldBootstrapTests.ClientRoot, "Data/World/station-resource-fields.json")))!;
        using var legacy = new SimulationEngine(registry);
        legacy.ConfigureStationResourceFields(rules);
        var original = SeededWorldBootstrapTests.Scenario();
        legacy.LoadScenario(original with { GameState = original.GameState with { MasterSeed = 42 } });
        var materialized = legacy.CaptureSaveState().GameState;
        var source = original with
        {
            GameState = original.GameState with
            {
                MasterSeed = 42, SpaceObjects = materialized.SpaceObjects.Select(o => o with { MarketBudgetCredits = null }).ToArray(), TradingMapGeneration = null,
                TradingMap = materialized.TradingMap, StationResourceFields = materialized.StationResourceFields
            }
        };
        using var engine = new SimulationEngine(registry);
        engine.ConfigureStationResourceFields(rules with { OuterRadiusKm = 5 });
        engine.LoadScenario(source, generation: GenerationInputSchemaTests.V2Config());
        var state = engine.CaptureSaveState().GameState;
        Assert.Equal(JsonSerializer.Serialize(source.GameState.StationResourceFields!.Rules), JsonSerializer.Serialize(state.StationResourceFields!.Rules));
        var objects = state.SpaceObjects.ToDictionary(o => o.ObjectId);
        foreach (var resource in source.GameState.StationResourceFields!.Asteroids)
        {
            var before = source.GameState.SpaceObjects.Single(o => o.ObjectId == resource.ObjectId);
            var after = objects[resource.ObjectId]; var owner = objects[resource.StationObjectId];
            Assert.InRange(double.Hypot(after.PositionX - owner.PositionX, after.PositionY - owner.PositionY) / 10, 2.5 - 1e-8, 4.5 + 1e-8);
            Assert.Equal(before.MassKg, after.MassKg); Assert.Equal(before.CompositionType, after.CompositionType);
        }
        foreach (var resource in source.GameState.StationResourceFields!.Asteroids)
            Assert.Equal(JsonSerializer.Serialize(resource), JsonSerializer.Serialize(state.StationResourceFields!.Asteroids.Single(r => r.ObjectId == resource.ObjectId)));
        foreach (var stream in source.GameState.StationResourceFields.RngStreams)
            Assert.Contains(state.StationResourceFields!.RngStreams, r => r == stream);
        var repeated = StationResourceFields.ExtendForClusters(state, 42, rules, registry);
        Assert.Equal(JsonSerializer.Serialize(state.StationResourceFields), JsonSerializer.Serialize(repeated.StationResourceFields));
        Assert.Equal(state.SpaceObjects.Count, repeated.SpaceObjects.Count);
        StationResourceFields.ValidateSaved(ScenarioGroupPlacement.InitialGeometry(state), registry);
    }

    [Fact]
    public void V2RigidResourceOffsetsSurviveOrbit()
    {
        using var engine = StationGeographyBootstrapTests.Create(config: GenerationInputSchemaTests.V2Config());
        var snapshot = engine.CaptureSnapshot(); var objects = snapshot.Objects.ToDictionary(o => o.ObjectId);
        foreach (int day in new[] { 0, 1, 30, 365 })
            foreach (var binding in snapshot.ClusterMap!.ResourceBindings)
            {
                var asteroid = objects[binding.FieldId]; var owner = objects[binding.AnchorStationId];
                var a = OrbitalMotionMath.At(asteroid, asteroid.Orbit!, day * 86400000L / 300);
                var b = OrbitalMotionMath.At(owner, owner.Orbit!, day * 86400000L / 300);
                Assert.InRange(Math.Abs(double.Hypot(a.X - b.X, a.Y - b.Y) - double.Hypot(binding.OffsetX, binding.OffsetY)), 0, 1e-6);
            }
    }

    internal static SimulationEngine Create(ulong seed = 42)
    {
        var engine = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        var config = EngineContentLoader.LoadSolarSystemGenerationConfig(Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Settings.json"))!;
        var rules = JsonSerializer.Deserialize<DeepSpaceSaga.Engine.Scenario.StationResourceFieldConfig>(File.ReadAllText(Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Data/World/station-resource-fields.json")))!;
        engine.ConfigureStationResourceFields(rules);
        var source = SeededWorldBootstrapTests.Scenario();
        engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = seed } }, generation: config);
        return engine;
    }

    [Fact]
    public void ClusterResourcesMatchEconomicRoles()
    {
        using var engine = Create();
        var saved = engine.CaptureSaveState(); var snapshot = engine.CaptureSnapshot();
        var fields = saved.GameState.StationResourceFields!;
        Assert.Equal(fields.Asteroids.Count, snapshot.ClusterMap!.ResourceBindings.Length);
        foreach (var member in snapshot.ClusterMap.Stations)
        {
            var role = fields.Rules.Roles.Single(r => r.MarketProfileId == member.MarketProfileId);
            foreach (var kind in role.Fields)
                Assert.Equal(kind.AsteroidCount, fields.Asteroids.Count(a => a.StationObjectId == member.ObjectId && a.FieldKindId == kind.FieldKindId));
        }
        Assert.All(fields.Asteroids, a => Assert.Equal(1000, a.Resources.Sum(r => r.Permille)));
        Assert.All(snapshot.Objects.Where(o => o.Survey is not null), o => Assert.True(o.Survey!.CompositionKnown));
    }

    [Fact]
    public void ResourcesRotateAndReplay()
    {
        using var a = Create(); using var b = Create();
        Assert.Equal(JsonSerializer.Serialize(a.CaptureSnapshot()), JsonSerializer.Serialize(b.CaptureSnapshot()));
        var first = a.CaptureSnapshot();
        foreach (int days in new[] { 1, 7, 30, 100, 365 })
            foreach (var binding in first.ClusterMap!.ResourceBindings)
            {
                var asteroid = first.Objects.Single(o => o.ObjectId == binding.FieldId);
                var owner = first.Objects.Single(o => o.ObjectId == binding.AnchorStationId);
                var futureA = OrbitalMotionMath.At(asteroid, asteroid.Orbit!, days * 86400000L / 300);
                var futureB = OrbitalMotionMath.At(owner, owner.Orbit!, days * 86400000L / 300);
                Assert.InRange(Math.Abs(Math.Sqrt(binding.OffsetX * binding.OffsetX + binding.OffsetY * binding.OffsetY) -
                    Math.Sqrt(Math.Pow(futureA.X - futureB.X, 2) + Math.Pow(futureA.Y - futureB.Y, 2))), 0, 1e-6);
            }
        var save = a.CaptureSaveState();
        using var restored = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        restored.LoadScenario(DeepSpaceSaga.Engine.Scenario.ScenarioLoader.LoadFromJson(DeepSpaceSaga.Engine.Scenario.ScenarioLoader.Serialize(save), true), true);
        Assert.Equal(JsonSerializer.Serialize(save.GameState.StationResourceFields), JsonSerializer.Serialize(restored.CaptureSaveState().GameState.StationResourceFields));
    }

    [Fact]
    public void FieldsDoNotSupplyMarkets()
    {
        using var fields = Create();
        var source = SeededWorldBootstrapTests.Scenario();
        var config = EngineContentLoader.LoadSolarSystemGenerationConfig(Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Settings.json"))!;
        using var control = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        control.LoadScenario(source with { GameState = source.GameState with { MasterSeed = 42 } }, generation: config);
        fields.CaptureSnapshotForTests(86400000, SimulationSpeed.Speed0, 288000);
        control.CaptureSnapshotForTests(86400000, SimulationSpeed.Speed0, 288000);
        Assert.Equal(JsonSerializer.Serialize(fields.CaptureMarketDiagnosticsForTests()), JsonSerializer.Serialize(control.CaptureMarketDiagnosticsForTests()));
    }
}
