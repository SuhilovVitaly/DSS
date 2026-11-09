using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using Silk.NET.Input;
using SkiaSharp;
using System.Reflection;
using Silk.NET.Windowing;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class KeyboardRoutingTests
{
    [Fact]
    public async Task D_toggles_decoy_only_in_fire_phase()
    {
        await using (var move = await View.Ep0019Scenes.Create("1-move"))
        {
            move.Screen.OnKeyDown(Key.D);
            Assert.Null(move.Screen.CaptureFrame(1920, 1080)!.FirePlan);
        }
        await using var fire = await View.Ep0019Scenes.Create("2-fire");
        string selected = fire.Screen.CaptureFrame(1920, 1080)!.FirePlan!.SelectedDroneId!;
        fire.Screen.OnKeyDown(Key.D);
        Assert.True(fire.Screen.BuildFireRequest().Targets.Single(t => t.DroneId == selected).Decoy);
        fire.Screen.OnKeyDown(Key.D, KeyModifiers.None, true);
        fire.Screen.OnKeyDown(Key.D, KeyModifiers.Control, false);
        Assert.True(fire.Screen.BuildFireRequest().Targets.Single(t => t.DroneId == selected).Decoy);
        fire.Screen.OnKeyDown(Key.D);
        Assert.False(fire.Screen.BuildFireRequest().Targets.Single(t => t.DroneId == selected).Decoy);
    }

    [Fact]
    public async Task E_toggles_evade_only_in_fire_phase()
    {
        await using (var move = await View.Ep0019Scenes.Create("1-move"))
        {
            move.Screen.OnKeyDown(Key.E);
            Assert.Null(move.Screen.CaptureFrame(1920, 1080)!.FirePlan);
        }
        await using var fire = await View.Ep0019Scenes.Create("2-fire");
        var selected = fire.Screen.CaptureFrame(1920, 1080)!.FirePlan!.SelectedDroneId;
        fire.Screen.OnKeyDown(Key.E);
        Assert.True(fire.Screen.BuildFireRequest().Targets.Single(t => t.DroneId == selected).Evade);
        fire.Screen.OnKeyDown(Key.E, KeyModifiers.None, true);
        Assert.True(fire.Screen.BuildFireRequest().Targets.Single(t => t.DroneId == selected).Evade);
        fire.Screen.OnKeyDown(Key.E);
        Assert.False(fire.Screen.BuildFireRequest().Targets.Single(t => t.DroneId == selected).Evade);
    }

    [Fact]
    public void WindowPollRoutesModifiersRepeatsAndFocusResetThroughActualScreenInterface()
    {
        var screen = new BattleScreen();
        using var window = new SkiaWindow(screen, new Factory(), null, createNativeWindow: false);
        var keyboard = DispatchProxy.Create<IKeyboard, KeyboardProxy>();
        var down = ((KeyboardProxy)(object)keyboard).Down;
        Set("_keyboard", keyboard);
        Set("_window", DispatchProxy.Create<IWindow, WindowProxy>());
        var tracker = (KeyboardEdgeTracker)typeof(SkiaWindow).GetField("_keyboardEdges", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
        tracker.RepeatDelayMilliseconds = 0;
        down.UnionWith([Key.ShiftLeft, Key.Left]);
        Invoke("PollKeyboard");
        Assert.Contains((Key.Left, KeyModifiers.Shift, false), screen.Events);
        Assert.Contains((Key.Left, KeyModifiers.Shift, true), screen.Events);
        Invoke("OnFocusChanged", false);
        Assert.True(screen.Reset);
        int count = screen.Events.Count;
        Invoke("OnFocusChanged", true);
        Invoke("PollKeyboard");
        Assert.Equal(count, screen.Events.Count);
        void Set(string name, object value) => typeof(SkiaWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, value);
        void Invoke(string name, params object[] args) => typeof(SkiaWindow).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, args);
    }
    [Theory]
    [InlineData(Key.F8)]
    [InlineData(Key.Number0)]
    [InlineData(Key.Keypad0)]
    [InlineData(Key.KeypadEnter)]
    public void StandardBattleKeysHaveOneEdgeWithoutAnyBinding(Key key)
    {
        var tracker = new KeyboardEdgeTracker();
        var down = new HashSet<Key> { key };
        Assert.Equal(new[] { key }, Poll(tracker, down, 0));
        Assert.Empty(Poll(tracker, down, 1000));
        Assert.Empty(Repeat(tracker, 1000));
        down.Clear(); Poll(tracker, down, 1100);
        down.Add(key);
        Assert.Equal(new[] { key }, Poll(tracker, down, 1200));
    }

    [Theory]
    [InlineData(Key.ShiftLeft, KeyModifiers.Shift)]
    [InlineData(Key.ShiftRight, KeyModifiers.Shift)]
    [InlineData(Key.ControlLeft, KeyModifiers.Control)]
    [InlineData(Key.ControlRight, KeyModifiers.Control)]
    [InlineData(Key.AltLeft, KeyModifiers.Alt)]
    [InlineData(Key.AltRight, KeyModifiers.Alt)]
    public void ModifierSnapshotIncludesEitherPhysicalSide(Key modifier, KeyModifiers expected)
    {
        var tracker = new KeyboardEdgeTracker();
        var down = new HashSet<Key> { modifier, Key.Tab };
        Assert.Contains(Key.Tab, Poll(tracker, down, 0));
        Assert.Equal(expected, tracker.Modifiers);
        down.Clear(); Poll(tracker, down, 1);
        Assert.Equal(KeyModifiers.None, tracker.Modifiers);
    }

    [Fact]
    public void ArrowRepeatHasInitialDelayPeriodAndNoLagBurstAndUsesCurrentModifiers()
    {
        var tracker = new KeyboardEdgeTracker();
        var down = new HashSet<Key> { Key.Up, Key.Right, Key.Enter };
        Poll(tracker, down, 0);
        Assert.Empty(Repeat(tracker, 399));
        Assert.Equal(new[] { Key.Up, Key.Right }, Repeat(tracker, 400));
        Assert.Empty(Repeat(tracker, 479));
        down.Add(Key.ShiftLeft);
        Poll(tracker, down, 480);
        Assert.Equal(KeyModifiers.Shift, tracker.Modifiers);
        Assert.Equal(new[] { Key.Up, Key.Right }, Repeat(tracker, 480));
        Assert.Equal(new[] { Key.Up, Key.Right }, Repeat(tracker, 5000));
        Assert.Empty(Repeat(tracker, 5000));
        down.Clear(); Poll(tracker, down, 5010);
        Assert.Empty(Repeat(tracker, 10000));
    }

    [Fact]
    public void FocusResyncAndScreenResetSuppressHeldRepeatUntilGenuineNewPress()
    {
        var tracker = new KeyboardEdgeTracker();
        var down = new HashSet<Key> { Key.Left };
        Poll(tracker, down, 0);
        tracker.ResyncToCurrentState(down.Contains);
        Assert.Empty(Poll(tracker, down, 1000));
        Assert.Empty(Repeat(tracker, 1000));
        down.Clear(); Poll(tracker, down, 1010);
        down.Add(Key.Left); Poll(tracker, down, 1100);
        Assert.Equal(new[] { Key.Left }, Repeat(tracker, 1500));
        tracker.ResetRepeats();
        Assert.Empty(Repeat(tracker, 2000));
        down.Clear(); Poll(tracker, down, 2010);
        down.Add(Key.Left); Poll(tracker, down, 2020);
        Assert.Equal(new[] { Key.Left }, Repeat(tracker, 2420));
    }

    [Fact]
    public void RepeatTimingCanBeConfiguredWithoutChangingEdges()
    {
        var tracker = new KeyboardEdgeTracker { RepeatDelayMilliseconds = 100, RepeatPeriodMilliseconds = 25 };
        Poll(tracker, new() { Key.Down }, 0);
        Assert.Empty(Repeat(tracker, 99));
        Assert.Equal(new[] { Key.Down }, Repeat(tracker, 100));
        Assert.Empty(Repeat(tracker, 124));
        Assert.Equal(new[] { Key.Down }, Repeat(tracker, 125));
    }

    [Fact]
    public void DefaultScreenOverloadPreservesLegacyDispatchAndBattleReceivesModifiersAndReset()
    {
        IScreen legacy = new LegacyScreen();
        Assert.False(legacy.WantsArrowRepeat);
        legacy.OnKeyDown(Key.Tab, KeyModifiers.Shift, repeat: false);
        Assert.Equal(Key.Tab, ((LegacyScreen)legacy).Last);
        IScreen battle = new BattleScreen();
        Assert.True(battle.WantsArrowRepeat);
        battle.OnKeyDown(Key.Left, KeyModifiers.Control | KeyModifiers.Shift, repeat: true);
        var actual = (BattleScreen)battle;
        Assert.Equal((Key.Left, KeyModifiers.Control | KeyModifiers.Shift, true), actual.Event);
        battle.OnInputReset();
        Assert.True(actual.Reset);
    }

    private static Key[] Poll(KeyboardEdgeTracker tracker, HashSet<Key> down, long now)
    {
        Span<Key> pressed = stackalloc Key[64];
        Span<Key> released = stackalloc Key[64];
        var result = tracker.PollBoth(down.Contains, pressed, released, now);
        return pressed[..result.pressedCount].ToArray();
    }
    private static Key[] Repeat(KeyboardEdgeTracker tracker, long now)
    {
        Span<Key> repeated = stackalloc Key[4];
        int count = tracker.PollRepeats(now, repeated);
        return repeated[..count].ToArray();
    }
    private class LegacyScreen : IScreen
    {
        public Key Last { get; private set; }
        public ScreenEvent OnKeyDown(Key key) { Last = key; return ScreenEvent.None; }
        public void Render(SKCanvas canvas, int width, int height) { }
        public ScreenEvent OnMouseDown(float x, float y, MouseButton button) => ScreenEvent.None;
        public bool OnMouseMove(float x, float y) => false;
        public ScreenEvent OnMouseWheel(float x, float y, float delta) => ScreenEvent.None;
        public void OnActivated() { }
        public void OnDeactivated() { }
    }
    private sealed class BattleScreen : LegacyScreen, IScreen
    {
        public bool WantsArrowRepeat => true;
        public (Key, KeyModifiers, bool) Event { get; private set; }
        public List<(Key, KeyModifiers, bool)> Events { get; } = [];
        public bool Reset { get; private set; }
        public ScreenEvent OnKeyDown(Key key, KeyModifiers modifiers, bool repeat) { Event = (key, modifiers, repeat); Events.Add(Event); return ScreenEvent.None; }
        public void OnInputReset() => Reset = true;
    }
    public class KeyboardProxy : DispatchProxy
    {
        internal HashSet<Key> Down { get; } = [];
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.Name switch
        {
            "IsKeyPressed" => Down.Contains((Key)args![0]!),
            "add_KeyChar" or "remove_KeyChar" => null,
            _ => throw new NotSupportedException(method.Name)
        };
    }
    public class WindowProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.Name switch
        {
            "set_TopMost" or "Dispose" => null,
            _ => throw new NotSupportedException(method.Name)
        };
    }
    private sealed class Factory : IGameSessionFactory
    {
        public IGameSessionConnection CreateSession() => throw new NotSupportedException();
        public IGameSessionConnection CreateSessionFromSave(string slotId) => throw new NotSupportedException();
        public IGameSessionConnection CreateSessionFromScenario(string path) => throw new NotSupportedException();
        public ScenarioInfo[] ListScenarios() => [];
        public SaveSlotInfo[] ListSaveSlots() => [];
        public bool HasQuickSave() => false;
        public void DeleteSaveSlot(string id) { }
    }
}
