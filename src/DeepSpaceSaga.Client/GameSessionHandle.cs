using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client;

/// <summary>
/// Owns the lifetime of a game session.
/// Created on NEW GAME, disposed on MAIN MENU or application exit.
/// </summary>
public sealed class GameSessionHandle : IDisposable
{
    private bool _disposed;

    public GameSessionHandle(IGameSessionConnection connection)
    {
        Connection = connection;
    }

    public IGameSessionConnection Connection { get; }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (Connection is IDisposable disposable)
            disposable.Dispose();
    }
}
