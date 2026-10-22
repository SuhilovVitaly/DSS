using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using Point = DeepSpaceSaga.Engine.Tests.PointCounterFixture;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0018TK0005Tests
{
    private static SpaceObjectData FatalPlayer(SpaceObjectData o) => o.ObjectId != Point.Player ? o : o with
    {
        Modules = o.Modules!.Where(m => m.ModuleTypeId.StartsWith("module.engine.", StringComparison.Ordinal) || m.ModuleId == Point.Torpedo)
            .Select(m => m with { StructurePoints = 1, ArmorClass = "none" }).ToArray()
    };

    [Fact]
    public void Same_timestamp_player_launch_precedes_npc_launch_and_scatter_consumption()
    {
        using var engine = Point.Create(enemyArmed: true, configure: o => o.ObjectId == Point.Enemy ? o with { Ai = new(25, 8, 35) } : o);
        engine.ReceiveCommand(new("player-first", 1, Point.Player, Point.Torpedo, CombatCommandTypes.Launch,
            TargetWorldX: 200, TargetWorldY: 0, ProfileId: "fast"));
        var snapshot = Point.At(engine, 1);
        var launches = snapshot.CombatJournal.Where(e => e.Type == CombatEventType.Launch).ToArray();
        Assert.Equal(new[] { Point.Player, Point.Enemy }, launches.Select(e => e.ActorObjectId));
        Assert.All(launches, e => Assert.Equal(1, e.MotionTimeMs));
        Assert.Equal(4ul, engine.CaptureLaunchScatterState().Counter);
    }

    [Fact]
    public void Player_manual_detonation_can_defeat_the_player_before_same_timestamp_npc_launch()
    {
        using var engine = Point.Create(enemyArmed: true, configure: o => o.ObjectId == Point.Enemy ? o with { Ai = new(25, 8, 35) } : FatalPlayer(o));
        engine.ReceiveCommand(new("setup", 1, Point.Player, Point.Torpedo, CombatCommandTypes.Launch,
            TargetWorldX: 600, TargetWorldY: 0, ProfileId: "slow"));
        string projectile = Assert.Single(Point.At(engine).Objects, o => o.OwnProjectile is not null).ObjectId;
        engine.ReceiveCommand(new("manual-first", 2, Point.Player, Point.Torpedo, CombatCommandTypes.Detonate, projectile));
        var snapshot = Point.At(engine, 1);
        Assert.Equal(SessionOutcomeState.Defeated, snapshot.EffectiveOutcome.State);
        Assert.Equal(1, snapshot.EffectiveOutcome.DeathMotionTimeMs);
        Assert.DoesNotContain(snapshot.CombatJournal, e => e.Type == CombatEventType.Launch && e.ActorObjectId == Point.Enemy);
        Assert.Equal(2ul, engine.CaptureLaunchScatterState().Counter);
    }

    [Fact]
    public void Scheduled_blast_death_precedes_the_queued_player_launch_at_the_same_timestamp()
    {
        using var engine = Point.Create(enemyArmed: true, enemyX: 16, configure: FatalPlayer);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Point.Enemy,
            Point.EnemyTorpedo, 0, 0, "fast", 0).Disposition);
        Point.At(engine);
        engine.ReceiveCommand(new("too-late", 1, Point.Player, Point.Torpedo, CombatCommandTypes.Launch,
            TargetWorldX: 200, TargetWorldY: 0, ProfileId: "fast"));
        var snapshot = Point.At(engine, 100);
        Assert.Equal(SessionOutcomeState.Defeated, snapshot.EffectiveOutcome.State);
        Assert.Equal(100, snapshot.EffectiveOutcome.DeathMotionTimeMs);
        Assert.Equal(CommandReasonCodes.SessionDefeated, Assert.Single(snapshot.CommandResults).ReasonCode);
        Assert.DoesNotContain(snapshot.CombatJournal, e => e.Type == CombatEventType.Launch && e.ActorObjectId == Point.Player);
        Assert.Equal(2ul, engine.CaptureLaunchScatterState().Counter);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Real_clock_snapshot_and_explicit_save_preserve_player_before_npc_boundary_order(bool save)
    {
        using var source = Point.Create(enemyArmed: true,
            configure: o => o.ObjectId == Point.Enemy ? o with { Ai = new(25, 8, 35) } : o);
        var baseline = Point.Save(source);
        long real = 0;
        using var engine = new SimulationEngine(Point.Registry(), clock: new SimulationClock(SimulationSpeed.Speed0, () => real));
        engine.LoadScenario(baseline with { GameState = baseline.GameState with { CurrentSpeed = "Speed1" } }, isSave: true);
        engine.ReceiveCommand(new("real-player-first", 1, Point.Player, Point.Torpedo, CombatCommandTypes.Launch,
            TargetWorldX: 200, TargetWorldY: 0, ProfileId: "fast"));
        real = 4;
        IEnumerable<CombatJournalEntry> journal = save ? engine.CaptureSaveStateForTests(1, SimulationSpeed.Speed1, 1).GameState.DefenseState!.Journal :
            engine.CaptureSnapshot(advanceClock: true).CombatJournal;
        var launches = journal.Where(e => e.Type == CombatEventType.Launch).ToArray();
        Assert.Equal(new[] { Point.Player, Point.Enemy }, launches.Select(e => e.ActorObjectId));
        Assert.All(launches, e => Assert.Equal(1, e.MotionTimeMs));
        Assert.Equal(4ul, engine.CaptureLaunchScatterState().Counter);
    }

    [Theory]
    [InlineData(1ul)]
    [InlineData(42ul)]
    [InlineData(ulong.MaxValue)]
    public void New_combat_preserves_module_damage_and_defeat(ulong seed)
    {
        using var engine = Point.Create(enemyArmed: true, seed: seed, enemyX: 155,
            configure: o => o.ObjectId == Point.Enemy ? o with { Ai = new(25, 8, 35) } : FatalPlayer(o));
        Point.At(engine, 1);
        var flight = Assert.Single(Point.Save(engine, 1).GameState.PointCombatState!.Projectiles).Flight;
        Assert.Equal("slow", flight.ProfileId);
        Assert.Equal(6, flight.SpeedKmS); Assert.Equal(200, flight.Damage); Assert.Equal(20, flight.ArmorPiercingPercent);
        const double fuse = 1 + 15500d / 6; // 15.5 km at the first equal-coverage catalog profile, launched at 1 ms
        Assert.Equal(fuse, flight.DetonationMotionTimeMs, 10);
        Assert.Equal(SessionOutcomeState.Running, Point.At(engine, 2584).EffectiveOutcome.State);
        var final = Point.At(engine, 2585);
        Assert.Equal(SessionOutcomeState.Defeated, final.EffectiveOutcome.State);
        Assert.Equal(fuse, final.EffectiveOutcome.DeathMotionTimeMs);
        Assert.Contains(final.CombatJournal, e => e.Type == CombatEventType.ModuleHit && e.TargetObjectId == Point.Player && e.StructureAfter == 0);
        Assert.Single(final.Objects, o => o.ObjectType == SpaceObjectType.Wreck);
        Assert.All(final.CombatJournal, e => { Assert.Null(e.Roll); Assert.Null(e.ChanceTenths); });
        Assert.Equal(final.Outcome, Point.At(engine, 10000).Outcome);
    }
}
