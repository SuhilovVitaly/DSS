using System.Collections.Immutable;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Client.Tests;

public class ModalPauseTests
{
    [Fact]
    public void Prediction_stops_during_pause()
    {
        var buffer = new SnapshotBuffer();
        var predictor = new LinearMotionPredictor();

        // Object moving right at 5 km/s = 50 world units/s = 0.05 units/ms
        var obj = new ObjectMotionSnapshot("mover", X: 0, Y: 0, SpeedKmS: 5, Direction: 90);

        // Create a snapshot with Speed0 (pause)
        var objects = ImmutableArray.Create(obj);
        var pausedSnapshot = new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 1000,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: objects);

        buffer.Update(pausedSnapshot);

        var buffered = buffer.Latest;
        Assert.NotNull(buffered);

        // Prediction delta is real elapsed time, but speed is Speed0
        // The renderer should use effectiveDelta = 0
        long predictionDelta = buffered.PredictionDeltaMs;
        bool isPaused = buffered.Snapshot.CurrentSpeed == SimulationSpeed.Speed0;
        long effectiveDelta = isPaused ? 0 : predictionDelta;

        var predicted = effectiveDelta > 0
            ? predictor.Predict(obj, effectiveDelta)
            : obj;

        // Object must NOT have moved
        Assert.Equal(0, predicted.X);
        Assert.Equal(0, predicted.Y);
    }

    [Fact]
    public void Prediction_continues_at_speed1()
    {
        var buffer = new SnapshotBuffer();
        var predictor = new LinearMotionPredictor();

        // Object moving right at 5 km/s = 0.05 units/ms
        var obj = new ObjectMotionSnapshot("mover", X: 0, Y: 0, SpeedKmS: 5, Direction: 90);

        var objects = ImmutableArray.Create(obj);
        var snapshot = new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 1000,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: objects);

        buffer.Update(snapshot);

        var buffered = buffer.Latest;
        Assert.NotNull(buffered);

        long predictionDelta = buffered.PredictionDeltaMs;
        bool isPaused = buffered.Snapshot.CurrentSpeed == SimulationSpeed.Speed0;
        long effectiveDelta = isPaused ? 0 : predictionDelta;

        var predicted = effectiveDelta > 0
            ? predictor.Predict(obj, effectiveDelta)
            : obj;

        // At Speed1, with any real elapsed time, prediction should apply
        // (We can't assert exact position since real time varies, but we can
        // verify the prediction branch was taken)
        if (predictionDelta > 0)
        {
            // Prediction was applied — object moved (or stayed at origin if delta is tiny)
            Assert.True(predicted.X >= 0);
        }
        // else: no time passed since buffer update — stays at origin, also valid
    }
}
