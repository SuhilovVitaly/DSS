using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using DeepSpaceSaga.Client.Tests.Tutorial;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

/// <summary>EP-0030 US-0003 TK-0004: the debug destroy request travels through the session handle and the battle flow like a turn.</summary>
[Collection("InterfaceLog")]
public sealed class DebugDestroyFlowTests
{
    [Fact]
    public async Task HandleForwardsAndMapsTransportFailureToUnavailable()
    {
        var connection = new Connection();
        await using (var handle = new GameSessionHandle(connection, deferDebugTuningWrites: true))
        {
            string id = "TB-debug-kill-" + Guid.NewGuid().ToString("N");
            var request = new DebugDestroyTacticalUnitRequest(id, "battle", 1, "tag-1");

            var response = await handle.DebugDestroyTacticalUnitAsync(request);

            Assert.Same(request, Assert.Single(connection.Requests));
            Assert.True(response.Accepted);
            Assert.Equal(id, response.CommandId);
            string log = ReadLog();
            Assert.Contains("TACTICAL send debug-destroy:", log);
            Assert.Contains("\"UnitId\":\"tag-1\"", log);
            Assert.Contains($"TACTICAL accepted debug-destroy: command={id}", log);
        }

        var broken = new Connection { Throw = true };
        await using var brokenHandle = new GameSessionHandle(broken, deferDebugTuningWrites: true);
        string failedId = "TB-debug-kill-" + Guid.NewGuid().ToString("N");

        var failed = await brokenHandle.DebugDestroyTacticalUnitAsync(new(failedId, "battle", 1, "tag-1"));

        Assert.False(failed.Accepted);
        Assert.Equal(TacticalBattleRejectionCode.Unavailable, failed.Code);
        Assert.Equal(failedId, failed.CommandId);
        Assert.Null(failed.State);
        Assert.Contains($"TACTICAL transport failed debug-destroy: command={failedId}", ReadLog());
        Assert.Null(brokenHandle.Failure);
    }

    [Fact]
    public async Task FlowAdoptsAcceptedStateAndReturnsNullWhilePending()
    {
        var connection = new Connection();
        await using var handle = new GameSessionHandle(connection, deferDebugTuningWrites: true);
        var flow = new TacticalBattleFlow(handle);
        var started = await flow.StartAsync(new("TB-start-" + Guid.NewGuid().ToString("N"), 1));
        Assert.True(started!.Accepted);
        var before = flow.State!;
        Assert.Equal(1UL, before.Revision);

        connection.DestroyGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var kill = flow.DebugDestroyAsync(new("TB-debug-kill-1", before.BattleId, before.Revision, "tag-1"));
        Assert.True(flow.Pending);

        // A second request while the first is in flight is refused locally and never reaches the connection.
        Assert.Null(await flow.DebugDestroyAsync(new("TB-debug-kill-2", before.BattleId, before.Revision, "tag-2")));
        Assert.Null(await flow.MoveAsync(new("TB-move", before.BattleId, before.Revision)));
        Assert.Same(before, flow.State);

        connection.DestroyGate.SetResult();
        var response = await kill.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.True(response!.Accepted);
        Assert.False(flow.Pending);
        Assert.Equal(2UL, flow.State!.Revision);
        Assert.Same(response.State, flow.State);
        Assert.Equal(TacticalUnitState.Destroyed, flow.State.Units.Single(u => u.Id == "tag-1").State);
        Assert.Same(flow.State, flow.PresentationState);
        Assert.Equal("", flow.Error);
        Assert.Single(connection.Requests.OfType<DebugDestroyTacticalUnitRequest>());
        Assert.Empty(connection.Requests.OfType<MoveTacticalBattleRequest>());

        // A refusal keeps the confirmed state and surfaces the localized refusal text.
        connection.RefuseDestroy = true;
        var refused = await flow.DebugDestroyAsync(new("TB-debug-kill-3", before.BattleId, 2, "tag-1"));
        Assert.False(refused!.Accepted);
        Assert.Equal(2UL, flow.State!.Revision);
        Assert.Contains(BattleFormat.Text(GameSessionHandle.TacticalBattleRefusalText(TacticalBattleRejectionCode.InvalidTarget)), flow.Error);
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
        private TacticalBattleSnapshot _state = TutorialTestBattle.Battle();
        public List<object> Requests { get; } = [];
        public bool Throw { get; init; }
        public bool RefuseDestroy { get; set; }
        public TaskCompletionSource? DestroyGate { get; set; }

        public ValueTask<TacticalBattleOptions> GetTacticalBattleOptionsAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new TacticalBattleOptions(new(true), [], [], [], [], []));
        public ValueTask<TacticalBattleSnapshot?> GetTacticalBattleStateAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<TacticalBattleSnapshot?>(_state);
        public ValueTask<TacticalBattleResponse> StartTacticalBattleAsync(StartTacticalBattleRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return ValueTask.FromResult(new TacticalBattleResponse(request.CommandId, true, TacticalBattleRejectionCode.None, _state.Phase, _state, []));
        }
        public ValueTask<TacticalBattleResponse> MoveTacticalBattleAsync(MoveTacticalBattleRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return ValueTask.FromResult(new TacticalBattleResponse(request.CommandId, false, TacticalBattleRejectionCode.WrongPhase, _state.Phase, _state, []));
        }
        public ValueTask<TacticalBattleResponse> FireTacticalBattleAsync(FireTacticalBattleRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public ValueTask<TacticalBattleResponse> ExitTacticalBattleAsync(ExitTacticalBattleRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public ValueTask<TacticalBattleResponse> DebugDestroyTacticalUnitAsync(DebugDestroyTacticalUnitRequest request, CancellationToken cancellationToken = default)
        {
            if (Throw) throw new IOException("test transport");
            Requests.Add(request);
            return DestroyGate is { } gate ? new(Destroy(gate.Task, request)) : ValueTask.FromResult(Apply(request));
        }
        private async Task<TacticalBattleResponse> Destroy(Task gate, DebugDestroyTacticalUnitRequest request)
        {
            await gate;
            return Apply(request);
        }
        private TacticalBattleResponse Apply(DebugDestroyTacticalUnitRequest request)
        {
            if (RefuseDestroy)
                return new(request.CommandId, false, TacticalBattleRejectionCode.InvalidTarget, _state.Phase, _state, []);
            _state = _state with
            {
                Revision = _state.Revision + 1,
                Units = _state.Units.Select(u => u.Id == request.UnitId ? u with { Hull = 0, Speed = 0, State = TacticalUnitState.Destroyed } : u)
                    .ToImmutableArray(),
            };
            return new(request.CommandId, true, TacticalBattleRejectionCode.None, _state.Phase, _state,
                [new(1, _state.Turn, _state.Phase, TacticalBattleLogKind.Destroyed, TargetId: request.UnitId)]);
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
}
