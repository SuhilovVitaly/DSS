using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using Point = DeepSpaceSaga.Engine.Tests.PointCounterFixture;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
namespace DeepSpaceSaga.Engine.Tests;

public sealed class DisabledModuleTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Disabled_point_launchers_reject_then_recover_exactly_at_deadline(bool counter)
    {
        string module = counter ? Point.Counter : Point.Torpedo;
        using var engine = Point.Create(configure: o => o.ObjectId != Point.Player ? o : o with
        { Modules = o.Modules!.Select(m => m.ModuleId == module ? m with { DisabledUntilMotionTimeMs = 60000 } : m).ToArray() });
        PlayerCommand Shot(string id) => counter ? Point.Shot(id) : new(id, 1, Point.Player, module, CombatCommandTypes.Launch, TargetWorldX: 150, TargetWorldY: 30, ProfileId: "medium");
        engine.ReceiveCommand(Shot("early")); var before = Point.At(engine);
        Assert.Equal(CommandReasonCodes.ModuleDisabled, Assert.Single(before.CommandResults).ReasonCode);
        Assert.Empty(Point.Save(engine).GameState.PointCombatState!.Projectiles); Assert.Equal(0ul, engine.CaptureLaunchScatterState().Counter);
        Point.At(engine, 59999); engine.ReceiveCommand(Shot("still-early"));
        Assert.Equal(CommandReasonCodes.ModuleDisabled, Assert.Single(Point.At(engine, 59999).CommandResults).ReasonCode);
        Point.At(engine, 60000); engine.ReceiveCommand(Shot("ready")); var ready = Point.At(engine, 60000);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(ready.CommandResults).Status);
        Assert.Single(ready.Objects, o => o.OwnProjectile is not null); Assert.Equal(2ul, engine.CaptureLaunchScatterState().Counter);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Destroyed_point_launcher_has_a_distinct_reason_and_no_random_effect(bool counter)
    {
        string module = counter ? Point.Counter : Point.Torpedo;
        using var engine = Point.Create(configure: o => o.ObjectId != Point.Player ? o : o with
        { Modules = o.Modules!.Select(m => m.ModuleId == module ? m with { StructurePoints = 0, Destroyed = true } : m).ToArray() });
        engine.ReceiveCommand(counter ? Point.Shot() : new("destroyed", 1, Point.Player, module, CombatCommandTypes.Launch, TargetWorldX: 100, TargetWorldY: 0, ProfileId: "slow"));
        Assert.Equal(CommandReasonCodes.ModuleDestroyed, Assert.Single(Point.At(engine).CommandResults).ReasonCode);
        Assert.Equal(0ul, engine.CaptureLaunchScatterState().Counter);
    }

    [Fact]
    public void Disabled_engine_coasts_and_recovers_current_point_ai_at_deadline()
    {
        using var engine = Create(TorpedoImpactTests.Scenario(o => o.ObjectId != Target ? o : o with
        {
            Ai = new(25, 8, 35),
            PositionY = -1000,
            SpeedMps = 1000,
            MovementType = "Linear",
            Modules = o.Modules!.Select(m => m.ModuleTypeId == "module.engine.basic"
                ? m with { PowerState = "On", DisabledUntilMotionTimeMs = 60000 } : m).ToArray()
        }));
        var before = At(engine, 0).Objects.Single(o => o.ObjectId == Target); var during = At(engine, 1000).Objects.Single(o => o.ObjectId == Target);
        Assert.Null(during.HostileCombat); Assert.Equal(before.Direction, during.Direction); Assert.Equal(before.SpeedKmS, during.SpeedKmS);
        Assert.Equal(before.Y - 10, during.Y, 6);
        Assert.NotNull(At(engine, 60100).Objects.Single(o => o.ObjectId == Target).HostileCombat);
    }

    [Fact]
    public void Real_nonlethal_edge_hit_interrupts_current_engine_cycle_then_coasts_for_eight_seconds()
    {
        const string module = "MOD-PLAYER-ENGINE-01";
        using var engine = Point.Create(enemyArmed: true, configure: o => o.ObjectId != Point.Player ? o : o with
        { SpeedMps = 1000, MovementType = "Linear", Modules = o.Modules!.Where(m => m.ModuleId == module).Select(m => m with { PowerState = "On", ArmorClass = "light" }).ToArray() });
        engine.ReceiveCommand(new("turn-before-hit", 1, Point.Player, module, ShipEngineCommandTypes.TurnRightUntilCancel)); Point.At(engine);
        Assert.Equal(ShipEngineCommandTypes.TurnRightUntilCancel, Point.At(engine, 1).InstalledModules.Single().ActiveCommandType);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Point.Enemy, Point.EnemyTorpedo, 80, 0, "slow", 1).Disposition);
        var hit = Point.At(engine, 2001); Assert.Equal(SessionOutcomeState.Running, hit.EffectiveOutcome.State);
        var fact = Assert.Single(hit.CombatJournal, e => e.Type == CombatEventType.ModuleHit && e.TargetObjectId == Point.Player);
        Assert.True(fact.StructureAfter > 0); Assert.True(fact.StructureAfter < fact.StructureBefore);
        var saved = Point.Save(engine, 2001); var ship = saved.GameState.SpaceObjects.Single(o => o.ObjectId == Point.Player); var damaged = Assert.Single(ship.Modules!);
        Assert.Null(damaged.ActiveCycle); Assert.Equal(fact.MotionTimeMs + 8000, damaged.DisabledUntilMotionTimeMs);
        var pose = hit.Objects.Single(o => o.ObjectId == Point.Player); Assert.Null(pose.ActiveEngineCommandType); Assert.Null(pose.HostileCombat);
        engine.ReceiveCommand(new("disabled", 2, Point.Player, module, ShipEngineCommandTypes.TurnRightStep));
        Assert.Equal(CommandReasonCodes.ModuleDisabled, Assert.Single(Point.At(engine, 2001).CommandResults).ReasonCode);
        var coast = Point.At(engine, 3001).Objects.Single(o => o.ObjectId == Point.Player);
        Assert.Equal(pose.Direction, coast.Direction); Assert.Equal(pose.SpeedKmS, coast.SpeedKmS);
        Assert.Equal(10 * pose.SpeedKmS, double.Hypot(coast.X - pose.X, coast.Y - pose.Y), 6);
    }

    [Fact]
    public void Retired_pending_burst_is_rejected_atomically_instead_of_being_resumed_after_disable()
    {
        using var engine = Point.Create(); var before = Point.Save(engine);
        var bad = before with
        {
            GameState = before.GameState with
            {
                DefenseState = before.GameState.DefenseState! with
                { Launchers = before.GameState.DefenseState!.Launchers.Select(l => l with { State = l.State with { PendingBurstShots = [new(10000, "torpedo-1", 2)] } }).ToArray() }
            }
        };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(bad, true));
        Assert.Equal(ScenarioLoader.Serialize(before), ScenarioLoader.Serialize(Point.Save(engine)));
    }
}
