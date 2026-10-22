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
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using Silk.NET.Windowing;
using SkiaSharp;

internal static class ModulePanelNativeEvidence
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    internal static int Run(string[] args)
    {
        if (args.Length != 6) throw new ArgumentException("<repo> <report.json> --module-panels-window <scale> <WxH> <ru|en>");
        string settings = Path.Combine(AppContext.BaseDirectory, "Settings.json");
        byte[] original = File.ReadAllBytes(settings);
        var config = JsonNode.Parse(original)!;
        config["gameSettings"]!["uiScale"] = float.Parse(args[3], CultureInfo.InvariantCulture);
        config["gameSettings"]!["language"] = args[5] == "ru" ? "Russian" : "English";
        try { File.WriteAllText(settings, config.ToJsonString()); return RunCore(args); }
        finally { File.WriteAllBytes(settings, original); }
    }

    private static int RunCore(string[] args)
    {
        string output = Path.GetFullPath(args[1]), directory = Path.GetDirectoryName(output)!;
        Directory.CreateDirectory(directory); Directory.SetCurrentDirectory(directory);
        string client = Path.Combine(Path.GetFullPath(args[0]), "src/DeepSpaceSaga.Client");
        float scale = float.Parse(args[3], CultureInfo.InvariantCulture);
        var dimensions = args[4].Split('x'); int width = int.Parse(dimensions[0]), height = int.Parse(dimensions[1]);
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(client, "Settings.json"), out _, out _);
        var source = ScenarioLoader.LoadFromFile(Path.Combine(client, "Scenarios/PlayerShipOnly/scenario.json"));
        using var engine = new SimulationEngine(registry, clock: new SimulationClock(SimulationSpeed.Speed0, () => 0));
        engine.LoadScenario(source with
        {
            GameState = source.GameState with
            {
                CurrentSpeed = "Speed0",
                SpaceObjects = source.GameState.SpaceObjects.Select(o => o with
                {
                    Ai = null,
                    PositionX = o.ObjectId == source.GameState.PlayerShipObjectId ? 0 : 300,
                    PositionY = 0,
                    SpeedMps = o.ObjectId == source.GameState.PlayerShipObjectId ? 3000 : 0,
                    DirectionDegrees = 90,
                    MovementType = "Linear",
                    Crew = o.Crew!.Select(c => c with { AccuracySkill = 20 }).ToArray(),
                    Modules = o.Modules!.Select(m => m with { PowerState = o.ObjectId == source.GameState.PlayerShipObjectId ? "On" : "Off" }).ToArray()
                }).ToArray()
            }
        });
        var world = engine.CaptureSnapshot(advanceClock: false);
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(world);
        var driver = new Driver(engine); var handle = new GameSessionHandle(driver, buffer);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), handle, uiScale: scale, timestampProvider: () => 0,
            combatSettings: CombatVisualSettings.Load(Path.Combine(client, CombatVisualSettings.RelativePath)),
            combatInputs: CombatInputSettings.Load(Path.Combine(client, CombatInputSettings.RelativePath)));
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_EXIT", "0");
        using var window = new SkiaWindow(screen, new Factory());
        typeof(SkiaWindow).GetField("_session", Private)!.SetValue(window, handle);
        typeof(SkiaWindow).GetField("_gameSessionScreen", Private)!.SetValue(window, screen);
        var native = (IWindow)typeof(SkiaWindow).GetField("_window", Private)!.GetValue(window)!;
        var checks = new Dictionary<string, bool>(); var captures = new List<string>(); var rowsAtCapture = new List<object>();
        string? failure = null; bool completed = false; int frame = 0, stage = 0;
        var watch = Stopwatch.StartNew(); (float X, float Y) aim = default;
        window.NativeFrameRendered = (surface, _) =>
        {
            try
            {
                if (watch.Elapsed.TotalSeconds > 60) throw new TimeoutException("Native panel evidence watchdog.");
                if (++frame == 1) { native.Size = new(width, height); return; }
                if (frame < 5 || frame % 3 != 0) return;
                var rows = screen.CommandsPanel.CommandPanelRows;
                var torpedo = rows.First(r => r.Weapon?.ModuleId == "MOD-PLAYER-TORPEDO-01");
                switch (stage++)
                {
                    case 0:
                        checks["openGl"] = typeof(SkiaWindow).GetField("_grContext", Private)!.GetValue(window) is not null;
                        checks["dimensions"] = native.Size.X == width && native.Size.Y == height;
                        checks["locale"] = Localization.Combat("Panel.Engine") == (args[5] == "ru" ? "Двигатель" : "Engine");
                        if (screen.IsPanelVisible) Click(screen.LastCloseRect);
                        break;
                    case 1:
                        checks["torpedoPriority"] = torpedo.Opened && (height / scale >= 600 || !rows.Any(r => r.Name == "Navigation" && r.Opened));
                        checks["profileRows"] = torpedo.DetailLines.Count(l => l.Contains('\t')) == 3;
                        checks["referenceSigma"] = torpedo.OperatorText!.Replace(',', '.').Contains("0.42");
                        checks["rangeAndLimit"] = torpedo.RangeKm == 70 && world.DestroyedModuleLimit == 3;
                        checks["captionsFit"] = rows.All(r => r.CaptionRect.Bottom * scale <= native.Size.Y);
                        Capture(surface, "panels");
                        Tap(Key.Q);
                        break;
                    case 2:
                        var free = screen.AvailableMapRect();
                        aim = (free.MidX, free.MidY); screen.OnMouseMove(aim.X, aim.Y);
                        break;
                    case 3:
                        checks["aimPreview"] = screen.PointAim?.ProfileId == "medium" && screen.CurrentTorpedoAimPreview is not null;
                        Capture(surface, "aim");
                        screen.OnMouseDown(aim.X, aim.Y); screen.OnMouseUp(aim.X, aim.Y);
                        world = engine.CaptureSnapshot(advanceClock: false); buffer.Update(world);
                        break;
                    case 4:
                        checks["actualLaunch"] = driver.Commands == 1 && world.Objects.Any(o => o.OwnProjectile?.Kind == ProjectileKind.Torpedo);
                        checks["reloadTimer"] = torpedo.ReloadTimerText == "40 " + Localization.Combat("Panel.Seconds");
                        var button = torpedo.Buttons[0]; screen.OnMouseMove(button.Rect.MidX * scale, button.Rect.MidY * scale);
                        break;
                    case 5:
                        checks["hoverKeepsStatus"] = torpedo.HoverText is not null && torpedo.StatusBarText?.Contains("40") == true;
                        Capture(surface, "reload"); Tap(Key.Q); screen.OnMouseMove(aim.X, aim.Y);
                        screen.OnMouseDown(aim.X, aim.Y); screen.OnMouseUp(aim.X, aim.Y);
                        checks["blockedAim"] = driver.Commands == 1 && screen.PointAimFeedbackIsError;
                        Tap(Key.Escape);
                        var engineRow = rows.First(r => r.Name == "Engine"); if (!engineRow.Opened) Click(engineRow.CaptionRect);
                        break;
                    case 6:
                        var propulsion = rows.First(r => r.Name == "Engine");
                        checks["engineTimes"] = propulsion.Opened && propulsion.DetailLines.Any(l => l.Contains(": 13 ")) && propulsion.DetailLines.Any(l => l.Contains(": 18 "));
                        Capture(surface, "engine"); completed = true; native.Close(); break;
                }
            }
            catch (Exception error) { failure = error.ToString(); native.Close(); }
        };
        try { window.Run(); }
        finally { screen.OnDeactivated(); handle.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
        bool passed = completed && failure is null && checks.Values.All(v => v);
        File.WriteAllText(output, JsonSerializer.Serialize(new
        {
            passed,
            completed,
            failure,
            frame,
            scale,
            locale = args[5],
            width,
            height,
            checks,
            captures,
            rowsAtCapture,
            renderer = typeof(SkiaWindow).GetField("_graphicsRenderer", Private)!.GetValue(window),
            input = "Managed production screen callbacks; actual Engine admission and GPU capture.",
            notRun = new[] { "physical keyboard/mouse", "human visual acceptance", "independent review" }
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(output); return passed ? 0 : 1;

        void Tap(Key key) { screen.OnKeyDown(key); screen.OnKeyUp(key); }
        void Click(SKRect rect) { screen.OnMouseDown(rect.MidX * scale, rect.MidY * scale); screen.OnMouseUp(rect.MidX * scale, rect.MidY * scale); }
        void Capture(SKSurface surface, string label)
        {
            string path = Path.ChangeExtension(output, "." + label + ".png");
            using var image = surface.Snapshot(); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(path, data.ToArray()); captures.Add(path);
            rowsAtCapture.Add(new
            {
                label,
                rows = screen.CommandsPanel.CommandPanelRows.Select(r => new
                {
                    r.Name,
                    r.Opened,
                    caption = new { r.CaptionRect.Left, r.CaptionRect.Top, r.CaptionRect.Right, r.CaptionRect.Bottom },
                    body = new { r.BodyRect.Left, r.BodyRect.Top, r.BodyRect.Right, r.BodyRect.Bottom },
                    r.DetailLines,
                    r.ReloadTimerText
                })
            });
        }
    }

    private sealed class Driver(SimulationEngine engine) : IGameSessionConnection
    {
        private readonly Channel<AuthoritativeSnapshot> _channel = Channel.CreateUnbounded<AuthoritativeSnapshot>();
        internal int Commands;
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) { Commands++; engine.ReceiveCommand(command); return ValueTask.CompletedTask; }
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
