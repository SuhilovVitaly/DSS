using System.Reflection;
using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
using static DeepSpaceSaga.Engine.Tests.ManualCountermeasureFireTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatReportLaunchTests
{
    private static List<SpaceObjectRuntime> Objects(SimulationEngine engine) =>
        (List<SpaceObjectRuntime>)typeof(SimulationEngine).GetField("_objects", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(engine)!;
    private static void Observe(SimulationEngine engine, CombatJournalEntry entry) =>
        typeof(SimulationEngine).GetMethod("ObserveCombatReportFact", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(engine, [entry]);

    [Fact]
    public void EnemyPlayerAndCountermeasureLaunches()
    {
        using var engine = Create(HostileTorpedoFireTests.HostileScenario());
        engine.TryStartHostileTorpedoFire(Target, "MOD-PIRATE-TORPEDO-01", 1);
        var first = At(engine, 1);
        var enemy = Assert.Single(first.CombatReports!.Reports, r => r.Kind == BattleReportKind.EnemyTorpedoLaunch);
        Assert.Equal(BattleReportReporterRole.ScannerOperator, enemy.Reporter.Role);
        var torpedo = first.Objects.Single(o => o.Torpedo is not null);
        engine.ReceiveCommand(Fire("player"));
        engine.ReceiveCommand(Manual("defense", torpedo.ObjectId));
        var next = At(engine, 1);
        Assert.Equal(new[] { BattleReportKind.PlayerTorpedoLaunch, BattleReportKind.PlayerCountermeasureLaunch }, next.CombatReports!.Reports.Select(r => r.Kind));
        Assert.All(next.CombatReports.Reports, r => { Assert.NotNull(r.Reporter.CrewId); Assert.NotNull(r.LauncherModuleId); });
        Assert.Equal(Enumerable.Range(1, next.CombatJournal.Length).Select(i => (long)i), next.CombatJournal.Select(e => e.EventId));
    }

    [Fact]
    public void BurstCreatesOneReportPerActualShot()
    {
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(ClientRoot, "Settings.json"), out _, out _);
        using var engine = Create(HostileTorpedoFireTests.HostileScenario(), registry);
        var torpedo = LaunchEnemy(engine);
        engine.ReceiveCommand(Manual("burst", torpedo));
        var first = At(engine, 1);
        var second = At(engine, 2001);
        var reports = first.CombatReports!.Reports.Concat(second.CombatReports!.Reports)
            .Where(r => r.Kind == BattleReportKind.PlayerCountermeasureLaunch).ToArray();
        Assert.Equal(2, reports.Length);
        Assert.Equal(new double[] { 1, 2001 }, reports.Select(r => r.MotionTimeMs));
        Assert.Equal(2, reports.Select(r => r.ProjectileObjectId).Distinct().Count());
    }

    [Fact]
    public void RejectedAndSkippedLaunchesHaveNoReport()
    {
        using var engine = Create(HostileTorpedoFireTests.HostileScenario());
        At(engine, 0);
        engine.ReceiveCommand(Fire("bad") with { TargetObjectId = "missing" });
        Assert.Empty(At(engine, 0).CombatReports!.Reports);
        Observe(engine, new(1, 0, CombatEventType.BurstShotSkipped, Player, Target, "absent", 0, 0));
        Assert.Empty(At(engine, 0).CombatReports!.Reports);
    }

    [Fact]
    public void EnemyNpcBattleAndUnknownLaunchAreExcluded()
    {
        using var engine = Create(HostileTorpedoFireTests.HostileScenario());
        engine.TryStartHostileTorpedoFire(Target, "MOD-PIRATE-TORPEDO-01", 1);
        var initial = At(engine, 1);
        var launch = initial.CombatJournal.Single(e => e.Type == CombatEventType.Launch);
        var objects = Objects(engine);
        int enemy = objects.FindIndex(o => o.InitialMotion.ObjectId == Target);
        objects[enemy] = objects[enemy] with { IsKnown = false };
        Observe(engine, launch);
        Assert.Empty(At(engine, 1).CombatReports!.Reports);
        objects[enemy] = objects[enemy] with { IsKnown = true };
        int projectile = objects.FindIndex(o => o.InitialMotion.ObjectId == launch.ProjectileObjectId);
        objects[projectile] = objects[projectile] with
        {
            InitialMotion = objects[projectile].InitialMotion with
            { Torpedo = objects[projectile].InitialMotion.Torpedo! with { TargetObjectId = Target } }
        };
        Observe(engine, launch);
        Assert.Empty(At(engine, 1).CombatReports!.Reports);
    }

    [Fact]
    public void DisabledScannerDoesNotReplayLaunches()
    {
        var scenario = HostileTorpedoFireTests.HostileScenario();
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o.ObjectId != Player ? o : o with
                { Modules = o.Modules!.Select(m => m.ModuleTypeId == "module.scanner.mk1" ? m with { DisabledUntilMotionTimeMs = 10 } : m).ToArray() }).ToArray()
            }
        };
        using var engine = Create(scenario);
        engine.TryStartHostileTorpedoFire(Target, "MOD-PIRATE-TORPEDO-01", 1);
        Assert.DoesNotContain(At(engine, 1).CombatReports!.Reports, r => r.Kind == BattleReportKind.EnemyTorpedoLaunch);
        Assert.DoesNotContain(At(engine, 10).CombatReports!.Reports, r => r.Kind == BattleReportKind.EnemyTorpedoLaunch);
    }
}
