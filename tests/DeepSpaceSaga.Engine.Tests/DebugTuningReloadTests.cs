using System.Text.Json;
using DeepSpaceSaga.Contracts;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class DebugTuningReloadTests
{
    private static void Tune(SimulationEngine engine, long duration, string id = "reload") =>
        engine.ReceiveCommand(new(id, 2, "", "", DebugTuningCommandTypes.Apply,
            DebugTuning: new([new("module.torpedo.launcher.basic", "reloadMs", duration)])));
    private static LauncherCombatSnapshot LauncherState(AuthoritativeSnapshot state) =>
        state.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!;

    [Theory]
    [InlineData(3000, 5000)]
    [InlineData(6000, 6000)]
    public void Retiming_uses_original_reload_start_and_completed_reload_emits_one_cue(long time, long expected)
    {
        using var engine = Create(o => o with { PositionX = o.ObjectId == Player ? 0 : 10000, PositionY = 0 }, scatter: 0);
        engine.ReceiveCommand(Launch()); PointCombatUS0018TK0002Tests.At(engine);
        PointCombatUS0018TK0002Tests.At(engine, time);
        Tune(engine, 5000);
        var changed = PointCombatUS0018TK0002Tests.At(engine, time);
        Assert.Equal(expected, LauncherState(changed).ReadyAtMotionTimeMs);
        Assert.Equal(5000, LauncherState(changed).ReloadDurationMs);
        var after = PointCombatUS0018TK0002Tests.At(engine, expected + 100);
        Assert.Single(after.CombatJournal, e => e.Type == CombatEventType.TorpedoReloadCompleted);
        Tune(engine, 6000, "ready-stays-ready");
        var stillReady = PointCombatUS0018TK0002Tests.At(engine, expected + 100);
        Assert.Equal(expected, LauncherState(stillReady).ReadyAtMotionTimeMs);
        Assert.Single(stillReady.CombatJournal, e => e.Type == CombatEventType.TorpedoReloadCompleted);
    }

    [Fact]
    public void Initially_ready_launcher_stays_ready_and_next_launch_captures_new_duration()
    {
        using var engine = Create(scatter: 0);
        Tune(engine, 5000);
        Assert.Null(LauncherState(PointCombatUS0018TK0002Tests.At(engine)).ReadyAtMotionTimeMs);
        engine.ReceiveCommand(Launch());
        Assert.Equal(5000, LauncherState(PointCombatUS0018TK0002Tests.At(engine)).ReadyAtMotionTimeMs);
    }

    [Theory]
    [InlineData(6000, 6000)]
    [InlineData(1000, 3000)]
    public void Countermissile_reload_uses_the_same_elapsed_reload_formula(long duration, long expected)
    {
        using var engine = PointCombatUS0004TK0001Tests.CreateCounter(scatter: 0, torpedoScatter: 0);
        engine.ReceiveCommand(new("cm", 1, Player, PointCombatUS0004TK0001Tests.Defense, DefenseCommandTypes.Launch,
            TargetWorldX: 100, TargetWorldY: 0, ProfileId: "wide"));
        PointCombatUS0018TK0002Tests.At(engine);
        PointCombatUS0018TK0002Tests.At(engine, 3000);
        engine.ReceiveCommand(new("cm-reload", 2, "", "", DebugTuningCommandTypes.Apply,
            DebugTuning: new([new("module.countermeasure.launcher.basic", "reloadMs", duration)])));
        var state = PointCombatUS0018TK0002Tests.At(engine, 3000);
        var defense = state.InstalledModules.Single(m => m.ModuleId == PointCombatUS0004TK0001Tests.Defense).Defense!;
        Assert.Equal(expected, defense.ReadyAtMotionTimeMs);
        Assert.Equal(duration, defense.ReloadDurationMs);
        Assert.Equal(expected > 3000 ? DefenseState.Reloading : DefenseState.Ready, defense.State);
        Assert.Equal(expected > 3000 ? expected : (double?)null, defense.ReloadDueMotionTimeMs);
    }

    [Fact]
    public void Tuning_commands_flights_and_reload_events_are_partition_invariant()
    {
        static string Run(bool split)
        {
            using var engine = Create(o => o with { PositionX = o.ObjectId == Player ? 0 : 10000, PositionY = 0 }, scatter: 0);
            engine.ReceiveCommand(Launch(x: 600, profile: "slow")); PointCombatUS0018TK0002Tests.At(engine);
            if (split) for (long t = 100; t < 3000; t += 100) PointCombatUS0018TK0002Tests.At(engine, t);
            PointCombatUS0018TK0002Tests.At(engine, 3000);
            Tune(engine, 5000);
            engine.ReceiveCommand(new("faster", 3, "", "", DebugTuningCommandTypes.Apply,
                DebugTuning: new([new("module.torpedo.launcher.basic", "speedKmS", 12, "slow")])));
            PointCombatUS0018TK0002Tests.At(engine, 3000);
            if (split) for (long t = 3100; t < 10000; t += 100) PointCombatUS0018TK0002Tests.At(engine, t);
            var final = PointCombatUS0018TK0002Tests.At(engine, 10000);
            return JsonSerializer.Serialize(new
            {
                final.Objects,
                final.InstalledModules,
                final.CombatJournal,
                final.DebugTuning,
                Rng = engine.CaptureLaunchScatterState()
            });
        }
        Assert.Equal(Run(false), Run(true));
    }
}
