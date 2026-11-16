using DeepSpaceSaga.Client.UI.DebugConsole;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests.DebugConsole;

public sealed class DebugConsoleControllerTests
{
    [Theory]
    [InlineData(Key.P, KeyModifiers.Control | KeyModifiers.Alt, true)]
    [InlineData(Key.P, KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Shift, true)]
    [InlineData(Key.P, KeyModifiers.Control, false)]
    [InlineData(Key.P, KeyModifiers.Alt, false)]
    [InlineData(Key.P, KeyModifiers.None, false)]
    [InlineData(Key.O, KeyModifiers.Control | KeyModifiers.Alt, false)]
    public void ToggleChordRequiresControlAndAlt(Key key, KeyModifiers modifiers, bool expected) =>
        Assert.Equal(expected, DebugConsoleController.IsToggleChord(key, modifiers));

    [Fact]
    public async Task EmptyLineDoesNothing()
    {
        var command = new FakeCommand("probe");
        using var console = Create(new FakeHost(), command);
        await console.SubmitAsync();
        Type(console, "   ");
        await console.SubmitAsync();
        Assert.Empty(console.Output);
        Assert.Equal(0, command.Calls);
        Assert.False(console.IsExecuting);
    }

    [Fact]
    public async Task UnknownCommandReportsErrorWithoutCallingHandlers()
    {
        var command = new FakeCommand("probe");
        using var console = Create(new FakeHost(), command);
        Type(console, "fly_away now");
        await console.SubmitAsync();
        Assert.Equal(["> fly_away now", "Неизвестная команда: fly_away. Введите help"], console.Output);
        Assert.Equal(0, command.Calls);
        Assert.Equal("", console.Input.Text);
        // Names are Ordinal: a different case is a different (unknown) command.
        Type(console, "PROBE");
        await console.SubmitAsync();
        Assert.Equal(0, command.Calls);
        Assert.Equal("Неизвестная команда: PROBE. Введите help", console.Output[^1]);
    }

    [Theory]
    [InlineData("kill_drone Red Leader 2", "kill_drone", "Red Leader 2")]
    [InlineData("   help   ", "help", "")]
    [InlineData("exit_battle\t  now  ", "exit_battle", "now")]
    [InlineData("a  b   c", "a", "b   c")]
    public void ParsesNameAndWholeRestAsArguments(string line, string name, string arguments)
    {
        Assert.True(DebugConsoleController.TryParse(line, out string parsedName, out string parsedArguments));
        Assert.Equal(name, parsedName);
        Assert.Equal(arguments, parsedArguments);
        Assert.False(DebugConsoleController.TryParse("  \t ", out _, out _));
    }

    [Fact]
    public async Task ParsedArgumentsReachTheHandler()
    {
        var command = new FakeCommand("kill_drone");
        using var console = Create(new FakeHost(), command);
        Type(console, "  kill_drone   Red Leader  ");
        await console.SubmitAsync();
        Assert.Equal("Red Leader", command.LastArguments);
    }

    [Fact]
    public void RegistryRejectsDuplicateOrdinalName()
    {
        var registry = new DebugCommandRegistry();
        registry.Register(new FakeCommand("help"));
        Assert.Throws<InvalidOperationException>(() => registry.Register(new FakeCommand("help")));
        registry.Register(new FakeCommand("HELP")); // Ordinal: different name
        Assert.True(registry.TryGet("help", out var found));
        Assert.Equal("help", found.Descriptor.Name);
        Assert.False(registry.TryGet("Help", out _));
    }

    [Fact]
    public void DescribeAllIsOrdinalAndComplete()
    {
        var registry = new DebugCommandRegistry();
        foreach (string name in new[] { "list_units", "exit_battle", "help", "Zeta", "kill_drone" })
            registry.Register(new FakeCommand(name));
        var all = registry.DescribeAll();
        Assert.Equal(["Zeta", "exit_battle", "help", "kill_drone", "list_units"], all.Select(d => d.Name));
        Assert.All(all, d => Assert.Equal(d.Name + " <x>", d.Syntax));
        Assert.Empty(new DebugCommandRegistry().DescribeAll());
    }

    [Fact]
    public async Task SecondSubmitWhileExecutingIsRejected()
    {
        var gate = new TaskCompletionSource<DebugCommandResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var command = new FakeCommand("slow", (_, _, _) => gate.Task);
        using var console = Create(new FakeHost(), command);
        Type(console, "slow");
        var first = console.SubmitAsync();
        Assert.True(console.IsExecuting);
        Type(console, "slow");
        await console.SubmitAsync();
        Assert.Equal(1, command.Calls);
        Assert.Equal(DebugConsoleController.AlreadyExecutingText, console.Output[^1]);
        gate.SetResult(DebugCommandResult.Ok("готово"));
        await first;
        Assert.False(console.IsExecuting);
        Assert.Equal("готово", console.Output[^1]);
        Assert.Equal(1, command.Calls);
    }

    [Fact]
    public async Task ResultFromReplacedSessionIsDiscarded()
    {
        await using var first = new GameSessionHandle(new IdleConnection(), deferDebugTuningWrites: true);
        await using var second = new GameSessionHandle(new IdleConnection(), deferDebugTuningWrites: true);
        var host = new FakeHost { Session = first };
        var gate = new TaskCompletionSource<DebugCommandResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        DebugCommandContext? seen = null;
        var command = new FakeCommand("slow", (context, _, _) => { seen = context; return gate.Task; });
        using var console = Create(host, command);
        console.Open();
        Type(console, "slow");
        var pending = console.SubmitAsync();
        Assert.Same(first, seen!.Session);
        Assert.True(seen.IsCurrent);
        host.Session = second;
        Assert.False(seen.IsCurrent);
        gate.SetResult(new DebugCommandResult(["опубликовано"], CloseConsole: true));
        await pending;
        Assert.DoesNotContain("опубликовано", console.Output);
        Assert.Equal(DebugConsoleController.StaleResultText, console.Output[^1]);
        Assert.True(console.IsOpen); // a discarded result does not close the console

        // Closing the session to the main menu is also a change.
        gate = new TaskCompletionSource<DebugCommandResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        host.Session = first;
        Type(console, "slow");
        pending = console.SubmitAsync();
        host.Session = null;
        gate.SetResult(DebugCommandResult.Ok("опубликовано"));
        await pending;
        Assert.Equal(DebugConsoleController.StaleResultText, console.Output[^1]);
    }

    [Fact]
    public async Task HandlerExceptionBecomesErrorLine()
    {
        var command = new FakeCommand("boom", (_, _, _) => throw new InvalidOperationException("сломано"));
        var asyncCommand = new FakeCommand("boom_async", async (_, _, _) => { await Task.Yield(); throw new InvalidOperationException("позже"); });
        using var console = Create(new FakeHost(), command, asyncCommand);
        Type(console, "boom");
        await console.SubmitAsync();
        Assert.Equal("Ошибка: сломано", console.Output[^1]);
        Assert.False(console.IsExecuting);
        Type(console, "boom_async");
        await console.SubmitAsync();
        Assert.Equal("Ошибка: позже", console.Output[^1]);
        Assert.False(console.IsExecuting);
    }

    [Fact]
    public async Task CloseConsoleResultCloses()
    {
        var closing = new FakeCommand("close", (_, _, _) => Task.FromResult(new DebugCommandResult(["закрыто"], CloseConsole: true)));
        var staying = new FakeCommand("stay");
        using var console = Create(new FakeHost(), closing, staying);
        console.Open();
        Type(console, "stay");
        await console.SubmitAsync();
        Assert.True(console.IsOpen);
        Type(console, "close");
        await console.SubmitAsync();
        Assert.False(console.IsOpen);
        Assert.Equal("закрыто", console.Output[^1]);
    }

    [Fact]
    public async Task EscapeClosesConsole()
    {
        var command = new FakeCommand("probe");
        using var console = Create(new FakeHost(), command);
        console.Toggle();
        Assert.True(console.IsOpen);
        Assert.True(console.OnKeyDown(Key.Escape, KeyModifiers.None, repeat: false));
        Assert.False(console.IsOpen);
        console.Open();

        // Every key is consumed; typing, Backspace and Enter work through the controller.
        foreach (char c in "probeX") console.OnTextInput(c);
        Assert.True(console.OnKeyDown(Key.Backspace, KeyModifiers.None, repeat: false));
        Assert.Equal("probe", console.Input.Text);
        Assert.True(console.OnKeyDown(Key.Enter, KeyModifiers.None, repeat: true)); // repeat does not submit
        Assert.Equal(0, command.Calls);
        Assert.True(console.OnKeyDown(Key.KeypadEnter, KeyModifiers.None, repeat: false));
        await WaitIdle(console);
        Assert.Equal(1, command.Calls);
        Assert.True(console.OnKeyDown(Key.F5, KeyModifiers.None, repeat: false));
        Assert.True(console.OnKeyDown(Key.Up, KeyModifiers.None, repeat: false));
        Assert.True(console.IsOpen);
        console.Toggle();
        Assert.False(console.IsOpen);
    }

    [Fact]
    public async Task LongResultIsFullyRetained()
    {
        string[] lines = Enumerable.Range(0, 1500).Select(i => $"строка {i}").ToArray();
        var command = new FakeCommand("long", (_, _, _) => Task.FromResult(DebugCommandResult.Ok(lines)));
        using var console = Create(new FakeHost(), command);
        Type(console, "long");
        await console.SubmitAsync();
        Assert.Equal(1501, console.Output.Count);
        Assert.Equal(lines, console.Output.Skip(1));
        Assert.Contains("строка 1499", console.OutputText);

        // Only the oldest lines beyond the 2000-line window are dropped.
        Type(console, "long");
        await console.SubmitAsync();
        Assert.Equal(DebugConsoleController.MaxOutputLines, console.Output.Count);
        Assert.Equal(lines, console.Output.TakeLast(1500));

        // Input is limited to 160 characters.
        foreach (char c in new string('x', 200)) console.OnTextInput(c);
        Assert.Equal(DebugConsoleController.MaxInputLength, console.Input.Length);
    }

    private static DebugConsoleController Create(FakeHost host, params IDebugCommand[] commands)
    {
        var registry = new DebugCommandRegistry();
        foreach (var command in commands) registry.Register(command);
        return new DebugConsoleController(registry, host);
    }

    private static void Type(DebugConsoleController console, string text)
    {
        console.Input.Clear();
        foreach (char c in text) console.OnTextInput(c);
    }

    private static async Task WaitIdle(DebugConsoleController console)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (console.IsExecuting) await Task.Delay(5, timeout.Token);
    }

    private sealed class FakeCommand(string name, Func<DebugCommandContext, string, CancellationToken, Task<DebugCommandResult>>? run = null) : IDebugCommand
    {
        public int Calls;
        public string? LastArguments;
        public DebugCommandDescriptor Descriptor { get; } = new(name, name + " <x>", "описание", "везде");

        public Task<DebugCommandResult> ExecuteAsync(DebugCommandContext context, string arguments, CancellationToken cancellationToken)
        {
            Calls++;
            LastArguments = arguments;
            return run?.Invoke(context, arguments, cancellationToken) ?? Task.FromResult(DebugCommandResult.Ok("ok " + name));
        }
    }

    private sealed class FakeHost : IDebugConsoleHost
    {
        public GameSessionHandle? Session { get; set; }
        public bool IsTransitionBusy { get; set; }
        public TacticalBattleScreen? ActiveBattle => null;

        public Task<DebugCommandResult> RunExclusiveAsync(Func<CancellationToken, Task<DebugCommandResult>> operation, CancellationToken cancellationToken) =>
            IsTransitionBusy ? Task.FromResult(DebugCommandResult.Busy()) : operation(cancellationToken);

        public Task<DebugCommandResult> ExitTutorialBattleAsync(GameSessionHandle? expectedSession, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class IdleConnection : IGameSessionConnection
    {
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            yield break;
        }
    }
}
