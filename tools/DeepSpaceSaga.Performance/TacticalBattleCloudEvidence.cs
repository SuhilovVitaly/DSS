using System.Diagnostics;
using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using SkiaSharp;

internal static class TacticalBattleCloudEvidence
{
    internal static void Measure(GRContext context, TacticalBattleFrame frame, string output)
    {
        var clumps = frame.Battle.Anomalies.SelectMany(a => a.Clumps).ToArray();
        var strongest = clumps.MaxBy(c => c.Amplitude)!;
        double left = clumps.Min(c => c.X - 3 * Math.Max(c.SigmaX, c.SigmaY));
        double right = clumps.Max(c => c.X + 3 * Math.Max(c.SigmaX, c.SigmaY));
        double top = clumps.Min(c => c.Y - 3 * Math.Max(c.SigmaX, c.SigmaY));
        double bottom = clumps.Max(c => c.Y + 3 * Math.Max(c.SigmaX, c.SigmaY));
        var views = new[]
        {
            (Name: "movement", View: frame.View),
            (Name: "cloud-center", View: frame.View with { CenterX = strongest.X, CenterY = strongest.Y }),
            (Name: "all-clouds", View: frame.View with { CenterX = (left + right) / 2, CenterY = (top + bottom) / 2,
                PixelsPerUnit = Math.Min(frame.View.PixelWidth / (right - left), frame.View.PixelHeight / (bottom - top)) })
        };
        var results = views.Select(scene => MeasureScene(context, frame with { View = scene.View }, scene.Name)).ToArray();
        File.WriteAllText(output, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static object MeasureScene(GRContext context, TacticalBattleFrame frame, string name)
    {
        using var layer = new PaintedCloudLayer();
        using var surface = SKSurface.Create(context, false, new SKImageInfo(1920, 1080));
        surface.Canvas.Scale(1.5f);
        List<double> rebuildMs = [];
        List<double> submitMs = [];
        for (int i = 0; i < 9; i++)
        {
            frame = frame with { View = frame.View with { CenterX = frame.View.CenterX + 1 } };
            context.Flush(true, true);
            long start = Stopwatch.GetTimestamp();
            layer.Draw(surface.Canvas, frame, context);
            submitMs.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            context.Flush(true, true);
            rebuildMs.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
        int before = layer.BuildCount;
        long reuse = Stopwatch.GetTimestamp();
        layer.Draw(surface.Canvas, frame, context);
        context.Flush(true, true);
        return new
        {
            scene = name,
            width = 1920,
            height = 1080,
            frame.Battle.Seed,
            frame.Battle.Turn,
            clumps = frame.Battle.Anomalies.Sum(a => a.Clumps.Length),
            visibleCenters = frame.Battle.Anomalies.SelectMany(a => a.Clumps).Count(c =>
                SKRect.Create(frame.View.PixelWidth, frame.View.PixelHeight).Contains(frame.View.Project(c.X, c.Y))),
            frame.View,
            gpuCompletionWait = true,
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
