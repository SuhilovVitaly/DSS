using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using System.Diagnostics;

namespace DeepSpaceSaga.Client.Tests;

public class TacticalMapFrameStateTests
{
    private static SnapshotPrediction Prediction(ulong sequence, long time, SimulationSpeed speed, double x = 0) =>
        new(new(new(sequence, time, speed,
            [new("player", x, 0, 1, 90), new("contact", x + 100, 0, 0, 0)], PlayerShipObjectId: "player"), 0),
            0, speed, 0);

    [Fact]
    public void Frame_update_uses_one_baseline_and_clock()
    {
        var updater = new TacticalMapStateUpdater(new LinearMotionPredictor());
        var prediction = Prediction(7, 1234, SimulationSpeed.Speed1);
        var frame = updater.Update(prediction, 987, .02);
        Assert.Same(prediction, frame.Prediction);
        Assert.Equal(987, frame.Timestamp);
        Assert.Equal(7UL, frame.SnapshotRevision);
        Assert.Equal(1234, frame.MotionTimeMs);
        Assert.All(frame.Objects, state => Assert.Contains(state.Source, prediction.BufferedSnapshot.Snapshot.Objects));
        Assert.Equal(frame.Objects[0].Pose, frame.PlayerFocus);
        long clockCalls = 0;
        var buffer = new SnapshotBuffer(() => ++clockCalls);
        buffer.Update(prediction.BufferedSnapshot.Snapshot);
        long before = clockCalls;
        var at = buffer.PredictionAt(Stopwatch.Frequency);
        Assert.NotNull(at);
        Assert.Equal(before, clockCalls);
    }

    [Fact]
    public void Frame_state_is_immutable_after_next_update()
    {
        var updater = new TacticalMapStateUpdater(new LinearMotionPredictor());
        var first = updater.Update(Prediction(1, 0, SimulationSpeed.Speed0), 100, .02);
        var poses = first.Objects.ToArray();
        var second = updater.Update(Prediction(2, 1000, SimulationSpeed.Speed0, 1000), 200, .02);
        Assert.Equal(poses, first.Objects);
        Assert.Equal(0, first.Objects[0].Pose.X);
        Assert.Equal(1000, second.Objects[0].Pose.X);
        Assert.True(second.FrameId > first.FrameId);
        var empty = updater.Update(null, 300, .02);
        Assert.Empty(empty.Objects);
        Assert.Equal(2, second.Objects.Length);
    }

    [Fact]
    public void Frame_update_preserves_pause_and_reconciliation()
    {
        var updater = new TacticalMapStateUpdater(new LinearMotionPredictor());
        var initial = updater.Update(Prediction(1, 0, SimulationSpeed.Speed1, 20), 1, .02);
        var paused = updater.Update(Prediction(2, 0, SimulationSpeed.Speed0, 50), 2, .02);
        Assert.Equal(initial.Objects[0].Pose.X, paused.Objects[0].Pose.X);
        var rebased = updater.Update(Prediction(3, 1000, SimulationSpeed.Speed0, 100), 3, .02);
        Assert.True(rebased.AuthoritativeRebase);
        Assert.Equal(100, rebased.Objects[0].Pose.X);
        var resumed = updater.Update(Prediction(4, 1000, SimulationSpeed.Speed1, 200), 4, .02);
        Assert.Equal(100, resumed.Objects[0].Pose.X);
        var settled = updater.Update(Prediction(4, 1000, SimulationSpeed.Speed1, 200), 5, .4);
        Assert.Equal(200, settled.Objects[0].Pose.X);
        Assert.False(settled.AuthoritativeRebase);
        Assert.Empty(settled.Diagnostics);
    }
}
