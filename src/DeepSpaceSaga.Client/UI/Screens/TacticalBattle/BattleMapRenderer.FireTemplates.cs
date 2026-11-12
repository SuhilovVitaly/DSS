using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static partial class TacticalBattleMapRenderer
{
    private static readonly SKColor TTRust = SKColor.Parse("#AD5C44"), TTRustEdge = SKColor.Parse("#5A2416"),
        TTReticle = SKColor.Parse("#FF4628"), TTAlt = SKColor.Parse("#FF9A6A");

    private static (SKPoint A, SKPoint B) Ribbon(SKCanvas canvas, SKPoint a, SKPoint b, float width,
        SKColor face, SKColor edge, float trimA, float trimB, List<SKRect> occupied)
    {
        float length = SKPoint.Distance(a, b);
        if (length < trimA + trimB + 4) return (a, b);
        var dir = Scale(b - a, 1 / length);
        var start = a + Scale(dir, trimA); var tip = b - Scale(dir, trimB);
        var end = tip - Scale(dir, .9f * width);
        using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Butt, Color = edge.WithAlpha(230), StrokeWidth = width + 2.5f };
        canvas.DrawLine(start, end, paint);
        paint.Color = face; paint.StrokeWidth = width; canvas.DrawLine(start, end, paint);
        using var dash = SKPathEffect.CreateDash([5, 4], 0);
        paint.Color = edge; paint.StrokeWidth = 1.1f; paint.PathEffect = dash; canvas.DrawLine(start, end, paint);
        var side = new SKPoint(-dir.Y * .95f * width, dir.X * .95f * width);
        Polygon(canvas, [tip, end + side, end - side], face, edge, 1.3f);
        for (int i = 0; i <= 12; i++)
        {
            var p = start + Scale(end - start, i * .08f);
            occupied.Add(SKRect.Create(p.X - width / 2, p.Y - width / 2, width, width));
        }
        return (start, end);
    }

    private static void Reticle(SKCanvas canvas, SKPoint p, bool primary)
    {
        float radius = primary ? 22 : 19, width = primary ? 2.6f : 1.6f;
        var color = primary ? TTReticle : TTAlt;
        Circle(canvas, p, radius, SKColors.Transparent, color, width);
        foreach (var axis in new SKPoint[] { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) })
            Stroke(canvas, p + Scale(axis, radius - 5), p + Scale(axis, radius + 7), color, width);
    }

    private static void DrawFireTemplates(SKCanvas c, TacticalBattleFrame f, TacticalFirePlan plan, List<SKRect> occupied)
    {
        if (plan.Selected?.Drone.Build is { } build)
        {
            using var sector = CreateFireSector(f.View, plan.Selected.Drone.Pose, build.Range, 2 * build.ArcDegrees);
            using var paint = new SKPaint { IsAntialias = true, Color = SKColors.Black.WithAlpha(85) };
            c.DrawPath(sector, paint);
            using var dash = SKPathEffect.CreateDash([7, 5], 0);
            paint.Style = SKPaintStyle.Stroke; paint.Color = TTGold; paint.StrokeWidth = 1.6f; paint.PathEffect = dash;
            c.DrawPath(sector, paint);
        }
        foreach (var incoming in f.Battle.Incoming)
        {
            var shooter = f.Battle.Units.FirstOrDefault(u => u.Id == incoming.ShooterId);
            var target = f.Battle.Units.FirstOrDefault(u => u.Id == incoming.TargetId);
            if (shooter is null || target is null) continue;
            Ribbon(c, f.View.Project(shooter.Pose), MarkerPoint(f.View, target), 8, TTRust, TTRustEdge, 16, 18, occupied);
        }
        foreach (var row in plan.Rows.OrderBy(r => r.Drone.Id == plan.SelectedDroneId ? 1 : 0))
        {
            var target = f.Battle.Units.FirstOrDefault(u => u.Id == row.TargetId);
            if (target is null || row.Drone.InDust || row.Evade) continue;
            var a = f.View.Project(row.Drone.Pose); var b = MarkerPoint(f.View, target);
            bool selected = row.Drone.Id == plan.SelectedDroneId;
            Ribbon(c, a, b, selected ? 14.5f : 11, selected ? TTSelected : TTCard, TTEdge, 16, 24, occupied);
            if (selected && row.Target is { Absorption: > 0 } candidate)
            {
                var actualTarget = f.View.Project(target.Pose);
                var segments = AnomalyPresentation.Segments(f.Battle, row.Drone.Pose, target.Pose);
                foreach (var segment in segments)
                    Stroke(c, a + Scale(actualTarget - a, (float)segment.T0), a + Scale(actualTarget - a, (float)segment.T1), Heat, 3.2f, [6, 3]);
                if (!segments.IsEmpty)
                {
                    var longest = segments.OrderByDescending(s => s.T1 - s.T0).First();
                    var mid = a + Scale(actualTarget - a, (float)((longest.T0 + longest.T1) / 2));
                    string text = M("Absorption", Math.Round(candidate.Absorption * 100));
                    float half = (Measure(text, 12) + 14) / 2;
                    var anchor = mid + new SKPoint(0, 26);
                    foreach (var delta in new SKPoint[] { new(0, 26), new(0, -26), new(half + 14, 0), new(-half - 14, 0), new(0, 48), new(0, -48) })
                    {
                        var q = mid + delta; var rect = SKRect.Create(q.X - half, q.Y - 17, half * 2, 19);
                        if (occupied.Any(r => r.IntersectsWith(rect))) continue;
                        anchor = q; break;
                    }
                    var box = Callout(c, anchor.X, anchor.Y, text, Heat, Heat); occupied.Add(box);
                    if (Recording is { } layout) layout.Rects["absorption:" + row.Drone.Id] = box;
                }
            }
        }
        if (plan.Selected is { } selectedRow)
            foreach (var candidate in selectedRow.Group.List)
            {
                var target = f.Battle.Units.FirstOrDefault(u => u.Id == candidate.TargetId);
                if (target is null) continue;
                var point = MarkerPoint(f.View, target);
                Reticle(c, point, candidate.TargetId == selectedRow.TargetId);
                var bounds = SKRect.Create(point.X - 22, point.Y - 22, 44, 44); occupied.Add(bounds);
                if (Recording is { } layout) layout.Rects["candidate:" + target.Id] = bounds;
            }
        foreach (var row in plan.Rows.Where(r => r.Drone.InDust))
        {
            var p = f.View.Project(row.Drone.Pose);
            var box = Callout(c, p.X, p.Y + 40, M("InDustNoFire"), Dust, Dust); occupied.Add(box);
            if (Recording is { } layout) layout.Rects["dust:" + row.Drone.Id] = box;
        }
    }
}
