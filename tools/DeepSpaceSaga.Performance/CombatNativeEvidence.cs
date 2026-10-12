using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Defeat;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Client.UI.Screens.Load;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using Silk.NET.Windowing;
using SkiaSharp;

internal static class CombatNativeEvidence
{
    private const string Player = "SPC-0001", First = "MOD-PLAYER-COUNTERMEASURE-01", Second = "MOD-PLAYER-COUNTERMEASURE-02";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Get<T>(object o, string name) => (T)o.GetType().GetField(name, Private)!.GetValue(o)!;
    private static void Set(object o, string name, object value) => o.GetType().GetField(name, Private)!.SetValue(o, value);

    internal static int Run(string[] args)
    {
        if (args.Length < 5 || args[3] is not ("1" or "1.2" or "1.5") || args[4] is not ("1280x720" or "1920x1080") ||
            args.Contains("--no-solution") && args.Contains("--recovery"))
            throw new ArgumentException("Use a supported scale/viewport; no-solution and recovery are separate cases.");
        string path = Path.Combine(AppContext.BaseDirectory, "Settings.json");
        byte[]? original = File.Exists(path) ? File.ReadAllBytes(path) : null;
        var config = original is null ? new JsonObject() : JsonNode.Parse(original)!;
        config["gameSettings"] ??= new JsonObject();
        config["gameSettings"]!["uiScale"] = float.Parse(args[3], CultureInfo.InvariantCulture);
        try { File.WriteAllText(path, config.ToJsonString()); return RunCore(args); }
        finally { if (original is null) File.Delete(path); else File.WriteAllBytes(path, original); }
    }

    private static int RunCore(string[] args)
    {
        if (args.Length < 5) throw new ArgumentException("<root> <report.json> --combat-window <scale> <width>x<height> [--recovery]");
        string root = Path.GetFullPath(args[0]), output = Path.GetFullPath(args[1]);
        float scale = float.Parse(args[3], CultureInfo.InvariantCulture);
        var size = args[4].Split('x'); int width = int.Parse(size[0]), height = int.Parse(size[1]);
        bool recovery = args.Contains("--recovery"), noSolution = args.Contains("--no-solution");
        string client = Path.Combine(root, "src/DeepSpaceSaga.Client"), settings = Path.Combine(client, "Settings.json");
        Directory.SetCurrentDirectory(client);
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        string slots = Path.ChangeExtension(output, ".slots"); Directory.CreateDirectory(slots);
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(settings, out _, out _);
        if (noSolution)
        {
            var baseline = registry;
            static IEnumerable<T> All<T>(TypeRegistry<T> r) where T : ITypeDefinition => Enumerable.Range(0, r.Count).Select(r.GetDefinition);
            registry = GameDataRegistry.Create(All(baseline.ModuleCategories), All(baseline.ModuleTypes).Select(m => m.CountermeasureAccuracy is null ? m : m with
            { CountermeasureAccuracy = 0, CountermeasureSpeedKmS = .01, CountermeasureMaxFlightTimeMs = 1000 }),
                All(baseline.ItemTypes), All(baseline.CommandDefinitions), All(baseline.FactoryTypes), All(baseline.Recipes), All(baseline.Dialogues), All(baseline.Quests),
                stationMarketProfiles: All(baseline.StationMarketProfiles), shipClasses: All(baseline.ShipClasses), stationMarketEvents: All(baseline.StationMarketEvents));
        }
        var source = ScenarioLoader.LoadFromFile(Path.Combine(client, "Scenarios/PlayerShipOnly/scenario.json"));
        var ships = source.GameState.SpaceObjects.Select(o => o with
        {
            PositionX = 10000,
            PositionY = o.ObjectId == Player ? 10000 : 9200,
            SpeedMps = 0,
            DirectionDegrees = o.ObjectId == Player ? 0 : 180,
            MovementType = "Stationary",
            Modules = o.Modules!.Select(m => m with { AutoDefenseEnabled = false, PowerState = m.ModuleTypeId == "module.engine.basic" ? "Off" : "On" }).ToArray()
        }).ToList();
        var enemy = ships.Single(o => o.ObjectId != Player);
        for (int i = 1; i < 8; i++)
        {
            string prefix = "extra-" + i + "-";
            ships.Add(enemy with
            {
                ObjectId = prefix + enemy.ObjectId,
                PositionX = 10000 + i * 120,
                Crew = enemy.Crew!.Select(c => c with { CrewId = prefix + c.CrewId }).ToArray(),
                Modules = enemy.Modules!.Select(m => m with { ModuleId = prefix + m.ModuleId, OperatorCrewId = m.OperatorCrewId is null ? null : prefix + m.OperatorCrewId }).ToArray()
            });
        }
        long now = 0;
        var engine = new SimulationEngine(registry, clock: new SimulationClock(SimulationSpeed.Speed0, () => Interlocked.Read(ref now)));
        engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = 42, SpaceObjects = ships.ToArray() } });
        engine.SetSpeed(SimulationSpeed.Speed1); now = 1; engine.SetSpeed(SimulationSpeed.Speed0);
        string target = engine.CaptureSnapshot().Objects.First(o => o.Torpedo is not null).ObjectId;
        engine.SetObjectInteractionState(null, target);
        var connection = new LocalGameSessionConnection(engine, slots);
        long Timestamp() => Interlocked.Read(ref now) * Stopwatch.Frequency / 1000;
        var handle = new GameSessionHandle(connection, new SnapshotBuffer(Timestamp));
        handle.Buffer.Update(engine.CaptureSnapshot());
        var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle, timestampProvider: Timestamp, uiScale: scale);
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_REPORT", output);
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_COMMIT", SolarMapEvidence.Revision(root));
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_CASE", $"EP8 actual-session custom initial fixture;8 enemies;two manual PR;scale={scale};recovery={recovery}");
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_WINDOW", args[4]);
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_IMAGE", Path.ChangeExtension(output, ".png"));
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_EXIT", "1");
        using var window = new SkiaWindow(screen, new Factory(settings, slots));
        Set(window, "_session", handle); Set(window, "_gameSessionScreen", screen);
        var native = Get<IWindow>(window, "_window");
        var checks = new Dictionary<string, bool>();
        var stopwatch = Stopwatch.StartNew();
        int frame = 0, step = 0, lastStepFrame = 0;
        Task? pending = null;
        CountermeasurePlan? preview = null;
        string? failure = null;
        window.NativeFrameRendered = (_, renderedScreen) =>
        {
            try
            {
                frame++;
                if (stopwatch.Elapsed > TimeSpan.FromMinutes(2)) throw new TimeoutException($"Native script stalled at step {step}.");
                if (pending is { IsCompleted: false }) return;
                pending?.GetAwaiter().GetResult(); pending = null;
                if (frame - lastStepFrame < 3) return;
                var snapshot = handle.Buffer.Latest!.Snapshot;
                var stack = Get<ScreenStack>(window, "_screens");
                bool ready = true;
                switch (step)
                {
                    case 0:
                        if (screen.IsPanelVisible) Click(screen.LastCloseRect);
                        screen.FitMapView(MapFitMode.Target); break;
                    case 1: Open(First); break;
                    case 2: Open(Second); break;
                    case 3: ready = Button(First, DefenseCommandTypes.Enable); break;
                    case 4:
                        ready = snapshot.InstalledModules.Single(m => m.ModuleId == First).Defense!.AutoEnabled;
                        if (ready) { checks["independentToggle"] = !snapshot.InstalledModules.Single(m => m.ModuleId == Second).Defense!.AutoEnabled; ready = Button(First, DefenseCommandTypes.Disable); }
                        break;
                    case 5:
                        ready = !snapshot.InstalledModules.Single(m => m.ModuleId == First).Defense!.AutoEnabled;
                        if (ready) ready = Hover(First); break;
                    case 6:
                        preview = screen.ManualDefensePreviewPlan;
                        checks["selectedTarget"] = screen.SelectedObjectId == target;
                        checks["preview"] = preview is not null && (noSolution ? preview.PredictedEncounterMotionTimeMs is null && screen.ManualDefensePreviewGeometry?.Intercept is null : preview.PredictedEncounterMotionTimeMs is not null);
                        SnapshotImage("preview"); ready = Button(First, DefenseCommandTypes.Fire); break;
                    case 7:
                        var flight = snapshot.Objects.FirstOrDefault(o => o.Countermeasure?.LauncherModuleId == First)?.Countermeasure;
                        ready = flight is not null;
                        if (ready) { checks["previewEqualsLaunch"] = JsonSerializer.Serialize(preview!.Route) == JsonSerializer.Serialize(flight!.Route); ready = Button(Second, DefenseCommandTypes.Fire); }
                        break;
                    case 8:
                        ready = snapshot.Objects.Count(o => o.Countermeasure is not null) == 2;
                        if (ready) { checks["capturedChance"] = snapshot.Objects.Where(o => o.Countermeasure is not null).All(o => o.Countermeasure!.FrozenChanceTenths == (noSolution ? 0 : 500)); checks["twoModules"] = snapshot.Objects.Count(o => o.Torpedo is not null) == 8 && snapshot.MotionTimeMs == 1; pending = connection.SaveAsync("before").AsTask(); }
                        break;
                    case 9:
                        double zoom = screen.CameraPixelsPerWorldUnit;
                        screen.OnMouseWheel(width * .55f, height * .4f, 1);
                        checks["zoom"] = screen.CameraPixelsPerWorldUnit != zoom;
                        Click(screen.SpeedButtonRects[4]); break;
                    case 10:
                        ready = snapshot.CurrentSpeed == SimulationSpeed.Speed4;
                        if (ready) { checks["speed100"] = true; Click(screen.SpeedButtonRects[0]); }
                        break;
                    case 11:
                        ready = snapshot.CurrentSpeed == SimulationSpeed.Speed0;
                        if (ready) { checks["pause"] = snapshot.MotionTimeMs == 1; SnapshotImage("two-pr"); }
                        break;
                    case 12:
                        var journal = Get<CombatJournalPanel>(screen, "_combatJournalPanel");
                        Click(new SKRect(journal.Bounds.Left, journal.Bounds.Top, journal.Bounds.Right, 74)); break;
                    case 13:
                        var expanded = Get<CombatJournalPanel>(screen, "_combatJournalPanel");
                        double oldZoom = screen.CameraPixelsPerWorldUnit;
                        int offset = Get<int>(expanded, "_offset");
                        screen.OnMouseWheel(expanded.Bounds.MidX * scale, expanded.Bounds.MidY * scale, 1);
                        checks["journalScroll"] = expanded.Expanded && Get<int>(expanded, "_offset") > offset && screen.CameraPixelsPerWorldUnit == oldZoom;
                        break;
                    case 14:
                        SnapshotImage("journal");
                        var opened = Get<CombatJournalPanel>(screen, "_combatJournalPanel");
                        Click(new SKRect(opened.Bounds.Left, opened.Bounds.Top, opened.Bounds.Right, 74)); break;
                    case 15:
                        if (recovery)
                        {
                            engine.SetSpeed(SimulationSpeed.Speed1); Interlocked.Add(ref now, 300000); engine.SetSpeed(SimulationSpeed.Speed0);
                        }
                        else Click(screen.SpeedButtonRects[1]);
                        break;
                    case 16:
                        if (!recovery) { ready = snapshot.CurrentSpeed == SimulationSpeed.Speed1; break; }
                        ready = renderedScreen is DefeatScreen renderedDefeat && renderedDefeat.Layout.Panel.Width > 0;
                        if (ready) { checks["defeat"] = snapshot.EffectiveOutcome.State == SessionOutcomeState.Defeated; SnapshotImage("defeat"); pending = Route(((DefeatScreen)stack.Current).OnKeyDown(Key.Enter)); }
                        break;
                    case 17:
                        if (!recovery) break;
                        ready = renderedScreen is LoadScreen renderedLoad && Get<int>(renderedLoad, "_screenWidth") > 0;
                        if (ready) { SnapshotImage("load"); pending = Route(LoadClick((LoadScreen)stack.Current, LoadLayout.CloseButtonRect())); }
                        break;
                    case 18:
                        if (!recovery) break;
                        ready = renderedScreen is DefeatScreen renderedAgain && renderedAgain.Layout.Panel.Width > 0;
                        if (ready) { checks["loadCancel"] = true; pending = Route(((DefeatScreen)stack.Current).OnKeyDown(Key.Enter)); }
                        break;
                    case 19:
                        if (!recovery) break;
                        ready = renderedScreen is LoadScreen renderedLoadAgain && Get<int>(renderedLoadAgain, "_screenWidth") > 0;
                        if (ready) LoadClick((LoadScreen)stack.Current, LoadLayout.RowRect(0)); break;
                    case 20:
                        if (!recovery) break;
                        var load = (LoadScreen)stack.Current;
                        pending = Route(LoadClick(load, LoadLayout.LoadButtonRect()), load.LastRequestedSlotId); break;
                    case 21:
                        if (!recovery) break;
                        ready = stack.Current is GameSessionScreen && Get<GameSessionHandle>(window, "_session").Buffer.Latest is not null;
                        if (ready) { var fresh = Get<GameSessionHandle>(window, "_session"); checks["loadSuccess"] = !fresh.IsDefeated && fresh.Buffer.Latest!.Snapshot.Objects.Count(o => o.Countermeasure is not null) == 2; }
                        break;
                    default:
                        if (!recovery)
                        {
                            if (snapshot.MotionTimeMs > 100)
                            {
                                checks["physicalTime"] = snapshot.GameTimeMs == snapshot.MotionTimeMs * 300;
                                if (!noSolution) checks["movingProjectiles"] = snapshot.Objects.Count(o => o.Countermeasure is not null) == 2 && screen.DefenseChanceLabels.Count == 2;
                            }
                            Interlocked.Add(ref now, 5);
                        }
                        return;
                }
                if (ready) { step++; lastStepFrame = frame; }
            }
            catch (Exception ex) { failure = ex.ToString(); native.Close(); }
        };
        try { window.Run(); }
        finally { handle.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
        bool passed = failure is null && step >= 22 && checks.Values.All(v => v) && File.Exists(output);
        var report = File.Exists(output) ? JsonNode.Parse(File.ReadAllText(output))! : new JsonObject();
        report["combatInteraction"] = JsonSerializer.SerializeToNode(new { passed, step, frame, checks, failure, recovery, noSolution, initialEnemies = 8 });
        File.WriteAllText(output, report.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Native combat passed={passed}; step={step}; {failure}");
        return passed ? 0 : 1;

        void Click(SKRect r) { screen.OnMouseDown(r.MidX * scale, r.MidY * scale); screen.OnMouseUp(r.MidX * scale, r.MidY * scale); }
        void Open(string module) { var row = screen.CommandsPanel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == module); if (!row.Opened) Click(row.CaptionRect); }
        bool Button(string module, string type)
        {
            var row = screen.CommandsPanel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == module);
            if (!row.Opened) { Click(row.CaptionRect); return false; }
            var b = screen.CommandsPanel.AllCommandButtons.Single(b => b.Weapon?.ModuleId == module && b.CommandTypeId == type);
            if (!b.Enabled) return false;
            Click(b.Rect); return true;
        }
        bool Hover(string module)
        {
            var row = screen.CommandsPanel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == module);
            if (!row.Opened) { Click(row.CaptionRect); return false; }
            var b = screen.CommandsPanel.AllCommandButtons.Single(b => b.Weapon?.ModuleId == module && b.CommandTypeId == DefenseCommandTypes.Fire);
            screen.OnMouseMove(b.Rect.MidX * scale, b.Rect.MidY * scale); return true;
        }
        Task Route(ScreenEvent action, string? payload = null) => (Task)typeof(SkiaWindow).GetMethod("HandleScreenEvent", Private)!.Invoke(window, [action, payload])!;
        ScreenEvent LoadClick(LoadScreen load, (float X, float Y, float W, float H) r) => load.OnMouseDown(LoadLayout.PanelLeft(width) + r.X + r.W / 2, LoadLayout.PanelTop(height) + r.Y + r.H / 2, MouseButton.Left);
        void SnapshotImage(string name) { using var image = Get<SKSurface>(window, "_surface").Snapshot(); using var data = image.Encode(SKEncodedImageFormat.Png, 100); File.WriteAllBytes(Path.ChangeExtension(output, "." + name + ".png"), data.ToArray()); }
    }

    private sealed class Factory(string settings, string directory) : IGameSessionFactory
    {
        public IGameSessionConnection CreateSession() => throw new NotSupportedException();
        public IGameSessionConnection CreateSessionFromScenario(string path) => throw new NotSupportedException();
        public IGameSessionConnection CreateSessionFromSave(string slotId) => LocalGameSessionConnection.CreateFromSaveFile(settings, Path.Combine(directory, slotId + ".json"), directory);
        public ScenarioInfo[] ListScenarios() => [];
        public bool HasQuickSave() => false;
        public SaveSlotInfo[] ListSaveSlots() => [new("before", "EP8 native pre-death", DateTime.UtcNow)];
        public void DeleteSaveSlot(string slotId) => throw new NotSupportedException();
    }
}
