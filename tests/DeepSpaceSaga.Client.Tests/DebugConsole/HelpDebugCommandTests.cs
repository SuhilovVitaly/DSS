using DeepSpaceSaga.Client.UI.DebugConsole;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

namespace DeepSpaceSaga.Client.Tests.DebugConsole;

/// <summary>
/// EP-0030 US-0004 TK-0001: <c>help</c> lists every registered command once (name, syntax, description, scope)
/// straight from the registry, without a session and without touching the host.
/// </summary>
public sealed class HelpDebugCommandTests
{
    [Fact]
    public async Task ListsEveryRegisteredCommandOnce()
    {
        string[] names = ["probe", "exit_battle", "kill_drone", "unavailable"];
        var registry = Registry(names.Select(n => new FakeCommand(n)).ToArray());
        var result = await Help(registry, new CountingHost());

        Assert.False(result.IsError);
        Assert.Equal("Команды консоли: 5", result.Lines[0]);
        string[] entries = result.Lines.Skip(1).ToArray();
        Assert.Equal(5, entries.Length);
        foreach (string name in names.Append("help"))
            Assert.Single(entries, line => line.StartsWith(name + " |", StringComparison.Ordinal));
    }

    [Fact]
    public async Task IncludesHelpItself()
    {
        var result = await Help(Registry(), new CountingHost());
        Assert.Equal(["Команды консоли: 1", "help | синтаксис: help | Показать все команды консоли | область: везде"], result.Lines);

        // The default catalog registers help next to the commands of the earlier stories.
        var catalog = DebugCommandCatalog.CreateDefault();
        Assert.True(catalog.TryGet("help", out var registered));
        Assert.IsType<HelpDebugCommand>(registered);
        var names = catalog.DescribeAll().Select(d => d.Name).ToArray();
        Assert.Contains("help", names);
        Assert.Contains("exit_battle", names);
        Assert.Contains("kill_drone", names);
        var catalogHelp = await registered.ExecuteAsync(new DebugCommandContext(catalog, new CountingHost()), "", CancellationToken.None);
        Assert.Contains("kill_drone | синтаксис: kill_drone <имя> | Уничтожить дрона любой стороны по Id или имени | область: текущий бой",
            catalogHelp.Lines);
    }

    [Fact]
    public async Task IncludesUnavailableCommandsWithoutSession()
    {
        var unavailable = new FakeCommand("battle_only", scope: "текущий бой",
            run: (_, _, _) => Task.FromResult(DebugCommandResult.Error("Нет активного боя")));
        var host = new CountingHost(); // main menu: no session, no battle
        var result = await Help(Registry(unavailable), host);

        Assert.False(result.IsError);
        Assert.Contains("battle_only | синтаксис: battle_only <x> | описание battle_only | область: текущий бой", result.Lines);
        Assert.Equal(0, unavailable.Calls); // listing never probes availability by executing commands
    }

    [Fact]
    public async Task IncludesNewRegistrationAutomatically()
    {
        var registry = Registry(new FakeCommand("alpha"));
        var before = await Help(registry, new CountingHost());
        Assert.DoesNotContain(before.Lines, line => line.StartsWith("beta |", StringComparison.Ordinal));

        registry.Register(new FakeCommand("beta"));
        var after = await Help(registry, new CountingHost());
        Assert.Equal("Команды консоли: 3", after.Lines[0]);
        Assert.Contains("beta | синтаксис: beta <x> | описание beta | область: везде", after.Lines);
    }

    [Fact]
    public void SortsOrdinalAndIncludesSyntaxDescriptionScope()
    {
        DebugCommandDescriptor[] descriptors =
        [
            new("Zeta", "Zeta", "верхний регистр", "везде"),
            new("exit_battle", "exit_battle", "Прервать обучающий бой и открыть карту", "обучающий бой"),
            new("help", "help", "Показать все команды консоли", "везде"),
            new("kill_drone", "kill_drone <имя>", "Уничтожить дрона любой стороны по Id или имени", "текущий бой"),
        ];
        var registry = Registry(descriptors.Where(d => d.Name != "help").Select(d => (IDebugCommand)new FakeCommand(d)).Reverse().ToArray());
        var lines = HelpDebugCommand.Format(registry.DescribeAll());

        Assert.Equal(
        [
            "Команды консоли: 4",
            "Zeta | синтаксис: Zeta | верхний регистр | область: везде",
            "exit_battle | синтаксис: exit_battle | Прервать обучающий бой и открыть карту | область: обучающий бой",
            "help | синтаксис: help | Показать все команды консоли | область: везде",
            "kill_drone | синтаксис: kill_drone <имя> | Уничтожить дрона любой стороны по Id или имени | область: текущий бой",
        ], lines);
        Assert.Equal(["Команды консоли: 0"], HelpDebugCommand.Format([]));
    }

    [Theory]
    [InlineData("kill_drone")]
    [InlineData("всё")]
    [InlineData("a b")]
    public async Task RejectsArguments(string arguments)
    {
        var host = new CountingHost();
        var registry = Registry(new FakeCommand("probe"));
        var context = new DebugCommandContext(registry, host);
        Assert.True(registry.TryGet("help", out var help));
        var result = await help.ExecuteAsync(context, arguments, CancellationToken.None);
        Assert.True(result.IsError);
        Assert.Equal([HelpDebugCommand.SyntaxText], result.Lines);
        Assert.Equal("Синтаксис: help", HelpDebugCommand.SyntaxText);

        // Through the console the error is shown under the echoed line.
        using var console = new DebugConsoleController(registry, host);
        Type(console, "help " + arguments);
        await console.SubmitAsync();
        Assert.Equal(["> help " + arguments, "Синтаксис: help"], console.Output);
    }

    /// <summary>
    /// <see cref="DebugCommandContext"/> captures <c>host.Session</c> once at construction and the controller reads it again
    /// for its staleness check; help itself must add no Session read and no other host call.
    /// </summary>
    [Fact]
    public async Task DispatchDoesNotAccessSession()
    {
        var host = new CountingHost();
        var registry = Registry(new FakeCommand("probe"));
        var context = new DebugCommandContext(registry, host);
        Assert.Equal(1, host.SessionReads);
        host.SessionReads = 0;
        Assert.True(registry.TryGet("help", out var help));
        await help.ExecuteAsync(context, "", CancellationToken.None);
        Assert.Equal(0, host.SessionReads);
        Assert.Equal(0, host.OtherCalls);

        // Through the controller help costs exactly as many Session reads as a command that ignores the host.
        using var console = new DebugConsoleController(registry, host);
        host.SessionReads = 0;
        Type(console, "probe");
        await console.SubmitAsync();
        int baseline = host.SessionReads;
        host.SessionReads = 0;
        Type(console, "help");
        await console.SubmitAsync();
        Assert.Equal(baseline, host.SessionReads);
        Assert.Equal(0, host.OtherCalls);
        Assert.Equal(["> probe", "ok", "> help", "Команды консоли: 2"], console.Output.Take(4));
    }

    [Fact]
    public async Task ReturnsAllLinesForScrollableOutput()
    {
        var commands = Enumerable.Range(0, 300).Select(i => (IDebugCommand)new FakeCommand($"cmd_{i:D3}")).ToArray();
        var registry = Registry(commands);
        using var console = new DebugConsoleController(registry, new CountingHost());
        Type(console, "help");
        await console.SubmitAsync();

        Assert.Equal(1 + 1 + 301, console.Output.Count); // echo + header + every command incl. help
        Assert.Equal("Команды консоли: 301", console.Output[1]);
        Assert.Equal("cmd_000 | синтаксис: cmd_000 <x> | описание cmd_000 | область: везде", console.Output[2]);
        Assert.Equal("help | синтаксис: help | Показать все команды консоли | область: везде", console.Output[^1]);
        Assert.Contains("cmd_299 | синтаксис: cmd_299 <x> | описание cmd_299 | область: везде", console.OutputText);
    }

    private static DebugCommandRegistry Registry(params IDebugCommand[] commands)
    {
        var registry = new DebugCommandRegistry();
        registry.Register(new HelpDebugCommand());
        foreach (var command in commands) registry.Register(command);
        return registry;
    }

    private static Task<DebugCommandResult> Help(DebugCommandRegistry registry, CountingHost host)
    {
        Assert.True(registry.TryGet("help", out var help));
        return help.ExecuteAsync(new DebugCommandContext(registry, host), "", CancellationToken.None);
    }

    private static void Type(DebugConsoleController console, string text)
    {
        console.Input.Clear();
        foreach (char c in text) console.OnTextInput(c);
    }

    private sealed class FakeCommand : IDebugCommand
    {
        private readonly Func<DebugCommandContext, string, CancellationToken, Task<DebugCommandResult>>? _run;
        public int Calls;

        public FakeCommand(string name, string scope = "везде",
            Func<DebugCommandContext, string, CancellationToken, Task<DebugCommandResult>>? run = null)
            : this(new DebugCommandDescriptor(name, name + " <x>", "описание " + name, scope), run) { }

        public FakeCommand(DebugCommandDescriptor descriptor,
            Func<DebugCommandContext, string, CancellationToken, Task<DebugCommandResult>>? run = null)
        {
            Descriptor = descriptor;
            _run = run;
        }

        public DebugCommandDescriptor Descriptor { get; }

        public Task<DebugCommandResult> ExecuteAsync(DebugCommandContext context, string arguments, CancellationToken cancellationToken)
        {
            Calls++;
            return _run?.Invoke(context, arguments, cancellationToken) ?? Task.FromResult(DebugCommandResult.Ok("ok"));
        }
    }

    /// <summary>Counts Session getter reads; any other host member is recorded as a forbidden call.</summary>
    private sealed class CountingHost : IDebugConsoleHost
    {
        public int SessionReads;
        public int OtherCalls;

        public GameSessionHandle? Session
        {
            get
            {
                SessionReads++;
                return null;
            }
        }

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
}
