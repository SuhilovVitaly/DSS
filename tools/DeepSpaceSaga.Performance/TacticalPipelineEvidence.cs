using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

internal static class TacticalPipelineEvidence
{
    private const int Warmup = 120, Frames = 600;
    internal static int Run(string[] args)
    {
        string root = Path.GetFullPath(args[0]), output = Path.GetFullPath(args[1]);
        string Value(string name, string fallback) => Array.IndexOf(args, name) is var i && i >= 0 ? args[i + 1] : fallback;
        Directory.SetCurrentDirectory(Path.Combine(root, "src/DeepSpaceSaga.Client"));
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        bool native = args.Contains("--native");
        int[] counts = native ? [int.Parse(Value("--objects", "5000"))] : [500, 5000];
        (int Width, int Height)[] sizes = native ? [ParseSize(Value("--size", "1920x1080"))] : [(1920, 1080), (3440, 1440)];
        float scale = float.Parse(Value("--scale", "1"), CultureInfo.InvariantCulture);
        string[] modes = native ? ["route"] : ["paused", "live", "zoom", "pan", "route"];
        var results = new List<object>();
        foreach (int count in counts) foreach (var size in sizes) foreach (string mode in modes)
        {
            using var fixture = new Fixture(count, size.Width, size.Height, scale, mode, native);
            if (native) return RunNative(fixture, root, output, scale);
            using var surface = SKSurface.Create(new SKImageInfo(size.Width, size.Height));
            for (int i = 0; i < Warmup; i++) { fixture.Advance(i); fixture.Screen.Render(surface.Canvas, size.Width, size.Height); }
            var elapsed = new double[Frames]; var allocated = new double[Frames];
            var update = new double[Frames]; var prepare = new double[Frames]; var draw = new double[Frames];
            long geometry = 0, paint = 0, reuse = 0, samples = 0, spatial = 0;
            int[] gc = Enumerable.Range(0, 3).Select(GC.CollectionCount).ToArray();
            var pause = GC.GetTotalPauseDuration();
            for (int i = 0; i < Frames; i++)
            {
                fixture.Advance(i + Warmup);
                long bytes = GC.GetAllocatedBytesForCurrentThread(), start = Stopwatch.GetTimestamp();
                fixture.Screen.Render(surface.Canvas, size.Width, size.Height);
                elapsed[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                allocated[i] = GC.GetAllocatedBytesForCurrentThread() - bytes;
                var m = fixture.Screen.LastPipelineMetrics!.Value;
                update[i] = m.UpdateMs; prepare[i] = m.PrepareMs; draw[i] = m.DrawMs;
                geometry += m.SceneBuilds + m.TrailBuilds + m.FutureBuilds + m.NavigationBuilds + m.LabelBuilds;
                paint += m.PaintBuilds; reuse += m.PaintReuses; samples += m.PathPoints; spatial += m.SpatialUpdates;
            }
            results.Add(new
            {
                objects = count,
                size.Width,
                size.Height,
                scale,
                mode,
                cpuMs = Stats(elapsed),
                allocatedBytes = Stats(allocated),
                updateMs = Stats(update),
                prepareMs = Stats(prepare),
                drawMs = Stats(draw),
                geometryBuilds = geometry,
                paintBuilds = paint,
                paintReuses = reuse,
                representedPathPoints = samples,
                spatialUpdates = spatial,
                gcCollections = Enumerable.Range(0, 3).Select(i => GC.CollectionCount(i) - gc[i]).ToArray(),
                gcPauseMs = (GC.GetTotalPauseDuration() - pause).TotalMilliseconds
            });
            Console.WriteLine($"{count} {size.Width}x{size.Height} {mode}: p99={elapsed.Order().ElementAt(593):F3} ms, {allocated.Average():F0} B/frame");
            File.WriteAllText(output, JsonSerializer.Serialize(new
            {
                status = results.Count == 20 ? "measured" : "in-progress",
                backend = "Skia raster; CPU only, not GPU presentation",
                warmupFrames = Warmup,
                measuredFrames = Frames,
                machine = Machine(root),
                results
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        return 0;
    }

    private static object Stats(double[] values)
    {
        var sorted = values.Order().ToArray();
        return new
        {
            p50 = sorted[(int)Math.Ceiling(.5 * sorted.Length) - 1],
            p95 = sorted[(int)Math.Ceiling(.95 * sorted.Length) - 1],
            p99 = sorted[(int)Math.Ceiling(.99 * sorted.Length) - 1],
            mean = values.Average(),
            max = sorted[^1]
        };
    }
    private static object Machine(string root) => new
    {
        cpu = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"),
        logicalProcessors = Environment.ProcessorCount,
        runtime = RuntimeInformation.FrameworkDescription,
        os = RuntimeInformation.OSDescription,
        baseCommit = SolarMapEvidence.Revision(root),
        clientBinarySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(GameSessionScreen).Assembly.Location))),
        note = "Binary hash identifies measured implementation, including uncommitted ticket changes. Deterministic synthetic contacts; snapshot preparation is outside raster Render timing."
    };
    private static (int, int) ParseSize(string value)
    {
        var parts = value.Split('x'); return (int.Parse(parts[0]), int.Parse(parts[1]));
    }

    private static int RunNative(Fixture fixture, string root, string output, float scale)
    {
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_REPORT", output);
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_COMMIT", SolarMapEvidence.Revision(root));
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_CASE", $"EP-0005 deterministic {fixture.Count} contacts; native scripted pause/resume/zoom/pan/selection; UI={scale}");
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_EXIT", "1");
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_WINDOW", $"{fixture.Width}x{fixture.Height}");
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_IMAGE", Path.ChangeExtension(output, ".png"));
        int frame = 0;
        var interactions = new List<string>();
        fixture.Screen.RenderStageCompleted = stage =>
        {
            if (stage != "begin") return;
            fixture.Advance(frame++);
            if (frame == 20)
            {
                double zoom = fixture.Screen.CameraPixelsPerWorldUnit;
                fixture.Screen.OnMouseWheel(fixture.Width / 2f, 300, -1);
                interactions.Add($"zoom={fixture.Screen.IsZoomAnimating || fixture.Screen.CameraPixelsPerWorldUnit != zoom}");
            }
            if (frame == 40)
            {
                fixture.StartPan();
                double focus = fixture.Screen.CameraFocusX;
                fixture.Screen.OnMouseMove(fixture.PanX + 80, fixture.PanY);
                fixture.Screen.OnMouseUp(fixture.PanX + 80, fixture.PanY);
                interactions.Add($"pan={fixture.Screen.CameraFocusX != focus}");
            }
            if (frame is 60 or 90)
            {
                int index = frame == 60 ? 0 : 1;
                var rect = fixture.Screen.SpeedButtonRects[index];
                fixture.Screen.OnMouseDown(rect.MidX * scale, rect.MidY * scale);
                fixture.Screen.OnMouseUp(rect.MidX * scale, rect.MidY * scale);
                interactions.Add($"speed={fixture.Buffer.CurrentSpeed}");
            }
            if (frame == 100 && fixture.Screen.PreparedScene is { } scene)
            {
                var target = scene.HitCandidates.FirstOrDefault(h => h.ObjectId == "target");
                if (target.ObjectId is not null)
                {
                    fixture.Screen.OnMouseDown(target.Center.X, target.Center.Y);
                    fixture.Screen.OnMouseUp(target.Center.X, target.Center.Y);
                    interactions.Add($"selected={fixture.Screen.SelectedObjectId}");
                }
            }
        };
        using (var window = new SkiaWindow(fixture.Screen, new EmptyFactory(), Stopwatch.StartNew())) window.Run();
        if (!File.Exists(output)) return 1;
        var report = JsonNode.Parse(File.ReadAllText(output))!;
        report["pipelineMachine"] = JsonSerializer.SerializeToNode(Machine(root));
        report["scriptedInteractions"] = JsonSerializer.SerializeToNode(interactions);
        report["pipelineFrames"] = JsonSerializer.SerializeToNode(fixture.Screen.CaptureFrameProfile().Frames.Where(f => f.FrameId > Warmup).Select(f => new { f.FrameId, f.Pipeline, f.RenderAllocatedBytes }));
        report["humanManualSmoke"] = "NOT RUN; scripted native interaction and image inspection are separate evidence";
        File.WriteAllText(output, report.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }

    private sealed class Fixture : IDisposable
    {
        internal int Count, Width, Height;
        internal float PanX, PanY;
        internal readonly GameSessionScreen Screen;
        internal readonly SnapshotBuffer Buffer;
        private readonly ObjectMotionSnapshot[] _initial;
        private readonly LinearMotionPredictor _predictor = new();
        private readonly string _mode;
        private readonly bool _native;
        private long _clock, _lastClock, _lastSnapshot;
        private double _motionMs;
        private ulong _sequence;
        internal Fixture(int count, int width, int height, float scale, string mode, bool native)
        {
            Count = count; Width = width; Height = height; _mode = mode; _native = native;
            _clock = native ? Stopwatch.GetTimestamp() : 0; _lastClock = _clock;
            Buffer = new SnapshotBuffer(() => _clock);
            int side = (int)Math.Ceiling(Math.Sqrt(count));
            _initial = Enumerable.Range(0, count).Select(i => new ObjectMotionSnapshot($"contact-{i}",
                (i % side - side / 2) * 160, (i / side - side / 2) * 160, i % 8 == 0 ? .2 : 0, 90,
                RenderObjectType: SpaceObjectType.Asteroid)).ToArray();
            _initial[0] = new("player", 0, 0, 1, 90, RenderObjectType: SpaceObjectType.PlayerShip);
            _initial[1] = new("target", 150, -130, .1, 90, RenderObjectType: SpaceObjectType.NpcShip);
            if (mode == "route") _initial[0] = _initial[0] with
            {
                NavigationTargetX = 5000,
                NavigationTargetY = -500,
                NavigationAngularInertiaDegPerSec = 1,
                TurnStepDegrees = 1,
                TurnStepIntervalMs = 250,
                TurnStepRemainingMs = 250,
                ActiveEngineCommandType = ShipEngineCommandTypes.Orbit,
                NavigationTargetObjectId = "target"
            };
            Publish(mode == "paused" ? SimulationSpeed.Speed0 : SimulationSpeed.Speed1);
            Screen = new(Buffer, _predictor, timestampProvider: () => _clock, uiScale: scale,
                mapSettings: TacticalMapSettings.Load("Settings.json"))
            { PipelineMetricsEnabled = true };
        }
        private void Publish(SimulationSpeed speed)
        {
            var objects = _initial.Select(o => _predictor.Predict(o, (long)_motionMs)).ToImmutableArray();
            Buffer.Update(new(++_sequence, (long)_motionMs, speed, objects, PlayerShipObjectId: "player"));
            _lastSnapshot = _clock;
        }
        internal void Advance(int frame)
        {
            _clock = _native ? Stopwatch.GetTimestamp() : _clock + Stopwatch.Frequency / 80;
            if (Buffer.CurrentSpeed != SimulationSpeed.Speed0) _motionMs += (_clock - _lastClock) * 1000.0 / Stopwatch.Frequency;
            _lastClock = _clock;
            if (_mode != "paused" && _clock - _lastSnapshot >= Stopwatch.Frequency) Publish(Buffer.CurrentSpeed);
            if (_mode == "zoom" && frame > 0 && frame % 16 == 0) Screen.OnMouseWheel(Width / 2f, 300, frame / 80 % 2 == 0 ? -1 : 1);
            if (_mode == "pan")
            {
                if (frame == 2) StartPan();
                if (frame > 2) Screen.OnMouseMove(PanX + (frame % 160 - 80), PanY);
            }
        }
        internal void StartPan()
        {
            var scene = Screen.PreparedScene;
            if (scene is null) return;
            for (float y = 150; y < Height - 180; y += 40) for (float x = 420; x < Width - 420; x += 40)
            {
                if (scene.View.Obstacles.Any(r => r.Contains(x, y)) || scene.HitCandidates.Any(h => Math.Pow(h.Center.X - x, 2) + Math.Pow(h.Center.Y - y, 2) <= 1000)) continue;
                PanX = x; PanY = y; Screen.OnMouseDown(x, y); return;
            }
        }
        public void Dispose() => Screen.Dispose();
    }
    private sealed class EmptyFactory : IGameSessionFactory
    {
        public IGameSessionConnection CreateSession() => throw new NotSupportedException();
        public IGameSessionConnection CreateSessionFromSave(string slotId) => throw new NotSupportedException();
        public IGameSessionConnection CreateSessionFromScenario(string path) => throw new NotSupportedException();
        public ScenarioInfo[] ListScenarios() => [];
        public bool HasQuickSave() => false;
        public SaveSlotInfo[] ListSaveSlots() => [];
        public void DeleteSaveSlot(string slotId) => throw new NotSupportedException();
    }
}
