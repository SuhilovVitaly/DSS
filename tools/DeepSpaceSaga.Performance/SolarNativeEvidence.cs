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
        if (args.Length < 7) throw new ArgumentException("Usage: <root> <output.json> --solar-window min|max system|belt|selected|cluster 1|1.2|1.5 1280x720|1920x1080");
        string root = Path.GetFullPath(args[0]), output = Path.GetFullPath(args[1]), mode = args[3], view = args[4];
        float scale = float.Parse(args[5], CultureInfo.InvariantCulture);
        if (mode is not ("min" or "max") || view is not ("system" or "belt" or "selected" or "cluster") || scale is not (1f or 1.2f or 1.5f))
            throw new ArgumentException("Unsupported native evidence case.");
        string client = Path.Combine(root, "src/DeepSpaceSaga.Client");
        Directory.SetCurrentDirectory(client);
        string settings = Path.Combine(client, "Settings.json");
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(settings, out _, out _);
        var config = EngineContentLoader.LoadSolarSystemGenerationConfig(settings)!;
        bool clusters = args.Contains("--clusters");
        int planets = mode == "min" ? 3 : 7, belts = mode == "min" ? 2 : 5;
        config = config with
        {
            MinPlanets = planets,
            MaxPlanets = planets,
            MinBelts = belts,
            MaxBelts = belts,
            StartMinDays = mode == "min" ? 50 : 75,
            StartMaxDays = mode == "min" ? 50 : 75,
            Clusters = clusters ? config.Clusters! with { MinClusters = 5, MaxClusters = 5, MinStations = 12, MaxStations = 12 } : null
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
        var stationIds = snapshot.ClusterMap?.Clusters.SelectMany(c => c.StationIds).ToArray() ?? [];
        var selected = new HashSet<string>(StringComparer.Ordinal);
        bool inspectionScrollPassed = false;
        int[]? gcStart = null;
        TimeSpan gcPauseStart = default;
        long allocatedStart = 0;
        screen.RenderStageCompleted = stage =>
        {
            if (stage != "begin") return;
            frame++;
            if (frame == 121)
            {
                gcStart = Enumerable.Range(0, 3).Select(GC.CollectionCount).ToArray();
                gcPauseStart = GC.GetTotalPauseDuration(); allocatedStart = GC.GetTotalAllocatedBytes();
            }
            if (frame == 2)
            {
                if (view == "system") screen.FitMapView(MapFitMode.System);
                else if (view == "belt") screen.FitBelt(snapshot.SolarSystemMap!.Belts[0].Id);
                else if (view == "cluster" && clusters) screen.FitCluster(snapshot.ClusterMap!.StartClusterId);
                else screen.FitMapView(MapFitMode.Target);
            }
            // Exercise actual UI speed buttons during warmup; measured frames resume.
            if (frame is 20 or 90) ClickSpeed(1);
            if (frame == 60) ClickSpeed(0);
            if (frame == 110)
            {
                if (view == "system") screen.FitMapView(MapFitMode.System);
                else if (view == "belt") screen.FitBelt(snapshot.SolarSystemMap!.Belts[0].Id);
                else if (view == "cluster" && clusters) screen.FitCluster(snapshot.ClusterMap!.Stations.Single(s => s.ObjectId == screen.SelectedObjectId).ClusterId);
                else screen.FitMapView(MapFitMode.Target);
            }
            if (clusters && frame is 95 or 100)
            {
                var body = screen.ObjectInfoPanel.RowBodyRects[1];
                double zoom = screen.CameraPixelsPerWorldUnit;
                screen.OnMouseWheel(body.MidX * scale, body.MidY * scale, frame == 95 ? -1 : 1);
                if (frame == 95) inspectionScrollPassed = screen.ObjectInfoPanel.ScrollOffset(1) > 0 && screen.CameraPixelsPerWorldUnit == zoom;
            }
            if (clusters && frame is >= 25 and < 85)
            {
                string id = stationIds[frame - 25];
                var district = snapshot.ClusterMap!.Clusters.Single(c => c.StationIds.Contains(id));
                screen.FitCluster(district.Id);
                var pose = screen.RenderStates.First(s => s.Pose.ObjectId == id).Pose;
                var size = args[6].Split('x'); float width = float.Parse(size[0]), height = float.Parse(size[1]);
                float x = (float)(width / 2 + (pose.X - screen.CameraFocusX) * screen.CameraPixelsPerWorldUnit);
                float y = (float)(height / 2 + (pose.Y - screen.CameraFocusY) * screen.CameraPixelsPerWorldUnit);
                screen.OnMouseDown(x, y); screen.OnMouseUp(x, y);
                if (screen.SelectedObjectId == id) selected.Add(id);
            }
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
            if (File.Exists(output) && clusters)
            {
                var renderGc = new
                {
                    collections = Enumerable.Range(0, 3).Select(i => GC.CollectionCount(i) - (gcStart?[i] ?? 0)).ToArray(),
                    pauseMs = (GC.GetTotalPauseDuration() - gcPauseStart).TotalMilliseconds,
                    allocatedBytes = GC.GetTotalAllocatedBytes() - allocatedStart
                };
                var report = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(output))!;
                report["renderGc"] = JsonSerializer.SerializeToNode(renderGc);
                report["worstCpuFrames"] = JsonSerializer.SerializeToNode(screen.CaptureFrameProfile().Frames.Where(f => f.FrameId > 120)
                    .OrderByDescending(f => f.Window?.CpuCallbackMs ?? f.RenderCpuMs).Take(10));
                report["clusterInteraction"] = JsonSerializer.SerializeToNode(new { requested = stationIds.Length, selected = selected.Count, inspectionScrollPassed, passed = selected.Count == stationIds.Length && inspectionScrollPassed, clusters = 5, stationsPerCluster = 12 });
                File.WriteAllText(output, report.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            }
            return File.Exists(output) && (!clusters || selected.Count == stationIds.Length && inspectionScrollPassed) ? 0 : 1;
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
