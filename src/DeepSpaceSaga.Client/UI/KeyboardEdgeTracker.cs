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

    public int Poll(IKeyboard keyboard, Span<Key> pressed)
    {
        int count = 0;

        AddEdge(Key.Escape, keyboard.IsKeyPressed(Key.Escape), ref _prevEscPressed, pressed, ref count);

        bool iDown = keyboard.IsKeyPressed(Key.I);
        bool ctrlDown = keyboard.IsKeyPressed(Key.ControlLeft) || keyboard.IsKeyPressed(Key.ControlRight);
        AddEdge(Key.I, iDown && ctrlDown, ref _prevIPressed, pressed, ref count);

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

        return count;
    }

    internal int Poll(Func<Key, bool> isKeyPressed, Span<Key> pressed)
    {
        int count = 0;

        AddEdge(Key.Escape, isKeyPressed(Key.Escape), ref _prevEscPressed, pressed, ref count);

        bool iDown = isKeyPressed(Key.I);
        bool ctrlDown = isKeyPressed(Key.ControlLeft) || isKeyPressed(Key.ControlRight);
        AddEdge(Key.I, iDown && ctrlDown, ref _prevIPressed, pressed, ref count);

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

        return count;
    }

    private static void AddEdge(
        Key key,
        bool isPressed,
        ref bool previous,
        Span<Key> pressed,
        ref int count)
    {
        if (isPressed && !previous)
            pressed[count++] = key;

        previous = isPressed;
    }
}
