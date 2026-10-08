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
        if (mode is not ("min" or "max") || view is not ("system" or "belt" or "selected" or "cluster" or "base" or "field" or "poi") || scale is not (1f or 1.2f or 1.5f))
            throw new ArgumentException("Unsupported native evidence case.");
        string client = Path.Combine(root, "src/DeepSpaceSaga.Client");
        Directory.SetCurrentDirectory(client);
        string settings = Path.Combine(client, "Settings.json");
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(settings, out _, out _);
        var config = EngineContentLoader.LoadSolarSystemGenerationConfig(settings)!;
        bool clusters = args.Contains("--clusters");
        bool allMapLayers = args.Contains("--all-map-layers");
        if (allMapLayers && !clusters) throw new ArgumentException("--all-map-layers requires --clusters.");
        if (!allMapLayers && view is "base" or "field" or "poi") throw new ArgumentException("Descriptor views require --all-map-layers.");
        int clusterCount = allMapLayers && mode == "min" ? 3 : 5, stationCount = allMapLayers && mode == "min" ? 10 : 12;
        int planets = mode == "min" ? 3 : 7, belts = mode == "min" ? 2 : 5;
        config = config with
        {
            MinPlanets = planets,
            MaxPlanets = planets,
            MinBelts = belts,
            MaxBelts = belts,
            StartMinDays = mode == "min" ? 50 : 75,
            StartMaxDays = mode == "min" ? 50 : 75,
            Clusters = clusters ? config.Clusters! with { MinClusters = clusterCount, MaxClusters = clusterCount, MinStations = stationCount, MaxStations = stationCount } : null,
            Ai = allMapLayers ? config.Ai! with { MinBases = mode == "min" ? 2 : 4, MaxBases = mode == "min" ? 2 : 4 } : null,
            Environment = allMapLayers ? config.Environment : null,
            PoiTemplates = allMapLayers ? config.PoiTemplates : null
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
        bool baseSelected = false, fieldSelected = false, poiSelected = false, systemFitted = false, clusterFitted = false, layersOff = false, layersOn = false;
        bool runningObserved = false, pausedObserved = false, resumedObserved = false, baseScrollPassed = false, diagnosticPanelClosed = false;
        var size = args[6].Split('x'); int width = int.Parse(size[0]), height = int.Parse(size[1]);
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
                if (allMapLayers) systemFitted = screen.FitMapView(MapFitMode.System);
                else if (view == "system") screen.FitMapView(MapFitMode.System);
                else if (view == "belt") screen.FitBelt(snapshot.SolarSystemMap!.Belts[0].Id);
                else if (view == "cluster" && clusters) screen.FitCluster(snapshot.ClusterMap!.StartClusterId);
                else screen.FitMapView(MapFitMode.Target);
            }
            // Exercise actual UI speed buttons during warmup; measured frames resume.
            if (frame is 20 or 90) ClickSpeed(1);
            if (frame == 60) ClickSpeed(0);
            if (frame == 110)
            {
                if (view is "system" or "base" or "field" or "poi") screen.FitMapView(MapFitMode.System);
                else if (view == "belt") screen.FitBelt(snapshot.SolarSystemMap!.Belts[0].Id);
                else if (view == "cluster" && clusters) screen.FitCluster(allMapLayers ? snapshot.ClusterMap!.StartClusterId : snapshot.ClusterMap!.Stations.Single(s => s.ObjectId == screen.SelectedObjectId).ClusterId);
                else screen.FitMapView(MapFitMode.Target);
            }
            if (clusters && !allMapLayers && frame is 95 or 100)
            {
                var body = screen.ObjectInfoPanel.RowBodyRects[1];
                double zoom = screen.CameraPixelsPerWorldUnit;
                screen.OnMouseWheel(body.MidX * scale, body.MidY * scale, frame == 95 ? -1 : 1);
                if (frame == 95) inspectionScrollPassed = screen.ObjectInfoPanel.ScrollOffset(1) > 0 && screen.CameraPixelsPerWorldUnit == zoom;
            }
            if (clusters && !allMapLayers && frame is >= 25 and < 85)
            {
                string id = stationIds[frame - 25];
                var district = snapshot.ClusterMap!.Clusters.Single(c => c.StationIds.Contains(id));
                screen.FitCluster(district.Id);
                var pose = screen.RenderStates.First(s => s.Pose.ObjectId == id).Pose;
                float x = (float)(width / 2 + (pose.X - screen.CameraFocusX) * screen.CameraPixelsPerWorldUnit);
                float y = (float)(height / 2 + (pose.Y - screen.CameraFocusY) * screen.CameraPixelsPerWorldUnit);
                screen.OnMouseDown(x, y); screen.OnMouseUp(x, y);
                if (screen.SelectedObjectId == id) selected.Add(id);
            }
            if (allMapLayers)
            {
                if (frame == 3) { Click(screen.LastCloseRect); diagnosticPanelClosed = !screen.IsPanelVisible; }
                if (frame == 5) clusterFitted = screen.FitCluster(snapshot.ClusterMap!.StartClusterId);
                if (frame == 10) screen.FitMapView(MapFitMode.System);
                if (frame == 50) runningObserved = handle.Buffer.Latest?.Snapshot.CurrentSpeed == SimulationSpeed.Speed1;
                if (frame == 80) pausedObserved = handle.Buffer.Latest?.Snapshot.CurrentSpeed == SimulationSpeed.Speed0;
                if (frame == 105) resumedObserved = handle.Buffer.Latest?.Snapshot.CurrentSpeed == SimulationSpeed.Speed1;
                if (frame is >= 25 and < 35 && !baseSelected) Pick("base");
                if (frame == 34 && baseSelected)
                {
                    var body = screen.ObjectInfoPanel.RowBodyRects[1]; double zoom = screen.CameraPixelsPerWorldUnit;
                    screen.OnMouseWheel(body.MidX * scale, body.MidY * scale, -10);
                    baseScrollPassed = screen.CameraPixelsPerWorldUnit == zoom && (width / scale >= 1100 || screen.ObjectInfoPanel.ScrollOffset(1) > 0);
                    screen.OnMouseWheel(body.MidX * scale, body.MidY * scale, 10);
                }
                if (frame is >= 35 and < 45 && !fieldSelected) Pick("field");
                if (frame is >= 45 and < 85 && !poiSelected) Pick("poi");
                if (frame is 85 or 95)
                {
                    foreach (int index in new[] { 5, 8, 9, 10 }) Click(screen.MapViewButtonRects[index]);
                    if (frame == 85) layersOff = screen.MapLayers == 0;
                    else layersOn = screen.MapLayers == MapLayerFlags.All;
                }
                if (frame is >= 111 and < 119 && view is "base" or "field" or "poi")
                {
                    if (view == "base" && (screen.SelectedObjectId != snapshot.AiMap!.Bases[0].ObjectId || screen.SelectedPoiId is not null || screen.SelectedFieldId is not null) ||
                        view == "field" && screen.SelectedFieldId is null || view == "poi" && screen.SelectedPoiId is null) Pick(view);
                }
                if (frame == 117 && view == "base")
                {
                    var body = screen.ObjectInfoPanel.RowBodyRects[1];
                    for (int i = 0; i < 10; i++) screen.OnMouseWheel(body.MidX * scale, body.MidY * scale, -1);
                }
            }
            void Pick(string kind)
            {
                var current = handle.Buffer.Latest!.Snapshot;
                var poses = screen.RenderStates.Select(s => s.Predicted).ToArray();
                double x, y;
                if (kind == "base") { var p = poses.First(p => p.ObjectId == current.AiMap!.Bases[0].ObjectId); x = p.X; y = p.Y; }
                else if (kind == "field")
                {
                    var p = EnvironmentFieldRenderer.Resolve(current, poses, current.MotionTimeMs).First(f => f.Data.Kind == "Radiation");
                    x = p.X + p.Data.OuterRadius * .5; y = p.Y;
                }
                else { var p = AiMapPresentation.Points(current, poses, current.MotionTimeMs)[0]; x = p.X; y = p.Y; }
                float sx = (float)(width / 2d + (x - screen.CameraFocusX) * screen.CameraPixelsPerWorldUnit);
                float sy = (float)(height / 2d + (y - screen.CameraFocusY) * screen.CameraPixelsPerWorldUnit);
                screen.OnMouseDown(sx, sy); screen.OnMouseUp(sx, sy);
                baseSelected |= screen.SelectedObjectId == current.AiMap!.Bases[0].ObjectId;
                fieldSelected |= screen.SelectedFieldId is not null;
                poiSelected |= screen.SelectedPoiId is not null;
            }
            void Click(SkiaSharp.SKRect r) { screen.OnMouseDown(r.MidX * scale, r.MidY * scale); screen.OnMouseUp(r.MidX * scale, r.MidY * scale); }
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
                report["config"] = JsonSerializer.SerializeToNode(config);
                report["allMapLayers"] = allMapLayers;
                if (allMapLayers) report["fullMapInteraction"] = JsonSerializer.SerializeToNode(new { systemFitted, clusterFitted, baseSelected, fieldSelected, poiSelected, layersOff, layersOn, diagnosticPanelClosed, baseScrollPassed, runningObserved, pausedObserved, resumedObserved, passed = FullMapPassed() });
                else report["clusterInteraction"] = JsonSerializer.SerializeToNode(new { requested = stationIds.Length, selected = selected.Count, inspectionScrollPassed, passed = selected.Count == stationIds.Length && inspectionScrollPassed, clusters = clusterCount, stationsPerCluster = stationCount });
                File.WriteAllText(output, report.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            }
            return File.Exists(output) && (allMapLayers ? FullMapPassed() : !clusters || selected.Count == stationIds.Length && inspectionScrollPassed) ? 0 : 1;
        }
        finally { handle.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
        bool FullMapPassed() => systemFitted && clusterFitted && baseSelected && fieldSelected && poiSelected && layersOff && layersOn &&
            diagnosticPanelClosed && baseScrollPassed && runningObserved && pausedObserved && resumedObserved;
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
