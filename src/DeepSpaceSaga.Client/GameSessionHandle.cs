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
