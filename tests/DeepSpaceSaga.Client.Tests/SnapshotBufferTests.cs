using System.Collections.Immutable;
using System.Diagnostics;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests;

public class SnapshotBufferTests
{
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

    private sealed class FakeClock
    {
        public long Timestamp { get; private set; }
        public void AdvanceMs(long milliseconds) => Timestamp += (long)(milliseconds * Stopwatch.Frequency / 1000.0);
    }
}
