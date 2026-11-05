using System.Runtime.CompilerServices;
using System.Threading.Channels;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class SessionHandleBattleTests
{
    [Fact]
    public async Task AllMethodsForwardOriginalPayloadsAndLogCommandsAndRefusals()
    {
        var connection = new Connection();
        await using var handle = new GameSessionHandle(connection, deferDebugTuningWrites: true);
        Assert.Same(connection.Options, await handle.GetTacticalBattleOptionsAsync());
        Assert.Null(await handle.GetTacticalBattleStateAsync());
        string id = Guid.NewGuid().ToString("N");
        var start = new StartTacticalBattleRequest(id + "-start", -1, "initial", "raid", "defence",
            [new("Vera", "gun", "plate", null, 0)]);
        var move = new MoveTacticalBattleRequest(id + "-move", "battle", 3, [new("Vera", 12.34567890123456, -7.125)]);
        var fire = new FireTacticalBattleRequest(id + "-fire", "battle", 4, [new("Vera", null)]);
        var exit = new ExitTacticalBattleRequest(id + "-exit", "battle");
        foreach (var response in new[]
        {
            await handle.StartTacticalBattleAsync(start), await handle.MoveTacticalBattleAsync(move),
            await handle.FireTacticalBattleAsync(fire), await handle.ExitTacticalBattleAsync(exit)
        })
        {
            Assert.False(response.Accepted);
            Assert.Equal(TacticalBattleRejectionCode.WrongPhase, response.Code);
            Assert.Equal(TacticalBattlePhase.Fire, response.CurrentPhase);
            Assert.Empty(response.NewEvents);
        }
        Assert.Equal(new object[] { start, move, fire, exit }, connection.Requests);
        string log = ReadLog();
        foreach (string operation in new[] { "start", "move", "fire", "exit" })
        {
            Assert.Contains("TACTICAL send " + operation + ":", log);
            Assert.Contains($"command={id}-{operation}, code=WrongPhase, phase=Fire", log);
        }
        Assert.Contains("12.34567890123456", log);
        Assert.Contains("\"TargetId\":null", log);
        Assert.Null(handle.Failure);
    }

    [Fact]
    public async Task BrokenTransportProducesTypedUnavailableAndDiagnosticInsteadOfUiException()
    {
        var connection = new Connection { Throw = true };
        await using var handle = new GameSessionHandle(connection, deferDebugTuningWrites: true);
        Assert.False((await handle.GetTacticalBattleOptionsAsync()).Availability.Available);
        Assert.Null(await handle.GetTacticalBattleStateAsync());
        string id = Guid.NewGuid().ToString("N");
        var response = await handle.StartTacticalBattleAsync(new(id, 1));
        Assert.Equal(id, response.CommandId);
        Assert.Equal(TacticalBattleRejectionCode.Unavailable, response.Code);
        Assert.False(response.Accepted);
        Assert.Contains($"command={id}, code=Unavailable, phase=none", ReadLog());
        Assert.Null(handle.Failure);
    }

    [Fact]
    public async Task CallerCancellationIsLoggedAndDoesNotBecomeGameplayRejection()
    {
        var connection = new Connection();
        await using var handle = new GameSessionHandle(connection, deferDebugTuningWrites: true);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        string id = Guid.NewGuid().ToString("N");
        var task = handle.StartTacticalBattleAsync(new(id, 1), cancellation.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.AsTask());
        Assert.Contains($"TACTICAL canceled start: command={id}", ReadLog());
        Assert.Empty(connection.Requests);
    }

    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public void EveryRefusalHasRealLocalizedTextAndUnknownCodesUseGenericMessage(string language)
    {
        using var scope = Localization.UseLocaleForTests(language);
        foreach (var code in Enum.GetValues<TacticalBattleRejectionCode>())
        {
            string text = GameSessionHandle.TacticalBattleRefusalText(code);
            if (code == TacticalBattleRejectionCode.None) Assert.Empty(text);
            else
            {
                Assert.False(string.IsNullOrWhiteSpace(text));
                Assert.DoesNotContain("TacticalBattle.", text);
                Assert.NotEqual(code.ToString(), text);
            }
        }
        Assert.Equal(GameSessionHandle.TacticalBattleRefusalText(TacticalBattleRejectionCode.Unavailable),
            GameSessionHandle.TacticalBattleRefusalText((TacticalBattleRejectionCode)999));
    }

    private static string ReadLog()
    {
        using var stream = new FileStream(InterfaceLog.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private sealed class Connection : IGameSessionConnection
    {
        private readonly Channel<AuthoritativeSnapshot> _snapshots = Channel.CreateUnbounded<AuthoritativeSnapshot>();
        public TacticalBattleOptions Options { get; } = new(new(true), [], [], [], [], []);
        public List<object> Requests { get; } = [];
        public bool Throw { get; init; }
        public ValueTask<TacticalBattleOptions> GetTacticalBattleOptionsAsync(CancellationToken cancellationToken = default) =>
            Throw ? throw new IOException("test transport") : ValueTask.FromResult(Options);
        public ValueTask<TacticalBattleSnapshot?> GetTacticalBattleStateAsync(CancellationToken cancellationToken = default) =>
            Throw ? throw new IOException("test transport") : ValueTask.FromResult<TacticalBattleSnapshot?>(null);
        private ValueTask<TacticalBattleResponse> Reply(object request, string id, CancellationToken token)
        {
            if (token.IsCancellationRequested) return ValueTask.FromCanceled<TacticalBattleResponse>(token);
            if (Throw) throw new IOException("test transport");
            Requests.Add(request);
            return ValueTask.FromResult(new TacticalBattleResponse(id, false, TacticalBattleRejectionCode.WrongPhase, TacticalBattlePhase.Fire, null, []));
        }
        public ValueTask<TacticalBattleResponse> StartTacticalBattleAsync(StartTacticalBattleRequest request, CancellationToken cancellationToken = default) => Reply(request, request.CommandId, cancellationToken);
        public ValueTask<TacticalBattleResponse> MoveTacticalBattleAsync(MoveTacticalBattleRequest request, CancellationToken cancellationToken = default) => Reply(request, request.CommandId, cancellationToken);
        public ValueTask<TacticalBattleResponse> FireTacticalBattleAsync(FireTacticalBattleRequest request, CancellationToken cancellationToken = default) => Reply(request, request.CommandId, cancellationToken);
        public ValueTask<TacticalBattleResponse> ExitTacticalBattleAsync(ExitTacticalBattleRequest request, CancellationToken cancellationToken = default) => Reply(request, request.CommandId, cancellationToken);
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
}
