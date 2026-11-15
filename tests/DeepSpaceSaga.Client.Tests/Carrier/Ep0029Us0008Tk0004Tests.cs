using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests;

public sealed class Ep0029Us0008Tk0004Tests
{
    private static AuthoritativeSnapshot Snapshot(ulong seq, CarrierOperationsSnapshot carrier) =>
        new(seq, 0, SimulationSpeed.Speed0, [], CarrierOperations: carrier);

    private static CarrierOperationsSnapshot Carrier(int revision, int ammo) =>
        new("SPC-0001", "ship.tetrarch", "Tetrarch", revision, 400, 400, false,
            OwnedDrones:
            [
                new OwnedDroneSnapshot("DRN-PLAYER-01", "SPC-0001", "BAY-01", 0, "CHR-PILOT-1",
                    "heavy.sword.shield", "gun.sword.t1", "plate.medium",
                    AmmoCurrent: ammo, AmmoCapacity: 6),
            ]);

    [Fact]
    public void OutOfOrderSnapshotDoesNotUndoReload()
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(Snapshot(2, Carrier(revision: 2, ammo: 4))); // post-rearm
        buffer.Update(Snapshot(1, Carrier(revision: 1, ammo: 1))); // stale, lower sequence

        Assert.Equal(2, buffer.Presented!.Snapshot.CarrierOperations!.Revision);
        Assert.Equal(4, buffer.Presented.Snapshot.CarrierOperations.OwnedDrones.Single(d => d.Id == "DRN-PLAYER-01").AmmoCurrent);
    }

    [Fact]
    public async Task LostResponseRetryKeepsIdentity()
    {
        await using var connection = new RecordingConnection();
        await using var handle = new GameSessionHandle(connection);

        var request = new RearmDroneRequest("cmd-1", "SPC-0001", "DRN-PLAYER-01", 3, 0);
        await handle.RearmDroneAsync(request);
        await handle.RearmDroneAsync(request); // retry with the same command id

        Assert.Equal("cmd-1", connection.LastRearm!.CommandId);
        Assert.Equal(2, connection.RearmCalls);
        Assert.False(handle.IsRearmPending("cmd-1"));
    }

    [Fact]
    public async Task SwitchSessionClearsPending()
    {
        await using var connection = new RecordingConnection();
        await using var handle = new GameSessionHandle(connection);

        Assert.False(handle.IsRearmPending("cmd-1"));

        // A fresh session handle carries no pending state from a previous one.
        await using var otherConnection = new RecordingConnection();
        await using var other = new GameSessionHandle(otherConnection);
        Assert.False(other.IsRearmPending("cmd-1"));
    }

    private sealed class RecordingConnection : IGameSessionConnection
    {
        public RearmDroneRequest? LastRearm;
        public int RearmCalls;

        public ValueTask<CarrierCommandResponse> RearmDroneAsync(RearmDroneRequest request, CancellationToken cancellationToken = default)
        {
            RearmCalls++;
            LastRearm = request;
            return ValueTask.FromResult(new CarrierCommandResponse(true, Revision: 1, Transferred: request.Quantity));
        }

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
