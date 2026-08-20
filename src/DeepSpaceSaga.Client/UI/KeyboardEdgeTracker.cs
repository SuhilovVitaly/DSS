using Silk.NET.Input;

namespace DeepSpaceSaga.Client.UI;

internal sealed class KeyboardEdgeTracker
{
    private bool _prevEscPressed;
    private bool _prevIPressed;
    private bool _prevSpacePressed;
    private bool _prev1Pressed;
    private bool _prev2Pressed;
    private bool _prev3Pressed;
    private bool _prev4Pressed;
    private bool _prev5Pressed;
    private bool _prevUpPressed;
    private bool _prevDownPressed;
    private bool _prevLeftPressed;
    private bool _prevRightPressed;
    private bool _prevF5Pressed;
    private bool _prevF9Pressed;
    private bool _prevF10Pressed;
    private bool _prevCPressed;
    private bool _prevCtrlLeftPressed;
    private bool _prevCtrlRightPressed;
    private bool _prevBackspacePressed;

    /// <summary>
    /// Poll both press and release edges in a single pass. All edges are detected
    /// against the SAME previous-state snapshot — this is essential for Ctrl key
    /// handling because the real SkiaWindow calls Poll (which mutates previous state)
    /// before PollUpKeys (which needs the OLD previous to detect the release edge).
    /// </summary>
    public (int pressedCount, int releasedCount) PollBoth(
        IKeyboard keyboard, Span<Key> pressed, Span<Key> released)
    {
        int p = 0;
        int r = 0;

        AddBoth(Key.Escape, keyboard.IsKeyPressed(Key.Escape), ref _prevEscPressed, pressed, ref p, released, ref r);

        bool iDown = keyboard.IsKeyPressed(Key.I);
        bool ctrlDown = keyboard.IsKeyPressed(Key.ControlLeft) || keyboard.IsKeyPressed(Key.ControlRight);
        AddBoth(Key.I, iDown && ctrlDown, ref _prevIPressed, pressed, ref p, released, ref r);

        AddBoth(Key.ControlLeft, keyboard.IsKeyPressed(Key.ControlLeft), ref _prevCtrlLeftPressed, pressed, ref p, released, ref r);
        AddBoth(Key.ControlRight, keyboard.IsKeyPressed(Key.ControlRight), ref _prevCtrlRightPressed, pressed, ref p, released, ref r);

        AddBoth(Key.Space, keyboard.IsKeyPressed(Key.Space), ref _prevSpacePressed, pressed, ref p, released, ref r);
        AddBoth(Key.Number1, keyboard.IsKeyPressed(Key.Number1), ref _prev1Pressed, pressed, ref p, released, ref r);
        AddBoth(Key.Number2, keyboard.IsKeyPressed(Key.Number2), ref _prev2Pressed, pressed, ref p, released, ref r);
        AddBoth(Key.Number3, keyboard.IsKeyPressed(Key.Number3), ref _prev3Pressed, pressed, ref p, released, ref r);
        AddBoth(Key.Number4, keyboard.IsKeyPressed(Key.Number4), ref _prev4Pressed, pressed, ref p, released, ref r);
        AddBoth(Key.Number5, keyboard.IsKeyPressed(Key.Number5), ref _prev5Pressed, pressed, ref p, released, ref r);
        AddBoth(Key.Up, keyboard.IsKeyPressed(Key.Up), ref _prevUpPressed, pressed, ref p, released, ref r);
        AddBoth(Key.Down, keyboard.IsKeyPressed(Key.Down), ref _prevDownPressed, pressed, ref p, released, ref r);
        AddBoth(Key.Left, keyboard.IsKeyPressed(Key.Left), ref _prevLeftPressed, pressed, ref p, released, ref r);
        AddBoth(Key.Right, keyboard.IsKeyPressed(Key.Right), ref _prevRightPressed, pressed, ref p, released, ref r);
        AddBoth(Key.F5, keyboard.IsKeyPressed(Key.F5), ref _prevF5Pressed, pressed, ref p, released, ref r);
        AddBoth(Key.F9, keyboard.IsKeyPressed(Key.F9), ref _prevF9Pressed, pressed, ref p, released, ref r);
        AddBoth(Key.F10, keyboard.IsKeyPressed(Key.F10), ref _prevF10Pressed, pressed, ref p, released, ref r);
        AddBoth(Key.C, keyboard.IsKeyPressed(Key.C), ref _prevCPressed, pressed, ref p, released, ref r);
        AddBoth(Key.Backspace, keyboard.IsKeyPressed(Key.Backspace), ref _prevBackspacePressed, pressed, ref p, released, ref r);

        return (p, r);
    }

    /// <summary>Test seam.</summary>
    internal (int pressedCount, int releasedCount) PollBoth(
        Func<Key, bool> isKeyPressed, Span<Key> pressed, Span<Key> released)
    {
        int p = 0;
        int r = 0;

        AddBoth(Key.Escape, isKeyPressed(Key.Escape), ref _prevEscPressed, pressed, ref p, released, ref r);

        bool iDown = isKeyPressed(Key.I);
        bool ctrlDown = isKeyPressed(Key.ControlLeft) || isKeyPressed(Key.ControlRight);
        AddBoth(Key.I, iDown && ctrlDown, ref _prevIPressed, pressed, ref p, released, ref r);

        AddBoth(Key.ControlLeft, isKeyPressed(Key.ControlLeft), ref _prevCtrlLeftPressed, pressed, ref p, released, ref r);
        AddBoth(Key.ControlRight, isKeyPressed(Key.ControlRight), ref _prevCtrlRightPressed, pressed, ref p, released, ref r);

        AddBoth(Key.Space, isKeyPressed(Key.Space), ref _prevSpacePressed, pressed, ref p, released, ref r);
        AddBoth(Key.Number1, isKeyPressed(Key.Number1), ref _prev1Pressed, pressed, ref p, released, ref r);
        AddBoth(Key.Number2, isKeyPressed(Key.Number2), ref _prev2Pressed, pressed, ref p, released, ref r);
        AddBoth(Key.Number3, isKeyPressed(Key.Number3), ref _prev3Pressed, pressed, ref p, released, ref r);
        AddBoth(Key.Number4, isKeyPressed(Key.Number4), ref _prev4Pressed, pressed, ref p, released, ref r);
        AddBoth(Key.Number5, isKeyPressed(Key.Number5), ref _prev5Pressed, pressed, ref p, released, ref r);
        AddBoth(Key.Up, isKeyPressed(Key.Up), ref _prevUpPressed, pressed, ref p, released, ref r);
        AddBoth(Key.Down, isKeyPressed(Key.Down), ref _prevDownPressed, pressed, ref p, released, ref r);
        AddBoth(Key.Left, isKeyPressed(Key.Left), ref _prevLeftPressed, pressed, ref p, released, ref r);
        AddBoth(Key.Right, isKeyPressed(Key.Right), ref _prevRightPressed, pressed, ref p, released, ref r);
        AddBoth(Key.F5, isKeyPressed(Key.F5), ref _prevF5Pressed, pressed, ref p, released, ref r);
        AddBoth(Key.F9, isKeyPressed(Key.F9), ref _prevF9Pressed, pressed, ref p, released, ref r);
        AddBoth(Key.F10, isKeyPressed(Key.F10), ref _prevF10Pressed, pressed, ref p, released, ref r);
        AddBoth(Key.C, isKeyPressed(Key.C), ref _prevCPressed, pressed, ref p, released, ref r);
        AddBoth(Key.Backspace, isKeyPressed(Key.Backspace), ref _prevBackspacePressed, pressed, ref p, released, ref r);

        return (p, r);
    }

    // Legacy methods kept for existing callers (the old Poll + PollUpKeys path is
    // still used by GameSessionScreen tests — but real SkiaWindow uses PollBoth).
    public int Poll(IKeyboard keyboard, Span<Key> pressed)
    {
        int count = 0;
        AddEdge(Key.Escape, keyboard.IsKeyPressed(Key.Escape), ref _prevEscPressed, pressed, ref count);
        bool iDown = keyboard.IsKeyPressed(Key.I);
        bool ctrlDown = keyboard.IsKeyPressed(Key.ControlLeft) || keyboard.IsKeyPressed(Key.ControlRight);
        AddEdge(Key.I, iDown && ctrlDown, ref _prevIPressed, pressed, ref count);
        AddEdge(Key.ControlLeft, keyboard.IsKeyPressed(Key.ControlLeft), ref _prevCtrlLeftPressed, pressed, ref count);
        AddEdge(Key.ControlRight, keyboard.IsKeyPressed(Key.ControlRight), ref _prevCtrlRightPressed, pressed, ref count);
        AddEdge(Key.Space, keyboard.IsKeyPressed(Key.Space), ref _prevSpacePressed, pressed, ref count);
        AddEdge(Key.Number1, keyboard.IsKeyPressed(Key.Number1), ref _prev1Pressed, pressed, ref count);
        AddEdge(Key.Number2, keyboard.IsKeyPressed(Key.Number2), ref _prev2Pressed, pressed, ref count);
        AddEdge(Key.Number3, keyboard.IsKeyPressed(Key.Number3), ref _prev3Pressed, pressed, ref count);
        AddEdge(Key.Number4, keyboard.IsKeyPressed(Key.Number4), ref _prev4Pressed, pressed, ref count);
        AddEdge(Key.Number5, keyboard.IsKeyPressed(Key.Number5), ref _prev5Pressed, pressed, ref count);
        AddEdge(Key.Up, keyboard.IsKeyPressed(Key.Up), ref _prevUpPressed, pressed, ref count);
        AddEdge(Key.Down, keyboard.IsKeyPressed(Key.Down), ref _prevDownPressed, pressed, ref count);
        AddEdge(Key.Left, keyboard.IsKeyPressed(Key.Left), ref _prevLeftPressed, pressed, ref count);
        AddEdge(Key.Right, keyboard.IsKeyPressed(Key.Right), ref _prevRightPressed, pressed, ref count);
        AddEdge(Key.F5, keyboard.IsKeyPressed(Key.F5), ref _prevF5Pressed, pressed, ref count);
        AddEdge(Key.F9, keyboard.IsKeyPressed(Key.F9), ref _prevF9Pressed, pressed, ref count);
        AddEdge(Key.F10, keyboard.IsKeyPressed(Key.F10), ref _prevF10Pressed, pressed, ref count);
        AddEdge(Key.C, keyboard.IsKeyPressed(Key.C), ref _prevCPressed, pressed, ref count);
        AddEdge(Key.Backspace, keyboard.IsKeyPressed(Key.Backspace), ref _prevBackspacePressed, pressed, ref count);
        return count;
    }

    internal int Poll(Func<Key, bool> isKeyPressed, Span<Key> pressed)
    {
        int count = 0;
        AddEdge(Key.Escape, isKeyPressed(Key.Escape), ref _prevEscPressed, pressed, ref count);
        bool iDown = isKeyPressed(Key.I);
        bool ctrlDown = isKeyPressed(Key.ControlLeft) || isKeyPressed(Key.ControlRight);
        AddEdge(Key.I, iDown && ctrlDown, ref _prevIPressed, pressed, ref count);
        AddEdge(Key.ControlLeft, isKeyPressed(Key.ControlLeft), ref _prevCtrlLeftPressed, pressed, ref count);
        AddEdge(Key.ControlRight, isKeyPressed(Key.ControlRight), ref _prevCtrlRightPressed, pressed, ref count);
        AddEdge(Key.Space, isKeyPressed(Key.Space), ref _prevSpacePressed, pressed, ref count);
        AddEdge(Key.Number1, isKeyPressed(Key.Number1), ref _prev1Pressed, pressed, ref count);
        AddEdge(Key.Number2, isKeyPressed(Key.Number2), ref _prev2Pressed, pressed, ref count);
        AddEdge(Key.Number3, isKeyPressed(Key.Number3), ref _prev3Pressed, pressed, ref count);
        AddEdge(Key.Number4, isKeyPressed(Key.Number4), ref _prev4Pressed, pressed, ref count);
        AddEdge(Key.Number5, isKeyPressed(Key.Number5), ref _prev5Pressed, pressed, ref count);
        AddEdge(Key.Up, isKeyPressed(Key.Up), ref _prevUpPressed, pressed, ref count);
        AddEdge(Key.Down, isKeyPressed(Key.Down), ref _prevDownPressed, pressed, ref count);
        AddEdge(Key.Left, isKeyPressed(Key.Left), ref _prevLeftPressed, pressed, ref count);
        AddEdge(Key.Right, isKeyPressed(Key.Right), ref _prevRightPressed, pressed, ref count);
        AddEdge(Key.F5, isKeyPressed(Key.F5), ref _prevF5Pressed, pressed, ref count);
        AddEdge(Key.F9, isKeyPressed(Key.F9), ref _prevF9Pressed, pressed, ref count);
        AddEdge(Key.F10, isKeyPressed(Key.F10), ref _prevF10Pressed, pressed, ref count);
        AddEdge(Key.C, isKeyPressed(Key.C), ref _prevCPressed, pressed, ref count);
        AddEdge(Key.Backspace, isKeyPressed(Key.Backspace), ref _prevBackspacePressed, pressed, ref count);
        return count;
    }

    public int PollUpKeys(IKeyboard keyboard, Span<Key> released)
    {
        int count = 0;
        AddUpEdge(Key.ControlLeft, keyboard.IsKeyPressed(Key.ControlLeft), ref _prevCtrlLeftPressed, released, ref count);
        AddUpEdge(Key.ControlRight, keyboard.IsKeyPressed(Key.ControlRight), ref _prevCtrlRightPressed, released, ref count);
        return count;
    }

    internal int PollUpKeys(Func<Key, bool> isKeyPressed, Span<Key> released)
    {
        int count = 0;
        AddUpEdge(Key.ControlLeft, isKeyPressed(Key.ControlLeft), ref _prevCtrlLeftPressed, released, ref count);
        AddUpEdge(Key.ControlRight, isKeyPressed(Key.ControlRight), ref _prevCtrlRightPressed, released, ref count);
        return count;
    }

    private static void AddBoth(
        Key key, bool isPressed, ref bool previous,
        Span<Key> pressed, ref int pressedCount,
        Span<Key> released, ref int releasedCount)
    {
        if (isPressed && !previous)
            pressed[pressedCount++] = key;
        else if (!isPressed && previous)
            released[releasedCount++] = key;

        previous = isPressed;
    }

    private static void AddEdge(
        Key key, bool isPressed, ref bool previous,
        Span<Key> pressed, ref int count)
    {
        if (isPressed && !previous)
            pressed[count++] = key;
        previous = isPressed;
    }

    /// <summary>
    /// Re-baselines every tracked key to whatever the keyboard currently reports,
    /// without emitting press/release edges. Call this when the window regains focus:
    /// an OS-level overlay stealing focus (e.g. Windows' Print Screen key opening the
    /// Snipping Tool over a borderless game window) can leave the underlying key-state
    /// array stale, which otherwise surfaces on the next poll as a spurious edge — most
    /// visibly a phantom Escape that pops the game menu open right as focus returns.
    /// </summary>
    public void ResyncToCurrentState(IKeyboard? keyboard)
    {
        if (keyboard is null)
            return;

        ResyncToCurrentState(keyboard.IsKeyPressed);
    }

    /// <summary>Test seam.</summary>
    internal void ResyncToCurrentState(Func<Key, bool> isKeyPressed)
    {
        _prevEscPressed = isKeyPressed(Key.Escape);
        _prevIPressed = isKeyPressed(Key.I)
            && (isKeyPressed(Key.ControlLeft) || isKeyPressed(Key.ControlRight));
        _prevCtrlLeftPressed = isKeyPressed(Key.ControlLeft);
        _prevCtrlRightPressed = isKeyPressed(Key.ControlRight);
        _prevSpacePressed = isKeyPressed(Key.Space);
        _prev1Pressed = isKeyPressed(Key.Number1);
        _prev2Pressed = isKeyPressed(Key.Number2);
        _prev3Pressed = isKeyPressed(Key.Number3);
        _prev4Pressed = isKeyPressed(Key.Number4);
        _prev5Pressed = isKeyPressed(Key.Number5);
        _prevUpPressed = isKeyPressed(Key.Up);
        _prevDownPressed = isKeyPressed(Key.Down);
        _prevLeftPressed = isKeyPressed(Key.Left);
        _prevRightPressed = isKeyPressed(Key.Right);
        _prevF5Pressed = isKeyPressed(Key.F5);
        _prevF9Pressed = isKeyPressed(Key.F9);
        _prevF10Pressed = isKeyPressed(Key.F10);
        _prevCPressed = isKeyPressed(Key.C);
        _prevBackspacePressed = isKeyPressed(Key.Backspace);
    }

    private static void AddUpEdge(
        Key key, bool isPressed, ref bool previous,
        Span<Key> released, ref int count)
    {
        if (!isPressed && previous)
            released[count++] = key;
        previous = isPressed;
    }
}
