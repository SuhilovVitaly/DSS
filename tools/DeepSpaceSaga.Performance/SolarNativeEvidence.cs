using System.Globalization;
using System.Text.Json;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

internal static class SolarNativeEvidence
{
    internal static int Run(string[] args)
    {
        if (args.Length < 7) throw new ArgumentException("Usage: <root> <output.json> --solar-window min|max system|belt|selected 1|1.2|1.5 1280x720|1920x1080");
        string root = Path.GetFullPath(args[0]), output = Path.GetFullPath(args[1]), mode = args[3], view = args[4];
        float scale = float.Parse(args[5], CultureInfo.InvariantCulture);
        if (mode is not ("min" or "max") || view is not ("system" or "belt" or "selected") || scale is not (1f or 1.2f or 1.5f))
            throw new ArgumentException("Unsupported native evidence case.");
        string client = Path.Combine(root, "src/DeepSpaceSaga.Client");
        Directory.SetCurrentDirectory(client);
        string settings = Path.Combine(client, "Settings.json");
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(settings, out _, out _);
        var config = EngineContentLoader.LoadSolarSystemGenerationConfig(settings)!;
        int planets = mode == "min" ? 3 : 7, belts = mode == "min" ? 2 : 5;
        config = config with
        {
            MinPlanets = planets,
            MaxPlanets = planets,
            MinBelts = belts,
            MaxBelts = belts,
            StartMinDays = mode == "min" ? 50 : 75,
            StartMaxDays = mode == "min" ? 50 : 75
        };
        var engine = new SimulationEngine(registry);
        engine.ConfigureStationResourceFields(JsonSerializer.Deserialize<StationResourceFieldConfig>(File.ReadAllText(Path.Combine(client, "Data/World/station-resource-fields.json")))!);
        var source = ScenarioLoader.LoadFromFile(Path.Combine(client, "Scenarios/Default_500/scenario.json"));
        engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = 1, CurrentSpeed = "Speed0" } }, generation: config);
        engine.SetObjectInteractionState(null, "SPC-0002");
        var snapshot = engine.CaptureSnapshot();
        var connection = new LocalGameSessionConnection(engine);
        var handle = new GameSessionHandle(connection);
        handle.Buffer.Update(snapshot);
        var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle, uiScale: scale,
            mapSettings: TacticalMapSettings.Load(settings), combatSettings: CombatVisualSettings.Load(Path.Combine(client, CombatVisualSettings.RelativePath)));
        int frame = 0;
        screen.RenderStageCompleted = stage =>
        {
            if (stage != "begin") return;
            frame++;
            if (frame == 2)
            {
                if (view == "system") screen.FitMapView(MapFitMode.System);
                else if (view == "belt") screen.FitBelt(snapshot.SolarSystemMap!.Belts[0].Id);
                else screen.FitMapView(MapFitMode.Target);
            }
            // Exercise actual UI speed buttons during warmup; measured frames resume.
            if (frame is 20 or 90) ClickSpeed(1);
            if (frame == 60) ClickSpeed(0);
            void ClickSpeed(int index)
            {
                var r = screen.SpeedButtonRects[index];
                screen.OnMouseDown(r.MidX * scale, r.MidY * scale);
                screen.OnMouseUp(r.MidX * scale, r.MidY * scale);
            }
        };
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_REPORT", output);
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_COMMIT", SolarMapEvidence.Revision(root));
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_CASE", $"Default_500 seed=1 config={mode} view={view} UI={scale.ToString(CultureInfo.InvariantCulture)}; scripted native UI acceptance");
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_EXIT", "1");
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_WINDOW", args[6]);
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_IMAGE", Path.ChangeExtension(output, ".png"));
        try
        {
            using var window = new SkiaWindow(screen, new EvidenceFactory(settings), System.Diagnostics.Stopwatch.StartNew());
            window.Run();
            return File.Exists(output) ? 0 : 1;
        }
        finally { handle.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
    }

    private sealed class EvidenceFactory(string settings) : IGameSessionFactory
    {
        public IGameSessionConnection CreateSession() => LocalGameSessionConnection.CreateFromSettingsFile(settings);
        public IGameSessionConnection CreateSessionFromScenario(string path) => LocalGameSessionConnection.CreateFromScenarioFile(settings, path);
        public IGameSessionConnection CreateSessionFromSave(string slotId) => throw new NotSupportedException("No save directory in evidence mode.");
        public ScenarioInfo[] ListScenarios() => ScenarioRepository.ListScenarios(Path.Combine(Path.GetDirectoryName(settings)!, "Scenarios"));
        public bool HasQuickSave() => false;
        public SaveSlotInfo[] ListSaveSlots() => [];
        public void DeleteSaveSlot(string slotId) => throw new NotSupportedException("No save directory in evidence mode.");
    }
}
