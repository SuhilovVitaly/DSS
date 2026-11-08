using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static partial class TacticalBattleMapRenderer
{
    static partial void DrawMovement(SKCanvas c, TacticalBattleFrame f, List<SKRect> occupied)
    {
        if (f.Movement is not { } movement) return;
        foreach (var row in movement.Orders)
        {
            bool selected = row.Unit.Id == movement.Selected;
            if (row.Path.IsDefaultOrEmpty) continue;
            using var path = new SKPath(); path.AddPoly(row.Path.Select(f.View.Project).ToArray(), false);
            using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, Color = selected ? Cyan.WithAlpha(60) : Cyan.WithAlpha(150), StrokeWidth = selected ? 8 : 1.3f };
            if (selected) c.DrawPath(path, paint);
            paint.Color = selected ? Cyan : Cyan.WithAlpha(150); paint.StrokeWidth = selected ? 3 : 1.3f; c.DrawPath(path, paint);
            var end = f.View.Project(row.Path[^1]); float radius = selected ? 5 : 4;
            var rect = new SKRect(end.X - radius, end.Y - radius, end.X + radius, end.Y + radius);
            Round(c, rect, selected ? Cyan.WithAlpha(90) : Bg, paint.Color, paint.StrokeWidth, 0, "field.move.handle");
            if (Recording is { } layout) layout.Rects["endpoint:" + row.Unit.Id] = rect;
            var inflated = rect; inflated.Inflate(1, 1); occupied.Add(inflated);
            if (selected)
            { var box = Callout(c, end.X, end.Y - 16, U("PathEnd"), Cyan, Text); occupied.Add(box); if (Recording is { } l) l.Rects["callout:endpoint"] = box; }
        }
    }
}
