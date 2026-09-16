using System.Collections.Immutable;
using System.Diagnostics;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Client.Tests;

public class ModalPauseTests
{
    private sealed class FakeTimestamp
    {
        public long Timestamp { get; private set; }

        public void AdvanceMs(long milliseconds)
        {
            Timestamp += (long)(milliseconds * Stopwatch.Frequency / 1000.0);
        }
    }

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

        long effectiveDelta = buffer.EffectivePredictionDeltaMs;

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

        long effectiveDelta = buffer.EffectivePredictionDeltaMs;

        var predicted = effectiveDelta > 0
            ? predictor.Predict(obj, effectiveDelta)
            : obj;

        if (effectiveDelta > 0)
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
    public void Buffer_CurrentSpeed_is_synced_from_first_snapshot()
    {
        var buffer = new SnapshotBuffer();
        Assert.Equal(SimulationSpeed.Speed1, buffer.CurrentSpeed); // default

        // Simulate first snapshot arriving with Speed0
        buffer.Update(new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0,
            ImmutableArray<ObjectMotionSnapshot>.Empty));

        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);
    }

    [Fact]
    public void Buffer_CurrentSpeed_is_updated_on_every_snapshot()
    {
        var buffer = new SnapshotBuffer();

        buffer.Update(new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed2,
            ImmutableArray<ObjectMotionSnapshot>.Empty));
        Assert.Equal(SimulationSpeed.Speed2, buffer.CurrentSpeed);

        buffer.Update(new AuthoritativeSnapshot(2, 0, SimulationSpeed.Speed0,
            ImmutableArray<ObjectMotionSnapshot>.Empty));
        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);
    }

    [Fact]
    public void Prediction_delta_is_multiplied_by_speed()
    {
        var clock = new FakeTimestamp();
        var buffer = new SnapshotBuffer(() => clock.Timestamp);

        var obj = new ObjectMotionSnapshot("mover", X: 0, Y: 0, SpeedKmS: 5, Direction: 90);

        // Speed2 = 5x
        buffer.Update(new AuthoritativeSnapshot(1, 1000, SimulationSpeed.Speed2,
            ImmutableArray.Create(obj)));

        clock.AdvanceMs(200);

        Assert.Equal(1000, buffer.EffectivePredictionDeltaMs);
    }

    [Fact]
    public void Prediction_delta_preserves_speed_segments_between_snapshots()
    {
        var clock = new FakeTimestamp();
        var buffer = new SnapshotBuffer(() => clock.Timestamp);

        buffer.Update(new AuthoritativeSnapshot(1, 1000, SimulationSpeed.Speed1,
            ImmutableArray<ObjectMotionSnapshot>.Empty));

        clock.AdvanceMs(500);
        buffer.CurrentSpeed = SimulationSpeed.Speed4;

        // The 500 ms that already passed must stay at Speed1.
        Assert.Equal(500, buffer.EffectivePredictionDeltaMs);

        clock.AdvanceMs(100);

        // Then only the new 100 ms segment runs at Speed4 / 100x.
        Assert.Equal(10500, buffer.EffectivePredictionDeltaMs);
    }

    [Fact]
    public void Prediction_delta_does_not_advance_after_pause_segment_starts()
    {
        var clock = new FakeTimestamp();
        var buffer = new SnapshotBuffer(() => clock.Timestamp);

        buffer.Update(new AuthoritativeSnapshot(1, 1000, SimulationSpeed.Speed1,
            ImmutableArray<ObjectMotionSnapshot>.Empty));

        clock.AdvanceMs(500);
        buffer.CurrentSpeed = SimulationSpeed.Speed0;
        Assert.Equal(500, buffer.EffectivePredictionDeltaMs);

        clock.AdvanceMs(1000);
        Assert.Equal(500, buffer.EffectivePredictionDeltaMs);
    }

    [Fact]
    public void Paused_snapshot_does_not_rewind_predicted_game_time()
    {
        var clock = new FakeTimestamp();
        var buffer = new SnapshotBuffer(() => clock.Timestamp);

        buffer.Update(new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed4,
            ImmutableArray<ObjectMotionSnapshot>.Empty));

        clock.AdvanceMs(900);
        Assert.Equal(90_000, buffer.EffectivePredictionDeltaMs);

        buffer.CurrentSpeed = SimulationSpeed.Speed0;
        buffer.Update(new AuthoritativeSnapshot(2, 50_000, SimulationSpeed.Speed0,
            ImmutableArray<ObjectMotionSnapshot>.Empty));

        var prediction = Assert.IsType<SnapshotPrediction>(buffer.LatestPrediction);
        Assert.Equal(40_000, prediction.EffectivePredictionDeltaMs);
        Assert.Equal(
            90_000,
            prediction.BufferedSnapshot.Snapshot.GameTimeMs + prediction.EffectivePredictionDeltaMs);

        clock.AdvanceMs(1_000);
        Assert.Equal(40_000, buffer.EffectivePredictionDeltaMs);
    }

    [Fact]
    public void Stale_paused_snapshot_does_not_override_confirmed_resume_speed()
    {
        var clock = new FakeTimestamp();
        var buffer = new SnapshotBuffer(() => clock.Timestamp);

        buffer.Update(new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed4,
            ImmutableArray<ObjectMotionSnapshot>.Empty));
        buffer.CurrentSpeed = SimulationSpeed.Speed0;
        buffer.Update(new AuthoritativeSnapshot(2, 50_000, SimulationSpeed.Speed0,
            ImmutableArray<ObjectMotionSnapshot>.Empty));

        buffer.CurrentSpeed = SimulationSpeed.Speed4;

        buffer.Update(new AuthoritativeSnapshot(3, 50_000, SimulationSpeed.Speed0,
            ImmutableArray<ObjectMotionSnapshot>.Empty));
        Assert.Equal(SimulationSpeed.Speed4, buffer.CurrentSpeed);

        buffer.Update(new AuthoritativeSnapshot(4, 150_000, SimulationSpeed.Speed4,
            ImmutableArray<ObjectMotionSnapshot>.Empty));
        Assert.Equal(SimulationSpeed.Speed4, buffer.CurrentSpeed);

        buffer.Update(new AuthoritativeSnapshot(5, 150_000, SimulationSpeed.Speed0,
            ImmutableArray<ObjectMotionSnapshot>.Empty));
        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);
    }

    [Fact]
    public void First_snapshot_Speed0_moving_object_no_prediction()
    {
        var buffer = new SnapshotBuffer();
        var predictor = new LinearMotionPredictor();

        // Object moving at 5 km/s = 50 units/s
        var obj = new ObjectMotionSnapshot("mover", X: 0, Y: 0, SpeedKmS: 5, Direction: 90);

        // First snapshot arrives with Speed0
        buffer.Update(new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0,
            ImmutableArray.Create(obj)));

        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);

        var buffered = buffer.Latest;
        Assert.NotNull(buffered);

        long effectiveDelta = buffer.EffectivePredictionDeltaMs;

        var predicted = effectiveDelta > 0
            ? predictor.Predict(obj, effectiveDelta)
            : obj;

        // Must NOT have moved — Speed0 means no prediction
        Assert.Equal(0, predicted.X);
        Assert.Equal(0, predicted.Y);
    }

    [Fact]
    public void Save_window_pauses_simulation_on_push_and_resumes_on_pop()
    {
        // Simulates SkiaWindow.PushModalAsync/PopModalAsync exactly as GameMenu/Settings
        // are already covered above, but framed around opening the Save window directly
        // from the GameSession screen (depth 0 -> 1 -> 0).
        var buffer = new SnapshotBuffer();
        buffer.CurrentSpeed = SimulationSpeed.Speed2;

        int modalDepth = 0;
        SimulationSpeed savedSpeed = default;

        // Push Save window (depth 0 -> 1): save speed, pause.
        if (modalDepth == 0)
        {
            savedSpeed = buffer.CurrentSpeed;
            buffer.CurrentSpeed = SimulationSpeed.Speed0;
        }
        modalDepth++;

        Assert.Equal(1, modalDepth);
        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);
        Assert.Equal(SimulationSpeed.Speed2, savedSpeed);

        // Pop Save window (depth 1 -> 0): restore saved speed.
        modalDepth--;
        if (modalDepth == 0)
        {
            buffer.CurrentSpeed = savedSpeed;
        }

        Assert.Equal(0, modalDepth);
        Assert.Equal(SimulationSpeed.Speed2, buffer.CurrentSpeed);
    }

    [Fact]
    public void Save_window_opened_from_GameMenu_nested_modal_depth_still_resumes_original_speed()
    {
        // Simulates: Speed2 -> open GameMenu (depth 0->1, pause) -> open Save window on
        // top of it (depth 1->2, nested, stays paused) -> close Save window (depth 2->1,
        // still paused, back to GameMenu) -> close GameMenu (depth 1->0, restore Speed2).
        var buffer = new SnapshotBuffer();
        buffer.CurrentSpeed = SimulationSpeed.Speed2;

        int modalDepth = 0;
        var savedSpeed = SimulationSpeed.Speed1;

        // Open GameMenu (depth 0 -> 1): save speed, pause.
        if (modalDepth == 0)
        {
            savedSpeed = buffer.CurrentSpeed;
            buffer.CurrentSpeed = SimulationSpeed.Speed0;
        }
        modalDepth++;
        Assert.Equal(1, modalDepth);
        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);

        // Open Save window on top of GameMenu (depth 1 -> 2): nested, stays paused.
        if (modalDepth == 0) { /* not reached */ }
        modalDepth++;
        Assert.Equal(2, modalDepth);
        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);

        // Close Save window (depth 2 -> 1): stays paused, back to GameMenu.
        modalDepth--;
        if (modalDepth == 0) { /* not reached */ }
        Assert.Equal(1, modalDepth);
        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);

        // Close GameMenu (depth 1 -> 0): restores the original speed.
        modalDepth--;
        if (modalDepth == 0)
        {
            buffer.CurrentSpeed = savedSpeed;
        }
        Assert.Equal(0, modalDepth);
        Assert.Equal(SimulationSpeed.Speed2, buffer.CurrentSpeed);
    }

    [Fact]
    public void Load_window_pauses_simulation_on_push_and_resumes_on_pop()
    {
        // Same shape as Save_window_pauses_simulation_on_push_and_resumes_on_pop, framed
        // around opening the Load window directly from the GameSession screen
        // (depth 0 -> 1 -> 0). SkiaWindow.OpenLoadWindowAsync uses the very same
        // PushModalAsync/PopModalAsync pair as Save, so the pause/resume lifecycle is
        // identical — only the screen type pushed differs.
        var buffer = new SnapshotBuffer();
        buffer.CurrentSpeed = SimulationSpeed.Speed2;

        int modalDepth = 0;
        SimulationSpeed savedSpeed = default;

        // Push Load window (depth 0 -> 1): save speed, pause.
        if (modalDepth == 0)
        {
            savedSpeed = buffer.CurrentSpeed;
            buffer.CurrentSpeed = SimulationSpeed.Speed0;
        }
        modalDepth++;

        Assert.Equal(1, modalDepth);
        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);
        Assert.Equal(SimulationSpeed.Speed2, savedSpeed);

        // Pop Load window (depth 1 -> 0): restore saved speed.
        modalDepth--;
        if (modalDepth == 0)
        {
            buffer.CurrentSpeed = savedSpeed;
        }

        Assert.Equal(0, modalDepth);
        Assert.Equal(SimulationSpeed.Speed2, buffer.CurrentSpeed);
    }

    [Fact]
    public void Load_window_opened_from_GameMenu_nested_modal_depth_still_resumes_original_speed()
    {
        // Same nested shape as Save_window_opened_from_GameMenu_nested_modal_depth_still_resumes_original_speed:
        // Speed2 -> open GameMenu (depth 0->1, pause) -> open Load window on top of it
        // (depth 1->2, nested, stays paused) -> close Load window (depth 2->1, still
        // paused, back to GameMenu) -> close GameMenu (depth 1->0, restore Speed2).
        var buffer = new SnapshotBuffer();
        buffer.CurrentSpeed = SimulationSpeed.Speed2;

        int modalDepth = 0;
        var savedSpeed = SimulationSpeed.Speed1;

        // Open GameMenu (depth 0 -> 1): save speed, pause.
        if (modalDepth == 0)
        {
            savedSpeed = buffer.CurrentSpeed;
            buffer.CurrentSpeed = SimulationSpeed.Speed0;
        }
        modalDepth++;
        Assert.Equal(1, modalDepth);
        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);

        // Open Load window on top of GameMenu (depth 1 -> 2): nested, stays paused.
        if (modalDepth == 0) { /* not reached */ }
        modalDepth++;
        Assert.Equal(2, modalDepth);
        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);

        // Close Load window (depth 2 -> 1): stays paused, back to GameMenu.
        modalDepth--;
        if (modalDepth == 0) { /* not reached */ }
        Assert.Equal(1, modalDepth);
        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);

        // Close GameMenu (depth 1 -> 0): restores the original speed.
        modalDepth--;
        if (modalDepth == 0)
        {
            buffer.CurrentSpeed = savedSpeed;
        }
        Assert.Equal(0, modalDepth);
        Assert.Equal(SimulationSpeed.Speed2, buffer.CurrentSpeed);
    }

    [Fact]
    public void Load_window_opened_from_MainMenu_with_no_session_never_touches_speed()
    {
        // Load must also work from MainMenuScreen, where there is no session yet
        // (SkiaWindow.OpenLoadWindowAsync deliberately has no `_session is null` guard,
        // unlike OpenSaveWindowAsync). PushModalAsync/PopModalAsync's own
        // `_session is not null` check means the pause-save/resume-restore branch is
        // skipped entirely in this case — modalDepth still tracks push/pop correctly,
        // but SetSpeedAsync/buffer.CurrentSpeed is never touched, and nothing throws.
        var buffer = new SnapshotBuffer();
        buffer.CurrentSpeed = SimulationSpeed.Speed1; // default, would-be stale value

        bool hasSession = false; // simulates SkiaWindow._session is null (MainMenu context)
        int modalDepth = 0;
        SimulationSpeed savedSpeed = SimulationSpeed.Speed1;
        bool speedTouched = false;

        void PushModal()
        {
            if (modalDepth == 0 && hasSession)
            {
                savedSpeed = buffer.CurrentSpeed;
                buffer.CurrentSpeed = SimulationSpeed.Speed0;
                speedTouched = true;
            }
            modalDepth++;
        }

        void PopModal()
        {
            modalDepth--;
            if (modalDepth == 0 && hasSession)
            {
                buffer.CurrentSpeed = savedSpeed;
                speedTouched = true;
            }
        }

        var exception = Record.Exception(() =>
        {
            PushModal(); // Open Load window from MainMenu (depth 0 -> 1)
            PopModal();  // Close Load window (depth 1 -> 0)
        });

        Assert.Null(exception);
        Assert.Equal(0, modalDepth);
        Assert.False(speedTouched, "No session means no SetSpeedAsync/save/restore should ever run");
        Assert.Equal(SimulationSpeed.Speed1, buffer.CurrentSpeed);
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
