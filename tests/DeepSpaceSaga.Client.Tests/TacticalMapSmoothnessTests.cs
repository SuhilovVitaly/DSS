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
