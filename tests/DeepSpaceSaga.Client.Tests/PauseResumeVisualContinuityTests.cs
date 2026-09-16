using System.Collections.Immutable;
using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// Regression tests for the visible "ship + trail + grid jump" bug at high simulation
/// speed. Root cause: a routine 1Hz authoritative snapshot can reveal that the object's
/// real trajectory differs from what the client had been extrapolating from the PREVIOUS
/// snapshot (clock disagreement between engine and client, a turn/command that progressed
/// while paused, etc.) — amplified to hundreds/thousands of world units at Speed4 (100x).
/// GameSessionScreen only smoothed the exact pause→resume transition frame; this kind of
/// jump lands on an ordinary running frame (often ~1s after resume, whenever the first
/// post-resume snapshot arrives) and was applied as an instant, unsmoothed snap. A first
/// attempted fix (extrapolating "backwards" from the NEW baseline by the clock-skew amount)
/// went negative and collapsed to zero smoothing whenever the skew exceeded elapsed time
/// since the snapshot arrived — which is the common case right after a snapshot lands. The
/// actual fix extrapolates the PREVIOUS baseline object forward to the same target time.
/// </summary>
public class PauseResumeVisualContinuityTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;
    private const long FrameMs = 13; // ~80 fps
    private const double SteadyStateDeltaPerFrame = FrameMs * 10.0; // 10 km/s @ Speed4 = 10 units/real-ms

    private sealed class FakeTimestamp
    {
        public long Timestamp { get; private set; }
        public void AdvanceMs(long milliseconds) => Timestamp += milliseconds * Stopwatch.Frequency / 1000;
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(15)]
    public void Snapshot_landing_ahead_of_prediction_does_not_snap_in_a_single_frame(long clockSkewMs)
    {
        var clock = new FakeTimestamp();
        var buffer = new SnapshotBuffer(() => clock.Timestamp);
        var predictor = new LinearMotionPredictor();

        var playerShip = new ObjectMotionSnapshot("player", 10000, 10000, SpeedKmS: 10, Direction: 90);
        buffer.Update(new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed4,
            ImmutableArray.Create(playerShip), PlayerShipObjectId: "player"));

        var screen = new GameSessionScreen(buffer, predictor, timestampProvider: () => clock.Timestamp);
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);

        // Run steadily for ~1s at Speed4 (matches the engine's real snapshot cadence).
        for (int i = 0; i < 76; i++)
        {
            clock.AdvanceMs(FrameMs);
            screen.Render(canvas, ScreenWidth, ScreenHeight);
        }

        long clientPredictedGameTimeMs = buffer.LatestPrediction!.BufferedSnapshot.Snapshot.GameTimeMs
            + buffer.LatestPrediction.EffectivePredictionDeltaMs;

        // A routine, non-pathological clock disagreement: the next authoritative snapshot
        // reports slightly MORE elapsed game time than the client already predicted.
        long engineGameTimeMs = clientPredictedGameTimeMs + clockSkewMs * 100; // ms * Speed4 multiplier
        var movedShip = predictor.Predict(playerShip, engineGameTimeMs);
        buffer.Update(new AuthoritativeSnapshot(2, engineGameTimeMs, SimulationSpeed.Speed4,
            ImmutableArray.Create(movedShip), PlayerShipObjectId: "player"));

        double maxFrameDelta = 0;
        double prevX = screen.CameraFocusX;
        for (int i = 0; i < 30; i++)
        {
            clock.AdvanceMs(FrameMs);
            screen.Render(canvas, ScreenWidth, ScreenHeight);
            double x = screen.CameraFocusX;
            maxFrameDelta = Math.Max(maxFrameDelta, Math.Abs(x - prevX));
            prevX = x;
        }

        // With unchanged velocity, extrapolating from the old vs. new baseline to the same
        // target time is mathematically identical, so no correction is created at all —
        // the only effect is the target game time itself catching up more than one frame's
        // worth in a single render call (a brief speed blip on a smooth, single-direction
        // trajectory), which is a much milder artifact than an unsmoothed teleport. The
        // margin here just guards against a full, un-smoothed snap of the whole skew.
        Assert.True(
            maxFrameDelta < SteadyStateDeltaPerFrame * 3.0,
            $"single-frame delta {maxFrameDelta:F2} (steady-state is {SteadyStateDeltaPerFrame:F2}) looks like an unsmoothed snap");
    }

    [Fact]
    public void Snapshot_revealing_a_heading_change_is_smoothed_not_snapped()
    {
        // Reproduces the real bug found via PauseResume.log: a new snapshot arrives very
        // shortly (game-time-wise) after the previous one, but reports the ship far off the
        // trajectory the client had been extrapolating (e.g. a turn/command took effect
        // while paused). The visual position must NOT snap directly to the new snapshot's
        // raw position — it must ease in like any other correction.
        var clock = new FakeTimestamp();
        var buffer = new SnapshotBuffer(() => clock.Timestamp);
        var predictor = new LinearMotionPredictor();

        var playerShip = new ObjectMotionSnapshot("player", 10000, 10000, SpeedKmS: 10, Direction: 90);
        buffer.Update(new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed4,
            ImmutableArray.Create(playerShip), PlayerShipObjectId: "player"));

        var screen = new GameSessionScreen(buffer, predictor, timestampProvider: () => clock.Timestamp);
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);

        for (int i = 0; i < 20; i++)
        {
            clock.AdvanceMs(FrameMs);
            screen.Render(canvas, ScreenWidth, ScreenHeight);
        }

        // A wildly different position/heading than continuing the old trajectory would
        // give — the new snapshot's own baseline, not an extrapolation of it.
        var turnedShip = playerShip with { X = 9993.299, Y = 8286.690, Direction = 210 };
        buffer.Update(new AuthoritativeSnapshot(2, 26_500, SimulationSpeed.Speed4,
            ImmutableArray.Create(turnedShip), PlayerShipObjectId: "player"));

        clock.AdvanceMs(FrameMs);
        screen.Render(canvas, ScreenWidth, ScreenHeight); // first frame observing the new snapshot

        // The raw new-snapshot position (what an unsmoothed implementation would jump
        // straight to) — the visual position must NOT land exactly on it this frame.
        double rawTargetDistance = Distance(
            screen.CameraFocusX, screen.CameraFocusY, turnedShip.X, turnedShip.Y);
        Assert.True(
            rawTargetDistance > 1.0,
            $"visual position landed exactly on the raw new-snapshot position ({rawTargetDistance:F3} units away) — unsmoothed snap");

        double maxFrameDelta = 0;
        double prevX = screen.CameraFocusX, prevY = screen.CameraFocusY;
        for (int i = 0; i < 30; i++)
        {
            clock.AdvanceMs(FrameMs);
            screen.Render(canvas, ScreenWidth, ScreenHeight);
            double dx = screen.CameraFocusX - prevX, dy = screen.CameraFocusY - prevY;
            maxFrameDelta = Math.Max(maxFrameDelta, Math.Sqrt(dx * dx + dy * dy));
            prevX = screen.CameraFocusX;
            prevY = screen.CameraFocusY;
        }

        Assert.True(
            maxFrameDelta < SteadyStateDeltaPerFrame * 3.0,
            $"single-frame delta {maxFrameDelta:F2} (steady-state is {SteadyStateDeltaPerFrame:F2}) looks like an unsmoothed snap");
    }

    private static double Distance(double x1, double y1, double x2, double y2)
    {
        double dx = x1 - x2, dy = y1 - y2;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    [Fact]
    public void Resume_after_pause_at_speed4_does_not_snap_even_with_input_lag()
    {
        var clock = new FakeTimestamp();
        var buffer = new SnapshotBuffer(() => clock.Timestamp);
        var predictor = new LinearMotionPredictor();

        var playerShip = new ObjectMotionSnapshot("player", 10000, 10000, SpeedKmS: 10, Direction: 90);
        buffer.Update(new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed4,
            ImmutableArray.Create(playerShip), PlayerShipObjectId: "player"));

        var screen = new GameSessionScreen(buffer, predictor, timestampProvider: () => clock.Timestamp);
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);

        for (int i = 0; i < 40; i++)
        {
            clock.AdvanceMs(FrameMs);
            screen.Render(canvas, ScreenWidth, ScreenHeight);
        }

        // Simulate realistic input lag: 100ms passes after the last rendered frame but
        // before the pause keypress is actually processed.
        clock.AdvanceMs(100);
        buffer.CurrentSpeed = SimulationSpeed.Speed0;

        clock.AdvanceMs(FrameMs);
        screen.Render(canvas, ScreenWidth, ScreenHeight); // entering-pause frame

        for (int i = 0; i < 20; i++)
        {
            clock.AdvanceMs(FrameMs);
            screen.Render(canvas, ScreenWidth, ScreenHeight);
        }

        buffer.CurrentSpeed = SimulationSpeed.Speed4; // resume

        double maxFrameDelta = 0;
        double prevX = screen.CameraFocusX;
        for (int i = 0; i < 40; i++)
        {
            clock.AdvanceMs(FrameMs);
            screen.Render(canvas, ScreenWidth, ScreenHeight);
            double x = screen.CameraFocusX;
            maxFrameDelta = Math.Max(maxFrameDelta, Math.Abs(x - prevX));
            prevX = x;
        }

        Assert.True(
            maxFrameDelta < SteadyStateDeltaPerFrame * 3.0,
            $"single-frame delta {maxFrameDelta:F2} (steady-state is {SteadyStateDeltaPerFrame:F2}) looks like an unsmoothed snap");
    }
}
