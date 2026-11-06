using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ScenarioArmorSchemaTests
{

    [Fact]
    public void Save20_keeps_world_module_structure_and_drops_legacy_armor()
    {
        using var legacy = Create();
        var legacyModule = legacy.CaptureSaveState().GameState.SpaceObjects.Single(o => o.ObjectId == Player)
            .Modules!.Single(m => m.ModuleId == "MOD-PLAYER-ENGINE-01");
        Assert.Equal("none", legacyModule.ArmorClass);

        var scenario = TorpedoImpactTests.Scenario(o => o.ObjectId != Player ? o : o with
        {
            Modules = o.Modules!.Select(m => m.ModuleId == "MOD-PLAYER-ENGINE-01" ? m with { ArmorClass = "light", StructurePoints = 123 } : m).ToArray()
        });
        using var engine = Create(scenario);
        using var restored = Create(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveState()), true));
        var module = restored.CaptureSaveState().GameState.SpaceObjects.Single(o => o.ObjectId == Player)
            .Modules!.Single(m => m.ModuleId == "MOD-PLAYER-ENGINE-01");
        Assert.Equal("none", module.ArmorClass);
        Assert.Equal(123, module.StructurePoints);
    }
}
