using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using DeepSpaceSaga.Client.Tests.TacticalBattle;
using DeepSpaceSaga.Client.Tests.Tutorial;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.DebugConsole;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.MainMenu;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.DebugConsole;

/// <summary>
/// EP-0030 US-0003 TK-0005: <c>kill_drone &lt;имя&gt;</c> resolves one drone of either side by Id or map label (G-04),
/// destroys it authoritatively through the battle flow and shows only the confirmed state.
/// </summary>
[Collection("InterfaceLog")]
public sealed class KillDroneDebugCommandTests
{
    private const string MapOpened = "Бой завершён, открыта карта";
    private static string EngineSettingsPath => Path.Combine(AppContext.BaseDirectory, "Settings.json");
    private static string TutorialPath => Path.Combine(AppContext.BaseDirectory, "Scenarios", "Tutorial", "scenario.json");

    [Fact]
    public void ResolvesExactIdOrPilotLabelOrdinal()
    {
        var state = TutorialTestBattle.Battle(units:
        [
            TutorialTestBattle.Unit("Скиталец", kind: TacticalUnitKind.Carrier, state: TacticalUnitState.Alive),
            Piloted(TutorialTestBattle.Unit("vera"), "Вера"),
            Piloted(TutorialTestBattle.Unit("yun"), "Alpha 1"),
            TutorialTestBattle.Unit("tag-1", "B"),
            Piloted(TutorialTestBattle.Unit("tag-2", "B"), "tag-2"),
        ]);

        AssertFound(state, "vera", "vera");
        AssertFound(state, "Вера", "vera");
        AssertFound(state, "  Вера ", "vera");
        AssertFound(state, "tag-1", "tag-1"); // no build: the label is the Id
        AssertFound(state, "tag-2", "tag-2"); // Id and label of the same unit are one match
        foreach (string miss in new[] { "вера", "VERA", "Ver", "era", "tag", "alpha 1", "", "   " })
        {
            var resolution = KillDroneDebugCommand.Resolve(state, miss);
            Assert.Equal(KillTargetStatus.NotFound, resolution.Status);
            Assert.Null(resolution.Unit);
        }

        var registry = DebugCommandCatalog.CreateDefault();
        Assert.True(registry.TryGet("kill_drone", out var command));
        Assert.Equal(new DebugCommandDescriptor("kill_drone", "kill_drone <имя>", "Уничтожить дрона любой стороны по Id или имени", "текущий бой"),
            command.Descriptor);
    }

    [Fact]
    public async Task NameWithSpacesIsSingleTarget()
    {
        Assert.True(DebugConsoleController.TryParse("kill_drone   Alpha 1  ", out string name, out string arguments));
        Assert.Equal("kill_drone", name);
        Assert.Equal("Alpha 1", arguments);

        await using (var synthetic = await SyntheticBattle.Create(Piloted(TutorialTestBattle.Unit("yun"), "Alpha 1"),
            Piloted(TutorialTestBattle.Unit("alpha"), "Alpha")))
        {
            var result = await synthetic.Run("Alpha 1");
            Assert.False(result.IsError);
            Assert.Equal(["Уничтожен: Alpha 1 (yun)"], result.Lines);
            Assert.Equal("yun", Assert.Single(synthetic.Connection.Kills).UnitId);
            Assert.Equal(TacticalUnitState.Air, synthetic.Battle.Flow.State!.Units.Single(u => u.Id == "alpha").State);
        }

        // The real battle: a pilot label with a space selects exactly that drone.
        await using var fixture = await StartRegularAsync();
        var battle = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        var before = battle.Flow.State!;
        var target = before.Units.First(u => u.Kind == TacticalUnitKind.Drone && PilotCardModel.Name(u).Contains(' ', StringComparison.Ordinal));
        string label = PilotCardModel.Name(target);

        var lines = await Submit(fixture.Window, "kill_drone " + label);

        Assert.Equal("Уничтожен: " + label + " (" + target.Id + ")", lines[^1]);
        var engine = fixture.Engine.GetTacticalBattleState()!;
        Assert.Equal(TacticalUnitState.Destroyed, engine.Units.Single(u => u.Id == target.Id).State);
        Assert.Single(engine.Units.Where(u => u.State == TacticalUnitState.Destroyed));
        Assert.Equal(before.Revision + 1, engine.Revision);
    }

    [Fact]
    public async Task AmbiguousRejectedWithoutRequest()
    {
        var vera = TutorialTestBattle.Unit("vera");
        var ghost = Piloted(TutorialTestBattle.Unit("ghost", "B"), "vera"); // its label equals another unit's Id
        var state = TutorialTestBattle.Battle(units: [vera, ghost]);
        var resolution = KillDroneDebugCommand.Resolve(state, "vera");
        Assert.Equal(KillTargetStatus.Ambiguous, resolution.Status);
        Assert.Null(resolution.Unit);
        Assert.Equal(["vera", "ghost"], resolution.MatchIds);

        await using var synthetic = await SyntheticBattle.Create(vera, ghost);
        var revision = synthetic.Battle.Flow.State!.Revision;
        var result = await synthetic.Run("vera");

        Assert.True(result.IsError);
        Assert.Equal(["Неоднозначное имя: vera — совпадения: vera, ghost"], result.Lines);
        Assert.Empty(synthetic.Connection.Kills);
        Assert.Equal(revision, synthetic.Battle.Flow.State!.Revision);

        var unknown = await synthetic.Run("nobody");
        Assert.Equal(["Цель не найдена: nobody"], unknown.Lines);
        Assert.True(unknown.IsError);
        Assert.Empty(synthetic.Connection.Kills);
    }

    [Fact]
    public async Task CarrierRejected()
    {
        var carrier = TutorialTestBattle.Unit("Скиталец", kind: TacticalUnitKind.Carrier, state: TacticalUnitState.Alive);
        Assert.Equal(KillTargetStatus.Carrier, KillDroneDebugCommand.Resolve(TutorialTestBattle.Battle(units: [carrier]), "Скиталец").Status);

        await using var fixture = await StartRegularAsync();
        var battle = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        var before = fixture.Engine.GetTacticalBattleState()!;
        foreach (var unit in before.Units.Where(u => u.Kind == TacticalUnitKind.Carrier))
        {
            var lines = await Submit(fixture.Window, "kill_drone " + unit.Id);
            Assert.Equal(KillDroneDebugCommand.CarrierText, lines[^1]);
        }
        Assert.Contains(before.Units, u => u.Kind == TacticalUnitKind.Carrier);
        var after = fixture.Engine.GetTacticalBattleState()!;
        Assert.Equal(before.Revision, after.Revision);
        Assert.Equal(before.Units.Select(u => (u.Id, u.Hull, u.State)), after.Units.Select(u => (u.Id, u.Hull, u.State)));
        Assert.Same(battle, fixture.Screens.Current);
        Assert.True(fixture.Window.DebugConsole.IsOpen);
    }

    [Fact]
    public async Task DestroyedReportsAlreadyDestroyed()
    {
        var dead = TutorialTestBattle.Unit("tag-1", "B", state: TacticalUnitState.Destroyed);
        var resolved = KillDroneDebugCommand.Resolve(TutorialTestBattle.Battle(units: [dead]), "tag-1");
        Assert.Equal(KillTargetStatus.AlreadyDestroyed, resolved.Status);

        await using var fixture = await StartRegularAsync();
        var enemy = FirstLiveEnemy(fixture.Engine.GetTacticalBattleState()!);
        var first = await Submit(fixture.Window, "kill_drone " + enemy.Id);
        Assert.StartsWith("Уничтожен: ", first[^1], StringComparison.Ordinal);
        var killed = fixture.Engine.GetTacticalBattleState()!;

        var again = await Submit(fixture.Window, "kill_drone " + enemy.Id);

        Assert.Equal(KillDroneDebugCommand.AlreadyDestroyedText, again[^1]);
        var after = fixture.Engine.GetTacticalBattleState()!;
        Assert.Equal(killed.Revision, after.Revision);
        Assert.Equal(killed.Log.Length, after.Log.Length);
        Assert.Single(after.Log.Where(e => e.Kind == TacticalBattleLogKind.Destroyed && e.TargetId == enemy.Id));
    }

    [Fact]
    public async Task DisabledDroneKillable()
    {
        var disabled = TutorialTestBattle.Unit("yun", state: TacticalUnitState.Disabled);
        Assert.Equal(KillTargetStatus.Found, KillDroneDebugCommand.Resolve(TutorialTestBattle.Battle(units: [disabled]), "yun").Status);

        await using var synthetic = await SyntheticBattle.Create(TutorialTestBattle.Unit("vera"), disabled, TutorialTestBattle.Unit("tag-1", "B"));
        var result = await synthetic.Run("yun");

        Assert.False(result.IsError);
        Assert.Equal(["Уничтожен: yun (yun)"], result.Lines);
        Assert.Equal("yun", Assert.Single(synthetic.Connection.Kills).UnitId);
        Assert.Equal(TacticalUnitState.Destroyed, synthetic.Battle.Flow.State!.Units.Single(u => u.Id == "yun").State);
        Assert.False(synthetic.Battle.TurnBusy);
    }

    [Fact]
    public async Task NoBattleOrSetupRejected()
    {
        await using (var menu = new TutorialFixture())
        {
            Assert.Equal(KillDroneDebugCommand.SyntaxText, (await Submit(menu.Window, "kill_drone"))[^1]);
            Assert.Equal(KillDroneDebugCommand.NoBattleText, (await Submit(menu.Window, "kill_drone Таг-1"))[^1]);
            Assert.IsType<MainMenuScreen>(menu.Screens.Current);
            Assert.Null(menu.Handle);
        }

        await using var fixture = await BattleNavigationTests.Fixture.Create();
        Assert.Equal(KillDroneDebugCommand.NoBattleText, (await Submit(fixture.Window, "kill_drone Таг-1"))[^1]);
        Assert.Same(fixture.Game, fixture.Screens.Current);

        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = Assert.IsType<TacticalBattleSetupScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        Assert.Equal(KillDroneDebugCommand.NoBattleText, (await Submit(fixture.Window, "kill_drone Таг-1"))[^1]);
        Assert.Same(setup, fixture.Screens.Current);
        Assert.False(fixture.Engine.IsTacticalBattleActive);

        await fixture.Route(ScreenEvent.StartTacticalBattle);
        var battle = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        var before = fixture.Engine.GetTacticalBattleState()!;
        var empty = await new KillDroneDebugCommand().ExecuteAsync(
            new DebugCommandContext(new DebugCommandRegistry(), fixture.Window), "   ", CancellationToken.None);
        Assert.Equal([KillDroneDebugCommand.SyntaxText], empty.Lines);
        Assert.True(empty.IsError);
        Assert.Equal(before.Revision, fixture.Engine.GetTacticalBattleState()!.Revision);
        Assert.Same(battle, fixture.Screens.Current);
    }

    [Fact]
    public async Task BusyDuringPendingTurnOrSalvoPresentation()
    {
        await using (var pending = await StartRegularAsync())
        {
            var battle = Assert.IsType<TacticalBattleScreen>(pending.Screens.Current);
            var enemy = FirstLiveEnemy(battle.Flow.State!);
            pending.Proxy.MoveGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var move = battle.Flow.MoveAsync(battle.BuildMoveRequest());
            Assert.True(battle.Flow.Pending);

            Assert.Equal(DebugCommandResult.BusyText, (await Submit(pending.Window, "kill_drone " + enemy.Id))[^1]);

            pending.Proxy.MoveGate.SetResult();
            Assert.True((await move.WaitAsync(TimeSpan.FromSeconds(10)))!.Accepted);
            Assert.NotEqual(TacticalUnitState.Destroyed, pending.Engine.GetTacticalBattleState()!.Units.Single(u => u.Id == enemy.Id).State);
        }

        await using var fixture = await StartRegularAsync();
        var screen = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        for (int turn = 0; turn < 30 && screen.Flow.State?.Phase == TacticalBattlePhase.Move; turn++)
            await fixture.Route(ScreenEvent.MoveTacticalBattle);
        fixture.AdvanceAnimation(TacticalBattleMovementAnimation.FireTransitionDurationMs);
        Assert.Equal(TacticalBattlePhase.Fire, screen.Flow.State!.Phase);

        fixture.ManualAnimation = true;
        await fixture.Route(ScreenEvent.FireTacticalBattle);
        Assert.True(screen.HasSalvoPresentation);
        var target = FirstLiveEnemy(screen.Flow.State!);
        var fired = fixture.Engine.GetTacticalBattleState()!;
        Assert.Equal(DebugCommandResult.BusyText, (await Submit(fixture.Window, "kill_drone " + target.Id))[^1]);

        // The salvo results stay on screen after the playback finished: still busy, no state is overwritten.
        fixture.AdvanceAnimation(10 * 60 * 1000);
        Assert.False(screen.TurnBusy);
        Assert.True(screen.HasSalvoPresentation);
        Assert.Equal(DebugCommandResult.BusyText, (await Submit(fixture.Window, "kill_drone " + target.Id))[^1]);
        Assert.Equal(fired.Revision, fixture.Engine.GetTacticalBattleState()!.Revision);

        // A movement animation in progress (frozen clock) is busy too.
        fixture.FinishPresentation();
        Assert.False(screen.HasSalvoPresentation);
        Assert.Equal(TacticalBattlePhase.Move, screen.Flow.State!.Phase);
        await fixture.Route(ScreenEvent.MoveTacticalBattle);
        Assert.True(screen.TurnBusy);
        Assert.False(screen.Flow.Pending);
        var moved = fixture.Engine.GetTacticalBattleState()!;
        Assert.Equal(DebugCommandResult.BusyText, (await Submit(fixture.Window, "kill_drone " + FirstLiveEnemy(moved).Id))[^1]);
        Assert.Equal(moved.Revision, fixture.Engine.GetTacticalBattleState()!.Revision);
    }

    [Fact]
    public async Task KillsEnemyEndToEndAndPresentationShowsConfirmedState()
    {
        await using var fixture = await StartRegularAsync();
        var battle = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        var before = battle.Flow.State!;
        var enemy = FirstLiveEnemy(before);
        int moves = fixture.Proxy.Moves, fires = fixture.Proxy.Fires;

        var lines = await Submit(fixture.Window, "kill_drone " + enemy.Id);

        Assert.Equal(["> kill_drone " + enemy.Id, "Уничтожен: " + PilotCardModel.Name(enemy) + " (" + enemy.Id + ")"], lines);
        Assert.True(fixture.Window.DebugConsole.IsOpen);
        Assert.Same(battle, fixture.Screens.Current);
        var confirmed = battle.Flow.State!;
        Assert.Equal(before.Revision + 1, confirmed.Revision);
        Assert.Equal((before.Turn, before.Phase), (confirmed.Turn, confirmed.Phase));
        Assert.Equal(TacticalUnitState.Destroyed, confirmed.Units.Single(u => u.Id == enemy.Id).State);
        foreach (var unit in before.Units.Where(u => u.Id != enemy.Id))
        {
            var now = confirmed.Units.Single(u => u.Id == unit.Id);
            Assert.Equal((unit.Hull, unit.Armor, unit.State), (now.Hull, now.Armor, now.State));
        }
        Assert.Equal(confirmed.Revision, fixture.Engine.GetTacticalBattleState()!.Revision);
        Assert.Equal((moves, fires), (fixture.Proxy.Moves, fixture.Proxy.Fires));

        // The presentation shows the confirmed state at once, without replaying old steps.
        Assert.False(battle.TurnBusy);
        Assert.False(battle.HasSalvoPresentation);
        Assert.True(battle.CanSubmitTurn(ScreenEvent.MoveTacticalBattle));
        var frame = battle.CaptureFrame(1280, 720)!;
        Assert.Equal(confirmed.Revision, frame.Battle.Revision);
        Assert.Equal(TacticalUnitState.Destroyed, frame.Battle.Units.Single(u => u.Id == enemy.Id).State);
        using (var bitmap = new SKBitmap(1280, 720))
        using (var canvas = new SKCanvas(bitmap))
            battle.Render(canvas, 1280, 720);

        // The next turn keeps the killed drone dead and in place.
        var pose = confirmed.Units.Single(u => u.Id == enemy.Id).Pose;
        await fixture.Route(ScreenEvent.MoveTacticalBattle);
        var next = battle.Flow.State!;
        Assert.True(next.Revision > confirmed.Revision);
        var corpse = next.Units.Single(u => u.Id == enemy.Id);
        Assert.Equal(TacticalUnitState.Destroyed, corpse.State);
        Assert.Equal(pose, corpse.Pose);
    }

    [Fact]
    public async Task LastEnemyKillEndsBattleThenExitBattleSettlesOnce()
    {
        await using var fixture = new TutorialFixture();
        var battle = await fixture.StartTutorialAsync();
        var state = battle.Flow.State!;
        var own = state.Units.Where(u => u.Side == state.PlayerSide && u.Kind == TacticalUnitKind.Drone && u.State != TacticalUnitState.Destroyed).ToList();
        Assert.True(own.Count >= 2, "the tutorial wing needs a spare drone to lose");
        // Owned sortie drones keep their owned drone id as the tactical unit id; settlement reports that id.
        var lost = own[0];

        Assert.StartsWith("Уничтожен: ", (await Submit(fixture.Window, "kill_drone " + lost.Id))[^1], StringComparison.Ordinal);
        Assert.Null(battle.Flow.State!.Outcome);

        IReadOnlyList<string> last = [];
        foreach (var enemy in state.Units.Where(u => u.Side != state.PlayerSide && u.Kind == TacticalUnitKind.Drone && u.State != TacticalUnitState.Destroyed))
        {
            Assert.NotEqual(TacticalBattlePhase.Over, battle.Flow.State!.Phase);
            last = await Submit(fixture.Window, "kill_drone " + enemy.Id);
            Assert.StartsWith("Уничтожен: ", last[last.Count > 2 ? ^2 : ^1], StringComparison.Ordinal);
        }

        Assert.Equal(KillDroneDebugCommand.BattleEndedText, last[^1]);
        var over = battle.Flow.State!;
        Assert.Equal(TacticalBattlePhase.Over, over.Phase);
        Assert.NotNull(over.Outcome);
        Assert.Equal(TacticalBattlePhase.Over, fixture.Engine!.GetTacticalBattleState()!.Phase);
        Assert.True(fixture.Window.DebugConsole.IsOpen);
        Assert.Equal(KillDroneDebugCommand.BattleOverText, (await Submit(fixture.Window, "kill_drone " + own[1].Id))[^1]);

        Assert.Equal(MapOpened, (await Submit(fixture.Window, "exit_battle"))[^1]);

        Assert.IsType<GameSessionScreen>(fixture.Screens.Current);
        var exit = Assert.Single(fixture.Proxy!.Exits);
        Assert.Equal(over.BattleId, exit.BattleId);
        var carrier = ReadCarrier(Path.Combine(fixture.SaveDirectory, "campaign-continuation.json"));
        Assert.Equal(1, carrier.Revision);
        Assert.Equal(over.BattleId, carrier.LastSettlement!.BattleId);
        Assert.Equal(new[] { lost.Id }, carrier.LastSettlement.LostDroneIds);

        // Nothing is left to kill or settle again.
        Assert.Equal(KillDroneDebugCommand.NoBattleText, (await Submit(fixture.Window, "kill_drone " + lost.Id))[^1]);
        Assert.Equal(ExitBattleDebugCommand.NoTutorialBattleText, (await Submit(fixture.Window, "exit_battle"))[^1]);
        Assert.Single(fixture.Proxy.Exits);
        Assert.Equal(1, ReadCarrier(Path.Combine(fixture.SaveDirectory, "campaign-continuation.json")).Revision);
    }

    [Fact]
    public async Task StaleSessionDiscarded()
    {
        await using var fixture = new TutorialFixture();
        // Dispatched from the main menu (no session), completed after the tutorial session replaced it.
        var staleContext = new DebugCommandContext(new DebugCommandRegistry(), fixture.Window);
        var battle = await fixture.StartTutorialAsync();
        Assert.False(staleContext.IsCurrent);
        var state = fixture.Engine!.GetTacticalBattleState()!;
        var enemy = FirstLiveEnemy(state);

        var stale = await new KillDroneDebugCommand().ExecuteAsync(staleContext, enemy.Id, CancellationToken.None);

        Assert.Equal([KillDroneDebugCommand.NoBattleText], stale.Lines);
        Assert.True(stale.IsError);

        // A context bound to another session never reaches this session's battle either.
        var otherConnection = new FakeConnection(TutorialTestBattle.Battle());
        await using var other = new GameSessionHandle(otherConnection, deferDebugTuningWrites: true);
        var foreign = await new KillDroneDebugCommand().ExecuteAsync(
            new DebugCommandContext(new DebugCommandRegistry(), new ForeignSessionHost(fixture.Window, other)), enemy.Id, CancellationToken.None);
        Assert.Equal([KillDroneDebugCommand.NoBattleText], foreign.Lines);

        var after = fixture.Engine.GetTacticalBattleState()!;
        Assert.Equal(state.Revision, after.Revision);
        Assert.NotEqual(TacticalUnitState.Destroyed, after.Units.Single(u => u.Id == enemy.Id).State);
        Assert.Empty(otherConnection.Kills);
        Assert.Same(battle, fixture.Screens.Current);
    }

    [Fact]
    public async Task SpeedUnchanged()
    {
        await using var fixture = await StartRegularAsync();
        var battle = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        await Task.Delay(150);
        int speedCommands = fixture.Proxy.Speeds.Count;
        var engineSpeed = fixture.Engine.CurrentSpeed;
        var bufferSpeed = fixture.Handle.Buffer.CurrentSpeed;
        var before = battle.Flow.State!;

        Assert.StartsWith("Уничтожен: ", (await Submit(fixture.Window, "kill_drone " + FirstLiveEnemy(before).Id))[^1], StringComparison.Ordinal);
        fixture.Window.DebugConsole.Close();
        await Task.Delay(150);

        Assert.Equal(speedCommands, fixture.Proxy.Speeds.Count);
        Assert.Equal(engineSpeed, fixture.Engine.CurrentSpeed);
        Assert.Equal(bufferSpeed, fixture.Handle.Buffer.CurrentSpeed);
        Assert.Equal((before.Turn, before.Phase), (battle.Flow.State!.Turn, battle.Flow.State.Phase));
    }

    private static void AssertFound(TacticalBattleSnapshot state, string name, string id)
    {
        var resolution = KillDroneDebugCommand.Resolve(state, name);
        Assert.Equal(KillTargetStatus.Found, resolution.Status);
        Assert.Equal(id, resolution.Unit!.Id);
        Assert.Equal([id], resolution.MatchIds);
    }

    private static TacticalUnitSnapshot FirstLiveEnemy(TacticalBattleSnapshot state) =>
        state.Units.First(u => u.Side != state.PlayerSide && u.Kind == TacticalUnitKind.Drone && u.State != TacticalUnitState.Destroyed &&
            KillDroneDebugCommand.Resolve(state, u.Id).Status == KillTargetStatus.Found);

    private static TacticalUnitSnapshot Piloted(TacticalUnitSnapshot unit, string pilotName) => unit with
    {
        Build = new TacticalBuildSnapshot("frame", "pilot-" + unit.Id, pilotName, "gun", "Gun", 1, 2, 0, 0, 100, 90, null, 0,
            "armor", "Armor", null, null, 0, 0, 0, 1, 1, 0, 0, 0),
    };

    private static async Task<BattleNavigationTests.Fixture> StartRegularAsync()
    {
        var fixture = await BattleNavigationTests.Fixture.Create();
        try
        {
            await fixture.Route(ScreenEvent.OpenTacticalBattle);
            var setup = Assert.IsType<TacticalBattleSetupScreen>(fixture.Screens.Current);
            await setup.Flow.OptionsTask!;
            await fixture.Route(ScreenEvent.StartTacticalBattle);
            var battle = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
            Assert.False(battle.Flow.IsTutorial);
            Assert.Equal(TacticalBattlePhase.Move, battle.Flow.State!.Phase);
            return fixture;
        }
        catch { await fixture.DisposeAsync(); throw; }
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

    private static CarrierStateData ReadCarrier(string path) =>
        Assert.Single(
            ScenarioLoader.LoadFromFile(path, allowNonZeroGameTime: true, isSave: true)
                .GameState.SpaceObjects,
            ship => ship.ObjectId == "SPC-0001").CarrierState!;

    private static T Field<T>(object owner, string name) =>
        (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;

    /// <summary>Host whose session differs from the window's: the battle on screen belongs to another session.</summary>
    private sealed class ForeignSessionHost(IDebugConsoleHost inner, GameSessionHandle session) : IDebugConsoleHost
    {
        public GameSessionHandle? Session => session;
        public bool IsTransitionBusy => inner.IsTransitionBusy;
        public TacticalBattleScreen? ActiveBattle => inner.ActiveBattle;
        public Task<DebugCommandResult> RunExclusiveAsync(Func<CancellationToken, Task<DebugCommandResult>> operation, CancellationToken cancellationToken) =>
            inner.RunExclusiveAsync(operation, cancellationToken);
        public Task<DebugCommandResult> ExitTutorialBattleAsync(GameSessionHandle? expectedSession, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    /// <summary>A battle screen over a synthetic connection (no Engine): drives the command against hand-made unit sets.</summary>
    private sealed class SyntheticBattle : IDebugConsoleHost, IAsyncDisposable
    {
        private SyntheticBattle(FakeConnection connection, GameSessionHandle handle, TacticalBattleScreen battle) =>
            (Connection, Handle, Battle) = (connection, handle, battle);

        internal FakeConnection Connection { get; }
        internal GameSessionHandle Handle { get; }
        internal TacticalBattleScreen Battle { get; }

        internal static async Task<SyntheticBattle> Create(params TacticalUnitSnapshot[] units)
        {
            var connection = new FakeConnection(TutorialTestBattle.Battle(units: [.. units]));
            var handle = new GameSessionHandle(connection, deferDebugTuningWrites: true);
            var flow = new TacticalBattleFlow(handle);
            Assert.True((await flow.StartAsync(new(TacticalBattleFlow.NewIdentity("start"), 1)))!.Accepted);
            return new(connection, handle, new TacticalBattleScreen(flow, 1));
        }

        internal Task<DebugCommandResult> Run(string arguments) =>
            new KillDroneDebugCommand().ExecuteAsync(new DebugCommandContext(new DebugCommandRegistry(), this), arguments, CancellationToken.None);

        public GameSessionHandle? Session => Handle;
        public bool IsTransitionBusy => false;
        public TacticalBattleScreen? ActiveBattle => Battle;
        public Task<DebugCommandResult> RunExclusiveAsync(Func<CancellationToken, Task<DebugCommandResult>> operation, CancellationToken cancellationToken) =>
            operation(cancellationToken);
        public Task<DebugCommandResult> ExitTutorialBattleAsync(GameSessionHandle? expectedSession, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public ValueTask DisposeAsync() => Handle.DisposeAsync();
    }

    /// <summary>Synthetic connection: accepts start and applies a forced destroy to its own snapshot.</summary>
    private sealed class FakeConnection(TacticalBattleSnapshot initial) : IGameSessionConnection
    {
        private readonly Channel<AuthoritativeSnapshot> _snapshots = Channel.CreateUnbounded<AuthoritativeSnapshot>();
        private TacticalBattleSnapshot _state = initial;
        internal List<DebugDestroyTacticalUnitRequest> Kills { get; } = [];

        public ValueTask<TacticalBattleOptions> GetTacticalBattleOptionsAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new TacticalBattleOptions(new(true), [], [], [], [], []));
        public ValueTask<TacticalBattleSnapshot?> GetTacticalBattleStateAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<TacticalBattleSnapshot?>(_state);
        public ValueTask<TacticalBattleResponse> StartTacticalBattleAsync(StartTacticalBattleRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new TacticalBattleResponse(request.CommandId, true, TacticalBattleRejectionCode.None, _state.Phase, _state, []));
        public ValueTask<TacticalBattleResponse> MoveTacticalBattleAsync(MoveTacticalBattleRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public ValueTask<TacticalBattleResponse> FireTacticalBattleAsync(FireTacticalBattleRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public ValueTask<TacticalBattleResponse> ExitTacticalBattleAsync(ExitTacticalBattleRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public ValueTask<TacticalBattleResponse> DebugDestroyTacticalUnitAsync(DebugDestroyTacticalUnitRequest request, CancellationToken cancellationToken = default)
        {
            Kills.Add(request);
            _state = _state with
            {
                Revision = _state.Revision + 1,
                Units = _state.Units.Select(u => u.Id == request.UnitId ? u with { Hull = 0, Speed = 0, State = TacticalUnitState.Destroyed } : u)
                    .ToImmutableArray(),
            };
            return ValueTask.FromResult(new TacticalBattleResponse(request.CommandId, true, TacticalBattleRejectionCode.None, _state.Phase, _state,
                [new(1, _state.Turn, _state.Phase, TacticalBattleLogKind.Destroyed, TargetId: request.UnitId)]));
        }
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var snapshot in _snapshots.Reader.ReadAllAsync(cancellationToken)) yield return snapshot;
        }
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() { _snapshots.Writer.TryComplete(); return ValueTask.CompletedTask; }
    }

    /// <summary>Main menu → New Game without saves → tutorial battle with a real engine (copied from ExitBattleDebugCommandTests).</summary>
    private sealed class TutorialFixture : IGameSessionFactory, IAsyncDisposable
    {
        private readonly string _directory;
        private long _clockMilliseconds = 1_000;

        internal readonly SkiaWindow Window;
        internal SimulationEngine? Engine { get; private set; }
        internal ExitRecorder? Proxy { get; private set; }
        internal string SaveDirectory { get; }
        internal ScreenStack Screens => Field<ScreenStack>(Window, "_screens");
        internal GameSessionHandle? Handle => Field<GameSessionHandle?>(Window, "_session");

        internal TutorialFixture()
        {
            string root = Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") ?? Path.GetTempPath();
            _directory = Path.Combine(root, $"ep0030-kill-drone-{Guid.NewGuid():N}");
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

        /// <summary>Routes the event without advancing the frozen clock; the battle screen always reads the fixture clock.</summary>
        internal async Task Route(ScreenEvent evt)
        {
            await (Task)typeof(SkiaWindow).GetMethod("HandleScreenEvent", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(Window, [evt, null])!;
            if (Screens.Current is TacticalBattleScreen battle)
                battle.ClockMilliseconds = () => Interlocked.Read(ref _clockMilliseconds);
        }

        public IGameSessionConnection CreateSessionFromScenario(string scenarioPath)
        {
            Engine = SimulationEngine.CreateFromScenarioFile(EngineSettingsPath, scenarioPath);
            var connection = DispatchProxy.Create<IGameSessionConnection, ExitRecorder>();
            Proxy = (ExitRecorder)(object)connection;
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

    /// <summary>Connection proxy that records exit requests and forwards everything to the local connection.</summary>
    public class ExitRecorder : DispatchProxy
    {
        internal LocalGameSessionConnection Inner { get; set; } = null!;
        internal List<ExitTacticalBattleRequest> Exits { get; } = [];

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == nameof(IGameSessionConnection.ExitTacticalBattleAsync))
                lock (Exits) Exits.Add((ExitTacticalBattleRequest)args![0]!);
            return method.Invoke(Inner, args);
        }
    }
}
