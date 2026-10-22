using System.Text.Json;
using DeepSpaceSaga.Contracts;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0004TK0001Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0004TK0002Tests
{
    private const string ForeignTorpedo = "MOD-PIRATE-TORPEDO-01", ForeignCounter = "MOD-PIRATE-COUNTERMEASURE-01";

    private static SimulationEngine Cluster()
    {
        var engine = CreateCounter(o => o.ObjectId != Target ? o : o with
        {
            PositionX = 0,
            PositionY = 0,
            Modules = o.Modules!.Select(m => m.ModuleId is ForeignTorpedo or ForeignCounter ? m with { PowerState = "On" } : m).ToArray()
        }, scatter: 0, torpedoScatter: 0);
        engine.ReceiveCommand(Launch()); At(engine);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, ForeignTorpedo, 700, 0, "slow", 0).Disposition);
        engine.ReceiveCommand(CounterLaunch("own-counter-victim", module: Defense2)); At(engine);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, ForeignCounter, 300, 0, "wide", 0, ProjectileKind.Countermissile).Disposition);
        engine.ReceiveCommand(CounterLaunch("trigger", x: 0));
        Assert.Equal(5, At(engine).Objects.Count(o => o.ObjectType is SpaceObjectType.Missile or SpaceObjectType.Countermeasure));
        return engine;
    }

    [Fact]
    public void Countermissile_destroys_all_projectile_kinds_and_sides()
    {
        using var engine = Cluster();
        var before = At(engine);
        var ownHealth = before.Objects.Single(o => o.ObjectId == Player).ModuleHealth;
        var random = engine.CaptureLaunchScatterState();
        var snapshot = At(engine, 1);
        Assert.DoesNotContain(snapshot.Objects, o => o.ObjectType is SpaceObjectType.Missile or SpaceObjectType.Countermeasure);
        var blast = Assert.Single(snapshot.CombatJournal, e => e.Type == CombatEventType.CountermissileDetonated);
        Assert.Equal("countermeasure-3", blast.ProjectileObjectId);
        Assert.Equal((0d, 0d), (blast.X, blast.Y));
        var victims = new[] { "countermeasure-1", "countermeasure-2", "torpedo-1", "torpedo-2" };
        Assert.Equal(victims, blast.DestroyedProjectileIds!.Value);
        var shotDown = snapshot.CombatJournal.Where(e => e.Type == CombatEventType.ShotDown).ToArray();
        Assert.Equal(victims, shotDown.Select(e => e.ProjectileObjectId));
        Assert.All(shotDown, e => { Assert.Equal("countermeasure-3", e.Result); Assert.Equal(ProjectileHitResult.ShotDown, e.HitResult); Assert.Null(e.Damage); });
        Assert.Equal(JsonSerializer.Serialize(ownHealth), JsonSerializer.Serialize(snapshot.Objects.Single(o => o.ObjectId == Player).ModuleHealth));
        Assert.DoesNotContain(snapshot.CombatJournal, e => e.Type is CombatEventType.TorpedoDetonated or CombatEventType.Hit or CombatEventType.ModuleHit or CombatEventType.ShipDestroyed);
        Assert.Equal(random, engine.CaptureLaunchScatterState());
        foreach (string id in new[] { Defense, Defense2 })
        {
            var defense = snapshot.InstalledModules.Single(m => m.ModuleId == id).Defense!;
            Assert.Empty(defense.ActiveProjectileIds!.Value);
            Assert.Null(defense.ActiveProjectileId);
            Assert.Equal(12500, defense.ReadyAtMotionTimeMs);
            Assert.Equal(DefenseState.Reloading, defense.State);
        }
        Assert.Empty(snapshot.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ActiveTorpedoIds);
        var saved = engine.CaptureSaveStateForTests(1, SimulationSpeed.Speed0);
        Assert.Equal(0UL, saved.GameState.CombatState!.HitLocationRngCounter);
        Assert.Equal(0UL, saved.GameState.DefenseState!.RngCounter);
        Assert.Single(At(engine, 3000).CombatJournal, e => e.Type == CombatEventType.CountermissileDetonated);
    }

    [Theory]
    [InlineData(10, true)]
    [InlineData(10.000001, false)]
    public void Same_time_countermissile_prevents_torpedo_blast(double offsetWorld, bool intercepted)
    {
        using var engine = CreateCounter(o => o.ObjectId != Target ? o : o with
        {
            PositionX = -10,
            PositionY = offsetWorld,
            Modules = o.Modules!.Select(m => m.ModuleId == ForeignTorpedo ? m with { PowerState = "On" } : m).ToArray()
        }, scatter: 0, torpedoScatter: 0);
        // Both deadlines are exactly 1000ms: torpedo covers16km at16km/s; counter covers15km at15km/s.
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, ForeignTorpedo, 150, offsetWorld, "fast", 0).Disposition);
        engine.ReceiveCommand(CounterLaunch(x: 150));
        var start = At(engine);
        Assert.Equal(1000, Flight(start).OwnProjectile!.DetonationMotionTimeMs);
        Assert.Equal(2, At(engine, 999).Objects.Count(o => o.ObjectType is SpaceObjectType.Missile or SpaceObjectType.Countermeasure));
        var snapshot = At(engine, 1000);
        var counter = Assert.Single(snapshot.CombatJournal, e => e.Type == CombatEventType.CountermissileDetonated);
        Assert.Equal(1000, counter.MotionTimeMs);
        Assert.Equal(150, counter.X);
        Assert.Equal(intercepted ? 1 : 0, counter.DestroyedProjectileIds!.Value.Length);
        if (intercepted)
        {
            Assert.Equal("torpedo-1", Assert.Single(counter.DestroyedProjectileIds.Value));
            Assert.Single(snapshot.CombatJournal, e => e.Type == CombatEventType.ShotDown);
            Assert.DoesNotContain(snapshot.CombatJournal, e => e.Type == CombatEventType.TorpedoDetonated);
        }
        else
        {
            var torpedo = Assert.Single(snapshot.CombatJournal, e => e.Type == CombatEventType.TorpedoDetonated);
            Assert.Equal(1000, torpedo.MotionTimeMs);
            Assert.True(counter.EventId < torpedo.EventId);
        }
        Assert.DoesNotContain(snapshot.CombatJournal, e => e.Type == CombatEventType.Hit);
    }

    [Fact]
    public void Earlier_ordinal_counter_destroys_another_due_counter_without_chain_blast()
    {
        using var engine = CreateCounter(o => o.ObjectId != Target ? o : o with
        {
            PositionX = 0,
            PositionY = 0,
            Modules = o.Modules!.Select(m => m.ModuleId == ForeignCounter ? m with { PowerState = "On" } : m).ToArray()
        }, scatter: 0, torpedoScatter: 0);
        engine.ReceiveCommand(CounterLaunch(x: 150)); At(engine);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, ForeignCounter, 150, 0, "wide", 0, ProjectileKind.Countermissile).Disposition);
        var snapshot = At(engine, 1000);
        var blast = Assert.Single(snapshot.CombatJournal, e => e.Type == CombatEventType.CountermissileDetonated);
        Assert.Equal("countermeasure-1", blast.ProjectileObjectId);
        Assert.Equal("countermeasure-2", Assert.Single(blast.DestroyedProjectileIds!.Value));
        Assert.Single(snapshot.CombatJournal, e => e.Type == CombatEventType.ShotDown);
    }

    [Fact]
    public void Counter_blast_partitioning_preserves_all_facts_and_rng_states()
    {
        using var whole = Cluster();
        using var split = Cluster();
        var expected = At(whole, 3000);
        foreach (long time in new long[] { 1, 7, 101, 999, 1000, 1001, 2000, 2999 }) At(split, time);
        var actual = At(split, 3000);
        Assert.Equal(JsonSerializer.Serialize(expected.Objects), JsonSerializer.Serialize(actual.Objects));
        Assert.Equal(JsonSerializer.Serialize(expected.CombatJournal), JsonSerializer.Serialize(actual.CombatJournal));
        Assert.Equal(whole.CaptureLaunchScatterState(), split.CaptureLaunchScatterState());
        Assert.Equal(JsonSerializer.Serialize(whole.CaptureSaveStateForTests(3000, SimulationSpeed.Speed0).GameState.CombatState),
            JsonSerializer.Serialize(split.CaptureSaveStateForTests(3000, SimulationSpeed.Speed0).GameState.CombatState));
    }
}
