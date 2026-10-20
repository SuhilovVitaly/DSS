using Silk.NET.Input;

namespace DeepSpaceSaga.Client.UI;

/// <summary>One physical edge state for native/test polling, mapped bindings and focus resync.</summary>
internal sealed class KeyboardEdgeTracker
{
    private static readonly Key[] StandardKeys =
    [
        Key.Escape, Key.I, Key.S, Key.F, Key.Space, Key.Tab, Key.Enter,
        Key.Number1, Key.Number2, Key.Number3, Key.Number4, Key.Number5,
        Key.Up, Key.Down, Key.Left, Key.Right, Key.F5, Key.F9, Key.F10, Key.C, Key.Backspace,
        Key.Equal, Key.Minus, Key.KeypadAdd, Key.KeypadSubtract, Key.Home, Key.End
    ];
    private readonly Key[] _keys;
    private readonly Dictionary<Key, bool> _previous = new();
    private IKeyboard? _keyboard;
    private Func<Key, bool>? _reader;

    internal KeyboardEdgeTracker(CombatInputSettings? bindings = null)
    {
        var input = (bindings ?? CombatInputSettings.Default).Validate();
        // Modifiers precede actions; aim entry precedes profile/speed keys in the same poll.
        _keys = new[] { Key.ControlLeft, Key.ControlRight, Key.AltLeft, Key.AltRight, input.TorpedoAim, input.CountermissileAim }
            .Concat(StandardKeys).Concat(input.MappedKeys).Distinct().ToArray();
    }

    private Func<Key, bool> Reader(IKeyboard keyboard)
    {
        if (!ReferenceEquals(_keyboard, keyboard)) { _keyboard = keyboard; _reader = keyboard.IsKeyPressed; }
        return _reader!;
    }

    private static bool Down(Key key, Func<Key, bool> reader, bool control) => reader(key) && (key is not (Key.I or Key.S) || control);

    public (int pressedCount, int releasedCount) PollBoth(IKeyboard keyboard, Span<Key> pressed, Span<Key> released) =>
        PollBoth(Reader(keyboard), pressed, released);

    internal (int pressedCount, int releasedCount) PollBoth(Func<Key, bool> isKeyPressed, Span<Key> pressed, Span<Key> released)
    {
        int p = 0, r = 0;
        bool control = isKeyPressed(Key.ControlLeft) || isKeyPressed(Key.ControlRight);
        foreach (var key in _keys)
        {
            bool down = Down(key, isKeyPressed, control);
            bool previous = _previous.GetValueOrDefault(key);
            if (down && !previous) pressed[p++] = key;
            else if (!down && previous) released[r++] = key;
            _previous[key] = down;
        }
        return (p, r);
    }

    public int Poll(IKeyboard keyboard, Span<Key> pressed) => Poll(Reader(keyboard), pressed);
    internal int Poll(Func<Key, bool> isKeyPressed, Span<Key> pressed)
    {
        int count = 0;
        bool control = isKeyPressed(Key.ControlLeft) || isKeyPressed(Key.ControlRight);
        foreach (var key in _keys)
        {
            bool down = Down(key, isKeyPressed, control);
            if (down && !_previous.GetValueOrDefault(key)) pressed[count++] = key;
            _previous[key] = down;
        }
        return count;
    }

    public int PollUpKeys(IKeyboard keyboard, Span<Key> released) => PollUpKeys(Reader(keyboard), released);
    internal int PollUpKeys(Func<Key, bool> isKeyPressed, Span<Key> released)
    {
        int count = 0;
        foreach (var key in new[] { Key.ControlLeft, Key.ControlRight })
        {
            bool down = isKeyPressed(key);
            if (!down && _previous.GetValueOrDefault(key)) released[count++] = key;
            _previous[key] = down;
        }
        return count;
    }

    public void ResyncToCurrentState(IKeyboard? keyboard)
    {
        if (keyboard is not null) ResyncToCurrentState(Reader(keyboard));
    }
    internal void ResyncToCurrentState(Func<Key, bool> isKeyPressed)
    {
        bool control = isKeyPressed(Key.ControlLeft) || isKeyPressed(Key.ControlRight);
        foreach (var key in _keys) _previous[key] = Down(key, isKeyPressed, control);
    }
}
