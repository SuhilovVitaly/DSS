using System.Text.Json;
using DeepSpaceSaga.Contracts;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class DebugTuningProjectileTests
{
    private static void Tune(SimulationEngine engine, string type, string field, double value, string profile = "medium") =>
        engine.ReceiveCommand(new("tune-flight", 2, "", "", DebugTuningCommandTypes.Apply,
            DebugTuning: new([new(type, field, value, profile)])));

    [Fact]
    public void Live_medium_flight_keeps_endpoint_and_retimes_remaining_twenty_km_to_one_second()
    {
        using var engine = Create(o => o with { PositionX = o.ObjectId == Player ? 0 : 10000, PositionY = 0 }, scatter: 0);
        engine.ReceiveCommand(Launch(x: 400, y: 0, profile: "medium"));
        var initial = Flight(PointCombatUS0018TK0002Tests.At(engine));
        var before = Flight(PointCombatUS0018TK0002Tests.At(engine, 2000));
        Assert.Equal(200, before.X, 8);
        var rng = engine.CaptureLaunchScatterState();
        Tune(engine, "module.torpedo.launcher.basic", "speedKmS", 20);
        var changed = Flight(PointCombatUS0018TK0002Tests.At(engine, 2000));
        Assert.Equal(initial.ObjectId, changed.ObjectId);
        Assert.Equal(before.X, changed.X, 9); Assert.Equal(before.Y, changed.Y, 9);
        Assert.Equal(400, changed.OwnProjectile!.DetonationX);
        Assert.Equal(3000, changed.OwnProjectile.DetonationMotionTimeMs, 8);
        Assert.Equal(rng, engine.CaptureLaunchScatterState());
        var end = PointCombatUS0018TK0002Tests.At(engine, 3000);
        Assert.DoesNotContain(end.Objects, o => o.ObjectId == initial.ObjectId);
        var blast = Assert.Single(end.CombatJournal, e => e.Type == CombatEventType.TorpedoDetonated);
        Assert.Equal(3000, blast.MotionTimeMs); Assert.Equal(400, blast.X, 8);
        Assert.Equal(0, Assert.Single(end.CombatJournal, e => e.Type == CombatEventType.Launch).MotionTimeMs);
    }

    [Fact]
    public void Foreign_radius_tuning_adds_edge_damage_without_disclosing_endpoint_or_deadline()
    {
        using var engine = Create(o => o with
        {
            PositionX = o.ObjectId == Player ? 0 : -300,
            PositionY = o.ObjectId == Player ? 100 : 0,
            Modules = o.Modules!.Select(m => m with
            {
                PowerState = m.ModuleTypeId.Contains("torpedo", StringComparison.Ordinal) ||
                o.ObjectId == Player ? "On" : "Off"
            }).ToArray()
        }, scatter: 0);
        var npc = engine.CaptureSaveState().GameState.SpaceObjects.Single(o => o.ObjectId == Target);
        var launcher = npc.Modules!.First(m => m.ModuleTypeId.Contains("torpedo", StringComparison.Ordinal));
        Assert.Equal(CommandStartDisposition.Started,
            engine.TryStartHostilePointLaunch(Target, launcher.ModuleId, 0, 0, "slow", 0).Disposition);
        PointCombatUS0018TK0002Tests.At(engine, 2000);
        Tune(engine, launcher.ModuleTypeId, "blastRadiusKm", 12, "slow");
        var state = PointCombatUS0018TK0002Tests.At(engine, 2000);
        var observed = Assert.Single(state.Objects, o => o.ObservedProjectile is not null);
        Assert.Null(observed.OwnProjectile); Assert.Equal(12, observed.ObservedProjectile!.BlastRadiusKm);
        string json = JsonSerializer.Serialize(observed.ObservedProjectile);
        Assert.DoesNotContain("Detonation", json); Assert.DoesNotContain("Launch", json); Assert.DoesNotContain("Start", json);
        var end = PointCombatUS0018TK0002Tests.At(engine, 5000);
        var hit = Assert.Single(end.CombatJournal, e => e.Type == CombatEventType.Hit && e.TargetObjectId == Player);
        Assert.Equal(48, hit.Damage); Assert.Equal(ProjectileHitResult.Edge, hit.HitResult);
    }

    [Fact]
    public void Retimed_simultaneous_countermissile_resolves_before_torpedo_and_keeps_launch_identities()
    {
        using var engine = PointCombatUS0004TK0001Tests.CreateCounter(o => o with
        { PositionX = o.ObjectId == Player ? 0 : 10000, PositionY = 0 }, scatter: 0, torpedoScatter: 0);
        engine.ReceiveCommand(Launch(x: 400, y: 0, profile: "medium"));
        engine.ReceiveCommand(new("cm", 2, Player, PointCombatUS0004TK0001Tests.Defense, DefenseCommandTypes.Launch,
            TargetWorldX: 300, TargetWorldY: 0, ProfileId: "wide"));
        var first = PointCombatUS0018TK0002Tests.At(engine);
        string[] ids = first.Objects.Where(o => o.OwnProjectile is not null).Select(o => o.ObjectId).Order().ToArray();
        Assert.Equal(2, ids.Length);
        PointCombatUS0018TK0002Tests.At(engine, 1000);
        var rng = engine.CaptureLaunchScatterState();
        engine.ReceiveCommand(new("simultaneous", 3, "", "", DebugTuningCommandTypes.Apply, DebugTuning: new([
            new("module.torpedo.launcher.basic", "speedKmS", 20, "medium"),
            new("module.countermeasure.launcher.basic", "speedKmS", 10, "wide"),
            new("module.countermeasure.launcher.basic", "blastRadiusKm", 12, "wide")])));
        var updated = PointCombatUS0018TK0002Tests.At(engine, 1000);
        Assert.Equal(ids, updated.Objects.Where(o => o.OwnProjectile is not null).Select(o => o.ObjectId).Order().ToArray());
        Assert.All(updated.Objects.Where(o => o.OwnProjectile is not null), o => Assert.Equal(2500, o.OwnProjectile!.DetonationMotionTimeMs));
        Assert.Equal(rng, engine.CaptureLaunchScatterState());
        var end = PointCombatUS0018TK0002Tests.At(engine, 2500);
        Assert.Single(end.CombatJournal, e => e.Type == CombatEventType.CountermissileDetonated);
        Assert.Single(end.CombatJournal, e => e.Type == CombatEventType.ShotDown);
        Assert.DoesNotContain(end.CombatJournal, e => e.Type == CombatEventType.TorpedoDetonated);
        Assert.DoesNotContain(end.Objects, o => o.OwnProjectile is not null);
    }
}
