using System.Diagnostics;
using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using SkiaSharp;

internal static class TacticalBattleCloudEvidence
{
    internal static void Measure(GRContext context, TacticalBattleFrame frame, string output)
    {
        using var layer = new PaintedCloudLayer();
        using var surface = SKSurface.Create(context, false, new SKImageInfo(1920, 1080));
        surface.Canvas.Scale(1.5f);
        List<double> rebuildMs = [];
        for (int i = 0; i < 9; i++)
        {
            frame = frame with { View = frame.View with { CenterX = frame.View.CenterX + 1 } };
            context.Flush(true, true);
            long start = Stopwatch.GetTimestamp();
            layer.Draw(surface.Canvas, frame, context);
            context.Flush(true, true);
            rebuildMs.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
        int before = layer.BuildCount;
        long reuse = Stopwatch.GetTimestamp();
        layer.Draw(surface.Canvas, frame, context);
        context.Flush(true, true);
        File.WriteAllText(output, JsonSerializer.Serialize(new
        {
            width = 1920,
            height = 1080,
            frame.Battle.Seed,
            frame.Battle.Turn,
            clumps = frame.Battle.Anomalies.Sum(a => a.Clumps.Length),
            gpuCompletionWait = true,
            coldMs = rebuildMs[0],
            warmRebuildMs = rebuildMs.Skip(1),
            maxWarmMs = rebuildMs.Skip(1).Max(),
            budgetMs = 12,
            warmBudgetPassed = rebuildMs.Skip(1).All(ms => ms <= 12),
            reuseMs = Stopwatch.GetElapsedTime(reuse).TotalMilliseconds,
            reused = before == layer.BuildCount,
            layer.BuildCount
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
