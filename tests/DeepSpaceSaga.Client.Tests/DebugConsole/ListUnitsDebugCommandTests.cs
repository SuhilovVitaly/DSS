using System.Collections.Immutable;
using DeepSpaceSaga.Client.Tests.Tutorial;
using DeepSpaceSaga.Client.UI.DebugConsole;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.DebugConsole;

/// <summary>
/// EP-0030 US-0004 TK-0002: <c>list_units</c> reads one tactical snapshot through the session and lists every unit
/// (both sides, carriers, disabled/destroyed) without mutating anything or publishing a stale answer.
/// </summary>
[Collection("InterfaceLog")]
public sealed class ListUnitsDebugCommandTests
{
    private const string Header = "Бой: b-7 | ревизия: 42 | ход: 3 | фаза: Fire";

    // Mixed sides on purpose; carrier without Build; two drones share the pilot name "Alpha" with different ids.
    private static readonly ImmutableArray<TacticalUnitSnapshot> MixedUnits =
    [
        Piloted(TutorialTestBattle.Unit("tag-2", "B"), "Alpha"),
        TutorialTestBattle.Unit("Скиталец", kind: TacticalUnitKind.Carrier, state: TacticalUnitState.Alive, hull: 300, maxHull: 400),
        Piloted(TutorialTestBattle.Unit("yun", state: TacticalUnitState.Disabled, hull: 10), "Юн"),
        TutorialTestBattle.Unit("Логово", "B", TacticalUnitKind.Carrier, TacticalUnitState.Alive, hull: 500, maxHull: 500),
        Piloted(TutorialTestBattle.Unit("vera", hull: 80), "Alpha"),
        Piloted(TutorialTestBattle.Unit("tag-1", "B", state: TacticalUnitState.Destroyed, hull: 0), "Bravo"),
    ];

    private static readonly string[] ExpectedRows =
    [
        "vera | пилот: Alpha | сторона: A | тип: Drone | состояние: Air | корпус: 80/100",
        "yun | пилот: Юн | сторона: A | тип: Drone | состояние: Disabled | корпус: 10/100",
        "Скиталец | пилот: — | сторона: A | тип: Carrier | состояние: Alive | корпус: 300/400",
        "tag-1 | пилот: Bravo | сторона: B | тип: Drone | состояние: Destroyed | корпус: 0/100",
        "tag-2 | пилот: Alpha | сторона: B | тип: Drone | состояние: Air | корпус: 100/100",
        "Логово | пилот: — | сторона: B | тип: Carrier | состояние: Alive | корпус: 500/500",
    ];

    [Fact]
    public async Task ListsBothSidesAndAllKinds()
    {
        await using var session = new Session(Battle(MixedUnits));
        var result = await Execute(session.Handle);

        Assert.False(result.IsError);
        Assert.Equal([Header, "Участников: 6", .. ExpectedRows], result.Lines);
        Assert.Contains(result.Lines, l => l.Contains("сторона: A | тип: Carrier", StringComparison.Ordinal));
        Assert.Contains(result.Lines, l => l.Contains("сторона: B | тип: Carrier", StringComparison.Ordinal));
        Assert.Contains(result.Lines, l => l.Contains("сторона: A | тип: Drone", StringComparison.Ordinal));
        Assert.Contains(result.Lines, l => l.Contains("сторона: B | тип: Drone", StringComparison.Ordinal));
    }

    [Fact]
    public void IncludesDestroyedAndDisabled()
    {
        var lines = ListUnitsDebugCommand.Format(Battle(MixedUnits));
        Assert.Contains("tag-1 | пилот: Bravo | сторона: B | тип: Drone | состояние: Destroyed | корпус: 0/100", lines);
        Assert.Contains("yun | пилот: Юн | сторона: A | тип: Drone | состояние: Disabled | корпус: 10/100", lines);
        Assert.Equal(MixedUnits.Length + 2, lines.Count);
    }

    [Fact]
    public void ShowsIdAndPilotNameSeparately()
    {
        var lines = ListUnitsDebugCommand.Format(Battle(MixedUnits));
        // Same pilot name, different ids: two separate rows, each with its own id.
        var alpha = lines.Where(l => l.Contains("| пилот: Alpha |", StringComparison.Ordinal)).ToArray();
        Assert.Equal(2, alpha.Length);
        Assert.StartsWith("vera | ", alpha[0]);
        Assert.StartsWith("tag-2 | ", alpha[1]);
    }

    [Fact]
    public void HandlesNullBuildAndEmptyUnits()
    {
        var lines = ListUnitsDebugCommand.Format(Battle([TutorialTestBattle.Unit("lone", kind: TacticalUnitKind.Carrier),
            Piloted(TutorialTestBattle.Unit("blank"), "  ")]));
        Assert.Equal("blank | пилот: — | сторона: A | тип: Drone | состояние: Air | корпус: 100/100", lines[2]);
        Assert.Equal("lone | пилот: — | сторона: A | тип: Carrier | состояние: Air | корпус: 100/100", lines[3]);

        Assert.Equal([Header, ListUnitsDebugCommand.NoUnitsText], ListUnitsDebugCommand.Format(Battle([])));
        Assert.Equal("Участников нет", ListUnitsDebugCommand.NoUnitsText);
    }

    [Fact]
    public void SortsSideThenIdOrdinal()
    {
        var units = ImmutableArray.Create(
            TutorialTestBattle.Unit("b", "B"), TutorialTestBattle.Unit("a", "b"), TutorialTestBattle.Unit("Z", "B"),
            TutorialTestBattle.Unit("a", "A"), TutorialTestBattle.Unit("_", "A"), TutorialTestBattle.Unit("B", "A"));
        var lines = ListUnitsDebugCommand.Format(Battle(units)).Skip(2)
            .Select(l =>
            {
                string[] parts = l.Split(" | ");
                return parts[2] + " " + parts[0];
            }).ToArray();
        Assert.Equal(["сторона: A B", "сторона: A _", "сторона: A a", "сторона: B Z", "сторона: B b", "сторона: b a"], lines);
    }

    [Fact]
    public void IncludesSnapshotIdentity()
    {
        var lines = ListUnitsDebugCommand.Format(Battle(MixedUnits) with
        {
            BattleId = "battle-x",
            Revision = 9001,
            Turn = 12,
            Phase = TacticalBattlePhase.Over,
        });
        Assert.Equal("Бой: battle-x | ревизия: 9001 | ход: 12 | фаза: Over", lines[0]);
        Assert.Equal(Header, ListUnitsDebugCommand.Format(Battle(MixedUnits))[0]);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("--all")]
    [InlineData("tag 1")]
    public async Task RejectsArgumentsBeforeQuery(string arguments)
    {
        await using var session = new Session(Battle(MixedUnits));
        var result = await Execute(session.Handle, arguments);
        Assert.True(result.IsError);
        Assert.Equal(["Синтаксис: list_units"], result.Lines);
        Assert.Equal(0, session.Connection.StateReads);

        // Arguments are checked before the session: still the syntax error in the main menu.
        Assert.Equal(["Синтаксис: list_units"], (await Execute(null, arguments)).Lines);
    }

    [Fact]
    public async Task HandlesNoSessionAndNullSnapshot()
    {
        var noSession = await Execute(null);
        Assert.True(noSession.IsError);
        Assert.Equal(["Нет активной сессии"], noSession.Lines);

        await using var session = new Session(null);
        var noBattle = await Execute(session.Handle);
        Assert.True(noBattle.IsError);
        Assert.Equal(["Нет активного боя или данные недоступны"], noBattle.Lines);

        // A failed read (mapped to null by the handle) never republishes the previous list as new data.
        session.Connection.State = Battle(MixedUnits);
        Assert.Equal(Header, (await Execute(session.Handle)).Lines[0]);
        session.Connection.Failure = new InvalidOperationException("transport down");
        var failed = await Execute(session.Handle);
        Assert.Equal([ListUnitsDebugCommand.NoBattleText], failed.Lines);
    }

    [Fact]
    public async Task DiscardsOldSessionOrBattleResponse()
    {
        await using var first = new Session(Battle(MixedUnits));
        await using var second = new Session(Battle([]));
        var host = new Host { Session = first.Handle };
        var registry = Registry();

        // Direct: the session changed while the read was in flight -> stale error, not the old list.
        var gate = first.Connection.Hold();
        var context = new DebugCommandContext(registry, host);
        Assert.True(registry.TryGet("list_units", out var command));
        var pending = command.ExecuteAsync(context, "", CancellationToken.None);
        host.Session = second.Handle;
        gate.SetResult(Battle(MixedUnits));
        var direct = await pending;
        Assert.True(direct.IsError);
        Assert.Equal([DebugConsoleController.StaleResultText], direct.Lines);

        // Through the console: a late answer after a switch to another session or to the main menu is not shown.
        foreach (var replacement in new[] { second.Handle, null })
        {
            host.Session = first.Handle;
            using var console = new DebugConsoleController(registry, host);
            gate = first.Connection.Hold();
            Type(console, "list_units");
            var submit = console.SubmitAsync();
            host.Session = replacement;
            gate.SetResult(Battle(MixedUnits));
            await submit;
            Assert.Equal(["> list_units", DebugConsoleController.StaleResultText], console.Output);
        }

        // The new session's own battle is listed with its own identity.
        host.Session = second.Handle;
        var fresh = await command.ExecuteAsync(new DebugCommandContext(registry, host), "", CancellationToken.None);
        Assert.Equal([Header, ListUnitsDebugCommand.NoUnitsText], fresh.Lines);
    }

    [Fact]
    public async Task CancellationDoesNotPublishStaleList()
    {
        await using var session = new Session(Battle(MixedUnits));
        var host = new Host { Session = session.Handle };
        var registry = Registry();

        // Direct: cancellation while waiting surfaces as cancellation, never as a list.
        var gate = session.Connection.Hold();
        using var cts = new CancellationTokenSource();
        Assert.True(registry.TryGet("list_units", out var command));
        var pending = command.ExecuteAsync(new DebugCommandContext(registry, host), "", cts.Token);
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);

        // Through the console: disposing the console (window shutdown) drops the in-flight answer silently.
        var console = new DebugConsoleController(registry, host);
        gate = session.Connection.Hold();
        Type(console, "list_units");
        var submit = console.SubmitAsync();
        console.Dispose();
        gate.TrySetResult(Battle(MixedUnits));
        await submit;
        Assert.Equal(["> list_units"], console.Output);
        Assert.False(console.IsExecuting);
    }

    [Fact]
    public async Task ReadsOnceWithoutMutation()
    {
        var snapshot = Battle(MixedUnits);
        await using var session = new Session(snapshot);
        var host = new Host { Session = session.Handle };
        var speed = session.Handle.Buffer.CurrentSpeed;
        using var console = new DebugConsoleController(Registry(), host);
        Type(console, "list_units");
        await console.SubmitAsync();

        Assert.Equal(["> list_units", Header, "Участников: 6", .. ExpectedRows], console.Output);
        Assert.Equal(1, session.Connection.StateReads);
        Assert.Empty(session.Connection.Mutations);
        Assert.Equal(0, host.OtherCalls); // no exclusive run, battle screen access or exit
        Assert.Equal(speed, session.Handle.Buffer.CurrentSpeed);
        Assert.Equal(MixedUnits, snapshot.Units);
        Assert.Equal(42UL, snapshot.Revision);
    }

    [Fact]
    public async Task AppearsInHelp()
    {
        var catalog = DebugCommandCatalog.CreateDefault();
        Assert.True(catalog.TryGet("list_units", out var registered));
        Assert.IsType<ListUnitsDebugCommand>(registered);
        Assert.True(catalog.TryGet("help", out var help));
        var result = await help.ExecuteAsync(new DebugCommandContext(catalog, new Host()), "", CancellationToken.None);
        Assert.Contains("list_units | синтаксис: list_units | Показать всех участников текущего боя | область: текущий бой", result.Lines);
        Assert.Single(result.Lines, l => l.StartsWith("list_units |", StringComparison.Ordinal));
    }

    private static TacticalBattleSnapshot Battle(ImmutableArray<TacticalUnitSnapshot> units) =>
        TutorialTestBattle.Battle(TacticalBattlePhase.Fire, 3, units) with { BattleId = "b-7", Revision = 42 };

    private static TacticalUnitSnapshot Piloted(TacticalUnitSnapshot unit, string pilotName) => unit with
    {
        Build = new TacticalBuildSnapshot("frame", "pilot-" + unit.Id, pilotName, "gun", "Gun", 1, 2, 0, 0, 100, 90, null, 0,
            "armor", "Armor", null, null, 0, 0, 0, 1, 1, 0, 0, 0),
    };

    private static DebugCommandRegistry Registry()
    {
        var registry = new DebugCommandRegistry();
        registry.Register(new HelpDebugCommand());
        registry.Register(new ListUnitsDebugCommand());
        return registry;
    }

    private static Task<DebugCommandResult> Execute(GameSessionHandle? session, string arguments = "")
    {
        var registry = Registry();
        Assert.True(registry.TryGet("list_units", out var command));
        return command.ExecuteAsync(new DebugCommandContext(registry, new Host { Session = session }), arguments, CancellationToken.None);
    }

    private static void Type(DebugConsoleController console, string text)
    {
        console.Input.Clear();
        foreach (char c in text) console.OnTextInput(c);
    }

    private sealed class Session(TacticalBattleSnapshot? state) : IAsyncDisposable
    {
        public StateConnection Connection { get; } = new() { State = state };
        public GameSessionHandle Handle => _handle ??= new GameSessionHandle(Connection, deferDebugTuningWrites: true);
        private GameSessionHandle? _handle;

        public ValueTask DisposeAsync() => _handle?.DisposeAsync() ?? ValueTask.CompletedTask;
    }

    /// <summary>Host without battle screen; any member other than Session counts as a forbidden call.</summary>
    private sealed class Host : IDebugConsoleHost
    {
        public int OtherCalls;
        public GameSessionHandle? Session { get; set; }

        public bool IsTransitionBusy
        {
            get
            {
                OtherCalls++;
                return false;
            }
        }

        public TacticalBattleScreen? ActiveBattle
        {
            get
            {
                OtherCalls++;
                return null;
            }
        }

        public Task<DebugCommandResult> RunExclusiveAsync(Func<CancellationToken, Task<DebugCommandResult>> operation, CancellationToken cancellationToken)
        {
            OtherCalls++;
            return operation(cancellationToken);
        }

        public Task<DebugCommandResult> ExitTutorialBattleAsync(GameSessionHandle? expectedSession, CancellationToken cancellationToken)
        {
            OtherCalls++;
            throw new NotSupportedException();
        }
    }

    /// <summary>Tactical state source with a controllable deferred answer; every non-read call is recorded as a mutation.</summary>
    private sealed class StateConnection : IGameSessionConnection
    {
        private TaskCompletionSource<TacticalBattleSnapshot?>? _held;
        public TacticalBattleSnapshot? State { get; set; }
        public Exception? Failure { get; set; }
        public int StateReads;
        public List<string> Mutations { get; } = [];

        public TaskCompletionSource<TacticalBattleSnapshot?> Hold() =>
            _held = new TaskCompletionSource<TacticalBattleSnapshot?>(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask<TacticalBattleSnapshot?> GetTacticalBattleStateAsync(CancellationToken cancellationToken = default)
        {
            StateReads++;
            if (Interlocked.Exchange(ref _held, null) is { } held) return new(held.Task.WaitAsync(cancellationToken));
            if (Failure is not null) return ValueTask.FromException<TacticalBattleSnapshot?>(Failure);
            return ValueTask.FromResult(State);
        }

        public ValueTask<TacticalBattleResponse> StartTacticalBattleAsync(StartTacticalBattleRequest request, CancellationToken cancellationToken = default) => Mutate<TacticalBattleResponse>("start");
        public ValueTask<TacticalBattleResponse> MoveTacticalBattleAsync(MoveTacticalBattleRequest request, CancellationToken cancellationToken = default) => Mutate<TacticalBattleResponse>("move");
        public ValueTask<TacticalBattleResponse> FireTacticalBattleAsync(FireTacticalBattleRequest request, CancellationToken cancellationToken = default) => Mutate<TacticalBattleResponse>("fire");
        public ValueTask<TacticalBattleResponse> ExitTacticalBattleAsync(ExitTacticalBattleRequest request, CancellationToken cancellationToken = default) => Mutate<TacticalBattleResponse>("exit");
        public ValueTask<TacticalBattleResponse> DebugDestroyTacticalUnitAsync(DebugDestroyTacticalUnitRequest request, CancellationToken cancellationToken = default) => Mutate<TacticalBattleResponse>("debug-destroy");
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) => MutateVoid("command");
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => MutateVoid("dialogue");
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) => MutateVoid("speed");
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => MutateVoid("interaction");
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => MutateVoid("save");
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            yield break;
        }

        private ValueTask<T> Mutate<T>(string name)
        {
            lock (Mutations) Mutations.Add(name);
            return ValueTask.FromException<T>(new InvalidOperationException("list_units must not mutate: " + name));
        }

        private ValueTask MutateVoid(string name)
        {
            lock (Mutations) Mutations.Add(name);
            return ValueTask.CompletedTask;
        }
    }
}
