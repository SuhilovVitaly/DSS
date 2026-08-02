using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.LocalClient;

namespace DeepSpaceSaga.Client;

public static class Program
{
    public static void Main()
    {
        var engine = new SimulationEngine();
        var connection = new LocalGameSessionConnection(engine);
        IGameSessionConnection sessionConnection = connection;

        using var window = new SkiaWindow(sessionConnection);
        window.Run();
    }
}
