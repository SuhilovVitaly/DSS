using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
using static DeepSpaceSaga.Engine.Tests.ManualCountermeasureFireTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PdReloadTests
{
    [Fact]
    public void Reload_is_due_14500_ms_after_first_barrel_not_after_intercept()
    {
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(
            Path.Combine(ClientRoot, "Settings.json"), out _, out _);
        var scenario = HostileTorpedoFireTests.HostileScenario();
        scenario = scenario with { GameState = scenario.GameState with { MasterSeed = 0 } };
        using var engine = Create(scenario, registry);
        string target = LaunchEnemy(engine);
        engine.ReceiveCommand(Manual("reload", target));
        var launched = At(engine, 1);
        var defense = launched.InstalledModules.Single(m => m.ModuleId == Defense1).Defense!;
        Assert.Equal(2, defense.Barrels);
        Assert.Equal(14501, defense.ReadyAtMotionTimeMs);
        Assert.Null(defense.ReloadDueMotionTimeMs);
        var before = At(engine, 14500);
        Assert.Equal(DefenseState.Reloading, before.InstalledModules.Single(m => m.ModuleId == Defense1).Defense!.State);
        Assert.Equal(14501, before.InstalledModules.Single(m => m.ModuleId == Defense1).Defense!.ReloadDueMotionTimeMs);
        var ready = At(engine, 14501);
        Assert.Equal(DefenseState.Ready, ready.InstalledModules.Single(m => m.ModuleId == Defense1).Defense!.State);
        Assert.Null(ready.InstalledModules.Single(m => m.ModuleId == Defense1).Defense!.ReloadDueMotionTimeMs);
    }
}
