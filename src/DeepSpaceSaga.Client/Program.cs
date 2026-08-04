using DeepSpaceSaga.Contracts;
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
            string settingsPath = Path.Combine(AppContext.BaseDirectory, "Settings.json");
            return LocalGameSessionConnection.CreateFromSettingsFile(settingsPath);
        }
    }
}
