using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.TacticalBattle.Ai;
using DeepSpaceSaga.Engine.TacticalBattle.Content;
using Silk.NET.Input;
using SkiaSharp;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

internal static class TacticalBattlePerformance
{
    internal static async Task<int> Run(string root, string report)
    {
        if (typeof(TacticalBattlePerformance).Assembly.GetCustomAttributes(typeof(DebuggableAttribute), false)
            .OfType<DebuggableAttribute>().Any(a => a.IsJITOptimizerDisabled))
            throw new InvalidOperationException("Run the tactical benchmark in Release.");
        const int width = 1920, height = 1080;
        var catalog = TacticalBattleContentLoader.Load(Path.Combine(root, "src/DeepSpaceSaga.Client/Data/TacticalBattle"));
        var turns = new List<double>(2048);
        for (int seed = 1; seed <= 120; seed++)
        {
            var battle = Battle.Create(catalog, "initial", seed);
            for (int i = 0; i < catalog.Rules.SeriesTurnCap && battle.Result is null; i++)
            {
                long start = Stopwatch.GetTimestamp();
                battle.Step(TacticalBattleAi.Plan);
                if (seed > 20) turns.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            }
        }
        int wins = 0;
        long seriesStart = Stopwatch.GetTimestamp();
        for (int seed = 1; seed <= 1000; seed++)
            if (Battle.Create(catalog, "initial", seed).Run(TacticalBattleAi.Plan).Kind == DeepSpaceSaga.Engine.TacticalBattle.TacticalOutcomeKind.A)
                wins++;
        double seriesSeconds = Stopwatch.GetElapsedTime(seriesStart).TotalSeconds;

        string settings = Path.Combine(root, "src/DeepSpaceSaga.Client/Settings.json");
        await using var handle = new GameSessionHandle(LocalGameSessionConnection.CreateFromSettingsFile(settings),
            deferDebugTuningWrites: true);
        var flow = new TacticalBattleFlow(handle);
        if ((await flow.StartAsync(new("performance", 1)))?.Accepted != true)
            throw new InvalidOperationException("Tactical start rejected.");
        using var surface = SKSurface.Create(new SKImageInfo(width, height)) ?? throw new InvalidOperationException("Raster Skia unavailable.");
        long now = 0;
        var screen = new TacticalBattleScreen(flow, 1) { ClockMilliseconds = () => now };
        var frames = new Dictionary<string, Stats>(StringComparer.Ordinal);
        var orders = new TacticalBattleOrders();
        orders.Observe(flow.State!);
        var selected = orders.Capture().SelectedOrder ?? throw new InvalidOperationException("No aiming sample.");
        double px = selected.Unit.Pose.X, py = selected.Unit.Pose.Y;
        for (int i = 0; i < 100; i++) orders.AimAt(px + i / 2.0, py + 50);
        var aim = Sample(1000, i => { orders.AimAt(px + (i % 120) / 2.0, py + (i % 137) / 2.0); });

        MeasureFrame("move");
        int sequence = 0;
        while (flow.State!.Phase != TacticalBattlePhase.Fire && sequence++ < 100)
        {
            var before = flow.State!;
            var response = await flow.MoveAsync(new($"performance-move-{sequence}", before.BattleId, before.Revision));
            if (response?.Accepted != true) throw new InvalidOperationException("Move rejected.");
            screen.OnBattleUpdated(before, response);
            now += 1_000;
            DismissSalvo();
        }
        if (flow.State!.Phase != TacticalBattlePhase.Fire) throw new InvalidOperationException("No fire phase.");
        MeasureFrame("fire");
        var firing = flow.State!;
        var salvo = await flow.FireAsync(new("performance-fire", firing.BattleId, firing.Revision,
            [.. firing.Targets.Select(t => new TacticalFireTarget(t.DroneId, t.DefaultTargetId))]));
        if (salvo?.Accepted != true || salvo.State!.Shots.IsEmpty) throw new InvalidOperationException("No salvo.");
        screen.OnBattleUpdated(firing, salvo);
        now += 400;
        MeasureFrame("salvo-flight");
        screen.OnKeyDown(Key.Enter);
        MeasureFrame("salvo-results");
        screen.OnKeyDown(Key.Enter);
        if (screen.HasSalvoPresentation) throw new InvalidOperationException("Salvo did not close.");
        screen.PanelTabs.Select("sides");
        MeasureFrame("sides");
        screen.PanelTabs.Select("log");
        MeasureFrame("log");
        screen.PanelTabs.Select("forecast");
        MeasureFrame("forecast");

        var turn = Stats.From(turns);
        bool passed = turn.P95 <= 2 && aim.Max <= 1 && frames.Values.All(s => s.P95 <= 16) && seriesSeconds <= 60;
        Directory.CreateDirectory(Path.GetDirectoryName(report)!);
        await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new
        {
            passed,
            os = RuntimeInformation.OSDescription,
            framework = RuntimeInformation.FrameworkDescription,
            logicalProcessors = Environment.ProcessorCount,
            width,
            height,
            turn,
            aim,
            frames,
            seriesSeconds,
            wins,
            frameBackend = "CPU Skia raster, real TacticalBattleScreen, draw + flush, no monitor presentation",
            limits = new { turnP95Ms = 2, aimMaxMs = 1, frameP95Ms = 16, seriesSeconds = 60 }
        }, new JsonSerializerOptions { WriteIndented = true }));
        await flow.ExitAsync();
        return passed ? 0 : 1;

        void DismissSalvo()
        {
            for (int i = 0; i < 2 && screen.HasSalvoPresentation; i++) screen.OnKeyDown(Key.Enter);
        }
        void MeasureFrame(string label)
        {
            void Render(int _)
            {
                screen.Render(surface.Canvas, width, height);
                surface.Canvas.Flush();
            }
            for (int i = 0; i < 30; i++) Render(i);
            frames.Add(label, Sample(200, Render));
        }
    }

    private static Stats Sample(int count, Action<int> action)
    {
        var times = new double[count];
        for (int i = 0; i < count; i++)
        {
            long start = Stopwatch.GetTimestamp();
            action(i);
            times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }
        return Stats.From(times);
    }

    private sealed record Stats(int Count, double P50, double P95, double Max)
    {
        public static Stats From(IEnumerable<double> times)
        {
            var sorted = times.Order().ToArray();
            if (sorted.Length == 0) throw new InvalidOperationException("No samples.");
            double Percentile(double p) => sorted[(int)Math.Ceiling(sorted.Length * p) - 1];
            return new(sorted.Length, Percentile(.5), Percentile(.95), sorted[^1]);
        }
    }
}
