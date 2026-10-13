using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class SetFireModeTests
{
    private static PlayerCommand Set(string id, string mode, string module = Launcher) =>
        new(id, 1, Player, module, TorpedoLauncherCommandTypes.SetMode, ModeId: mode);

    [Fact]
    public void Outside_combat_selection_is_immediate_even_while_paused()
    {
        var scenario = TorpedoImpactTests.Scenario(o => o.ObjectId == Target ? o with
        { PositionY = -5000, RelationToPlayer = PlayerRelation.Neutral } : o);
        using var engine = Create(scenario);
        engine.ReceiveCommand(Set("peaceful-mode", "maneuverability"));
        var snapshot = At(engine, 0);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(snapshot.CommandResults).Status);
        var launcher = snapshot.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!;
        Assert.Equal("maneuverability", launcher.ModeId);
        Assert.Null(launcher.ReadyAtMotionTimeMs);
        Assert.Equal(0, snapshot.MotionTimeMs);
    }

    [Fact]
    public void Combat_selection_costs_one_reload_cycle_and_duplicate_is_durable()
    {
        var scenario = TorpedoImpactTests.Scenario(o => o.ObjectId == Target ? o with
        { RelationToPlayer = PlayerRelation.Enemy } : o);
        using var engine = Create(scenario);
        engine.ReceiveCommand(Set("combat-mode", "overheat"));
        var changed = At(engine, 0);
        Assert.Equal("overheat", changed.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ModeId);
        Assert.Equal(15000, changed.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ReadyAtMotionTimeMs);
        engine.ReceiveCommand(Set("combat-mode", "standard"));
        Assert.Equal(15000, At(engine, 0).InstalledModules.Single(m => m.ModuleId == Launcher)
            .LauncherCombat!.ReadyAtMotionTimeMs);
        var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(
            engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0)), true);
        using var restored = new SimulationEngine(Registry.Value);
        restored.LoadScenario(save, isSave: true);
        restored.ReceiveCommand(Set("combat-mode", "standard"));
        var duplicate = At(restored, 0);
        Assert.Equal("overheat", duplicate.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ModeId);
        Assert.Equal(15000, duplicate.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ReadyAtMotionTimeMs);
    }

    [Fact]
    public void In_flight_selection_extends_existing_ready_time_and_pd_rejects_mode()
    {
        using var engine = Create();
        engine.ReceiveCommand(Fire("flight"));
        At(engine, 0);
        engine.ReceiveCommand(Set("mid-flight", "maneuverability"));
        var changed = At(engine, 0);
        Assert.Equal(30000, changed.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ReadyAtMotionTimeMs);
        engine.ReceiveCommand(Set("defense-mode", "maneuverability", ManualCountermeasureFireTests.Defense1));
        Assert.Equal("mode_not_applicable", At(engine, 0).CommandResults
            .Single(r => r.CommandId == "defense-mode").ReasonCode);
    }
}
