using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Scenario;
using Silk.NET.Input;
using Silk.NET.Windowing;
using SkiaSharp;

internal static class DebugTuningNativeEvidence
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    internal static int Run(string[] args)
    {
        if (args.Length != 5) throw new ArgumentException("<repo> <report.json> --debug-window <scale> <ru|en>");
        string settings = Path.Combine(AppContext.BaseDirectory, "Settings.json");
        byte[] original = File.ReadAllBytes(settings);
        var json = JsonNode.Parse(original)!;
        json["gameSettings"]!["uiScale"] = float.Parse(args[3], CultureInfo.InvariantCulture);
        json["gameSettings"]!["language"] = args[4] == "ru" ? "Russian" : "English";
        try { File.WriteAllText(settings, json.ToJsonString()); return RunCore(args); }
        finally { File.WriteAllBytes(settings, original); }
    }

    private static int RunCore(string[] args)
    {
        string output = Path.GetFullPath(args[1]), directory = Path.GetDirectoryName(output)!;
        Directory.CreateDirectory(directory);
        string client = Path.Combine(Path.GetFullPath(args[0]), "src/DeepSpaceSaga.Client");
        Directory.SetCurrentDirectory(client);
        var checks = new Dictionary<string, bool>(); var captures = new List<string>();
        string? failure = null; bool completed = false; int frame = 0, stage = 0;
        string? renderer = null; long time = 0; float scale = float.Parse(args[3], CultureInfo.InvariantCulture);
        var watch = Stopwatch.StartNew();
        try
        {
            using var engine = SimulationEngine.CreateFromScenarioFile(Path.Combine(client, "Settings.json"), Path.Combine(client, "Scenarios/PlayerShipOnly/scenario.json"));
            var source = ScenarioLoader.LoadFromFile(Path.Combine(client, "Scenarios/PlayerShipOnly/scenario.json"));
            engine.LoadScenario(source with
            {
                GameState = source.GameState with
                {
                    CurrentSpeed = "Speed0",
                    SpaceObjects = source.GameState.SpaceObjects.Select(o => o with
                    {
                        SpeedMps = o.ObjectId == source.GameState.PlayerShipObjectId ? 3000 : o.SpeedMps,
                        Ai = null,
                        Modules = o.Modules!.Select(m => m with { PowerState = o.ObjectId == source.GameState.PlayerShipObjectId ? "On" : "Off" }).ToArray()
                    }).ToArray()
                }
            });
            var buffer = new SnapshotBuffer();
            var world = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0); buffer.Update(world);
            var writer = new DebugTuningFileWriter(Path.Combine(directory, "DebugTuning"));
            DebugTuningFileWriter.WriteDefaults(writer.DirectoryPath, world.DebugTuning!);
            string reference = File.ReadAllText(Path.Combine(writer.DirectoryPath, "debug-tuning.defaults.json"));
            var handle = new GameSessionHandle(new Driver(engine), buffer, new CombatMetricsWriter(Path.Combine(directory, "metrics")), writer);
            var screen = new GameSessionScreen(buffer, new DeepSpaceSaga.Motion.LinearMotionPredictor(), handle, uiScale: scale);
            Environment.SetEnvironmentVariable("DSS_MAP_FRAME_EXIT", "0");
            using var window = new SkiaWindow(screen, new Factory());
            typeof(SkiaWindow).GetField("_session", Private)!.SetValue(window, handle);
            typeof(SkiaWindow).GetField("_gameSessionScreen", Private)!.SetValue(window, screen);
            var native = (IWindow)typeof(SkiaWindow).GetField("_window", Private)!.GetValue(window)!;
            Task? flush = null;
            var down = new HashSet<Key>(); var keys = new KeyboardEdgeTracker();
            var pressed = new Key[64]; var released = new Key[64];
            window.NativeFrameRendered = (surface, _) =>
            {
                try
                {
                    frame++;
                    if (watch.Elapsed > TimeSpan.FromSeconds(60)) throw new TimeoutException("Native EP13 internal60s cap.");
                    checks["nativeOpenGlContext"] = typeof(SkiaWindow).GetField("_grContext", Private)!.GetValue(window) is not null;
                    renderer = typeof(SkiaWindow).GetField("_graphicsRenderer", Private)!.GetValue(window)?.ToString();
                    if (frame < 3 || frame % 3 != 0) return;
                    switch (stage)
                    {
                        case 0:
                            SetKey(Key.F12, true); SetKey(Key.F12, false); stage++; break;
                        case 1:
                            checks["f12Opens"] = screen.DebugPanel.Visible;
                            checks["physicalPanelWidth"] = Math.Abs(screen.DebugPanel.Bounds.Width * scale - 360 * scale) < .01;
                            checks["actualUiScale"] = Math.Abs(screen.UiScale - scale) < .001;
                            checks["localeTitle"] = Localization.Get("Debug.Title") == (args[4] == "ru" ? "Отладка боя" : "Combat tuning");
                            var row = screen.DebugPanel.RowBounds(0);
                            screen.OnMouseDown((row.Right - 45) * scale, row.MidY * scale); Publish(); stage++; break;
                        case 2:
                            checks["plusConfirms5point5"] = world.DebugTuning!.Values[0].Value == 5.5;
                            double zoom = screen.CameraPixelsPerWorldUnit;
                            screen.OnMouseWheel(screen.DebugPanel.Bounds.MidX * scale, screen.DebugPanel.Bounds.MidY * scale, -1);
                            checks["panelWheelDoesNotZoom"] = screen.CameraPixelsPerWorldUnit == zoom;
                            Capture(surface, "panel"); SetKey(Key.W, true); SetKey(Key.D, true); Publish(); stage++; break;
                        case 3:
                            time = 2000; Publish(); stage++; break;
                        case 4:
                            var ship = world.Objects.Single(o => o.ObjectId == world.PlayerShipObjectId);
                            checks["combinedAxesAuthoritativePose"] = Math.Abs(ship.SpeedKmS - 4.2) < 1e-6 && Math.Abs(ship.Direction - 40) < 1e-6;
                            Capture(surface, "axes"); SetKey(Key.W, false); SetKey(Key.D, false); Publish(); stage++; break;
                        case 5:
                            var reset = screen.DebugPanel.ResetAllBounds;
                            screen.OnMouseDown(reset.MidX * scale, reset.MidY * scale);
                            screen.OnMouseDown(reset.MidX * scale, reset.MidY * scale); Publish(); flush = handle.FlushDebugTuningAsync(); stage++; break;
                        case 6:
                            if (!flush!.IsCompleted) break;
                            flush.GetAwaiter().GetResult();
                            checks["confirmedResetDeletesCurrent"] = writer.Status.Deleted && !File.Exists(writer.CurrentPath);
                            checks["defaultsReferencePreserved"] = reference == File.ReadAllText(Path.Combine(writer.DirectoryPath, "debug-tuning.defaults.json"));
                            checks["defaultMaxRestored"] = world.DebugTuning!.Values[0].Value == world.DebugTuning.Defaults[0].Value;
                            Capture(surface, "reset"); SetKey(Key.Escape, true); SetKey(Key.Escape, false);
                            checks["escapeCloses"] = !screen.DebugPanel.Visible;
                            completed = true; native.Close(); break;
                    }
                }
                catch (Exception error) { failure = error.ToString(); native.Close(); }
            };
            try { window.Run(); }
            finally { handle.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
            void Publish() { world = engine.CaptureSnapshotForTests(time * 300, SimulationSpeed.Speed0, time); buffer.Update(world); }
            void SetKey(Key key, bool active)
            {
                if (active) down.Add(key); else down.Remove(key);
                var (p, r) = keys.PollBoth(down.Contains, pressed, released);
                screen.SynchronizeModifiers(false, false);
                for (int i = 0; i < p; i++) screen.OnKeyDown(pressed[i]);
                for (int i = 0; i < r; i++) screen.OnKeyUp(released[i]);
            }
            void Capture(SKSurface surface, string name)
            {
                string path = Path.Combine(directory, name + ".png");
                using var image = surface.Snapshot(); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var file = File.Create(path); data.SaveTo(file); captures.Add(path);
            }
        }
        catch (Exception error) { failure = error.ToString(); }
        bool passed = completed && failure is null && checks.Count > 0 && checks.Values.All(v => v);
        File.WriteAllText(output, JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            passed,
            completed,
            failure,
            frame,
            stage,
            renderer,
            elapsedSeconds = watch.Elapsed.TotalSeconds,
            scale,
            locale = args[4],
            checks,
            captures,
            input = "managed physical-key edges through production tracker/screen; no hardware input injection",
            notRun = new[] { "human visual acceptance", "physical keyboard and mouse acceptance", "audio acceptance" }
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(output); Console.WriteLine(passed ? "Native EP13 PASS" : "Native EP13 FAILED / unavailable");
        return passed ? 0 : 1;
    }

    private sealed class Driver(SimulationEngine engine) : IGameSessionConnection
    {
        private readonly Channel<AuthoritativeSnapshot> _channel = Channel.CreateUnbounded<AuthoritativeSnapshot>();
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) { engine.ReceiveCommand(command); return ValueTask.CompletedTask; }
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        { await foreach (var snapshot in _channel.Reader.ReadAllAsync(cancellationToken)) yield return snapshot; }
        public ValueTask DisposeAsync() { _channel.Writer.TryComplete(); return ValueTask.CompletedTask; }
    }
    private sealed class Factory : IGameSessionFactory
    {
        public IGameSessionConnection CreateSession() => throw new NotSupportedException();
        public IGameSessionConnection CreateSessionFromScenario(string path) => throw new NotSupportedException();
        public IGameSessionConnection CreateSessionFromSave(string slotId) => throw new NotSupportedException();
        public ScenarioInfo[] ListScenarios() => [];
        public bool HasQuickSave() => false;
        public SaveSlotInfo[] ListSaveSlots() => [];
        public void DeleteSaveSlot(string slotId) => throw new NotSupportedException();
    }
}
