using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.CombatReportsScenarioFixtures;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatReportsEndToEndTests
{
    [Fact]
    public void CombatMukhaUsesProductionScenario()
    {
        var run = ProductionRun();
        var range = run.Reports.Where(r => r.Kind == BattleReportKind.EnemyInRange).ToArray();
        Assert.Equal(new[] { "SPC-MUKHA-01", "SPC-MUKHA-02", "SPC-MUKHA-03" }, range.Select(r => r.TargetObjectId).Order());
        Assert.Contains(run.Reports, r => r.Kind == BattleReportKind.EnemyTorpedoLaunch);
        Assert.Contains(run.Reports, r => r.Kind == BattleReportKind.PlayerTorpedoLaunch);
        Assert.True(run.Final.EffectiveOutcome.State == SessionOutcomeState.Defeated || !run.Final.Objects.Any(o => o.RenderObjectType == SpaceObjectType.NpcShip));
    }

    [Fact]
    public void EveryReportKindHasDeterministicProof()
    {
        var reports = ProductionRun().Reports.Concat(AttackReports()).Concat(InterceptReports()).ToArray();
        Assert.Equal(Enum.GetValues<BattleReportKind>().Order(), reports.Select(r => r.Kind).Distinct().Order());
        Assert.Contains(reports, r => r.Kind == BattleReportKind.EnemyShipHit && r.Variant == BattleReportVariant.Hit);
        Assert.Contains(reports, r => r.Kind == BattleReportKind.EnemyShipHit && r.Variant == BattleReportVariant.Destroyed);
        Assert.All(reports, r => { Assert.True(r.Sequence > 0); Assert.True(double.IsFinite(r.MotionTimeMs)); });
    }

    [Fact]
    public void SameSeedReportsAreIdentical()
    {
        var first = ProductionRun(); var second = ProductionRun();
        Assert.Equal(JsonSerializer.Serialize(first.Reports), JsonSerializer.Serialize(second.Reports));
        Assert.Equal(Enumerable.Range(1, first.Reports.Length).Select(i => (long)i), first.Reports.Select(r => r.Sequence));
    }

    [Fact]
    public void ReportProjectionDoesNotChangeCombatOutcome()
    {
        var reporting = ProductionRun(); var suppressedScanner = ProductionRun(false);
        Assert.NotEqual(reporting.Reports.Length, suppressedScanner.Reports.Length);
        Assert.Equal(JsonSerializer.Serialize(reporting.Final.CombatJournal), JsonSerializer.Serialize(suppressedScanner.Final.CombatJournal));
        Assert.Equal(reporting.Final.Outcome, suppressedScanner.Final.Outcome);
        Assert.Equal(reporting.Final.MotionTimeMs, suppressedScanner.Final.MotionTimeMs);
        Assert.Equal(reporting.Final.Objects.Select(o => (o.ObjectId, o.X, o.Y, o.SpeedKmS, o.Direction)),
            suppressedScanner.Final.Objects.Select(o => (o.ObjectId, o.X, o.Y, o.SpeedKmS, o.Direction)));
    }

    [Fact]
    public void SaveLoadAndTerminalReportMatrix()
    {
        using var source = Create(); var before = source.CaptureSaveState();
        using var restored = Create(); restored.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(before), true), true);
        Assert.Empty(At(restored, 0).CombatReports!.Reports);
        restored.ReceiveCommand(Fire("after-load"));
        var launch = At(restored, 0); Assert.Single(launch.CombatReports!.Reports);
        var hit = At(restored, 5000); Assert.Single(hit.CombatReports!.Reports, r => r.Kind == BattleReportKind.EnemyShipHit);
        using var defeated = Create(PlayerDefeatTests.LethalScenario(150));
        var end = At(defeated, 100000);
        Assert.Equal(SessionOutcomeState.Defeated, end.EffectiveOutcome.State);
        Assert.Contains(end.CombatReports!.Reports, r => r.Kind == BattleReportKind.PlayerShipDamaged);
        Assert.Throws<InvalidOperationException>(() => defeated.CaptureSaveState());
    }
}
