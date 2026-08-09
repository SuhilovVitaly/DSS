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
            var connection = LocalGameSessionConnection.CreateFromSaveFile(SettingsPath, SavePath);

            // A missing masterSeed here means a legacy save predating it — the engine
            // already generated and is using a fresh one; this is only about surfacing the
            // warning requirements §15 calls for (New Game's own missing-masterSeed case is
            // expected and never reaches this method).
            if (connection.MasterSeedWasMissingOnLoad)
            {
                InterfaceLog.Write(
                    "QuickLoad: save file had no masterSeed (legacy save) — generated and will save a new one.");
            }

            return connection;
        }

        public bool HasQuickSave()
        {
            return File.Exists(SavePath);
        }
    }
}
