using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using Point = DeepSpaceSaga.Engine.Tests.PointCounterFixture;
namespace DeepSpaceSaga.Engine.Tests;

public class CountermeasureLifetimeTests
{
    [Theory]
    [InlineData("wide", 150, 1000)]
    [InlineData("fast", 250, 1000)]
    [InlineData("wide", 300, 2000)]
    public void Point_fuse_is_inclusive_physical_and_independent_of_reload(string profile, double x, long deadline)
    {
        using var engine = Point.Create(); engine.ReceiveCommand(Point.Shot(x: x, y: 0, profile: profile));
        Assert.Equal(deadline, Point.Flight(Point.At(engine)).DetonationMotionTimeMs);
        Assert.NotNull(Point.Flight(Point.At(engine, deadline - 1)));
        var after = Point.At(engine, deadline);
        Assert.DoesNotContain(after.Objects, o => o.OwnProjectile is not null);
        Assert.Equal(deadline, Assert.Single(after.CombatJournal, e => e.Type == CombatEventType.CountermissileDetonated).MotionTimeMs);
        var defense = after.InstalledModules.Single(m => m.ModuleId == Point.Counter).Defense!;
        Assert.Equal(12500, defense.ReadyAtMotionTimeMs);
        Assert.Equal(DefenseState.Reloading, defense.State);
        Assert.Equal(DefenseState.Ready, Point.At(engine, 12500).InstalledModules.Single(m => m.ModuleId == Point.Counter).Defense!.State);
        Assert.All(after.CombatJournal, e => { Assert.Null(e.Roll); Assert.Null(e.ChanceTenths); });
    }

    [Fact]
    public void Fractional_fuse_and_partitioned_steps_produce_identical_save()
    {
        using var large = Point.Create(); using var small = Point.Create();
        large.ReceiveCommand(Point.Shot(x: 101, y: 0)); small.ReceiveCommand(Point.Shot(x: 101, y: 0));
        Point.At(large);
        double deadline = Point.Flight(Point.At(small)).DetonationMotionTimeMs;
        Assert.InRange(deadline, 673.333333333, 673.333333334);
        for (int t = 1; t <= 1000; t++) Point.At(small, t);
        Assert.Equal(ScenarioLoader.Serialize(Point.Save(large, 1000)), ScenarioLoader.Serialize(Point.Save(small, 1000)));
        Assert.Equal(deadline, Assert.Single(Point.At(small, 1000).CombatJournal, e => e.Type == CombatEventType.CountermissileDetonated).MotionTimeMs);
    }

    [Fact]
    public void Paused_calendar_and_retired_toggles_do_not_extend_captured_fuse()
    {
        using var engine = Point.Create(); engine.ReceiveCommand(Point.Shot(x: 150, y: 0));
        var initial = Point.Flight(Point.At(engine));
        foreach (string command in new[] { DefenseCommandTypes.Enable, DefenseCommandTypes.Disable })
        {
            engine.ReceiveCommand(new(command, 1, Point.Player, Point.Counter, command));
            var paused = engine.CaptureSnapshotForTests(9000000, SimulationSpeed.Speed0, 0);
            Assert.Equal(initial, Point.Flight(paused));
        }
        Assert.DoesNotContain(Point.At(engine, 1000).Objects, o => o.OwnProjectile is not null);
        Assert.Equal(2ul, Point.Save(engine, 1000).GameState.PointCombatState!.LaunchScatter.Counter);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(673)]
    public void Captured_fractional_deadline_survives_serializer_loader(long checkpoint)
    {
        using var original = Point.Create(); original.ReceiveCommand(Point.Shot(x: 101, y: 0));
        var save = Point.Save(original, checkpoint);
        using var resumed = new SimulationEngine(Point.Registry());
        resumed.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, isSave: true), true);
        foreach (long time in new[] { checkpoint, 674, 12500 })
            Assert.Equal(ScenarioLoader.Serialize(Point.Save(original, time)), ScenarioLoader.Serialize(Point.Save(resumed, time)));
    }
}
