using DeepSpaceSaga.Client.UI.DebugConsole;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.LocalClient;

namespace DeepSpaceSaga.Client.Tests.Demo;

[CollectionDefinition("Loot debug process", DisableParallelization = true)]
public sealed class LootDebugProcessCollection;
[Collection("Loot debug process")]
public sealed class DemoEp0036Us0012Tk0002Tests
{
    private sealed class Host(GameSessionHandle? session = null) : IDebugConsoleHost
    {
        public GameSessionHandle? Session => session;
        public bool IsTransitionBusy => false;
        public TacticalBattleScreen? ActiveBattle => null;
        public int Calls;
        public Task<DebugCommandResult> RunExclusiveAsync(Func<CancellationToken, Task<DebugCommandResult>> action, CancellationToken token) { Calls++; return action(token); }
        public Task<DebugCommandResult> ExitTutorialBattleAsync(GameSessionHandle? expectedSession, CancellationToken token) => throw new NotSupportedException();
    }
    [Fact]
    public async Task SyntaxAndHelp_ReadOnlyStateDoNotSendCommands()
    {
        var registry = DebugCommandCatalog.CreateDefault(); var host = new Host(); var context = new DebugCommandContext(registry, host);
        foreach (string name in new[] { "engineer_bonus", "loot_force", "loot_state" }) Assert.True(registry.TryGet(name, out _));
        Assert.True((await new LootForceDebugCommand().ExecuteAsync(context, "rolls success", default)).IsError);
        Assert.True((await new EngineerBonusDebugCommand().ExecuteAsync(context, "9", default)).IsError);
        Assert.Equal("Нет открытого сбора", Assert.Single((await new LootStateDebugCommand().ExecuteAsync(context, "", default)).Lines));
        Assert.Equal(0, host.Calls);
        Assert.Contains("навык", EngineerBonusDebugCommand.Describe(new(), 12));
        Assert.Contains("override", EngineerBonusDebugCommand.Describe(new(EngineerBonusPp: 0), 12));
        Assert.True((await new LootForceDebugCommand().ExecuteAsync(context, "rolls keep", default)).IsError);
    }
    [Fact]
    public async Task ActiveSessionAppliesOverride_AndLootStateIsReadOnly()
    {
        try
        {
            using var engine = DemoEp0036Us0007Tk0004Tests.Won();
            await using var connection = new LocalGameSessionConnection(engine);
            await using var session = new GameSessionHandle(connection);
            await DemoEp0036Us0007Tk0004Tests.WaitFor(() => session.DemoCampaign?.Rewards?.Loot is not null);
            var context = new DebugCommandContext(DebugCommandCatalog.CreateDefault(), new Host(session));
            var result = await new LootForceDebugCommand().ExecuteAsync(context, "attempts success", default);
            Assert.False(result.IsError);
            Assert.Equal("success", engine.CaptureSnapshot().DemoCampaign!.DebugSettings!.LootAttempts);
            long revision = engine.CaptureSnapshot().DemoCampaign!.Revision;
            var report = await new LootStateDebugCommand().ExecuteAsync(context, "", default);
            Assert.False(report.IsError); Assert.Contains(report.Lines, line => line.Contains("свободно", StringComparison.Ordinal));
            Assert.Equal(revision, engine.CaptureSnapshot().DemoCampaign!.Revision);
        }
        finally { SimulationEngine.SetLootDebug(new("reset", 0)); }
    }
}
