using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0018TK0002Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0004TK0003Tests
{
    private const string Counter = "MOD-PLAYER-COUNTERMEASURE-01", Counter2 = "MOD-PLAYER-COUNTERMEASURE-02";
    private static PlayerCommand Shot(string id, string module = Counter, string profile = "wide", double y = -100) =>
        new(id, 1, Player, module, DefenseCommandTypes.Launch, TargetWorldX: 150, TargetWorldY: y, ProfileId: profile);
    private static string Ships(SimulationEngine engine, long time = 0) => JsonSerializer.Serialize(engine.CaptureSaveStateForTests(time * 300, SimulationSpeed.Speed0, time).GameState.SpaceObjects
        .Where(o => o.ObjectType is SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip).Select(o => new { o.ObjectId, o.Modules, o.Crew }));

    [Theory]
    [InlineData("defense.fire")]
    [InlineData("defense.enable")]
    [InlineData("defense.disable")]
    public void Legacy_automatic_defense_commands_are_rejected_without_clock_reload_identity_or_rng_effect(string command)
    {
        using var engine = Create(); var before = engine.CaptureLaunchScatterState(); var saved = engine.CaptureSaveState();
        engine.ReceiveCommand(new("retired", 1, Player, Counter, command, Enemy)); var state = At(engine);
        var result = Assert.Single(state.CommandResults); Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(CommandReasonCodes.UnknownCommandType, result.ReasonCode); Assert.Equal(0, state.MotionTimeMs);
        Assert.Equal(before, engine.CaptureLaunchScatterState());
        var after = engine.CaptureSaveState(); Assert.Equal(saved.GameState.DefenseState!.ProjectileSequence, after.GameState.DefenseState!.ProjectileSequence);
        Assert.Equal(saved.GameState.DefenseState.RngCounter, after.GameState.DefenseState.RngCounter);
        Assert.All(state.InstalledModules.Where(m => m.Defense is not null), m =>
        { Assert.False(m.Defense!.AutoEnabled); Assert.Null(m.Defense.ReadyAtMotionTimeMs); Assert.Equal(DefenseState.Ready, m.Defense.State); });
    }

    [Theory]
    [InlineData("wide", 15d, 1d)]
    [InlineData("fast", 25d, .5d)]
    public void Two_manual_area_launchers_never_guide_burst_or_draw_intercept_rolls_and_keep_launch_based_reload(string profile, double speed, double radius)
    {
        using var engine = Create(); string ships = Ships(engine); var saved = engine.CaptureSaveState();
        engine.ReceiveCommand(Shot("one", profile: profile)); At(engine);
        engine.ReceiveCommand(Shot("two", Counter2, profile, 100)); var state = At(engine);
        Assert.Equal(2, state.Objects.Count(o => o.OwnProjectile?.Kind == ProjectileKind.Countermissile));
        Assert.All(state.Objects.Where(o => o.OwnProjectile is not null), o =>
        { Assert.Equal(profile, o.OwnProjectile!.ProfileId); Assert.Equal(speed, o.OwnProjectile.SpeedKmS); Assert.Equal(radius, o.OwnProjectile.BlastRadiusKm); Assert.Null(o.Countermeasure); });
        Assert.All(state.InstalledModules.Where(m => m.Defense is not null), m =>
        { Assert.False(m.Defense!.AutoEnabled); Assert.Equal(DefenseState.Reloading, m.Defense.State); Assert.True(m.Defense.PendingBurstShots!.Value.IsEmpty); Assert.Equal(12500, m.Defense.ReadyAtMotionTimeMs); });
        var ended = At(engine, 3000); Assert.DoesNotContain(ended.Objects, o => o.OwnProjectile is not null);
        Assert.Equal(2, ended.CombatJournal.Count(e => e.Type == CombatEventType.Launch && e.ProjectileKind == ProjectileKind.Countermissile));
        Assert.All(ended.InstalledModules.Where(m => m.Defense is not null), m => Assert.Equal(DefenseState.Reloading, m.Defense!.State));
        Assert.Equal(ships, Ships(engine, 3000)); Assert.Equal(4UL, engine.CaptureLaunchScatterState().Counter);
        Assert.Equal(saved.GameState.DefenseState!.RngCounter, engine.CaptureSaveStateForTests(900000, SimulationSpeed.Speed0, 3000).GameState.DefenseState!.RngCounter);
        Assert.DoesNotContain(ended.CombatJournal, e => e.Roll is not null || e.ChanceTenths is not null);
        var ready = At(engine, 12500); Assert.All(ready.InstalledModules.Where(m => m.Defense is not null), m => Assert.Equal(DefenseState.Ready, m.Defense!.State));
        Assert.Equal(2, ready.CombatJournal.Count(e => e.Type == CombatEventType.Launch));
    }

    [Fact]
    public void Incoming_real_point_torpedo_never_triggers_an_automatic_counter_launch()
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/DeepSpaceSaga.Client"));
        var source = ScenarioLoader.LoadFromFile(Path.Combine(root, "Scenarios/PlayerShipOnly/scenario.json"));
        source = source with
        {
            GameState = source.GameState with
            {
                CurrentSpeed = "Speed0",
                MasterSeed = 42,
                SpaceObjects = source.GameState.SpaceObjects.Select(o => o with
                {
                    PositionX = o.ObjectId == Player ? 0 : 600,
                    PositionY = 0,
                    SpeedMps = 0,
                    MovementType = "Stationary",
                    Ai = null
                }).ToArray()
            }
        };
        using var engine = new SimulationEngine(Registry(), clock: new SimulationClock(SimulationSpeed.Speed0, () => 0));
        engine.LoadScenario(source);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Enemy, "MOD-PIRATE-TORPEDO-01", 0, 0, "slow", 0).Disposition);
        var before = engine.CaptureLaunchScatterState(); var state = At(engine, 1000);
        Assert.Contains(state.Objects, o => o.ObservedProjectile?.Kind == ProjectileKind.Torpedo);
        Assert.DoesNotContain(state.Objects, o => o.OwnProjectile?.Kind == ProjectileKind.Countermissile);
        Assert.DoesNotContain(state.CombatJournal, e => e.Type == CombatEventType.Launch && e.ProjectileKind == ProjectileKind.Countermissile);
        Assert.All(state.InstalledModules.Where(m => m.Defense is not null), m => Assert.False(m.Defense!.AutoEnabled));
        Assert.Equal(before, engine.CaptureLaunchScatterState());
    }

    [Fact]
    public void Point_counter_continuation_is_partition_invariant_and_save_load_consumes_no_new_scatter()
    {
        using var whole = Create(); using var split = Create();
        foreach (var engine in new[] { whole, split }) { engine.ReceiveCommand(Shot("one")); At(engine); engine.ReceiveCommand(Shot("two", Counter2, "fast", 100)); At(engine); }
        At(split, 211); var save = split.CaptureSaveStateForTests(63300, SimulationSpeed.Speed0, 211); var before = split.CaptureLaunchScatterState();
        using var loaded = new SimulationEngine(Registry()); loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true), true);
        Assert.Equal(before, loaded.CaptureLaunchScatterState());
        foreach (long time in new long[] { 499, 1000, 1500, 3000, 12500 }) At(loaded, time);
        var a = At(whole, 12500); var b = At(loaded, 12500);
        Assert.Equal(JsonSerializer.Serialize(a.CombatJournal), JsonSerializer.Serialize(b.CombatJournal));
        Assert.Equal(JsonSerializer.Serialize(a.InstalledModules), JsonSerializer.Serialize(b.InstalledModules));
        Assert.Equal(whole.CaptureLaunchScatterState(), loaded.CaptureLaunchScatterState());
    }

    [Fact]
    public void Engine_source_contains_no_retired_counter_scheduler_roll_or_guidance_helpers()
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/DeepSpaceSaga.Engine"));
        string source = string.Join("\n", Directory.GetFiles(root, "SimulationEngine*.cs").Select(File.ReadAllText));
        foreach (string retired in new[] { "NextDefenseLaunch(", "NextPendingBurst(", "ProcessPendingBurst(", "FindFirstIntercept(",
            "ResolveFirstIntercept(", "TryManualDefenseFire(", "ValidateManualDefense(", "CountermeasureGuidanceMath.", "_countermeasureRng.NextRoll(", "_countermeasureTargetPlans" })
            Assert.DoesNotContain(retired, source);
        Assert.False(File.Exists(Path.Combine(root, "SimulationEngine.ManualDefense.cs")));
        Assert.False(File.Exists(Path.Combine(root, "SimulationEngine.DefensePreview.cs")));
    }
}
