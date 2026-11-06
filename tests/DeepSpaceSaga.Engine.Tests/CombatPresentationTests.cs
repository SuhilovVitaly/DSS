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
