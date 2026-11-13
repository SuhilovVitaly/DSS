using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static partial class TacticalBattleMapRenderer
{
    static partial void DrawMovement(SKCanvas c, TacticalBattleFrame f, List<SKRect> occupied)
    {
        if (f.Movement is not { } movement) return;
        if (TacticalBattleSkinRenderScope.Tabletop) { DrawTemplates(c, f, movement, occupied); return; }
        if (movement.SelectedOrder is { } selectedOrder && !selectedOrder.Path.IsDefaultOrEmpty &&
            selectedOrder.Unit.Build is { Range: > 0, ArcDegrees: > 0 } build)
        {
            using var sector = CreateFireSector(f.View, selectedOrder.Path[^1], build.Range, 2 * build.ArcDegrees);
            var forecast = new SKColor(47, 191, 143);
            DrawFireSector(c, sector, forecast.WithAlpha(35), forecast.WithAlpha(115));
        }
        if (f.AimingMovement) DrawReachableArea(c, f.View, movement);
        using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round };
        foreach (var row in movement.Orders)
        {
            bool selected = row.Unit.Id == movement.Selected;
            if (row.Path.IsDefaultOrEmpty) continue;
            using var path = new SKPath(); path.AddPoly(row.Path.Select(f.View.Project).ToArray(), false);
            paint.Color = selected ? new SKColor(190, 190, 190) : new SKColor(128, 128, 128, 60);
            paint.StrokeWidth = selected ? 1.5f : .75f;
            c.DrawPath(path, paint);
            var end = f.View.Project(row.Path[^1]); float radius = selected ? 5 : 4;
            var rect = new SKRect(end.X - radius, end.Y - radius, end.X + radius, end.Y + radius);
            var color = TacticalBattleSkinRenderScope.Color("field.move.handle", "fill", Cyan);
            Triangle(c, end, radius, row.Path[^1].Course, selected ? color : color.WithAlpha(95));
            if (Recording is { } layout) layout.Rects["endpoint:" + row.Unit.Id] = rect;
            var inflated = rect; inflated.Inflate(1, 1); occupied.Add(inflated);
            if (selected && AnomalyPresentation.Inside(f.Battle, DeepSpaceSaga.Motion.TacticalBattle.TacticalAnomalyKind.Dust, row.Path[^1]))
            {
                var box = Callout(c, end.X, end.Y - 16, M("MoveEndInDust"), Dust, Dust);
                occupied.Add(box);
                if (Recording is { } l) l.Rects["callout:endpoint"] = box;
            }
        }
    }

    private static void DrawReachableArea(SKCanvas canvas, TacticalBattleView view, TacticalMovementFrame movement)
    {
        if (movement.SelectedOrder is null || movement.Reachable.Length < 3) return;
        var points = movement.Reachable.Select(view.Project).ToArray();
        double twiceArea = 0;
        for (int i = 1; i < points.Length - 1; i++)
        {
            var a = points[i] - points[0];
            var b = points[i + 1] - points[0];
            twiceArea += (double)a.X * b.Y - (double)a.Y * b.X;
        }
        if (!double.IsFinite(twiceArea) || Math.Abs(twiceArea) < .01) return;

        using var path = new SKPath();
        path.AddPoly(points, true);
        using var paint = new SKPaint { IsAntialias = true, Color = Cyan.WithAlpha(28) };
        canvas.DrawPath(path, paint);
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 1;
        paint.Color = Cyan.WithAlpha(145);
        canvas.DrawPath(path, paint);
    }
}
