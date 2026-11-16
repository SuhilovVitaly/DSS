using System.Diagnostics;
using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using SkiaSharp;

internal static class TacticalBattleCloudEvidence
{
    /// <summary>Smallest world span framed by the all-clouds scene; one clump with zero sigma must not zoom to infinity.</summary>
    internal const double MinimumSpan = 1;

    internal static void Measure(GRContext context, TacticalBattleFrame frame, string output) =>
        Measure(frame, output, context, size => SKSurface.Create(context, false, size));

    /// <summary>Test seam: a null context skips GPU flushes; the factory returns null like SKSurface.Create on a lost context.</summary>
    internal static void Measure(TacticalBattleFrame frame, string output, GRContext? context, Func<SKImageInfo, SKSurface?> createSurface)
    {
        var clumps = frame.Battle.Anomalies.IsDefault ? [] : frame.Battle.Anomalies.SelectMany(a => a.Clumps).ToArray();
        object[] results;
        if (clumps.Length == 0)
        {
            results = [new { scene = "all", status = "skipped", reason = "no-anomaly-clumps", frame.Battle.Seed, frame.Battle.Turn, clumps = 0 }];
        }
        else
        {
            var strongest = clumps.MaxBy(c => c.Amplitude)!;
            double left = clumps.Min(c => c.X - 3 * Math.Max(c.SigmaX, c.SigmaY));
            double right = clumps.Max(c => c.X + 3 * Math.Max(c.SigmaX, c.SigmaY));
            double top = clumps.Min(c => c.Y - 3 * Math.Max(c.SigmaX, c.SigmaY));
            double bottom = clumps.Max(c => c.Y + 3 * Math.Max(c.SigmaX, c.SigmaY));
            double width = Math.Max(right - left, MinimumSpan), height = Math.Max(bottom - top, MinimumSpan);
            var views = new[]
            {
                (Name: "movement", View: frame.View),
                (Name: "cloud-center", View: frame.View with { CenterX = strongest.X, CenterY = strongest.Y }),
                (Name: "all-clouds", View: frame.View with { CenterX = (left + right) / 2, CenterY = (top + bottom) / 2,
                    PixelsPerUnit = Math.Min(frame.View.PixelWidth / width, frame.View.PixelHeight / height) })
            };
            results = views.Select(scene => MeasureScene(context, createSurface, frame with { View = scene.View }, scene.Name)).ToArray();
        }
        File.WriteAllText(output, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static object MeasureScene(GRContext? context, Func<SKImageInfo, SKSurface?> createSurface, TacticalBattleFrame frame, string name)
    {
        using var surface = createSurface(new SKImageInfo(1920, 1080));
        if (surface is null)
            return new { scene = name, status = "skipped", reason = "surface-unavailable", width = 1920, height = 1080,
                frame.Battle.Seed, frame.Battle.Turn, frame.View };
        using var layer = new PaintedCloudLayer();
        surface.Canvas.Scale(1.5f);
        List<double> rebuildMs = [];
        List<double> submitMs = [];
        for (int i = 0; i < 9; i++)
        {
            frame = frame with { View = frame.View with { CenterX = frame.View.CenterX + 1 } };
            context?.Flush(true, true);
            long start = Stopwatch.GetTimestamp();
            layer.Draw(surface.Canvas, frame, context);
            submitMs.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            context?.Flush(true, true);
            rebuildMs.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
        int before = layer.BuildCount;
        long reuse = Stopwatch.GetTimestamp();
        layer.Draw(surface.Canvas, frame, context);
        context?.Flush(true, true);
        return new
        {
            scene = name,
            status = "measured",
            width = 1920,
            height = 1080,
            frame.Battle.Seed,
            frame.Battle.Turn,
            clumps = frame.Battle.Anomalies.Sum(a => a.Clumps.Length),
            visibleCenters = frame.Battle.Anomalies.SelectMany(a => a.Clumps).Count(c =>
                SKRect.Create(frame.View.PixelWidth, frame.View.PixelHeight).Contains(frame.View.Project(c.X, c.Y))),
            frame.View,
            gpuCompletionWait = context is not null,
            coldMs = rebuildMs[0],
            submitMs,
            warmRebuildMs = rebuildMs.Skip(1),
            maxWarmMs = rebuildMs.Skip(1).Max(),
            budgetMs = 12,
            warmBudgetPassed = rebuildMs.Skip(1).All(ms => ms <= 12),
            budgetPassed = rebuildMs.All(ms => ms <= 12),
            reuseMs = Stopwatch.GetElapsedTime(reuse).TotalMilliseconds,
            reused = before == layer.BuildCount,
            layer.BuildCount
        };
    }
}
