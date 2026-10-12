using DeepSpaceSaga.Contracts;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
using static DeepSpaceSaga.Engine.Tests.AutomaticDefenseTests;
namespace DeepSpaceSaga.Engine.Tests;

public class CountermeasureResolutionTests
{
    [Fact]
    public void Intercept_58_example_uses_direct_operator_skills()
    {
        var registry = DeepSpaceSaga.Engine.Content.EngineContentLoader.LoadRegistryFromSettingsFile(
            Path.Combine(ClientRoot, "Settings.json"), out _, out _);
        var scenario = DefenseScenario(600);
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o with
                {
                    Crew = o.Crew!.Select(c => c with { AccuracySkill = 20, ManeuverabilitySkill = 20 }).ToArray()
                }).ToArray()
            }
        };
        using var engine = Create(scenario, registry);
        engine.ReceiveCommand(Fire("example"));
        var launched = At(engine, 0);
        var torpedo = Assert.Single(launched.Objects, o => o.Torpedo is not null).Torpedo!;
        Assert.Equal(70m, torpedo.CapturedManeuverability);
        Assert.Equal(20, torpedo.RatingBreakdown!.Skill);
        var defense = Assert.Single(At(engine, 100).Objects, o => o.Countermeasure is not null).Countermeasure!;
        Assert.Equal(78m, defense.CapturedAccuracy);
        Assert.Equal(70m, defense.CapturedTargetManeuverability);
        Assert.Equal(580, defense.FrozenChanceTenths);
        var resolved = At(engine, 20000);
        var rolls = resolved.CombatJournal.Where(e => e.RatingBreakdown is not null && e.Roll is not null).ToArray();
        Assert.NotEmpty(rolls);
        Assert.All(rolls, roll => Assert.Equal(580, roll.ChanceTenths));
        Assert.Equal((ulong)rolls.Length, engine.CaptureSaveState().GameState.DefenseState!.RngCounter);
    }

    [Fact]
    public void Successful_intercept_removes_both_once_and_no_module_damage()
    {
        using var engine = new SimulationEngine(WeaponRegistry(100, 0));
        engine.LoadScenario(DefenseScenario(600, 0, 100));
        engine.ReceiveCommand(Fire("fire")); At(engine, 0);
        var snapshot = At(engine, 5000);
        Assert.DoesNotContain(snapshot.Objects, o => o.Torpedo is not null || o.Countermeasure is not null);
        var impact = Assert.Single(snapshot.CombatImpacts);
        Assert.Equal(TorpedoTerminationKind.Intercept, impact.TerminationKind);
        Assert.Equal(0, impact.DamageApplied);
        Assert.DoesNotContain(snapshot.CombatJournal, e => e.Type == CombatEventType.ModuleHit);
        var entry = Assert.Single(snapshot.CombatJournal.Where(e => e.Roll is not null));
        Assert.Equal(1000, entry.ChanceTenths);
        Assert.Equal(801, entry.Roll);
        Assert.Empty(snapshot.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ActiveTorpedoIds);
        Assert.Equal(entry.MotionTimeMs + 10000, snapshot.Objects.Single(o => o.ObjectId == Target).Defense!.ReloadDueMotionTimeMs);
        Assert.Single(At(engine, 20000).CombatJournal.Where(e => e.Roll is not null));
        Assert.Equal(DefenseState.Ready, At(engine, 20000).Objects.Single(o => o.ObjectId == Target).Defense!.State);
    }
    [Fact]
    public void Miss_coasts_straight_then_reload_starts_and_never_retries()
    {
        using var engine = Create(DefenseScenario());
        engine.ReceiveCommand(Fire("fire")); At(engine, 0);
        var snapshot = At(engine, 4000);
        var pr = Assert.Single(snapshot.Objects, o => o.Countermeasure is not null);
        var flight = pr.Countermeasure!;
        Assert.Equal(CountermeasurePhase.MissedCoast, flight.Phase);
        Assert.Equal(801, flight.ResolutionRoll);
        Assert.Equal(500, flight.FrozenChanceTenths);
        Assert.Single(snapshot.Objects, o => o.Torpedo is not null);
        Assert.Equal(DefenseState.Guiding, snapshot.Objects.Single(o => o.ObjectId == Target).Defense!.State);
        var coast = Assert.Single(At(engine, 5000).Objects, o => o.Countermeasure is not null);
        Assert.Equal(pr.Direction, coast.Direction);
        Assert.InRange(Math.Abs(Math.Sqrt(Math.Pow(pr.X - coast.X, 2) + Math.Pow(pr.Y - coast.Y, 2)) - 120), 0, 1e-6);
        snapshot = At(engine, 6000);
        Assert.DoesNotContain(snapshot.Objects, o => o.Countermeasure is not null);
        Assert.Equal(flight.MissExpiresAtMotionTimeMs + 10000, snapshot.Objects.Single(o => o.ObjectId == Target).Defense!.ReloadDueMotionTimeMs);
        snapshot = At(engine, 18000);
        Assert.DoesNotContain(snapshot.Objects, o => o.Countermeasure is not null);
        Assert.Single(snapshot.CombatJournal.Where(e => e.Roll is not null));
        Assert.True(Assert.Single(snapshot.Objects, o => o.Torpedo is not null).CountermeasureAttempted);
    }
    [Theory]
    [InlineData(1000, false)]
    [InlineData(4000, true)]
    public void Target_loss_before_contact_aborts_but_after_miss_preserves_coast(long time, bool missed)
    {
        using var engine = Create(DefenseScenario());
        engine.ReceiveCommand(Fire("fire")); At(engine, 0);
        var snapshot = At(engine, time);
        string torpedo = Assert.Single(snapshot.Objects, o => o.Torpedo is not null).ObjectId;
        engine.ReceiveCommand(new("cancel", 2, Player, Launcher, CombatCommandTypes.SelfDestruct, torpedo));
        snapshot = At(engine, time);
        Assert.Equal(missed ? 1 : 0, snapshot.Objects.Count(o => o.Countermeasure is not null));
        Assert.Equal(missed ? DefenseState.Guiding : DefenseState.Reloading, snapshot.Objects.Single(o => o.ObjectId == Target).Defense!.State);
        Assert.Equal(missed ? 1 : 0, snapshot.CombatJournal.Count(e => e.Roll is not null));
    }
    [Fact]
    public void Large_step_and_partitions_produce_same_resolution()
    {
        using var large = Create(DefenseScenario());
        using var small = Create(DefenseScenario());
        large.ReceiveCommand(Fire("fire")); small.ReceiveCommand(Fire("fire")); At(large, 0); At(small, 0);
        var a = At(large, 21000);
        for (int time = 17; time < 21000; time += 17) At(small, time);
        var b = At(small, 21000);
        Assert.Equal(a.CombatJournal.Length, b.CombatJournal.Length);
        Assert.Equal(a.CombatJournal.Single(e => e.RatingBreakdown is not null && e.Roll is not null).Roll,
            b.CombatJournal.Single(e => e.RatingBreakdown is not null && e.Roll is not null).Roll);
        Assert.InRange(Math.Abs(a.CombatJournal.Single(e => e.RatingBreakdown is not null && e.Roll is not null).MotionTimeMs -
            b.CombatJournal.Single(e => e.RatingBreakdown is not null && e.Roll is not null).MotionTimeMs), 0, 1e-5);
        Assert.Equal(a.Objects.Single(o => o.ObjectId == Target).HullCombat, b.Objects.Single(o => o.ObjectId == Target).HullCombat);
    }
}
