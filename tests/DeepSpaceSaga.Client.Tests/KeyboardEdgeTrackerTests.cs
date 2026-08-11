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

    [Fact]
    public void PollBoth_reports_press_and_release_in_same_frame()
    {
        // Real SkiaWindow calls Poll (which mutates previous) before PollUpKeys
        // (which needs old previous). PollBoth captures both from one snapshot.
        var tracker = new KeyboardEdgeTracker();
        var pressed = new HashSet<Key> { Key.ControlLeft };
        PollBoth(tracker, pressed, out var p, out var r);
        Assert.Equal([Key.ControlLeft], p);
        Assert.Empty(r);

        // Ctrl still held — no edges.
        PollBoth(tracker, pressed, out p, out r);
        Assert.Empty(p);
        Assert.Empty(r);

        // Ctrl released — must see release edge even on the very next PollBoth.
        pressed.Remove(Key.ControlLeft);
        PollBoth(tracker, pressed, out p, out r);
        Assert.Empty(p);
        Assert.Equal([Key.ControlLeft], r); // P1 fix: release NOT lost
    }

    [Fact]
    public void Ctrl_i_reports_both_edges_via_poll_both()
    {
        var tracker = new KeyboardEdgeTracker();
        var pressed = new HashSet<Key> { Key.ControlLeft, Key.I };

        PollBoth(tracker, pressed, out var p, out var r);
        Assert.Contains(Key.ControlLeft, p);
        Assert.Contains(Key.I, p);
        Assert.Empty(r);
    }

    [Fact]
    public void Resync_on_focus_regain_suppresses_phantom_escape_edge()
    {
        // Regression: an OS overlay (e.g. Print Screen -> Windows Snipping Tool)
        // stealing and returning focus can leave Escape reporting as "down" without
        // a real edge ever having been polled. Resyncing on focus-regain must adopt
        // that state as the baseline instead of surfacing it as a fresh press —
        // otherwise it pops the game menu open on its own.
        var tracker = new KeyboardEdgeTracker();
        var pressed = new HashSet<Key> { Key.Escape };

        tracker.ResyncToCurrentState(pressed.Contains);
        Assert.Empty(Poll(tracker, pressed)); // no phantom press edge

        pressed.Remove(Key.Escape);
        Assert.Empty(Poll(tracker, pressed)); // release of a never-reported press is also silent

        // A genuine Escape press afterwards must still open the menu.
        pressed.Add(Key.Escape);
        Assert.Equal([Key.Escape], Poll(tracker, pressed));
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

    private static void PollBoth(KeyboardEdgeTracker tracker,
        HashSet<Key> pressed, out Key[] p, out Key[] r)
    {
        Span<Key> pBuf = stackalloc Key[16];
        Span<Key> rBuf = stackalloc Key[16];
        var (pc, rc) = tracker.PollBoth(pressed.Contains, pBuf, rBuf);
        p = pBuf[..pc].ToArray();
        r = rBuf[..rc].ToArray();
    }
}
