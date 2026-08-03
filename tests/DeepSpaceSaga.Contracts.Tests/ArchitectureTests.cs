using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class ArchitectureTests
{
    [Fact]
    public void Snapshot_sequence_is_incrementing()
    {
        var objects = ImmutableArray.Create(
            new ObjectMotionSnapshot("o1", 0, 0, SpeedKmS: 0, Direction: 0));

        var s1 = new AuthoritativeSnapshot(1, 1000, SimulationSpeed.Speed1, objects);
        var s2 = new AuthoritativeSnapshot(2, 2000, SimulationSpeed.Speed1, objects);
        var s3 = new AuthoritativeSnapshot(3, 3000, SimulationSpeed.Speed1, objects);

        Assert.True(s1.SnapshotSequence < s2.SnapshotSequence);
        Assert.True(s2.SnapshotSequence < s3.SnapshotSequence);
    }

    [Fact]
    public void ObjectMotionSnapshot_holds_coordinates()
    {
        var obj = new ObjectMotionSnapshot("probe-1", 500.0, 300.0, SpeedKmS: 5.0, Direction: 90);

        Assert.Equal("probe-1", obj.ObjectId);
        Assert.Equal(500.0, obj.X);
        Assert.Equal(300.0, obj.Y);
        Assert.Equal(5.0, obj.SpeedKmS);
        Assert.Equal(90, obj.Direction);
    }
}
