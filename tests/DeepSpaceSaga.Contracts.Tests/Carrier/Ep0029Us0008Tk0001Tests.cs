using System.Runtime.CompilerServices;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

/// <summary>
/// EP-0029 US-0008 TK-0001 — rearm request/response contract and the pre-rearm transport default.
/// </summary>
public class Ep0029Us0008Tk0001Tests
{
    [Fact]
    public void RequestAndReceiptRoundTrip()
    {
        var request = new RearmDroneRequest("cmd-1", "SPC-0001", "DRN-PLAYER-01", 5, 42);
        var copy = JsonSerializer.Deserialize<RearmDroneRequest>(JsonSerializer.Serialize(request))!;
        Assert.Equal("cmd-1", copy.CommandId);
        Assert.Equal("SPC-0001", copy.ShipId);
        Assert.Equal("DRN-PLAYER-01", copy.DroneId);
        Assert.Equal(5, copy.Quantity);
        Assert.Equal(42, copy.ExpectedRevision);

        var snapshot = new CarrierOperationsSnapshot("SPC-0001", "ship.tetrarch", "Tetrarch", 43, 400, 400, false);
        var response = new CarrierCommandResponse(true, Revision: 43, Transferred: 5, Snapshot: snapshot);
        var responseCopy = JsonSerializer.Deserialize<CarrierCommandResponse>(JsonSerializer.Serialize(response))!;
        Assert.True(responseCopy.Accepted);
        Assert.Null(responseCopy.Reason);
        Assert.Equal(43, responseCopy.Revision);
        Assert.Equal(5, responseCopy.Transferred);
        Assert.Equal(43, responseCopy.Snapshot!.Revision);
    }

    [Fact]
    public async Task UnsupportedConnectionDoesNotInventSuccess()
    {
        IGameSessionConnection connection = new LegacyConnection();
        var response = await connection.RearmDroneAsync(
            new RearmDroneRequest("cmd-1", "SPC-0001", "DRN-PLAYER-01", 5, 0), CancellationToken.None);

        Assert.False(response.Accepted);
        Assert.Equal("unsupported", response.Reason);
        Assert.Equal(0, response.Transferred);
        Assert.Equal(0, response.Revision);
        Assert.Null(response.Snapshot);
    }

    private sealed class LegacyConnection : IGameSessionConnection
    {
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId,
            CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
