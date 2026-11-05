using Silk.NET.Input;
using DeepSpaceSaga.Client.UI.Screens;

namespace DeepSpaceSaga.Client.UI;

/// <summary>One physical edge state for native/test polling, mapped bindings and focus resync.</summary>
internal sealed class KeyboardEdgeTracker
{
    private static readonly Key[] StandardKeys =
    [
        Key.Escape, Key.I, Key.S, Key.F, Key.Space, Key.Tab, Key.Enter,
        Key.Number0, Key.Keypad0, Key.KeypadEnter, Key.F8,
        Key.Number1, Key.Number2, Key.Number3, Key.Number4, Key.Number5,
        Key.Up, Key.Down, Key.Left, Key.Right, Key.F5, Key.F9, Key.F10, Key.C, Key.Backspace,
        Key.Equal, Key.Minus, Key.KeypadAdd, Key.KeypadSubtract, Key.Home, Key.End
    ];
    private readonly Key[] _keys;
    private readonly Dictionary<Key, bool> _previous = new();
    private readonly Dictionary<Key, long> _nextRepeat = new();
    internal int RepeatDelayMilliseconds { get; set; } = 400;
    internal int RepeatPeriodMilliseconds { get; set; } = 80;
    internal KeyModifiers Modifiers { get; private set; }
    private IKeyboard? _keyboard;
    private Func<Key, bool>? _reader;

    internal KeyboardEdgeTracker(CombatInputSettings? bindings = null, TacticalBattleInputSettings? tactical = null)
    {
        var input = (bindings ?? CombatInputSettings.Default).Validate();
        var battle = tactical ?? TacticalBattleInputSettings.Default;
        RepeatDelayMilliseconds = battle.RepeatDelayMs;
        RepeatPeriodMilliseconds = battle.RepeatPeriodMs;
        // Modifiers precede actions; aim entry precedes profile/speed keys in the same poll.
        _keys = new[] { Key.ControlLeft, Key.ControlRight, Key.AltLeft, Key.AltRight, Key.ShiftLeft, Key.ShiftRight, input.TorpedoAim, input.CountermissileAim }
            .Concat(StandardKeys).Concat(input.MappedKeys).Append(battle.TacticalBattle).Distinct().ToArray();
    }

    private Func<Key, bool> Reader(IKeyboard keyboard)
    {
        if (!ReferenceEquals(_keyboard, keyboard)) { _keyboard = keyboard; _reader = keyboard.IsKeyPressed; }
        return _reader!;
    }

    private static bool Down(Key key, Func<Key, bool> reader, bool control) => reader(key) && (key != Key.I || control);

    public (int pressedCount, int releasedCount) PollBoth(IKeyboard keyboard, Span<Key> pressed, Span<Key> released) =>
        PollBoth(Reader(keyboard), pressed, released);

    internal (int pressedCount, int releasedCount) PollBoth(Func<Key, bool> isKeyPressed, Span<Key> pressed, Span<Key> released, long? nowMs = null)
    {
        int p = 0, r = 0;
        bool control = isKeyPressed(Key.ControlLeft) || isKeyPressed(Key.ControlRight);
        Modifiers = ReadModifiers(isKeyPressed);
        long now = nowMs ?? Environment.TickCount64;
        foreach (var key in _keys)
        {
            bool down = Down(key, isKeyPressed, control);
            bool previous = _previous.GetValueOrDefault(key);
            if (down && !previous)
            {
                pressed[p++] = key;
                if (IsArrow(key)) _nextRepeat[key] = now + RepeatDelayMilliseconds;
            }
            else if (!down && previous) released[r++] = key;
            if (!down) _nextRepeat.Remove(key);
            _previous[key] = down;
        }
        return (p, r);
    }

    private static bool IsArrow(Key key) => key is Key.Up or Key.Down or Key.Left or Key.Right;
    internal static KeyModifiers ReadModifiers(Func<Key, bool> reader) =>
        (reader(Key.ShiftLeft) || reader(Key.ShiftRight) ? KeyModifiers.Shift : KeyModifiers.None)
        | (reader(Key.ControlLeft) || reader(Key.ControlRight) ? KeyModifiers.Control : KeyModifiers.None)
        | (reader(Key.AltLeft) || reader(Key.AltRight) ? KeyModifiers.Alt : KeyModifiers.None);
    internal int PollRepeats(long nowMs, Span<Key> repeated)
    {
        int count = 0;
        foreach (var key in _keys)
            if (_nextRepeat.TryGetValue(key, out long next) && nowMs >= next)
            {
                repeated[count++] = key;
                _nextRepeat[key] = nowMs + RepeatPeriodMilliseconds;
            }
        return count;
    }
    internal void ResetRepeats() => _nextRepeat.Clear();

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
        ResetRepeats();
        if (keyboard is not null) ResyncToCurrentState(Reader(keyboard));
    }
    internal void ResyncToCurrentState(Func<Key, bool> isKeyPressed)
    {
        ResetRepeats();
        Modifiers = ReadModifiers(isKeyPressed);
        bool control = isKeyPressed(Key.ControlLeft) || isKeyPressed(Key.ControlRight);
        foreach (var key in _keys) _previous[key] = Down(key, isKeyPressed, control);
    }
}
