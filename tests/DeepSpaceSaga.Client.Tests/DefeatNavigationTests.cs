using System.Reflection;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Defeat;
using DeepSpaceSaga.Client.UI.Screens.GameMenu;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.Load;
using DeepSpaceSaga.Client.UI.Screens.MainMenu;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests;

public class DefeatNavigationTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Terminal_snapshot_opens_one_defeat_screen_above_existing_modals(int overlayDepth)
    {
        await using var f = await Fixture.Create();
        f.Game.PrepareCombatReports();
        var assets = Get<CombatReportAssets>(f.Game, "_combatReportAssets");
        Assert.True(assets.IllustrationCount > 0);
        await f.Route(ScreenEvent.OpenGameMenu);
        Assert.IsType<GameMenuScreen>(f.Screens.Current);
        if (overlayDepth == 2) await f.Route(ScreenEvent.OpenLoadWindow);
        Assert.False(f.Game.CombatReportResourcesDisposed);
        Assert.Contains(Get<Action?>(f.Handle, "Closing")!.GetInvocationList(), d => ReferenceEquals(d.Target, f.Game));
        await f.Defeat();
        Invoke(f.Window, "PollGameSessionAutoTransition");
        if (Get<Task?>(f.Window, "_defeatTransitionTask") is { } pending) await pending;
        var screen = Assert.IsType<DefeatScreen>(f.Screens.Current);
        Assert.True(f.Game.CombatReportResourcesDisposed);
        Assert.True(f.Game.DebugPanel.IsDisposed);
        Assert.Equal(0, assets.IllustrationCount);
        Assert.Equal(0, assets.PortraitCount);
        Assert.DoesNotContain(Get<Action?>(f.Handle, "Closing")?.GetInvocationList() ?? [], d => ReferenceEquals(d.Target, f.Game));
        f.Game.PrepareCombatReports(); Assert.Empty(f.Game.ReportStore.Reports);
        for (int i = 0; i < 3; i++) await f.Route(ScreenEvent.None);
        Assert.Same(screen, f.Screens.Current);
        Assert.Equal(1, f.Screens.Count);
        foreach (var action in new[] { ScreenEvent.Resume, ScreenEvent.OpenGameMenu, ScreenEvent.OpenSaveWindow, ScreenEvent.CloseLoadWindow })
            await f.Route(action);
        Assert.Same(screen, f.Screens.Current);
        await f.Handle.SetSpeedAsync(SimulationSpeed.Speed4);
        Assert.Equal(SimulationSpeed.Speed0, f.Handle.Buffer.CurrentSpeed);
        Assert.Equal(ScreenEvent.None, f.Game.OnKeyDown(Key.Space));
        Assert.Equal(ScreenEvent.None, f.Game.OnKeyDown(Key.F5));
    }

    [Fact]
    public async Task Cancel_or_failed_load_keeps_defeat()
    {
        await using var f = await Fixture.Create();
        await f.Defeat();
        await f.Route(ScreenEvent.OpenLoadWindow);
        Assert.IsType<LoadScreen>(f.Screens.Current);
        Assert.IsType<DefeatScreen>(f.Screens.UnderCurrent);
        await f.Route(ScreenEvent.LoadSlotRequested, "missing");
        Assert.Same(f.Handle, Get<GameSessionHandle>(f.Window, "_session"));
        Assert.IsType<LoadScreen>(f.Screens.Current);
        Assert.True(f.Handle.IsDefeated);
        await f.Route(ScreenEvent.CloseLoadWindow);
        Assert.IsType<DefeatScreen>(f.Screens.Current);
        Assert.Equal(1, f.Screens.Count);
        Assert.Equal(SimulationSpeed.Speed0, f.Handle.Buffer.CurrentSpeed);
    }

    [Fact]
    public async Task Compatible_predeath_load_restores_play_and_main_menu_disposes_session()
    {
        await using var f = await Fixture.Create();
        await f.Connection.SaveAsync("before");
        await f.Defeat();
        await f.Route(ScreenEvent.OpenLoadWindow);
        await f.Route(ScreenEvent.LoadSlotRequested, "before");
        var fresh = Get<GameSessionHandle>(f.Window, "_session");
        Assert.NotSame(f.Handle, fresh);
        await WaitForSnapshot(fresh);
        Assert.False(fresh.IsDefeated);
        Assert.True(fresh.Buffer.Latest!.Snapshot.EffectiveOutcome.CanSave);
        Assert.Contains(fresh.Buffer.Latest.Snapshot.Objects, o => o.ObjectId == "SPC-0001");
        Assert.IsType<GameSessionScreen>(f.Screens.Current);
        Assert.Equal(1, f.Screens.Count);
        Assert.Equal(SimulationSpeed.Speed0, fresh.Buffer.CurrentSpeed);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => f.Connection.SetSimulationSpeedAsync(SimulationSpeed.Speed1).AsTask());
        await fresh.SaveAsync("restored");
        Assert.True(File.Exists(Path.Combine(f.DirectoryPath, "restored.json")));
        await f.Route(ScreenEvent.MainMenu);
        Assert.IsType<MainMenuScreen>(f.Screens.Current);
        Assert.Null(Get<GameSessionHandle?>(f.Window, "_session"));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => fresh.Connection.SetSimulationSpeedAsync(SimulationSpeed.Speed1).AsTask());
    }

    [Fact]
    public async Task Quick_save_never_overwrites_after_defeat_including_stale_save_callback()
    {
        await using var f = await Fixture.Create();
        await f.Connection.SaveAsync(SaveSlots.Quicksave);
        string path = Path.Combine(f.DirectoryPath, "quicksave.json");
        byte[] before = await File.ReadAllBytesAsync(path);
        await f.Defeat();
        await f.Route(ScreenEvent.QuickSave);
        await (Task)Invoke(f.Window, "SaveToSlotAsync", SaveSlots.Quicksave)!;
        Assert.Equal(CommandReasonCodes.SessionDefeated,
            (await Assert.ThrowsAsync<InvalidOperationException>(() => f.Handle.SaveAsync("auto").AsTask())).Message);
        Assert.Equal(before, await File.ReadAllBytesAsync(path));
        Assert.Single(Directory.GetFiles(f.DirectoryPath));
        Assert.IsType<DefeatScreen>(f.Screens.Current);
    }

    [Fact]
    public async Task Keyboard_focus_and_activation_reach_actual_window_event_router()
    {
        await using var f = await Fixture.Create();
        await f.Defeat();
        await f.Route(ScreenEvent.None);
        var tracker = Get<KeyboardEdgeTracker>(f.Window, "_keyboardEdges");
        var pressed = new Key[32]; var released = new Key[32];
        var keys = new HashSet<Key> { Key.Tab };
        var count = tracker.PollBoth(keys.Contains, pressed, released);
        Assert.Equal(Key.Tab, Assert.Single(pressed.Take(count.pressedCount)));
        foreach (var key in pressed.Take(count.pressedCount)) Invoke(f.Window, "HandleKeyboardEdge", key);
        keys.Clear(); tracker.PollBoth(keys.Contains, pressed, released);
        keys.Add(Key.Enter);
        count = tracker.PollBoth(keys.Contains, pressed, released);
        Assert.Equal(Key.Enter, Assert.Single(pressed.Take(count.pressedCount)));
        foreach (var key in pressed.Take(count.pressedCount)) Invoke(f.Window, "HandleKeyboardEdge", key);
        if (Get<Task?>(f.Window, "_pendingKeyboardTransition") is { } pending) await pending;
        Assert.IsType<MainMenuScreen>(f.Screens.Current);
        Assert.Null(Get<GameSessionHandle?>(f.Window, "_session"));
    }

    [Fact]
    public async Task Window_edge_buffers_hold_simultaneous_press_and_release_of_all_tracked_keys()
    {
        await using var f = await Fixture.Create();
        var tracker = Get<KeyboardEdgeTracker>(f.Window, "_keyboardEdges");
        var pressed = Get<Key[]>(f.Window, "_keyboardPressedKeys");
        var released = Get<Key[]>(f.Window, "_keyboardReleasedKeys");
        var down = tracker.PollBoth(_ => true, pressed, released);
        Assert.Contains(Key.Enter, pressed.Take(down.pressedCount));
        Assert.Contains(Key.Tab, pressed.Take(down.pressedCount));
        var up = tracker.PollBoth(_ => false, pressed, released);
        Assert.Equal(down.pressedCount, up.releasedCount);
        tracker.ResyncToCurrentState(_ => true);
        Assert.Equal(0, tracker.PollBoth(_ => true, pressed, released).pressedCount);
    }

    private static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
    private static object? Invoke(object target, string name, params object?[] args) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args);
    private static async Task WaitForSnapshot(GameSessionHandle handle)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (handle.Buffer.Latest is null) await Task.Delay(10, timeout.Token);
    }

    private sealed class Fixture : IAsyncDisposable, IGameSessionFactory
    {
        private static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/DeepSpaceSaga.Client"));
        internal string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "dss-defeat-ui-" + Guid.NewGuid().ToString("N"));
        private long _presentationNow;
        internal SimulationEngine Engine { get; }
        internal LocalGameSessionConnection Connection { get; }
        internal GameSessionHandle Handle { get; }
        internal GameSessionScreen Game { get; }
        internal SkiaWindow Window { get; }
        internal ScreenStack Screens => Get<ScreenStack>(Window, "_screens");
        private Fixture()
        {
            var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(Root, "Settings.json"), out _, out _);
            var scenario = ScenarioLoader.LoadFromFile(Path.Combine(Root, "Scenarios/PlayerShipOnly/scenario.json"));
            scenario = scenario with
            {
                GameState = scenario.GameState with
                {
                    MasterSeed = 42,
                    SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o with
                    {
                        PositionX = 0,
                        PositionY = o.ObjectId == "SPC-0001" ? 0 : -155,
                        DirectionDegrees = o.ObjectId == "SPC-0001" ? 0 : 180,
                        SpeedMps = 0,
                        MovementType = "Stationary",
                        Ai = o.ObjectId == "SPC-0001" ? null : new(25, 8, 35),
                        Modules = o.Modules!.Where(m => o.ObjectId != "SPC-0001" || m.ModuleTypeId.StartsWith("module.engine.")).Select(m => m with
                        { ArmorClass = "none", PowerState = m.ModuleTypeId.StartsWith("module.engine.") ? "Off" : "On" }).ToArray()
                    }).ToArray()
                }
            };
            Engine = new(registry, clock: new SimulationClock(SimulationSpeed.Speed0, () => 0));
            Engine.LoadScenario(scenario);
            Connection = new(Engine, DirectoryPath);
            Handle = new(Connection, new SnapshotBuffer(() => _presentationNow));
            Game = new(Handle.Buffer, new LinearMotionPredictor(), Handle);
            Window = new(Game, this, null, createNativeWindow: false);
            Set(Window, "_session", Handle); Set(Window, "_gameSessionScreen", Game);
        }
        internal static async Task<Fixture> Create() { var f = new Fixture(); await WaitForSnapshot(f.Handle); return f; }
        internal async Task Defeat()
        {
            Engine.SetSpeed(SimulationSpeed.Speed4);
            var snapshot = Engine.CaptureSnapshotForTests(1800000, simulationTimeMs: 45000);
            Assert.Equal(SessionOutcomeState.Defeated, snapshot.EffectiveOutcome.State);
            Assert.False(Handle.IsDefeated); // A terminal world cannot bypass the scene clock.
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (!Handle.IsDefeated)
            {
                _presentationNow += System.Diagnostics.Stopwatch.Frequency / 10;
                Handle.Buffer.PreparePresentation();
                Assert.Null(Handle.Failure);
                await Task.Delay(10, timeout.Token);
            }
        }
        internal Task Route(ScreenEvent action, string? payload = null) => (Task)Invoke(Window, "HandleScreenEvent", action, payload)!;
        public IGameSessionConnection CreateSessionFromSave(string slotId) => LocalGameSessionConnection.CreateFromSaveFile(
            Path.Combine(Root, "Settings.json"), Path.Combine(DirectoryPath, slotId + ".json"), DirectoryPath);
        public IGameSessionConnection CreateSession() => throw new NotSupportedException();
        public IGameSessionConnection CreateSessionFromScenario(string path) => throw new NotSupportedException();
        public ScenarioInfo[] ListScenarios() => [];
        public bool HasQuickSave() => File.Exists(Path.Combine(DirectoryPath, "quicksave.json"));
        public SaveSlotInfo[] ListSaveSlots() => [];
        public void DeleteSaveSlot(string slot) { }
        public async ValueTask DisposeAsync()
        {
            if (Get<GameSessionHandle?>(Window, "_session") is { } current) await current.DisposeAsync();
            Window.Dispose(); await Handle.DisposeAsync();
            if (Directory.Exists(DirectoryPath)) Directory.Delete(DirectoryPath, true);
        }
    }
}
