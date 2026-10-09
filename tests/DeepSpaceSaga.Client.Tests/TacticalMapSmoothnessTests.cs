using System.Collections.Immutable;
using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class TacticalMapSmoothnessTests
{
    [Fact]
    public async Task Legacy_approach_returned_geometry_is_drawn_from_cache()
    {
        var ship = new ObjectMotionSnapshot("player", 0, 0, 1, 90,
            ActiveEngineCommandType: NavigationComputerCommandTypes.Approach,
            NavigationTargetX: 1000, NavigationTargetY: 0,
            NavigationTargetSpeedKmS: 0, NavigationTargetDirectionDegrees: 0,
            TurnStepDegrees: 1, TurnStepIntervalMs: 250, TurnStepRemainingMs: 250);
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [ship], PlayerShipObjectId: "player"));
        using var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        screen.Render(surface.Canvas, 1280, 720);
        TacticalMapSnapshotDocument? captured = null;
        screen.SnapshotWriter = (document, _) => { captured = document; return "probe"; };
        screen.RequestTacticalMapSnapshot();
        await screen.SnapshotSaveTask;
        var drawn = Assert.Single(captured!.State.Trajectories, t => t.Kind == "navigation");
        Assert.True(drawn.Points.Count > 1);
        Assert.Equal((0d, 0d), (drawn.Points[0].X, drawn.Points[0].Y));
        Assert.InRange(Math.Abs(drawn.Points[^1].X - 1000), 0, 1);
        Assert.DoesNotContain(captured.State.Trajectories, t => t.Kind == "navigation-join");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Reconciliation_keeps_marker_joined_to_route_and_join_expires(bool resume)
    {
        long clock = 0;
        ObjectMotionSnapshot Ship(double x)
        {
            var ship = new ObjectMotionSnapshot("player", x, 500, 3, 270,
                ActiveEngineCommandType: NavigationComputerCommandTypes.Approach,
                NavigationTargetX: 0, NavigationTargetY: 0, NavigationPhase: ApproachLineCaptureMath.Phase);
            return ship with { ApproachRoute = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, 2, 10, 4)! };
        }
        var buffer = new SnapshotBuffer(() => clock);
        buffer.Update(new(1, 0, resume ? SimulationSpeed.Speed0 : SimulationSpeed.Speed1,
            [Ship(-1000)], PlayerShipObjectId: "player"));
        using var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => clock);
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        screen.Render(surface.Canvas, 1280, 720);
        var updated = Ship(-800);
        clock += Stopwatch.Frequency / 80;
        buffer.Update(new(2, 0, SimulationSpeed.Speed1, [updated], PlayerShipObjectId: "player"));
        screen.Render(surface.Canvas, 1280, 720);
        async Task<TacticalMapSnapshotDocument> Capture()
        {
            TacticalMapSnapshotDocument? result = null;
            screen.SnapshotWriter = (document, _) => { result = document; return "probe"; };
            screen.RequestTacticalMapSnapshot();
            await screen.SnapshotSaveTask;
            return result!;
        }
        var first = await Capture();
        var marker = Assert.Single(first.State.Objects).Rendered;
        var route = Assert.Single(first.State.Trajectories, t => t.Kind == "navigation");
        Assert.True(Math.Abs(marker.X - route.Points[0].X) > 1);
        var join = Assert.Single(first.State.Trajectories, t => t.Kind == "navigation-join");
        Assert.Equal((marker.X, marker.Y), (join.Points[0].X, join.Points[0].Y));
        Assert.Equal(route.Points[0], join.Points[^1]);
        var endpoint = ApproachLineCaptureMath.PredictPose(updated.ApproachRoute!, updated.ApproachRoute!.DurationMs);
        Assert.Contains(route.Points, p => p.X == endpoint.X && p.Y == endpoint.Y);
        for (int i = 0; i < 40; i++)
        {
            clock += Stopwatch.Frequency / 80;
            screen.Render(surface.Canvas, 1280, 720);
        }
        var settled = await Capture();
        Assert.DoesNotContain(settled.State.Trajectories, t => t.Kind == "navigation-join");
    }

    [Fact]
    public void Paused_time_advance_rebases_pose_camera_and_trail()
    {
        long clock = 0;
        var buffer = new SnapshotBuffer(() => clock);
        var obj = new ObjectMotionSnapshot("player", 0, 0, 1, 90);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [obj], PlayerShipObjectId: "player"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => clock);
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        screen.Render(surface.Canvas, 1280, 720);
        Assert.True(screen.GetObjectTrail("player").Count > 1);
        clock += Stopwatch.Frequency / 80;
        buffer.Update(new(2, 3000000, SimulationSpeed.Speed0,
            [obj with { X = 100000 }], PlayerShipObjectId: "player", SimulationTimeMs: 10000));
        screen.Render(surface.Canvas, 1280, 720);
        Assert.Equal(100000, screen.RenderStates[0].Pose.X);
        Assert.Equal(100000, screen.CameraFocusX);
        Assert.Equal(100000, Assert.Single(screen.GetObjectTrail("player")).X);

        // Repeated paused publications at the same physical time retain the anchor.
        buffer.Update(new(3, 3000001, SimulationSpeed.Speed0,
            [obj with { X = 100001 }], PlayerShipObjectId: "player", SimulationTimeMs: 10000));
        screen.Render(surface.Canvas, 1280, 720);
        Assert.Equal(100000, screen.RenderStates[0].Pose.X);

        // Membership still follows the current publication at the same paused time.
        buffer.Update(new(4, 3000001, SimulationSpeed.Speed0,
            [obj with { ObjectId = "replacement", X = 200000 }], SimulationTimeMs: 10000));
        screen.Render(surface.Canvas, 1280, 720);
        Assert.Equal("replacement", Assert.Single(screen.RenderStates).Pose.ObjectId);
        Assert.Empty(screen.GetObjectTrail("player"));

        buffer.Update(new(5, 3000001, SimulationSpeed.Speed0,
            [obj with { X = 100000 }], PlayerShipObjectId: "player", SimulationTimeMs: 10000));
        screen.Render(surface.Canvas, 1280, 720);
        buffer.Update(new(6, 3000001, SimulationSpeed.Speed1,
            [obj with { X = 100000 }], PlayerShipObjectId: "player", SimulationTimeMs: 10000));
        clock += Stopwatch.Frequency / 80;
        screen.Render(surface.Canvas, 1280, 720);
        Assert.InRange(screen.RenderStates[0].Pose.X, 100000, 100001);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Snapshot_during_reconciliation_preserves_the_in_progress_visual_pose(bool baselineIncludesCorrection)
    {
        long clock = 0;
        var buffer = new SnapshotBuffer(() => clock);
        var controlBuffer = new SnapshotBuffer(() => clock);
        var predictor = new LinearMotionPredictor();
        var obj = new ObjectMotionSnapshot("player", 0, 0, 1, 90);
        var initial = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed4, [obj], PlayerShipObjectId: "player");
        buffer.Update(initial);
        controlBuffer.Update(initial);
        var screen = new GameSessionScreen(buffer, predictor, timestampProvider: () => clock);
        var control = new GameSessionScreen(controlBuffer, predictor, timestampProvider: () => clock);
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        void Frame()
        {
            clock += Stopwatch.Frequency / 80;
            screen.Render(surface.Canvas, 1280, 720);
            control.Render(surface.Canvas, 1280, 720);
        }
        for (int i = 0; i < 80; i++) Frame();
        var ahead = new AuthoritativeSnapshot(2, 106000, SimulationSpeed.Speed4,
            [predictor.Predict(obj, 106000)], PlayerShipObjectId: "player");
        buffer.Update(ahead);
        controlBuffer.Update(ahead);
        Frame(); // Both begin smoothing a 60-world-unit offset.
        for (int i = 0; i < 8; i++) Frame();

        long time = ahead.MotionTimeMs + buffer.EffectivePredictionDeltaMs;
        double progress = (9.0 / 80) / .3; // Correction age on the next rendered frame.
        double remainingOffset = 60 * (1 - progress * progress * (3 - 2 * progress));
        if (!baselineIncludesCorrection) time += 4000;
        var next = predictor.Predict(obj, time);
        if (baselineIncludesCorrection) next = next with { X = next.X - remainingOffset };
        buffer.Update(new(3, time, SimulationSpeed.Speed4, [next], PlayerShipObjectId: "player"));
        Frame();
        Assert.Equal(control.RenderStates[0].Predicted.X, screen.RenderStates[0].Predicted.X, 6);
        for (int i = 0; i < 40; i++) Frame();
        Assert.Equal(predictor.Predict(next, buffer.EffectivePredictionDeltaMs).X,
            screen.RenderStates[0].Predicted.X, 6);
    }

    [Fact]
    public void Speed100_prediction_preserves_submillisecond_real_time()
    {
        long clock = 0;
        var buffer = new SnapshotBuffer(() => clock);
        buffer.Update(new(1, 0, SimulationSpeed.Speed4, []));
        clock = Stopwatch.Frequency / 80; // 12.5 real ms at 80 Hz
        Assert.Equal(1250, buffer.EffectivePredictionDeltaMs);
        clock = Stopwatch.Frequency * 3 / 80;
        Assert.Equal(3750, buffer.EffectivePredictionDeltaMs);
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(15, false)]
    [InlineData(60, false)]
    [InlineData(60, true)]
    public void All_500_contacts_keep_continuous_motion_when_snapshots_advance_time(int skewRealMs, bool skipSnapshotFrame)
    {
        long clock = 0;
        var buffer = new SnapshotBuffer(() => clock);
        var predictor = new LinearMotionPredictor();
        var objects = Enumerable.Range(0, 500)
            .Select(i => new ObjectMotionSnapshot("object-" + i, i * 100, i * 50, 1 + i % 20, 90))
            .ToImmutableArray();
        buffer.Update(new(1, 0, SimulationSpeed.Speed4, objects, PlayerShipObjectId: objects[0].ObjectId));
        var screen = new GameSessionScreen(buffer, predictor, timestampProvider: () => clock);
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        void Frame()
        {
            clock += Stopwatch.Frequency / 80;
            screen.Render(surface.Canvas, 1280, 720);
        }
        for (int i = 0; i < 80; i++) Frame();
        var before = screen.RenderStates.Select(s => s.Predicted.X).ToArray();
        long previousTime = buffer.LatestPrediction!.BufferedSnapshot.Snapshot.MotionTimeMs + buffer.EffectivePredictionDeltaMs;
        void Publish(ulong sequence, long time) => buffer.Update(new(sequence, time, SimulationSpeed.Speed4,
            objects.Select(o => predictor.Predict(o, time)).ToImmutableArray(), PlayerShipObjectId: objects[0].ObjectId));
        if (skipSnapshotFrame) Publish(2, previousTime + skewRealMs * 50);
        Publish(3, previousTime + skewRealMs * 100);
        Frame();
        for (int i = 0; i < objects.Length; i++)
        {
            double normalStep = objects[i].SpeedKmS * 10 * 1.25;
            double actualStep = screen.RenderStates[i].Predicted.X - before[i];
            Assert.InRange(actualStep, normalStep * .999, normalStep * 1.001);
        }
        for (int i = 0; i < 40; i++) Frame();
        var prediction = buffer.LatestPrediction!;
        for (int i = 0; i < objects.Length; i++)
        {
            var expected = predictor.Predict(prediction.BufferedSnapshot.Snapshot.Objects[i], prediction.EffectivePredictionDeltaMs);
            Assert.Equal(expected.X, screen.RenderStates[i].Predicted.X, 6);
        }
    }
}
