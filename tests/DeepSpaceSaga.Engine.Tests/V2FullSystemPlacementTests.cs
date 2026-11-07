using System.Text.Json;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class V2FullSystemPlacementTests
{
    internal static SolarSystemGenerationConfig Config()
    {
        var content = EngineContentLoader.LoadSolarSystemGenerationConfig(Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Settings.json"))!;
        return content with { GeneratorVersion = 2, BeltWidthFraction = 0.20, Clusters = new(3, 5, 10, 12, 1, 3, 10, 16, 30, 50) };
    }

    [Fact]
    public void AiPlacementRespectsV2TradingNetwork()
    {
        foreach (ulong seed in new ulong[] { 1, 42, 100 })
        {
            using var engine = StationGeographyBootstrapTests.Create(config: Config(), seed: seed);
            var state = engine.CaptureSaveState().GameState;
            var proof = engine.CaptureAiPlacementValidation()!;
            Assert.True(proof.IsValid); Assert.Empty(proof.Violations);
            Assert.Equal(365 * 86400000L, proof.HorizonGameTimeMs);
            Assert.All(proof.Connectivity, row => Assert.Equal(1, row.Components));
            Assert.NotEmpty(proof.Checks); Assert.All(proof.Checks, row => Assert.True(row.Clearance > 0));
            var objects = state.SpaceObjects.ToDictionary(o => o.ObjectId);
            foreach (var ai in state.AiMap!.Bases)
            {
                Assert.DoesNotContain(state.ClusterMap!.Stations, s => s.ObjectId == ai.ObjectId);
                Assert.Null(objects[ai.ObjectId].MarketProfileId);
                var territory = state.AiMap.Territories.Single(t => t.BaseObjectId == ai.ObjectId);
                foreach (var station in state.ClusterMap.Stations)
                    Assert.True(double.Hypot(objects[station.ObjectId].PositionX - objects[ai.ObjectId].PositionX,
                        objects[station.ObjectId].PositionY - objects[ai.ObjectId].PositionY) > territory.PatrolRadiusKm * 10);
            }
            Assert.InRange(proof.Attempts, 1, Config().Ai!.MaxPlacementAttempts);
        }
    }

    [Fact]
    public void EnvironmentAndPoiAnchorsRemainValid()
    {
        using var engine = StationGeographyBootstrapTests.Create(config: Config());
        var state = engine.CaptureSaveState().GameState; var map = state.AiMap!;
        Assert.Equal(5, map.Fields.Length); Assert.Equal(2, map.PointsOfInterest.Length);
        Assert.Equal(1, map.RulesVersion); Assert.Equal(2, state.ClusterMap!.RulesVersion);
        var objects = state.SpaceObjects.ToDictionary(o => o.ObjectId);
        foreach (var field in map.Fields.Where(f => f.ParentObjectId is not null)) Assert.True(objects.ContainsKey(field.ParentObjectId!));
        foreach (var poi in map.PointsOfInterest)
        {
            Assert.False(string.IsNullOrWhiteSpace(poi.Description));
            if (poi.ParentObjectId is not null) Assert.True(objects.ContainsKey(poi.ParentObjectId));
            else Assert.NotNull(poi.Orbit);
        }
        Assert.All(state.SpaceObjects.Where(o => o.Orbit is not null), o => Assert.True(o.Orbit!.SemiMajorAxis <= state.SolarSystem!.SystemRadius));
        EnvironmentFieldGenerator.ValidateWorld(state); PointOfInterestGenerator.ValidateWorld(state);
        var registry = SeededWorldBootstrapTests.Registry();
        var clusterMap = state.ClusterMap!;
        foreach (var invalid in new[]
        {
            clusterMap with { Geography = null },
            clusterMap with { Geography = clusterMap.Geography! with { ReferenceSpeedKmS = 0 } },
            clusterMap with { Geography = clusterMap.Geography! with { ReferenceSpeedKmS = double.NaN } },
            clusterMap with { Geography = clusterMap.Geography! with { MinimumStationDays = 0.5 } },
            clusterMap with { RulesVersion = 1 }
        }) Assert.Throws<ScenarioException>(() => StationClusterSaveValidation.Validate(state with { ClusterMap = invalid }, registry));
        using var repeat = StationGeographyBootstrapTests.Create(config: Config());
        Assert.Equal(JsonSerializer.Serialize(map), JsonSerializer.Serialize(repeat.CaptureSnapshot().AiMap));
    }

    [Fact]
    public void PlacementFailureIsBoundedAndDiagnostic()
    {
        using var engine = StationGeographyBootstrapTests.Create();
        string before = ScenarioLoader.Serialize(engine.CaptureSaveState());
        var source = SeededWorldBootstrapTests.Scenario();
        source = source with { GameState = source.GameState with { MasterSeed = 42 } };
        var config = Config() with { Ai = Config().Ai! with { PatrolRadiusKm = 1e15, MaxPlacementAttempts = 2 } };
        var error = Assert.Throws<ScenarioException>(() => engine.LoadScenario(source, generation: config));
        Assert.Contains("ai/v1 seed=42 attempts=2", error.Message);
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }
}
