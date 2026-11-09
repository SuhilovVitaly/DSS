using System.Collections.Concurrent;
using System.Reflection;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class BattleNavigationTests
{
    [Fact]
    public async Task RealBattleAndNewBattleReplaceOneOpaqueModalAndRestoreWorldCameraAndSpeed()
    {
        await using var fixture = await Fixture.Create();
        var camera = Field<CameraState>(fixture.Game, "_camera");
        camera.SetFocus(14326.5, -4987.25); camera.SetZoom(0.002345);
        var captured = (fixture.Game.CameraFocusX, fixture.Game.CameraFocusY, fixture.Game.CameraPixelsPerWorldUnit);
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = Assert.IsAssignableFrom<TacticalBattleModalScreen>(fixture.Screens.Current);
        Assert.True(setup.IsSetup); Assert.True(setup.IsOpaque);
        await setup.Flow.OptionsTask!;
        Assert.True(setup.Flow.Options!.Availability.Available);
        Assert.Equal(SimulationSpeed.Speed0, fixture.Engine.CurrentSpeed);
        Assert.Equal(1, fixture.Depth);
        Assert.Equal(new[] { SimulationSpeed.Speed0 }, fixture.Proxy.Speeds.ToArray());
        Assert.Same(setup, Assert.Single(fixture.Screens.VisibleBottomToTop()));
        Assert.Same(fixture.Game, fixture.Screens.UnderCurrent);
        Assert.Null(fixture.Engine.GetTacticalBattleState());
        await fixture.Route(setup.OnKeyDown(Key.Enter));
        var battle = Assert.IsAssignableFrom<TacticalBattleModalScreen>(fixture.Screens.Current);
        Assert.False(battle.IsSetup); Assert.Same(setup.Flow, battle.Flow);
        Assert.Equal(TacticalBattlePhase.Move, battle.Flow.State!.Phase);
        Assert.Equal(1, fixture.Depth);
        Assert.Same(battle, Assert.Single(fixture.Screens.VisibleBottomToTop()));
        Assert.True(fixture.Engine.CaptureSnapshot().IsTacticalBattleActive);
        await fixture.Route(ScreenEvent.StartTacticalBattle); // queued second press cannot start another battle
        Assert.Equal((ulong)1, battle.Flow.State.Revision);
        for (int i = 0; battle.Flow.State.Phase != TacticalBattlePhase.Over && i < 100; i++)
        {
            fixture.FinishPresentation();
            await fixture.Route(battle.OnKeyDown(Key.Enter));
        }
        fixture.FinishPresentation();
        Assert.Equal(TacticalBattlePhase.Over, battle.Flow.State.Phase);
        Assert.NotNull(battle.Flow.State.Outcome);
        using var bitmap = new SKBitmap(1280, 720); using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Magenta);
        battle.Render(canvas, 1280, 720);
        Assert.NotEqual(SKColors.Magenta, bitmap.GetPixel(0, 0));
        var newBattle = Assert.IsType<TacticalBattleScreen>(battle).NewBattleBounds;
        Assert.Equal(ScreenEvent.NewTacticalBattle, battle.OnMouseDown(newBattle.MidX, newBattle.MidY, MouseButton.Left));
        await fixture.Route(ScreenEvent.NewTacticalBattle);
        var next = Assert.IsAssignableFrom<TacticalBattleModalScreen>(fixture.Screens.Current);
        Assert.True(next.IsSetup); Assert.NotSame(setup, next); Assert.Same(battle.Flow, next.Flow);
        Assert.Null(fixture.Engine.GetTacticalBattleState());
        Assert.Equal(1, fixture.Depth);
        Assert.Equal(new[] { SimulationSpeed.Speed0 }, fixture.Proxy.Speeds.ToArray());
        await fixture.Route(next.OnKeyDown(Key.Escape));
        Assert.Same(fixture.Game, fixture.Screens.Current);
        Assert.Equal(0, fixture.Depth);
        Assert.Equal(SimulationSpeed.Speed2, fixture.Engine.CurrentSpeed);
        Assert.Equal(new[] { SimulationSpeed.Speed0, SimulationSpeed.Speed2 }, fixture.Proxy.Speeds.ToArray());
        Assert.Equal(captured, (fixture.Game.CameraFocusX, fixture.Game.CameraFocusY, fixture.Game.CameraPixelsPerWorldUnit));
    }

    [Fact]
    public async Task ExitClosesTheModalWhenTheEngineNoLongerHasThisBattle()
    {
        await using var fixture = await Fixture.Create();
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = (TacticalBattleModalScreen)fixture.Screens.Current;
        await setup.Flow.OptionsTask!;
        await fixture.Route(setup.OnKeyDown(Key.Enter));
        var battle = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        // The exit was processed but its response was lost: the engine has no battle, the client still shows one.
        Assert.True(fixture.Engine.ExitTacticalBattle(new("TB-lost-exit", battle.Flow.State!.BattleId)).Accepted);

        await fixture.Route(ScreenEvent.CloseTacticalBattle);

        Assert.Null(battle.Flow.State);
        Assert.Same(fixture.Game, fixture.Screens.Current);
        Assert.Equal(0, fixture.Depth);
        Assert.Equal(SimulationSpeed.Speed2, fixture.Engine.CurrentSpeed);
    }

    [Fact]
    public async Task ExitOfAStaleBattleExitsTheLiveEngineBattleInstead()
    {
        await using var fixture = await Fixture.Create();
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = (TacticalBattleModalScreen)fixture.Screens.Current;
        await setup.Flow.OptionsTask!;
        await fixture.Route(setup.OnKeyDown(Key.Enter));
        var battle = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        var request = battle.Flow.LastStart!;
        Assert.True(fixture.Engine.ExitTacticalBattle(new("TB-other-exit", battle.Flow.State!.BattleId)).Accepted);
        var live = fixture.Engine.StartTacticalBattle(request with { CommandId = "TB-other-start" });
        Assert.True(live.Accepted);

        await fixture.Route(ScreenEvent.CloseTacticalBattle);

        Assert.False(fixture.Engine.IsTacticalBattleActive);
        Assert.Same(fixture.Game, fixture.Screens.Current);
        Assert.Equal(0, fixture.Depth);
    }

    [Fact]
    public async Task RejectedStartAdoptsTheLiveBattleSoItCanBeExited()
    {
        await using var fixture = await Fixture.Create();
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = (TacticalBattleModalScreen)fixture.Screens.Current;
        await setup.Flow.OptionsTask!;
        // A start was accepted but its response never reached this modal.
        var orphan = fixture.Engine.StartTacticalBattle(setup.BuildStartRequest() with { CommandId = "TB-orphan-start" });
        Assert.True(orphan.Accepted);

        await fixture.Route(ScreenEvent.StartTacticalBattle);

        var battle = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        Assert.Equal(orphan.State!.BattleId, battle.Flow.State!.BattleId);
        await fixture.Route(ScreenEvent.CloseTacticalBattle);
        Assert.False(fixture.Engine.IsTacticalBattleActive);
        Assert.Same(fixture.Game, fixture.Screens.Current);
        Assert.Equal(SimulationSpeed.Speed2, fixture.Engine.CurrentSpeed);
    }

    [Fact]
    public async Task ClosingSetupExitsAnOrphanedEngineBattle()
    {
        await using var fixture = await Fixture.Create();
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = (TacticalBattleModalScreen)fixture.Screens.Current;
        await setup.Flow.OptionsTask!;
        Assert.True(fixture.Engine.StartTacticalBattle(setup.BuildStartRequest() with { CommandId = "TB-orphan-start" }).Accepted);

        await fixture.Route(ScreenEvent.CloseTacticalBattle);

        Assert.False(fixture.Engine.IsTacticalBattleActive);
        Assert.Same(fixture.Game, fixture.Screens.Current);
        Assert.Equal(SimulationSpeed.Speed2, fixture.Engine.CurrentSpeed);
    }

    [Fact]
    public async Task ActiveEscapeRequiresConfirmationAndActualExitPrecedesResume()
    {
        await using var fixture = await Fixture.Create();
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = (TacticalBattleModalScreen)fixture.Screens.Current;
        await setup.Flow.OptionsTask!;
        await fixture.Route(setup.OnKeyDown(Key.Enter));
        var battle = (TacticalBattleModalScreen)fixture.Screens.Current;
        Assert.Equal(ScreenEvent.None, battle.OnKeyDown(Key.Escape));
        Assert.True(battle.ConfirmExit);
        Assert.True(fixture.Engine.IsTacticalBattleActive);
        Assert.Equal(ScreenEvent.None, battle.OnKeyDown(Key.Escape));
        Assert.False(battle.ConfirmExit);
        battle.OnKeyDown(Key.Escape);
        await fixture.Route(battle.OnKeyDown(Key.KeypadEnter));
        Assert.False(fixture.Engine.IsTacticalBattleActive);
        Assert.Equal(SimulationSpeed.Speed2, fixture.Engine.CurrentSpeed);
        Assert.Equal(new[] { SimulationSpeed.Speed0, SimulationSpeed.Speed2 }, fixture.Proxy.Speeds.ToArray());
        Assert.Same(fixture.Game, fixture.Screens.Current);
    }

    [Fact]
    public async Task ClosingWhileOptionsArePendingCancelsQueryAndRestoresSpeed()
    {
        await using var fixture = await Fixture.Create();
        fixture.Proxy.DelayOptions = true;
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = (TacticalBattleModalScreen)fixture.Screens.Current;
        Assert.False(setup.Flow.OptionsTask!.IsCompleted);
        Assert.Null(setup.Flow.Options);
        Assert.Equal(ScreenEvent.None, setup.OnKeyDown(Key.Enter));
        await fixture.Route(setup.OnKeyDown(Key.Escape));
        await setup.Flow.OptionsTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(fixture.Proxy.OptionsCancelled);
        Assert.Null(fixture.Engine.GetTacticalBattleState());
        Assert.Equal(SimulationSpeed.Speed2, fixture.Engine.CurrentSpeed);
        Assert.Same(fixture.Game, fixture.Screens.Current);
    }

    [Fact]
    public void OpaqueStackSkipsLowerDrawsAndReplacePreservesActivationDepth()
    {
        var root = new CountingScreen(); var modal = new CountingScreen(true);
        var replacement = new CountingScreen(true); var overlay = new CountingScreen();
        var stack = new ScreenStack(); stack.SetRoot(root); stack.Push(modal);
        stack.Replace(replacement); stack.Push(overlay);
        Assert.Equal(new IScreen[] { root, replacement, overlay }, stack.AllBottomToTop());
        using var bitmap = new SKBitmap(20, 20); using var canvas = new SKCanvas(bitmap);
        foreach (var screen in stack.VisibleBottomToTop()) screen.Render(canvas, 20, 20);
        Assert.Equal(0, root.Draws); Assert.Equal(0, modal.Draws);
        Assert.Equal(1, replacement.Draws); Assert.Equal(1, overlay.Draws);
        Assert.Equal(1, modal.Deactivations); Assert.Equal(3, stack.Count);
        stack.Pop(); stack.Pop();
        Assert.Same(root, stack.Current); Assert.Equal(2, root.Activations);
        Assert.Same(root, Assert.Single(stack.VisibleBottomToTop()));
    }

    private sealed class CountingScreen(bool opaque = false) : IScreen
    {
        public bool IsOpaque => opaque;
        public int Draws { get; private set; }
        public int Activations { get; private set; }
        public int Deactivations { get; private set; }
        public void Render(SKCanvas canvas, int width, int height) => Draws++;
        public void OnActivated() => Activations++;
        public void OnDeactivated() => Deactivations++;
        public ScreenEvent OnMouseDown(float x, float y, MouseButton button) => ScreenEvent.None;
        public bool OnMouseMove(float x, float y) => false;
        public ScreenEvent OnMouseWheel(float x, float y, float delta) => ScreenEvent.None;
        public ScreenEvent OnKeyDown(Key key) => ScreenEvent.None;
    }

    public class ConnectionProxy : DispatchProxy
    {
        internal LocalGameSessionConnection Inner { get; set; } = null!;
        internal ConcurrentQueue<SimulationSpeed> Speeds { get; } = new();
        internal bool DelayOptions { get; set; }
        internal bool OptionsCancelled { get; private set; }
        internal TacticalBattleOptions? OptionsOverride { get; set; }
        internal int Starts { get; private set; }
        internal int Moves { get; private set; }
        internal int Fires { get; private set; }
        internal TaskCompletionSource MoveSeen { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource? MoveGate { get; set; }
        internal Func<TacticalBattleResponse, TacticalBattleResponse>? TransformMoveResponse { get; set; }
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == nameof(IGameSessionConnection.SetSimulationSpeedAsync)) Speeds.Enqueue((SimulationSpeed)args![0]!);
            if (DelayOptions && method.Name == nameof(IGameSessionConnection.GetTacticalBattleOptionsAsync))
                return new ValueTask<TacticalBattleOptions>(WaitForOptions((CancellationToken)args![0]!));
            if (OptionsOverride is { } options && method.Name == nameof(IGameSessionConnection.GetTacticalBattleOptionsAsync))
                return ValueTask.FromResult(options);
            if (method.Name == nameof(IGameSessionConnection.StartTacticalBattleAsync)) Starts++;
            if (method.Name == nameof(IGameSessionConnection.MoveTacticalBattleAsync))
            {
                Moves++;
                MoveSeen.TrySetResult();
                if (MoveGate is not null || TransformMoveResponse is not null)
                    return new ValueTask<TacticalBattleResponse>(WaitForMove(MoveGate?.Task ?? Task.CompletedTask,
                        (MoveTacticalBattleRequest)args![0]!, (CancellationToken)args[1]!));
            }
            if (method.Name == nameof(IGameSessionConnection.FireTacticalBattleAsync)) Fires++;
            return method.Invoke(Inner, args);
        }
        private async Task<TacticalBattleResponse> WaitForMove(Task gate, MoveTacticalBattleRequest request, CancellationToken cancellation)
        {
            await gate.WaitAsync(cancellation);
            var response = await Inner.MoveTacticalBattleAsync(request, cancellation);
            return TransformMoveResponse?.Invoke(response) ?? response;
        }
        private async Task<TacticalBattleOptions> WaitForOptions(CancellationToken cancellation)
        {
            try { await Task.Delay(Timeout.Infinite, cancellation); }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { OptionsCancelled = true; throw; }
            return await Inner.GetTacticalBattleOptionsAsync(cancellation);
        }
    }

    internal sealed class Fixture : IGameSessionFactory, IAsyncDisposable
    {
        internal readonly SimulationEngine Engine;
        internal readonly GameSessionHandle Handle;
        internal readonly GameSessionScreen Game;
        internal readonly SkiaWindow Window;
        internal readonly ConnectionProxy Proxy;
        private long _clockMilliseconds;
        internal bool ManualAnimation { get; set; }
        internal void AdvanceAnimation(long ms) => Interlocked.Add(ref _clockMilliseconds, ms);
        internal void FinishPresentation()
        {
            AdvanceAnimation(TacticalBattleMovementAnimation.FireTransitionDurationMs);
            if (Screens.Current is not TacticalBattleScreen { HasSalvoPresentation: true } battle) return;
            battle.OnKeyDown(Key.Enter);
            if (battle.HasSalvoPresentation) battle.OnKeyDown(Key.Enter);
        }
        internal ScreenStack Screens => Field<ScreenStack>(Window, "_screens");
        internal int Depth => Field<int>(Window, "_modalDepth");
        private Fixture(TacticalBattleInputSettings? inputs, float uiScale, string? saveDirectory, SimulationSpeed initialSpeed, string? settingsPath, string? engineSettingsPath)
        {
            string settings = engineSettingsPath ?? Path.Combine(AppContext.BaseDirectory, "Settings.json");
            Engine = SimulationEngine.CreateFromSettingsFile(settings);
            Engine.SetSpeed(initialSpeed);
            var connection = DispatchProxy.Create<IGameSessionConnection, ConnectionProxy>();
            Proxy = (ConnectionProxy)(object)connection;
            Proxy.Inner = new LocalGameSessionConnection(Engine, saveDirectory);
            Handle = new GameSessionHandle(connection, deferDebugTuningWrites: true);
            Game = new GameSessionScreen(Handle.Buffer, new LinearMotionPredictor(), Handle, uiScale: uiScale, tacticalBattleInputs: inputs);
            Window = new SkiaWindow(Game, this, null, createNativeWindow: false, settingsPath);
            Set(Window, "_session", Handle); Set(Window, "_gameSessionScreen", Game);
        }
        internal static async Task<Fixture> Create(TacticalBattleInputSettings? inputs = null, float uiScale = 1,
            string? saveDirectory = null, SimulationSpeed initialSpeed = SimulationSpeed.Speed2, string? settingsPath = null, string? engineSettingsPath = null)
        {
            var fixture = new Fixture(inputs, uiScale, saveDirectory, initialSpeed, settingsPath, engineSettingsPath);
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                while (fixture.Handle.Buffer.Latest is null || fixture.Handle.Buffer.CurrentSpeed != initialSpeed)
                {
                    Assert.Null(fixture.Handle.Failure);
                    await Task.Delay(10, timeout.Token);
                }
                return fixture;
            }
            catch { await fixture.DisposeAsync(); throw; }
        }
        internal async Task Route(ScreenEvent evt)
        {
            if (!ManualAnimation)
            {
                if (evt is ScreenEvent.MoveTacticalBattle or ScreenEvent.FireTacticalBattle) FinishPresentation();
                else AdvanceAnimation(TacticalBattleMovementAnimation.FireTransitionDurationMs);
            }
            await (Task)typeof(SkiaWindow).GetMethod("HandleScreenEvent", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(Window, new object?[] { evt, null })!;
            if (Screens.Current is TacticalBattleScreen battle)
                battle.ClockMilliseconds = () => Interlocked.Read(ref _clockMilliseconds);
        }
        internal async Task PressKey(Key key)
        {
            typeof(SkiaWindow).GetMethod("HandleKeyboardEdge", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Window, new object[] { key });
            if (Field<Task?>(Window, "_pendingKeyboardTransition") is { } task) await task;
        }
        public IGameSessionConnection CreateSession() => throw new NotSupportedException();
        public IGameSessionConnection CreateSessionFromSave(string slotId) => throw new NotSupportedException();
        public IGameSessionConnection CreateSessionFromScenario(string path) => throw new NotSupportedException();
        public ScenarioInfo[] ListScenarios() => [];
        public SaveSlotInfo[] ListSaveSlots() => [];
        public bool HasQuickSave() => false;
        public void DeleteSaveSlot(string id) { }
        public async ValueTask DisposeAsync() { Window.Dispose(); await Handle.DisposeAsync(); }
    }
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
    private static void Set(object owner, string name, object value) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(owner, value);
}
