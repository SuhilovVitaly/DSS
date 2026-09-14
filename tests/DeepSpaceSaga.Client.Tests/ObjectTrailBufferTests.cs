using DeepSpaceSaga.Client.UI.Screens.GameSession;

namespace DeepSpaceSaga.Client.Tests;

public class ObjectTrailBufferTests
{
    [Fact]
    public void Wrap_growth_and_pruning_preserve_chronological_history()
    {
        var buffer = new ObjectTrailBuffer();
        for (int i = 0; i < 250; i++) buffer.Add(new(i, -i, i));
        buffer.RemoveFirst(200);
        for (int i = 250; i < 900; i++) buffer.Add(new(i, -i, i));
        Assert.Equal(700, buffer.Count);
        Assert.Equal(Enumerable.Range(200, 700).Select(i => (long)i), buffer.Select(p => p.Timestamp));
        Assert.Equal((200d, -899d, 899d, -200d), buffer.Bounds);
        buffer.RemoveFirst(699);
        Assert.Equal((899d, -899d, 899d, -899d), buffer.Bounds);
        buffer[0] = new(10, 20, 900);
        Assert.Equal((10d, 20d, 10d, 20d), buffer.Bounds);
        buffer.RemoveFirst(1);
        buffer.Add(new(-4, -5, 901));
        Assert.Equal(new ObjectTrailPoint(-4, -5, 901), Assert.Single(buffer));
        Assert.Equal((-4d, -5d, -4d, -5d), buffer.Bounds);
    }

    [Fact]
    public void Bounds_include_interior_bends_after_tail_removal()
    {
        var buffer = new ObjectTrailBuffer();
        buffer.Add(new(0, 0, 0)); buffer.Add(new(-100, 200, 1)); buffer.Add(new(1, 1, 2));
        Assert.Equal((-100d, 0d, 1d, 200d), buffer.Bounds);
        buffer.RemoveFirst(1);
        Assert.Equal((-100d, 1d, 1d, 200d), buffer.Bounds);
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.RemoveFirst(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer[-1]);
    }
}
