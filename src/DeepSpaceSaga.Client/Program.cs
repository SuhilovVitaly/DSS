using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.MainMenu;

namespace DeepSpaceSaga.Client;

public static class Program
{
    public static void Main()
    {
        // TEMP DIAG — startup timing investigation, remove once resolved.
        var startupStopwatch = System.Diagnostics.Stopwatch.StartNew();
        using (var proc = System.Diagnostics.Process.GetCurrentProcess())
        {
            var beforeMain = DateTime.Now - proc.StartTime;
            InterfaceLog.Write(
                $"STARTUP DIAG: OS process start to Main() entry — {beforeMain.TotalMilliseconds:F0} ms (.NET host/runtime init)");
        }

        var factory = new LocalGameSessionFactory();
        var mainMenu = new MainMenuScreen();

        using var window = new SkiaWindow(mainMenu, factory, startupStopwatch);
        window.Run();
    }

    private sealed class LocalGameSessionFactory : IGameSessionFactory
    {
        // The only place that knows about Saves/ on disk (mirrors Settings.json).
        private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "Settings.json");
        private static string SaveDirectory => Path.Combine(AppContext.BaseDirectory, "Saves");
        private static string ScenariosDirectory => Path.Combine(AppContext.BaseDirectory, "Scenarios");
        private static string QuickSavePath =>
            Path.Combine(SaveDirectory, SaveSlotNaming.ToFileName(SaveSlots.Quicksave));

        public IGameSessionConnection CreateSession()
        {
            return LocalGameSessionConnection.CreateFromSettingsFile(SettingsPath, SaveDirectory);
        }

        public IGameSessionConnection CreateSessionFromScenario(string scenarioPath)
        {
            return LocalGameSessionConnection.CreateFromScenarioFile(SettingsPath, scenarioPath, SaveDirectory);
        }

        public ScenarioInfo[] ListScenarios()
        {
            return ScenarioRepository.ListScenarios(ScenariosDirectory);
        }

        public IGameSessionConnection CreateSessionFromSave(string slotId)
        {
            string savePath = Path.Combine(SaveDirectory, SaveSlotNaming.ToFileName(slotId));
            var connection = LocalGameSessionConnection.CreateFromSaveFile(SettingsPath, savePath, SaveDirectory);

            // A missing masterSeed here means a legacy save predating it — the engine
            // already generated and is using a fresh one; this is only about surfacing the
            // warning requirements §15 calls for (New Game's own missing-masterSeed case is
            // expected and never reaches this method).
            if (connection.MasterSeedWasMissingOnLoad)
            {
                InterfaceLog.Write(
                    $"Load: save file '{slotId}' had no masterSeed (legacy save) — generated and will save a new one.");
            }

            return connection;
        }

        public bool HasQuickSave()
        {
            return File.Exists(QuickSavePath);
        }

        public SaveSlotInfo[] ListSaveSlots()
        {
            return SaveSlotRepository.ListSlots(SaveDirectory);
        }

        public void DeleteSaveSlot(string slotId)
        {
            SaveSlotRepository.DeleteSlot(SaveDirectory, slotId);
        }
    }
}
