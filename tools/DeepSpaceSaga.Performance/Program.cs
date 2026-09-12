using System.Diagnostics;
using System.Text.Json;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using SkiaSharp;

// Standalone, deterministic Release harness. No timings are assertions in unit tests.
// Raster Skia executes the real screen's drawing pipeline; these are CPU/raster
// timings, not GPU presentation FPS. Measurements exclude scenario loading/JIT warmup.
if (args.Length < 2)
    throw new ArgumentException("Usage: DeepSpaceSaga.Performance <DSS root> <output.json> [--stages | --soak]");
string root = Path.GetFullPath(args[0]);
string output = Path.GetFullPath(args[1]);
if (Array.IndexOf(args, "--compare") is var compareIndex && compareIndex >= 0)
{
    CompareFrames(args[compareIndex + 1], args[compareIndex + 2]);
    return;
}
var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(root, "src/DeepSpaceSaga.Client/Settings.json"), out _, out _);
var scenario = ScenarioLoader.LoadFromFile(Path.Combine(root, "src/DeepSpaceSaga.Client/Scenarios/Default_500/scenario.json"));
scenario = scenario with { GameState = scenario.GameState with { MasterSeed = 500 } };
var results = new List<Measurement>();
var stages = new Dictionary<string, (double Ms, long Bytes, int Count)>();
const int frames = 600;
SimulationEngine Engine()
{
    var engine = new SimulationEngine(registry);
    engine.LoadScenario(scenario);
    return engine;
}
if (args.Contains("--soak"))
{
    RunSoak();
    return;
}
using (var engine = Engine())
{
    long time = 0;
    Measure("engine_snapshot_502", 1000, () => engine.CaptureSnapshotForTests(time += 1000, SimulationSpeed.Speed1));
}
using (var engine = Engine())
{
    long time = 0;
    int command = 0;
    Measure("approach_command_and_snapshot", 100, () =>
    {
        engine.ReceiveCommand(new PlayerCommand($"perf-{++command}", (ulong)command, "SPC-0001", "MOD-PLAYER-ENGINE-01",
            NavigationComputerCommandTypes.Approach, TargetObjectId: "AST-0001"));
        var snapshot = engine.CaptureSnapshotForTests(time += 250, SimulationSpeed.Speed1);
        if (snapshot.CommandResults.Any(r => r.Status == CommandResultStatus.Rejected))
            throw new InvalidOperationException("Benchmark command was rejected.");
    });
}
using (var engine = Engine())
{
    var objects = engine.CaptureSnapshotForTests().Objects;
    var predictor = new LinearMotionPredictor();
    double checksum = 0;
    Measure("predict_502_positions", 2000, () =>
    {
        foreach (var obj in objects) checksum += predictor.Predict(obj, 375).X;
    });
    GC.KeepAlive(checksum);
}
RunScreen("render_paused", SimulationSpeed.Speed0, false);
RunScreen("render_running", SimulationSpeed.Speed1, false);
RunScreen("render_approach", SimulationSpeed.Speed1, true);
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllText(output, JsonSerializer.Serialize(new
{
    Scenario = scenario.Metadata.ScenarioId, Objects = scenario.GameState.SpaceObjects.Count,
    Seed = 500, Width = 1280, Height = 720, Frames = frames,
    Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    OS = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
    ProcessorCount = Environment.ProcessorCount, Backend = "Skia raster, full GameSessionScreen.Render",
    Measurements = results,
    Stages = stages.ToDictionary(p => p.Key, p => new { MeanMs = p.Value.Ms / p.Value.Count,
        AllocatedBytesPerFrame = p.Value.Bytes / (double)p.Value.Count })
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(output);

void RunScreen(string name, SimulationSpeed speed, bool approach)
{
    using var engine = Engine();
    long clock = 0;
    var buffer = new SnapshotBuffer(() => clock);
    if (approach)
        engine.ReceiveCommand(new PlayerCommand("perf-approach", 1, "SPC-0001", "MOD-PLAYER-ENGINE-01",
            NavigationComputerCommandTypes.Approach, TargetObjectId: "AST-0001"));
    buffer.Update(engine.CaptureSnapshotForTests(0, speed));
    var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => clock);
    using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
    int frame = 0;
    if (args.Contains("--stages"))
    {
        long stageStart = 0, stageBytes = 0;
        screen.RenderStageCompleted = stage =>
        {
            long now = Stopwatch.GetTimestamp(), bytes = GC.GetAllocatedBytesForCurrentThread();
            if (stage != "begin" && frame > 160)
            {
                string key = name + "/" + stage;
                stages.TryGetValue(key, out var current);
                stages[key] = (current.Ms + Stopwatch.GetElapsedTime(stageStart, now).TotalMilliseconds,
                    current.Bytes + bytes - stageBytes, current.Count + 1);
            }
            stageStart = Stopwatch.GetTimestamp();
            stageBytes = GC.GetAllocatedBytesForCurrentThread();
        };
    }
    Action prepare = () =>
    {
        clock = ++frame * Stopwatch.Frequency / 80;
        if (frame % 80 == 0)
            buffer.Update(engine.CaptureSnapshotForTests(speed == SimulationSpeed.Speed0 ? 0 : frame * 1000 / 80, speed));
    };
    Measure(name, frames, () =>
    {
        surface.Canvas.Clear(SKColors.Black);
        screen.Render(surface.Canvas, 1280, 720);
        surface.Canvas.Flush();
    }, prepare);
    using var picture = surface.Snapshot();
    using var png = picture.Encode(SKEncodedImageFormat.Png, 100);
    string imageDirectory = Path.Combine(Directory.GetParent(root)!.FullName, "DSS-Images", "temp", "map-performance");
    Directory.CreateDirectory(imageDirectory);
    using var file = File.Create(Path.Combine(imageDirectory, Path.GetFileNameWithoutExtension(output) + "." + name + ".png"));
    png.SaveTo(file);
    GC.KeepAlive(screen);
}

void Measure(string name, int count, Action action, Action? prepare = null)
{
    for (int i = 0; i < 160; i++) { prepare?.Invoke(); action(); }
    GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
    long retainedBefore = GC.GetTotalMemory(false);
    using var process = Process.GetCurrentProcess();
    process.Refresh();
    long privateBefore = process.PrivateMemorySize64;
    int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
    double[] times = new double[count];
    long allocated = 0;
    for (int i = 0; i < count; i++)
    {
        prepare?.Invoke();
        long allocationStart = GC.GetAllocatedBytesForCurrentThread();
        long start = Stopwatch.GetTimestamp();
        action();
        times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        allocated += GC.GetAllocatedBytesForCurrentThread() - allocationStart;
    }
    var collections = new[] { GC.CollectionCount(0) - gen0, GC.CollectionCount(1) - gen1, GC.CollectionCount(2) - gen2 };
    GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
    long retainedDelta = GC.GetTotalMemory(false) - retainedBefore;
    process.Refresh();
    long privateAfter = process.PrivateMemorySize64;
    Array.Sort(times);
    var result = new Measurement(name, count, times.Average(), times[count / 2], times[(int)(count * .95)],
        times[^1], allocated / (double)count, retainedDelta, privateBefore, privateAfter, collections);
    results.Add(result);
    Console.WriteLine(JsonSerializer.Serialize(result));
}

void RunSoak()
{
    using var engine = Engine();
    long clock = 0;
    var buffer = new SnapshotBuffer(() => clock);
    buffer.Update(engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed1));
    var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => clock);
    using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
    using var process = Process.GetCurrentProcess();
    var samples = new List<object>();
    for (int frame = 1; frame <= 9600; frame++)
    {
        clock = frame * Stopwatch.Frequency / 80;
        if (frame % 80 == 0)
            buffer.Update(engine.CaptureSnapshotForTests(frame * 1000 / 80, SimulationSpeed.Speed1));
        surface.Canvas.Clear(SKColors.Black);
        screen.Render(surface.Canvas, 1280, 720);
        surface.Canvas.Flush();
        if (frame % 800 != 0) continue;
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        process.Refresh();
        var trails = screen.TrailStatistics;
        var sample = new { SimulatedSeconds = frame / 80, ManagedBytes = GC.GetTotalMemory(false),
            PrivateBytes = process.PrivateMemorySize64, WorkingSetBytes = process.WorkingSet64,
            trails.Trails, trails.Points, trails.Capacity };
        samples.Add(sample);
        Console.WriteLine(JsonSerializer.Serialize(sample));
    }
    Directory.CreateDirectory(Path.GetDirectoryName(output)!);
    File.WriteAllText(output, JsonSerializer.Serialize(new { Objects = 502, Frames = 9600,
        SimulatedSeconds = 120, Backend = "Skia raster, full GameSessionScreen.Render", Samples = samples },
        new JsonSerializerOptions { WriteIndented = true }));
    GC.KeepAlive(screen);
}

void CompareFrames(string before, string after)
{
    string folder = Path.Combine(Directory.GetParent(root)!.FullName, "DSS-Images", "temp", "map-performance");
    var comparisons = new List<object>();
    foreach (string name in new[] { "render_paused", "render_running", "render_approach" })
    {
        using var a = SKBitmap.Decode(Path.Combine(folder, before + "." + name + ".png"));
        using var b = SKBitmap.Decode(Path.Combine(folder, after + "." + name + ".png"));
        if (a.Width != b.Width || a.Height != b.Height) throw new InvalidOperationException("Image sizes differ.");
        long error = 0, changed = 0;
        int maximum = 0;
        for (int y = 0; y < a.Height; y++)
        for (int x = 0; x < a.Width; x++)
        {
            var p = a.GetPixel(x, y); var q = b.GetPixel(x, y);
            int r = Math.Abs(p.Red - q.Red), g = Math.Abs(p.Green - q.Green), blue = Math.Abs(p.Blue - q.Blue);
            error += r + g + blue;
            if (r + g + blue > 0) changed++;
            maximum = Math.Max(maximum, Math.Max(r, Math.Max(g, blue)));
        }
        comparisons.Add(new { Name = name, MeanAbsoluteChannelError = error / (double)(a.Width * a.Height * 3),
            ChangedPixelPercent = 100d * changed / (a.Width * a.Height), MaxChannelDifference = maximum });
    }
    File.WriteAllText(output, JsonSerializer.Serialize(comparisons, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine(File.ReadAllText(output));
}

record Measurement(string Name, int Samples, double MeanMs, double MedianMs, double P95Ms, double MaxMs,
    double AllocatedBytesPerOperation, long RetainedManagedDeltaBytes, long PrivateBytesBefore, long PrivateBytesAfter, int[] Collections);
