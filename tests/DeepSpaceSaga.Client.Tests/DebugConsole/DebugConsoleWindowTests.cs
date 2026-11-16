using System.Numerics;
using System.Reflection;
using DeepSpaceSaga.Client.Tests.TacticalBattle;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.DebugConsole;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameMenu;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.MainMenu;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.LocalClient;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.DebugConsole;

/// <summary>EP-0030 US-0001 TK-0003: the console overlay owns window input while open and never touches the screen stack or speed.</summary>
[Collection("InterfaceLog")]
public sealed class DebugConsoleWindowTests
{
    [Fact]
    public void ChordOpensConsoleOnMainMenuWithoutSession()
    {
        var factory = new NoSessionFactory();
        var menu = new MainMenuScreen();
        using var window = new SkiaWindow(menu, factory, null, createNativeWindow: false);
        var keys = AttachKeyboard(window);

        keys.UnionWith([Key.ControlRight, Key.AltRight, Key.P]);
        Poll(window);

        Assert.True(window.DebugConsole.IsOpen);
        var screens = Field<ScreenStack>(window, "_screens");
        Assert.Same(menu, screens.Current);
        Assert.Equal(1, screens.Count); // the overlay is not a stack screen
        Assert.Null(Field<GameSessionHandle?>(window, "_session"));
        Assert.Null(((IDebugConsoleHost)window).Session);
        Assert.Null(((IDebugConsoleHost)window).ActiveBattle);
        Assert.Equal(0, factory.Calls); // no world is created

        // An unknown command reports an error and still creates nothing.
        keys.Clear();
        Poll(window);
        foreach (char c in "spawn") Type(window, c);
        keys.Add(Key.Enter);
        Poll(window);
        Assert.Equal("Неизвестная команда: spawn. Введите help", window.DebugConsole.Output[^1]);
        Assert.Equal(0, factory.Calls);
        Assert.Same(menu, screens.Current);
    }

    [Fact]
    public async Task ChordOpensOverMapAndTutorialBattleWithoutSpeedChange()
    {
        await using (var map = await BattleNavigationTests.Fixture.Create())
        {
            var keys = AttachKeyboard(map.Window);
            int speedCommands = map.Proxy.Speeds.Count;
            keys.UnionWith([Key.ControlLeft, Key.AltLeft, Key.P]);
            Poll(map.Window);
            Assert.True(map.Window.DebugConsole.IsOpen);
            Assert.Same(map.Game, map.Screens.Current);
            Assert.Equal(1, map.Screens.Count);
            await Task.Delay(150);
            Assert.Equal(SimulationSpeed.Speed2, map.Engine.CurrentSpeed);
            Assert.Equal(SimulationSpeed.Speed2, map.Handle.Buffer.CurrentSpeed);

            keys.Remove(Key.P); Poll(map.Window);
            keys.Add(Key.P); Poll(map.Window); // second chord closes
            Assert.False(map.Window.DebugConsole.IsOpen);
            await Task.Delay(150);
            Assert.Equal(speedCommands, map.Proxy.Speeds.Count);
            Assert.Equal(SimulationSpeed.Speed2, map.Engine.CurrentSpeed);
            Assert.Same(map.Game, map.Screens.Current);
        }

        await using var tutorial = new TutorialFixture();
        await tutorial.Route(ScreenEvent.NewGame);
        var battle = Assert.IsType<TacticalBattleScreen>(tutorial.Screens.Current);
        Assert.True(battle.Flow.IsTutorial);
        var tutorialKeys = AttachKeyboard(tutorial.Window);
        var speed = tutorial.Engine!.CurrentSpeed;
        int tutorialSpeedCommands = tutorial.Proxy!.Speeds.Count;
        int depth = tutorial.Screens.Count;

        tutorialKeys.UnionWith([Key.ControlRight, Key.AltLeft, Key.P]);
        Poll(tutorial.Window);
        Assert.True(tutorial.Window.DebugConsole.IsOpen);
        Assert.Same(battle, tutorial.Screens.Current);
        tutorialKeys.Clear(); Poll(tutorial.Window);
        tutorialKeys.Add(Key.Escape); Poll(tutorial.Window);
        await PendingTransition(tutorial.Window);

        Assert.False(tutorial.Window.DebugConsole.IsOpen);
        Assert.Same(battle, tutorial.Screens.Current); // Esc did not reach the battle (no exit confirmation)
        Assert.Equal(depth, tutorial.Screens.Count);
        Assert.Equal(speed, tutorial.Engine.CurrentSpeed);
        Assert.Equal(tutorialSpeedCommands, tutorial.Proxy.Speeds.Count);
        Assert.True(tutorial.Engine.IsTacticalBattleActive);
    }

    [Fact]
    public void KeysCharsAndClicksDoNotReachScreenWhileOpen()
    {
        var screen = new RecordingScreen();
        using var window = new SkiaWindow(screen, new NoSessionFactory(), null, createNativeWindow: false);
        var keys = AttachKeyboard(window);
        keys.UnionWith([Key.ControlLeft, Key.AltLeft, Key.P]);
        Poll(window);
        Assert.True(window.DebugConsole.IsOpen);
        Assert.Equal(1, screen.Resets);
        Type(window, 'p'); // the chord's own character is dropped
        Type(window, 'з');
        Assert.Equal("", window.DebugConsole.Input.Text);
        int before = screen.Events.Count;

        keys.Clear(); Poll(window); // releases stay with the console
        foreach (var key in new[] { Key.Space, Key.S, Key.F5, Key.F9, Key.Tab, Key.Enter, Key.Left, Key.Backspace })
        {
            keys.Add(key); Poll(window);
            keys.Remove(key); Poll(window);
        }
        foreach (char c in "ab c") Type(window, c);
        var mouse = DispatchProxy.Create<IMouse, MouseProxy>();
        ((MouseProxy)(object)mouse).Position = new Vector2(30, 30);
        Invoke(window, "OnMouseDown", mouse, MouseButton.Left);
        Invoke(window, "OnMouseMove", mouse, new Vector2(40, 40));
        Invoke(window, "OnMouseUp", mouse, MouseButton.Left);
        Invoke(window, "OnMouseDown", mouse, MouseButton.Right);
        Invoke(window, "OnMouseScroll", mouse, new ScrollWheel(0, -1));

        Assert.Equal(before, screen.Events.Count);
        Assert.Equal("ab c", window.DebugConsole.Input.Text);
        Assert.Empty(window.DebugConsole.Output); // Enter on the empty line was a no-op
        Assert.True(window.DebugConsole.IsOpen);
        Assert.Null(Field<Task?>(window, "_pendingKeyboardTransition"));
    }

    [Fact]
    public async Task EscapeClosesOnlyConsole()
    {
        await using var map = await BattleNavigationTests.Fixture.Create();
        var keys = AttachKeyboard(map.Window);
        keys.UnionWith([Key.ControlLeft, Key.AltLeft, Key.P]);
        Poll(map.Window);
        keys.Clear(); Poll(map.Window);
        Assert.True(map.Window.DebugConsole.IsOpen);

        keys.Add(Key.Escape); Poll(map.Window);
        await PendingTransition(map.Window);
        Assert.False(map.Window.DebugConsole.IsOpen);
        Assert.Same(map.Game, map.Screens.Current); // no game menu
        Assert.Equal(1, map.Screens.Count);

        // Holding Esc after closing does not replay it; a fresh press keeps its old meaning.
        Poll(map.Window);
        await PendingTransition(map.Window);
        Assert.Equal(1, map.Screens.Count);
        keys.Clear(); Poll(map.Window);
        keys.Add(Key.Escape); Poll(map.Window);
        await PendingTransition(map.Window);
        Assert.IsType<GameMenuScreen>(map.Screens.Current);
    }

    [Fact]
    public void HeldChordTogglesOnce()
    {
        var screen = new RecordingScreen();
        using var window = new SkiaWindow(screen, new NoSessionFactory(), null, createNativeWindow: false);
        var keys = AttachKeyboard(window);
        keys.UnionWith([Key.ControlLeft, Key.AltRight, Key.P]);
        for (int i = 0; i < 10; i++)
        {
            Poll(window);
            Assert.True(window.DebugConsole.IsOpen);
        }
        Assert.Equal(1, screen.Resets);

        keys.Remove(Key.P); Poll(window);
        Assert.True(window.DebugConsole.IsOpen);
        keys.Add(Key.P);
        for (int i = 0; i < 10; i++)
        {
            Poll(window);
            Assert.False(window.DebugConsole.IsOpen);
        }
        Assert.DoesNotContain(screen.Events, e => e == "down:P");
        Type(window, 'p');
        Assert.DoesNotContain(screen.Events, e => e.StartsWith("char:", StringComparison.Ordinal));

        // Bare P without the chord still reaches the screen as before.
        keys.Clear(); Poll(window);
        keys.Add(Key.P); Poll(window);
        Assert.Contains("down:P", screen.Events);
        Assert.False(window.DebugConsole.IsOpen);
    }

    [Fact]
    public void CloseDoesNotReplayHeldKeys()
    {
        var screen = new RecordingScreen();
        using var window = new SkiaWindow(screen, new NoSessionFactory(), null, createNativeWindow: false);
        var keys = AttachKeyboard(window);
        Field<KeyboardEdgeTracker>(window, "_keyboardEdges").RepeatDelayMilliseconds = 0;
        keys.UnionWith([Key.ControlLeft, Key.AltLeft, Key.P]);
        Poll(window);
        keys.Remove(Key.P); Poll(window);
        int before = screen.Events.Count;

        keys.UnionWith([Key.Space, Key.Left]); // pressed while the console is open
        Poll(window);
        keys.Add(Key.P); Poll(window); // chord closes while Space/Left are still held
        Assert.False(window.DebugConsole.IsOpen);
        for (int i = 0; i < 5; i++) Poll(window);

        Assert.DoesNotContain(screen.Events.Skip(before), e => e.StartsWith("down:", StringComparison.Ordinal));
        keys.Remove(Key.Left); keys.Remove(Key.Space); Poll(window);
        keys.Add(Key.Space); Poll(window);
        Assert.Contains("down:Space", screen.Events.Skip(before)); // a genuine new press works again
    }

    [Fact]
    public async Task RunExclusiveReportsBusyWhileTransitionLockHeld()
    {
        using var window = new SkiaWindow(new RecordingScreen(), new NoSessionFactory(), null, createNativeWindow: false);
        IDebugConsoleHost host = window;
        var transitionLock = Field<SemaphoreSlim>(window, "_transitionLock");
        Assert.False(host.IsTransitionBusy);

        await transitionLock.WaitAsync();
        int calls = 0;
        var busy = await host.RunExclusiveAsync(_ => { calls++; return Task.FromResult(DebugCommandResult.Ok("ran")); }, CancellationToken.None);
        Assert.True(host.IsTransitionBusy);
        Assert.Equal(DebugCommandResult.BusyText, Assert.Single(busy.Lines));
        Assert.True(busy.IsError);
        Assert.Equal(0, calls);
        transitionLock.Release();

        bool heldInside = false;
        var ran = await host.RunExclusiveAsync(async _ => { await Task.Yield(); heldInside = host.IsTransitionBusy; return DebugCommandResult.Ok("ran"); }, CancellationToken.None);
        Assert.Equal("ran", Assert.Single(ran.Lines));
        Assert.True(heldInside);
        Assert.False(host.IsTransitionBusy);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            host.RunExclusiveAsync(_ => throw new InvalidOperationException("boom"), CancellationToken.None));
        Assert.False(host.IsTransitionBusy); // released after a failing operation too
    }

    [Fact]
    public void OverlayRendersAboveStack()
    {
        var screen = new RecordingScreen();
        using var window = new SkiaWindow(screen, new NoSessionFactory(), null, createNativeWindow: false);
        using var surface = SKSurface.Create(new SKImageInfo(400, 300));

        window.RenderScreenStack(surface.Canvas, 400, 300, out _);
        surface.Canvas.Flush();
        Assert.Equal(SKColors.Red, Pixel(surface, 200, 10));

        window.DebugConsole.Open();
        surface.Canvas.Clear(SKColors.Black);
        int drawn = window.RenderScreenStack(surface.Canvas, 400, 300, out _);
        surface.Canvas.Flush();
        Assert.Equal(1, drawn); // the overlay is not counted as a screen
        var top = Pixel(surface, 200, 3);
        Assert.True(top.Red < 100, $"overlay must cover the screen at the top, got {top}");
        Assert.Equal(SKColors.Red, Pixel(surface, 200, 290)); // the lower part of the screen stays visible
        Assert.Equal(1, Field<ScreenStack>(window, "_screens").Count);

        window.DebugConsole.Close();
        window.RenderScreenStack(surface.Canvas, 400, 300, out _);
        surface.Canvas.Flush();
        Assert.Equal(SKColors.Red, Pixel(surface, 200, 3));
    }

    private static SKColor Pixel(SKSurface surface, int x, int y)
    {
        using var image = surface.Snapshot();
        using var bitmap = SKBitmap.FromImage(image);
        return bitmap.GetPixel(x, y);
    }

    private static HashSet<Key> AttachKeyboard(SkiaWindow window)
    {
        var keyboard = DispatchProxy.Create<IKeyboard, KeyboardRoutingTests.KeyboardProxy>();
        typeof(SkiaWindow).GetField("_keyboard", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, keyboard);
        return ((KeyboardRoutingTests.KeyboardProxy)(object)keyboard).Down;
    }

    private static void Poll(SkiaWindow window) => Invoke(window, "PollKeyboard");

    private static void Type(SkiaWindow window, char c) =>
        Invoke(window, "OnKeyChar", Field<IKeyboard>(window, "_keyboard"), c);

    private static async Task PendingTransition(SkiaWindow window)
    {
        if (Field<Task?>(window, "_pendingKeyboardTransition") is { } task) await task;
    }

    private static void Invoke(SkiaWindow window, string name, params object[] args) =>
        typeof(SkiaWindow).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, args);

    private static T Field<T>(object owner, string name) =>
        (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;

    private sealed class RecordingScreen : IScreen
    {
        internal List<string> Events { get; } = [];
        internal int Resets { get; private set; }
        public bool WantsArrowRepeat => true;
        public void Render(SKCanvas canvas, int width, int height) => canvas.Clear(SKColors.Red);
        public ScreenEvent OnMouseDown(float x, float y, MouseButton button) { Events.Add("mouse-down"); return ScreenEvent.None; }
        public void OnMouseUp(float x, float y, MouseButton button) => Events.Add("mouse-up");
        public bool OnMouseMove(float x, float y) { Events.Add("mouse-move"); return true; }
        public ScreenEvent OnMouseWheel(float x, float y, float delta) { Events.Add("wheel"); return ScreenEvent.None; }
        public ScreenEvent OnKeyDown(Key key) => OnKeyDown(key, KeyModifiers.None, false);
        public ScreenEvent OnKeyDown(Key key, KeyModifiers modifiers, bool repeat) { Events.Add((repeat ? "repeat:" : "down:") + key); return ScreenEvent.None; }
        public void OnKeyUp(Key key) => Events.Add("up:" + key);
        public void OnTextInput(char c) => Events.Add("char:" + c);
        public void OnInputReset() => Resets++;
        public void OnActivated() { }
        public void OnDeactivated() { }
    }

    public class MouseProxy : DispatchProxy
    {
        internal Vector2 Position { get; set; }
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.Name switch
        {
            "get_Position" => Position,
            _ => throw new NotSupportedException(method.Name)
        };
    }

    private sealed class NoSessionFactory : IGameSessionFactory
    {
        internal int Calls;
        public IGameSessionConnection CreateSession() { Calls++; throw new NotSupportedException(); }
        public IGameSessionConnection CreateSessionFromSave(string slotId) { Calls++; throw new NotSupportedException(); }
        public IGameSessionConnection CreateSessionFromScenario(string path) { Calls++; throw new NotSupportedException(); }
        public ScenarioInfo[] ListScenarios() => [];
        public SaveSlotInfo[] ListSaveSlots() => [];
        public bool HasQuickSave() => false;
        public void DeleteSaveSlot(string id) { }
    }

    /// <summary>Main menu → New Game without saves → tutorial battle, with a speed-recording connection (pattern of TutorialFlowTests).</summary>
    private sealed class TutorialFixture : IGameSessionFactory, IAsyncDisposable
    {
        private readonly string _directory;
        private readonly string _saveDirectory;
        internal readonly SkiaWindow Window;
        internal SimulationEngine? Engine { get; private set; }
        internal BattleNavigationTests.ConnectionProxy? Proxy { get; private set; }
        internal ScreenStack Screens => Field<ScreenStack>(Window, "_screens");

        internal TutorialFixture()
        {
            string root = Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") ?? Path.GetTempPath();
            _directory = Path.Combine(root, $"ep0030-console-{Guid.NewGuid():N}");
            _saveDirectory = Path.Combine(_directory, "Saves");
            Directory.CreateDirectory(_saveDirectory);
            string settingsPath = Path.Combine(_directory, "Settings.json");
            File.WriteAllText(settingsPath, """{"gameSettings":{"tacticalBattleSkinId":"classic"}}""");
            Window = new SkiaWindow(new MainMenuScreen(), this, null, createNativeWindow: false, settingsPath);
        }

        internal Task Route(ScreenEvent evt) =>
            (Task)typeof(SkiaWindow).GetMethod("HandleScreenEvent", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(Window, new object?[] { evt, null })!;

        public string? GetTutorialScenarioPath() => Path.Combine(AppContext.BaseDirectory, "Scenarios", "Tutorial", "scenario.json");

        public IGameSessionConnection CreateSessionFromScenario(string scenarioPath)
        {
            Engine = SimulationEngine.CreateFromScenarioFile(Path.Combine(AppContext.BaseDirectory, "Settings.json"), scenarioPath);
            var connection = DispatchProxy.Create<IGameSessionConnection, BattleNavigationTests.ConnectionProxy>();
            Proxy = (BattleNavigationTests.ConnectionProxy)(object)connection;
            Proxy.Inner = new LocalGameSessionConnection(Engine, _saveDirectory);
            return connection;
        }

        public IGameSessionConnection CreateSession() => throw new NotSupportedException();
        public IGameSessionConnection CreateSessionFromSave(string slotId) => throw new NotSupportedException();
        public ScenarioInfo[] ListScenarios() => [];
        public SaveSlotInfo[] ListSaveSlots() => [];
        public bool HasQuickSave() => false;
        public void DeleteSaveSlot(string slotId) { }

        public async ValueTask DisposeAsync()
        {
            try
            {
                Window.Dispose();
                if (Proxy is { } proxy) await proxy.Inner.DisposeAsync();
            }
            finally
            {
                try { Directory.Delete(_directory, recursive: true); }
                catch (IOException) { }
            }
        }
    }
}
