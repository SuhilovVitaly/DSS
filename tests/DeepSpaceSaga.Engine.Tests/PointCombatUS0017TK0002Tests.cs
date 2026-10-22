using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0004TK0001Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0017TK0002Tests
{
    private const string NpcLauncher = "MOD-PIRATE-TORPEDO-01";
    private static SimulationEngine Battle(double x = 400, bool known = true, bool enemyFire = false, int? enemyArmor = null) =>
        CreateCounter(o => o with
        {
            IsKnown = o.ObjectId == Player || known,
            PositionX = o.ObjectId == Player ? 0 : x,
            PositionY = 0,
            Modules = enemyArmor.HasValue && o.ObjectId == Target
                ? o.Modules!.Where(m => m.ModuleTypeId == "module.engine.basic").Select(m => m with
                { StructurePoints = 150, ArmorClass = enemyArmor.Value == 0 ? "none" : "light", Armor = enemyArmor.Value }).ToArray()
                : o.Modules!.Select(m => m.ModuleId == NpcLauncher && enemyFire ? m with { PowerState = "On" } : m).ToArray()
        }, scatter: 0, torpedoScatter: 0);

    [Theory]
    [InlineData(700, 1)]
    [InlineData(700.1, 0)]
    public void Point_range_report_uses_seventy_km_and_is_once(double x, int count)
    {
        using var engine = Battle(x);
        var first = At(engine);
        Assert.Equal(count, first.CombatReports!.Reports.Count(r => r.Kind == BattleReportKind.EnemyInRange));
        Assert.Empty(At(engine).CombatReports!.Reports);
    }

    [Fact]
    public void Own_launch_reports_capture_profile_and_immutable_operator_without_future_point()
    {
        using var engine = Battle();
        engine.ReceiveCommand(Launch(x: 600, y: 0, profile: "fast"));
        engine.ReceiveCommand(CounterLaunch(x: 300)); var first = At(engine);
        var torpedo = Assert.Single(first.CombatReports!.Reports, r => r.Kind == BattleReportKind.PlayerTorpedoLaunch);
        var counter = Assert.Single(first.CombatReports.Reports, r => r.Kind == BattleReportKind.PlayerCountermeasureLaunch);
        var own = first.Objects.Single(o => o.OwnProjectile?.Kind == ProjectileKind.Torpedo).OwnProjectile!;
        Assert.Equal(own.Reporter, torpedo.Reporter);
        Assert.Equal(BattleReportReporterRole.TorpedoOperator, torpedo.Reporter.Role);
        Assert.Equal(BattleReportReporterRole.CountermeasureOperator, counter.Reporter.Role);
        Assert.Equal("fast", torpedo.ProfileId); Assert.Equal(2.5, torpedo.BlastRadiusKm);
        Assert.Equal("wide", counter.ProfileId); Assert.Equal(1, counter.BlastRadiusKm);
        Assert.Equal(0, torpedo.X); Assert.Equal(0, counter.X);
        Assert.Null(torpedo.TargetObjectId); Assert.Null(counter.TargetObjectId);
        engine.ReceiveCommand(Launch(x: 600, y: 0, profile: "fast"));
        engine.ReceiveCommand(CounterLaunch(x: 300));
        var replay = At(engine);
        Assert.Empty(replay.CombatReports!.Reports);
        Assert.Equal(first.CombatReports.LatestSequence, replay.CombatReports.LatestSequence);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void Enemy_launch_uses_scanner_and_never_enemy_module_or_endpoint(bool known, int count)
    {
        using var engine = Battle(known: known, enemyFire: true);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, NpcLauncher, 700, 0, "slow", 0).Disposition);
        var snapshot = At(engine);
        var reports = snapshot.CombatReports!.Reports.Where(r => r.Kind == BattleReportKind.EnemyTorpedoLaunch).ToArray();
        Assert.Equal(count, reports.Length);
        if (count == 0) return;
        var report = Assert.Single(reports);
        Assert.Equal(BattleReportReporterRole.ScannerOperator, report.Reporter.Role);
        Assert.Null(report.LauncherModuleId); Assert.Null(report.Damage);
        Assert.Equal(400, report.X); Assert.Equal(0, report.Y);
        Assert.DoesNotContain(NpcLauncher, JsonSerializer.Serialize(report));
        Assert.Null(Assert.Single(snapshot.Objects, o => o.ObservedProjectile is not null).OwnProjectile);
    }

    [Fact]
    public void Actual_counter_interception_reports_own_profile_and_one_hostile_victim()
    {
        using var engine = Battle(x: 310, enemyFire: true);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, NpcLauncher, 0, 0, "fast", 0).Disposition);
        engine.ReceiveCommand(CounterLaunch(x: 150)); At(engine);
        var result = At(engine, 1000);
        var report = Assert.Single(result.CombatReports!.Reports, r => r.Kind == BattleReportKind.EnemyTorpedoIntercepted);
        Assert.Equal(1000, report.MotionTimeMs);
        Assert.Equal(BattleReportReporterRole.CountermeasureOperator, report.Reporter.Role);
        Assert.Equal("wide", report.ProfileId); Assert.Equal(1, report.BlastRadiusKm);
        Assert.Equal(ProjectileKind.Countermissile, report.ProjectileKind);
        Assert.Equal(ProjectileHitResult.ShotDown, report.HitResult);
        Assert.Equal(Defense, report.LauncherModuleId); Assert.Null(report.Damage);
        Assert.Empty(At(engine, 1000).CombatReports!.Reports);
    }

    [Theory]
    [InlineData(0, BattleReportVariant.Destroyed)]
    [InlineData(60, BattleReportVariant.Hit)]
    public void Enemy_hit_reports_only_public_outcome_and_captured_shooter(int armor, BattleReportVariant variant)
    {
        using var engine = Battle(enemyArmor: armor);
        engine.ReceiveCommand(Launch(x: 400, y: 0, profile: "fast")); var start = At(engine);
        var captured = Flight(start).OwnProjectile!.Reporter;
        var objects = (List<SpaceObjectRuntime>)typeof(SimulationEngine).GetField("_objects", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(engine)!;
        int playerIndex = objects.FindIndex(o => o.InitialMotion.ObjectId == Player);
        var player = objects[playerIndex];
        objects[playerIndex] = player with { Crew = player.Crew.Select(c => c with { DisplayName = "Changed after launch" }).ToImmutableArray() };
        var actual = At(engine, 2500);
        var report = Assert.Single(actual.CombatReports!.Reports, r => r.Kind == BattleReportKind.EnemyShipHit);
        Assert.Equal(variant, report.Variant); Assert.Equal(captured, report.Reporter);
        Assert.Equal(ProjectileHitResult.Core, report.HitResult); Assert.Equal("fast", report.ProfileId);
        Assert.Equal(400, report.X); Assert.Equal(0, report.Y);
        Assert.Null(report.Damage);
        Assert.DoesNotContain("MOD-PIRATE", JsonSerializer.Serialize(report));
    }

    [Fact]
    public void Unknown_enemy_contact_does_not_disclose_identity_or_numeric_module_damage()
    {
        using var engine = Battle(known: false, enemyArmor: 60);
        engine.ReceiveCommand(Launch(x: 400, y: 0, profile: "fast")); At(engine);
        var report = Assert.Single(At(engine, 2500).CombatReports!.Reports, r => r.Kind == BattleReportKind.EnemyShipHit);
        Assert.Null(report.TargetObjectId); Assert.Null(report.TargetDisplayName); Assert.Null(report.Damage);
        Assert.EndsWith(":unknown", report.EpisodeId);
    }

    [Fact]
    public void Own_damage_report_uses_captain_and_actual_own_module_loss()
    {
        using var engine = Battle(enemyFire: true);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, NpcLauncher, 0, 0, "slow", 0).Disposition);
        At(engine);
        var result = At(engine, 7000);
        var report = Assert.Single(result.CombatReports!.Reports, r => r.Kind == BattleReportKind.PlayerShipDamaged);
        Assert.Equal(BattleReportReporterRole.Captain, report.Reporter.Role);
        Assert.Equal(Player, report.TargetObjectId); Assert.Null(report.LauncherModuleId);
        Assert.NotNull(report.Damage);
        Assert.StartsWith("MOD-PLAYER", report.Damage.ModuleId);
        Assert.True(report.Damage.ArmorAfter < report.Damage.ArmorBefore || report.Damage.StructureAfter < report.Damage.StructureBefore);
        Assert.Equal(ProjectileHitResult.Core, report.HitResult);
        Assert.Equal(0, report.X); Assert.Equal(0, report.Y);
    }

    [Fact]
    public void Rounded_zero_blast_has_no_hit_report()
    {
        using var engine = Battle(x: 689.99);
        engine.ReceiveCommand(Launch(x: 600, y: 0, profile: "slow")); At(engine);
        Assert.DoesNotContain(At(engine, 10000).CombatReports!.Reports, r => r.Kind is BattleReportKind.EnemyShipHit or BattleReportKind.PlayerShipDamaged);
    }

    internal static SimulationEngine OldEnemyFlightAcrossNewEncounter()
    {
        var scenario = TorpedoImpactTests.Scenario(o => o with
        {
            Ai = null,
            IsKnown = true,
            PositionX = o.ObjectId == Player ? 0 : 400,
            PositionY = 0,
            Modules = o.ObjectId != Target ? o.Modules : o.Modules!.Where(m => m.ModuleTypeId is "module.engine.basic" or "module.torpedo.launcher.basic")
                .Select(m => m with
                {
                    StructurePoints = 100,
                    ArmorClass = "none",
                    Armor = 0,
                    PowerState = m.ModuleId == NpcLauncher ? "On" : "Off"
                }).ToArray()
        });
        var first = scenario.GameState.SpaceObjects.Single(o => o.ObjectId == Target);
        var second = first with
        {
            ObjectId = "new-enemy",
            PositionX = 2000,
            Crew = first.Crew!.Select(c => c with { CrewId = "new-" + c.CrewId }).ToArray(),
            Modules = first.Modules!.Select(m => m with
            { ModuleId = "new-" + m.ModuleId, OperatorCrewId = m.OperatorCrewId is null ? null : "new-" + m.OperatorCrewId }).ToArray()
        };
        var engine = new SimulationEngine(CounterRegistry(scatter: 0, torpedoScatter: 0));
        try
        {
            engine.LoadScenario(scenario with { GameState = scenario.GameState with { SpaceObjects = scenario.GameState.SpaceObjects.Append(second).ToArray() } });
            Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, NpcLauncher, 0, 0, "slow", 0).Disposition);
            engine.ReceiveCommand(Launch(x: 400, y: 0, profile: "fast")); At(engine);
            Assert.Equal(CombatEncounterState.Won, At(engine, 2500).Encounter!.State);
            Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch("new-enemy", "new-" + NpcLauncher, 1700, 0, "slow", 2500).Disposition);
            engine.ReceiveCommand(CounterLaunch(x: 2500.0 / 14)); At(engine, 2500);
            return engine;
        }
        catch { engine.Dispose(); throw; }
    }

    [Fact]
    public void Intercept_report_retains_hostile_launch_side_after_owner_death_and_new_encounter()
    {
        using var engine = OldEnemyFlightAcrossNewEncounter();
        var snapshot = At(engine, 3700);
        Assert.Equal(CombatEncounterState.Active, snapshot.Encounter!.State);
        Assert.Single(snapshot.CombatReports!.Reports, r => r.Kind == BattleReportKind.EnemyTorpedoIntercepted);
        Assert.DoesNotContain(snapshot.Objects, o => o.ObjectId == Target);
    }
}
