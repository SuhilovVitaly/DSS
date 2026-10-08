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
        object? failureRepro = null;
        string? measurementsPath = null;
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
            bool clusters = args.Contains("--clusters");
            bool aiPlacement = args.Contains("--ai-placement");
            bool allMapLayers = args.Contains("--all-map-layers");
            bool boundaryOnly = allMapLayers || aiPlacement || args.Contains("--boundary-only");
            if (aiPlacement && !clusters) throw new ArgumentException("--ai-placement requires --clusters.");
            if (allMapLayers && !clusters) throw new ArgumentException("--all-map-layers requires --clusters.");
            var aiConfig = aiPlacement || allMapLayers ? config.Ai ?? throw new ArgumentException("AI config missing.") : null;
            if (aiConfig is not null)
            {
                int count = mode == "min" ? aiConfig.MinBases : aiConfig.MaxBases;
                aiConfig = aiConfig with
                {
                    MinBases = count,
                    MaxBases = count,
                    PatrolRadiusKm = double.Parse(Option("--ai-patrol-radius-km", aiConfig.PatrolRadiusKm.ToString(System.Globalization.CultureInfo.InvariantCulture)), System.Globalization.CultureInfo.InvariantCulture),
                    MaxPlacementAttempts = int.Parse(Option("--ai-attempts", aiConfig.MaxPlacementAttempts.ToString(System.Globalization.CultureInfo.InvariantCulture)), System.Globalization.CultureInfo.InvariantCulture)
                };
            }
            int planets = mode == "min" ? 3 : 7, belts = mode == "min" ? 2 : 5;
            config = config with
            {
                MinPlanets = planets,
                MaxPlanets = planets,
                MinBelts = belts,
                MaxBelts = belts,
                StartMinDays = mode == "min" ? 50 : 75,
                StartMaxDays = mode == "min" ? 50 : 75,
                Clusters = clusters ? config.Clusters : null,
                Ai = aiConfig,
                Environment = allMapLayers ? config.Environment : null,
                PoiTemplates = allMapLayers ? config.PoiTemplates : null
            };
            var boundaries = clusters && boundaryOnly ? [(Clusters: mode == "min" ? 3 : 5, Stations: mode == "min" ? 10 : 12, Belts: belts)]
                : clusters ? (from c in new[] { 3, 5 } from s in new[] { 10, 12 } from b in new[] { 2, 5 } select (Clusters: c, Stations: s, Belts: b)).ToArray()
                : [(Clusters: 0, Stations: 5, Belts: belts)];
            string economyPath = Option("--economy-report", "");
            object? economyReportRef = null;
            var economicSeeds = new HashSet<ulong>();
            var economicCases = new HashSet<(string Scenario, ulong Seed)>();
            if (clusters && economyPath.Length > 0)
            {
                economyPath = Path.GetFullPath(economyPath);
                using var economy = JsonDocument.Parse(File.ReadAllText(economyPath));
                var e = economy.RootElement;
                if (e.GetProperty("schemaVersion").GetInt32() != 1 || !e.TryGetProperty("commit", out var commit) || string.IsNullOrWhiteSpace(commit.GetString())) throw new ArgumentException("Invalid economy report identity.");
                foreach (var c in e.GetProperty("cases").EnumerateArray())
                {
                    ulong economicSeed = c.GetProperty("seed").GetUInt64();
                    economicSeeds.Add(economicSeed);
                    if (c.TryGetProperty("clusterScenario", out var scenario) && scenario.GetString() is { } scenarioName)
                        economicCases.Add((scenarioName, economicSeed));
                }
                economyReportRef = new
                {
                    path = economyPath,
                    sha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(economyPath))),
                    commit = commit.GetString(),
                    status = e.GetProperty("status").GetString(),
                    seeds = economicSeeds.Order().ToArray(),
                    scenarios = economicCases.Select(c => c.Scenario).Distinct().Order(StringComparer.Ordinal).ToArray(),
                    coverage = "Scenario and seed reference only; economy ship/config runs are independent of this boundary corpus.",
                    shipConfigurations = e.GetProperty("matrix").GetProperty("shipConfigurations").EnumerateArray().Select(c => c.GetProperty("id").GetString()).ToArray(),
                    balance = "not-assessed"
                };
            }
            string baselinePath = Option("--baseline", "");
            object? baselineRef = null;
            if (baselinePath.Length > 0)
            {
                baselinePath = Path.GetFullPath(baselinePath);
                using var baseline = JsonDocument.Parse(File.ReadAllText(baselinePath));
                using var machine = JsonDocument.Parse(JsonSerializer.Serialize(Machine()));
                if (machine.RootElement.EnumerateObject().Any(p => !baseline.RootElement.GetProperty("machine").TryGetProperty(p.Name, out var v) || v.GetRawText() != p.Value.GetRawText()) ||
                    DateTime.UtcNow - File.GetLastWriteTimeUtc(baselinePath) > TimeSpan.FromHours(3) ||
                    baseline.RootElement.GetProperty("config").GetProperty("maxPlanets").GetInt32() != config.MaxPlanets ||
                    baseline.RootElement.GetProperty("config").GetProperty("startMaxDays").GetDouble() != config.StartMaxDays) throw new ArgumentException("Baseline must be fresh and from this host/runtime/max config.");
                baselineRef = new { path = baselinePath, sha256 = Hash(baselinePath), commit = baseline.RootElement.GetProperty("commit").GetString(), comparison = "same host/runtime; inspect per-scenario/config measurements; no automatic speedup claim" };
            }
            string framePath = Option("--client-frame-report", "");
            object? clientFrameReportRef = null;
            if (framePath.Length > 0)
            {
                framePath = Path.GetFullPath(framePath);
                using var frameReport = JsonDocument.Parse(File.ReadAllText(framePath));
                var f = frameReport.RootElement;
                if (f.GetProperty("backend").GetString()?.StartsWith("OpenGL/Skia native window") != true ||
                    f.GetProperty("measuredFrames").GetInt32() <= 0) throw new ArgumentException("Expected actual client frame evidence.");
                clientFrameReportRef = new { path = framePath, sha256 = Hash(framePath), commit = f.GetProperty("commit").GetString(), targetVerdict = f.GetProperty("targetVerdict").GetString(), coverage = "Independent native case; inspect its map counts, layers, config and epoch. Not GPU execution timing." };
            }
            var fields = JsonSerializer.Deserialize<StationResourceFieldConfig>(File.ReadAllText(Path.Combine(client, "Data/World/station-resource-fields.json")))!;
            string scenarioArgument = Option("--scenarios", "all");
            var names = Directory.GetFiles(Path.Combine(client, "Scenarios"), "scenario.json", SearchOption.AllDirectories)
                .Select(p => Path.GetFileName(Path.GetDirectoryName(p))!).Order(StringComparer.Ordinal).ToArray();
            if (scenarioArgument != "all")
            {
                var requested = scenarioArgument.Split(',');
                if (requested.Any(n => !names.Contains(n))) throw new ArgumentException("Unknown scenario.");
                names = requested.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            }
            Directory.CreateDirectory(Path.GetDirectoryName(output!)!);
            measurementsPath = output + "." + Guid.NewGuid().ToString("N") + ".rows.tmp";
            using var measurementsWriter = new StreamWriter(measurementsPath, false, new UTF8Encoding(false));
            var rendering = new List<object>();
            foreach (string name in names)
            {
                var source = ScenarioLoader.LoadFromFile(Path.Combine(client, "Scenarios", name, "scenario.json"));
                foreach (var boundary in boundaries)
                {
                    var caseConfig = config with
                    {
                        MinBelts = boundary.Belts,
                        MaxBelts = boundary.Belts,
                        Clusters = config.Clusters is null ? null : config.Clusters with
                        { MinClusters = boundary.Clusters, MaxClusters = boundary.Clusters, MinStations = boundary.Stations, MaxStations = boundary.Stations }
                    };
                    for (ulong seed = from; ; seed++)
                    {
                        failureRepro = new { scenario = name, seed, config = caseConfig };
                        long allocated = GC.GetAllocatedBytesForCurrentThread();
                        long start = Stopwatch.GetTimestamp();
                        using var engine = new SimulationEngine(registry);
                        engine.ConfigureStationResourceFields(fields);
                        engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = seed, CurrentSpeed = "Speed0" } }, generation: caseConfig);
                        double generationMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                        long generationBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
                        start = Stopwatch.GetTimestamp(); allocated = GC.GetAllocatedBytesForCurrentThread();
                        var snapshot = engine.CaptureSnapshot();
                        double snapshotMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                        long snapshotBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
                        start = Stopwatch.GetTimestamp();
                        allocated = GC.GetAllocatedBytesForCurrentThread();
                        string save = ScenarioLoader.Serialize(engine.CaptureSaveState());
                        double saveSerializationMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                        long saveAllocationBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
                        var placement = engine.CaptureAiPlacementValidation();
                        if ((aiPlacement || allMapLayers) && placement is not { IsValid: true }) throw new InvalidOperationException("Missing or invalid production placement diagnostics.");
                        // Spill each world's detailed proof; a large corpus must not retain millions of checks in memory.
                        measurementsWriter.WriteLine(JsonSerializer.Serialize(new
                        {
                            scenario = name,
                            seed,
                            generatorVersion = 1,
                            saveFormatVersion = SaveFormat.CurrentSaveFormatVersion,
                            objects = snapshot.Objects.Length,
                            mapCounts = Counts(snapshot),
                            epoch = new { calendarTimeMs = snapshot.GameTimeMs, motionTimeMs = snapshot.MotionTimeMs },
                            planets,
                            belts = boundary.Belts,
                            placement = placement is null ? null : new
                            {
                                status = placement.IsValid ? "passed" : "failed",
                                horizonDays = placement.HorizonGameTimeMs / (double)AiTradePlacementValidator.Day,
                                scope = "Geometric horizon; not a guarantee of future safety or an autopilot.",
                                sunExclusionRadiusWorld = placement.SunExclusionRadius,
                                attempts = placement.Attempts,
                                criticalEpochs = placement.CriticalEpochs,
                                components = placement.Connectivity.Select(c => new { epochGameTimeMs = c.EpochGameTimeMs, count = c.Components }).ToArray(),
                                minSampledClearanceWorld = placement.Checks.IsEmpty ? (double?)null : placement.Checks.Min(c => c.Clearance),
                                checkCount = placement.Checks.Length,
                                checksPolicy = aiPlacement ? "complete" : "summary; use --ai-placement for complete checks",
                                placementChecks = !aiPlacement ? null : placement.Checks.OrderBy(c => c.EpochGameTimeMs).ThenBy(c => c.BaseId, StringComparer.Ordinal).ThenBy(c => c.LinkId, StringComparer.Ordinal)
                                    .Select(c => new { epochGameTimeMs = c.EpochGameTimeMs, baseId = c.BaseId, linkId = c.LinkId, clearance = c.Clearance }).ToArray(),
                                violations = placement.Violations
                            },
                            clusterCounts = clusters ? new
                            {
                                clusters = snapshot.ClusterMap!.Clusters.Length,
                                stationsPerCluster = boundary.Stations,
                                stations = snapshot.ClusterMap.Stations.Length,
                                belts = boundary.Belts,
                                resourceAsteroids = snapshot.ClusterMap.ResourceBindings.Length,
                                markets = snapshot.ClusterMap.Stations.Length
                            } : null,
                            economyEvidence = clusters ? economicCases.Contains((name, seed)) ? "linked-by-scenario-and-seed; independent-config; balance-not-assessed" : "missing-scenario-seed-evidence" : null,
                            generationMs,
                            generationBytes,
                            snapshotMs,
                            snapshotBytes,
                            saveSerializationMs,
                            saveAllocationBytes,
                            saveBytes = Encoding.UTF8.GetByteCount(save)
                        }));
                        // Rendering is sampled at the first requested seed for every scenario,
                        // separately from the complete generation/save seed corpus.
                        if (seed == from && (boundaryOnly || !clusters || boundary is (5, 12, 5)))
                            foreach (string view in clusters ? new[] { "system", "belt", "cluster" } : new[] { "system", "belt" })
                                rendering.Add(Render(snapshot, name, seed, view));
                        if (seed == to) break;
                    }
                }
            }
            measurementsWriter.Flush();
            measurementsWriter.Dispose();
            Write(output!, new
            {
                schemaVersion = 1,
                status = clusters ? "measured; profitability-not-assessed" : "passed",
                backend = "CPU/Skia raster",
                machine = Machine(),
                assetRoot = client,
                commit = Revision(root),
                config,
                aiPlacement,
                allMapLayers,
                boundaryOnly,
                layerContent = new { orbits = true, territories = aiConfig is not null, fields = allMapLayers, pointsOfInterest = allMapLayers },
                vsync = "not-applicable; CPU raster has no window",
                clientFrameReportRef,
                criteria = new { generationSnapshotSave = "measured", rasterFrames = "measured", nativePresentation = clientFrameReportRef is null ? "not-measured" : "linked-independent-case", gpuExecution = "not-measured", improvement = baselineRef is null ? "not-measured; missing fresh baseline" : "not-assessed; inspect matched cases" },
                clusterCounts = clusters ? boundaries.Select(b => new { clusters = b.Clusters, stationsPerCluster = b.Stations, belts = b.Belts }).ToArray() : null,
                economyReportRef,
                economicAcceptance = clusters ? economyReportRef is null ? "missing-report; not-assessed" : "linked; profitability-not-assessed; see seed coverage" : null,
                baselineRef,
                seedRange = new { from, to },
                scenarios = names,
                measurements = ReadMeasurements(measurementsPath),
                rendering,
                renderSampling = "First requested seed per scenario; 120 warmup and 600 measured frames for each requested view.",
                generationTiming = "Production LoadScenario pipeline with configured resources; catalog file parsing excluded.",
                presentation = new { status = "not-measured", targetFps = 80, reason = "Raster timings do not measure GPU presentation." }
            });
            return 0;
        }
        catch (Exception ex)
        {
            if (output is not null) Write(output, new { schemaVersion = 1, status = "failed", backend = "CPU/Skia raster", machine = Machine(), repro = failureRepro, error = ex.ToString() });
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            if (measurementsPath is not null) File.Delete(measurementsPath);
        }
    }

    private static IEnumerable<JsonElement> ReadMeasurements(string path)
    {
        foreach (string line in File.ReadLines(path))
        {
            using var document = JsonDocument.Parse(line);
            yield return document.RootElement;
        }
    }

    private static object Render(AuthoritativeSnapshot snapshot, string scenario, ulong seed, string view)
    {
        var buffer = new SnapshotBuffer(); buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        if (view == "system") screen.FitMapView(MapFitMode.System);
        else if (view == "cluster") screen.FitCluster(snapshot.ClusterMap!.StartClusterId);
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
            layers = screen.MapLayers.ToString(),
            mapCounts = Counts(snapshot),
            epoch = new { calendarTimeMs = snapshot.GameTimeMs, motionTimeMs = snapshot.MotionTimeMs },
            warmupFrames = 120,
            measuredFrames = 600,
            p50Ms = ms[299],
            p95Ms = ms[569],
            p99Ms = ms[593],
            meanMs = ms.Average(),
            allocationBytesPerFrame = bytes / 600d
        };
    }

    public static object Counts(AuthoritativeSnapshot snapshot) => new
    {
        authoritativeEntities = snapshot.Objects.Length,
        aiBases = snapshot.AiMap?.Bases.Length ?? 0,
        territories = snapshot.AiMap is { Territories.IsDefaultOrEmpty: false } a ? a.Territories.Length : 0,
        fields = snapshot.AiMap is { Fields.IsDefaultOrEmpty: false } b ? b.Fields.Length : 0,
        pointsOfInterest = snapshot.AiMap is { PointsOfInterest.IsDefaultOrEmpty: false } c ? c.PointsOfInterest.Length : 0,
        configuredBeltDecorationSamples = snapshot.SolarSystemMap?.Belts.Sum(b => Math.Clamp(b.DecorationSamples, 0, 65536)) ?? 0,
        configuredDebrisDecorationSamples = snapshot.AiMap is { Fields.IsDefaultOrEmpty: false } d ? d.Fields.Count(f => f.Kind == "Debris") * 64 : 0,
        decorationMeaning = "Configured sample budgets; LOD/viewport may draw fewer. Metadata and decoration are not entities."
    };

    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream));
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
        using var stream = File.Create(path);
        JsonSerializer.Serialize(stream, report, new JsonSerializerOptions { WriteIndented = true });
    }
}
