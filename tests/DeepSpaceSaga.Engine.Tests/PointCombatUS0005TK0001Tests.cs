using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0004TK0001Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0005TK0001Tests
{
    private const string ForeignTorpedo = "PRIVATE-MOD-PIRATE-TORPEDO-01", ForeignCounter = "PRIVATE-MOD-PIRATE-COUNTERMEASURE-01";
    private sealed class Time { internal long Real; internal long Sequence; }
    private static IEnumerable<T> All<T>(TypeRegistry<T> registry) where T : ITypeDefinition => Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);
    private static SimulationEngine Scene(Time time, bool ai = false, bool engineOnly = false)
    {
        var source = CounterRegistry(scatter: 0, torpedoScatter: 0);
        var registry = GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes), All(source.ItemTypes),
            All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes), All(source.Dialogues), All(source.Quests),
            stationMarketProfiles: All(source.StationMarketProfiles), shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents),
            combatRules: source.CombatRules! with { Damage = source.CombatRules.Damage with { HitDisableMs = 8000 } });
        var scenario = TorpedoImpactTests.Scenario(o => o.ObjectId != Target ? o with { IsKnown = true } : o with
        {
            IsKnown = true,
            PositionX = 400,
            PositionY = 0,
            DirectionDegrees = 270,
            Ai = ai ? new(25, 8, 35) : null,
            Modules = o.Modules!.Where(m => !engineOnly || m.ModuleTypeId == "module.engine.basic").Select(m => m with
            {
                ModuleId = "PRIVATE-" + m.ModuleId,
                StructurePoints = engineOnly ? 150 : m.StructurePoints,
                ArmorClass = engineOnly ? "light" : m.ArmorClass,
                Armor = engineOnly ? 60 : m.Armor,
                PowerState = m.ModuleTypeId == "module.engine.basic" ? ai ? "On" : "Off" : "On"
            }).ToArray()
        });
        var engine = new SimulationEngine(registry, clock: new SimulationClock(SimulationSpeed.Speed0, () => time.Real));
        engine.LoadScenario(scenario);
        engine.SetPresentationTimestampForTests(() => time.Real * Stopwatch.Frequency / 1000);
        engine.EnablePresentationLease(); engine.SetSpeed(SimulationSpeed.Speed1);
        return engine;
    }

    private static CombatPresentationUpdate Capture(SimulationEngine engine, Time time, long real = 0)
    {
        time.Real = real;
        if (time.Sequence > 0) engine.ReportPresentationProgress(time.Sequence, false, engine.CapturePresentationControl().SessionId);
        var update = engine.CapturePresentationUpdate(fullSnapshot: true);
        time.Sequence = update.Sequence;
        return update;
    }

    private static void AssertPrivacy(AuthoritativeSnapshot snapshot)
    {
        foreach (var row in snapshot.Objects.Where(o => o.ObservedProjectile is not null))
        {
            Assert.Null(row.OwnProjectile); Assert.Null(row.Torpedo); Assert.Null(row.Countermeasure);
            var observed = JsonSerializer.SerializeToElement(row.ObservedProjectile);
            Assert.Equal(new[] { "BlastRadiusKm", "DirectionX", "DirectionY", "Kind", "ProfileId", "SpeedKmS", "X", "Y" },
                observed.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
        }
        foreach (var enemy in snapshot.Objects.Where(o => o.ObjectId == Target))
        {
            Assert.Empty(enemy.ModuleHealth); Assert.Null(enemy.Defense); Assert.NotNull(enemy.EnemyStatus);
        }
        foreach (var entry in snapshot.CombatJournal)
        {
            Assert.Null(entry.ChanceTenths); Assert.Null(entry.Roll); Assert.Null(entry.RatingBreakdown);
            Assert.Null(entry.Attack); Assert.Null(entry.Defense);
            if (entry.TargetObjectId == Target)
            {
                Assert.Null(entry.HitModuleId); Assert.Null(entry.Damage);
                Assert.Null(entry.ArmorBefore); Assert.Null(entry.StructureBefore); Assert.Null(entry.DestroyReason);
            }
            if (entry.ActorObjectId == Target) Assert.Null(entry.LauncherModuleId);
        }
        Assert.DoesNotContain("PRIVATE-", JsonSerializer.Serialize(snapshot.CombatJournal));
        Assert.DoesNotContain("PRIVATE-", JsonSerializer.Serialize(snapshot.CombatReports));
    }

    [Fact]
    public void Enemy_secrets_are_absent_from_full_and_compact_scene()
    {
        var time = new Time(); using var engine = Scene(time);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, ForeignTorpedo, 700, 0, "slow", 0).Disposition);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, ForeignCounter, 700, 0, "wide", 0, ProjectileKind.Countermissile).Disposition);
        engine.ReceiveCommand(Launch(x: 600, y: 0, profile: "slow"));
        var first = Capture(engine, time); var second = Capture(engine, time, 2000);
        foreach (var update in new[] { first, second })
        {
            AssertPrivacy(update.FullSnapshot!);
            Assert.NotEmpty(update.Frames);
            foreach (var frame in update.Frames) AssertPrivacy(frame.State);
        }
        Assert.Equal(2, second.FullSnapshot!.Objects.Count(o => o.ObservedProjectile is not null));
        var own = second.FullSnapshot.Objects.Single(o => o.OwnProjectile is not null).OwnProjectile!;
        Assert.Equal(600, own.DetonationX); Assert.Equal(10000, own.DetonationMotionTimeMs);
        Assert.NotNull(own.Reporter);
    }

    [Fact]
    public void Own_modules_and_enemy_public_status_remain_available()
    {
        var time = new Time(); using var engine = Scene(time, engineOnly: true);
        engine.ReceiveCommand(Launch(x: 400, y: 0, profile: "fast")); Capture(engine, time);
        var damaged = Capture(engine, time, 10000).FullSnapshot!;
        AssertPrivacy(damaged);
        Assert.NotEmpty(damaged.Objects.Single(o => o.ObjectId == Player).ModuleHealth);
        var status = damaged.Objects.Single(o => o.ObjectId == Target).EnemyStatus!;
        Assert.True(status.Damaged); Assert.False(status.Destroyed); Assert.Equal(10500, status.ImmobilizedUntilMotionMs);
        var recovered = Capture(engine, time, 42000).FullSnapshot!.Objects.Single(o => o.ObjectId == Target).EnemyStatus!;
        Assert.True(recovered.Damaged); Assert.Null(recovered.ImmobilizedUntilMotionMs);
    }

    [Fact]
    public void Explicit_O12_hostile_combat_payload_is_preserved()
    {
        var time = new Time(); using var engine = Scene(time, ai: true);
        Capture(engine, time);
        var snapshot = Capture(engine, time, 2000).FullSnapshot!;
        var enemy = snapshot.Objects.Single(o => o.ObjectId == Target);
        Assert.NotNull(enemy.HostileCombat);
        Assert.StartsWith("PRIVATE-", enemy.HostileCombat.EngineModuleId);
        Assert.StartsWith("PRIVATE-", enemy.HostileCombat.WeaponModuleId);
        AssertPrivacy(snapshot); // O12 payload itself is intentionally not treated as an undisclosed module array.
    }

    [Fact]
    public void Mixed_internal_point_state_never_retains_a_legacy_flight_payload()
    {
        var time = new Time(); using var engine = Scene(time);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, ForeignTorpedo, 700, 0, "slow", 0).Disposition);
        // Deliberately malformed internal transport data; no retired runtime/catalog is needed.
        var old = new TorpedoSnapshot(Target, ForeignTorpedo, Player, 0, 6, 90, 200, 0,
            new(0, 1, TorpedoRoutePhase.Straight, false, [new(0, 0, 0, 6, 0, 0)]));
        var objects = (List<SpaceObjectRuntime>)typeof(SimulationEngine).GetField("_objects", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(engine)!;
        int index = objects.FindIndex(o => o.InitialMotion.OwnProjectile is not null);
        objects[index] = objects[index] with { InitialMotion = objects[index].InitialMotion with { Torpedo = old } };
        AssertPrivacy(Capture(engine, time).FullSnapshot!);
    }

    [Fact]
    public void Point_fact_and_report_boundary_strips_extra_enemy_metadata()
    {
        var time = new Time(); using var engine = Scene(time);
        typeof(SimulationEngine).GetMethod("AppendCombatEvent", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(engine,
            [new CombatJournalEntry(0, 0, CombatEventType.Hit, Target, Target, "probe", 400, 0,
                ChanceTenths: 500, Roll: 42, Damage: 200, LauncherModuleId: ForeignTorpedo,
                Attack: 75, Defense: 20, HitModuleId: "PRIVATE-module", ArmorBefore: 60, ArmorAfter: 0,
                StructureBefore: 150, StructureAfter: 10, DestroyReason: "PRIVATE-reason")]);
        typeof(SimulationEngine).GetMethod("EmitCombatReport", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(engine,
            [new BattleReportSnapshot(0, 0, BattleReportKind.EnemyShipHit, "probe", new(BattleReportReporterRole.ScannerOperator),
                SourceObjectId: Target, TargetObjectId: Target, LauncherModuleId: ForeignTorpedo,
                Damage: new("PRIVATE-module", "Engine", 60, 0, 150, 10))]);
        AssertPrivacy(Capture(engine, time).FullSnapshot!);
    }
}
