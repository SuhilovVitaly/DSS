using System.Text.Json;
using Point = DeepSpaceSaga.Engine.Tests.PointCounterFixture;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public class ManualCountermeasureFireTests
{
    internal const string Defense1 = "MOD-PLAYER-COUNTERMEASURE-01", Defense2 = "MOD-PLAYER-COUNTERMEASURE-02";
    [Theory]
    [InlineData("defense.enable")]
    [InlineData("defense.disable")]
    public void Paused_area_launch_uses_explicit_profile_and_ignores_retired_toggle_requests(string retired)
    {
        using var engine = Point.Create(); engine.ReceiveCommand(new("toggle", 1, Point.Player, Defense1, retired)); Point.At(engine);
        engine.ReceiveCommand(Point.Shot()); var state = Point.At(engine); var flight = Point.Flight(state);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(state.CommandResults).Status);
        Assert.Equal(ProjectileKind.Countermissile, flight.Kind); Assert.Equal("wide", flight.ProfileId);
        Assert.Equal(15, flight.SpeedKmS); Assert.Equal(1, flight.BlastRadiusKm); Assert.Equal(0, state.MotionTimeMs);
        Assert.Equal(DefenseState.Reloading, state.InstalledModules.Single(m => m.ModuleId == Defense1).Defense!.State);
        Assert.False(state.InstalledModules.Single(m => m.ModuleId == Defense1).Defense!.AutoEnabled);
        Assert.Null(state.Objects.Single(o => o.OwnProjectile is not null).Countermeasure);
        Assert.Equal(2UL, engine.CaptureLaunchScatterState().Counter);
    }

    [Fact]
    public void Independent_modules_and_duplicate_ids_spawn_once_and_reloading_rejection_draws_nothing()
    {
        using var engine = Point.Create(); engine.ReceiveCommand(Point.Shot("first")); Point.At(engine);
        engine.ReceiveCommand(Point.Shot("first", Defense2)); engine.ReceiveCommand(Point.Shot("second", Defense2, y: -30));
        var state = Point.At(engine); Assert.Equal(2, state.Objects.Count(o => o.OwnProjectile is not null));
        Assert.Equal(2, state.Objects.Where(o => o.OwnProjectile is not null).Select(o => o.OwnProjectile!.LauncherModuleId).Distinct().Count());
        var before = engine.CaptureLaunchScatterState(); engine.ReceiveCommand(Point.Shot("busy"));
        var result = Assert.Single(Point.At(engine).CommandResults); Assert.Equal(CommandReasonCodes.LauncherReloading, result.ReasonCode);
        Assert.Equal(before, engine.CaptureLaunchScatterState()); Assert.Equal(4UL, before.Counter);
    }

    [Fact]
    public void Empty_space_requires_no_projectile_target_or_intercept_solution()
    {
        using var engine = Point.Create(); engine.ReceiveCommand(Point.Shot(x: 125, y: 25)); var state = Point.At(engine);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(state.CommandResults).Status);
        var flight = Point.Flight(state); Assert.Equal((125d, 25d), (flight.DetonationX, flight.DetonationY));
        Assert.Equal(double.Hypot(125, 25) / 150 * 1000, flight.DetonationMotionTimeMs, 8);
        Assert.Empty(Assert.Single(state.CombatJournal).TargetObjectId);
    }

    [Theory]
    [InlineData("missing-profile", CommandReasonCodes.MissingProfile)]
    [InlineData("profile", CommandReasonCodes.UnknownProfile)]
    [InlineData("coordinates", CommandReasonCodes.InvalidTargetCoordinates)]
    [InlineData("range", CommandReasonCodes.LaunchOutOfRange)]
    [InlineData("owner", CommandReasonCodes.UnknownObject)]
    [InlineData("module", CommandReasonCodes.UnknownCommandType)]
    [InlineData("off", CommandReasonCodes.ModuleUnavailable)]
    [InlineData("destroyed", CommandReasonCodes.ModuleDestroyed)]
    [InlineData("operator", CommandReasonCodes.NoWeaponOperator)]
    public void Invalid_point_request_has_no_spawn_reload_sequence_or_random_side_effect(string defect, string expected)
    {
        using var engine = Point.Create(configure: o => o.ObjectId != Point.Player ? o : o with
        {
            Modules = o.Modules!.Select(m => m.ModuleId != Defense1 ? m : m with
            {
                PowerState = defect == "off" ? "Off" : m.PowerState,
                Destroyed = defect == "destroyed" || m.Destroyed,
                StructurePoints = defect == "destroyed" ? 0 : m.StructurePoints,
                OperatorCrewId = defect == "operator" ? null : m.OperatorCrewId
            }).ToArray()
        });
        var before = engine.CaptureLaunchScatterState();
        var command = Point.Shot(profile: defect == "missing-profile" ? null : defect == "profile" ? "absent" : "wide",
            x: defect == "coordinates" ? double.NaN : defect == "range" ? 300.000001 : 150);
        if (defect == "owner") command = command with { ObjectId = Point.Enemy };
        if (defect == "module") command = command with { ModuleId = Point.Torpedo };
        engine.ReceiveCommand(command); var state = Point.At(engine); var result = Assert.Single(state.CommandResults);
        Assert.Equal(DefenseCommandTypes.Launch, result.CommandType); Assert.Equal(CommandResultStatus.Rejected, result.Status); Assert.Equal(expected, result.ReasonCode);
        Assert.Equal(before, engine.CaptureLaunchScatterState()); Assert.DoesNotContain(state.Objects, o => o.OwnProjectile is not null);
        Assert.Equal(0, Point.Save(engine).GameState.DefenseState!.ProjectileSequence);
    }

    [Theory]
    [InlineData(PlayerRelation.Friend)]
    [InlineData(PlayerRelation.Neutral)]
    public void Counter_area_can_be_aimed_at_any_faction_without_hull_damage_or_target_lock(string relation)
    {
        using var engine = Point.Create(configure: o => o.ObjectId != Point.Enemy ? o : o with { RelationToPlayer = relation });
        engine.ReceiveCommand(Point.Shot(x: 200, y: 0)); var launched = Point.At(engine); var health = launched.Objects.Single(o => o.ObjectId == Point.Player).ModuleHealth;
        var ended = Point.At(engine, 2000); Assert.Equal(CommandResultStatus.Executed, Assert.Single(launched.CommandResults).Status);
        Assert.Contains(ended.Objects, o => o.ObjectId == Point.Enemy); Assert.DoesNotContain(ended.CombatJournal, e => e.Type is CombatEventType.Hit or CombatEventType.ModuleHit);
        Assert.Equal(JsonSerializer.Serialize(health), JsonSerializer.Serialize(ended.Objects.Single(o => o.ObjectId == Point.Player).ModuleHealth));
    }

    [Theory]
    [InlineData(0d, true)]
    [InlineData(.000001d, false)]
    public void Point_launch_range_is_inclusive_at_thirty_kilometres(double offset, bool allowed)
    {
        using var engine = Point.Create(); engine.ReceiveCommand(Point.Shot(x: 300 + offset, y: 0)); var state = Point.At(engine);
        Assert.Equal(allowed ? 1 : 0, state.Objects.Count(o => o.OwnProjectile is not null));
        Assert.Equal(allowed ? CommandResultStatus.Executed : CommandResultStatus.Rejected, Assert.Single(state.CommandResults).Status);
        if (allowed) Assert.Equal(2000, Point.Flight(state).DetonationMotionTimeMs); else Assert.Equal(CommandReasonCodes.LaunchOutOfRange, Assert.Single(state.CommandResults).ReasonCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Durable_point_identity_survives_receipt_eviction_payload_change_and_file_roundtrip(bool differentFamily)
    {
        using var engine = Point.Create(); engine.ReceiveCommand(Point.Shot("original")); var first = Point.Flight(Point.At(engine));
        for (int i = 0; i <= SimulationEngine.CommandReceiptLimit; i++) engine.ReceiveCommand(new("invalid-" + i, (ulong)i + 2, Point.Player, "absent", "invalid"));
        Point.At(engine); var save = Point.Save(engine); Assert.DoesNotContain(save.GameState.CommandReceipts!, r => r.CommandId == "original");
        using var restored = new SimulationEngine(Point.Registry()); restored.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true), true);
        var before = restored.CaptureLaunchScatterState();
        restored.ReceiveCommand(differentFamily ? new("original", 1, Point.Player, Point.Torpedo, CombatCommandTypes.Launch,
            TargetWorldX: 250, TargetWorldY: -30, ProfileId: "fast") : Point.Shot("original", Defense2, 250, -30, "fast"));
        var after = Point.At(restored); Assert.Single(after.Objects, o => o.OwnProjectile is not null); Assert.Equal(first, Point.Flight(after));
        Assert.Equal(before, restored.CaptureLaunchScatterState()); Assert.Equal(DefenseState.Ready, after.InstalledModules.Single(m => m.ModuleId == Defense2).Defense!.State);
    }

    [Fact]
    public void New_manual_launch_is_possible_after_fixed_reload_without_automatic_retry()
    {
        using var engine = Point.Create(); engine.ReceiveCommand(Point.Shot("first")); Point.At(engine);
        var ready = Point.At(engine, 12500); Assert.Single(ready.CombatJournal, e => e.Type == CombatEventType.Launch);
        Assert.Equal(DefenseState.Ready, ready.InstalledModules.Single(m => m.ModuleId == Defense1).Defense!.State);
        engine.ReceiveCommand(Point.Shot("second", profile: "fast")); var state = Point.At(engine, 12500);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(state.CommandResults).Status); Assert.Equal("fast", Point.Flight(state).ProfileId);
        Assert.Equal(25000, state.InstalledModules.Single(m => m.ModuleId == Defense1).Defense!.ReadyAtMotionTimeMs);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    public void All_legal_operator_skills_draw_only_the_two_launch_scatter_values_and_no_intercept_roll(int skill)
    {
        using var engine = Point.Create(configure: o => o.ObjectId != Point.Player ? o : o with { Crew = o.Crew!.Select(c => c with { AccuracySkill = skill }).ToArray() });
        engine.ReceiveCommand(Point.Shot()); var launched = Point.At(engine);
        Assert.Equal(skill, launched.InstalledModules.Single(m => m.ModuleId == Point.Counter).Defense!.Operator!.Skill);
        var state = Point.At(engine, 3000);
        Assert.Equal(2UL, engine.CaptureLaunchScatterState().Counter); Assert.Equal(0UL, Point.Save(engine, 3000).GameState.DefenseState!.RngCounter);
        Assert.DoesNotContain(state.CombatJournal, e => e.ChanceTenths is not null || e.Roll is not null);
    }

    [Fact]
    public void Real_counter_area_removes_friendly_and_enemy_point_torpedoes_at_the_same_crossing()
    {
        using var engine = Point.Create(enemyArmed: true);
        engine.ReceiveCommand(new("own", 1, Point.Player, Point.Torpedo, CombatCommandTypes.Launch, TargetWorldX: 200, TargetWorldY: 0, ProfileId: "slow"));
        Point.At(engine); Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Point.Enemy, Point.EnemyTorpedo, 0, 0, "slow", 0).Disposition);
        Point.At(engine, 1000); engine.ReceiveCommand(Point.Shot(x: 100, y: 0)); Point.At(engine, 1000);
        var state = Point.At(engine, 1700); var blast = Assert.Single(state.CombatJournal, e => e.Type == CombatEventType.CountermissileDetonated);
        Assert.Equal(new[] { "torpedo-1", "torpedo-2" }, blast.DestroyedProjectileIds!.Value);
        Assert.Equal(2, state.CombatJournal.Count(e => e.Type == CombatEventType.ShotDown));
        Assert.DoesNotContain(state.CombatJournal, e => e.Type is CombatEventType.TorpedoDetonated or CombatEventType.ModuleHit);
    }

    [Fact]
    public void Orphaned_foreign_point_flight_survives_owner_death_and_save_then_can_be_shot_down()
    {
        using var engine = Point.Create(enemyArmed: true, configure: o => o.ObjectId != Point.Enemy ? o : o with
        {
            Modules = o.Modules!.Where(m => m.ModuleId == Point.EnemyTorpedo || m.ModuleTypeId.StartsWith("module.engine.", StringComparison.Ordinal)).Select(m =>
            m with { StructurePoints = 1, Armor = 0, ArmorClass = "none" }).ToArray()
        });
        engine.ReceiveCommand(new("kill-owner", 1, Point.Player, Point.Torpedo, CombatCommandTypes.Launch, TargetWorldX: 200, TargetWorldY: 0, ProfileId: "fast")); Point.At(engine);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Point.Enemy, Point.EnemyTorpedo, -300, 0, "slow", 0).Disposition);
        var orphaned = Point.At(engine, 1400); Assert.DoesNotContain(orphaned.Objects, o => o.ObjectId == Point.Enemy);
        Assert.Contains(orphaned.Objects, o => o.ObjectId == "torpedo-2" && o.ObservedProjectile is not null);
        var save = Point.Save(engine, 1400); using var restored = new SimulationEngine(Point.Registry()); restored.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true), true);
        double x = 116 / 1.24; restored.ReceiveCommand(Point.Shot("orphan", x: x, y: 0, profile: "fast")); Point.At(restored, 1400);
        var state = Point.At(restored, 1800); Assert.Contains(state.CombatJournal, e => e.Type == CombatEventType.ShotDown && e.ProjectileObjectId == "torpedo-2");
        Assert.DoesNotContain(state.Objects, o => o.ObjectId == "torpedo-2");
    }

    [Fact]
    public void Point_counter_roundtrips_midflight_without_duplicate_spawn_and_keeps_exact_fuse()
    {
        using var engine = Point.Create(); engine.ReceiveCommand(Point.Shot("saved")); Point.At(engine);
        var save = Point.Save(engine, 101); using var restored = new SimulationEngine(Point.Registry());
        restored.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true), true);
        restored.ReceiveCommand(Point.Shot("saved", Defense2, 250, 25, "fast")); Point.At(restored, 101);
        var expected = Point.At(engine, 1000); var actual = Point.At(restored, 1000);
        Assert.Equal(JsonSerializer.Serialize(expected.Objects), JsonSerializer.Serialize(actual.Objects));
        Assert.Single(actual.Objects, o => o.OwnProjectile is not null); Assert.Equal(engine.CaptureLaunchScatterState(), restored.CaptureLaunchScatterState());
    }
}
