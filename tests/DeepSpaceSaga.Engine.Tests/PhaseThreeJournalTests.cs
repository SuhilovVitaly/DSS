using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using Point = DeepSpaceSaga.Engine.Tests.PointCounterFixture;
namespace DeepSpaceSaga.Engine.Tests;

public class PhaseThreeJournalTests
{
    [Fact]
    public void Explicit_area_launch_and_detonation_record_geometry_without_probability()
    {
        using var engine = Point.Create(); engine.ReceiveCommand(Point.Shot(x: 150, y: 0));
        var launch = Assert.Single(Point.At(engine).CombatJournal);
        Assert.Equal(CombatEventType.Launch, launch.Type); Assert.Equal(ProjectileKind.Countermissile, launch.ProjectileKind);
        Assert.Equal("wide", launch.ProfileId); Assert.Equal(1, launch.BlastRadiusKm);
        var snapshot = Point.At(engine, 1000); Assert.Equal(new[] { CombatEventType.Launch, CombatEventType.CountermissileDetonated, CombatEventType.Miss }, snapshot.CombatJournal.Select(e => e.Type));
        var blast = Assert.Single(snapshot.CombatJournal, e => e.Type == CombatEventType.CountermissileDetonated);
        Assert.Equal(150, blast.X); Assert.Equal(0, blast.Y); Assert.Equal(1000, blast.MotionTimeMs);
        Assert.All(snapshot.CombatJournal, e => { Assert.Null(e.ChanceTenths); Assert.Null(e.Roll); Assert.Null(e.RatingBreakdown); Assert.Null(e.LaunchMode); Assert.Null(e.TerminationKind); });
        Assert.Equal(ProjectileHitResult.Miss, Assert.Single(snapshot.CombatJournal, e => e.Type == CombatEventType.Miss).HitResult);
        Assert.DoesNotContain(snapshot.CombatJournal, e => e.Type is CombatEventType.Intercept or CombatEventType.Expired or CombatEventType.TargetLost);
        Assert.Equal(snapshot.CombatJournal.ToArray(), Point.At(engine, 1000).CombatJournal.ToArray());
    }

    [Fact]
    public void Detonation_journal_roundtrips_without_duplicate_facts_or_extra_rng()
    {
        using var engine = Point.Create(); engine.ReceiveCommand(Point.Shot(x: 101, y: 0));
        var before = Point.At(engine, 674); var save = Point.Save(engine, 674);
        using var resumed = new SimulationEngine(Point.Registry());
        resumed.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, isSave: true), true);
        Assert.Equal(JsonSerializer.Serialize(before.CombatJournal), JsonSerializer.Serialize(Point.At(resumed, 674).CombatJournal));
        Assert.Single(Point.At(resumed, 12600).CombatJournal, e => e.Type == CombatEventType.CountermissileDetonated);
        Assert.Equal(0ul, Point.Save(resumed, 12600).GameState.DefenseState!.RngCounter);
    }

    [Fact]
    public void Concurrent_areas_remove_other_projectile_without_chain_detonation()
    {
        using var engine = Point.Create(); engine.ReceiveCommand(Point.Shot("one", x: 150, y: 0));
        engine.ReceiveCommand(Point.Shot("two", Point.Counter2, x: 150, y: 0)); Point.At(engine);
        var snapshot = Point.At(engine, 1000);
        var blast = Assert.Single(snapshot.CombatJournal, e => e.Type == CombatEventType.CountermissileDetonated);
        var shot = Assert.Single(snapshot.CombatJournal, e => e.Type == CombatEventType.ShotDown);
        Assert.True(blast.EventId < shot.EventId); Assert.Equal(blast.MotionTimeMs, shot.MotionTimeMs);
        Assert.Equal(shot.ProjectileObjectId, Assert.Single(blast.DestroyedProjectileIds!.Value));
        Assert.Equal(ProjectileHitResult.ShotDown, shot.HitResult);
        Assert.DoesNotContain(snapshot.Objects, o => o.OwnProjectile is not null);
        Assert.Empty(snapshot.CombatImpacts); Assert.All(snapshot.CombatJournal, e => Assert.Null(e.Roll));
    }
}
