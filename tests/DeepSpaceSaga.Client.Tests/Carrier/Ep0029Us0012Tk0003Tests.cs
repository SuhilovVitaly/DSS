using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.MainMenu;
using DeepSpaceSaga.Client.UI.Screens.ShipDestroyed;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// EP-0029 US-0012 TK-0003 (AC-02..AC-04): a settled sortie routes by its real terminal state,
/// a failed continuation checkpoint keeps the battle modal and its Retry visible instead of
/// navigating, and a confirmed mid-tutorial abort writes the losses/checkpoint before the session
/// is disposed. No screen event or animation may resurrect a destroyed ship.
/// </summary>
[Collection("InterfaceLog")]
public sealed class Ep0029Us0012Tk0003Tests
{
    private const string ContinuationFile = "campaign-continuation.json";
    private static string EngineSettingsPath => Path.Combine(AppContext.BaseDirectory, "Settings.json");
    private static string TutorialPath => Path.Combine(AppContext.BaseDirectory, "Scenarios", "Tutorial", "scenario.json");

    [Fact]
    public async Task ShipDeathCannotNavigateToLiveMap()
    {
        await using var fixture = await LifecycleFixture.CreateBattleAsync();
        var battle = await OpenBattleAsync(fixture);
        fixture.Proxy!.Transform = WithDestroyedPlayer;
        fixture.Publish(WithDestroyedPlayer);
        Assert.True(fixture.Handle!.IsPlayerShipDestroyed);

        // A destroyed-player poll must not replace the still-active battle UI with the terminal.
        LifecycleFixture.Poll(fixture.Window);
        Assert.Same(battle, fixture.Screens.Current);
        Assert.Equal(2, fixture.Screens.Count);
        Assert.Equal(1, fixture.Depth);

        await fixture.Route(ScreenEvent.CloseTacticalBattle);

        Assert.IsType<ShipDestroyedScreen>(fixture.Screens.Current);
        Assert.Equal(1, fixture.Screens.Count);
        Assert.Equal(0, fixture.Depth);
        Assert.Null(fixture.Screens.Current as GameSessionScreen);
    }

    [Fact]
    public async Task NoDronesWithLiveShipReturnsMap()
    {
        await using var fixture = await LifecycleFixture.CreateBattleAsync();
        await OpenBattleAsync(fixture);
        fixture.Proxy!.Transform = WithLostWingLiveCarrier;
        fixture.Publish(WithLostWingLiveCarrier);
        Assert.False(fixture.Handle!.IsPlayerShipDestroyed);
        Assert.NotEmpty(fixture.Handle.CarrierOperations!.OwnedDrones);
        Assert.All(fixture.Handle.CarrierOperations!.OwnedDrones,
            drone => Assert.False(drone.ReadinessReasons.IsDefaultOrEmpty));

        await fixture.Route(ScreenEvent.CloseTacticalBattle);

        Assert.IsType<GameSessionScreen>(fixture.Screens.Current);
        Assert.Equal(1, fixture.Screens.Count);
        Assert.Equal(0, fixture.Depth);
    }

    [Fact]
    public async Task AbortToMenuWritesLossesFirst()
    {
        await using var fixture = LifecycleFixture.CreateTutorial();
        var battle = await StartTutorialAsync(fixture);
        string battleId = battle.Flow.State!.BattleId;

        await fixture.Route(ScreenEvent.MainMenu).WaitAsync(TimeSpan.FromSeconds(20));

        Assert.IsType<MainMenuScreen>(fixture.Screens.Current);
        Assert.Equal(1, fixture.Screens.Count);
        Assert.Null(fixture.Handle);
        await fixture.LastSessionDisposal.WaitAsync(TimeSpan.FromSeconds(10));
        var proxy = fixture.Proxy!;
        Assert.True(proxy.Disposed);
        Assert.Equal(new[] { "exit", "dispose" }, proxy.Events);
        var exit = Assert.Single(proxy.Exits);
        Assert.Equal(battleId, exit.BattleId);
        var carrier = ReadCarrier(Path.Combine(fixture.SaveDirectory, ContinuationFile));
        Assert.Equal(battleId, carrier.LastSettlement!.BattleId);
        Assert.Equal(1, carrier.Revision);
    }

    [Fact]
    public async Task FailedWriteKeepsRetryVisible()
    {
        await using var fixture = LifecycleFixture.CreateTutorial();
        var battle = await StartTutorialAsync(fixture);
        string battleId = battle.Flow.State!.BattleId;
        fixture.BlockSaveDirectory();

        await fixture.Route(ScreenEvent.MainMenu);

        Assert.Same(battle, fixture.Screens.Current);
        Assert.True(battle.Flow.CheckpointPending);
        Assert.False(string.IsNullOrWhiteSpace(battle.Flow.Error));
        Assert.NotNull(fixture.Handle);
        var proxy = fixture.Proxy!;
        Assert.DoesNotContain("dispose", proxy.Events);
        Assert.Single(proxy.Exits);

        fixture.UnblockSaveDirectory();
        await fixture.Route(ScreenEvent.MainMenu);

        Assert.IsType<MainMenuScreen>(fixture.Screens.Current);
        Assert.Equal(2, proxy.Exits.Count);
        Assert.Equal(battleId, proxy.Exits[1].BattleId);
        Assert.NotEqual(proxy.Exits[0].CommandId, proxy.Exits[1].CommandId);
        await fixture.LastSessionDisposal.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(proxy.Disposed);
        var carrier = ReadCarrier(Path.Combine(fixture.SaveDirectory, ContinuationFile));
        Assert.Equal(1, carrier.Revision); // the retry wrote the same settlement; it never re-settled
        Assert.Equal(battleId, carrier.LastSettlement!.BattleId);
    }

    private static async Task<TacticalBattleScreen> OpenBattleAsync(LifecycleFixture fixture)
    {
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = Assert.IsType<TacticalBattleSetupScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        await fixture.Route(ScreenEvent.StartTacticalBattle);
        return Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
    }

    private static async Task<TacticalBattleScreen> StartTutorialAsync(LifecycleFixture fixture)
    {
        await fixture.Route(ScreenEvent.NewGame);
        return Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
    }

    private static AuthoritativeSnapshot WithDestroyedPlayer(AuthoritativeSnapshot snapshot) =>
        snapshot.PlayerShipObjectId is { } playerId
            ? snapshot with
            {
                Objects = snapshot.Objects.Select(o => o.ObjectId == playerId ? o with { IsDestroyed = true } : o).ToImmutableArray(),
                CarrierOperations = snapshot.CarrierOperations is { } carrier ? carrier with { IsDestroyed = true } : null,
            }
            : snapshot;

    private static AuthoritativeSnapshot WithLostWingLiveCarrier(AuthoritativeSnapshot snapshot) =>
        snapshot.CarrierOperations is { } carrier
            ? snapshot with
            {
                CarrierOperations = carrier with
                {
                    OwnedDrones = carrier.OwnedDrones
                        .Select(drone => drone with { ReadinessReasons = ImmutableArray.Create("destroyed") })
                        .ToImmutableArray(),
                },
            }
            : snapshot;

    private static CarrierStateData ReadCarrier(string path) =>
        Assert.Single(
            ScenarioLoader.LoadFromFile(path, allowNonZeroGameTime: true, isSave: true)
                .GameState.SpaceObjects,
            ship => ship.ObjectId == "SPC-0001").CarrierState!;

    private static T Field<T>(object owner, string name) =>
        (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;

    private static void Set(object owner, string name, object value) =>
        owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(owner, value);

    /// <summary>One window with a real in-process engine; the proxy can reshape snapshots and records exit/dispose order.</summary>
    private sealed class LifecycleFixture : IGameSessionFactory, IAsyncDisposable
    {
        private readonly string _directory;

        internal readonly SkiaWindow Window;
        internal SimulationEngine? Engine { get; private set; }
        internal Proxy? Proxy { get; private set; }
        internal string SaveDirectory { get; }
        internal string SettingsPath { get; }
        internal ScreenStack Screens => Field<ScreenStack>(Window, "_screens");
        internal int Depth => Field<int>(Window, "_modalDepth");
        internal GameSessionHandle? Handle => Field<GameSessionHandle?>(Window, "_session");
        internal Task LastSessionDisposal => Field<Task>(Window, "_lastSessionDisposal");

        private LifecycleFixture(bool tutorial)
        {
            string root = Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") ?? Path.GetTempPath();
            _directory = Path.Combine(root, $"ep0029-us0012-tk0003-{Guid.NewGuid():N}");
            SaveDirectory = Path.Combine(_directory, "Saves");
            Directory.CreateDirectory(SaveDirectory);
            SettingsPath = Path.Combine(_directory, "Settings.json");
            File.WriteAllText(SettingsPath, """{"gameSettings":{"tacticalBattleSkinId":"classic"}}""");

            if (tutorial)
            {
                Window = new SkiaWindow(new MainMenuScreen(), this, null, createNativeWindow: false, SettingsPath);
                return;
            }

            Engine = SimulationEngine.CreateFromSettingsFile(EngineSettingsPath);
            Engine.SetSpeed(SimulationSpeed.Speed2);
            var connection = DispatchProxy.Create<IGameSessionConnection, Proxy>();
            Proxy = (Proxy)(object)connection;
            Proxy.Inner = new LocalGameSessionConnection(Engine, SaveDirectory);
            var handle = new GameSessionHandle(connection, deferDebugTuningWrites: true);
            var game = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle, uiScale: 1);
            Window = new SkiaWindow(game, this, null, createNativeWindow: false, SettingsPath);
            Set(Window, "_session", handle);
            Set(Window, "_gameSessionScreen", game);
        }

        internal static async Task<LifecycleFixture> CreateBattleAsync()
        {
            var fixture = new LifecycleFixture(tutorial: false);
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                while (fixture.Handle!.Buffer.Latest is null || fixture.Handle.Buffer.CurrentSpeed != SimulationSpeed.Speed2)
                {
                    Assert.Null(fixture.Handle.Failure);
                    await Task.Delay(10, timeout.Token);
                }
                return fixture;
            }
            catch
            {
                await fixture.DisposeAsync();
                throw;
            }
        }

        internal static LifecycleFixture CreateTutorial() => new(tutorial: true);

        internal async Task Route(ScreenEvent evt) =>
            await (Task)typeof(SkiaWindow).GetMethod("HandleScreenEvent", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(Window, [evt, null])!;

        internal static void Poll(SkiaWindow window) =>
            typeof(SkiaWindow).GetMethod("PollGameSessionAutoTransition", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(window, null);

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

        public ValueTask DisposeAsync()
        {
            try
            {
                Window.Dispose();
            }
            finally
            {
                try { Directory.Delete(_directory, recursive: true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>Connection proxy: re-shapes snapshots for routing tests and records exit-before-dispose evidence.</summary>
    public class Proxy : DispatchProxy
    {
        internal LocalGameSessionConnection Inner { get; set; } = null!;
        internal Func<AuthoritativeSnapshot, AuthoritativeSnapshot>? Transform { get; set; }
        internal List<ExitTacticalBattleRequest> Exits { get; } = [];
        internal List<string> Events { get; } = [];
        internal bool Disposed { get; private set; }

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            string name = method!.Name;
            if (name == nameof(IGameSessionConnection.ExitTacticalBattleAsync))
            {
                Exits.Add((ExitTacticalBattleRequest)args![0]!);
                Events.Add("exit");
            }
            if (name == nameof(IGameSessionConnection.ReadSnapshotsAsync) && Transform is { } transform)
            {
                var source = (IAsyncEnumerable<AuthoritativeSnapshot>)method.Invoke(Inner, args)!;
                return TransformSnapshotsAsync(source, transform, (CancellationToken)args![0]!);
            }
            if (name == nameof(IAsyncDisposable.DisposeAsync))
            {
                Events.Add("dispose");
                Disposed = true;
            }
            return method.Invoke(Inner, args);
        }

        private static async IAsyncEnumerable<AuthoritativeSnapshot> TransformSnapshotsAsync(
            IAsyncEnumerable<AuthoritativeSnapshot> source, Func<AuthoritativeSnapshot, AuthoritativeSnapshot> transform,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var snapshot in source.WithCancellation(cancellationToken))
                yield return transform(snapshot);
        }
    }
}
