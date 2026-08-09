using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client;

public interface IGameSessionFactory
{
    IGameSessionConnection CreateSession();

    /// <summary>Bootstrap a new session from the quicksave file (F9). Throws if the file is missing or invalid.</summary>
    IGameSessionConnection CreateSessionFromSave();

    /// <summary>Whether a quicksave file currently exists on disk.</summary>
    bool HasQuickSave();
}
