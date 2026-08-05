using DeepSpaceSaga.Client.UI;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests;

public class KeyboardEdgeTrackerTests
{
    [Fact]
    public void Arrow_keys_are_reported_on_press_edge()
    {
        var tracker = new KeyboardEdgeTracker();
        var pressed = new HashSet<Key> { Key.Up, Key.Right };

        Assert.Equal([Key.Up, Key.Right], Poll(tracker, pressed));
        Assert.Empty(Poll(tracker, pressed));

        pressed.Remove(Key.Up);
        pressed.Remove(Key.Right);
        Assert.Empty(Poll(tracker, pressed));

        pressed.Add(Key.Up);
        Assert.Equal([Key.Up], Poll(tracker, pressed));
    }

    [Fact]
    public void Ctrl_i_reports_i_only_when_control_is_down()
    {
        var tracker = new KeyboardEdgeTracker();
        var pressed = new HashSet<Key> { Key.I };

        Assert.Empty(Poll(tracker, pressed));

        pressed.Add(Key.ControlLeft);
        Assert.Equal([Key.I], Poll(tracker, pressed));
        Assert.Empty(Poll(tracker, pressed));
    }

    private static Key[] Poll(KeyboardEdgeTracker tracker, HashSet<Key> pressed)
    {
        Span<Key> buffer = stackalloc Key[7];
        int count = tracker.Poll(pressed.Contains, buffer);
        return buffer[..count].ToArray();
    }
}
