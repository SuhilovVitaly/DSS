using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.MainMenu;
using DeepSpaceSaga.Client.UI.Screens.ScenarioSelect;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.LocalClient;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

/// <summary>EP-0027-US-0001-TK-0001: New game without saves opens the tutorial battle directly.</summary>
[Collection("InterfaceLog")]
public sealed class TutorialFlowTests
{
    private static string TutorialPath => Path.Combine(AppContext.BaseDirectory, "Scenarios", "Tutorial", "scenario.json");

    [Fact]
    public async Task NewGameWithoutSavesStartsTutorialBattleAndNeverShowsTheMap()
    {
        await using var fixture = new TutorialFixture();

        await fixture.Route(ScreenEvent.NewGame);

        var battle = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        Assert.True(battle.Flow.IsTutorial);
        Assert.False(battle.IsSetup);
        Assert.Equal(TutorialPath, Assert.Single(fixture.CreatedPaths));
        Assert.Equal(TacticalBattlePhase.Move, battle.Flow.State!.Phase);
        Assert.Equal(1, battle.Flow.State.Seed);
        Assert.True(fixture.Engine!.IsTacticalBattleActive);
        Assert.Equal(1, fixture.Proxy!.Starts);
        Assert.Equal(2, fixture.Screens.Count);
        Assert.IsType<GameSessionScreen>(fixture.Screens.UnderCurrent);
        Assert.Same(battle, Assert.Single(fixture.Screens.VisibleBottomToTop()));
        Assert.Equal(1, fixture.Depth);
        Assert.Equal(SimulationSpeed.Speed0, fixture.Engine.CurrentSpeed);
        // Every awaited engine call of the start sequence happened while the main menu was still on screen.
        Assert.NotEmpty(fixture.Proxy.ScreensAtCalls);
        Assert.All(fixture.Proxy.ScreensAtCalls, screen => Assert.Equal(typeof(MainMenuScreen), screen));
    }

    [Fact]
    public async Task NewGameWithSavesOpensScenarioSelectAsBefore()
    {
        await using var fixture = new TutorialFixture(saves: [new SaveSlotInfo("slot-1", "slot 1", DateTime.UtcNow)]);

        await fixture.Route(ScreenEvent.NewGame);

        Assert.IsType<ScenarioSelectScreen>(fixture.Screens.Current);
        Assert.Equal(1, fixture.Screens.Count);
        Assert.Empty(fixture.CreatedPaths);
        Assert.Null(fixture.Session);
    }

    [Theory]
    [InlineData(StartFailure.NoTutorial)]
    [InlineData(StartFailure.SessionThrows)]
    [InlineData(StartFailure.BattleRejected)]
    [InlineData(StartFailure.BattleThrows)]
    public async Task TutorialStartFailureReturnsToMainMenuAndDisposesSession(StartFailure failure)
    {
        await using var fixture = new TutorialFixture(failure: failure);

        await fixture.Route(ScreenEvent.NewGame);

        Assert.IsType<MainMenuScreen>(fixture.Screens.Current);
        Assert.Equal(1, fixture.Screens.Count);
        Assert.Null(fixture.Session);
        Assert.Equal(0, fixture.Depth);
        await fixture.LastSessionDisposal.WaitAsync(TimeSpan.FromSeconds(10));
        if (fixture.Proxy is { } proxy)
        {
            Assert.True(proxy.Disposed);
            Assert.All(proxy.ScreensAtCalls, screen => Assert.Equal(typeof(MainMenuScreen), screen));
        }
    }

    [Fact]
    public async Task TutorialMainMenuEventEndsTheBattleSessionCleanly()
    {
        await using var fixture = new TutorialFixture();
        await fixture.Route(ScreenEvent.NewGame);
        Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        Assert.True(fixture.Engine!.IsTacticalBattleActive);

        await fixture.Route(ScreenEvent.MainMenu).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.IsType<MainMenuScreen>(fixture.Screens.Current);
        Assert.Equal(1, fixture.Screens.Count);
        Assert.Null(fixture.Session);
        Assert.Equal(0, fixture.Depth);
        await fixture.LastSessionDisposal.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(fixture.LastSessionDisposal.IsCompletedSuccessfully);
        Assert.True(fixture.Proxy!.Disposed);
    }

    [Fact]
    public async Task RegularBattleStillBlocksMainMenu()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = (TacticalBattleModalScreen)fixture.Screens.Current;
        await setup.Flow.OptionsTask!;
        await fixture.Route(setup.OnKeyDown(Key.Enter));
        var battle = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        Assert.False(battle.Flow.IsTutorial);

        await fixture.Route(ScreenEvent.MainMenu);

        Assert.Same(battle, fixture.Screens.Current);
        Assert.True(fixture.Engine.IsTacticalBattleActive);
        Assert.Equal(BattleFormat.Localize("WorldActionBlocked"), battle.BlockedNotice);
        Assert.Equal(BattleFormat.Localize("ConfirmExit"), battle.ExitConfirmationText);
        battle.OnKeyDown(Key.Escape);
        Assert.Equal(ScreenEvent.CloseTacticalBattle, battle.OnKeyDown(Key.Enter));
    }

    [Fact]
    public async Task TutorialBattleBlocksSavingAndEngineRefusesToSave()
    {
        await using var fixture = new TutorialFixture();
        await fixture.Route(ScreenEvent.NewGame);
        var battle = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);

        await fixture.PressKey(Key.F5);
        Assert.Equal(ScreenEvent.None, battle.OnKeyDown(Key.S, KeyModifiers.Control, false));
        await fixture.Route(ScreenEvent.QuickSave);
        await fixture.Route(ScreenEvent.OpenSaveWindow);

        Assert.Same(battle, fixture.Screens.Current);
        Assert.Equal(BattleFormat.Localize("WorldActionBlocked"), battle.BlockedNotice);
        Assert.Empty(Directory.GetFiles(fixture.SaveDirectory));
        await Assert.ThrowsAnyAsync<Exception>(() => fixture.Proxy!.Inner.SaveAsync("tutorial-manual").AsTask());
        Assert.Empty(Directory.GetFiles(fixture.SaveDirectory));
    }

    [Fact]
    public async Task TutorialEscapeConfirmationLeadsToMainMenu()
    {
        await using var fixture = new TutorialFixture();
        await fixture.Route(ScreenEvent.NewGame);
        var battle = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);

        Assert.Equal(ScreenEvent.None, battle.OnKeyDown(Key.Escape));
        Assert.True(battle.ConfirmExit);
        Assert.Equal(BattleFormat.Localize("ConfirmExitToMenu"), battle.ExitConfirmationText);
        using (Localization.UseLocaleForTests("Russian"))
            Assert.Equal("Выйти в главное меню? Прогресс обучения будет потерян.", battle.ExitConfirmationText);
        Assert.Equal(ScreenEvent.None, battle.OnKeyDown(Key.Escape));
        Assert.False(battle.ConfirmExit);
        battle.OnKeyDown(Key.Escape);

        using (var bitmap = new SKBitmap(1280, 720))
        using (var canvas = new SKCanvas(bitmap))
            battle.Render(canvas, 1280, 720);
        // The confirm button of the tabletop exit dialog (left button of the centred 520x180 box at 1280x720).
        Assert.Equal(ScreenEvent.MainMenu, battle.OnMouseDown(380 + 24 + 110, 270 + 180 - 68 + 20, MouseButton.Left));

        await fixture.PressKey(Key.Enter);

        Assert.IsType<MainMenuScreen>(fixture.Screens.Current);
        Assert.Equal(1, fixture.Screens.Count);
        Assert.Null(fixture.Session);
        await fixture.LastSessionDisposal.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(fixture.Proxy!.Disposed);
    }

    [Fact]
    public async Task FinishedTutorialOffersOnlyTheMapThenTheMapIsNormal()
    {
        await using var fixture = new TutorialFixture();
        await fixture.Route(ScreenEvent.NewGame);
        var battle = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        for (int i = 0; battle.Flow.State!.Phase != TacticalBattlePhase.Over && i < 100; i++)
        {
            fixture.FinishPresentation();
            await fixture.Route(battle.OnKeyDown(Key.Enter));
        }
        fixture.FinishPresentation();
        Assert.Equal(TacticalBattlePhase.Over, battle.Flow.State!.Phase);

        using (Localization.UseLocaleForTests("Russian"))
        using (var bitmap = new SKBitmap(1280, 720))
        using (var canvas = new SKCanvas(bitmap))
        {
            battle.Render(canvas, 1280, 720);
            Assert.False(battle.Layout.Rects.ContainsKey("button:new"));
            Assert.DoesNotContain(battle.Layout.Texts, t => t.Text == "Новый бой");
            Assert.Contains(battle.Layout.Texts, t => t.Text == "На карту");
            var primary = battle.Layout.Rects["button:primary"];
            Assert.Equal(ScreenEvent.CloseTacticalBattle, battle.OnMouseDown(primary.MidX, primary.MidY, MouseButton.Left));
        }
        Assert.False(battle.CanStartNewBattle);
        Assert.True(battle.NewBattleBounds.IsEmpty);
        await fixture.Route(ScreenEvent.NewTacticalBattle);
        Assert.Same(battle, fixture.Screens.Current);

        await fixture.PressKey(Key.Escape);

        var map = Assert.IsType<GameSessionScreen>(fixture.Screens.Current);
        Assert.Equal(1, fixture.Screens.Count);
        Assert.Equal(0, fixture.Depth);
        Assert.False(fixture.Engine!.IsTacticalBattleActive);
        Assert.NotNull(fixture.Session);

        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = Assert.IsType<TacticalBattleSetupScreen>(fixture.Screens.Current);
        Assert.False(setup.Flow.IsTutorial);
        await setup.Flow.OptionsTask!;
        await fixture.Route(ScreenEvent.CloseTacticalBattle);
        Assert.Same(map, fixture.Screens.Current);

        await fixture.Route(ScreenEvent.QuickSave);
        Assert.True(File.Exists(Path.Combine(fixture.SaveDirectory, SaveSlotNaming.ToFileName(SaveSlots.Quicksave))));
    }

    [Theory]
    [InlineData("Russian", "Выйти в главное меню? Прогресс обучения будет потерян.")]
    [InlineData("English", "Exit to the main menu? Tutorial progress will be lost.")]
    public void ConfirmExitToMenuIsLocalized(string language, string expected)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Data", "Locale", language + ".json");
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal(expected, json.RootElement.GetProperty("TacticalBattle.ConfirmExitToMenu").GetString());
    }

    public enum StartFailure { None, NoTutorial, SessionThrows, BattleRejected, BattleThrows }

    public class TutorialProxy : DispatchProxy
    {
        internal LocalGameSessionConnection Inner { get; set; } = null!;
        internal Func<IScreen>? CurrentScreen { get; set; }
        internal StartFailure Failure { get; set; }
        internal ConcurrentQueue<Type> ScreensAtCalls { get; } = new();
        internal int Starts { get; private set; }
        internal bool Disposed { get; private set; }
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            string name = method!.Name;
            if (name is nameof(IGameSessionConnection.SetSimulationSpeedAsync) or nameof(IGameSessionConnection.StartTacticalBattleAsync)
                or nameof(IGameSessionConnection.GetTacticalBattleOptionsAsync) && CurrentScreen is { } current)
                ScreensAtCalls.Enqueue(current().GetType());
            if (name == nameof(IGameSessionConnection.StartTacticalBattleAsync))
            {
                Starts++;
                var request = (StartTacticalBattleRequest)args![0]!;
                if (Failure == StartFailure.BattleRejected)
                    return ValueTask.FromResult(new TacticalBattleResponse(request.CommandId, false, TacticalBattleRejectionCode.Unsupported, null, null, []));
                if (Failure == StartFailure.BattleThrows)
                    return ValueTask.FromException<TacticalBattleResponse>(new InvalidOperationException("tutorial battle start failed"));
            }
            if (name == nameof(IAsyncDisposable.DisposeAsync)) Disposed = true;
            return method.Invoke(Inner, args);
        }
    }

    private sealed class TutorialFixture : IGameSessionFactory, IAsyncDisposable
    {
        private readonly SaveSlotInfo[] _saves;
        private readonly StartFailure _failure;
        private readonly string _directory;
        private long _clockMilliseconds;
        internal readonly SkiaWindow Window;
        internal SimulationEngine? Engine { get; private set; }
        internal TutorialProxy? Proxy { get; private set; }
        internal List<string> CreatedPaths { get; } = [];
        internal string SaveDirectory { get; }
        internal ScreenStack Screens => Field<ScreenStack>(Window, "_screens");
        internal int Depth => Field<int>(Window, "_modalDepth");
        internal GameSessionHandle? Session => Field<GameSessionHandle?>(Window, "_session");
        internal Task LastSessionDisposal => Field<Task>(Window, "_lastSessionDisposal");

        internal TutorialFixture(SaveSlotInfo[]? saves = null, StartFailure failure = StartFailure.None)
        {
            _saves = saves ?? [];
            _failure = failure;
            string validationRoot = Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") ?? Path.GetTempPath();
            _directory = Path.Combine(validationRoot, $"tutorial-flow-{Guid.NewGuid():N}");
            SaveDirectory = Path.Combine(_directory, "Saves");
            Directory.CreateDirectory(SaveDirectory);
            string settingsPath = Path.Combine(_directory, "Settings.json");
            File.WriteAllText(settingsPath, """{"gameSettings":{"tacticalBattleSkinId":"classic"}}""");
            Window = new SkiaWindow(new MainMenuScreen(), this, null, createNativeWindow: false, settingsPath);
        }

        internal void FinishPresentation()
        {
            Interlocked.Add(ref _clockMilliseconds, TacticalBattleMovementAnimation.FireTransitionDurationMs);
            if (Screens.Current is not TacticalBattleScreen { HasSalvoPresentation: true } battle) return;
            battle.OnKeyDown(Key.Enter);
            if (battle.HasSalvoPresentation) battle.OnKeyDown(Key.Enter);
        }

        internal async Task Route(ScreenEvent evt)
        {
            if (evt is ScreenEvent.MoveTacticalBattle or ScreenEvent.FireTacticalBattle) FinishPresentation();
            else Interlocked.Add(ref _clockMilliseconds, TacticalBattleMovementAnimation.FireTransitionDurationMs);
            await (Task)typeof(SkiaWindow).GetMethod("HandleScreenEvent", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(Window, new object?[] { evt, null })!;
            AttachClock();
        }

        internal async Task PressKey(Key key)
        {
            typeof(SkiaWindow).GetMethod("HandleKeyboardEdge", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Window, new object[] { key });
            if (Field<Task?>(Window, "_pendingKeyboardTransition") is { } task) await task;
            AttachClock();
        }

        private void AttachClock()
        {
            if (Screens.Current is TacticalBattleScreen battle)
                battle.ClockMilliseconds = () => Interlocked.Read(ref _clockMilliseconds);
        }

        public string? GetTutorialScenarioPath() => _failure == StartFailure.NoTutorial ? null : TutorialPath;

        public IGameSessionConnection CreateSessionFromScenario(string scenarioPath)
        {
            CreatedPaths.Add(scenarioPath);
            if (_failure == StartFailure.SessionThrows) throw new InvalidDataException("tutorial scenario is broken");
            Engine = SimulationEngine.CreateFromScenarioFile(Path.Combine(AppContext.BaseDirectory, "Settings.json"), scenarioPath);
            var connection = DispatchProxy.Create<IGameSessionConnection, TutorialProxy>();
            Proxy = (TutorialProxy)(object)connection;
            Proxy.Inner = new LocalGameSessionConnection(Engine, SaveDirectory);
            Proxy.Failure = _failure;
            Proxy.CurrentScreen = () => Screens.Current;
            return connection;
        }

        public IGameSessionConnection CreateSession() => throw new NotSupportedException();
        public IGameSessionConnection CreateSessionFromSave(string slotId) => throw new NotSupportedException();
        public ScenarioInfo[] ListScenarios() => [];
        public SaveSlotInfo[] ListSaveSlots() => _saves;
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

    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
}
