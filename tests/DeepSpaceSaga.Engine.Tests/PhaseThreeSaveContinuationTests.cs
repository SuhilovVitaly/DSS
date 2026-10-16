using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
using static DeepSpaceSaga.Engine.Tests.ManualCountermeasureFireTests;

namespace DeepSpaceSaga.Engine.Tests;

public class PhaseThreeSaveContinuationTests
{
    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
    private static ScenarioFile Save(SimulationEngine engine, long time) => engine.CaptureSaveStateForTests(time, SimulationSpeed.Speed0, time);

    private static SimulationEngine Roundtrip(ScenarioFile save, GameDataRegistry? registry = null)
    {
        string path = Path.Combine(Path.GetTempPath(), "ep8-continuation-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, ScenarioLoader.Serialize(save));
            var engine = new SimulationEngine(registry ?? Registry.Value);
            try { engine.LoadScenario(ScenarioLoader.LoadFromFile(path, allowNonZeroGameTime: true, isSave: true), isSave: true); }
            catch { engine.Dispose(); throw; }
            return engine;
        }
        finally { File.Delete(path); }
    }

    private static void Same(SimulationEngine continuous, SimulationEngine restored, long time)
    {
        Assert.Equal(ScenarioLoader.Serialize(Save(continuous, time)), ScenarioLoader.Serialize(Save(restored, time)));
    }

    [Theory]
    [InlineData(false, 155)]
    [InlineData(false, 1200)]
    [InlineData(true, 155)]
    [InlineData(true, 1200)]
    public void Orbit_handedness_and_rng_continue_exactly(bool orbit, long checkpoint)
    {
        using var continuous = Create(orbit ? HostileCombatPathTests.OrbitScene(true) : HostileOrbitRuntimeTests.Scene());
        At(continuous, 100);
        if (orbit) { continuous.ReceiveCommand(Fire("saved-attack")); At(continuous, 100); }
        var save = Save(continuous, checkpoint);
        var actor = Assert.Single(save.GameState.HostileCombatState!.Actors);
        Assert.True(actor.ActiveMotion);
        Assert.Equal(orbit ? HostileCombatPhase.OrbitHold : HostileCombatPhase.Pursuit, actor.State.Phase);
        using var restored = Roundtrip(save);
        Same(continuous, restored, checkpoint);
        foreach (long time in new[] { checkpoint + 1, checkpoint + 99, 3000, 30000 })
        {
            Same(continuous, restored, time);
            var a = At(continuous, time); var b = At(restored, time);
            Assert.Equal(Json(a.Objects), Json(b.Objects));
            Assert.Equal(Json(a.CombatJournal), Json(b.CombatJournal));
            Assert.Equal(actor.State.Clockwise, b.Objects.Single(o => o.ObjectId == Target).HostileCombat!.Clockwise);
        }
        if (orbit) Assert.True(Save(restored, 30000).GameState.DefenseState!.RngCounter > 0);
    }

    [Theory]
    [InlineData(501, CountermeasurePhase.Guiding)]
    [InlineData(1101, CountermeasurePhase.MissedCoast)]
    [InlineData(3501, null)]
    public void Two_launchers_resume_with_distinct_deadlines(long checkpoint, CountermeasurePhase? expectedPhase)
    {
        var registry = AutomaticDefenseTests.WeaponRegistry(0, 100);
        using var continuous = new SimulationEngine(registry);
        var scenario = HostileTorpedoFireTests.HostileScenario(155);
        continuous.LoadScenario(scenario with
        {
            GameState = scenario.GameState with
            { SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o.ObjectId == Target ? o with { DirectionDegrees = 180 } : o).ToArray() }
        });
        string target = LaunchEnemy(continuous);
        continuous.ReceiveCommand(Manual("first", target)); At(continuous, 1);
        At(continuous, 101);
        continuous.ReceiveCommand(Manual("second", target, Defense2)); At(continuous, 101);
        var save = Save(continuous, checkpoint);
        var defense = save.GameState.DefenseState!;
        if (expectedPhase is { } phase)
        {
            Assert.Equal(2, defense.Projectiles.Count);
            Assert.All(defense.Projectiles, p => Assert.Equal(phase, p.Flight.Phase));
            Assert.Equal(2, defense.Projectiles.Select(p => p.Flight.ExpiresAtMotionTimeMs).Distinct().Count());
        }
        else
        {
            Assert.Empty(defense.Projectiles);
            var deadlines = defense.Launchers.Where(l => l.OwnerObjectId == Player).Select(l => l.State.ReloadDueMotionTimeMs).ToArray();
            Assert.All(deadlines, d => Assert.NotNull(d));
            Assert.Equal(2, deadlines.Distinct().Count());
        }
        using var restored = Roundtrip(save, registry);
        Same(continuous, restored, checkpoint);
        // Command identity is durable across launchers and does not consume another projectile ID.
        restored.ReceiveCommand(Manual("first", target, Defense2)); At(restored, checkpoint);
        Same(continuous, restored, checkpoint);
        foreach (long time in new[] { checkpoint + 1, 4000, 5000 }) Same(continuous, restored, time);
        Assert.Equal(2ul, Save(restored, 5000).GameState.DefenseState!.RngCounter);
    }

    [Fact]
    public void Reload_preserves_automatic_retry_and_rng()
    {
        var registry = AutomaticDefenseTests.WeaponRegistry(1, 0);
        using var continuous = new SimulationEngine(registry);
        continuous.LoadScenario(AutomaticDefenseTests.DefenseScenario());
        continuous.ReceiveCommand(Fire("auto-target")); At(continuous, 0);
        var save = Save(continuous, 17000);
        var defense = Assert.IsType<CountermeasureStateData>(save.GameState.DefenseState);
        Assert.DoesNotContain("AttemptedTorpedoIds", JsonSerializer.Serialize(defense));
        Assert.Contains(defense.Journal, e => e.Type == CombatEventType.Miss && e.LaunchMode == LaunchMode.Auto);
        Assert.True(defense.RngCounter >= 1);
        using var restored = Roundtrip(save, registry);
        foreach (long time in new[] { 17000, 18000, 19999 }) Same(continuous, restored, time);
        Assert.Equal(At(continuous, 19999).CombatJournal.Count(e => e.Result == "countermeasure" && e.Type == CombatEventType.Launch),
            At(restored, 19999).CombatJournal.Count(e => e.Result == "countermeasure" && e.Type == CombatEventType.Launch));
        Assert.Equal(Save(continuous, 19999).GameState.DefenseState!.RngCounter,
            Save(restored, 19999).GameState.DefenseState!.RngCounter);
    }

    [Fact]
    public void Hostile_next_fire_boundary_survives_real_file_load()
    {
        using var continuous = Create(PlayerDefeatTests.LethalScenario());
        var save = Save(continuous, 5000);
        Assert.Single(save.GameState.CombatState!.Projectiles);
        using var restored = Roundtrip(save);
        Same(continuous, restored, 5001);
        Same(continuous, restored, 17501);
        var next = Assert.Single(Save(restored, 17501).GameState.CombatState!.Projectiles);
        Assert.InRange(next.Flight.LaunchMotionTimeMs, 16001, 17501);
        Same(continuous, restored, 20000);
    }

    [Fact]
    public void Invalid_staged_ai_engine_does_not_replace_running_world()
    {
        using var engine = Create(HostileOrbitRuntimeTests.Scene());
        var save = Save(engine, 155);
        var actor = Assert.Single(save.GameState.HostileCombatState!.Actors);
        var bad = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId != actor.ObjectId ? o : o with
                { Modules = o.Modules!.Select(m => m.ModuleId == actor.State.EngineModuleId ? m with { StructurePoints = 0 } : m).ToArray() }).ToArray()
            }
        };
        // Structural JSON checks succeed; registry/runtime compatibility must still be atomic.
        var parsed = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(bad), true, isSave: true);
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(parsed, isSave: true));
        Assert.Equal(ScenarioLoader.Serialize(save), ScenarioLoader.Serialize(Save(engine, 155)));
    }

    [Fact]
    public void Saved_geography_and_voyage_finance_are_preserved()
    {
        using var fixture = TradingVoyageFixture.Create();
        fixture.Trade(TradeCommandTypes.Buy, fixture.OutboundItem, 2);
        var (_, receipt) = fixture.Send(QuotedTradeExecutionTests.BridgeModuleId, NavigationComputerCommandTypes.Undock, target: fixture.Destination);
        Assert.Equal(CommandResultStatus.Executed, receipt!.Status);
        var save = fixture.Save();
        Assert.NotNull(save.GameState.VoyageState);
        Assert.NotNull(save.GameState.TradingMap);
        using var restored = Roundtrip(save, QuotedTradeExecutionTests.RealRegistry());
        foreach (long delta in new[] { 0L, 101, 499 })
        {
            var expected = fixture.Advance(delta);
            var actual = restored.CaptureSnapshotForTests(expected.GameTimeMs, SimulationSpeed.Speed0, fixture.MotionTime);
            var a = fixture.Save();
            var b = restored.CaptureSaveStateForTests(expected.GameTimeMs, SimulationSpeed.Speed0, fixture.MotionTime);
            Assert.Equal(Json(SimulationEngine.NormalizeTradingContinuationForTests(a.GameState)), Json(SimulationEngine.NormalizeTradingContinuationForTests(b.GameState)));
            Assert.Equal(Json(expected.ActiveVoyage), Json(actual.ActiveVoyage));
        }
    }
}
