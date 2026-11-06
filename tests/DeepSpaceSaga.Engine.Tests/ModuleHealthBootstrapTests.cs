using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ModuleHealthBootstrapTests
{
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    [Fact]
    public void Legacy_armor_does_not_change_world_structure()
    {
        var loaded = EngineContentLoader.LoadFromSettingsFile(Path.Combine(ClientRoot, "Settings.json"));
        string playerId = loaded.DefaultScenario.GameState.PlayerShipObjectId;
        var scenario = loaded.DefaultScenario with
        {
            GameState = loaded.DefaultScenario.GameState with
            {
                SpaceObjects = loaded.DefaultScenario.GameState.SpaceObjects.Select(obj => obj.ObjectId == playerId
                    ? obj with
                    {
                        Modules = obj.Modules!.Select((module, index) => index == 0
                        ? module with { ArmorClass = "light", StructurePoints = 150 } : module).ToArray()
                    }
                    : obj).ToArray()
            }
        };
        using var engine = new SimulationEngine(loaded.Registry);
        engine.LoadScenario(scenario);
        var module = engine.CaptureSnapshot().InstalledModules[0];
        Assert.Equal(150, module.StructureCurrent);
        Assert.Equal(150, module.StructureMax);
        Assert.False(module.IsDestroyed);
    }

    [Fact]
    public void Legacy_modules_no_armor()
    {
        using var engine = EngineContentLoader.CreateEngineFromSettingsFile(Path.Combine(ClientRoot, "Settings.json"));
        Assert.All(engine.CaptureSnapshot().InstalledModules, module =>
        {
            Assert.Equal(150, module.StructureMax);
            Assert.False(module.IsDestroyed);
        });
    }

    [Fact]
    public void Module_health_state_survives_save_load()
    {
        var loaded = EngineContentLoader.LoadFromSettingsFile(Path.Combine(ClientRoot, "Settings.json"));
        string playerId = loaded.DefaultScenario.GameState.PlayerShipObjectId;
        var scenario = loaded.DefaultScenario with
        {
            GameState = loaded.DefaultScenario.GameState with
            {
                SpaceObjects = loaded.DefaultScenario.GameState.SpaceObjects.Select(obj => obj.ObjectId == playerId
                    ? obj with
                    {
                        Modules = obj.Modules!.Select((module, index) => index == 0
                        ? module with
                        {
                            ArmorClass = "light",
                            Armor = 17,
                            StructurePoints = 90,
                            DisabledUntilMotionTimeMs = 1200
                        } : module).ToArray()
                    }
                    : obj).ToArray()
            }
        };
        using var engine = new SimulationEngine(loaded.Registry);
        engine.LoadScenario(scenario);
        var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveState()),
            allowNonZeroGameTime: true, isSave: true);
        using var restored = new SimulationEngine(loaded.Registry);
        restored.LoadScenario(save, isSave: true);
        var module = restored.CaptureSnapshot().InstalledModules[0];
        Assert.Equal(90, module.StructureCurrent);
    }
}
