using System.Collections.Immutable;
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

        var s1 = new AuthoritativeSnapshot(1, 1000, objects);
        buffer.Update(s1);
        Assert.Same(s1, buffer.Latest?.Snapshot);

        var s2 = new AuthoritativeSnapshot(2, 2000, objects);
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

        buffer.Update(new AuthoritativeSnapshot(1, 1000, objects));

        var latest = buffer.Latest;
        Assert.NotNull(latest);
        Assert.True(latest.PredictionDeltaMs >= 0);
    }
}
