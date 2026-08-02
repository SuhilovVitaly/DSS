using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.LocalClient;

public sealed class LocalGameSessionConnection : IGameSessionConnection, IDisposable
{
    private readonly SimulationEngine _engine;
    private bool _disposed;

    public LocalGameSessionConnection(SimulationEngine engine)
    {
        _engine = engine;
    }

    public void SendCommand(Command command)
    {
        throw new NotImplementedException();
    }

    public event Action<AuthoritativeSnapshot>? SnapshotReceived;

    private void OnSnapshotReceived(AuthoritativeSnapshot snapshot)
    {
        SnapshotReceived?.Invoke(snapshot);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        DisposeEngine();
    }

    private void DisposeEngine()
    {
        (_engine as IDisposable)?.Dispose();
    }
}
