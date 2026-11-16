using System.Runtime.CompilerServices;
using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class DemoEp0032Us0007Tk0001Tests
{
    [Fact]
    public void OldSnapshot_DeserializesWithoutDemo()
    {
        var snapshot = JsonSerializer.Deserialize<AuthoritativeSnapshot>(
            """{"SnapshotSequence":7,"GameTimeMs":300,"CurrentSpeed":0,"Objects":[],"PlayerCredits":42}""")!;
        Assert.Null(snapshot.DemoCampaign);
        Assert.Equal(42, snapshot.PlayerCredits);
    }

    [Fact]
    public async Task UnknownCommand_IsUnsupported()
    {
        IGameSessionConnection connection = new LegacyConnection();
        var response = await connection.ExecuteDemoCampaignAsync(new UnknownCommand("request-1", 9));
        Assert.False(response.Applied);
        Assert.Equal("unsupported", response.Reason);
        Assert.Equal("request-1", response.CommandId);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await connection.ExecuteDemoCampaignAsync(new UnknownCommand("cancelled", 0), cancellation.Token));
    }

    [Fact]
    public void Response_RoundTripsIdentity()
    {
        var receipt = new DemoCampaignResponse("command-42", false, "stale_revision", 12);
        Assert.Equal(receipt, JsonSerializer.Deserialize<DemoCampaignResponse>(JsonSerializer.Serialize(receipt)));
        var snapshot = JsonSerializer.Deserialize<DemoCampaignSnapshot>("""{"Revision":12,"CommitSequence":18}""")!;
        Assert.Equal(18, snapshot.CommitSequence);
        Assert.Null(snapshot.Company);
        Assert.Null(snapshot.Transport);
        Assert.Null(snapshot.Rewards);
    }

    private sealed record UnknownCommand(string CommandId, long ExpectedRevision)
        : DemoCampaignCommand(CommandId, ExpectedRevision);

    private sealed class LegacyConnection : IGameSessionConnection
    {
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
