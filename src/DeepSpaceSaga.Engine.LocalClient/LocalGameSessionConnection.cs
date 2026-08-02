using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.LocalClient;

public sealed class LocalGameSessionConnection : IGameSessionConnection
{
    private readonly SimulationEngine _engine;

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
}
