using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client;

/// <summary>
/// Owns the lifetime of a game session.
/// Starts a background receive loop that reads snapshots from the connection
/// and feeds them into the SnapshotBuffer.
/// </summary>
public sealed class GameSessionHandle : IAsyncDisposable
{
    private readonly IGameSessionConnection _connection;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _receiveTask;
    private long _nextClientSequence;
    private bool _disposed;

    public GameSessionHandle(IGameSessionConnection connection)
    {
        _connection = connection;

        Buffer = new SnapshotBuffer();

        // Background loop: connection → buffer
        _receiveTask = Task.Run(() => ReceiveLoopAsync(_cts.Token));
    }

    public IGameSessionConnection Connection => _connection;
    public SnapshotBuffer Buffer { get; }

    public ValueTask SendEngineCommandAsync(
        string objectId,
        string moduleId,
        string commandType,
        CancellationToken cancellationToken = default)
    {
        ulong sequence = (ulong)Interlocked.Increment(ref _nextClientSequence);
        var command = new PlayerCommand(
            CommandId: $"CMD-{sequence:D8}-{Guid.NewGuid():N}",
            ClientSequence: sequence,
            ObjectId: objectId,
            ModuleId: moduleId,
            CommandType: commandType);

        return _connection.SendCommandAsync(command, cancellationToken);
    }

    /// <summary>
    /// Set the simulation speed and immediately update the client-side tracker.
    /// Properly awaits the connection call — for local connections this completes
    /// synchronously; for network connections the speed is confirmed before the
    /// client-side state is updated.
    /// </summary>
    public async ValueTask SetSpeedAsync(SimulationSpeed speed)
    {
        await _connection.SetSimulationSpeedAsync(speed);
        Buffer.CurrentSpeed = speed;
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var snapshot in _connection.ReadSnapshotsAsync(ct))
            {
                Buffer.Update(snapshot);
            }
        }
        catch (OperationCanceledException) { }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        _cts.Cancel();

        try { await _receiveTask; } catch { }

        _cts.Dispose();

        if (_connection is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
    }
}
