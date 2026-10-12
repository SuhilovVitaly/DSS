using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
using static DeepSpaceSaga.Engine.Tests.ManualCountermeasureFireTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PdBurstTests
{
    private static GameDataRegistry ProductionRegistry() => EngineContentLoader.LoadRegistryFromSettingsFile(
        Path.Combine(ClientRoot, "Settings.json"), out _, out _);

    [Fact]
    public void Second_barrel_fires_after_two_seconds_and_survives_save_load()
    {
        var registry = ProductionRegistry();
        using var engine = Create(HostileTorpedoFireTests.HostileScenario(), registry);
        string torpedoId = LaunchEnemy(engine);
        engine.ReceiveCommand(Manual("burst", torpedoId));
        var first = At(engine, 1);
        var defense = first.InstalledModules.Single(m => m.ModuleId == Defense1).Defense!;
        Assert.Equal(2, defense.Barrels);
        Assert.Equal(2001, Assert.Single(defense.PendingBurstShots!.Value).DueMotionTimeMs);
        Assert.Single(first.Objects, o => o.Countermeasure?.LauncherModuleId == Defense1);

        var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(
            engine.CaptureSaveStateForTests(1000, SimulationSpeed.Speed0, 1000)), true);
        using var restored = new SimulationEngine(registry);
        restored.LoadScenario(save, isSave: true);
        var before = At(engine, 2000);
        Assert.Single(before.Objects, o => o.Countermeasure?.LauncherModuleId == Defense1);
        var second = At(engine, 2001);
        var resumed = At(restored, 2001);
        Assert.Equal(2, second.Objects.Count(o => o.Countermeasure?.LauncherModuleId == Defense1));
        Assert.Equal(second.CombatJournal.Select(e => (e.Type, e.ProjectileObjectId, e.MotionTimeMs)),
            resumed.CombatJournal.Select(e => (e.Type, e.ProjectileObjectId, e.MotionTimeMs)));
        Assert.Empty(second.InstalledModules.Single(m => m.ModuleId == Defense1).Defense!.PendingBurstShots!.Value);
    }

    [Fact]
    public void Single_barrel_has_no_second_shot()
    {
        using var engine = Create(HostileTorpedoFireTests.HostileScenario());
        string torpedoId = LaunchEnemy(engine);
        engine.ReceiveCommand(Manual("single", torpedoId));
        var first = At(engine, 1);
        Assert.Equal(1, first.InstalledModules.Single(m => m.ModuleId == Defense1).Defense!.Barrels);
        Assert.Empty(first.InstalledModules.Single(m => m.ModuleId == Defense1).Defense!.PendingBurstShots!.Value);
        var later = At(engine, 2001);
        Assert.Single(later.CombatJournal, e => e.Type == CombatEventType.Launch &&
            e.Result == "countermeasure" && e.LauncherModuleId == Defense1);
    }

    [Fact]
    public void Second_barrel_is_skipped_after_target_is_intercepted()
    {
        var scenario = HostileTorpedoFireTests.HostileScenario(100);
        scenario = scenario with { GameState = scenario.GameState with { MasterSeed = 0 } };
        var registry = ProductionRegistry();
        using var engine = Create(scenario, registry);
        string torpedoId = LaunchEnemy(engine);
        engine.ReceiveCommand(Manual("short-burst", torpedoId));
        At(engine, 1);
        var intercepted = At(engine, 1500);
        Assert.DoesNotContain(intercepted.Objects, o => o.ObjectId == torpedoId);
        var saved = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(
            engine.CaptureSaveStateForTests(1500, SimulationSpeed.Speed0, 1500)), true);
        using var restored = new SimulationEngine(registry);
        restored.LoadScenario(saved, isSave: true);
        var skipped = At(engine, 2001);
        var resumed = At(restored, 2001);
        Assert.Single(skipped.CombatJournal, e => e.Type == CombatEventType.BurstShotSkipped);
        Assert.Equal(skipped.CombatJournal.Select(e => (e.Type, e.ProjectileObjectId)),
            resumed.CombatJournal.Select(e => (e.Type, e.ProjectileObjectId)));
        Assert.Single(skipped.CombatJournal, e => e.Type == CombatEventType.Launch &&
            e.Result == "countermeasure" && e.LauncherModuleId == Defense1);
        ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(
            engine.CaptureSaveStateForTests(2001, SimulationSpeed.Speed0, 2001)), true);
    }
}
