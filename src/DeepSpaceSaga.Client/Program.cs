using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.MainMenu;

namespace DeepSpaceSaga.Client;

public static class Program
{
    public static void Main()
    {
        var factory = new LocalGameSessionFactory();
        var mainMenu = new MainMenuScreen();

        using var window = new SkiaWindow(mainMenu, factory);
        window.Run();
    }

    private sealed class LocalGameSessionFactory : IGameSessionFactory
    {
        public IGameSessionConnection CreateSession()
        {
            var engine = new SimulationEngine();

            // Minimal test object for the render/prediction pipeline demo.
            // 1 unit = 100 m, Sun at (0, 0).
            engine.AddTestObject(new ObjectMotionSnapshot(
                ObjectId: "probe-1",
                X: 500,
                Y: 300,
                SpeedKmS: 5,     // 5 km/s
                Direction: 90    // 90° = right (clockwise from up)
            ));

            return new LocalGameSessionConnection(engine);
        }
    }
}
