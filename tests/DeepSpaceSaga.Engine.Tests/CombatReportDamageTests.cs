using System.Reflection;
using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatReportDamageTests
{
    [Fact]
    public void LethalHitCreatesOneDestroyedReport()
    {
        using var engine = Create(TorpedoImpactTests.Scenario(o => o.ObjectId == Target ? o with
        { Modules = o.Modules!.Where(m => m.ModuleTypeId == "module.engine.basic").ToArray() } : o));
        engine.ReceiveCommand(Fire("lethal")); At(engine, 0);
        var snapshot = At(engine, 5000);
        var report = Assert.Single(snapshot.CombatReports!.Reports, r => r.Kind == BattleReportKind.EnemyShipHit);
        Assert.Equal(BattleReportVariant.Destroyed, report.Variant);
        Assert.Equal(Target, report.TargetObjectId);
        Assert.NotNull(report.TargetDisplayName);
        Assert.DoesNotContain(snapshot.Objects, o => o.ObjectId == Target);
    }

    [Fact]
    public void NonlethalHitCreatesOneHitReport()
    {
        using var engine = Create();
        engine.ReceiveCommand(Fire("hit")); At(engine, 0);
        var snapshot = At(engine, 5000);
        var report = Assert.Single(snapshot.CombatReports!.Reports, r => r.Kind == BattleReportKind.EnemyShipHit);
        Assert.Equal(BattleReportVariant.Hit, report.Variant);
        Assert.NotNull(report.Damage);
        Assert.Contains(snapshot.CombatJournal, e => e.Type == CombatEventType.ModuleDestroyed);
    }

    [Fact]
    public void ArmorOnlyDamageCreatesCaptainReport()
    {
        using var engine = Create(PlayerDefeatTests.LethalScenario());
        ManualCountermeasureFireTests.LaunchEnemy(engine);
        var objects = (List<SpaceObjectRuntime>)typeof(SimulationEngine).GetField("_objects", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(engine)!;
        int player = objects.FindIndex(o => o.InitialMotion.ObjectId == Player);
        objects[player] = objects[player] with
        {
            Modules = objects[player].Modules.Select(m => m with { Armor = 60, ArmorMax = 60, ArmorClass = "light" }).ToImmutableArray()
        };
        int projectile = objects.FindIndex(o => o.InitialMotion.Torpedo is not null);
        objects[projectile] = objects[projectile] with
        {
            InitialMotion = objects[projectile].InitialMotion with
            {
                Torpedo = objects[projectile].InitialMotion.Torpedo! with { Damage = 1, ArmorPiercingPercent = 0 }
            }
        };
        var snapshot = At(engine, 6251);
        var report = Assert.Single(snapshot.CombatReports!.Reports, r => r.Kind == BattleReportKind.PlayerShipDamaged);
        Assert.Equal(BattleReportReporterRole.Captain, report.Reporter.Role);
        Assert.NotNull(report.Reporter.DisplayName);
        Assert.True(report.Damage!.ArmorAfter < report.Damage.ArmorBefore);
        Assert.Equal(report.Damage.StructureBefore, report.Damage.StructureAfter);
    }

    [Fact]
    public void ZeroDamageDoesNotCreateE07()
    {
        var source = PlayerDefeatTests.LethalScenario();
        using var engine = Create(source with
        {
            GameState = source.GameState with
            { SpaceObjects = source.GameState.SpaceObjects.Select(o => o.ObjectId == Player ? o with { ShipClassId = null } : o).ToArray() }
        });
        var snapshot = At(engine, 6251);
        Assert.Contains(snapshot.CombatJournal, e => e.Type == CombatEventType.Hit && e.Damage == 0);
        Assert.DoesNotContain(snapshot.CombatReports!.Reports, r => r.Kind == BattleReportKind.PlayerShipDamaged);
    }

    [Fact]
    public void SameTimeProjectilesRemainSeparate()
    {
        using var engine = Create();
        engine.ReceiveCommand(Fire("first")); var launched = At(engine, 0);
        var projectile = engine.RuntimeObjects.Single(o => o.InitialMotion.Torpedo is not null);
        var target = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == Target);
        var method = typeof(SimulationEngine).GetMethod("EmitCombatContactReport", BindingFlags.Instance | BindingFlags.NonPublic)!;
        method.Invoke(engine, [projectile, target, 12.5, null, false]);
        method.Invoke(engine, [projectile with { InitialMotion = projectile.InitialMotion with { ObjectId = "second-torpedo" } }, target, 12.5, null, false]);
        var reports = At(engine, 0).CombatReports!.Reports;
        Assert.Equal(2, reports.Length);
        Assert.All(reports, r => Assert.Equal(BattleReportVariant.Hit, r.Variant));
        Assert.Equal(2, reports.Select(r => r.EpisodeId).Distinct().Count());
    }

    [Fact]
    public void DefeatDoesNotWaitForReportPresentation()
    {
        using var engine = Create(PlayerDefeatTests.LethalScenario(150));
        var snapshot = At(engine, 6000);
        Assert.Equal(SessionOutcomeState.Defeated, snapshot.EffectiveOutcome.State);
        var report = Assert.Single(snapshot.CombatReports!.Reports, r => r.Kind == BattleReportKind.PlayerShipDamaged);
        Assert.Equal(snapshot.EffectiveOutcome.DeathMotionTimeMs, report.MotionTimeMs);
        Assert.NotNull(report.Reporter.DisplayName);
        Assert.Empty(At(engine, 10000).CombatReports!.Reports);
    }
}
