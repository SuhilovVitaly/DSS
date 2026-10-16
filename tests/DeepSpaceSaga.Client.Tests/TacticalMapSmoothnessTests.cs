using System.Collections.Immutable;
using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class TacticalMapSmoothnessTests
{
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
        long aheadTime = buffer.EffectivePredictionDeltaMs + 6000;
        var ahead = new AuthoritativeSnapshot(2, aheadTime, SimulationSpeed.Speed4,
            [predictor.Predict(obj, aheadTime)], PlayerShipObjectId: "player");
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
        // Quarter pace can quantize the two baselines one physical millisecond apart.
        Assert.InRange(Math.Abs(control.RenderStates[0].Predicted.X - screen.RenderStates[0].Predicted.X), 0, .010001);
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
        Assert.Equal(312, buffer.EffectivePredictionDeltaMs);
        clock = Stopwatch.Frequency * 3 / 80;
        Assert.Equal(937, buffer.EffectivePredictionDeltaMs);
    }

    [Fact]
    public void Hostile_ship_keeps_moving_between_one_second_snapshots()
    {
        long clock = 0;
        var buffer = new SnapshotBuffer(() => clock);
        var player = new ObjectMotionSnapshot("player", 0, 0, 0, 0,
            RenderObjectType: SpaceObjectType.PlayerShip);
        var segment = new HostileMotionSegment(0, 100, 0, 100, 0, 2, 90, 0, 0);
        var hostile = new ObjectMotionSnapshot("mukha", 100, 0, 2, 90,
            RenderObjectType: SpaceObjectType.NpcShip,
            HostileCombat: new("player", "engine", "weapon", 50, true,
                HostileCombatPhase.Pursuit, segment));
        buffer.Update(new(1, 0, SimulationSpeed.Speed1, [player, hostile], PlayerShipObjectId: "player"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => clock);
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));

        foreach (int realMs in new[] { 50, 150, 300, 600, 900 })
        {
            clock = Stopwatch.Frequency * realMs / 1000;
            screen.Render(surface.Canvas, 1280, 720);
            Assert.Equal(100 + 2 * 10 * (realMs / 4) / 1000.0,
                screen.RenderStates.Single(s => s.Source.ObjectId == "mukha").Predicted.X, 6);
        }
        Assert.Equal(100, hostile.X);
        Assert.Equal(0, hostile.HostileCombat!.Segment.ElapsedMs);
    }

    [Fact]
    public void Hostile_visual_prediction_keeps_curvature_between_snapshots()
    {
        long clock = 0;
        var buffer = new SnapshotBuffer(() => clock);
        var segment = new HostileMotionSegment(0, 100, 0, 0, 0, 3, 0, 0, 12);
        var hostile = new ObjectMotionSnapshot("mukha", 0, 0, 3, 0,
            RenderObjectType: SpaceObjectType.NpcShip,
            HostileCombat: new("player", "engine", "weapon", 50, true, HostileCombatPhase.FlankApproach, segment));
        buffer.Update(new(1, 0, SimulationSpeed.Speed1, [hostile]));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => clock);
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        foreach (int ms in new[] { 100, 250, 500, 750, 1000 })
        {
            clock = Stopwatch.Frequency * ms / 1000;
            screen.Render(surface.Canvas, 1280, 720);
            var pose = Assert.Single(screen.RenderStates).Predicted;
            double motionSeconds = (ms / 4) / 1000.0;
            double angle = 12 * Math.PI / 180 * motionSeconds;
            double radius = 30 / (12 * Math.PI / 180);
            Assert.Equal(radius * (1 - Math.Cos(angle)), pose.X, 5);
            Assert.Equal(-radius * Math.Sin(angle), pose.Y, 5);
            Assert.Equal(12 * motionSeconds, pose.Direction, 5);
        }
        Assert.Equal(100, hostile.HostileCombat!.Segment.DurationMs);
    }

    [Fact]
    public void Hostile_visual_pose_is_continuous_when_next_segment_arrives()
    {
        long clock = 0;
        var buffer = new SnapshotBuffer(() => clock);
        var predictor = new LinearMotionPredictor();
        var player = new ObjectMotionSnapshot("player", 0, 0, 0, 0,
            RenderObjectType: SpaceObjectType.PlayerShip);
        var segment = new HostileMotionSegment(0, 100, 0, 100, 0, 2, 90, 0, 0);
        var hostile = new ObjectMotionSnapshot("shmel", 100, 0, 2, 90,
            RenderObjectType: SpaceObjectType.NpcShip,
            HostileCombat: new("player", "engine", "weapon", 50, true,
                HostileCombatPhase.FlankApproach, segment));
        buffer.Update(new(1, 0, SimulationSpeed.Speed1, [player, hostile], PlayerShipObjectId: "player"));
        var screen = new GameSessionScreen(buffer, predictor, timestampProvider: () => clock);
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        clock = Stopwatch.Frequency;
        screen.Render(surface.Canvas, 1280, 720);
        double before = screen.RenderStates.Single(s => s.Source.ObjectId == "shmel").Predicted.X;

        var next = hostile with
        {
            X = before,
            HostileCombat = hostile.HostileCombat! with
            {
                Segment = segment with { EpochMs = 1000, StartX = before, ElapsedMs = 0 }
            }
        };
        buffer.Update(new(2, 1000, SimulationSpeed.Speed1, [player, next], PlayerShipObjectId: "player"));
        screen.Render(surface.Canvas, 1280, 720);
        Assert.Equal(before, screen.RenderStates.Single(s => s.Source.ObjectId == "shmel").Predicted.X, 6);
        clock += Stopwatch.Frequency / 5;
        screen.Render(surface.Canvas, 1280, 720);
        Assert.Equal(before + 1, screen.RenderStates.Single(s => s.Source.ObjectId == "shmel").Predicted.X, 6);
    }

    [Fact]
    public void Hostile_target_of_torpedo_reconciles_visual_pose_without_jumping()
    {
        long clock = 0;
        var buffer = new SnapshotBuffer(() => clock);
        var player = new ObjectMotionSnapshot("player", 0, 0, 0, 0,
            RenderObjectType: SpaceObjectType.PlayerShip);
        var segment = new HostileMotionSegment(0, 100, 0, 100, 0, 2, 90, 0, 0);
        var hostile = new ObjectMotionSnapshot("mukha", 100, 0, 2, 90,
            RenderObjectType: SpaceObjectType.NpcShip,
            HostileCombat: new("player", "engine", "weapon", 50, true,
                HostileCombatPhase.Pursuit, segment));
        var torpedo = new ObjectMotionSnapshot("torpedo", 0, 0, 3, 90,
            RenderObjectType: SpaceObjectType.Missile,
            Torpedo: new("player", "launcher", "mukha", 0, 3, 90, 150, 0,
                new(0, 1, TorpedoRoutePhase.Straight, false, [new(0, 0, 90, 3, 0, 1000)])));
        buffer.Update(new(1, 0, SimulationSpeed.Speed1, [player, hostile, torpedo], PlayerShipObjectId: "player"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => clock);
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        clock = Stopwatch.Frequency;
        screen.Render(surface.Canvas, 1280, 720);
        double before = screen.RenderStates.Single(s => s.Source.ObjectId == "mukha").Predicted.X;

        var next = hostile with
        {
            X = before + 10,
            HostileCombat = hostile.HostileCombat! with
            {
                Segment = segment with { EpochMs = 1000, StartX = before + 10, ElapsedMs = 0 }
            }
        };
        buffer.Update(new(2, 1000, SimulationSpeed.Speed1, [player, next, torpedo], PlayerShipObjectId: "player"));
        screen.Render(surface.Canvas, 1280, 720);
        Assert.Equal(before, screen.RenderStates.Single(s => s.Source.ObjectId == "mukha").Predicted.X, 6);
    }

    [Theory]
    [InlineData(200, false)]
    [InlineData(1500, false)]
    [InlineData(6000, false)]
    [InlineData(6000, true)]
    public void All_500_contacts_keep_continuous_motion_when_snapshots_advance_time(int skewMotionMs, bool skipSnapshotFrame)
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
        if (skipSnapshotFrame) Publish(2, previousTime + skewMotionMs / 2);
        Publish(3, previousTime + skewMotionMs);
        Frame();
        for (int i = 0; i < objects.Length; i++)
        {
            double normalStep = objects[i].SpeedKmS * 10 * .3125;
            double actualStep = screen.RenderStates[i].Predicted.X - before[i];
            // A physical timestamp has integer-millisecond precision.
            double quantization = objects[i].SpeedKmS * .01;
            Assert.InRange(actualStep, normalStep - quantization, normalStep + quantization);
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
