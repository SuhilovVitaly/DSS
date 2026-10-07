using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ClusterResourcePlacementTests
{
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
                Assert.Equal(Math.Sqrt(binding.OffsetX * binding.OffsetX + binding.OffsetY * binding.OffsetY),
                    Math.Sqrt(Math.Pow(futureA.X - futureB.X, 2) + Math.Pow(futureA.Y - futureB.Y, 2)), 6);
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
