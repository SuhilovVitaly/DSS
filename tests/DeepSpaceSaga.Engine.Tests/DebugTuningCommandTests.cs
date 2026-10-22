using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class DebugTuningCommandTests
{
    private static PlayerCommand Apply(string id, params DebugTuningValue[] values) =>
        new(id, 1, "", "", DebugTuningCommandTypes.Apply, DebugTuning: new([.. values]));

    [Fact]
    public void Paused_command_is_atomic_idempotent_and_publishes_the_confirmed_registry()
    {
        using var engine = PointCombatUS0018TK0002Tests.Create();
        var before = engine.CaptureLaunchScatterState();
        var command = Apply("tune", new DebugTuningValue("module.torpedo.launcher.basic", "speedKmS", 20, "medium"));
        engine.ReceiveCommand(command);
        var state = PointCombatUS0018TK0002Tests.At(engine);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(state.CommandResults).Status);
        Assert.Equal(SimulationSpeed.Speed0, state.CurrentSpeed);
        Assert.Equal(1, state.DebugTuning!.Revision);
        Assert.Equal(20, state.InstalledModules.Single(m => m.ModuleId == PointCombatUS0018TK0002Tests.Launcher)
            .LauncherCombat!.Profiles!.Value.Single(p => p.ProfileId == "medium").SpeedKmS);
        engine.ReceiveCommand(command);
        Assert.Equal(1, PointCombatUS0018TK0002Tests.At(engine).DebugTuning!.Revision);
        Assert.Equal(before, engine.CaptureLaunchScatterState());
        Assert.Equal(0, state.MotionTimeMs);
    }

    [Theory]
    [InlineData("unknown", "maxSpeedKmS", 1, "debug_unknown_parameter")]
    [InlineData("module.engine.tetrarch", "maxSpeedKmS", -1, "debug_value_out_of_range")]
    public void Rejection_leaves_both_fields_and_revision_unchanged(string type, string field, double value, string reason)
    {
        using var engine = PointCombatUS0018TK0002Tests.Create();
        engine.ReceiveCommand(Apply("invalid", new DebugTuningValue("module.engine.strizh", "maxSpeedKmS", 12), new DebugTuningValue(type, field, value)));
        var state = PointCombatUS0018TK0002Tests.At(engine);
        Assert.Equal(reason, Assert.Single(state.CommandResults).ReasonCode);
        Assert.Equal(0, state.DebugTuning!.Revision);
        Assert.Equal(state.DebugTuning.Defaults.ToArray(), state.DebugTuning.Values.ToArray());
    }

    [Fact]
    public void Session_must_be_loaded_and_compact_updates_send_only_changed_revisions()
    {
        using (var empty = new SimulationEngine())
        {
            empty.ReceiveCommand(Apply("no-session", new DebugTuningValue("module.engine.tetrarch", "maxSpeedKmS", 2)));
            Assert.Equal(DebugTuningReasonCodes.SessionNotRunning,
                Assert.Single(empty.CaptureSnapshotForTests().CommandResults).ReasonCode);
        }
        using var engine = PointCombatUS0018TK0002Tests.Create();
        engine.EnablePresentationLease();
        Assert.NotNull(engine.CapturePresentationUpdate(advanceClock: false).FullSnapshot!.DebugTuning);
        var unchanged = engine.CapturePresentationUpdate(advanceClock: false);
        Assert.All(unchanged.Frames, frame => Assert.Null(frame.State.DebugTuning));
        engine.ReceiveCommand(Apply("changed", new DebugTuningValue("module.engine.tetrarch", "maxSpeedKmS", 2)));
        var changed = engine.CapturePresentationUpdate(advanceClock: false);
        Assert.Equal(1, Assert.Single(changed.Frames, frame => frame.State.DebugTuning is not null).State.DebugTuning!.Revision);
        Assert.All(engine.CapturePresentationUpdate(advanceClock: false).Frames, frame => Assert.Null(frame.State.DebugTuning));
    }

    [Fact]
    public void Dialogue_does_not_block_debug_category()
    {
        using var engine = DockCommandTests.CreateEngine();
        engine.ReceiveCommand(new("dock-dialogue", 1, "SPC-0001", "MOD-NAV-01", NavigationComputerCommandTypes.Dock,
            TargetObjectId: "STATION-01"));
        Assert.NotNull(engine.CaptureSnapshotForTests().ActiveDialogue);
        engine.ReceiveCommand(Apply("debug-in-dialogue"));
        var state = engine.CaptureSnapshotForTests();
        Assert.NotNull(state.ActiveDialogue);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(state.CommandResults).Status);
        Assert.Equal(1, state.DebugTuning!.Revision);
    }
}
