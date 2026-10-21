using DeepSpaceSaga.Contracts;
using Point = DeepSpaceSaga.Engine.Tests.PointCounterFixture;
namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0003TK0005Tests
{
    [Fact]
    public void Historical_unarmed_scenario_authoring_does_not_relax_actual_save_integrity()
    {
        using var engine = Point.Create(); var current = Point.Save(engine);
        var missing = current with { GameState = current.GameState with { PointCombatState = null } };
        string before = DeepSpaceSaga.Engine.Scenario.ScenarioLoader.Serialize(current);
        Assert.Throws<DeepSpaceSaga.Engine.Scenario.ScenarioException>(() => engine.LoadScenario(missing, isSave: true));
        Assert.Equal(before, DeepSpaceSaga.Engine.Scenario.ScenarioLoader.Serialize(Point.Save(engine)));
        var historical = TradingEconomySaveSchemaTests.WithoutNewContinuation(current, 14);
        Assert.Throws<DeepSpaceSaga.Engine.Scenario.ScenarioException>(() => engine.LoadScenario(historical, isSave: true));
        engine.LoadScenario(historical); // Explicit scenario authoring; weapons have been removed.
        Assert.Empty(Point.At(engine).InstalledModules.Where(m => m.LauncherCombat is not null || m.Defense is not null));
        Assert.NotNull(Point.Save(engine).GameState.PointCombatState);
        Assert.Equal(0ul, engine.CaptureLaunchScatterState().Counter);
    }

    [Theory]
    [InlineData(0ul)]
    [InlineData(42ul)]
    [InlineData(ulong.MaxValue)]
    public void Torpedo_resolution_has_no_hit_roll_or_mode_rng(ulong seed)
    {
        using var engine = Point.Create(seed: seed);
        Assert.Equal(seed, engine.MasterSeed);
        engine.ReceiveCommand(new("core", 1, Point.Player, Point.Torpedo, CombatCommandTypes.Launch, TargetWorldX: 200, TargetWorldY: 0, ProfileId: "fast"));
        Point.At(engine); var result = Point.At(engine, 1250);
        Assert.Single(result.CombatJournal, e => e.Type == CombatEventType.Hit && e.TargetObjectId == Point.Enemy);
        var save = Point.Save(engine, 1250);
        Assert.Equal(2ul, save.GameState.PointCombatState!.LaunchScatter.Counter);
        Assert.Equal(1ul, save.GameState.CombatState!.HitLocationRngCounter);
        Assert.Equal(0ul, save.GameState.CombatState.TorpedoHitRngCounter);
        Assert.Equal(0ul, save.GameState.CombatState.LauncherBreakdownRngCounter);
        Assert.Equal(0ul, save.GameState.DefenseState!.RngCounter);
        Assert.All(result.CombatJournal, e => { Assert.Null(e.Roll); Assert.Null(e.ChanceTenths); Assert.Null(e.RatingBreakdown); });
        Assert.DoesNotContain(result.Objects, o => o.OwnProjectile is not null);
    }

    [Theory]
    [InlineData(false, 50)]
    [InlineData(true, 75)]
    public void Turning_the_carrier_does_not_replan_or_redirect_either_captured_point_flight(bool counter, double x)
    {
        using var engine = Point.Create(configure: o => o.ObjectId != Point.Player ? o : o with
        { Modules = o.Modules!.Select(m => m.ModuleTypeId.StartsWith("module.engine.", StringComparison.Ordinal) ? m with { PowerState = "On" } : m).ToArray() });
        engine.ReceiveCommand(counter ? Point.Shot(x: 200, y: 0) : new("torpedo", 1, Point.Player, Point.Torpedo, CombatCommandTypes.Launch, TargetWorldX: 200, TargetWorldY: 0, ProfileId: "medium"));
        var initial = Point.At(engine); var flight = Point.Flight(initial, counter ? Point.Counter : Point.Torpedo);
        Point.At(engine, 200); engine.ReceiveCommand(new("turn", 2, Point.Player, "MOD-PLAYER-ENGINE-01", ShipEngineCommandTypes.TurnRightStep)); Point.At(engine, 200);
        var moved = Point.At(engine, 500); var current = Point.Flight(moved, counter ? Point.Counter : Point.Torpedo);
        Assert.NotEqual(initial.Objects.Single(o => o.ObjectId == Point.Player).Direction, moved.Objects.Single(o => o.ObjectId == Point.Player).Direction);
        Assert.Equal(flight.DetonationX, current.DetonationX); Assert.Equal(flight.DetonationY, current.DetonationY);
        Assert.Equal(flight.DetonationMotionTimeMs, current.DetonationMotionTimeMs); Assert.Equal(x, current.X, 8); Assert.Equal(0, current.Y, 8);
        Assert.Equal(2ul, engine.CaptureLaunchScatterState().Counter);
    }

    [Fact]
    public void Retired_math_rng_and_scheduler_types_have_no_runtime_consumers()
    {
        foreach (string type in new[] { "FireMode" + "Math", "CombatChance" + "Math", "Countermeasure" + "Rng", "Interception" + "Math" })
            Assert.Null(typeof(SimulationEngine).Assembly.GetType("DeepSpaceSaga.Engine.Combat." + type));
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/DeepSpaceSaga.Engine"));
        string source = string.Join("\n", Directory.GetFiles(root, "SimulationEngine*.cs").Select(File.ReadAllText));
        foreach (string retired in new[] { "TryLaunchTorpedo(", "TrySetFireMode(", "RefreshCombatGuidance(", "TorpedoGuidanceMath.",
            "CountermeasureGuidanceMath.", "_torpedoHitRng", "_launcherBreakdownRng", "_countermeasureRng" }) Assert.DoesNotContain(retired, source);
    }
}
