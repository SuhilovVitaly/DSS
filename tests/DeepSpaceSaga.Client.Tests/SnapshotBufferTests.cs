using System.Collections.Immutable;
using System.Diagnostics;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests;

public class SnapshotBufferTests
{
    [Theory]
    [InlineData(SimulationSpeed.Speed0)]
    [InlineData(SimulationSpeed.Speed1)]
    [InlineData(SimulationSpeed.Speed2)]
    [InlineData(SimulationSpeed.Speed3)]
    [InlineData(SimulationSpeed.Speed4)]
    public void Stalled_snapshot_stream_caps_prediction(SimulationSpeed speed)
    {
        long clock = 0;
        var buffer = new SnapshotBuffer(() => clock);
        buffer.Update(new(1, 0, speed, []));
        clock = Stopwatch.Frequency * 2;
        long cap = buffer.LatestPrediction!.EffectivePredictionDeltaMs;
        clock = Stopwatch.Frequency * 60;
        Assert.Equal(cap, buffer.LatestPrediction!.EffectivePredictionDeltaMs);
        Assert.Equal(2000L * (int)speed, cap);
        Assert.Equal(speed, buffer.CurrentSpeed);
        Assert.True(buffer.LatestPrediction.IsStale);
        Assert.Equal(60000, buffer.LatestPrediction.SnapshotAgeMs);
    }

    [Fact]
    public void Stale_age_is_real_time_across_speed_changes_and_pause()
    {
        long clock = 0;
        var buffer = new SnapshotBuffer(() => clock);
        buffer.Update(new(1, 0, SimulationSpeed.Speed1, []));
        clock = Stopwatch.Frequency;
        buffer.CurrentSpeed = SimulationSpeed.Speed4;
        clock = Stopwatch.Frequency * 2 - Stopwatch.Frequency / 1000;
        Assert.False(buffer.LatestPrediction!.IsStale);
        clock = Stopwatch.Frequency * 2;
        Assert.True(buffer.LatestPrediction!.IsStale);
        Assert.Equal(101000, buffer.EffectivePredictionDeltaMs);
        clock = Stopwatch.Frequency * 10;
        buffer.CurrentSpeed = SimulationSpeed.Speed0;
        buffer.CurrentSpeed = SimulationSpeed.Speed4;
        clock = Stopwatch.Frequency * 20;
        Assert.Equal(101000, buffer.EffectivePredictionDeltaMs);
        Assert.Equal(20000, buffer.LatestPrediction!.SnapshotAgeMs);
        buffer.Update(new(2, 150000, SimulationSpeed.Speed4, []));
        Assert.False(buffer.LatestPrediction!.IsStale);
        Assert.Equal(0, buffer.LatestPrediction.SnapshotAgeMs);
        Assert.Equal(0, buffer.EffectivePredictionDeltaMs);
        clock += Stopwatch.Frequency / 10;
        Assert.Equal(10000, buffer.EffectivePredictionDeltaMs);
    }

    [Fact]
    public void Backwards_clock_and_old_packets_cannot_reset_receipt_age()
    {
        long clock = 0;
        var buffer = new SnapshotBuffer(() => clock);
        buffer.Update(new(2, 1000, SimulationSpeed.Speed1, []));
        clock = Stopwatch.Frequency * 3 / 2;
        Assert.Equal(1500, buffer.LatestPrediction!.SnapshotAgeMs);
        clock = Stopwatch.Frequency;
        Assert.Equal(1500, buffer.LatestPrediction!.SnapshotAgeMs);
        Assert.Equal(1500, buffer.EffectivePredictionDeltaMs);
        clock = Stopwatch.Frequency * 4;
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, []));
        Assert.Equal(4000, buffer.LatestPrediction!.SnapshotAgeMs);
        Assert.True(buffer.LatestPrediction.IsStale);
        Assert.Equal(2UL, buffer.Latest!.Snapshot.SnapshotSequence);
    }

    [Fact]
    public void Update_replaces_previous_snapshot()
    {
        var buffer = new SnapshotBuffer();
        var objects = ImmutableArray.Create(
            new ObjectMotionSnapshot("o1", 0, 0, SpeedKmS: 0, Direction: 0));

        var s1 = new AuthoritativeSnapshot(1, 1000, SimulationSpeed.Speed1, objects);
        buffer.Update(s1);
        Assert.Same(s1, buffer.Latest?.Snapshot);

        var s2 = new AuthoritativeSnapshot(2, 2000, SimulationSpeed.Speed1, objects);
        buffer.Update(s2);
        Assert.Same(s2, buffer.Latest?.Snapshot);
    }

    [Fact]
    public void Latest_returns_null_when_empty()
    {
        var buffer = new SnapshotBuffer();
        Assert.Null(buffer.Latest);
    }

    [Fact]
    public void BufferedSnapshot_has_prediction_delta()
    {
        var buffer = new SnapshotBuffer();
        var objects = ImmutableArray.Create(
            new ObjectMotionSnapshot("o1", 0, 0, SpeedKmS: 0, Direction: 0));

        buffer.Update(new AuthoritativeSnapshot(1, 1000, SimulationSpeed.Speed1, objects));

        var latest = buffer.Latest;
        Assert.NotNull(latest);
        Assert.True(latest.PredictionDeltaMs >= 0);
    }

    [Fact]
    public void ReconciliationForwardJumpMs_is_zero_when_snapshot_matches_prediction()
    {
        var buffer = new SnapshotBuffer();
        var objects = ImmutableArray<ObjectMotionSnapshot>.Empty;

        buffer.Update(new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed1, objects));
        buffer.Update(new AuthoritativeSnapshot(2, 0, SimulationSpeed.Speed1, objects));

        Assert.Equal(0, buffer.LatestPrediction!.ReconciliationForwardJumpMs);
    }

    [Fact]
    public void ReconciliationForwardJumpMs_reports_gap_when_snapshot_lands_ahead_of_prediction()
    {
        var buffer = new SnapshotBuffer();
        var objects = ImmutableArray<ObjectMotionSnapshot>.Empty;

        // No real time passes between updates, so the client predicted 0 extra ms —
        // a snapshot reporting 500 ms of extra game time is a pure forward jump.
        buffer.Update(new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed1, objects));
        buffer.Update(new AuthoritativeSnapshot(2, 500, SimulationSpeed.Speed1, objects));

        Assert.Equal(500, buffer.LatestPrediction!.ReconciliationForwardJumpMs);
    }

    [Fact]
    public void ReconciliationForwardJumpMs_is_zero_when_snapshot_rewinds_instead()
    {
        var clock = new FakeClock();
        var buffer = new SnapshotBuffer(() => clock.Timestamp);
        var objects = ImmutableArray<ObjectMotionSnapshot>.Empty;

        buffer.Update(new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed4, objects));
        clock.AdvanceMs(900); // client predicts far ahead (90_000 ms at Speed4)

        // A stale/rewinding snapshot must be clamped (existing behavior), not reported
        // as a forward jump.
        buffer.Update(new AuthoritativeSnapshot(2, 1_000, SimulationSpeed.Speed4, objects));

        Assert.Equal(0, buffer.LatestPrediction!.ReconciliationForwardJumpMs);
    }

    [Fact]
    public void Calendar_minutes_do_not_become_motion_prediction_or_reconciliation_jumps()
    {
        var clock = new FakeClock();
        var buffer = new SnapshotBuffer(() => clock.Timestamp);
        buffer.Update(new AuthoritativeSnapshot(1, 300_000, SimulationSpeed.Speed1, [], SimulationTimeMs: 1000));
        clock.AdvanceMs(1000);
        Assert.Equal(1000, buffer.EffectivePredictionDeltaMs);
        buffer.Update(new AuthoritativeSnapshot(2, 600_000, SimulationSpeed.Speed1, [], SimulationTimeMs: 2000));
        Assert.Equal(0, buffer.LatestPrediction!.ReconciliationForwardJumpMs);
        Assert.Equal(0, buffer.EffectivePredictionDeltaMs);
        clock.AdvanceMs(500);
        Assert.Equal(500, buffer.EffectivePredictionDeltaMs);
        buffer.CurrentSpeed = SimulationSpeed.Speed0;
        clock.AdvanceMs(10_000);
        Assert.Equal(500, buffer.EffectivePredictionDeltaMs);
    }
    private sealed class FakeClock
    {
        public long Timestamp { get; private set; }
        public void AdvanceMs(long milliseconds) => Timestamp += (long)(milliseconds * Stopwatch.Frequency / 1000.0);
    }
}
