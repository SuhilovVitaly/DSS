using System.Diagnostics;
using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatPresentationTests
{
    private static PlayerCommand PointLaunch(string id = "presentation-point") => LaunchAt(id, 0, -100);

    [Fact]
    public void Flight_between_full_snapshots_retains_launch_contact_geometry_and_consequences()
    {
        long real = 0;
        using var engine = new SimulationEngine(Registry.Value, clock: new SimulationClock(SimulationSpeed.Speed4, () => real));
        var scenario = TorpedoImpactTests.Scenario();
        engine.LoadScenario(scenario with { GameState = scenario.GameState with { CurrentSpeed = "Speed4" } });
        engine.EnablePresentationLease();
        var bootstrap = engine.CapturePresentationUpdate(true, false);
        engine.ReceiveCommand(PointLaunch());
        var launch = engine.CapturePresentationUpdate();
        Assert.Contains(launch.Frames.SelectMany(f => f.State.Objects), o => o.OwnProjectile is not null);
        engine.ReportPresentationProgress(launch.Sequence, false);
        real = 200; // Five physical seconds; the 10 km flight ends inside this update.
        var contact = engine.CapturePresentationUpdate();
        Assert.Null(contact.FullSnapshot);
        Assert.DoesNotContain(bootstrap.FullSnapshot!.Objects, o => o.OwnProjectile is not null);
        Assert.DoesNotContain(contact.Frames[^1].State.Objects, o => o.OwnProjectile is not null);
        var fact = Assert.Single(contact.Frames.SelectMany(f => f.State.CombatJournal).Where(e => e.Type == CombatEventType.TorpedoDetonated).DistinctBy(e => e.EventId));
        var frame = contact.Frames.First(f => f.State.CombatJournal.Any(e => e.EventId == fact.EventId));
        Assert.Equal(fact.MotionTimeMs, frame.MotionTimeMs);
        Assert.InRange(Math.Abs(frame.State.GameTimeMs - fact.MotionTimeMs * 300), 0, 1);
        Assert.Contains(frame.State.CombatJournal, e => e.Type == CombatEventType.Hit);
        Assert.NotEmpty(frame.State.CombatReports!.Reports);
        Assert.DoesNotContain(frame.State.Objects, o => o.ObjectId == fact.ProjectileObjectId);
        Assert.Contains(fact.ProjectileObjectId, frame.RemovedObjectIds);
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
            engine.ReceiveCommand(PointLaunch());
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

}
