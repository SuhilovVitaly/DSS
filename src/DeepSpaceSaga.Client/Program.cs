using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
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

            // Load the default scenario
            string scenarioPath = Path.Combine(
                AppContext.BaseDirectory, "Scenarios", "Default", "scenario.json");

            var scenario = ScenarioLoader.LoadFromFile(scenarioPath);
            engine.LoadScenario(scenario);

            return new LocalGameSessionConnection(engine);
        }
    }
}
