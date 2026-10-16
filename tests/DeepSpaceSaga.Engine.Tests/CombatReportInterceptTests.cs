using System.Reflection;
using DeepSpaceSaga.Contracts;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
using static DeepSpaceSaga.Engine.Tests.ManualCountermeasureFireTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatReportInterceptTests
{
    [Theory]
    [InlineData(100, true)]
    [InlineData(0, false)]
    public void OnlySuccessfulPlayerInterceptCreatesE05(decimal accuracy, bool expected)
    {
        using var engine = Create(HostileTorpedoFireTests.HostileScenario(), AutomaticDefenseTests.WeaponRegistry(accuracy, 0));
        string torpedo = LaunchEnemy(engine);
        engine.ReceiveCommand(Manual("intercept", torpedo));
        At(engine, 1);
        var snapshot = At(engine, 12000);
        Assert.Equal(expected ? 1 : 0, snapshot.CombatReports!.Reports.Count(r => r.Kind == BattleReportKind.EnemyTorpedoIntercepted));
        Assert.Equal(expected ? 1 : 0, snapshot.CombatJournal.Count(e => e.Type == CombatEventType.Intercept));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ForeignAndFriendlyInterceptExcluded(bool foreign)
    {
        using var engine = Create(HostileTorpedoFireTests.HostileScenario(), AutomaticDefenseTests.WeaponRegistry(100, 0));
        string torpedo = LaunchEnemy(engine);
        engine.ReceiveCommand(Manual("intercept", torpedo));
        At(engine, 1);
        var objects = (List<SpaceObjectRuntime>)typeof(SimulationEngine).GetField("_objects", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(engine)!;
        if (foreign)
        {
            int i = objects.FindIndex(o => o.InitialMotion.Countermeasure is not null);
            objects[i] = objects[i] with
            {
                InitialMotion = objects[i].InitialMotion with
                { Countermeasure = objects[i].InitialMotion.Countermeasure! with { OwnerObjectId = Target } }
            };
        }
        else
        {
            int i = objects.FindIndex(o => o.InitialMotion.ObjectId == torpedo);
            objects[i] = objects[i] with
            {
                InitialMotion = objects[i].InitialMotion with
                { Torpedo = objects[i].InitialMotion.Torpedo! with { OwnerPlayerRelationAtLaunch = PlayerRelation.Friend } }
            };
        }
        Assert.DoesNotContain(At(engine, 12000).CombatReports!.Reports, r => r.Kind == BattleReportKind.EnemyTorpedoIntercepted);
    }

    [Fact]
    public void RemovedProjectileStillHasCapturedReporter()
    {
        using var engine = Create(HostileTorpedoFireTests.HostileScenario(), AutomaticDefenseTests.WeaponRegistry(100, 0));
        string torpedo = LaunchEnemy(engine);
        engine.ReceiveCommand(Manual("intercept", torpedo));
        var launched = At(engine, 1);
        var projectile = launched.Objects.Single(o => o.Countermeasure is not null);
        var captured = projectile.Countermeasure!.RatingBreakdown.DefenseOperator;
        var snapshot = At(engine, 12000);
        var report = Assert.Single(snapshot.CombatReports!.Reports, r => r.Kind == BattleReportKind.EnemyTorpedoIntercepted);
        Assert.Equal(captured.CrewId, report.Reporter.CrewId);
        Assert.Equal(captured.DisplayName, report.Reporter.DisplayName);
        Assert.DoesNotContain(snapshot.Objects, o => o.ObjectId == torpedo || o.ObjectId == projectile.ObjectId);
        Assert.Equal(torpedo, report.TargetObjectId);
    }
}
