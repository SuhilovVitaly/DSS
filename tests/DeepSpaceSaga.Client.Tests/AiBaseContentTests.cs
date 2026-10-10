using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public sealed class AiBaseContentTests
{
    [Fact]
    public void AiContentLoadsAndIsBounded()
    {
        var config = EngineContentLoader.LoadSolarSystemGenerationConfig(DefaultSystemContentTests.Settings)!;
        Assert.Equal(new AiGenerationConfig(2, 4, 200, 1000, 64), config.Ai);
        Assert.Equal(config, SolarSystemGeneration.ValidateConfig(config));
    }

    [Fact]
    public void AiContentKeepsHumanMarkets()
    {
        var config = EngineContentLoader.LoadSolarSystemGenerationConfig(DefaultSystemContentTests.Settings)!;
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(DefaultSystemContentTests.Settings, out _, out _);
        foreach (string name in config.EnabledScenarios)
            foreach (ulong seed in new ulong[] { 1, 2, 42 })
                foreach (int count in new[] { 2, 4 })
                {
                    var source = ScenarioLoader.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Scenarios", name, "scenario.json"));
                    source = source with { GameState = source.GameState with { MasterSeed = seed } };
                    using var engine = new SimulationEngine(registry);
                    using var human = new SimulationEngine(registry);
                    engine.LoadScenario(source, generation: config with { Ai = config.Ai! with { MinBases = count, MaxBases = count } });
                    human.LoadScenario(source, generation: config with { Ai = null });
                    var snapshot = engine.CaptureSnapshot();
                    Assert.Equal(count, snapshot.AiMap!.Bases.Length);
                    Assert.Equal(JsonSerializer.Serialize(human.CaptureSnapshot().ClusterMap), JsonSerializer.Serialize(snapshot.ClusterMap));
                    foreach (var ai in snapshot.AiMap.Bases)
                    {
                        var obj = snapshot.Objects.Single(o => o.ObjectId == ai.ObjectId);
                        Assert.Equal(PlayerRelation.Enemy, obj.RelationToPlayer);
                        Assert.NotNull(obj.DisplayName);
                    }
                    var save = engine.CaptureSaveState();
                    foreach (var station in human.CaptureSaveState().GameState.SpaceObjects.Where(o => o.ObjectType == "Station"))
                        Assert.Equal(JsonSerializer.Serialize(station), JsonSerializer.Serialize(save.GameState.SpaceObjects.Single(o => o.ObjectId == station.ObjectId)));
                }
    }
}
