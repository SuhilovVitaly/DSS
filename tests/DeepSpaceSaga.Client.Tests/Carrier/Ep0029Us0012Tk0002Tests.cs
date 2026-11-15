using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.MainMenu;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// EP-0029 US-0012 TK-0002 (AC-01): Tutorial and F8 launch the player's own persistent carrier
/// wing. The start requests carry the authoritative ShipId/revision, never a public loadout, and
/// a missing/empty carrier surface fails the Tutorial start atomically back to the menu instead of
/// fabricating four encounter drones.
/// </summary>
[Collection("InterfaceLog")]
public sealed class Ep0029Us0012Tk0002Tests
{
    private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "Settings.json");
    private static string TutorialPath => Path.Combine(AppContext.BaseDirectory, "Scenarios", "Tutorial", "scenario.json");
    private static string DefaultPath => Path.Combine(AppContext.BaseDirectory, "Scenarios", "Default", "scenario.json");

    [Fact]
    public async Task TutorialStartsOwnedShip()
    {
        await using var fixture = await SetupFixture.CreateAsync(tutorial: true);

        await fixture.Route(ScreenEvent.NewGame);

        var battle = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        Assert.True(battle.Flow.IsTutorial);
        Assert.Equal(TacticalBattlePhase.Move, battle.Flow.State!.Phase);
        var request = Assert.Single(fixture.Proxy!.Starts);
        Assert.Equal("SPC-0001", request.SourceShipId);
        Assert.Equal(0, request.ExpectedCarrierRevision);
        Assert.Equal("initial", request.ScenarioId);
        Assert.Equal(SkiaWindow.TutorialBattleSeed, request.Seed);
        Assert.True(request.Loadout.IsDefaultOrEmpty);
    }

    [Fact]
    public async Task F8UsesSurvivorsNotFourDefaults()
    {
        await using var fixture = await SetupFixture.CreateAsync(tutorial: false, configureEngine: engine =>
        {
            var wing = ReadExpandedWing(DefaultPath);
            Assert.Equal(4, wing.Count);
            engine.SetOwnedWingForTests("SPC-0001", wing.Take(2).ToArray());
        });

        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = Assert.IsType<TacticalBattleSetupScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        Render(setup);

        Assert.True(setup.OwnedWingMode);
        Assert.Equal(2, setup.WingComposition.Length);
        Assert.Equal(new[] { "DRN-PLAYER-01", "DRN-PLAYER-02" }, setup.WingComposition.Select(drone => drone.Id));
        // The encounter's diagnostic four drones are not the composition shown or launched.
        var options = setup.Flow.Options!;
        var scenario = options.Scenarios[0];
        var diagnostic = scenario.Drones
            .Where(drone => drone.Side == scenario.PlayerSide)
            .Select(drone => drone.Id);
        Assert.NotEqual(diagnostic.AsEnumerable(), setup.WingComposition.Select(drone => drone.Id));
        Assert.True(setup.CanStart);
        var request = setup.BuildStartRequest();
        Assert.Equal("SPC-0001", request.SourceShipId);
        Assert.Equal(0, request.ExpectedCarrierRevision);
        Assert.True(request.Loadout.IsDefaultOrEmpty);
    }

    [Fact]
    public async Task SetupCannotGrantWeapon()
    {
        await using var fixture = await SetupFixture.CreateAsync(tutorial: false);
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = Assert.IsType<TacticalBattleSetupScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        Render(setup);

        Assert.True(setup.OwnedWingMode);
        Assert.Equal(4, setup.WingComposition.Length);
        Assert.Equal(0, setup.EditableLoadoutControls);
        for (int i = 0; i < 12; i++) setup.OnKeyDown(Key.Tab);
        var request = setup.BuildStartRequest();
        var options = setup.Flow.Options!;
        Assert.True(request.Loadout.IsDefaultOrEmpty);
        Assert.NotEmpty(options.Weapons); // the catalog exists; the owned entry still may not use it
        Assert.Equal(options.Scenarios[0].Id, request.ScenarioId);
        Assert.Equal(1, request.Seed);

        // A legacy producer without the carrier surface gets an explanation, not an editable
        // four-drone fallback and not a free loadout.
        await using var legacy = await SetupFixture.CreateAsync(tutorial: false, stripCarrier: true);
        await legacy.Route(ScreenEvent.OpenTacticalBattle);
        var blocked = Assert.IsType<TacticalBattleSetupScreen>(legacy.Screens.Current);
        await blocked.Flow.OptionsTask!;
        Render(blocked);
        Assert.False(blocked.OwnedWingMode);
        Assert.Empty(blocked.WingComposition);
        Assert.Equal(0, blocked.EditableLoadoutControls);
        Assert.False(blocked.CanStart);
        Assert.Throws<InvalidOperationException>(() => blocked.BuildStartRequest());
    }

    [Theory]
    [InlineData(StartFailure.MissingCarrier)]
    [InlineData(StartFailure.EmptyWing)]
    [InlineData(StartFailure.BattleRejected)]
    public async Task StartFailureReturnsMenuWithoutPartialSession(StartFailure failure)
    {
        await using var fixture = await SetupFixture.CreateAsync(tutorial: true, failure: failure);

        await fixture.Route(ScreenEvent.NewGame);

        Assert.IsType<MainMenuScreen>(fixture.Screens.Current);
        Assert.Equal(1, fixture.Screens.Count);
        Assert.Null(fixture.Session);
        Assert.Equal(0, fixture.Depth);
        await fixture.LastSessionDisposal.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(fixture.LastSessionDisposal.IsCompletedSuccessfully);
        var proxy = fixture.Proxy!;
        Assert.True(proxy.Disposed);
        Assert.Empty(fixture.Screens.VisibleBottomToTop().OfType<TacticalBattleModalScreen>());
        if (failure == StartFailure.BattleRejected) Assert.Single(proxy.Starts);
        else Assert.Empty(proxy.Starts);
    }

    private static IReadOnlyList<OwnedDroneData> ReadExpandedWing(string scenarioPath) =>
        Assert.Single(
            ScenarioLoader.LoadFromFile(scenarioPath).GameState.SpaceObjects,
            ship => ship.ObjectId == "SPC-0001").CarrierState!.OwnedDrones!;

    private static void Render(TacticalBattleSetupScreen setup)
    {
        using var bitmap = new SkiaSharp.SKBitmap(1280, 720);
        using var canvas = new SkiaSharp.SKCanvas(bitmap);
        setup.Render(canvas, 1280, 720);
    }

    private static T Field<T>(object owner, string name) =>
        (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;

    private static void Set(object owner, string name, object value) =>
        owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(owner, value);

    public enum StartFailure { None, MissingCarrier, EmptyWing, BattleRejected }

    /// <summary>Owns one window plus the in-process connection; the proxy can hide or reshape the carrier surface.</summary>
    private sealed class SetupFixture : IGameSessionFactory, IAsyncDisposable
    {
        private readonly StartFailure _failure;
        private readonly bool _stripCarrier;
        private readonly string _directory;

        internal readonly SkiaWindow Window;
        internal SimulationEngine? Engine { get; private set; }
        internal Proxy? Proxy { get; private set; }
        internal string SaveDirectory { get; }
        internal ScreenStack Screens => Field<ScreenStack>(Window, "_screens");
        internal int Depth => Field<int>(Window, "_modalDepth");
        internal GameSessionHandle? Session => Field<GameSessionHandle?>(Window, "_session");
        internal Task LastSessionDisposal => Field<Task>(Window, "_lastSessionDisposal");

        private SetupFixture(bool tutorial, StartFailure failure, bool stripCarrier, Action<SimulationEngine>? configureEngine)
        {
            _failure = failure;
            _stripCarrier = stripCarrier;
            string root = Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") ?? Path.GetTempPath();
            _directory = Path.Combine(root, $"ep0029-us0012-tk0002-{Guid.NewGuid():N}");
            SaveDirectory = Path.Combine(_directory, "Saves");
            Directory.CreateDirectory(SaveDirectory);
            SettingsPathLocal = Path.Combine(_directory, "Settings.json");
            File.WriteAllText(SettingsPathLocal, """{"gameSettings":{"tacticalBattleSkinId":"classic"}}""");

            if (tutorial)
            {
                Window = new SkiaWindow(new MainMenuScreen(), this, null, createNativeWindow: false, SettingsPathLocal);
                if (failure is StartFailure.MissingCarrier or StartFailure.EmptyWing)
                    Window.TutorialCarrierSnapshotTimeout = TimeSpan.FromMilliseconds(250);
                return;
            }

            Engine = SimulationEngine.CreateFromSettingsFile(SettingsPath);
            configureEngine?.Invoke(Engine);
            Engine.SetSpeed(SimulationSpeed.Speed2);
            var connection = DispatchProxy.Create<IGameSessionConnection, Proxy>();
            Proxy = (Proxy)(object)connection;
            Proxy.Inner = new LocalGameSessionConnection(Engine, SaveDirectory);
            Proxy.StripCarrier = stripCarrier;
            var handle = new GameSessionHandle(connection, deferDebugTuningWrites: true);
            var game = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle, uiScale: 1);
            Window = new SkiaWindow(game, this, null, createNativeWindow: false, SettingsPathLocal);
            Set(Window, "_session", handle);
            Set(Window, "_gameSessionScreen", game);
        }

        internal static async Task<SetupFixture> CreateAsync(bool tutorial, StartFailure failure = StartFailure.None,
            bool stripCarrier = false, Action<SimulationEngine>? configureEngine = null)
        {
            var fixture = new SetupFixture(tutorial, failure, stripCarrier, configureEngine);
            if (tutorial) return fixture;
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                while (fixture.Session!.Buffer.Latest is null || fixture.Session.Buffer.CurrentSpeed != SimulationSpeed.Speed2)
                {
                    Assert.Null(fixture.Session.Failure);
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

        internal string SettingsPathLocal { get; }

        internal async Task Route(ScreenEvent evt) =>
            await (Task)typeof(SkiaWindow).GetMethod("HandleScreenEvent", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(Window, [evt, null])!;

        public IGameSessionConnection CreateSessionFromScenario(string scenarioPath)
        {
            Engine = SimulationEngine.CreateFromScenarioFile(SettingsPath, scenarioPath);
            var connection = DispatchProxy.Create<IGameSessionConnection, Proxy>();
            Proxy = (Proxy)(object)connection;
            Proxy.Inner = new LocalGameSessionConnection(Engine, SaveDirectory);
            Proxy.Failure = _failure;
            Proxy.StripCarrier = _stripCarrier;
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

    /// <summary>Connection proxy: records the owned start request and optionally hides/shapes the carrier surface.</summary>
    public class Proxy : DispatchProxy
    {
        internal LocalGameSessionConnection Inner { get; set; } = null!;
        internal StartFailure Failure { get; set; }
        internal bool StripCarrier { get; set; }
        internal List<StartTacticalBattleRequest> Starts { get; } = [];
        internal bool Disposed { get; private set; }

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            string name = method!.Name;
            if (name == nameof(IGameSessionConnection.StartTacticalBattleAsync))
            {
                var request = (StartTacticalBattleRequest)args![0]!;
                Starts.Add(request);
                if (Failure == StartFailure.BattleRejected)
                    return ValueTask.FromResult(new TacticalBattleResponse(request.CommandId, false,
                        TacticalBattleRejectionCode.Unsupported, null, null, []));
            }
            if (name == nameof(IGameSessionConnection.ReadSnapshotsAsync))
            {
                var source = (IAsyncEnumerable<AuthoritativeSnapshot>)method.Invoke(Inner, args)!;
                bool hideCarrier = StripCarrier || Failure == StartFailure.MissingCarrier;
                bool emptyWing = Failure == StartFailure.EmptyWing;
                return hideCarrier || emptyWing
                    ? ShapeSnapshotsAsync(source, hideCarrier, (CancellationToken)args![0]!)
                    : source;
            }
            if (name == nameof(IAsyncDisposable.DisposeAsync)) Disposed = true;
            return method.Invoke(Inner, args);
        }

        private static async IAsyncEnumerable<AuthoritativeSnapshot> ShapeSnapshotsAsync(
            IAsyncEnumerable<AuthoritativeSnapshot> source, bool hideCarrier, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var snapshot in source.WithCancellation(cancellationToken))
            {
                if (snapshot.CarrierOperations is not { } carrier) { yield return snapshot; continue; }
                yield return snapshot with
                {
                    CarrierOperations = hideCarrier
                        ? null
                        : carrier with { OwnedDrones = ImmutableArray<OwnedDroneSnapshot>.Empty },
                };
            }
        }
    }
}
