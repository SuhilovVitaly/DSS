using System.Reflection;
using DeepSpaceSaga.Contracts;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatReportOutboxTests
{
    private static void Emit(SimulationEngine engine, double time = 0) =>
        typeof(SimulationEngine).GetMethod("EmitCombatReport", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(engine, [new BattleReportSnapshot(999, time, BattleReportKind.PlayerTorpedoLaunch,
                "projectile", new(BattleReportReporterRole.TorpedoOperator))]);

    [Fact]
    public void OutboxSequenceAndBootstrapBaseline()
    {
        using var engine = Create(TorpedoImpactTests.Scenario());
        Assert.Equal(0, engine.CaptureSnapshot().CombatReports!.BaselineSequence);
        Emit(engine, 12.5);
        Emit(engine, 13.5);
        var batch = engine.CaptureSnapshot().CombatReports!;
        Assert.Equal(0, batch.BaselineSequence);
        Assert.Equal(2, batch.LatestSequence);
        Assert.Equal(new long[] { 1, 2 }, batch.Reports.Select(r => r.Sequence));
        Assert.Equal(new double[] { 12.5, 13.5 }, batch.Reports.Select(r => r.MotionTimeMs));
        engine.LoadScenario(TorpedoImpactTests.Scenario());
        Assert.Equal(0, engine.CaptureSnapshot().CombatReports!.LatestSequence);
    }

    [Fact]
    public void SnapshotPublishesOutboxOnce()
    {
        using var engine = Create(TorpedoImpactTests.Scenario());
        Emit(engine);
        Assert.Single(engine.CaptureSnapshot().CombatReports!.Reports);
        var next = engine.CaptureSnapshot().CombatReports!;
        Assert.Empty(next.Reports);
        Assert.Equal(1, next.LatestSequence);
    }

    [Fact]
    public void ReportOutboxOverflowIsExplicit()
    {
        using var engine = Create(TorpedoImpactTests.Scenario());
        for (int i = 0; i < CombatReportBatchSnapshot.PendingLimit; i++) Emit(engine);
        var failure = Assert.Throws<TargetInvocationException>(() => Emit(engine));
        Assert.IsType<InvalidOperationException>(failure.InnerException);
        var batch = engine.CaptureSnapshot().CombatReports!;
        Assert.Equal(CombatReportBatchSnapshot.PendingLimit, batch.LatestSequence);
        Assert.Equal(CombatReportBatchSnapshot.PendingLimit, batch.Reports.Length);
    }

    [Fact]
    public void ReportsDoNotAdvanceRngOrClocks()
    {
        using var engine = Create(TorpedoImpactTests.Scenario());
        var before = System.Text.Json.JsonSerializer.Serialize(engine.CaptureSaveState());
        Emit(engine);
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(engine.CaptureSaveState()));
    }
}
