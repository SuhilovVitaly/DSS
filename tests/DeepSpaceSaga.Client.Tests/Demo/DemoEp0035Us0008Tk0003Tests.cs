using DeepSpaceSaga.Client.UI.DebugConsole;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0035Us0008Tk0003Tests
{
    [Fact]
    public async Task RegisteredQueryAndBooleanSet_RejectOtherArgumentsWithoutMutation()
    {
        var registry = DebugCommandCatalog.CreateDefault();
        Assert.True(registry.TryGet("fog_of_war", out var command));
        Assert.Equal("fog_of_war [true|false]", command.Descriptor.Syntax);
        var host = new Host();
        var context = new DebugCommandContext(registry, host);
        Assert.Contains("True", (await command.ExecuteAsync(context, "", default)).Lines[0]);
        Assert.False((await command.ExecuteAsync(context, "false", default)).IsError);
        Assert.False(host.Value);
        foreach (var invalid in new[] { "0", "1", "true false", "toggle", "null" })
            Assert.True((await command.ExecuteAsync(context, invalid, default)).IsError);
        Assert.False(host.Value);
        Assert.False((await command.ExecuteAsync(context, "true", default)).IsError);
        Assert.True(host.Value);
    }

    private sealed class Host : IDebugConsoleHost
    {
        public bool Value = true;
        public GameSessionHandle? Session => null;
        public bool IsTransitionBusy => false;
        public TacticalBattleScreen? ActiveBattle => null;
        public Task<DebugCommandResult> ConfigureFogOfWarAsync(bool? enabled, CancellationToken cancellationToken)
        { if (enabled is { } value) Value = value; return Task.FromResult(DebugCommandResult.Ok(Value.ToString())); }
        public Task<DebugCommandResult> RunExclusiveAsync(Func<CancellationToken, Task<DebugCommandResult>> operation, CancellationToken cancellationToken) => operation(cancellationToken);
        public Task<DebugCommandResult> ExitTutorialBattleAsync(GameSessionHandle? expectedSession, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
