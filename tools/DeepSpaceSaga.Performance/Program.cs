using System.Diagnostics;
using System.Text.Json;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI;
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
if (args.Contains("--map-review"))
{
    RenderMapReview();
    return;
}
if (args.Contains("--trajectory-review"))
{
    RenderTrajectoryReview();
    return;
}
if (args.Contains("--grid-review"))
{
    RenderGridReview();
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
if (args.Contains("--scales"))
{
    for (int i = 0; i < 5; i++) RunScreen($"scale_{i}", SimulationSpeed.Speed1, false, i);
    RunScreen("system_view", SimulationSpeed.Speed1, false, 5);
}
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

void RunScreen(string name, SimulationSpeed speed, bool approach, int? scaleIndex = null)
{
    using var engine = Engine();
    long clock = 0;
    var buffer = new SnapshotBuffer(() => clock);
    if (approach)
        engine.ReceiveCommand(new PlayerCommand("perf-approach", 1, "SPC-0001", "MOD-PLAYER-ENGINE-01",
            NavigationComputerCommandTypes.Approach, TargetObjectId: "AST-0001"));
    buffer.Update(engine.CaptureSnapshotForTests(0, speed));
    var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => clock,
        mapSettings: TacticalMapSettings.Load(Path.Combine(root, "src/DeepSpaceSaga.Client/Settings.json")));
    using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
    screen.Render(surface.Canvas, 1280, 720);
    if (scaleIndex is { } index)
    {
        if (index == 5) screen.FitMapView(MapFitMode.System);
        else
        {
            var rect = screen.ScaleButtonRects[index];
            screen.OnMouseDown(rect.MidX, rect.MidY);
        }
    }
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

void RenderGridReview()
{
    var settings = TacticalMapSettings.Load(Path.Combine(root, "src/DeepSpaceSaga.Client/Settings.json"));
    var grid = new GridRenderer(settings);
    string directory = Path.Combine(Directory.GetParent(root)!.FullName, "DSS-Images", "temp", "map-performance");
    Directory.CreateDirectory(directory);
    foreach (var (name, relativeZoom) in new[] { ("max", 1.0), ("fade", .15), ("parent", .1),
        ("grandparent", .02), ("far", 1e-12) })
    {
        const int size = 1200;
        double ppu = settings.MaximumPpu * relativeZoom;
        using var surface = SKSurface.Create(new SKImageInfo(size, size));
        var camera = new CameraState(550 / ppu, 550 / ppu, ppu);
        grid.Draw(surface.Canvas, camera, size, size);
        using var picture = surface.Snapshot(); using var png = picture.Encode(SKEncodedImageFormat.Png, 100);
        string path = Path.Combine(directory, "grid-review-" + name + ".png");
        using var file = File.Create(path); png.SaveTo(file); Console.WriteLine(path);
    }
}

void RenderTrajectoryReview()
{
    using var engine = Engine();
    var baseline = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0);
    var ship = baseline.Objects.Single(o => o.ObjectId == baseline.PlayerShipObjectId) with
    {
        X = -1000, Y = 500, SpeedKmS = 3, Direction = 270,
        ActiveEngineCommandType = NavigationComputerCommandTypes.Approach,
        NavigationTargetX = 0, NavigationTargetY = 0, NavigationTargetSpeedKmS = 5,
        NavigationTargetDirectionDegrees = 90, NavigationTargetObjectId = "QA-TARGET"
    };
    ship = ship with { ApproachRoute = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, 5, 10, 4) };
    foreach (var (name, preset, approach) in new[] { ("approach", 1, true), ("approach-far", 4, true), ("straight-far", 4, false) })
    {
        var player = approach ? ship : ship with { ApproachRoute = null, ActiveEngineCommandType = null,
            NavigationTargetX = null, NavigationTargetY = null, Direction = 90 };
        var buffer = new SnapshotBuffer();
        buffer.Update(baseline with { Objects = [player, new("QA-TARGET", 0, 0, 5, 90,
            RenderObjectType: SpaceObjectType.Asteroid, DisplayName: "Faster target")] });
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var surface = SKSurface.Create(new SKImageInfo(1920, 1080));
        screen.Render(surface.Canvas, 1920, 1080);
        var button = screen.ScaleButtonRects[preset]; screen.OnMouseDown(button.MidX, button.MidY);
        screen.Render(surface.Canvas, 1920, 1080);
        using var picture = surface.Snapshot(); using var png = picture.Encode(SKEncodedImageFormat.Png, 100);
        string directory = Path.Combine(Directory.GetParent(root)!.FullName, "DSS-Images", "temp", "map-performance");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "trajectory-review-" + name + ".png");
        using var file = File.Create(path); png.SaveTo(file); Console.WriteLine(path);
    }
}

void RenderMapReview()
{
    // Synthetic known system for visual QA; never loaded into a player's save.
    using var engine = Engine();
    var baseline = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0);
    var player = baseline.Objects.Single(o => o.ObjectId == baseline.PlayerShipObjectId) with
    {
        X = 1.4e9, Y = 0, SpeedKmS = 0, NavigationTargetX = 1.4e9 + 10000,
        NavigationTargetY = 10000, NavigationTargetObjectId = "QA-TARGET"
    };
    var objects = System.Collections.Immutable.ImmutableArray.Create(player,
        new ObjectMotionSnapshot("QA-TARGET", 1.4e9 + 10000, 10000, 0, 0, RenderObjectType: SpaceObjectType.UnknownSpaceObject),
        new ObjectMotionSnapshot("QA-SUN", 0, 0, 0, 0, RenderObjectType: SpaceObjectType.Sun, DisplayName: "Sun"),
        new ObjectMotionSnapshot("QA-PLANET", 1.4e9, 0, 0, 0, RenderObjectType: SpaceObjectType.Planet, DisplayName: "Planet"));
    var buffer = new SnapshotBuffer();
    buffer.Update(baseline with { Objects = objects });
    foreach (var (name, width, height, uiScale, mode) in new[] {
        ("system", 1920, 1080, 1f, (MapFitMode?)MapFitMode.System),
        ("target", 1920, 1080, 1.5f, (MapFitMode?)MapFitMode.Target),
        ("offscreen", 1280, 720, 1f, (MapFitMode?)null) })
    {
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), uiScale: uiScale);
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        screen.Render(surface.Canvas, width, height);
        if (mode is { } view) screen.FitMapView(view);
        for (int i = 0; i < 3; i++) screen.Render(surface.Canvas, width, height);
        using var picture = surface.Snapshot();
        using var png = picture.Encode(SKEncodedImageFormat.Png, 100);
        string directory = Path.Combine(Directory.GetParent(root)!.FullName, "DSS-Images", "temp", "map-performance");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "map-review-" + name + ".png");
        using var file = File.Create(path); png.SaveTo(file); Console.WriteLine(path);
    }
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
