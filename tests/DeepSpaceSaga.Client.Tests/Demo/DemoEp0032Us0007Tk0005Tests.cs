using System.Runtime.CompilerServices;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0032Us0007Tk0005Tests
{
    [Fact]
    public async Task ObserverFailure_DoesNotReplaceConfirmedReceipt()
    {
        var connection = new Connection();
        await using var handle = new GameSessionHandle(connection, deferDebugTuningWrites: true);
        handle.DemoCampaignResponded += _ => throw new InvalidOperationException("observer");
        var receipt = new DemoCampaignResponse("confirmed", true, null, 1);
        connection.Response.SetResult(receipt);
        Assert.Equal(receipt, await handle.ExecuteDemoCampaignAsync(new Command("confirmed", 0)));
    }

    [Fact]
    public async Task Pending_DoesNotChangeBalance()
    {
        var connection = new Connection();
        await using var handle = new GameSessionHandle(connection, deferDebugTuningWrites: true);
        var command = new Command("one", 0);
        var first = handle.ExecuteDemoCampaignAsync(command).AsTask();
        var second = handle.ExecuteDemoCampaignAsync(command).AsTask();
        Assert.True(handle.IsDemoCampaignPending("one"));
        Assert.Equal(1, connection.Calls);
        Assert.Null(handle.DemoCampaign);
        connection.Response.SetResult(new("one", false, "unsupported", 0));
        Assert.Equal(await first, await second);
    }

    [Fact]
    public async Task OldSessionResponse_IsIgnored()
    {
        var connection = new Connection();
        var handle = new GameSessionHandle(connection, deferDebugTuningWrites: true);
        int presented = 0;
        handle.DemoCampaignResponded += _ => presented++;
        var pending = handle.ExecuteDemoCampaignAsync(new Command("old", 0)).AsTask();
        await handle.DisposeAsync();
        connection.Response.SetResult(new("old", true, null, 1));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(0, presented);
    }

    [Fact]
    public async Task RepeatedResponse_IsPresentedOnce()
    {
        var connection = new Connection();
        await using var handle = new GameSessionHandle(connection, deferDebugTuningWrites: true);
        int presented = 0;
        handle.DemoCampaignResponded += _ => presented++;
        connection.Response.SetResult(new("same", false, "unsupported", 0));
        await handle.ExecuteDemoCampaignAsync(new Command("same", 0));
        await handle.ExecuteDemoCampaignAsync(new Command("same", 0));
        Assert.Equal(1, presented);
    }

    [Fact]
    public async Task AlreadyConfirmedReceiptIsNotReplacedWhenClosingBegins()
    {
        var connection = new Connection();
        await using var handle = new GameSessionHandle(connection, deferDebugTuningWrites: true);
        var receipt = new DemoCampaignResponse("committed", true, null, 1);
        connection.Response.SetResult(receipt);
        connection.BeforeReturn = () => _ = handle.DisposeAsync();
        Assert.Equal(receipt, await handle.ExecuteDemoCampaignAsync(new Command("committed", 0)));
    }

    private sealed record Command(string CommandId, long ExpectedRevision) : DemoCampaignCommand(CommandId, ExpectedRevision);

    private sealed class Connection : IGameSessionConnection
    {
        public readonly TaskCompletionSource<DemoCampaignResponse> Response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Calls;
        public Action? BeforeReturn;
        public ValueTask<DemoCampaignResponse> ExecuteDemoCampaignAsync(DemoCampaignCommand command, CancellationToken cancellationToken = default)
        {
            Calls++;
            BeforeReturn?.Invoke();
            return new(Response.Task);
        }
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            yield break;
        }
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
