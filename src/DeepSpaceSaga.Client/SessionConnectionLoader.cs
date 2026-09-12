using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client;

/// <summary>Owns background-created connections until the window adopts them.</summary>
internal sealed class SessionConnectionLoader : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly HashSet<IGameSessionConnection> _pending = new();
    private bool _closed;

    internal Task<IGameSessionConnection> CreateAsync(Func<IGameSessionConnection> factory) => Task.Run(async () =>
    {
        var connection = factory();
        lock (_gate)
        {
            if (!_closed) { _pending.Add(connection); return connection; }
        }
        await connection.DisposeAsync();
        throw new OperationCanceledException("Window closed during session loading.");
    });

    internal bool TryAdopt(IGameSessionConnection connection)
    {
        lock (_gate) return !_closed && _pending.Remove(connection);
    }

    public async ValueTask DisposeAsync()
    {
        IGameSessionConnection[] pending;
        lock (_gate) { _closed = true; pending = _pending.ToArray(); _pending.Clear(); }
        foreach (var connection in pending) await connection.DisposeAsync().ConfigureAwait(false);
    }
}
