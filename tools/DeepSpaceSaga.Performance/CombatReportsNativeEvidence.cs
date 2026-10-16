using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using Silk.NET.Windowing;
using SkiaSharp;

internal static class CombatReportsNativeEvidence
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    internal static int Run(string[] args)
    {
        string settings = Path.Combine(AppContext.BaseDirectory, "Settings.json");
        byte[] original = File.ReadAllBytes(settings);
        var config = JsonNode.Parse(original)!;
        float scale = float.Parse(args[3], CultureInfo.InvariantCulture);
        config["gameSettings"]!["uiScale"] = scale;
        config["gameSettings"]!["language"] = args[5];
        try { File.WriteAllText(settings, config.ToJsonString()); return RunCore(args, scale); }
        finally { File.WriteAllBytes(settings, original); }
    }

    private static int RunCore(string[] args, float scale)
    {
        string root = Path.GetFullPath(args[0]), output = Path.GetFullPath(args[1]);
        string client = Path.Combine(root, "src/DeepSpaceSaga.Client"); Directory.SetCurrentDirectory(client);
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        var dimensions = args[4].Split('x'); int width = int.Parse(dimensions[0]), height = int.Parse(dimensions[1]);
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(client, "Settings.json"), out _, out _);
        var scenario = ScenarioLoader.LoadFromFile(Path.Combine(client, "Scenarios/CombatMukha/scenario.json"));
        using var engine = new SimulationEngine(registry);
        engine.LoadScenario(scenario with { GameState = scenario.GameState with { MasterSeed = 1 } });
        var world = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0);
        var connection = new QuietConnection(); var handle = new GameSessionHandle(connection);
        handle.Buffer.Update(world);
        var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle, uiScale: scale);
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_REPORT", Path.ChangeExtension(output, ".frame.json"));
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_WINDOW", args[4]);
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_EXIT", "0");
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_CASE", "EP0010 native report matrix; production CombatMukha seed1 paused world + explicit UI visual fixtures");
        using var window = new SkiaWindow(screen, new Factory());
        typeof(SkiaWindow).GetField("_session", Private)!.SetValue(window, handle);
        typeof(SkiaWindow).GetField("_gameSessionScreen", Private)!.SetValue(window, screen);
        var native = (IWindow)typeof(SkiaWindow).GetField("_window", Private)!.GetValue(window)!;
        var checks = new Dictionary<string, bool>(); var empty = new List<double>(); var full = new List<double>();
        int frame = 0; long previous = 0, sequence = 0; ulong tick = world.SnapshotSequence;
        string? failure = null;
        bool performance = args.Contains("--performance");
        var elapsed = Stopwatch.StartNew();
        window.NativeFrameRendered = (surface, _) =>
        {
            try
            {
                long now = Stopwatch.GetTimestamp(); frame++;
                if (elapsed.Elapsed > TimeSpan.FromMinutes(3)) throw new TimeoutException("Native report harness exceeded 3 minutes.");
                if (frame == 3)
                {
                    if (screen.IsPanelVisible) Click(screen.LastCloseRect);
                    screen.OnKeyDown(Key.End);
                }
                if (frame == 5) { CheckLayout("empty-open"); Capture("empty-open"); Click(screen.ObjectInfoPanel.HideShowButtonRect); }
                if (frame == 8) { CheckLayout("empty-closed"); Capture("empty-closed"); Click(screen.ObjectInfoPanel.HideShowButtonRect); Publish(200); }
                if (frame == 12)
                {
                    CheckLayout("200-open"); Capture("200-open");
                    checks["history200"] = screen.ReportStore.Reports.Count == 200;
                    checks["newestExpanded"] = screen.ReportStore.ExpandedSequence == sequence;
                    checks["noFocusSteal"] = !screen.ReportPanel.HasFocus;
                    var before = (screen.CameraFocusX, screen.CameraFocusY, screen.CameraPixelsPerWorldUnit, handle.Buffer.CurrentSpeed);
                    var panel = screen.CombatReportsBounds; Click(new(panel.Left, panel.Top, panel.Right, panel.Top + 25));
                    screen.OnMouseWheel(panel.MidX * scale, (panel.Top + 20) * scale, -2);
                    screen.OnKeyDown(Key.Down); screen.OnKeyDown(Key.Enter);
                    checks["inputIsolation"] = before == (screen.CameraFocusX, screen.CameraFocusY, screen.CameraPixelsPerWorldUnit, handle.Buffer.CurrentSpeed) && connection.Commands == 0 && connection.Speeds == 0;
                    Click(screen.ObjectInfoPanel.HideShowButtonRect);
                }
                if (frame == 16) { CheckLayout("200-closed"); Capture("200-closed"); }
                if (args.Contains("--art") && frame is >= 20 and < 52 && frame % 4 == 0)
                {
                    int variant = (frame - 20) / 4;
                    Publish(1, variant);
                }
                if (args.Contains("--art") && frame is >= 22 and < 54 && (frame - 22) % 4 == 0)
                    Capture("variant-" + (frame - 22) / 4);
                if (frame == 60)
                {
                    screen.ReportStore.Reset(); screen.ReportPanel.ClearFocus();
                    if (screen.ObjectInfoPanel.State == ObjectInfoPanelState.Closed) Click(screen.ObjectInfoPanel.HideShowButtonRect);
                }
                if (performance)
                {
                    if (previous != 0 && frame > 180 && frame <= 780) empty.Add(Stopwatch.GetElapsedTime(previous, now).TotalMilliseconds);
                    if (frame == 780) Publish(200);
                    if (previous != 0 && frame > 900 && frame <= 1500) full.Add(Stopwatch.GetElapsedTime(previous, now).TotalMilliseconds);
                    if (frame == 1500) native.Close();
                }
                else if (frame >= 64) native.Close();
                previous = now;
            }
            catch (Exception error) { failure = error.ToString(); native.Close(); }
        };
        try { window.Run(); }
        finally { handle.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
        bool passed = failure is null && frame >= 64 && checks.Values.All(v => v);
        var report = new
        {
            schemaVersion = 1,
            passed,
            failure,
            checks,
            frame,
            durationSeconds = elapsed.Elapsed.TotalSeconds,
            viewport = new { width, height, scale, language = args[5] },
            world = "production CombatMukha seed1 at physical0; UI fixture reports explicitly injected",
            backend = "native SkiaWindow OpenGL frame-callback intervals including prior presentation wait; raster tests excluded",
            cpu = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"),
            logicalProcessors = Environment.ProcessorCount,
            baseline0 = Stats(empty),
            history200 = Stats(full),
            targetFrameMs = 12.5,
            targetPassed = performance && full.Count == 600 && full.Order().ElementAt(593) <= 12.5,
            humanAcceptance = "OPEN"
        };
        File.WriteAllText(output, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Native reports {args[4]} scale={scale} {args[5]} passed={passed}; frames={frame}; {failure}");
        return passed ? 0 : 1;

        void Click(SKRect r) { screen.OnMouseDown(r.MidX * scale, r.MidY * scale); screen.OnMouseUp(r.MidX * scale, r.MidY * scale); }
        void Capture(string label)
        {
            using var image = ((SKSurface)typeof(SkiaWindow).GetField("_surface", Private)!.GetValue(window)!).Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(Path.ChangeExtension(output, "." + label + ".png"), data.ToArray());
        }
        void CheckLayout(string label)
        {
            var panel = screen.CombatReportsBounds;
            var journal = (CombatJournalPanel)typeof(GameSessionScreen).GetField("_combatJournalPanel", Private)!.GetValue(screen)!;
            checks[label] = panel.Height >= 80 && panel.Width == screen.ObjectInfoPanel.Bounds.Width &&
                panel.Top >= screen.ObjectInfoPanel.Bounds.Bottom && !panel.IntersectsWith(screen.MapToolbarRect) && journal.Bounds.Right < panel.Left;
        }
        void Publish(int count, int? forcedVariant = null)
        {
            var reports = Enumerable.Range(0, count).Select(i =>
            {
                long id = ++sequence; int variant = forcedVariant ?? i % 8;
                var kind = variant switch
                {
                    0 => BattleReportKind.EnemyInRange,
                    1 => BattleReportKind.EnemyTorpedoLaunch,
                    2 => BattleReportKind.PlayerTorpedoLaunch,
                    3 => BattleReportKind.PlayerCountermeasureLaunch,
                    4 => BattleReportKind.EnemyTorpedoIntercepted,
                    5 or 6 => BattleReportKind.EnemyShipHit,
                    _ => BattleReportKind.PlayerShipDamaged
                };
                return new BattleReportSnapshot(id, 12345.5, kind, $"visual-{id}",
                    new(kind switch { BattleReportKind.PlayerShipDamaged => BattleReportReporterRole.Captain, BattleReportKind.EnemyInRange or BattleReportKind.EnemyTorpedoLaunch => BattleReportReporterRole.ScannerOperator, BattleReportKind.PlayerCountermeasureLaunch or BattleReportKind.EnemyTorpedoIntercepted => BattleReportReporterRole.CountermeasureOperator, _ => BattleReportReporterRole.TorpedoOperator }),
                    SourceObjectId: "source", SourceDisplayName: "Муха-2 / Mukha-2", TargetObjectId: "target", TargetDisplayName: "Муха-1 / Mukha-1",
                    Variant: variant == 6 ? BattleReportVariant.Destroyed : BattleReportVariant.Hit);
            }).ToArray();
            handle.Buffer.Update(world with { SnapshotSequence = ++tick, CombatReports = new(0, sequence, reports.ToImmutableArray()) });
        }
    }

    private static object Stats(List<double> values)
    {
        var sorted = values.Order().ToArray();
        double At(double p) => sorted.Length == 0 ? 0 : sorted[Math.Clamp((int)Math.Ceiling(sorted.Length * p) - 1, 0, sorted.Length - 1)];
        return new { frames = sorted.Length, p50Ms = At(.5), p95Ms = At(.95), p99Ms = At(.99), maxMs = sorted.LastOrDefault() };
    }

    private sealed class QuietConnection : IGameSessionConnection
    {
        private readonly Channel<AuthoritativeSnapshot> _channel = Channel.CreateUnbounded<AuthoritativeSnapshot>();
        internal int Commands, Speeds;
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) { Commands++; return ValueTask.CompletedTask; }
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) { Speeds++; return ValueTask.CompletedTask; }
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
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
