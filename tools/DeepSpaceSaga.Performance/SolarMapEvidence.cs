using DeepSpaceSaga.Client;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using SkiaSharp;

public static class SolarMapEvidence
{
    public static int Run(string[] args)
    {
        string? output = args.Length > 1 ? Path.GetFullPath(args[1]) : null;
        string originalDirectory = Directory.GetCurrentDirectory();
        try
        {
            if (args.Length < 2) throw new ArgumentException("Expected repository root and output.json.");
            string root = Path.GetFullPath(args[0]);
            string Option(string key, string fallback)
            {
                int i = Array.IndexOf(args, key);
                if (i < 0) return fallback;
                if (i + 1 >= args.Length || args[i + 1].StartsWith("--")) throw new ArgumentException($"Missing {key} value.");
                return args[i + 1];
            }
            string mode = Option("--config", "max");
            if (mode is not ("min" or "max")) throw new ArgumentException("--config must be min or max.");
            string[] range = Option("--seeds", "1:100").Split(':');
            if (range.Length != 2 || !ulong.TryParse(range[0], out ulong from) || !ulong.TryParse(range[1], out ulong to) ||
                from > to || to - from >= 10000) throw new ArgumentException("Invalid --seeds range (maximum 10000 seeds).");
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
            var fields = JsonSerializer.Deserialize<StationResourceFieldConfig>(File.ReadAllText(Path.Combine(client, "Data/World/station-resource-fields.json")))!;
            string scenarioArgument = Option("--scenarios", "all");
            var names = Directory.GetFiles(Path.Combine(client, "Scenarios"), "scenario.json", SearchOption.AllDirectories)
                .Select(p => Path.GetFileName(Path.GetDirectoryName(p))!).Order(StringComparer.Ordinal).ToArray();
            if (scenarioArgument != "all")
            {
                var requested = scenarioArgument.Split(',');
                if (requested.Any(n => !names.Contains(n))) throw new ArgumentException("Unknown scenario.");
                names = requested;
            }
            var rows = new List<object>();
            var rendering = new List<object>();
            foreach (string name in names)
            {
                var source = ScenarioLoader.LoadFromFile(Path.Combine(client, "Scenarios", name, "scenario.json"));
                for (ulong seed = from; ; seed++)
                {
                    long allocated = GC.GetAllocatedBytesForCurrentThread();
                    long start = Stopwatch.GetTimestamp();
                    using var engine = new SimulationEngine(registry);
                    engine.ConfigureStationResourceFields(fields);
                    engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = seed, CurrentSpeed = "Speed0" } }, generation: config);
                    double generationMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                    long generationBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
                    start = Stopwatch.GetTimestamp(); allocated = GC.GetAllocatedBytesForCurrentThread();
                    var snapshot = engine.CaptureSnapshot();
                    double snapshotMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                    long snapshotBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
                    start = Stopwatch.GetTimestamp();
                    string save = ScenarioLoader.Serialize(engine.CaptureSaveState());
                    double saveSerializationMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                    rows.Add(new
                    {
                        scenario = name,
                        seed,
                        generatorVersion = 1,
                        saveFormatVersion = SaveFormat.CurrentSaveFormatVersion,
                        objects = snapshot.Objects.Length,
                        planets,
                        belts,
                        generationMs,
                        generationBytes,
                        snapshotMs,
                        snapshotBytes,
                        saveSerializationMs,
                        saveBytes = Encoding.UTF8.GetByteCount(save)
                    });
                    // Rendering is sampled at the first requested seed for every scenario,
                    // separately from the complete generation/save seed corpus.
                    if (seed == from)
                        foreach (string view in new[] { "system", "belt" })
                            rendering.Add(Render(snapshot, name, seed, view));
                    if (seed == to) break;
                }
            }
            Write(output!, new
            {
                schemaVersion = 1,
                status = "passed",
                backend = "CPU/Skia raster",
                machine = Machine(),
                assetRoot = client,
                commit = Revision(root),
                config,
                seedRange = new { from, to },
                scenarios = names,
                measurements = rows,
                rendering,
                renderSampling = "First requested seed per scenario; 120 warmup and 600 measured frames for each system/belt view.",
                generationTiming = "Production LoadScenario pipeline with configured resources; catalog file parsing excluded.",
                presentation = new { status = "not-measured", targetFps = 80, reason = "Raster timings do not measure GPU presentation." }
            });
            return 0;
        }
        catch (Exception ex)
        {
            if (output is not null) Write(output, new { schemaVersion = 1, status = "failed", backend = "CPU/Skia raster", machine = Machine(), error = ex.ToString() });
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        finally { Directory.SetCurrentDirectory(originalDirectory); }
    }

    private static object Render(AuthoritativeSnapshot snapshot, string scenario, ulong seed, string view)
    {
        var buffer = new SnapshotBuffer(); buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        if (view == "system") screen.FitMapView(MapFitMode.System);
        else screen.FitBelt(snapshot.SolarSystemMap!.Belts[0].Id);
        for (int i = 0; i < 120; i++) { screen.Render(canvas, 1920, 1080); canvas.Flush(); }
        var ms = new double[600];
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < ms.Length; i++)
        {
            long start = Stopwatch.GetTimestamp();
            screen.Render(canvas, 1920, 1080); canvas.Flush();
            ms[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }
        long bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
        Array.Sort(ms);
        return new
        {
            scenario,
            seed,
            view,
            backend = "CPU/Skia raster",
            width = 1920,
            height = 1080,
            uiScale = 1,
            warmupFrames = 120,
            measuredFrames = 600,
            p50Ms = ms[299],
            p95Ms = ms[569],
            p99Ms = ms[593],
            meanMs = ms.Average(),
            allocationBytesPerFrame = bytes / 600d
        };
    }

    public static object Machine() => new
    {
        cpu = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"),
        logicalProcessors = Environment.ProcessorCount,
        runtime = RuntimeInformation.FrameworkDescription,
        os = RuntimeInformation.OSDescription,
        architecture = RuntimeInformation.ProcessArchitecture.ToString()
    };

    public static string Revision(string root)
    {
        using var process = Process.Start(new ProcessStartInfo("git")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            ArgumentList = { "rev-parse", "HEAD" }
        })!;
        string commit = process.StandardOutput.ReadToEnd().Trim(); process.WaitForExit();
        return process.ExitCode == 0 ? commit : "unavailable";
    }

    private static void Write(string path, object report)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }
}
