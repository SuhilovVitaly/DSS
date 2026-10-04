using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
using static DeepSpaceSaga.Engine.Tests.AutomaticDefenseTests;
namespace DeepSpaceSaga.Engine.Tests;

public class CountermeasureSaveLoadTests
{
    [Theory]
    [InlineData(1000)]
    [InlineData(4000)]
    [InlineData(6000)]
    [InlineData(21000)]
    public void All_phases_resume_exactly_through_real_serializer_loader(long saveAt)
    {
        using var continuous = Create(DefenseScenario());
        continuous.ReceiveCommand(Fire("fire")); At(continuous, 0); At(continuous, saveAt);
        var saved = continuous.CaptureSaveStateForTests(saveAt, SimulationSpeed.Speed0);
        Assert.Equal(11, saved.SaveFormatVersion);
        var loaded = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(saved), true);
        using var resumed = new SimulationEngine(Registry.Value);
        resumed.LoadScenario(loaded, isSave: true);
        Assert.Equal(JsonSerializer.Serialize(saved.GameState.DefenseState), JsonSerializer.Serialize(resumed.CaptureSaveState().GameState.DefenseState));
        var a = At(continuous, 25000); var b = At(resumed, 25000);
        Assert.Equal(JsonSerializer.Serialize(a.CombatJournal), JsonSerializer.Serialize(b.CombatJournal));
        Assert.Equal(JsonSerializer.Serialize(a.Objects), JsonSerializer.Serialize(b.Objects));
        Assert.Equal(JsonSerializer.Serialize(continuous.CaptureSaveStateForTests(25000, SimulationSpeed.Speed0).GameState.DefenseState), JsonSerializer.Serialize(resumed.CaptureSaveStateForTests(25000, SimulationSpeed.Speed0).GameState.DefenseState));
    }
    [Fact]
    public void Invalid_load_does_not_replace_running_world()
    {
        using var engine = Create(DefenseScenario());
        engine.ReceiveCommand(Fire("fire")); At(engine, 0); At(engine, 1000);
        var saved = engine.CaptureSaveStateForTests(1000, SimulationSpeed.Speed0);
        var defense = saved.GameState.DefenseState!;
        var bad = saved with
        {
            GameState = saved.GameState with
            {
                DefenseState = defense with
                { Projectiles = defense.Projectiles.Select(p => p with { Flight = p.Flight with { FrozenChanceTenths = 999 } }).ToArray() }
            }
        };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(bad, isSave: true));
        Assert.Equal(ScenarioLoader.Serialize(saved), ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(1000, SimulationSpeed.Speed0)));
    }
}
