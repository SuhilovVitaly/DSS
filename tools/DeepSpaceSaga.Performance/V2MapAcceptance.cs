using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
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

/// <summary>Reproducible production-renderer evidence. CPU timings never assert native FPS.</summary>
public static class V2MapAcceptance
{
    private static readonly string[] Views = ["player", "station", "cluster", "system"];
    private static readonly string[] Scenarios = ["Default", "Default_500", "Docked", "Undocked", "MarketProfiles", "PlayerShipOnly"];
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    public static string Digest(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static void Write(string path, object value) => File.WriteAllText(path, JsonSerializer.Serialize(value, Json));

    public static int Run(string[] args)
    {
        string original = Directory.GetCurrentDirectory();
        string? output = null;
        try
        {
            if (args.Length < 3) throw new ArgumentException("Expected root, absolute output.json and --map-v2-acceptance.");
            string root = Path.GetFullPath(args[0]);
            output = Path.GetFullPath(args[1]);
            string allowed = Path.GetFullPath(Path.Combine(root, "../artifacts/validation")) + Path.DirectorySeparatorChar;
            if (!Path.IsPathFullyQualified(args[1]) || !output.StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Output must be absolute and under workspace artifacts/validation.");
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            if (!args.Contains("--map-v2-acceptance")) throw new ArgumentException("Missing mode flag.");
            var options = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] == "--map-v2-acceptance") { if (!options.TryAdd(args[i], "true")) throw new ArgumentException("Duplicate flag."); continue; }
                if (args[i] is not ("--seeds" or "--scenarios" or "--viewports" or "--ui-scales" or "--warmup" or "--frames")) throw new ArgumentException("Unknown option: " + args[i]);
                string key = args[i];
                if (++i >= args.Length || args[i].StartsWith("--") || !options.TryAdd(key, args[i])) throw new ArgumentException("Missing or duplicate option: " + key);
            }
            string Option(string key, string fallback) => options.GetValueOrDefault(key, fallback);
            var seeds = Option("--seeds", "1,42,100").Split(',').Select(ulong.Parse).ToArray();
            var names = Option("--scenarios", string.Join(',', Scenarios)).Split(',');
            var viewports = Option("--viewports", "1280x720,1920x1080,3440x1440,5120x1440").Split(',').Select(v =>
            {
                var parts = v.Split('x');
                if (parts.Length != 2) throw new ArgumentException("Invalid viewport.");
                int w = int.Parse(parts[0]), h = int.Parse(parts[1]);
                if (w < 640 || h < 480 || w > 5120 || h > 2160) throw new ArgumentException("Viewport out of bounds.");
                return (Width: w, Height: h);
            }).ToArray();
            var scales = Option("--ui-scales", "1,1.5,2").Split(',').Select(v => float.Parse(v, CultureInfo.InvariantCulture)).ToArray();
            int warmup = int.Parse(Option("--warmup", "5")), frames = int.Parse(Option("--frames", "30"));
            if (seeds.Length is < 1 or > 100 || seeds.Distinct().Count() != seeds.Length || names.Length == 0 || names.Distinct().Count() != names.Length || names.Any(n => !Scenarios.Contains(n)) ||
                scales.Length == 0 || scales.Distinct().Count() != scales.Length || scales.Any(s => s is not (1f or 1.5f or 2f)) || viewports.Distinct().Count() != viewports.Length || warmup is < 1 or > 120 || frames is < 2 or > 600)
                throw new ArgumentException("Invalid or duplicate matrix values/sample counts.");
            string client = Path.Combine(root, "src/DeepSpaceSaga.Client");
            Directory.SetCurrentDirectory(client);
            string settings = Path.Combine(client, "Settings.json");
            var registry = EngineContentLoader.LoadRegistryFromSettingsFile(settings, out _, out _);
            var config = EngineContentLoader.LoadSolarSystemGenerationConfig(settings)!;
            if (config.GeneratorVersion != 2) throw new ArgumentException("Expected shipped generation v2.");
            using var settingsJson = JsonDocument.Parse(File.ReadAllText(settings));
            var mapSettings = settingsJson.RootElement.GetProperty("gameSettings").GetProperty("tacticalMap").Deserialize<TacticalMapSettings>(new JsonSerializerOptions
            { PropertyNameCaseInsensitive = true, UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow })!.Validate();
            var fields = JsonSerializer.Deserialize<StationResourceFieldConfig>(File.ReadAllText(Path.Combine(client, "Data/World/station-resource-fields.json")))!;
            string directory = Path.Combine(Path.GetDirectoryName(output)!, Path.GetFileNameWithoutExtension(output) + "-evidence");
            if (Directory.Exists(directory)) throw new ArgumentException("Evidence directory already exists; use a unique output.");
            Directory.CreateDirectory(directory);
            var worlds = new List<object>(); var rows = new List<object>();
            foreach (string scenario in names)
                foreach (ulong seed in seeds)
                {
                    var source = ScenarioLoader.LoadFromFile(Path.Combine(client, "Scenarios", scenario, "scenario.json"));
                    using var engine = new SimulationEngine(registry);
                    engine.ConfigureStationResourceFields(fields);
                    long allocated = GC.GetAllocatedBytesForCurrentThread(), start = Stopwatch.GetTimestamp();
                    engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = seed, CurrentSpeed = "Speed0" } }, generation: config);
                    double generationMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                    long generationBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
                    var snapshot = engine.CaptureSnapshot();
                    var map = snapshot.ClusterMap!;
                    double unitsPerDay = map.Geography!.ReferenceSpeedKmS * 86400 / 300 * 10;
                    var poses = snapshot.Objects.ToDictionary(o => o.ObjectId);
                    var stations = map.Stations.Select(s => poses[s.ObjectId]).ToArray();
                    double minimum = stations.SelectMany((a, i) => stations.Skip(i + 1).Select(b => double.Hypot(a.X - b.X, a.Y - b.Y) / unitsPerDay)).Min();
                    var nearest = map.Clusters.SelectMany(c => c.StationIds.Select(id => c.StationIds.Where(other => other != id).Min(other => double.Hypot(poses[id].X - poses[other].X, poses[id].Y - poses[other].Y) / unitsPerDay))).ToArray();
                    if (minimum < 1 || nearest.Any(d => d < 1 || d > 3)) throw new InvalidDataException("Human geography violates v2 distances.");
                    string world = Path.Combine(directory, $"{scenario}-{seed}-snapshot.json");
                    Write(world, snapshot);
                    worlds.Add(new
                    {
                        scenario,
                        seed,
                        generationVersion = snapshot.SolarSystemMap!.GeneratorVersion,
                        referenceSpeedKmS = map.Geography.ReferenceSpeedKmS,
                        unitsPerDay,
                        minimumDays = minimum,
                        nearestMinimumDays = nearest.Min(),
                        nearestMaximumDays = nearest.Max(),
                        generationMs,
                        generationBytes,
                        retries = new { solarAndCluster = "not-exposed", configuredSolarAttemptLimit = config.MaxPlacementAttempts, ai = engine.CaptureAiPlacementValidation() },
                        snapshot = world,
                        snapshotSha256 = Digest(world)
                    });
                    foreach (var viewport in viewports)
                        foreach (float scale in scales)
                            foreach (string view in Views)
                            {
                                string station = map.Clusters.First(c => c.Id == map.StartClusterId).StationIds[0];
                                var buffer = new SnapshotBuffer(); buffer.Update(view == "station" ? snapshot with { SelectedObjectId = station } : snapshot);
                                long clock = 0;
                                var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => clock, uiScale: scale, mapSettings: mapSettings);
                                if (screen.UiScale != scale) throw new InvalidDataException("Requested UI scale was not applied.");
                                using var bitmap = new SKBitmap(viewport.Width, viewport.Height); using var canvas = new SKCanvas(bitmap);
                                screen.Render(canvas, viewport.Width, viewport.Height);
                                // Use real overlay controls to make the map available on small/high-DPI windows.
                                void Click(SKRect rect) { screen.OnMouseDown(rect.MidX * scale, rect.MidY * scale); screen.OnMouseUp(rect.MidX * scale, rect.MidY * scale); }
                                Click(screen.LastCloseRect); Click(screen.CommandsPanel.HideShowButtonRect);
                                if (view is "cluster" or "system") Click(screen.ObjectInfoPanel.HideShowButtonRect);
                                screen.Render(canvas, viewport.Width, viewport.Height);
                                if (view == "station" && !screen.FocusStation(station)) throw new InvalidDataException("Station focus failed.");
                                if (view == "cluster" && !screen.FitCluster(map.StartClusterId)) throw new InvalidDataException("Cluster fit failed.");
                                if (view == "system" && !screen.FitMapView(MapFitMode.System)) throw new InvalidDataException("System fit failed.");
                                void Frame() { clock += Stopwatch.Frequency / 80; screen.Render(canvas, viewport.Width, viewport.Height); canvas.Flush(); }
                                for (int i = 0; i < warmup; i++) Frame();
                                var samples = new double[frames]; allocated = GC.GetAllocatedBytesForCurrentThread();
                                for (int i = 0; i < frames; i++) { start = Stopwatch.GetTimestamp(); Frame(); samples[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
                                long bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
                                double ppu = screen.CameraPixelsPerWorldUnit;
                                double shortHours = Math.Min(viewport.Width, viewport.Height) / ppu / unitsPerDay * 24;
                                double diagonalDays = double.Hypot(viewport.Width, viewport.Height) / ppu / unitsPerDay;
                                if (view is "player" or "station" && (screen.MapDetail != MapDetailLevel.Local || shortHours > 4.0000001 || diagonalDays >= 1)) throw new InvalidDataException("Local camera violates contract.");
                                string image = Path.Combine(directory, $"{scenario}-{seed}-{viewport.Width}x{viewport.Height}-{scale.ToString(CultureInfo.InvariantCulture)}-{view}.png");
                                using (var data = bitmap.Encode(SKEncodedImageFormat.Png, 100)) using (var file = File.Create(image)) data.SaveTo(file);
                                Array.Sort(samples);
                                rows.Add(new
                                {
                                    scenario,
                                    seed,
                                    view,
                                    width = viewport.Width,
                                    height = viewport.Height,
                                    uiScale = scale,
                                    detailLevel = screen.MapDetail.ToString(),
                                    camera = new { x = screen.CameraFocusX, y = screen.CameraFocusY, ppu, shortHours, diagonalDays },
                                    labels = screen.MapLabels.Count,
                                    aggregates = screen.MapClusterCount,
                                    layers = screen.MapLayers.ToString(),
                                    overlay = "diagnostics closed; commands collapsed; object info collapsed for cluster/system",
                                    image,
                                    imageSha256 = Digest(image),
                                    warmupFrames = warmup,
                                    measuredFrames = frames,
                                    p50Ms = samples[(int)Math.Ceiling(frames * .5) - 1],
                                    p95Ms = samples[(int)Math.Ceiling(frames * .95) - 1],
                                    p99Ms = samples[(int)Math.Ceiling(frames * .99) - 1],
                                    allocationBytesPerFrame = bytes / (double)frames
                                });
                            }
                }
            Write(output, new
            {
                schemaVersion = 2,
                status = "raster-evidence-complete",
                backend = "CPU/Skia raster",
                generationVersion = 2,
                commit = SolarMapEvidence.Revision(root),
                sourceAssemblySha256 = Digest(typeof(V2MapAcceptance).Assembly.Location),
                settingsSha256 = Digest(settings),
                config,
                machine = SolarMapEvidence.Machine(),
                invocation = args,
                matrix = new { scenarios = names, seeds, viewports = viewports.Select(v => new { width = v.Width, height = v.Height }), uiScales = scales, views = Views },
                worlds,
                rendering = rows,
                presentation = new { status = "not-measured", targetFps = 80, p95TargetMs = 12.5, reason = "CPU raster is not native presentation." },
                baseline = "OPEN: no matched pre-change native baseline",
                humanAcceptance = "NOT RUN",
                clock = "deterministic UI clock at 80Hz; CPU timings use Stopwatch"
            });
            if (!Validate(output, out string error)) throw new InvalidDataException(error);
            return 0;
        }
        catch (Exception ex)
        {
            // Invalid paths must not cause diagnostic writes outside the authorized artifact root.
            if (output is not null && Directory.Exists(Path.GetDirectoryName(output)) && output.StartsWith(Path.GetFullPath(Path.Combine(args[0], "../artifacts/validation")) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                Write(output, new { schemaVersion = 2, status = "failed", error = ex.ToString() });
            Console.Error.WriteLine(ex); return 1;
        }
        finally { Directory.SetCurrentDirectory(original); }
    }

    public static bool Validate(string path, out string error)
    {
        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(path)); var r = json.RootElement;
            if (r.GetProperty("status").GetString() != "raster-evidence-complete" || r.GetProperty("generationVersion").GetInt32() != 2 || r.GetProperty("backend").GetString() != "CPU/Skia raster" || r.GetProperty("presentation").GetProperty("status").GetString() != "not-measured" || r.GetProperty("commit").GetString()?.Length != 40 || r.GetProperty("machine").GetProperty("logicalProcessors").GetInt32() < 1) throw new InvalidDataException("Report identity/backend missing.");
            var matrix = r.GetProperty("matrix");
            int count = matrix.GetProperty("scenarios").GetArrayLength() * matrix.GetProperty("seeds").GetArrayLength() * matrix.GetProperty("viewports").GetArrayLength() * matrix.GetProperty("uiScales").GetArrayLength() * 4;
            var worlds = new Dictionary<(string, ulong), double>();
            foreach (var w in r.GetProperty("worlds").EnumerateArray())
            {
                string file = w.GetProperty("snapshot").GetString()!;
                if (Digest(file) != w.GetProperty("snapshotSha256").GetString()) throw new InvalidDataException("World evidence hash mismatch.");
                var snapshot = JsonSerializer.Deserialize<AuthoritativeSnapshot>(File.ReadAllText(file))!;
                if (snapshot.SolarSystemMap?.GeneratorVersion != 2 || snapshot.SolarSystemMap.Seed != w.GetProperty("seed").GetUInt64() || snapshot.ClusterMap?.RulesVersion != 2 || snapshot.ClusterMap.Geography is null) throw new InvalidDataException("World version/seed missing or inconsistent.");
                double units = snapshot.ClusterMap.Geography.ReferenceSpeedKmS * 86400 / 300 * 10;
                var poses = snapshot.Objects.ToDictionary(o => o.ObjectId);
                var human = snapshot.ClusterMap.Stations.Select(s => poses[s.ObjectId]).ToArray();
                double minimum = human.SelectMany((a, i) => human.Skip(i + 1).Select(b => double.Hypot(a.X - b.X, a.Y - b.Y) / units)).Min();
                if (minimum < 1 || Math.Abs(minimum - w.GetProperty("minimumDays").GetDouble()) > 1e-9) throw new InvalidDataException("Geometry mismatch.");
                foreach (var cluster in snapshot.ClusterMap.Clusters)
                    foreach (string id in cluster.StationIds)
                    {
                        double nearest = cluster.StationIds.Where(other => other != id).Min(other => double.Hypot(poses[id].X - poses[other].X, poses[id].Y - poses[other].Y) / units);
                        if (nearest < 1 || nearest > 3) throw new InvalidDataException("Nearest geometry mismatch.");
                    }
                worlds.Add((w.GetProperty("scenario").GetString()!, w.GetProperty("seed").GetUInt64()), units);
            }
            var keys = new HashSet<string>();
            foreach (var row in r.GetProperty("rendering").EnumerateArray())
            {
                string scenario = row.GetProperty("scenario").GetString()!, view = row.GetProperty("view").GetString()!;
                ulong seed = row.GetProperty("seed").GetUInt64(); int width = row.GetProperty("width").GetInt32(), height = row.GetProperty("height").GetInt32();
                float scale = row.GetProperty("uiScale").GetSingle();
                if (!matrix.GetProperty("scenarios").EnumerateArray().Any(s => s.GetString() == scenario) || !matrix.GetProperty("seeds").EnumerateArray().Any(s => s.GetUInt64() == seed) || !Views.Contains(view) ||
                    !matrix.GetProperty("viewports").EnumerateArray().Any(v => v.GetProperty("width").GetInt32() == width && v.GetProperty("height").GetInt32() == height) || !matrix.GetProperty("uiScales").EnumerateArray().Any(s => s.GetSingle() == scale) ||
                    !keys.Add($"{scenario}/{seed}/{view}/{width}/{height}/{scale}")) throw new InvalidDataException("Missing, extra or duplicate matrix case.");
                string image = row.GetProperty("image").GetString()!;
                if (Digest(image) != row.GetProperty("imageSha256").GetString()) throw new InvalidDataException("Image missing or changed.");
                using var codec = SKCodec.Create(image);
                if (codec is null || codec.Info.Width != width || codec.Info.Height != height) throw new InvalidDataException("Image dimensions mismatch.");
                double units = worlds[(scenario, seed)], ppu = row.GetProperty("camera").GetProperty("ppu").GetDouble();
                double hours = Math.Min(width, height) / ppu / units * 24, diagonal = double.Hypot(width, height) / ppu / units;
                if (!double.IsFinite(ppu) || ppu <= 0 || !double.IsFinite(row.GetProperty("camera").GetProperty("x").GetDouble()) || !double.IsFinite(row.GetProperty("camera").GetProperty("y").GetDouble()) || Math.Abs(hours - row.GetProperty("camera").GetProperty("shortHours").GetDouble()) > 1e-8 || Math.Abs(diagonal - row.GetProperty("camera").GetProperty("diagonalDays").GetDouble()) > 1e-8 ||
                    view is "player" or "station" && (hours > 4.0000001 || diagonal >= 1 || row.GetProperty("detailLevel").GetString() != "Local") || row.GetProperty("measuredFrames").GetInt32() < 2 || row.GetProperty("p95Ms").GetDouble() <= 0)
                    throw new InvalidDataException("Camera or timing evidence inconsistent.");
            }
            if (keys.Count != count) throw new InvalidDataException("Incomplete matrix.");
            error = ""; return true;
        }
        catch (Exception ex) { error = ex.Message; return false; }
    }
}
