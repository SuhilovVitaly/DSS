using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using DeepSpaceSaga.Client.Tests.TacticalBattle;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.DebugConsole;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.MainMenu;
using DeepSpaceSaga.Client.UI.Screens.ShipDestroyed;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.DebugConsole;

/// <summary>
/// EP-0030 US-0002 TK-0001: <c>exit_battle</c> aborts the tutorial battle through the regular exit/settlement path
/// and opens the map of the same session (or the ship-destroyed screen, G-01), without touching ordinary exits.
/// </summary>
[Collection("InterfaceLog")]
public sealed class ExitBattleDebugCommandTests
{
    private const string ContinuationFile = "campaign-continuation.json";
    private const string MapOpened = "Бой завершён, открыта карта";
    private static string EngineSettingsPath => Path.Combine(AppContext.BaseDirectory, "Settings.json");
    private static string TutorialPath => Path.Combine(AppContext.BaseDirectory, "Scenarios", "Tutorial", "scenario.json");

    [Fact]
    public async Task ExitsUnfinishedTutorialToMapOfSameSession()
    {
        await using var fixture = new Fixture();
        var battle = await fixture.StartTutorialAsync();
        var session = fixture.Handle;
        var map = Field<GameSessionScreen>(fixture.Window, "_gameSessionScreen");
        Assert.NotNull(session);
        Assert.Null(battle.Flow.State!.Outcome);

        var lines = await Submit(fixture.Window, "exit_battle");

        Assert.Equal(["> exit_battle", MapOpened], lines);
        Assert.False(fixture.Window.DebugConsole.IsOpen);
        Assert.Same(map, fixture.Screens.Current);
        Assert.Equal(1, fixture.Screens.Count);
        Assert.Equal(0, fixture.Depth);
        Assert.Same(session, fixture.Handle); // the same world, no new session and no main menu
        Assert.False(fixture.Engine!.IsTacticalBattleActive);
        Assert.False(fixture.Proxy!.Disposed);

        // The map allows the regular save once the exit completed.
        await fixture.Route(ScreenEvent.QuickSave);
        Assert.True(File.Exists(Path.Combine(fixture.SaveDirectory, SaveSlotNaming.ToFileName(SaveSlots.Quicksave))));
    }

    [Fact]
    public async Task AbortedSettlementAppliedOnceAndAbandonRecorded()
    {
        await using var fixture = new Fixture();
        var battle = await fixture.StartTutorialAsync();
        RenderOnce(battle); // the tutorial observes the battle on render, as in a real frame
        string battleId = battle.Flow.State!.BattleId;

        Assert.Equal(MapOpened, (await Submit(fixture.Window, "exit_battle"))[^1]);

        var exit = Assert.Single(fixture.Proxy!.Exits);
        Assert.Equal(battleId, exit.BattleId);
        var carrier = ReadCarrier(Path.Combine(fixture.SaveDirectory, ContinuationFile));
        Assert.Equal(battleId, carrier.LastSettlement!.BattleId);
        Assert.Equal(1, carrier.Revision);
        string report = TacticalBattleMetrics.ReportPath(battleId, TacticalBattleMetrics.DirectoryPath);
        using (var json = JsonDocument.Parse(File.ReadAllText(report)))
            Assert.Equal("exited", json.RootElement.GetProperty("status").GetString());
        var events = JournalKinds(battle);
        Assert.Equal("abandoned", events[^1]);
        Assert.Single(events, kind => kind == "abandoned");

        // A repeated command finds no battle and settles nothing again.
        var again = await Submit(fixture.Window, "exit_battle");
        Assert.Equal(ExitBattleDebugCommand.NoTutorialBattleText, again[^1]);
        Assert.Single(fixture.Proxy.Exits);
        Assert.Equal(1, ReadCarrier(Path.Combine(fixture.SaveDirectory, ContinuationFile)).Revision);
        Assert.Single(JournalKinds(battle), kind => kind == "abandoned");
    }

    [Fact]
    public async Task WorksDuringAnimationAndWithConfirmExitOpen()
    {
        await using var fixture = new Fixture();
        var battle = await fixture.StartTutorialAsync();
        Assert.Equal(TacticalBattlePhase.Move, battle.Flow.State!.Phase);
        Assert.True(battle.CanSubmitTurn(ScreenEvent.MoveTacticalBattle));

        // The fixture clock is frozen: the accepted move's animation stays active.
        await fixture.Route(ScreenEvent.MoveTacticalBattle);
        Assert.Same(battle, fixture.Screens.Current);
        Assert.True(battle.TurnBusy);
        Assert.False(battle.Flow.Pending);
        typeof(TacticalBattleModalScreen).GetProperty(nameof(TacticalBattleModalScreen.ConfirmExit))!.SetValue(battle, true);
        Assert.True(battle.ConfirmExit);

        var lines = await Submit(fixture.Window, "exit_battle");

        Assert.Equal(MapOpened, lines[^1]);
        Assert.IsType<GameSessionScreen>(fixture.Screens.Current);
        Assert.Equal(1, fixture.Screens.Count);
        Assert.Equal(0, fixture.Depth);
        Assert.False(fixture.Engine!.IsTacticalBattleActive);
        Assert.Single(fixture.Proxy!.Exits);

        // Late events of the closed battle no longer reach the map.
        fixture.Advance(TacticalBattleMovementAnimation.FireTransitionDurationMs * 4);
        await fixture.Route(ScreenEvent.CloseTacticalBattle);
        await fixture.Route(ScreenEvent.MoveTacticalBattle);
        Assert.IsType<GameSessionScreen>(fixture.Screens.Current);
        Assert.Single(fixture.Proxy.Exits);
        Assert.False(fixture.Engine.IsTacticalBattleActive);
    }

    [Fact]
    public async Task RejectsArguments()
    {
        await using var fixture = new Fixture();
        var battle = await fixture.StartTutorialAsync();

        var lines = await Submit(fixture.Window, "exit_battle now");

        Assert.Equal("Синтаксис: exit_battle", lines[^1]);
        Assert.True(fixture.Window.DebugConsole.IsOpen);
        Assert.Same(battle, fixture.Screens.Current);
        Assert.Empty(fixture.Proxy!.Exits);
        Assert.True(fixture.Engine!.IsTacticalBattleActive);

        var direct = await new ExitBattleDebugCommand().ExecuteAsync(
            new DebugCommandContext(new DebugCommandRegistry(), fixture.Window), "x", CancellationToken.None);
        Assert.True(direct.IsError);
        Assert.False(direct.CloseConsole);
        Assert.Empty(fixture.Proxy.Exits);
    }

    [Fact]
    public async Task NoSessionAndNonTutorialBattleReturnNoTutorialBattle()
    {
        await using (var menu = new Fixture())
        {
            var lines = await Submit(menu.Window, "exit_battle");
            Assert.Equal(ExitBattleDebugCommand.NoTutorialBattleText, lines[^1]);
            Assert.True(menu.Window.DebugConsole.IsOpen);
            Assert.IsType<MainMenuScreen>(menu.Screens.Current);
            Assert.Null(menu.Handle);
            Assert.Null(menu.Proxy);
        }

        await using var regular = await BattleNavigationTests.Fixture.Create();
        Assert.Equal(ExitBattleDebugCommand.NoTutorialBattleText, (await Submit(regular.Window, "exit_battle"))[^1]);
        Assert.Same(regular.Game, regular.Screens.Current);

        await regular.Route(ScreenEvent.OpenTacticalBattle);
        var setup = Assert.IsType<TacticalBattleSetupScreen>(regular.Screens.Current);
        await setup.Flow.OptionsTask!;
        Assert.Equal(ExitBattleDebugCommand.NoTutorialBattleText, (await Submit(regular.Window, "exit_battle"))[^1]);
        Assert.Same(setup, regular.Screens.Current);

        await regular.Route(ScreenEvent.StartTacticalBattle);
        var battle = Assert.IsType<TacticalBattleScreen>(regular.Screens.Current);
        Assert.False(battle.Flow.IsTutorial);
        Assert.Equal(ExitBattleDebugCommand.NoTutorialBattleText, (await Submit(regular.Window, "exit_battle"))[^1]);
        Assert.Same(battle, regular.Screens.Current);
        Assert.True(regular.Engine.IsTacticalBattleActive);
        Assert.Equal(1, regular.Depth);
    }

    [Fact]
    public async Task PendingFlowOrHeldLockReturnsBusy()
    {
        await using var fixture = new Fixture();
        var battle = await fixture.StartTutorialAsync();
        var proxy = fixture.Proxy!;

        proxy.MoveGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var move = battle.Flow.MoveAsync(battle.BuildMoveRequest());
        Assert.True(battle.Flow.Pending);
        var pending = await Submit(fixture.Window, "exit_battle");
        Assert.Equal(DebugCommandResult.BusyText, pending[^1]);
        Assert.Same(battle, fixture.Screens.Current);
        Assert.Empty(proxy.Exits);
        proxy.MoveGate.SetResult();
        Assert.NotNull(await move.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.False(battle.Flow.Pending);

        var transitionLock = Field<SemaphoreSlim>(fixture.Window, "_transitionLock");
        await transitionLock.WaitAsync();
        try
        {
            var locked = await Submit(fixture.Window, "exit_battle");
            Assert.Equal(DebugCommandResult.BusyText, locked[^1]);
        }
        finally { transitionLock.Release(); }

        Assert.Same(battle, fixture.Screens.Current);
        Assert.Empty(proxy.Exits);
        Assert.True(fixture.Engine!.IsTacticalBattleActive);
        Assert.True(fixture.Window.DebugConsole.IsOpen);
    }

    [Fact]
    public async Task CheckpointFailureKeepsBattleThenRetryDoesNotResettle()
    {
        await using var fixture = new Fixture();
        var battle = await fixture.StartTutorialAsync();
        RenderOnce(battle);
        string battleId = battle.Flow.State!.BattleId;
        fixture.BlockSaveDirectory();

        var failed = await Submit(fixture.Window, "exit_battle");

        Assert.StartsWith("Не удалось завершить бой: ", failed[^1], StringComparison.Ordinal);
        Assert.EndsWith(". Повторите exit_battle", failed[^1], StringComparison.Ordinal);
        Assert.Contains(battle.Flow.Error, failed[^1], StringComparison.Ordinal);
        Assert.True(fixture.Window.DebugConsole.IsOpen); // no false success
        Assert.Same(battle, fixture.Screens.Current);
        Assert.Equal(1, fixture.Depth);
        Assert.True(battle.Flow.CheckpointPending);
        Assert.NotNull(fixture.Handle);
        Assert.DoesNotContain("abandoned", JournalKinds(battle));
        var proxy = fixture.Proxy!;
        Assert.Single(proxy.Exits);

        fixture.UnblockSaveDirectory();
        var retried = await Submit(fixture.Window, "exit_battle");

        Assert.Equal(MapOpened, retried[^1]);
        Assert.IsType<GameSessionScreen>(fixture.Screens.Current);
        Assert.Equal(2, proxy.Exits.Count);
        Assert.Equal(battleId, proxy.Exits[1].BattleId);
        Assert.NotEqual(proxy.Exits[0].CommandId, proxy.Exits[1].CommandId);
        var carrier = ReadCarrier(Path.Combine(fixture.SaveDirectory, ContinuationFile));
        Assert.Equal(1, carrier.Revision); // the retry wrote the same settlement; it never re-settled
        Assert.Equal(battleId, carrier.LastSettlement!.BattleId);
        Assert.Single(JournalKinds(battle), kind => kind == "abandoned");
    }

    [Fact]
    public async Task FinishedTutorialClosesNormally()
    {
        await using var fixture = new Fixture();
        var battle = await fixture.StartTutorialAsync();
        RenderOnce(battle);
        for (int i = 0; battle.Flow.State!.Phase != TacticalBattlePhase.Over && i < 100; i++)
        {
            fixture.FinishPresentation();
            await fixture.Route(battle.OnKeyDown(Key.Enter));
        }
        fixture.FinishPresentation();
        var finished = battle.Flow.State!;
        Assert.Equal(TacticalBattlePhase.Over, finished.Phase);
        Assert.NotNull(finished.Outcome);
        RenderOnce(battle);
        Assert.DoesNotContain("abandoned", JournalKinds(battle));

        var lines = await Submit(fixture.Window, "exit_battle");

        Assert.Equal(MapOpened, lines[^1]);
        Assert.IsType<GameSessionScreen>(fixture.Screens.Current);
        Assert.Equal(1, fixture.Screens.Count);
        Assert.False(fixture.Engine!.IsTacticalBattleActive);
        var exit = Assert.Single(fixture.Proxy!.Exits);
        Assert.Equal(finished.BattleId, exit.BattleId);
        var carrier = ReadCarrier(Path.Combine(fixture.SaveDirectory, ContinuationFile));
        Assert.Equal(finished.BattleId, carrier.LastSettlement!.BattleId);
        Assert.Equal(1, carrier.Revision);
        Assert.DoesNotContain("abandoned", JournalKinds(battle)); // a finished battle is not marked abandoned
    }

    [Fact]
    public async Task DestroyedCarrierRoutesToShipDestroyedScreen()
    {
        await using var fixture = new Fixture();
        var battle = await fixture.StartTutorialAsync();
        fixture.Proxy!.Transform = WithDestroyedPlayer;
        fixture.Publish(WithDestroyedPlayer);
        Assert.True(fixture.Handle!.IsPlayerShipDestroyed);
        Assert.Same(battle, fixture.Screens.Current);

        var lines = await Submit(fixture.Window, "exit_battle");

        Assert.Equal("Бой прерван. Носитель уничтожен — экран гибели", lines[^1]);
        Assert.False(fixture.Window.DebugConsole.IsOpen);
        Assert.IsType<ShipDestroyedScreen>(fixture.Screens.Current);
        Assert.Equal(1, fixture.Screens.Count);
        Assert.Equal(0, fixture.Depth);
        Assert.Single(fixture.Proxy.Exits);
        Assert.False(fixture.Engine!.IsTacticalBattleActive);

        // The guard is not bypassed: a later command or map event cannot resurrect the ship.
        Assert.Equal(ExitBattleDebugCommand.NoTutorialBattleText, (await Submit(fixture.Window, "exit_battle"))[^1]);
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        Assert.IsType<ShipDestroyedScreen>(fixture.Screens.Current);
        Assert.Single(fixture.Proxy.Exits);
    }

    [Fact]
    public async Task StaleSessionRejected()
    {
        await using var fixture = new Fixture();
        // Dispatched from the main menu (no session) and completed only after the tutorial session replaced it.
        var staleContext = new DebugCommandContext(new DebugCommandRegistry(), fixture.Window);
        Assert.Null(staleContext.Session);
        var battle = await fixture.StartTutorialAsync();
        Assert.False(staleContext.IsCurrent);

        var stale = await new ExitBattleDebugCommand().ExecuteAsync(staleContext, "", CancellationToken.None);

        Assert.True(stale.IsError);
        Assert.Equal(ExitBattleDebugCommand.NoTutorialBattleText, Assert.Single(stale.Lines));
        Assert.Same(battle, fixture.Screens.Current);
        Assert.Empty(fixture.Proxy!.Exits);
        Assert.True(fixture.Engine!.IsTacticalBattleActive);

        IDebugConsoleHost host = fixture.Window;
        var direct = await host.ExitTutorialBattleAsync(null, CancellationToken.None);
        Assert.Equal(ExitBattleDebugCommand.NoTutorialBattleText, Assert.Single(direct.Lines));
        Assert.Same(battle, fixture.Screens.Current);
        Assert.Empty(fixture.Proxy.Exits);
    }

    [Fact]
    public async Task EscAndExitButtonsUnchanged()
    {
        var registry = DebugCommandCatalog.CreateDefault();
        Assert.True(registry.TryGet("exit_battle", out var command));
        Assert.Equal(new DebugCommandDescriptor("exit_battle", "exit_battle", "Прервать обучающий бой и открыть карту", "обучающий бой"),
            command.Descriptor);

        await using var fixture = new Fixture();
        var battle = await fixture.StartTutorialAsync();

        // Esc still asks the "to main menu" confirmation, a second Esc cancels it, the confirm button leads to the main menu.
        Assert.Equal(ScreenEvent.None, battle.OnKeyDown(Key.Escape));
        Assert.True(battle.ConfirmExit);
        Assert.Equal(BattleFormat.Localize("ConfirmExitToMenu"), battle.ExitConfirmationText);
        Assert.Equal(ScreenEvent.None, battle.OnKeyDown(Key.Escape));
        Assert.False(battle.ConfirmExit);
        battle.OnKeyDown(Key.Escape);
        Assert.Equal(ScreenEvent.MainMenu, battle.OnKeyDown(Key.Enter));
        Assert.Empty(fixture.Proxy!.Exits);
        Assert.Same(battle, fixture.Screens.Current);

        await fixture.Route(ScreenEvent.MainMenu).WaitAsync(TimeSpan.FromSeconds(20));
        Assert.IsType<MainMenuScreen>(fixture.Screens.Current);
        Assert.Null(fixture.Handle);
        Assert.Single(fixture.Proxy.Exits);
    }

    private static async Task<IReadOnlyList<string>> Submit(SkiaWindow window, string line)
    {
        var console = window.DebugConsole;
        console.Open();
        int before = console.Output.Count;
        foreach (char c in line) console.OnTextInput(c);
        await console.SubmitAsync().WaitAsync(TimeSpan.FromSeconds(20));
        return console.Output.Skip(before).ToList();
    }

    private static void RenderOnce(TacticalBattleScreen battle)
    {
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        battle.Render(canvas, 1280, 720);
    }

    private static List<string> JournalKinds(TacticalBattleScreen battle)
    {
        string path = battle.Tutorial!.Metrics!.FilePath;
        if (!File.Exists(path)) return [];
        using var journal = JsonDocument.Parse(File.ReadAllText(path));
        return journal.RootElement.GetProperty("events").EnumerateArray()
            .Select(e => e.GetProperty("kind").GetString() ?? "").ToList();
    }

    private static AuthoritativeSnapshot WithDestroyedPlayer(AuthoritativeSnapshot snapshot) =>
        snapshot.PlayerShipObjectId is { } playerId
            ? snapshot with
            {
                Objects = snapshot.Objects.Select(o => o.ObjectId == playerId ? o with { IsDestroyed = true } : o).ToImmutableArray(),
                CarrierOperations = snapshot.CarrierOperations is { } carrier ? carrier with { IsDestroyed = true } : null,
            }
            : snapshot;

    private static CarrierStateData ReadCarrier(string path) =>
        Assert.Single(
            ScenarioLoader.LoadFromFile(path, allowNonZeroGameTime: true, isSave: true)
                .GameState.SpaceObjects,
            ship => ship.ObjectId == "SPC-0001").CarrierState!;

    private static T Field<T>(object owner, string name) =>
        (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;

    /// <summary>Main menu → New Game without saves → tutorial battle with a real engine (pattern of Ep0029Us0012Tk0003Tests).</summary>
    private sealed class Fixture : IGameSessionFactory, IAsyncDisposable
    {
        private readonly string _directory;
        private long _clockMilliseconds = 1_000;

        internal readonly SkiaWindow Window;
        internal SimulationEngine? Engine { get; private set; }
        internal Proxy? Proxy { get; private set; }
        internal string SaveDirectory { get; }
        internal ScreenStack Screens => Field<ScreenStack>(Window, "_screens");
        internal int Depth => Field<int>(Window, "_modalDepth");
        internal GameSessionHandle? Handle => Field<GameSessionHandle?>(Window, "_session");

        internal Fixture()
        {
            string root = Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") ?? Path.GetTempPath();
            _directory = Path.Combine(root, $"ep0030-exit-battle-{Guid.NewGuid():N}");
            SaveDirectory = Path.Combine(_directory, "Saves");
            Directory.CreateDirectory(SaveDirectory);
            string settingsPath = Path.Combine(_directory, "Settings.json");
            File.WriteAllText(settingsPath, """{"gameSettings":{"tacticalBattleSkinId":"classic"}}""");
            Window = new SkiaWindow(new MainMenuScreen(), this, null, createNativeWindow: false, settingsPath);
        }

        internal async Task<TacticalBattleScreen> StartTutorialAsync()
        {
            await Route(ScreenEvent.NewGame).WaitAsync(TimeSpan.FromSeconds(20));
            var battle = Assert.IsType<TacticalBattleScreen>(Screens.Current);
            Assert.True(battle.Flow.IsTutorial);
            return battle;
        }

        internal void Advance(long ms) => Interlocked.Add(ref _clockMilliseconds, ms);

        internal void FinishPresentation()
        {
            Advance(TacticalBattleMovementAnimation.FireTransitionDurationMs);
            if (Screens.Current is not TacticalBattleScreen { HasSalvoPresentation: true } battle) return;
            battle.OnKeyDown(Key.Enter);
            if (battle.HasSalvoPresentation) battle.OnKeyDown(Key.Enter);
        }

        /// <summary>Routes the event without advancing the frozen clock; the battle screen always reads the fixture clock.</summary>
        internal async Task Route(ScreenEvent evt)
        {
            await (Task)typeof(SkiaWindow).GetMethod("HandleScreenEvent", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(Window, [evt, null])!;
            if (Screens.Current is TacticalBattleScreen battle)
                battle.ClockMilliseconds = () => Interlocked.Read(ref _clockMilliseconds);
        }

        /// <summary>Publishes one newer accepted snapshot through the same transform the proxy applies to the stream.</summary>
        internal void Publish(Func<AuthoritativeSnapshot, AuthoritativeSnapshot> transform)
        {
            var latest = Handle!.Buffer.Latest!.Snapshot;
            Handle.Buffer.Update(transform(latest) with { SnapshotSequence = latest.SnapshotSequence + 1 });
        }

        internal void BlockSaveDirectory()
        {
            Directory.Delete(SaveDirectory, recursive: true);
            File.WriteAllText(SaveDirectory, "blocking file");
        }

        internal void UnblockSaveDirectory()
        {
            File.Delete(SaveDirectory);
            Directory.CreateDirectory(SaveDirectory);
        }

        public IGameSessionConnection CreateSessionFromScenario(string scenarioPath)
        {
            Engine = SimulationEngine.CreateFromScenarioFile(EngineSettingsPath, scenarioPath);
            var connection = DispatchProxy.Create<IGameSessionConnection, Proxy>();
            Proxy = (Proxy)(object)connection;
            Proxy.Inner = new LocalGameSessionConnection(Engine, SaveDirectory);
            return connection;
        }

        public IGameSessionConnection CreateSession() => throw new NotSupportedException();
        public IGameSessionConnection CreateSessionFromSave(string slotId) => throw new NotSupportedException();
        public ScenarioInfo[] ListScenarios() => [];
        public string? GetTutorialScenarioPath() => TutorialPath;
        public bool HasQuickSave() => false;
        public SaveSlotInfo[] ListSaveSlots() => [];
        public void DeleteSaveSlot(string id) { }

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
                catch (UnauthorizedAccessException) { }
            }
        }
    }

    /// <summary>Connection proxy: records exits, gates moves, and re-shapes every streamed snapshot with the current transform.</summary>
    public class Proxy : DispatchProxy
    {
        internal LocalGameSessionConnection Inner { get; set; } = null!;
        internal Func<AuthoritativeSnapshot, AuthoritativeSnapshot>? Transform { get; set; }
        internal TaskCompletionSource? MoveGate { get; set; }
        internal List<ExitTacticalBattleRequest> Exits { get; } = [];
        internal bool Disposed { get; private set; }

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            string name = method!.Name;
            if (name == nameof(IGameSessionConnection.ExitTacticalBattleAsync))
                lock (Exits) Exits.Add((ExitTacticalBattleRequest)args![0]!);
            if (name == nameof(IGameSessionConnection.ReadSnapshotsAsync))
            {
                var source = (IAsyncEnumerable<AuthoritativeSnapshot>)method.Invoke(Inner, args)!;
                return TransformSnapshotsAsync(source, (CancellationToken)args![0]!);
            }
            if (name == nameof(IGameSessionConnection.MoveTacticalBattleAsync) && MoveGate is { } gate)
                return new ValueTask<TacticalBattleResponse>(GatedMove(gate.Task, (MoveTacticalBattleRequest)args![0]!, (CancellationToken)args[1]!));
            if (name == nameof(IAsyncDisposable.DisposeAsync)) Disposed = true;
            return method.Invoke(Inner, args);
        }

        private async Task<TacticalBattleResponse> GatedMove(Task gate, MoveTacticalBattleRequest request, CancellationToken cancellationToken)
        {
            await gate.WaitAsync(cancellationToken);
            return await Inner.MoveTacticalBattleAsync(request, cancellationToken);
        }

        private async IAsyncEnumerable<AuthoritativeSnapshot> TransformSnapshotsAsync(
            IAsyncEnumerable<AuthoritativeSnapshot> source, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var snapshot in source.WithCancellation(cancellationToken))
                yield return Transform is { } transform ? transform(snapshot) : snapshot;
        }
    }
}
