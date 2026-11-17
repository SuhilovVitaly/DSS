using DeepSpaceSaga.Client.UI.DebugConsole;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class Ep0035Us12Tk5Tests
{
    [Fact]
    public async Task MainMenu_QueryAndSet_WorkWithoutSession()
    {
        var registry = DebugCommandCatalog.CreateDefault();
        Assert.True(registry.TryGet("pirate_squad_chance", out var command));
        var host = new Host();
        var context = new DebugCommandContext(registry, host);
        Assert.False((await command.ExecuteAsync(context, "100", default)).IsError);
        Assert.Equal(100, host.Value);
        Assert.Contains("100", (await command.ExecuteAsync(context, "", default)).Lines[0]);
        Assert.Null(host.Session);
        foreach (string invalid in new[] { "NaN", "-1", "101", "1.5", "10 20" })
            Assert.True((await command.ExecuteAsync(context, invalid, default)).IsError);
        Assert.Equal(100, host.Value);
    }

    private sealed class Host : IDebugConsoleHost
    {
        public int Value = 10;
        public GameSessionHandle? Session => null;
        public bool IsTransitionBusy => false;
        public TacticalBattleScreen? ActiveBattle => null;
        public Task<DebugCommandResult> ConfigurePirateSquadChanceAsync(int? percent, CancellationToken cancellationToken)
        { if (percent is { } value) Value = value; return Task.FromResult(DebugCommandResult.Ok(Value.ToString())); }
        public Task<DebugCommandResult> RunExclusiveAsync(Func<CancellationToken, Task<DebugCommandResult>> operation, CancellationToken cancellationToken) => operation(cancellationToken);
        public Task<DebugCommandResult> ExitTutorialBattleAsync(GameSessionHandle? expectedSession, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
