using System.Reflection;
using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatReportRangeTests
{
    private static ScenarioFile Scene(double distanceKm = 100) => TorpedoImpactTests.Scenario(o => o with
    {
        IsKnown = true,
        RelationToPlayer = o.ObjectId == Player ? null : PlayerRelation.Enemy,
        PositionY = o.ObjectId == Player ? 0 : -distanceKm * 10
    });

    private static List<SpaceObjectRuntime> Objects(SimulationEngine engine) =>
        (List<SpaceObjectRuntime>)typeof(SimulationEngine).GetField("_objects", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(engine)!;

    [Fact]
    public void ThreeMukhaHaveIndependentRangeReports()
    {
        var scene = Scene();
        var enemy = scene.GameState.SpaceObjects.Single(o => o.ObjectId == Target);
        scene = scene with
        {
            GameState = scene.GameState with
            {
                SpaceObjects = scene.GameState.SpaceObjects.Concat(
            new[] { enemy with { ObjectId = "enemy-3" }, enemy with { ObjectId = "enemy-4" } }).ToArray()
            }
        };
        using var engine = Create(scene);
        var reports = engine.CaptureSnapshot().CombatReports!.Reports;
        Assert.Equal(new[] { Target, "enemy-3", "enemy-4" }, reports.Select(r => r.TargetObjectId));
        Assert.All(reports, r => Assert.Equal(BattleReportKind.EnemyInRange, r.Kind));
    }

    [Theory]
    [InlineData(200, 1)]
    [InlineData(200.0001, 0)]
    public void BoundaryUsesConfiguredRange(double distance, int count)
    {
        using var engine = Create(Scene(distance));
        Assert.Equal(count, engine.CaptureSnapshot().CombatReports!.Reports.Length);
    }

    [Fact]
    public void ReloadAndSelectionDoNotAffectRange()
    {
        using var engine = Create(Scene());
        engine.ReceiveCommand(Fire("shot"));
        var snapshot = At(engine, 0);
        Assert.Contains(snapshot.CombatReports!.Reports, r => r.Kind == BattleReportKind.EnemyInRange);
        Assert.Null(snapshot.SelectedObjectId);
        Assert.Empty(At(engine, 1).CombatReports!.Reports);
    }

    [Fact]
    public void UnknownDestroyedAndDisabledAreExcluded()
    {
        using var engine = Create(Scene());
        var objects = Objects(engine);
        int enemy = objects.FindIndex(o => o.InitialMotion.ObjectId == Target);
        objects[enemy] = objects[enemy] with { IsKnown = false };
        Assert.Empty(At(engine, 0).CombatReports!.Reports);
        objects[enemy] = objects[enemy] with { IsKnown = true, IsDestroyed = true };
        Assert.Empty(At(engine, 0).CombatReports!.Reports);
        objects[enemy] = objects[enemy] with { IsDestroyed = false };
        int player = objects.FindIndex(o => o.InitialMotion.ObjectId == Player);
        var original = objects[player];
        objects[player] = original with
        {
            Modules = original.Modules.Select(m => m.ModuleId.Contains("SCANNER")
            ? m with { DisabledUntilMotionTimeMs = 10 } : m).ToImmutableArray()
        };
        Assert.Empty(At(engine, 0).CombatReports!.Reports);
        Assert.Single(At(engine, 10).CombatReports!.Reports);
    }

    [Fact]
    public void ReentryDoesNotRepeat()
    {
        using var engine = Create(Scene());
        Assert.Single(At(engine, 0).CombatReports!.Reports);
        var objects = Objects(engine);
        int index = objects.FindIndex(o => o.InitialMotion.ObjectId == Target);
        var original = objects[index];
        objects[index] = original with { InitialMotion = original.InitialMotion with { Y = -100000 } };
        Assert.Empty(At(engine, 1).CombatReports!.Reports);
        objects[index] = original;
        Assert.Empty(At(engine, 2).CombatReports!.Reports);
    }

    [Fact]
    public void NewGameInRangeEmitsOnce()
    {
        using var engine = Create(Scene());
        Assert.Single(engine.CaptureSnapshot().CombatReports!.Reports);
        Assert.Empty(engine.CaptureSnapshot().CombatReports!.Reports);
        engine.LoadScenario(Scene());
        Assert.Single(engine.CaptureSnapshot().CombatReports!.Reports);
    }
}
