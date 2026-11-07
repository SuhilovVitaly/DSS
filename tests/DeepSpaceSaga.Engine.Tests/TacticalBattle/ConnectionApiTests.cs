using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class ConnectionApiTests
{
    private static SimulationEngine Engine()
    {
        string root = Path.GetFullPath(Path.Combine(ReferenceFixtureTests.Root, "../../../.."));
        var engine = SimulationEngine.CreateFromSettingsFile(Path.Combine(root, "src/DeepSpaceSaga.Client/Settings.json"));
        engine.SetSpeed(SimulationSpeed.Speed0);
        return engine;
    }

    [Fact]
    public async Task LegacyImplementationsInheritTypedUnavailableDefaults()
    {
        IGameSessionConnection connection = new LegacyConnection();
        Assert.False((await connection.GetTacticalBattleOptionsAsync()).Availability.Available);
        Assert.Null(await connection.GetTacticalBattleStateAsync());
        foreach (var task in new[]
        {
            connection.StartTacticalBattleAsync(new("start", 1)), connection.MoveTacticalBattleAsync(new("move", "start", 1)),
            connection.FireTacticalBattleAsync(new("fire", "start", 1)), connection.ExitTacticalBattleAsync(new("exit", "start"))
        })
        {
            var response = await task;
            Assert.False(response.Accepted);
            Assert.Equal(TacticalBattleRejectionCode.Unsupported, response.Code);
        }
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        var pending = connection.GetTacticalBattleOptionsAsync(canceled.Token); // obtaining the ValueTask does not throw
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.AsTask());
    }

    [Fact]
    public async Task LocalApiForwardsAllPhasesAndTypedRejections()
    {
        await using var connection = new LocalGameSessionConnection(Engine());
        Assert.True((await connection.GetTacticalBattleOptionsAsync()).Availability.Available);
        Assert.Null(await connection.GetTacticalBattleStateAsync());
        var invalid = await connection.StartTacticalBattleAsync(new("invalid", 1, "unknown"));
        Assert.Equal(TacticalBattleRejectionCode.UnknownScenario, invalid.Code);
        var start = await connection.StartTacticalBattleAsync(new("start", 1));
        Assert.True(start.Accepted);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(start.State),
            System.Text.Json.JsonSerializer.Serialize(await connection.GetTacticalBattleStateAsync()));
        var wrongPhase = await connection.FireTacticalBattleAsync(new("wrong", "start", start.State!.Revision));
        Assert.Equal(TacticalBattleRejectionCode.WrongPhase, wrongPhase.Code);
        var move = await connection.MoveTacticalBattleAsync(new("move", "start", start.State.Revision));
        Assert.True(move.Accepted);
        Assert.True(move.State!.FireSkipped);
        Assert.Equal(move, await connection.MoveTacticalBattleAsync(new("move", "start", start.State.Revision)));
        Assert.True((await connection.ExitTacticalBattleAsync(new("exit", "start"))).Accepted);
        Assert.Null(await connection.GetTacticalBattleStateAsync());
    }

    [Fact]
    public async Task ConcurrentCommandsAndWorldCapturesShareOneAuthoritativeBoundary()
    {
        var engine = Engine();
        await using var connection = new LocalGameSessionConnection(engine);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await using var snapshots = connection.ReadSnapshotsAsync(timeout.Token).GetAsyncEnumerator();
        Assert.True(await snapshots.MoveNextAsync()); // actual LocalClient tick transport runs concurrently
        var starts = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(async () =>
            await connection.StartTacticalBattleAsync(new("same-start", 1)))));
        Assert.All(starts, s => Assert.Equal(starts[0], s));
        var first = starts[0].State!;
        var moves = await Task.WhenAll(Enumerable.Range(0, 16).Select(i => Task.Run(async () =>
        {
            Assert.True(engine.CaptureSnapshot().IsTacticalBattleActive);
            return await connection.MoveTacticalBattleAsync(new($"move-{i}", first.BattleId, first.Revision));
        })));
        Assert.Single(moves, m => m.Accepted);
        Assert.Equal(15, moves.Count(m => m.Code == TacticalBattleRejectionCode.StaleRevision));
        var current = await connection.GetTacticalBattleStateAsync();
        Assert.Equal((ulong)2, current!.Revision);
        Assert.Equal(2, current.Turn);
        Assert.Equal(74, engine.TacticalBattleSessionForTests!.Battle.Random.Calls);
    }

    [Fact]
    public async Task CancellationAndDisposalFaultTasksWithoutApplyingCommands()
    {
        var engine = Engine();
        var connection = new LocalGameSessionConnection(engine);
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        var pending = connection.StartTacticalBattleAsync(new("canceled", 1), canceled.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.AsTask());
        Assert.False(engine.IsTacticalBattleActive);
        await connection.DisposeAsync();
        var disposed = connection.StartTacticalBattleAsync(new("disposed", 1));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => disposed.AsTask());
    }

    private sealed class LegacyConnection : IGameSessionConnection
    {
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
