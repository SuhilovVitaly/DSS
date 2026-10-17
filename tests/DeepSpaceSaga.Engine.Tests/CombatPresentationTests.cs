using System.Diagnostics;
using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatPresentationTests
{
    private static PlayerCommand Fire(string id = "presentation-fire") =>
        new(id, 1, Player, Launcher, CombatCommandTypes.Fire, Target);

    [Fact]
    public void Flight_between_full_snapshots_retains_launch_contact_geometry_and_consequences()
    {
        long real = 0;
        using var engine = new SimulationEngine(Registry.Value, clock: new SimulationClock(SimulationSpeed.Speed4, () => real));
        var scenario = TorpedoImpactTests.Scenario();
        engine.LoadScenario(scenario with { GameState = scenario.GameState with { CurrentSpeed = "Speed4" } });
        engine.EnablePresentationLease();
        var bootstrap = engine.CapturePresentationUpdate(true, false);
        engine.ReceiveCommand(Fire());
        var launch = engine.CapturePresentationUpdate();
        Assert.Contains(launch.Frames.SelectMany(f => f.State.Objects), o => o.Torpedo is not null);
        engine.ReportPresentationProgress(launch.Sequence, false);
        real = 200; // Five physical seconds; the 10 km flight ends inside this update.
        var contact = engine.CapturePresentationUpdate();
        Assert.Null(contact.FullSnapshot);
        Assert.DoesNotContain(bootstrap.FullSnapshot!.Objects, o => o.Torpedo is not null);
        Assert.DoesNotContain(contact.Frames[^1].State.Objects, o => o.Torpedo is not null);
        var impact = Assert.Single(contact.Frames.SelectMany(f => f.State.CombatImpacts));
        var frame = contact.Frames.First(f => f.State.CombatImpacts.Any(i => i.EventId == impact.EventId));
        Assert.Equal(impact.MotionTimeMs, frame.MotionTimeMs);
        Assert.InRange(Math.Abs(frame.State.GameTimeMs - impact.MotionTimeMs * 300), 0, 1);
        Assert.Contains(frame.State.CombatJournal, e => e.Type == CombatEventType.Hit);
        Assert.NotEmpty(frame.State.CombatReports!.Reports);
        Assert.DoesNotContain(frame.State.Objects, o => o.ObjectId == impact.TorpedoObjectId);
        Assert.Contains(impact.TorpedoObjectId, frame.RemovedObjectIds);
        Assert.Equal(bootstrap.SessionId, contact.SessionId);
    }

    [Theory]
    [InlineData(SimulationSpeed.Speed1)]
    [InlineData(SimulationSpeed.Speed2)]
    [InlineData(SimulationSpeed.Speed3)]
    [InlineData(SimulationSpeed.Speed4)]
    public void Publication_partition_preserves_combat_rng_and_event_order(SimulationSpeed speed)
    {
        long wholeTime = 0, splitTime = 0;
        using var whole = new SimulationEngine(Registry.Value, clock: new SimulationClock(speed, () => wholeTime));
        using var split = new SimulationEngine(Registry.Value, clock: new SimulationClock(speed, () => splitTime));
        var scenario = TorpedoImpactTests.Scenario();
        scenario = scenario with { GameState = scenario.GameState with { CurrentSpeed = speed.ToString() } };
        foreach (var engine in new[] { whole, split })
        {
            engine.LoadScenario(scenario);
            engine.EnablePresentationLease();
            engine.CapturePresentationUpdate(true, false);
            engine.ReceiveCommand(Fire());
            engine.CapturePresentationUpdate();
        }
        wholeTime = 20000;
        whole.CapturePresentationUpdate();
        for (splitTime = 100; splitTime <= 20000; splitTime += 100) split.CapturePresentationUpdate();
        var expected = whole.CaptureSnapshot();
        var actual = split.CaptureSnapshot();
        Assert.Equal(expected.CombatJournal.Length, actual.CombatJournal.Length);
        for (int i = 0; i < expected.CombatJournal.Length; i++)
        {
            Assert.InRange(Math.Abs(expected.CombatJournal[i].MotionTimeMs - actual.CombatJournal[i].MotionTimeMs), 0, 1e-5);
            Assert.InRange(Math.Abs(expected.CombatJournal[i].X - actual.CombatJournal[i].X), 0, 1e-5);
            Assert.InRange(Math.Abs(expected.CombatJournal[i].Y - actual.CombatJournal[i].Y), 0, 1e-5);
            Assert.Equal(expected.CombatJournal[i] with { MotionTimeMs = 0, X = 0, Y = 0 }, actual.CombatJournal[i] with { MotionTimeMs = 0, X = 0, Y = 0 });
        }
        Assert.Equal(expected.CombatImpacts.Length, actual.CombatImpacts.Length);
        for (int i = 0; i < expected.CombatImpacts.Length; i++)
        {
            Assert.InRange(Math.Abs(expected.CombatImpacts[i].MotionTimeMs - actual.CombatImpacts[i].MotionTimeMs), 0, 1e-5);
            Assert.Equal(expected.CombatImpacts[i].X, actual.CombatImpacts[i].X, 5);
            Assert.Equal(expected.CombatImpacts[i].Y, actual.CombatImpacts[i].Y, 5);
            Assert.Equal(expected.CombatImpacts[i].DamageApplied, actual.CombatImpacts[i].DamageApplied);
            Assert.Equal(expected.CombatImpacts[i].TerminationKind, actual.CombatImpacts[i].TerminationKind);
        }
        Assert.Equal(JsonSerializer.Serialize(whole.CaptureSaveState().GameState.CombatState),
            JsonSerializer.Serialize(split.CaptureSaveState().GameState.CombatState));
        Assert.Equal(speed, whole.CurrentSpeed);
        Assert.Equal(speed, split.CurrentSpeed);
    }

    [Fact]
    public void Lease_uses_monotonic_time_pauses_without_feedback_and_requires_explicit_resume()
    {
        long now = 0, real = 0;
        using var engine = new SimulationEngine(Registry.Value, clock: new SimulationClock(SimulationSpeed.Speed4, () => real));
        var scenario = TorpedoImpactTests.Scenario();
        engine.LoadScenario(scenario with { GameState = scenario.GameState with { CurrentSpeed = "Speed4" } });
        engine.SetPresentationTimestampForTests(() => now);
        engine.EnablePresentationLease();
        var initial = engine.CapturePresentationUpdate(true, false);
        now = Stopwatch.Frequency;
        Assert.False(engine.CapturePresentationUpdate().AutomaticPause);
        now++;
        var paused = engine.CapturePresentationUpdate();
        Assert.True(paused.AutomaticPause);
        Assert.Equal(SimulationSpeed.Speed0, engine.CurrentSpeed);
        engine.ReportPresentationProgress(paused.Sequence, false);
        engine.SetSpeed(SimulationSpeed.Speed4); // Modal restoration cannot clear the latch.
        Assert.Equal(SimulationSpeed.Speed0, engine.CurrentSpeed);
        engine.ResumePresentation(SimulationSpeed.Speed4);
        Assert.Equal(SimulationSpeed.Speed4, engine.CurrentSpeed);
        Assert.False(engine.CapturePresentationUpdate().AutomaticPause);
        Assert.True(initial.Sequence < paused.Sequence);
    }

    [Fact]
    public void Ordinary_pause_is_not_treated_as_feedback_failure_and_load_changes_session_identity()
    {
        long now = 0;
        using var engine = new SimulationEngine(Registry.Value);
        engine.LoadScenario(TorpedoImpactTests.Scenario());
        engine.SetPresentationTimestampForTests(() => now);
        engine.EnablePresentationLease();
        var before = engine.CapturePresentationUpdate(true, false);
        now = 10 * Stopwatch.Frequency;
        Assert.False(engine.CapturePresentationUpdate().AutomaticPause);
        engine.LoadScenario(TorpedoImpactTests.Scenario());
        var after = engine.CapturePresentationUpdate();
        Assert.NotEqual(before.SessionId, after.SessionId);
        Assert.Equal(1, after.Sequence);
        Assert.NotNull(after.FullSnapshot);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Intercept_or_miss_is_recorded_at_contact_with_the_actual_post_contact_flight(bool success)
    {
        long real = 0;
        var registry = success ? AutomaticDefenseTests.WeaponRegistry(100, 0) : Registry.Value;
        using var engine = new SimulationEngine(registry, clock: new SimulationClock(SimulationSpeed.Speed4, () => real));
        var scenario = AutomaticDefenseTests.DefenseScenario(600);
        engine.LoadScenario(scenario with { GameState = scenario.GameState with { CurrentSpeed = "Speed4" } });
        engine.EnablePresentationLease();
        engine.CapturePresentationUpdate(true, false);
        engine.ReceiveCommand(Fire());
        engine.CapturePresentationUpdate();
        real = 300;
        var update = engine.CapturePresentationUpdate();
        var revision = update.Frames.First(f => f.State.CombatJournal.Any(e =>
            e.Type == (success ? CombatEventType.Intercept : CombatEventType.Miss) && e.Roll is not null));
        var resolved = revision.State.CombatJournal.First(e => e.Roll is not null);
        Assert.Equal(801, resolved.Roll);
        Assert.Equal(resolved.MotionTimeMs, revision.MotionTimeMs);
        if (success)
        {
            Assert.DoesNotContain(revision.State.Objects, o => o.Torpedo is not null || o.Countermeasure is not null);
            Assert.Equal(TorpedoTerminationKind.Intercept, Assert.Single(revision.State.CombatImpacts).TerminationKind);
        }
        else
        {
            Assert.Single(revision.State.Objects, o => o.Torpedo is not null);
            Assert.Equal(CountermeasurePhase.MissedCoast,
                Assert.Single(revision.State.Objects, o => o.Countermeasure is not null).Countermeasure!.Phase);
            Assert.Contains(update.Frames, f => f.State.CombatJournal.Any(e => e.TerminationKind == CountermeasureTerminationKind.MissedCoastExpired));
        }
    }

    [Fact]
    public void Destroyed_target_rejection_does_not_change_rng_reload_or_projectile_sequence()
    {
        using var engine = Create();
        for (int shot = 0; shot < 3; shot++)
        {
            At(engine, shot * 15000);
            engine.ReceiveCommand(Fire("kill-" + shot));
            At(engine, shot * 15000);
            At(engine, shot * 15000 + 5000);
        }
        var before = engine.CaptureSaveStateForTests(35000, SimulationSpeed.Speed0);
        Assert.Contains(engine.CaptureSnapshotForTests(35000).CombatImpacts, i => i.DestroyedObjectId == Target);
        engine.ReceiveCommand(Fire("stale-target"));
        var rejected = engine.CaptureSnapshotForTests(35000);
        Assert.Equal(CommandReasonCodes.TargetDestroyed, Assert.Single(rejected.CommandResults).ReasonCode);
        var after = engine.CaptureSaveStateForTests(35000, SimulationSpeed.Speed0);
        Assert.Equal(JsonSerializer.Serialize(before.GameState.CombatState), JsonSerializer.Serialize(after.GameState.CombatState));
        Assert.Equal(JsonSerializer.Serialize(before.GameState.DefenseState), JsonSerializer.Serialize(after.GameState.DefenseState));
        At(engine, 45000);
        engine.ReceiveCommand(Fire("missing-target") with { TargetObjectId = null });
        Assert.Equal(CommandReasonCodes.MissingTarget, Assert.Single(At(engine, 45000).CommandResults).ReasonCode);
    }

    [Fact]
    public void Compact_publications_exclude_static_world_and_do_not_build_full_snapshots()
    {
        using var engine = new SimulationEngine(Registry.Value, clock: new SimulationClock(SimulationSpeed.Speed0, () => 0));
        var extras = Enumerable.Range(0, 2000).Select(i => Obstacle("static-" + i, 100000 + i * 100, 100000)).ToArray();
        engine.LoadScenario(TorpedoImpactTests.Scenario(null, extras));
        engine.EnablePresentationLease();
        Assert.Equal(2002, engine.CapturePresentationUpdate(true, false).FullSnapshot!.Objects.Length);
        for (int i = 0; i < 10; i++)
        {
            var update = engine.CapturePresentationUpdate();
            Assert.Null(update.FullSnapshot);
            Assert.All(update.Frames, frame => Assert.Equal(2, frame.State.Objects.Length));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Lease_expiry_does_not_simulate_the_unobserved_backlog_after_its_deadline(bool pauseCommand)
    {
        long real = 0;
        using var engine = new SimulationEngine(Registry.Value, clock: new SimulationClock(SimulationSpeed.Speed4, () => real));
        var scenario = TorpedoImpactTests.Scenario();
        engine.LoadScenario(scenario with { GameState = scenario.GameState with { CurrentSpeed = "Speed4" } });
        engine.SetPresentationTimestampForTests(() => real * Stopwatch.Frequency / 1000);
        engine.EnablePresentationLease();
        engine.CapturePresentationUpdate(true, false);
        real = 2500;
        if (pauseCommand) engine.SetSpeed(SimulationSpeed.Speed0);
        var stopped = engine.CapturePresentationUpdate();
        Assert.True(stopped.AutomaticPause);
        Assert.Equal(25000, stopped.ConfirmedMotionTimeMs); // Exactly the first 1000 real ms at Speed4.
        Assert.Equal(SimulationSpeed.Speed0, engine.CurrentSpeed);
    }

    [Fact]
    public void Countermeasure_trails_never_extend_beyond_their_frame_time()
    {
        long real = 0;
        using var engine = new SimulationEngine(Registry.Value, clock: new SimulationClock(SimulationSpeed.Speed4, () => real));
        var scenario = AutomaticDefenseTests.DefenseScenario(600);
        engine.LoadScenario(scenario with { GameState = scenario.GameState with { CurrentSpeed = "Speed4" } });
        engine.EnablePresentationLease();
        engine.CapturePresentationUpdate(true, false);
        engine.ReceiveCommand(Fire());
        engine.CapturePresentationUpdate();
        int trails = 0;
        for (real = 100; real <= 1000; real += 100)
        {
            var update = engine.CapturePresentationUpdate();
            Assert.All(update.Frames, frame =>
            {
                foreach (var projectile in frame.State.Objects.Where(o => o.Countermeasure is not null))
                    foreach (var segment in projectile.Countermeasure!.Trail)
                    {
                        trails++;
                        Assert.True(segment.StartMotionTimeMs + segment.Segment.DurationMs <= frame.MotionTimeMs + 1e-6);
                    }
            });
        }
        Assert.True(trails > 0);
    }

    [Fact]
    public void Living_static_target_leaves_compact_set_without_a_removal_after_flight_expiry()
    {
        long real = 0;
        var source = Registry.Value;
        static IEnumerable<T> All<T>(DeepSpaceSaga.Engine.Content.TypeRegistry<T> registry)
            where T : DeepSpaceSaga.Engine.Content.ITypeDefinition => Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);
        var registry = DeepSpaceSaga.Engine.Content.GameDataRegistry.Create(All(source.ModuleCategories),
            All(source.ModuleTypes).Select(m => m.TorpedoSpeedKmS is null ? m : m with { TorpedoMaxFlightTimeMs = 1000 }),
            All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes),
            All(source.Dialogues), All(source.Quests), stationMarketProfiles: All(source.StationMarketProfiles),
            shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents),
            combatRules: source.CombatRules, fireModes: source.FireModes);
        using var engine = new SimulationEngine(registry, clock: new SimulationClock(SimulationSpeed.Speed4, () => real));
        const string asteroidId = "distant-asteroid";
        var scenario = TorpedoImpactTests.Scenario(null, Obstacle(asteroidId, 0, -1900));
        engine.LoadScenario(scenario with { GameState = scenario.GameState with { CurrentSpeed = "Speed4" } });
        engine.EnablePresentationLease();
        engine.CapturePresentationUpdate(true, false);
        engine.ReceiveCommand(Fire() with { TargetObjectId = asteroidId });
        var launch = engine.CapturePresentationUpdate();
        string torpedoId = Assert.Single(launch.Frames[^1].State.Objects, o => o.Torpedo is not null).ObjectId;
        Assert.Contains(launch.Frames[^1].State.Objects, o => o.ObjectId == asteroidId);
        CombatPresentationUpdate update = launch;
        for (real = 100; real <= 10000; real += 100)
        {
            update = engine.CapturePresentationUpdate();
            Assert.DoesNotContain(update.Frames.SelectMany(f => f.RemovedObjectIds), id => id == asteroidId);
            if (update.Frames.Any(f => f.RemovedObjectIds.Contains(torpedoId))) break;
        }
        Assert.Contains(update.Frames, f => f.RemovedObjectIds.Contains(torpedoId));
        Assert.Contains(update.Frames.SelectMany(f => f.State.CombatImpacts), i => i.TerminationKind == TorpedoTerminationKind.Expired);
        Assert.DoesNotContain(update.Frames[^1].State.Objects, o => o.ObjectId == asteroidId);
        Assert.Contains(engine.CaptureSnapshot().Objects, o => o.ObjectId == asteroidId);
    }
}
