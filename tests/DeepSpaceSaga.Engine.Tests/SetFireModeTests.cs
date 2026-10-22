using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0018TK0002Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class SetFireModeTests
{
    private static PlayerCommand Retired(string id, string mode = "overheat", string module = Launcher) =>
        new(id, 9, Player, module, "torpedo.setMode", ModeId: mode);

    [Fact]
    public void Retired_mode_selection_while_paused_changes_no_profile_readiness_or_rng()
    {
        using var engine = Create(); var before = engine.CaptureLaunchScatterState();
        engine.ReceiveCommand(Retired("paused-mode")); var state = At(engine);
        Assert.Equal(CommandReasonCodes.UnknownCommandType, Assert.Single(state.CommandResults).ReasonCode);
        Assert.Equal(before, engine.CaptureLaunchScatterState()); Assert.Equal(0, state.MotionTimeMs);
        var launcher = state.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!;
        Assert.Empty(launcher.ModeId); Assert.Null(launcher.ReadyAtMotionTimeMs); Assert.Empty(launcher.ActiveTorpedoIds);
    }

    [Fact]
    public void Rejected_mode_receipt_is_durable_and_payload_change_cannot_restart_it_after_save_load()
    {
        using var engine = Create(); engine.ReceiveCommand(Retired("mode-id"));
        var rejected = Assert.Single(At(engine).CommandResults); Assert.Equal(CommandResultStatus.Rejected, rejected.Status);
        var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveState()), true);
        using var restored = new SimulationEngine(Registry()); restored.LoadScenario(save, true);
        var before = restored.CaptureLaunchScatterState();
        restored.ReceiveCommand(Launch("mode-id", "fast"));
        var replayed = Assert.Single(At(restored).CommandResults, r => r.CommandId == "mode-id");
        Assert.Equal(rejected.Status, replayed.Status); Assert.Equal(rejected.ReasonCode, replayed.ReasonCode);
        Assert.Equal("torpedo.setMode", replayed.CommandType); Assert.Equal(before, restored.CaptureLaunchScatterState());
        Assert.DoesNotContain(At(restored).Objects, o => o.OwnProjectile is not null);
    }

    [Fact]
    public void Retired_mode_cannot_extend_active_reload_or_mutate_captured_flight_on_either_weapon_category()
    {
        using var engine = Create(); engine.ReceiveCommand(Launch(profile: "fast"));
        var state = At(engine); var flight = Assert.Single(state.Objects, o => o.OwnProjectile is not null).OwnProjectile!;
        var before = engine.CaptureLaunchScatterState();
        engine.ReceiveCommand(Retired("torpedo-mode", "maneuverability"));
        engine.ReceiveCommand(Retired("counter-mode", "standard", "MOD-PLAYER-COUNTERMEASURE-01"));
        var after = At(engine); Assert.All(after.CommandResults.Where(r => r.CommandId.EndsWith("-mode", StringComparison.Ordinal)), r =>
        { Assert.Equal(CommandResultStatus.Rejected, r.Status); Assert.Equal(CommandReasonCodes.UnknownCommandType, r.ReasonCode); });
        Assert.Equal(before, engine.CaptureLaunchScatterState());
        Assert.Equal(flight, Assert.Single(after.Objects, o => o.OwnProjectile is not null).OwnProjectile);
        Assert.Equal(10000, after.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ReadyAtMotionTimeMs);
        Assert.Null(after.InstalledModules.Single(m => m.ModuleId == "MOD-PLAYER-COUNTERMEASURE-01").Defense!.ReadyAtMotionTimeMs);
    }
}
