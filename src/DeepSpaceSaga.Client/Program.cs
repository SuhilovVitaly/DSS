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
        // The only place that knows about Saves/quicksave.json on disk (mirrors Settings.json).
        private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "Settings.json");
        private static string SavePath => Path.Combine(AppContext.BaseDirectory, "Saves", "quicksave.json");

        public IGameSessionConnection CreateSession()
        {
            return LocalGameSessionConnection.CreateFromSettingsFile(SettingsPath, SavePath);
        }

        public IGameSessionConnection CreateSessionFromSave()
        {
            return LocalGameSessionConnection.CreateFromSaveFile(SettingsPath, SavePath);
        }

        public bool HasQuickSave()
        {
            return File.Exists(SavePath);
        }
    }
}
