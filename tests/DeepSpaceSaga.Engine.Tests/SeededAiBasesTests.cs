using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class SeededAiBasesTests
{
    internal static SolarSystemGenerationConfig Config() => GenerationInputSchemaTests.Config() with
    {
        Clusters = LocalClusterGenerationTests.Config() with { MinClusters = 3, MaxClusters = 3 },
        Ai = new(2, 4, 200, 1000, 64)
    };

    internal static SimulationEngine Create(ulong seed = 42, SolarSystemGenerationConfig? config = null)
    {
        var engine = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        var source = SeededWorldBootstrapTests.Scenario("MarketProfiles");
        engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = seed } }, generation: config ?? Config());
        return engine;
    }

    [Theory]
    [InlineData(1UL)]
    [InlineData(2UL)]
    [InlineData(42UL)]
    public void AiBaseSeedAndParentOrbit(ulong seed)
    {
        using var a = Create(seed); using var b = Create(seed);
        var snapshot = a.CaptureSnapshot();
        Assert.Equal(JsonSerializer.Serialize(snapshot), JsonSerializer.Serialize(b.CaptureSnapshot()));
        var bases = snapshot.AiMap!.Bases;
        Assert.InRange(bases.Length, 2, 4);
        Assert.Contains(bases, x => x.BaseType == "Planetary");
        Assert.Contains(bases, x => x.BaseType == "Orbital");
        Assert.True(bases.Count(x => x.BaseType == "Planetary") < snapshot.SolarSystemMap!.Planets.Length);
        foreach (var ai in bases)
        {
            var obj = snapshot.Objects.Single(o => o.ObjectId == ai.ObjectId);
            Assert.Equal(PlayerRelation.Enemy, obj.RelationToPlayer);
            Assert.Contains("ИИ", obj.DisplayName);
            foreach (long time in new[] { 0L, 86400000L / 300, 365 * 86400000L / 300 })
            {
                var expectedOrbit = ai.ParentObjectId is { } parent
                    ? snapshot.Objects.Single(o => o.ObjectId == parent).Orbit! : ai.Orbit!;
                var expected = OrbitalMotionMath.At(new("expected", 0, 0, 0, 0), expectedOrbit, time);
                var actual = a.CaptureSnapshotForTests(simulationTimeMs: time).Objects.Single(o => o.ObjectId == ai.ObjectId);
                Assert.Equal(expected.X, actual.X, 7); Assert.Equal(expected.Y, actual.Y, 7);
            }
        }
    }

    [Fact]
    public void NoHumanPlanetarySettlement()
    {
        using var engine = Create(); using var baseline = Create(config: Config() with { Ai = null });
        var save = engine.CaptureSaveState();
        Assert.Equal(JsonSerializer.Serialize(baseline.CaptureSnapshot().ClusterMap), JsonSerializer.Serialize(engine.CaptureSnapshot().ClusterMap));
        foreach (var ai in save.GameState.AiMap!.Bases)
        {
            var obj = save.GameState.SpaceObjects.Single(o => o.ObjectId == ai.ObjectId);
            Assert.Null(obj.MarketProfileId); Assert.Empty(obj.Inventory ?? []); Assert.Empty(obj.ProducingModules ?? []);
        }
        foreach (var human in baseline.CaptureSaveState().GameState.SpaceObjects.Where(o => o.ObjectType == "Station"))
        {
            var actual = save.GameState.SpaceObjects.Single(o => o.ObjectId == human.ObjectId);
            Assert.Equal(JsonSerializer.Serialize(human), JsonSerializer.Serialize(actual));
            Assert.DoesNotContain(save.GameState.AiMap.Bases, b => b.ObjectId == human.ObjectId);
        }
    }

    [Fact]
    public void InvalidParentAndDuplicateBaseIdRejected()
    {
        using var engine = Create();
        var save = engine.CaptureSaveState();
        var map = save.GameState.AiMap!;
        string before = ScenarioLoader.Serialize(save);
        foreach (var broken in new[]
        {
            map with { Bases = map.Bases.SetItem(0, map.Bases[0] with { ParentObjectId = "missing" }) },
            map with { Bases = map.Bases.Add(map.Bases[0] with { ObjectId = map.Bases[0].ObjectId.ToLowerInvariant() }) },
            map with { Bases = map.Bases.SetItem(0, map.Bases[0] with { Owner = "Human" }) }
        })
        {
            Assert.Throws<ScenarioException>(() => engine.LoadScenario(save with { GameState = save.GameState with { AiMap = broken } }, isSave: true));
            Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
        }
        using var restored = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        restored.LoadScenario(ScenarioLoader.LoadFromJson(before, true), isSave: true);
        Assert.Equal(JsonSerializer.Serialize(map), JsonSerializer.Serialize(restored.CaptureSnapshot().AiMap));
        Assert.Throws<DeepSpaceSaga.Engine.Content.ContentException>(() => SolarSystemGeneration.ValidateConfig(Config() with { Ai = Config().Ai! with { PatrolRadiusKm = double.NaN } }));
    }
}
