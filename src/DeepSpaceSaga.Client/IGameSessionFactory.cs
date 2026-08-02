using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client;

public interface IGameSessionFactory
{
    IGameSessionConnection CreateSession();
}
