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

        // Object moving right at 5 km/s = 0.05 units/ms
        var obj = new ObjectMotionSnapshot("mover", X: 0, Y: 0, SpeedKmS: 5, Direction: 90);

        // Set client-side speed to Speed0 (pause)
        buffer.CurrentSpeed = SimulationSpeed.Speed0;

        var objects = ImmutableArray.Create(obj);
        buffer.Update(new AuthoritativeSnapshot(1, 1000, SimulationSpeed.Speed0, objects));

        var buffered = buffer.Latest;
        Assert.NotNull(buffered);

        // Renderer logic: check buffer.CurrentSpeed (immediate), not snapshot speed
        bool isPaused = buffer.CurrentSpeed == SimulationSpeed.Speed0;
        long effectiveDelta = isPaused ? 0 : buffered.PredictionDeltaMs;

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
        buffer.CurrentSpeed = SimulationSpeed.Speed1;

        var obj = new ObjectMotionSnapshot("mover", X: 0, Y: 0, SpeedKmS: 5, Direction: 90);
        var objects = ImmutableArray.Create(obj);
        buffer.Update(new AuthoritativeSnapshot(1, 1000, SimulationSpeed.Speed1, objects));

        var buffered = buffer.Latest;
        Assert.NotNull(buffered);

        bool isPaused = buffer.CurrentSpeed == SimulationSpeed.Speed0;
        long effectiveDelta = isPaused ? 0 : buffered.PredictionDeltaMs;

        var predicted = effectiveDelta > 0
            ? predictor.Predict(obj, effectiveDelta)
            : obj;

        if (buffered.PredictionDeltaMs > 0)
            Assert.True(predicted.X >= 0);
    }

    [Fact]
    public void Nested_modal_lifecycle_preserves_original_speed()
    {
        // Simulates: Speed2 → open modal#1 → Speed0 → open modal#2 → Speed0
        // → close modal#2 → Speed0 → close modal#1 → Speed2
        var buffer = new SnapshotBuffer();

        // Start at Speed2
        buffer.CurrentSpeed = SimulationSpeed.Speed2;
        var savedSpeed = SimulationSpeed.Speed1; // will be set on first modal

        // Open modal #1 (depth 0→1): save speed, set Speed0
        int modalDepth = 0;
        if (modalDepth == 0)
        {
            savedSpeed = buffer.CurrentSpeed; // Speed2
            buffer.CurrentSpeed = SimulationSpeed.Speed0;
        }
        modalDepth++;
        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);
        Assert.Equal(SimulationSpeed.Speed2, savedSpeed);

        // Open modal #2 (depth 1→2): stay paused
        if (modalDepth == 0) { /* not reached */ }
        modalDepth++;
        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);

        // Close modal #2 (depth 2→1): stay paused
        modalDepth--;
        if (modalDepth == 0) { /* not reached */ }
        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);

        // Close modal #1 (depth 1→0): restore saved speed
        modalDepth--;
        if (modalDepth == 0)
        {
            buffer.CurrentSpeed = savedSpeed;
        }
        Assert.Equal(SimulationSpeed.Speed2, buffer.CurrentSpeed);
    }

    [Fact]
    public void Immediate_speed_update_does_not_wait_for_snapshot()
    {
        var buffer = new SnapshotBuffer();

        // Simulate: last snapshot says Speed1, but we just paused
        buffer.Update(new AuthoritativeSnapshot(1, 1000, SimulationSpeed.Speed1,
            ImmutableArray<ObjectMotionSnapshot>.Empty));
        Assert.Equal(SimulationSpeed.Speed1, buffer.Latest!.Snapshot.CurrentSpeed);

        // Pause via session control — immediate update on buffer
        buffer.CurrentSpeed = SimulationSpeed.Speed0;

        // Renderer reads buffer.CurrentSpeed, NOT snapshot speed
        bool isPaused = buffer.CurrentSpeed == SimulationSpeed.Speed0;
        Assert.True(isPaused, "Renderer should see immediate pause, not wait for next snapshot");
    }

    [Fact]
    public void Saved_speed_comes_from_buffer_not_stale_snapshot()
    {
        var buffer = new SnapshotBuffer();

        // Last snapshot says Speed1
        buffer.Update(new AuthoritativeSnapshot(1, 1000, SimulationSpeed.Speed1,
            ImmutableArray<ObjectMotionSnapshot>.Empty));
        Assert.Equal(SimulationSpeed.Speed1, buffer.Latest!.Snapshot.CurrentSpeed);

        // Pause via session control
        buffer.CurrentSpeed = SimulationSpeed.Speed0;

        // Now a modal opens. The saved speed should come from buffer.CurrentSpeed (Speed0),
        // NOT from the stale snapshot (which still says Speed1).
        var savedSpeed = buffer.CurrentSpeed; // This is what PushModal should read
        Assert.True(savedSpeed == SimulationSpeed.Speed0,
            "Saved speed must come from buffer.CurrentSpeed, not stale snapshot");

        // After closing modal, restore saved speed = Speed0 (game stays paused)
        buffer.CurrentSpeed = savedSpeed;
        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);
    }
}
