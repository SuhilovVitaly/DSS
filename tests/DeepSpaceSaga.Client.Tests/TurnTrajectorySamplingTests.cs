using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Client.Tests;

public class TurnTrajectorySamplingTests
{
    [Fact]
    public void Viewport_projection_invalidates_cached_turn_for_each_changed_motion_state()
    {
        var predictor = new LinearMotionPredictor();
        var projector = new FutureTrajectoryProjector(predictor);
        var state = new ObjectMotionSnapshot("ship", 10, 20, 4, 90,
            ShipEngineCommandTypes.TurnRightUntilCancel, 1, 137, 333);
        var points = new List<FutureTrajectoryPoint>(801);
        var camera = new CameraState(0, 0, 1);
        foreach (var next in new[] { state, state with { }, state with { X = 100 },
                     state with { Direction = 270 }, state with { SpeedKmS = 2 },
                     state with { TurnStepRemainingMs = 0 }, state with { TurnStepDegrees = -3 } })
        {
            projector.ProjectViewportInto(next, points, camera, 1920, 1080);
            Assert.Equal(801, points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                var expected = predictor.Predict(next, i * 250L);
                Assert.InRange(Math.Abs(expected.X - points[i].X), 0, 1e-7);
                Assert.InRange(Math.Abs(expected.Y - points[i].Y), 0, 1e-7);
            }
        }
    }

    [Fact]
    public void Fresh_and_cached_turn_projections_do_not_allocate_one_snapshot_per_sample()
    {
        var projector = new FutureTrajectoryProjector(new LinearMotionPredictor());
        var state = new ObjectMotionSnapshot("ship", 0, 0, 4, 0, null, 1, 250, 250);
        var next = state with { X = 1 };
        var points = new List<FutureTrajectoryPoint>(801);
        projector.ProjectInto(state, points);
        long before = GC.GetAllocatedBytesForCurrentThread();
        projector.ProjectInto(next, points);
        long freshBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        before = GC.GetAllocatedBytesForCurrentThread();
        projector.ProjectInto(next, points);
        long cachedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.InRange(freshBytes, 0, 4096);
        Assert.InRange(cachedBytes, 0, 256);
    }

    [Fact]
    public void Custom_predictor_retains_absolute_time_contract()
    {
        var predictor = new AbsolutePredictor();
        var projector = new FutureTrajectoryProjector(predictor);
        var points = projector.Project(new ObjectMotionSnapshot("ship", 0, 0, 4, 0, null, 1, 250, 250));
        Assert.Equal(801, predictor.Calls);
        Assert.Equal(200000, points[^1].X);
    }

    private sealed class AbsolutePredictor : IMotionPredictor
    {
        public int Calls;
        public ObjectMotionSnapshot Predict(ObjectMotionSnapshot state, long elapsedMs)
        {
            Calls++;
            return state with { X = elapsedMs };
        }
    }
}
