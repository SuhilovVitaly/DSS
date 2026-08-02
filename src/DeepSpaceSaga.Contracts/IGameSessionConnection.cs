namespace DeepSpaceSaga.Contracts;

public sealed record Command;

public sealed record AuthoritativeSnapshot;

public interface IGameSessionConnection
{
    void SendCommand(Command command);

    event Action<AuthoritativeSnapshot>? SnapshotReceived;
}
