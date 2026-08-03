using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client;

/// <summary>
/// Owns the lifetime of a game session.
/// Starts a background receive loop that reads snapshots from the connection
/// and feeds them into the SnapshotBuffer.
/// Tracks authoritative simulation speed for immediate client access
/// (without waiting for the next 1 Hz snapshot).
/// </summary>
public sealed class GameSessionHandle : IAsyncDisposable
{
    private readonly IGameSessionConnection _connection;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _receiveTask;
    private bool _disposed;

    /// <summary>
    /// Client-side authoritative simulation speed.
    /// Updated immediately after SetSimulationSpeedAsync completes,
    /// so the renderer does not need to wait for the next snapshot.
    /// </summary>
    public SimulationSpeed CurrentSpeed { get; private set; } = SimulationSpeed.Speed1;

    public GameSessionHandle(IGameSessionConnection connection)
    {
        _connection = connection;

        Buffer = new SnapshotBuffer();

        // Background loop: connection → buffer
        _receiveTask = Task.Run(() => ReceiveLoopAsync(_cts.Token));
    }

    public IGameSessionConnection Connection => _connection;
    public SnapshotBuffer Buffer { get; }

    /// <summary>
    /// Update the client-side speed tracker immediately after a confirmed
    /// speed change. For local connections this is synchronous and instant.
    /// </summary>
    public void UpdateSpeed(SimulationSpeed speed)
    {
        CurrentSpeed = speed;
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
