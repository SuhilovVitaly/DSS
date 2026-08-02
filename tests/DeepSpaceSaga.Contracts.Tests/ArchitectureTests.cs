using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class ArchitectureTests
{
    [Fact]
    public void Snapshot_sequence_is_incrementing()
    {
        var objects = new List<ObjectMotionSnapshot> { new("o1", 0, 0, 0, 0) };

        var s1 = new AuthoritativeSnapshot(1, 1000, objects);
        var s2 = new AuthoritativeSnapshot(2, 2000, objects);
        var s3 = new AuthoritativeSnapshot(3, 3000, objects);

        Assert.True(s1.SnapshotSequence < s2.SnapshotSequence);
        Assert.True(s2.SnapshotSequence < s3.SnapshotSequence);
    }

    [Fact]
    public void ObjectMotionSnapshot_holds_coordinates()
    {
        var obj = new ObjectMotionSnapshot("probe-1", 500.0, 300.0, 50.0, Math.PI / 2);

        Assert.Equal("probe-1", obj.ObjectId);
        Assert.Equal(500.0, obj.X);
        Assert.Equal(300.0, obj.Y);
        Assert.Equal(50.0, obj.Speed);
        Assert.Equal(Math.PI / 2, obj.Direction);
    }
}
