using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
using Point = DeepSpaceSaga.Engine.Tests.PointCounterFixture;

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
        Assert.Equal(0ul, Save(restored, 30000).GameState.DefenseState!.RngCounter);
    }

    [Theory]
    [InlineData(501, 2)]
    [InlineData(1050, 1)]
    [InlineData(3501, 0)]
    public void Two_point_launchers_resume_with_distinct_deadlines(long checkpoint, int activeCount)
    {
        using var continuous = Point.Create();
        continuous.ReceiveCommand(Point.Shot("first", x: 150, y: 0)); Point.At(continuous);
        Point.At(continuous, 100);
        continuous.ReceiveCommand(Point.Shot("second", Point.Counter2, x: 150, y: 0)); Point.At(continuous, 100);
        var save = Point.Save(continuous, checkpoint);
        Assert.Equal(activeCount, save.GameState.PointCombatState!.Projectiles.Count);
        var deadlines = save.GameState.DefenseState!.Launchers.Where(l => l.OwnerObjectId == Point.Player).Select(l => l.State.ReadyAtMotionTimeMs).ToArray();
        Assert.Equal(new double?[] { 12500, 12600 }, deadlines);
        using var restored = Roundtrip(save, Point.Registry());
        restored.ReceiveCommand(Point.Shot("first", Point.Counter2, x: -150, profile: "fast"));
        foreach (long time in new[] { checkpoint, checkpoint + 1, 4000, 12600 })
            Assert.Equal(ScenarioLoader.Serialize(Point.Save(continuous, time)), ScenarioLoader.Serialize(Point.Save(restored, time)));
        Assert.Equal(4ul, Point.Save(restored, 12600).GameState.PointCombatState!.LaunchScatter.Counter);
        Assert.Equal(0ul, Point.Save(restored, 12600).GameState.DefenseState!.RngCounter);
    }

    [Fact]
    public void Reload_preserves_explicit_readiness_without_automatic_retry_or_rng()
    {
        using var continuous = Point.Create(); continuous.ReceiveCommand(Point.Shot(x: 150, y: 0)); Point.At(continuous);
        var save = Point.Save(continuous, 1001);
        using var restored = Roundtrip(save, Point.Registry());
        foreach (long time in new[] { 1001L, 12499, 12500, 20000 })
        {
            Assert.Equal(ScenarioLoader.Serialize(Point.Save(continuous, time)), ScenarioLoader.Serialize(Point.Save(restored, time)));
            Assert.Empty(Point.At(restored, time).Objects.Where(o => o.OwnProjectile is not null));
        }
        Assert.Single(Point.At(restored, 20000).CombatJournal, e => e.Type == CombatEventType.Launch);
        Assert.Equal(2ul, Point.Save(restored, 20000).GameState.PointCombatState!.LaunchScatter.Counter);
        restored.ReceiveCommand(Point.Shot("explicit-next"));
        Assert.Single(Point.Save(restored, 20000).GameState.PointCombatState!.Projectiles);
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
