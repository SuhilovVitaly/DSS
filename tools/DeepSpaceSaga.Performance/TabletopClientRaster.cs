using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.MainMenu;
using DeepSpaceSaga.Client.UI.Screens.Station;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using SkiaSharp;

internal static class TabletopClientRaster
{
    internal static void Run(string root, string output)
    {
        string allowed = Path.GetFullPath(Path.Combine(root, "../artifacts/validation")) + Path.DirectorySeparatorChar;
        if (!Path.IsPathFullyQualified(output) || !Path.GetFullPath(output).StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Output must be an absolute path under workspace artifacts/validation.", nameof(output));
        output = Path.GetFullPath(output);
        string directory = Path.GetDirectoryName(output)!;
        Directory.CreateDirectory(directory);
        string client = Path.Combine(root, "src/DeepSpaceSaga.Client");
        using var engine = SimulationEngine.CreateFromSettingsFile(Path.Combine(client, "Settings.json"));
        var scenario = ScenarioLoader.LoadFromFile(Path.Combine(client, "Scenarios/Default_500/scenario.json"));
        engine.LoadScenario(scenario with { GameState = scenario.GameState with { MasterSeed = 1, CurrentSpeed = "Speed0" } });
        var snapshot = engine.CaptureSnapshot();
        var buffer = new SnapshotBuffer();
        buffer.Update(snapshot);
        List<object> results = [];
        foreach (var (width, height, scale, language) in new[] { (1280, 720, 1f, "English"), (1920, 1080, 1.5f, "Russian") })
        {
            using var locale = Localization.UseLocaleForTests(language);
            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            Measure("menu", new MainMenuScreen());
            Measure("station", new StationScreen(buffer));
            var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), uiScale: scale);
            screen.Render(surface.Canvas, width, height);
            var camera = (CameraState)typeof(GameSessionScreen)
                .GetField("_camera", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(screen)!;
            double speed = snapshot.ClusterMap?.Geography?.ReferenceSpeedKmS ?? 4;
            foreach (var (detail, days) in new[] { (MapDetailLevel.Local, .12), (MapDetailLevel.Region, 2d),
                (MapDetailLevel.Cluster, 12d), (MapDetailLevel.System, 55d) })
            {
                double ppu = height / (days * speed * 86400 / 300 * 10);
                camera.SetZoom(ppu, Math.Min(CameraState.DefaultMinPpu, ppu), Math.Max(CameraState.DefaultMaxPpu, ppu));
                screen.Render(surface.Canvas, width, height);
                if (screen.MapDetail != detail)
                    throw new InvalidOperationException($"Expected {detail}, rendered {screen.MapDetail}.");
                Measure(detail.ToString().ToLowerInvariant(), screen);
            }

            void Measure(string name, IScreen screen)
            {
                const int warmup = 30, samples = 120;
                var first = Stopwatch.GetTimestamp();
                Draw();
                double firstMs = Stopwatch.GetElapsedTime(first).TotalMilliseconds;
                for (int frame = 0; frame < warmup; frame++)
                    Draw();
                var times = new double[samples];
                for (int frame = 0; frame < samples; frame++)
                {
                    long start = Stopwatch.GetTimestamp();
                    Draw();
                    times[frame] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                }
                Array.Sort(times);
                using var image = surface.Snapshot();
                using var png = image.Encode(SKEncodedImageFormat.Png, 100);
                string path = Path.Combine(directory, $"{name}-{width}-{language}.png");
                using var file = File.Create(path);
                png.SaveTo(file);
                results.Add(new { name, width, height, scale, language, firstMs, p95Ms = times[113], path });

                void Draw()
                {
                    surface.Canvas.Clear(SKColors.Black);
                    screen.Render(surface.Canvas, width, height);
                    surface.Canvas.Flush();
                }
            }
        }
        File.WriteAllText(output, JsonSerializer.Serialize(new { seed = 1, mode = "CPU raster", results },
            new JsonSerializerOptions { WriteIndented = true }));
    }
}
