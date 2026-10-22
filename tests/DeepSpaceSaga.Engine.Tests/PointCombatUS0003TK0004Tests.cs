using System.Text.Json;
using DeepSpaceSaga.Contracts;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0003TK0004Tests
{
    private static AuthoritativeSnapshot At(SimulationEngine engine, long time) => engine.CaptureSnapshotForTests(time * 4, simulationTimeMs: time);
    private static PlayerCommand Detonate(string projectileId, string id = "detonate") =>
        new(id, 2, Player, Launcher, CombatCommandTypes.Detonate, projectileId);

    [Fact]
    public void Manual_detonation_uses_current_position()
    {
        using var engine = Create(o => o.ObjectId == Target ? o with { PositionX = 160, PositionY = 0 } : o, scatter: 0);
        engine.ReceiveCommand(Launch(profile: "fast"));
        string id = Flight(At(engine, 0)).ObjectId;
        var before = At(engine, 1000);
        Assert.Equal(160, Flight(before).X);
        var random = engine.CaptureLaunchScatterState();
        double? ready = before.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ReadyAtMotionTimeMs;
        var command = Detonate(id);
        engine.ReceiveCommand(command);
        var after = At(engine, 1000);
        Assert.DoesNotContain(after.Objects, o => o.OwnProjectile is not null);
        var blast = Assert.Single(after.CombatJournal, e => e.Type == CombatEventType.ManuallyDetonated);
        Assert.Equal((160d, 0d), (blast.X, blast.Y));
        Assert.Equal(1000, blast.MotionTimeMs);
        Assert.Equal(ProjectileKind.Torpedo, blast.ProjectileKind);
        Assert.Equal(2.5, blast.BlastRadiusKm);
        Assert.DoesNotContain(after.CombatJournal, e => e.Type == CombatEventType.TorpedoDetonated);
        Assert.Single(after.CombatJournal, e => e.Type == CombatEventType.Hit && e.TargetObjectId == Target);
        Assert.DoesNotContain(after.CombatJournal, e => e.Type == CombatEventType.Hit && e.TargetObjectId == Player);
        Assert.Empty(after.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ActiveTorpedoIds);
        Assert.Equal(ready, after.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ReadyAtMotionTimeMs);
        Assert.Equal(random, engine.CaptureLaunchScatterState());
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(after.CommandResults).Status);
        engine.ReceiveCommand(command);
        var replay = At(engine, 1000);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(replay.CommandResults).Status);
        Assert.Equal(JsonSerializer.Serialize(after.CombatJournal), JsonSerializer.Serialize(replay.CombatJournal));
        Assert.Equal(random, engine.CaptureLaunchScatterState());
    }

    [Fact]
    public void Foreign_or_finished_projectile_rejected_without_effect()
    {
        using var engine = Create(o => o.ObjectId != Target ? o : o with
        {
            Modules = o.Modules!.Select(m => m.ModuleId == "MOD-PIRATE-TORPEDO-01" ? m with { PowerState = "On" } : m).ToArray()
        }, scatter: 0);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, "MOD-PIRATE-TORPEDO-01", 700, -100, "fast", 0).Disposition);
        var before = At(engine, 0);
        string foreign = Assert.Single(before.Objects, o => o.ObservedProjectile is not null).ObjectId;
        var random = engine.CaptureLaunchScatterState();
        engine.ReceiveCommand(Detonate(foreign));
        var after = At(engine, 0);
        Assert.Equal(CommandReasonCodes.NotOwnProjectile, Assert.Single(after.CommandResults).ReasonCode);
        Assert.Equal(JsonSerializer.Serialize(before.Objects), JsonSerializer.Serialize(after.Objects));
        Assert.Equal(JsonSerializer.Serialize(before.CombatJournal), JsonSerializer.Serialize(after.CombatJournal));
        Assert.Equal(random, engine.CaptureLaunchScatterState());
        At(engine, 5000);
        engine.ReceiveCommand(Detonate(foreign, "stale"));
        var stale = At(engine, 5000);
        Assert.Equal(CommandReasonCodes.StaleProjectile, Assert.Single(stale.CommandResults).ReasonCode);
        Assert.DoesNotContain(stale.CombatJournal, e => e.Type == CombatEventType.ManuallyDetonated);
    }

    [Fact]
    public void Wrong_launcher_and_missing_target_do_not_mutate_own_flight()
    {
        using var engine = Create(scatter: 0);
        engine.ReceiveCommand(Launch());
        var before = At(engine, 0);
        string id = Flight(before).ObjectId;
        engine.ReceiveCommand(Detonate(id) with { ModuleId = "other-launcher" });
        var wrong = At(engine, 0);
        Assert.Equal(CommandReasonCodes.NotOwnProjectile, Assert.Single(wrong.CommandResults).ReasonCode);
        Assert.Equal(Flight(before), Flight(wrong));
        engine.ReceiveCommand(Detonate(id, "missing") with { TargetObjectId = null });
        Assert.Equal(CommandReasonCodes.StaleProjectile, Assert.Single(At(engine, 0).CommandResults).ReasonCode);
        Assert.Equal(Flight(before), Flight(At(engine, 0)));
    }

    [Fact]
    public void Manual_self_damage_can_defeat_player_and_snapshot_confirms_pause_immediately()
    {
        using var engine = Create(o =>
        {
            if (o.ObjectId != Player) return o;
            var fragile = TorpedoImpactTests.FragileForOneHit(o);
            return fragile with { Modules = fragile.Modules!.Select(m => m with { StructurePoints = m.Destroyed ? 0 : 1 }).ToArray() };
        }, scatter: 0);
        engine.ReceiveCommand(Launch(profile: "fast"));
        var launched = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1, 0);
        engine.ReceiveCommand(Detonate(Flight(launched).ObjectId));
        var snapshot = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1, 0);
        Assert.Equal(SessionOutcomeState.Defeated, snapshot.EffectiveOutcome.State);
        Assert.Equal(SimulationSpeed.Speed0, snapshot.CurrentSpeed);
        Assert.Equal(0, snapshot.EffectiveOutcome.DeathMotionTimeMs);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(snapshot.CommandResults).Status);
        Assert.Single(snapshot.CombatJournal, e => e.Type == CombatEventType.ManuallyDetonated);
        Assert.False(snapshot.EffectiveOutcome.CanSave);
    }
}
