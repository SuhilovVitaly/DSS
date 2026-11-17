using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0035Us0002Tk0002Tests
{
    [Fact]
    public void CaptureSearchContinuesPastInitialSafeZoneUntilOutside()
    {
        double? crossing = SimulationEngine.TrajectoryCircleEntry(t => new(101 - t * .01, 0),
            250, .01, 200, t => -1 + t * .01);
        Assert.NotNull(crossing);
        Assert.InRange(crossing.Value, 100, 100.001);
        Assert.True(-1 + crossing.Value * .01 > 0);
    }
    [Fact]
    public void CurvedTangentUsesActualPathInsteadOfChord()
    {
        // The chord crosses the capture circle; the upper semicircle remains 201 units away.
        PiratePoint Arc(double t) => new(201 * Math.Cos(Math.PI * t), 201 * Math.Sin(Math.PI * t));
        Assert.NotNull(SimulationEngine.RelativeCircleEntry(201, 0, -201, 0, 200));
        Assert.Null(SimulationEngine.TrajectoryCircleEntry(Arc, 1, 201 * Math.PI, 200));
        PiratePoint InsideArc(double t) => new(201 * Math.Cos(Math.PI * t), 199 * Math.Sin(Math.PI * t));
        Assert.InRange(SimulationEngine.TrajectoryCircleEntry(InsideArc, 1, 201 * Math.PI, 200)!.Value, .2, .5);
    }

    [Fact]
    public void ReturnRoutesAroundStartAndReachesSavedWaypoint()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var world = engine.CaptureSaveState().GameState.DemoCampaign!.PirateWorld! with { AccelerationKmS2 = 1, TurnRateDegreesPerSecond = 30 };
        var group = world.Groups[0] with { X = 8100, Y = 0, Direction = 270, SpeedKmS = 0, Phase = "return",
            PatrolPoints = [new(-10000, 0), new(-10000, 1000), new(-11000, 1000), new(-11000, 0)], NextPatrolPoint = 0 };
        for (int i = 0; i < 40000 && group.Phase == "return"; i++)
        {
            var next = SimulationEngine.StepPirateGroup(group, world, new(0, 0), new(0, 0), 10, 2000, .25);
            Assert.True(SimulationEngine.SegmentDistance(new(group.X, group.Y), new(next.X, next.Y), new(0, 0)) > 8000);
            group = next;
        }
        Assert.Equal("patrol", group.Phase);
        Assert.Equal(1, group.NextPatrolPoint);
    }
    [Fact]
    public void ContactLossKeepsObservedPoint_AndSafeZoneCancelsPursuit()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var world = engine.CaptureSaveState().GameState.DemoCampaign!.PirateWorld!;
        var group = world.Groups[0] with { X = 20000, Y = 0, Direction = 90, SpeedKmS = 0 };
        var start = new PiratePoint(0, 0);
        var observed = new PiratePoint(21000, 0);
        group = SimulationEngine.StepPirateGroup(group, world, observed, start, 5, 2000, .25);
        Assert.Equal("pursuit", group.Phase);
        Assert.Equal(observed, group.LastObservedPlayer);
        long epoch = group.ContactEpoch;
        group = SimulationEngine.StepPirateGroup(group, world, new(40000, 0), start, 5, 2000, .25);
        Assert.Equal("last_known", group.Phase);
        Assert.Equal(observed, group.LastObservedPlayer);
        group = SimulationEngine.StepPirateGroup(group, world, new(8000, 0), start, 5, 2000, .25);
        Assert.Equal("return", group.Phase);
        Assert.Equal("start_safe", group.LastPursuitEndReason);
        Assert.Equal(epoch, group.ContactEpoch);
    }

    [Theory]
    [InlineData(-1000, 0, 1000, 0, .4)]
    [InlineData(-1000, 200, 1000, 200, .5)]
    [InlineData(0, 0, 1000, 0, 0)]
    public void SweptEntryIncludesTangentAndInside(double x0, double y0, double x1, double y1, double expected) =>
        Assert.Equal(expected, SimulationEngine.RelativeCircleEntry(x0, y0, x1, y1, 200)!.Value, 10);

    [Fact]
    public void BrakingGuardKeepsEntireTrajectoryOutsideStartWithoutInstantStop()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var world = engine.CaptureSaveState().GameState.DemoCampaign!.PirateWorld! with { AccelerationKmS2 = 1, TurnRateDegreesPerSecond = 15 };
        var group = world.Groups[0] with { X = 8100, Y = 0, Direction = 270, SpeedKmS = 3 };
        for (int i = 0; i < 60; i++)
        {
            var next = SimulationEngine.StepPirateGroup(group, world, new(8001, 0), new(0, 0), 3, 2000, .25);
            Assert.True(SimulationEngine.SegmentDistance(new(group.X, group.Y), new(next.X, next.Y), new(0, 0)) > 8000);
            Assert.InRange(Math.Abs(next.SpeedKmS - group.SpeedKmS), 0, .25000001);
            group = next;
        }
    }

    [Fact]
    public void WorldMotionIsPartitionAndSaveLoadInvariant_AndPauseIsStable()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var initial = engine.CaptureSaveState();
        engine.CaptureSnapshotForTests(40000, SimulationSpeed.Speed0, 10000);
        var one = engine.CaptureSaveStateForTests(40000, SimulationSpeed.Speed0, 10000).GameState.DemoCampaign!.PirateWorld!;
        Assert.NotEqual(initial.GameState.DemoCampaign!.PirateWorld!.Groups[0].X, one.Groups[0].X);
        engine.LoadScenario(initial, true);
        engine.CaptureSnapshotForTests(492, SimulationSpeed.Speed0, 123);
        var partial = engine.CaptureSaveStateForTests(492, SimulationSpeed.Speed0, 123);
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(partial), true, true), true);
        for (int i = 1; i <= 10; i++) engine.CaptureSnapshotForTests(i * 4000, SimulationSpeed.Speed0, i * 1000);
        var split = engine.CaptureSaveStateForTests(40000, SimulationSpeed.Speed0, 10000).GameState.DemoCampaign!.PirateWorld!;
        Assert.Equal(JsonSerializer.Serialize(one), JsonSerializer.Serialize(split));
        engine.CaptureSnapshotForTests(40000, SimulationSpeed.Speed0, 10000);
        Assert.Equal(JsonSerializer.Serialize(split), JsonSerializer.Serialize(engine.CaptureSaveStateForTests(40000, SimulationSpeed.Speed0, 10000).GameState.DemoCampaign!.PirateWorld));
    }
}
