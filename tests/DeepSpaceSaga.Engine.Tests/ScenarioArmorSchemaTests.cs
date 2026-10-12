using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ScenarioArmorSchemaTests
{
    [Fact]
    public void Unknown_armor_class_rejected()
    {
        var scenario = TorpedoImpactTests.Scenario(o => o.ObjectId != Player ? o : o with
        {
            Modules = o.Modules!.Select(m => m.ModuleId == Launcher ? m with { ArmorClass = "unknown" } : m).ToArray()
        });
        using var engine = Create();
        string before = ScenarioLoader.Serialize(engine.CaptureSaveState());
        var error = Assert.Throws<ScenarioException>(() => engine.LoadScenario(scenario));
        Assert.Contains("armorClass", error.Message, StringComparison.Ordinal);
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }

    [Fact]
    public void Legacy_scenario_defaults_and_configured_armor_roundtrip()
    {
        using var legacy = Create();
        var legacyModule = legacy.CaptureSaveState().GameState.SpaceObjects.Single(o => o.ObjectId == Player)
            .Modules!.Single(m => m.ModuleId == Launcher);
        Assert.Equal("none", legacyModule.ArmorClass);

        var scenario = TorpedoImpactTests.Scenario(o => o.ObjectId != Player ? o : o with
        {
            Modules = o.Modules!.Select(m => m.ModuleId == Launcher ? m with { ArmorClass = "light" } : m).ToArray()
        });
        using var engine = Create(scenario);
        using var restored = Create(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveState()), true));
        var module = restored.CaptureSaveState().GameState.SpaceObjects.Single(o => o.ObjectId == Player)
            .Modules!.Single(m => m.ModuleId == Launcher);
        Assert.Equal("light", module.ArmorClass);
    }
}
