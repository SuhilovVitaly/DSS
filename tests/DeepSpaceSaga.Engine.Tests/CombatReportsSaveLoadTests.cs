using System.Reflection;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatReportsSaveLoadTests
{
    private static void Move(SimulationEngine engine, double y)
    {
        var objects = (List<SpaceObjectRuntime>)typeof(SimulationEngine).GetField("_objects", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(engine)!;
        int i = objects.FindIndex(o => o.InitialMotion.ObjectId == Target);
        objects[i] = objects[i] with { InitialMotion = objects[i].InitialMotion with { Y = y } };
    }

    [Fact]
    public void ReportedOutsideRangeNeverRepeatsAfterLoad()
    {
        using var engine = Create();
        Assert.Single(At(engine, 0).CombatReports!.Reports);
        Move(engine, -10000);
        var save = engine.CaptureSaveState();
        Assert.Contains(Target, save.GameState.CombatReportsState!.ReportedRangeTargetIds);
        using var loaded = Create();
        loaded.LoadScenario(save, true);
        Assert.Empty(At(loaded, 0).CombatReports!.Reports);
        Move(loaded, -100);
        Assert.Empty(At(loaded, 0).CombatReports!.Reports);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LoadedInRangeIsSilentBaseline(bool preFeature)
    {
        using var engine = Create();
        var save = engine.CaptureSaveState();
        if (preFeature) save = save with { GameState = save.GameState with { CombatReportsState = null } };
        using var loaded = Create();
        loaded.LoadScenario(save, true);
        var batch = At(loaded, 0).CombatReports!;
        Assert.Empty(batch.Reports);
        Assert.Equal(preFeature ? 0 : save.GameState.CombatReportsState!.Sequence, batch.BaselineSequence);
        Assert.Equal(batch.BaselineSequence, batch.LatestSequence);
        Assert.Contains(Target, loaded.CaptureSaveState().GameState.CombatReportsState!.ReportedRangeTargetIds);
    }

    [Fact]
    public void FirstPostLoadNewEventIsDelivered()
    {
        using var engine = Create();
        var save = engine.CaptureSaveState();
        using var loaded = Create();
        loaded.LoadScenario(save, true);
        loaded.ReceiveCommand(Fire("new-shot"));
        var snapshot = At(loaded, 0);
        var report = Assert.Single(snapshot.CombatReports!.Reports);
        Assert.Equal(BattleReportKind.PlayerTorpedoLaunch, report.Kind);
        Assert.Equal(save.GameState.CombatReportsState!.Sequence + 1, report.Sequence);
        Assert.Equal(save.GameState.CombatReportsState.Sequence, snapshot.CombatReports.BaselineSequence);
    }

    [Fact]
    public void FailedRestorePreservesCurrentSession()
    {
        using var engine = Create();
        var save = engine.CaptureSaveState();
        engine.ReceiveCommand(Fire("pending"));
        var invalid = save with { GameState = save.GameState with { CombatReportsState = new(2, 50, []) } };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(invalid, true));
        var snapshot = At(engine, 0);
        Assert.Equal(new[] { BattleReportKind.EnemyInRange, BattleReportKind.PlayerTorpedoLaunch }, snapshot.CombatReports!.Reports.Select(r => r.Kind));
        Assert.Equal(new long[] { 1, 2 }, snapshot.CombatReports.Reports.Select(r => r.Sequence));
    }

    [Fact]
    public void RemovedTargetIdsAreRetainedAndNewGameResets()
    {
        using var engine = Create();
        var save = engine.CaptureSaveState();
        save = save with { GameState = save.GameState with { CombatReportsState = new(1, 42, [Target, "destroyed-enemy"]) } };
        engine.LoadScenario(save, true);
        Assert.Equal(42, At(engine, 0).CombatReports!.BaselineSequence);
        Assert.Contains("destroyed-enemy", engine.CaptureSaveState().GameState.CombatReportsState!.ReportedRangeTargetIds);
        engine.LoadScenario(TorpedoImpactTests.Scenario());
        Assert.Equal(1, Assert.Single(At(engine, 0).CombatReports!.Reports).Sequence);
    }

    [Fact]
    public void ReportGenerationDoesNotPermitDefeatedSave()
    {
        using var engine = Create(PlayerDefeatTests.LethalScenario(150));
        Assert.Equal(SessionOutcomeState.Defeated, At(engine, 6000).EffectiveOutcome.State);
        Assert.Throws<InvalidOperationException>(() => engine.CaptureSaveState());
    }
}
