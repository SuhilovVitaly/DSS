using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Dialogue;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SkiaSharp;

public static class V2NativeMapAcceptance
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private static object? Field(object target, string name) => target.GetType().GetField(name, Private)!.GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private)!.SetValue(target, value);
    public static string CreateOwnedSlots(string parent)
    {
        string path = Path.Combine(Path.GetFullPath(parent), "native-slots-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path); File.WriteAllText(Path.Combine(path, ".ep0018-owned"), Path.GetFileName(path)); return path;
    }
    public static void CleanupOwnedSlots(string parent, string slots)
    {
        string path = Path.GetFullPath(slots), root = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string marker = Path.Combine(path, ".ep0018-owned");
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || Path.GetDirectoryName(path) + Path.DirectorySeparatorChar != root || !Path.GetFileName(path).StartsWith("native-slots-", StringComparison.Ordinal) ||
            !File.Exists(marker) || File.ReadAllText(marker) != Path.GetFileName(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException("Refusing cleanup outside the owned slot directory.");
        Directory.Delete(path, true);
    }

    public static bool RouteComplete(string directory, out string error)
    {
        try
        {
            AuthoritativeSnapshot Read(string phase) => JsonSerializer.Deserialize<AuthoritativeSnapshot>(File.ReadAllText(Path.Combine(directory, phase + ".json")))!;
            var start = Read("start"); var approach = Read("approach"); var dock = Read("docked"); var loaded = Read("loaded"); var returned = Read("returned");
            ObjectMotionSnapshot Player(AuthoritativeSnapshot s) => s.Objects.Single(o => o.ObjectId == s.PlayerShipObjectId);
            if (Player(start).IsDocked || !Player(dock).IsDocked || !Player(loaded).IsDocked || !Player(returned).IsDocked ||
                Player(approach).NavigationTargetObjectId is null || Player(dock).DockedStationObjectId is null || Player(dock).DockedStationObjectId != Player(loaded).DockedStationObjectId ||
                start.SolarSystemMap?.GeneratorVersion != 2 || start.ClusterMap?.RulesVersion != 2 || start.ClusterMap.Geography is null || loaded.SolarSystemMap?.GeneratorVersion != 2 || loaded.ClusterMap?.RulesVersion != 2 || start.SolarSystemMap.Seed != loaded.SolarSystemMap.Seed || JsonSerializer.Serialize(dock.ClusterMap) != JsonSerializer.Serialize(loaded.ClusterMap) ||
                Player(dock).X != Player(loaded).X || Player(dock).Y != Player(loaded).Y || returned.CurrentSpeed != SimulationSpeed.Speed0)
                throw new InvalidDataException("Route lacks observed Approach, dock, materialized load or paused return.");
            error = ""; return true;
        }
        catch (Exception ex) { error = ex.Message; return false; }
    }
    public static bool AcceptanceComplete(bool route, string human, string physical, bool nativeFps, bool baseline) => route && human == "passed" && physical == "passed" && nativeFps && baseline;

    public static bool ValidateReport(string path, out string error)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path)); var r = document.RootElement;
            if (r.GetProperty("schemaVersion").GetInt32() != 2 || r.GetProperty("status").GetString() != "scripted-route-complete" || !r.GetProperty("nativeOpened").GetBoolean() || !r.GetProperty("route").GetBoolean() || !r.GetProperty("slotCleanup").GetBoolean()) throw new InvalidDataException("Native route did not complete.");
            string[] requiredChecks = ["initialLocal", "systemDetail", "stationSelection", "stationLocal", "pan", "wheelRegionObserved", "resizeObserved", "fastSpeedObserved", "dockObserved", "savePaused", "loadGeometry", "returnFollow"];
            foreach (string check in requiredChecks) if (!r.GetProperty("checks").GetProperty(check).GetBoolean()) throw new InvalidDataException("Missing observed check: " + check);
            string[] phases = ["start", "system", "cluster", "station-local", "approach", "docked-running", "docked", "loaded", "returned"];
            var captured = new HashSet<string>(); string? directory = null;
            foreach (var c in r.GetProperty("captures").EnumerateArray())
            {
                if (!captured.Add(c.GetProperty("phase").GetString()!)) throw new InvalidDataException("Duplicate capture.");
                string image = c.GetProperty("image").GetString()!, data = c.GetProperty("snapshot").GetString()!;
                directory = Path.GetDirectoryName(data);
                if (V2MapAcceptance.Digest(image) != c.GetProperty("imageSha256").GetString() || V2MapAcceptance.Digest(data) != c.GetProperty("snapshotSha256").GetString()) throw new InvalidDataException("Native image/state evidence missing or changed.");
                using var codec = SKCodec.Create(image);
                if (codec is null || codec.Info.Width != c.GetProperty("width").GetInt32() || codec.Info.Height != c.GetProperty("height").GetInt32() || c.GetProperty("camera").GetProperty("ppu").GetDouble() <= 0 || c.GetProperty("camera").GetProperty("uiScale").GetSingle() != r.GetProperty("scale").GetSingle()) throw new InvalidDataException("Native capture dimensions/camera inconsistent.");
                var snapshot = JsonSerializer.Deserialize<AuthoritativeSnapshot>(File.ReadAllText(data))!;
                if (snapshot.SolarSystemMap?.GeneratorVersion != 2 || snapshot.SolarSystemMap.Seed != r.GetProperty("seed").GetUInt64()) throw new InvalidDataException("Native capture seed/version inconsistent.");
            }
            if (!captured.SetEquals(phases) || directory is null || !RouteComplete(directory, out string routeError)) throw new InvalidDataException("Incomplete route captures.");
            string frame = r.GetProperty("nativeFrames").GetString()!;
            if (V2MapAcceptance.Digest(frame) != r.GetProperty("nativeFrameSha256").GetString()) throw new InvalidDataException("Native frame evidence missing or changed.");
            using var frameDoc = JsonDocument.Parse(File.ReadAllText(frame)); var f = frameDoc.RootElement;
            if (f.GetProperty("backend").GetString()?.Contains("OpenGL", StringComparison.Ordinal) != true || f.GetProperty("warmupFrames").GetInt32() != 120 || f.GetProperty("measuredFrames").GetInt32() != 600 || f.GetProperty("intervalsMs").GetArrayLength() != 600 || f.GetProperty("context").GetProperty("GeneratorVersion").GetInt32() != 2) throw new InvalidDataException("Actual native frame samples required.");
            if (r.GetProperty("productAccepted").GetBoolean() && (r.GetProperty("humanAcceptance").GetString() != "passed" || r.GetProperty("physicalInput").GetString() != "passed" || r.GetProperty("baseline").GetString() != "passed" || r.GetProperty("fpsStatus").GetString() != "passed-p95")) throw new InvalidDataException("Scripted route cannot imply product acceptance.");
            error = ""; return true;
        }
        catch (Exception ex) { error = ex.Message; return false; }
    }

    public static int Run(string[] args)
    {
        string original = Directory.GetCurrentDirectory(); string? output = null, owned = null;
        string? failure = null; bool nativeOpened = false, route = false, cleaned = false;
        var inputs = new List<object>(); var captures = new List<object>(); var checks = new Dictionary<string, bool>();
        var watch = Stopwatch.StartNew(); int stage = 0, frame = 0; ulong seed = 1; string size = "1920x1080"; float scale = 1;
        string? framePath = null, evidence = null, settings = null; object? machine = null; string? commit = null;
        GameSessionHandle? initialHandle = null;
        try
        {
            if (args.Length < 3 || !args.Contains("--map-v2-native-acceptance")) throw new ArgumentException("Expected root output.json --map-v2-native-acceptance [--seed N --viewport WxH --ui-scale N].");
            string root = Path.GetFullPath(args[0]); output = Path.GetFullPath(args[1]);
            string allowed = Path.GetFullPath(Path.Combine(root, "../artifacts/validation")) + Path.DirectorySeparatorChar;
            if (!Path.IsPathFullyQualified(args[1]) || !output.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Output outside workspace validation root.");
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            var seen = new HashSet<string>();
            for (int i = 2; i < args.Length; i++)
            {
                string key = args[i]; if (!seen.Add(key)) throw new ArgumentException("Duplicate flag.");
                if (key == "--map-v2-native-acceptance") continue;
                if (key is not ("--seed" or "--viewport" or "--ui-scale") || ++i >= args.Length) throw new ArgumentException("Unknown or missing option: " + key);
                if (key == "--seed") seed = ulong.Parse(args[i]);
                if (key == "--viewport") size = args[i];
                if (key == "--ui-scale") scale = float.Parse(args[i], CultureInfo.InvariantCulture);
            }
            var dimensions = size.Split('x');
            if (dimensions.Length != 2 || !int.TryParse(dimensions[0], out int width) || !int.TryParse(dimensions[1], out int height) || width is < 1280 or > 5120 || height is < 720 or > 2160 || scale is not (1f or 1.5f or 2f)) throw new ArgumentException("Unsupported viewport/scale.");
            evidence = Path.Combine(Path.GetDirectoryName(output)!, Path.GetFileNameWithoutExtension(output) + "-evidence");
            if (Directory.Exists(evidence)) throw new ArgumentException("Use a new evidence output.");
            Directory.CreateDirectory(evidence); owned = CreateOwnedSlots(evidence);
            framePath = Path.Combine(evidence, "native-frames.json");
            string client = Path.Combine(root, "src/DeepSpaceSaga.Client"); Directory.SetCurrentDirectory(client);
            settings = Path.Combine(client, "Settings.json");
            var registry = EngineContentLoader.LoadRegistryFromSettingsFile(settings, out _, out _);
            var config = EngineContentLoader.LoadSolarSystemGenerationConfig(settings)!;
            var source = ScenarioLoader.LoadFromFile(Path.Combine(client, "Scenarios/Default/scenario.json"));
            var engine = new SimulationEngine(registry);
            engine.ConfigureStationResourceFields(JsonSerializer.Deserialize<StationResourceFieldConfig>(File.ReadAllText(Path.Combine(client, "Data/World/station-resource-fields.json")))!);
            engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = seed, CurrentSpeed = "Speed0" } }, generation: config);
            var snapshot = engine.CaptureSnapshot();
            var buffer = new SnapshotBuffer(); buffer.Update(snapshot);
            initialHandle = new(new LocalGameSessionConnection(engine, owned), buffer, new DebugTuningFileWriter(Path.Combine(owned, "DebugTuning")));
            var mapSettings = TacticalMapSettings.Load(settings);
            var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), initialHandle, uiScale: scale, mapSettings: mapSettings);
            var factory = new Factory(settings, owned);
            commit = SolarMapEvidence.Revision(root); machine = SolarMapEvidence.Machine();
            Environment.SetEnvironmentVariable("DSS_MAP_FRAME_REPORT", framePath);
            Environment.SetEnvironmentVariable("DSS_MAP_FRAME_COMMIT", commit);
            Environment.SetEnvironmentVariable("DSS_MAP_FRAME_CASE", $"EP0018 Default seed={seed} {size} UI={scale}; scripted production route");
            Environment.SetEnvironmentVariable("DSS_MAP_FRAME_WINDOW", size);
            Environment.SetEnvironmentVariable("DSS_MAP_FRAME_EXIT", "0");
            using var window = new SkiaWindow(screen, factory, watch);
            Set(window, "_session", initialHandle); Set(window, "_gameSessionScreen", screen);
            var native = (IWindow)Field(window, "_window")!;
            Task? pending = null; int entered = 0; string station = "SPC-0002"; string? geometry = null;
            GameSessionHandle lastHandle = initialHandle;
            GameSessionHandle Session() => Field(window, "_session") as GameSessionHandle ?? lastHandle;
            void Input(string action) => inputs.Add(new { frame, elapsedMs = watch.Elapsed.TotalMilliseconds, action });
            void Event(ScreenEvent action) { Input("production event " + action); pending = (Task)typeof(SkiaWindow).GetMethod("HandleScreenEvent", Private)!.Invoke(window, [action, null])!; }
            void Click(SKRect r, bool raw = false)
            {
                float factor = raw ? 1 : screen.UiScale;
                if (r.IsEmpty || r.MidX * factor < 0 || r.MidX * factor >= native.Size.X || r.MidY * factor < 0 || r.MidY * factor >= native.Size.Y)
                    throw new InvalidDataException("Requested UI target is outside the native window.");
                Input($"screen mouse {r.MidX * factor},{r.MidY * factor}");
                var result = screen.OnMouseDown(r.MidX * factor, r.MidY * factor);
                screen.OnMouseUp(r.MidX * factor, r.MidY * factor);
                if (result != ScreenEvent.None) Event(result);
            }
            bool Command(string type)
            {
                var buttons = screen.CommandsPanel.AllCommandButtons.Where(b => b.CommandTypeId == type && b.Enabled).ToArray();
                if (buttons.Length == 0) return false;
                Click(buttons[0].Rect); Input("command button " + type); return true;
            }
            void Next() { stage++; entered = frame; Console.WriteLine($"stage={stage} frame={frame} elapsed={watch.Elapsed.TotalSeconds:F2}"); }
            void Capture(SKSurface surface, string name, AuthoritativeSnapshot world)
            {
                string image = Path.Combine(evidence, name + ".png"), data = Path.Combine(evidence, name + ".json");
                using (var img = surface.Snapshot()) using (var encoded = img.Encode(SKEncodedImageFormat.Png, 100)) using (var file = File.Create(image)) encoded.SaveTo(file);
                File.WriteAllText(data, JsonSerializer.Serialize(world, Json));
                captures.Add(new
                {
                    phase = name,
                    image,
                    imageSha256 = V2MapAcceptance.Digest(image),
                    snapshot = data,
                    snapshotSha256 = V2MapAcceptance.Digest(data),
                    camera = new { x = screen.CameraFocusX, y = screen.CameraFocusY, ppu = screen.CameraPixelsPerWorldUnit, detail = screen.MapDetail.ToString(), uiScale = screen.UiScale },
                    width = surface.Canvas.DeviceClipBounds.Width,
                    height = surface.Canvas.DeviceClipBounds.Height
                });
            }
            window.NativeFrameRendered = (surface, current) =>
            {
                try
                {
                    frame++; nativeOpened = true;
                    if (watch.Elapsed.TotalSeconds > 40) throw new TimeoutException("Native route internal40s cap.");
                    if (pending is { IsCompleted: false } || frame - entered < 4) return;
                    pending?.GetAwaiter().GetResult(); pending = null;
                    screen = (GameSessionScreen)Field(window, "_gameSessionScreen")!;
                    lastHandle = Session();
                    if (screen.UiScale != scale) { screen.SetUiScale(scale); entered = frame; return; }
                    var s = Session().Buffer.Latest?.Snapshot; if (s is null) return;
                    var player = s.Objects.Single(o => o.ObjectId == s.PlayerShipObjectId);
                    if (current is DialogueScreen dialogue && !dialogue.IsPending)
                    {
                        var choices = dialogue.State.Choices;
                        int choice = -1;
                        for (int i = 0; i < choices.Length; i++)
                            if (choices[i].Enabled && choices[i].ChoiceId is "truthful_id" or "accept_fee" or "continue") { choice = i; break; }
                        if (choice >= 0)
                        {
                            Input("dialogue choice " + choices[choice].ChoiceId);
                            var rect = DialogueLayout.Choice(choice, width, height);
                            var result = dialogue.OnMouseDown(rect.MidX, rect.MidY, MouseButton.Left);
                            if (result != ScreenEvent.None) Event(result);
                            entered = frame;
                        }
                        return;
                    }
                    switch (stage)
                    {
                        case 0:
                            Capture(surface, "start", s); checks["initialLocal"] = screen.MapDetail == MapDetailLevel.Local;
                            Click(screen.LastCloseRect); Click(screen.CommandsPanel.HideShowButtonRect); Click(screen.ObjectInfoPanel.HideShowButtonRect);
                            screen.OnKeyDown(Key.End); Input("End/system; overlay panels collapsed"); Next(); break;
                        case 1:
                            Capture(surface, "system", s); checks["systemDetail"] = screen.MapDetail == MapDetailLevel.System;
                            Click(screen.MapViewButtonRects[7]); Next(); break;
                        case 2:
                            Capture(surface, "cluster", s); screen.FocusStation(station); Input("production FocusStation"); Next(); break;
                        case 3:
                            if (!screen.MapLabels.TryGetValue(station, out var plaque)) break;
                            Click(new(plaque.PlaqueRect.Right - 8, plaque.PlaqueRect.Top, plaque.PlaqueRect.Right - 2, plaque.PlaqueRect.Bottom), raw: true); Next(); break;
                        case 4:
                            if (screen.SelectedObjectId != station)
                            {
                                // Marker hits intentionally precede plaques. Exercise the real repeat-click
                                // cycle when a ship/resource occupies the station's label or marker.
                                var pose = screen.RenderStates.Single(o => o.Pose.ObjectId == station).Pose;
                                float sx = (float)(native.Size.X / 2d + (pose.X - screen.CameraFocusX) * screen.CameraPixelsPerWorldUnit);
                                float sy = (float)(native.Size.Y / 2d + (pose.Y - screen.CameraFocusY) * screen.CameraPixelsPerWorldUnit);
                                Click(new(sx - 1, sy - 1, sx + 1, sy + 1), raw: true); entered = frame; break;
                            }
                            if (s.SelectedObjectId != station) break;
                            checks["stationSelection"] = true; Click(screen.MapViewButtonRects[11]); Next(); break;
                        case 5:
                            Capture(surface, "station-local", s); checks["stationLocal"] = screen.MapDetail == MapDetailLevel.Local;
                            var free = screen.AvailableMapRect(); double x = screen.CameraFocusX;
                            screen.OnMouseDown(free.Left + 20, free.MidY); screen.OnMouseMove(free.Left + 50, free.MidY + 20); screen.OnMouseUp(free.Left + 50, free.MidY + 20);
                            checks["pan"] = screen.CameraFocusX != x; Input("managed screen drag"); screen.FocusStation(station);
                            double units = s.ClusterMap!.Geography!.ReferenceSpeedKmS * 86400 / 300 * 10;
                            double days = Math.Min(width, height) / screen.CameraPixelsPerWorldUnit / units;
                            float wheel = (float)(-Math.Ceiling(Math.Log(.5 / days, mapSettings.WheelFactor)) * mapSettings.WheelUnitsPerStep);
                            screen.OnMouseWheel(free.MidX, free.MidY, wheel); Input("screen wheel to Region: " + wheel); Next(); break;
                        case 6:
                            if (screen.IsZoomAnimating) break;
                            checks["wheelRegionObserved"] = screen.MapDetail == MapDetailLevel.Region;
                            native.Size = new Vector2D<int>(width - 100, height - 80); Input("native resize smaller"); Next(); break;
                        case 7:
                            checks["resizeObserved"] = surface.Canvas.DeviceClipBounds.Width == width - 100;
                            native.Size = new Vector2D<int>(width, height); Input("native resize restore"); screen.FocusStation(station);
                            Click(screen.CommandsPanel.HideShowButtonRect); Next(); break;
                        case 8:
                            if (!Command(NavigationComputerCommandTypes.Approach)) break;
                            Click(screen.SpeedButtonRects[1]); Next(); break;
                        case 9:
                            if (player.NavigationTargetObjectId != station) break;
                            Capture(surface, "approach", s); Click(screen.SpeedButtonRects[^1]); Next(); break;
                        case 10:
                            if (s.CurrentSpeed != SimulationSpeed.Speed4) break;
                            checks["fastSpeedObserved"] = s.CurrentSpeed == SimulationSpeed.Speed4;
                            if (!Command(ShipEngineCommandTypes.SpeedSynchronization)) break;
                            Next(); break;
                        case 11:
                            if (Math.Abs(player.SpeedKmS - s.Objects.Single(o => o.ObjectId == station).SpeedKmS) > 1e-6) break;
                            if (!Command(ShipEngineCommandTypes.DirectionSynchronization)) break;
                            Next(); break;
                        case 12:
                            var target = s.Objects.Single(o => o.ObjectId == station);
                            if (Math.Abs(player.SpeedKmS - target.SpeedKmS) > 1e-6 || Math.Abs(player.Direction - target.Direction) > .01) break;
                            if (!Command(NavigationComputerCommandTypes.Dock)) break;
                            Next(); break;
                        case 13:
                            if (!player.IsDocked) break;
                            Capture(surface, "docked-running", s); checks["dockObserved"] = player.DockedStationObjectId == station;
                            Event(ScreenEvent.CloseStation); Next(); break;
                        case 14:
                            Event(ScreenEvent.QuickSave); Next(); break;
                        case 15:
                            if (!factory.HasQuickSave() || s.CurrentSpeed != SimulationSpeed.Speed0) break;
                            Capture(surface, "docked", s); checks["savePaused"] = true; geometry = JsonSerializer.Serialize(s.ClusterMap);
                            Event(ScreenEvent.QuickLoad); Next(); break;
                        case 16:
                            if (factory.LoadCalls == 0 || ReferenceEquals(Session(), initialHandle) || s.CurrentSpeed != SimulationSpeed.Speed0 || !player.IsDocked) break;
                            Capture(surface, "loaded", s); checks["loadGeometry"] = geometry == JsonSerializer.Serialize(s.ClusterMap);
                            Event(ScreenEvent.CloseStation); Next(); break;
                        case 17:
                            screen.OnKeyDown(Key.End); Input("End after Load"); Next(); break;
                        case 18:
                            screen.FocusStation(station); screen.OnKeyDown(Key.Home); Input("Home return/follow"); Next(); break;
                        case 19:
                            Capture(surface, "returned", s); checks["returnFollow"] = screen.IsFocusAttachedToPlayer;
                            route = RouteComplete(evidence, out string routeError); if (!route) throw new InvalidDataException(routeError);
                            Next(); break;
                        default:
                            if (File.Exists(framePath)) native.Close(); break;
                    }
                }
                catch (Exception ex) { failure = ex.ToString(); native.Close(); }
            };
            try { window.Run(); }
            finally { Session().DisposeAsync().AsTask().GetAwaiter().GetResult(); }
        }
        catch (Exception ex) { failure = ex.ToString(); }
        finally
        {
            initialHandle?.DisposeAsync().AsTask().GetAwaiter().GetResult();
            if (owned is not null) { CleanupOwnedSlots(evidence!, owned); cleaned = !Directory.Exists(owned); }
            Directory.SetCurrentDirectory(original);
            foreach (string key in new[] { "REPORT", "COMMIT", "CASE", "WINDOW", "EXIT" }) Environment.SetEnvironmentVariable("DSS_MAP_FRAME_" + key, null);
        }
        bool passed = route && failure is null && cleaned && checks.Count >= 8 && checks.Values.All(v => v);
        if (output is not null && evidence is not null)
        {
            JsonNode? frames = framePath is not null && File.Exists(framePath) ? JsonNode.Parse(File.ReadAllText(framePath)) : null;
            double? p95 = frames?["frameIntervals"]?["P95Ms"]?.GetValue<double>();
            bool displayLimited = frames?["targetVerdict"]?.GetValue<string>() == "display-limited";
            File.WriteAllText(output, JsonSerializer.Serialize(new
            {
                schemaVersion = 2,
                status = passed ? "scripted-route-complete" : nativeOpened ? "failed" : "native-unavailable",
                seed,
                size,
                scale,
                commit,
                machine,
                sourceAssemblySha256 = V2MapAcceptance.Digest(typeof(V2NativeMapAcceptance).Assembly.Location),
                settingsSha256 = settings is not null ? V2MapAcceptance.Digest(settings) : null,
                frame,
                stage,
                elapsedSeconds = watch.Elapsed.TotalSeconds,
                nativeOpened,
                route,
                checks,
                inputs,
                captures,
                slotCleanup = cleaned,
                failure,
                nativeFrames = framePath,
                nativeFrameSha256 = frames is not null ? V2MapAcceptance.Digest(framePath!) : null,
                p95TargetMs = 12.5,
                targetFps = 80,
                fpsStatus = frames is null ? "NOT RUN" : displayLimited ? "display-limited" : p95 <= 12.5 ? "passed-p95" : "failed-p95",
                input = "scripted screen handlers and production event/session factory paths; native OpenGL surface and real window resize; no physical input injection",
                humanAcceptance = "NOT RUN",
                physicalInput = "NOT RUN",
                baseline = "OPEN",
                productAccepted = AcceptanceComplete(passed, "NOT RUN", "NOT RUN", p95 <= 12.5 && !displayLimited, false)
            }, Json));
        }
        Console.WriteLine(output); Console.WriteLine(passed ? "Scripted route complete" : failure);
        return passed ? 0 : 1;
    }

    private sealed class Factory(string settings, string slots) : IGameSessionFactory
    {
        public int LoadCalls { get; private set; }
        public IGameSessionConnection CreateSession() => throw new NotSupportedException();
        public IGameSessionConnection CreateSessionFromScenario(string path) => throw new NotSupportedException();
        public IGameSessionConnection CreateSessionFromSave(string slot)
        {
            if (slot != SaveSlots.Quicksave) throw new ArgumentException("Only owned quicksave allowed.");
            LoadCalls++; return LocalGameSessionConnection.CreateFromSaveFile(settings, Path.Combine(slots, SaveSlotNaming.ToFileName(slot)), slots, Path.Combine(slots, "DebugTuning/debug-tuning.json"));
        }
        public bool HasQuickSave() => File.Exists(Path.Combine(slots, SaveSlotNaming.ToFileName(SaveSlots.Quicksave)));
        public SaveSlotInfo[] ListSaveSlots() => [];
        public ScenarioInfo[] ListScenarios() => [];
        public void DeleteSaveSlot(string slot) => throw new NotSupportedException("Cleanup is owned-directory only.");
    }
}
