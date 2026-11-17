using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ModuleHealthBootstrapTests
{
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        TestPaths.Repository, "src", "DeepSpaceSaga.Client"));

    [Fact]
    public void Configured_structure_is_projected()
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
                        ? module with { StructurePoints = 150 } : module).ToArray()
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
    public void Default_modules_have_structure()
    {
        using var engine = EngineContentLoader.CreateEngineFromSettingsFile(Path.Combine(ClientRoot, "Settings.json"));
        var modules = engine.CaptureSnapshot().InstalledModules;
        // Default New Game is the demo: the shared starter's 9 modules plus the passenger quarters (EP-0034-US-0001).
        Assert.Equal(10, modules.Length);
        // The starter manifest (EP-0029 US-0011 TK-0002) starts every module intact at its type max:
        // medium drone bays 250 SP, every other starter module 150 SP.
        Assert.All(modules, module =>
        {
            Assert.Equal(module.ModuleTypeId == "module.drone-bay.medium" ? 250 : 150, module.StructureMax);
            Assert.Equal(module.StructureMax, module.StructureCurrent);
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
                            StructurePoints = 90,
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
