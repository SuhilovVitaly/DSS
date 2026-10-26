using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.Load;
using DeepSpaceSaga.Client.UI.Screens.MainMenu;
using DeepSpaceSaga.Client.UI.Screens.Save;
using DeepSpaceSaga.Client.UI.Screens.Trade;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using Silk.NET.Windowing;
using SkiaSharp;

internal static class PlatformLayoutNativeEvidence
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    internal static int Run(string[] args)
    {
        if (args.Length != 6) throw new ArgumentException("<repo> <report.json> --platform-layout-window <scale> <WxH> <ru|en>");
        string output = Path.GetFullPath(args[1]), directory = Path.GetDirectoryName(output)!;
        Directory.CreateDirectory(directory);
        string settings = Path.Combine(AppContext.BaseDirectory, "Settings.json");
        byte[] original = File.ReadAllBytes(settings);
        string previousCwd = Environment.CurrentDirectory;
        string? previousLog = Environment.GetEnvironmentVariable("DSS_INTERFACE_LOG_PATH");
        var captures = new List<object>();
        string? failure = null;
        float scale = float.Parse(args[3], CultureInfo.InvariantCulture);
        var size = args[4].Split('x');
        int width = int.Parse(size[0], CultureInfo.InvariantCulture), height = int.Parse(size[1], CultureInfo.InvariantCulture);
        try
        {
            Environment.SetEnvironmentVariable("DSS_INTERFACE_LOG_PATH", Path.Combine(directory, "Interface.log"));
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            var config = JsonNode.Parse(original)!;
            config["gameSettings"]!["uiScale"] = scale;
            config["gameSettings"]!["language"] = args[5] == "ru" ? "Russian" : "English";
            File.WriteAllText(settings, config.ToJsonString());
            string client = Path.Combine(Path.GetFullPath(args[0]), "src", "DeepSpaceSaga.Client");
            string contentSettings = Path.Combine(client, "Settings.json");
            var registry = EngineContentLoader.LoadRegistryFromSettingsFile(contentSettings, out _, out _);
            var source = ScenarioLoader.LoadFromFile(Path.Combine(client, "Scenarios", "Docked", "scenario.json"));
            using var engine = new SimulationEngine(registry, clock: new SimulationClock(SimulationSpeed.Speed0, () => 0));
            engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = 1 } });
            string saves = Path.Combine(directory, "Saves");
            var connection = new LocalGameSessionConnection(engine, saves);
            var buffer = new SnapshotBuffer();
            var handle = new GameSessionHandle(connection, buffer,
                new CombatMetricsWriter(Path.Combine(directory, "CombatMetrics")),
                new DebugTuningFileWriter(Path.Combine(directory, "DebugTuning"), active: false));
            try
            {
                var ready = Stopwatch.StartNew();
                while (buffer.Latest is null && ready.Elapsed.TotalSeconds < 10) Thread.Sleep(10);
                if (buffer.Latest is null) throw new TimeoutException("Local transport bootstrap did not arrive.");
                connection.SaveAsync("ubuntu-layout").AsTask().GetAwaiter().GetResult();
                foreach (string file in Directory.EnumerateFiles(saves))
                    File.SetLastWriteTimeUtc(file, new DateTime(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc));
                var factory = new Factory(contentSettings, client, saves);
                handle.TravelStationAsync(StationDistrict.Market).AsTask().GetAwaiter().GetResult();
                var mapSource = ScenarioLoader.LoadFromFile(Path.Combine(client, "Scenarios", "PlayerShipOnly", "scenario.json"));
                using var mapEngine = new SimulationEngine(registry, clock: new SimulationClock(SimulationSpeed.Speed0, () => 0));
                mapEngine.LoadScenario(mapSource with { GameState = mapSource.GameState with { MasterSeed = 1, CurrentSpeed = "Speed0" } });
                var mapConnection = new LocalGameSessionConnection(mapEngine, saves);
                var mapBuffer = new SnapshotBuffer();
                var mapHandle = new GameSessionHandle(mapConnection, mapBuffer,
                    new CombatMetricsWriter(Path.Combine(directory, "MapMetrics")),
                    new DebugTuningFileWriter(Path.Combine(directory, "MapDebugTuning"), active: false));
                using var mapLifetime = new SessionLease(mapHandle);
                ready.Restart();
                while (mapBuffer.Latest is null && ready.Elapsed.TotalSeconds < 10) Thread.Sleep(10);
                if (mapBuffer.Latest is null) throw new TimeoutException("Map local transport bootstrap did not arrive.");
                var game = new GameSessionScreen(mapBuffer, new LinearMotionPredictor(), mapHandle, uiScale: scale);
                var trade = new TradeScreen(buffer, handle);
                var screens = new (string Name, IScreen Screen)[]
                {
                    ("menu", new MainMenuScreen()),
                    ("save", new SaveScreen(() => factory.ListSaveSlots(), id => connection.SaveAsync(id).AsTask().GetAwaiter().GetResult(), factory.DeleteSaveSlot)),
                    ("load", new LoadScreen(() => factory.ListSaveSlots(), factory.DeleteSaveSlot)),
                    ("trade", trade), ("map", game)
                };
                foreach (var entry in screens)
                {
                    using var window = new SkiaWindow(entry.Screen, factory);
                    var native = (IWindow)typeof(SkiaWindow).GetField("_window", Private)!.GetValue(window)!;
                    if (entry.Screen == trade || entry.Screen == game)
                    {
                        typeof(SkiaWindow).GetField("_session", Private)!.SetValue(window, entry.Screen == game ? mapHandle : handle);
                        if (entry.Screen == game)
                            typeof(SkiaWindow).GetField("_gameSessionScreen", Private)!.SetValue(window, game);
                    }
                    int frame = 0;
                    var watch = Stopwatch.StartNew();
                    bool captured = false;
                    native.Update += _ => { if (watch.Elapsed.TotalSeconds > 60) native.Close(); };
                    window.NativeFrameRendered = (surface, current) =>
                    {
                        if (++frame == 1) { native.Size = new(width, height); return; }
                        if (entry.Screen == trade && frame == 8)
                        {
                            var viewport = TradeLayout.Viewport(width, height);
                            float fit = TradeLayout.FitScale(width, height);
                            var row = TradeLayout.Row(0);
                            float x = viewport.Left + row.MidX * fit, y = viewport.Top + row.MidY * fit;
                            trade.OnMouseDown(x, y, MouseButton.Left); trade.OnMouseUp(x, y);
                        }
                        if (entry.Screen == game && frame == 10) game.OnMouseMove(width / 2f, height / 2f);
                        if (!captured && frame >= 18)
                        {
                            if (entry.Screen == trade && trade.Model.AuthoritativeQuote is null) return;
                            if (!ReferenceEquals(current, entry.Screen))
                                throw new InvalidOperationException("Unexpected active screen: " + current?.GetType().Name);
                            if (entry.Screen == trade && !trade.HasValidVisit)
                                throw new InvalidOperationException("Trade did not retain its actual docked visit.");
                            Capture(surface, entry.Name, entry.Screen);
                            captured = true;
                            if (entry.Screen == game) { game.OnKeyDown(Key.F12); game.OnKeyUp(Key.F12); }
                            else native.Close();
                        }
                        if (entry.Screen == game && frame == 30)
                        { Capture(surface, "debug", game); native.Close(); }
                    };
                    window.Run();
                    if (!captured) throw new TimeoutException("No native capture for " + entry.Name);

                    void Capture(SKSurface surface, string label, IScreen screen)
                    {
                        if (typeof(SkiaWindow).GetField("_grContext", Private)!.GetValue(window) is null)
                            throw new InvalidOperationException("Native OpenGL context is missing.");
                        if (native.Size.X != width || native.Size.Y != height)
                            throw new InvalidOperationException("Native size differs from requested dimensions.");
                        string path = Path.ChangeExtension(output, "." + label + ".png");
                        using var image = surface.Snapshot();
                        using var pixels = SKBitmap.FromImage(image);
                        int coloredSamples = 0;
                        for (int y = 0; y < pixels.Height; y += 16)
                            for (int x = 0; x < pixels.Width; x += 16)
                            {
                                var pixel = pixels.GetPixel(x, y);
                                if (pixel.Red != 0 || pixel.Green != 0 || pixel.Blue != 0) coloredSamples++;
                            }
                        if (coloredSamples < 10) throw new InvalidOperationException("Blank native surface for " + label);
                        if (screen == game && game.CommandsPanel.CommandPanelRows.Any(r => r.CaptionRect.Bottom * scale > height))
                            throw new InvalidOperationException("Module captions extend below the viewport.");
                        if (screen == trade)
                        {
                            var bounds = TradeLayout.Viewport(width, height);
                            if (bounds.Left < 0 || bounds.Top < 0 || bounds.Right > width || bounds.Bottom > height)
                                throw new InvalidOperationException("Trade panel extends beyond the viewport.");
                        }
                        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                        File.WriteAllBytes(path, data.ToArray());
                        captures.Add(new
                        {
                            label,
                            path,
                            screen = screen.GetType().Name,
                            width,
                            height,
                            coloredSamples,
                            renderer = typeof(SkiaWindow).GetField("_graphicsRenderer", Private)!.GetValue(window),
                            quote = screen == trade ? new { trade.Model.SelectedItemId, trade.Model.AuthoritativeQuote?.QuoteId } : null,
                            rows = screen == game ? game.CommandsPanel.CommandPanelRows.Select(r => new
                            {
                                r.Name,
                                r.Opened,
                                left = r.CaptionRect.Left,
                                top = r.CaptionRect.Top,
                                right = r.CaptionRect.Right,
                                bottom = r.CaptionRect.Bottom,
                                r.DetailLines
                            }).ToArray() : null
                        });
                    }
                }
            }
            finally { handle.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
        }
        catch (Exception error) { failure = error.ToString(); }
        finally
        {
            File.WriteAllBytes(settings, original);
            Directory.SetCurrentDirectory(previousCwd);
            Environment.SetEnvironmentVariable("DSS_INTERFACE_LOG_PATH", previousLog);
        }
        var metrics = new List<object>();
        foreach (var role in new[] { ("Ui", UiFonts.Ui), ("UiBold", UiFonts.UiBold), ("Mono", UiFonts.Mono),
                     ("Body", UiFonts.Body), ("BodyBold", UiFonts.BodyBold), ("BodyItalic", UiFonts.BodyItalic) })
        {
            using var paint = new SKPaint { Typeface = role.Item2, TextSize = 12 };
            metrics.Add(new
            {
                role = role.Item1,
                family = role.Item2.FamilyName,
                english = paint.MeasureText("Reload readiness 12.3 s"),
                russian = paint.MeasureText("Точность Броня 12,3 с")
            });
        }
        bool passed = failure is null && captures.Count == 6;
        File.WriteAllText(output, JsonSerializer.Serialize(new
        {
            passed,
            failure,
            scale,
            width,
            height,
            locale = args[5],
            platform = Environment.OSVersion.ToString(),
            captures,
            metrics,
            data = "Seed1 paused production Engine, local transport, real save slot and docked Trade visit.",
            notRun = new[] { "before-font native baseline", "physical input", "auditory acceptance", "human visual acceptance", "native Wayland" }
        }, new JsonSerializerOptions { WriteIndented = true }));
        return passed ? 0 : 1;
    }

    private sealed class SessionLease(GameSessionHandle handle) : IDisposable
    {
        public void Dispose() => handle.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private sealed class Factory(string settings, string client, string saves) : IGameSessionFactory
    {
        public IGameSessionConnection CreateSession() => LocalGameSessionConnection.CreateFromSettingsFile(settings, saves);
        public IGameSessionConnection CreateSessionFromScenario(string path) => LocalGameSessionConnection.CreateFromScenarioFile(settings, path, saves);
        public IGameSessionConnection CreateSessionFromSave(string slotId) =>
            LocalGameSessionConnection.CreateFromSaveFile(settings, Path.Combine(saves, SaveSlotNaming.ToFileName(slotId)), saves);
        public ScenarioInfo[] ListScenarios() => ScenarioRepository.ListScenarios(Path.Combine(client, "Scenarios"));
        public bool HasQuickSave() => File.Exists(Path.Combine(saves, SaveSlotNaming.ToFileName(SaveSlots.Quicksave)));
        public SaveSlotInfo[] ListSaveSlots() => SaveSlotRepository.ListSlots(saves);
        public void DeleteSaveSlot(string slotId) => SaveSlotRepository.DeleteSlot(saves, slotId);
    }
}
