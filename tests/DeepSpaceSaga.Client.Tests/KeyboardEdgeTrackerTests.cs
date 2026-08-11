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
    public void Control_keys_are_reported_on_press_edge()
    {
        var tracker = new KeyboardEdgeTracker();
        var pressed = new HashSet<Key> { Key.ControlLeft };

        Assert.Equal([Key.ControlLeft], Poll(tracker, pressed));
        Assert.Empty(Poll(tracker, pressed)); // held — not reported again

        pressed.Remove(Key.ControlLeft);
        Assert.Empty(Poll(tracker, pressed));

        pressed.Add(Key.ControlRight);
        Assert.Equal([Key.ControlRight], Poll(tracker, pressed));
    }

    [Fact]
    public void Control_release_edges_are_reported_once()
    {
        var tracker = new KeyboardEdgeTracker();
        var pressed = new HashSet<Key> { Key.ControlLeft };
        Poll(tracker, pressed); // consume press edge

        pressed.Remove(Key.ControlLeft);
        var released = PollUpKeys(tracker, pressed);
        Assert.Equal([Key.ControlLeft], released);

        released = PollUpKeys(tracker, pressed);
        Assert.Empty(released); // not reported again
    }

    [Fact]
    public void Ctrl_i_reports_i_and_ctrl_when_control_is_down()
    {
        var tracker = new KeyboardEdgeTracker();
        var pressed = new HashSet<Key> { Key.I };

        Assert.Empty(Poll(tracker, pressed));

        pressed.Add(Key.ControlLeft);
        // Both ControlLeft and I are press edges on the same frame.
        var result = Poll(tracker, pressed);
        Assert.Contains(Key.I, result);
        Assert.Contains(Key.ControlLeft, result);

        Assert.Empty(Poll(tracker, pressed));
    }

    [Fact]
    public void Ctrl_i_still_works_after_adding_ctrl_edge_reporting()
    {
        // Regression: Ctrl+I must still produce 'I' so the info panel opens.
        var tracker = new KeyboardEdgeTracker();
        var pressed = new HashSet<Key> { Key.ControlLeft, Key.I };

        var result = Poll(tracker, pressed);
        Assert.Contains(Key.ControlLeft, result);
        Assert.Contains(Key.I, result);
    }

    [Fact]
    public void Control_right_release_edge_is_reported()
    {
        var tracker = new KeyboardEdgeTracker();
        var pressed = new HashSet<Key> { Key.ControlRight };
        Poll(tracker, pressed);

        pressed.Remove(Key.ControlRight);
        Assert.Equal([Key.ControlRight], PollUpKeys(tracker, pressed));
    }

    private static Key[] Poll(KeyboardEdgeTracker tracker, HashSet<Key> pressed)
    {
        Span<Key> buffer = stackalloc Key[16];
        int count = tracker.Poll(pressed.Contains, buffer);
        return buffer[..count].ToArray();
    }

    private static Key[] PollUpKeys(KeyboardEdgeTracker tracker, HashSet<Key> pressed)
    {
        Span<Key> buffer = stackalloc Key[16];
        int count = tracker.PollUpKeys(pressed.Contains, buffer);
        return buffer[..count].ToArray();
    }
}
