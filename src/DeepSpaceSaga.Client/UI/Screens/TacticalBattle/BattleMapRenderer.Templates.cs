using System.Globalization;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static partial class TacticalBattleMapRenderer
{
    private static readonly SKColor TTCard = SKColor.Parse("#D6CBA6"), TTSelected = SKColor.Parse("#E8D48C"),
        TTEdge = SKColor.Parse("#463822"), TTInk = SKColor.Parse("#2A1A08"), TTGold = SKColor.Parse("#FFE27A"),
        TTTan = SKColor.Parse("#CDBF9C"), TTBadge = SKColor.Parse("#4A1C10");

    private static void DrawTemplates(SKCanvas c, TacticalBattleFrame f, TacticalMovementFrame movement, List<SKRect> occupied)
    {
        if (movement.SelectedOrder is not null && movement.Reachable.Length >= 3)
        {
            using var area = new SKPath(); area.AddPoly(movement.Reachable.Select(f.View.Project).ToArray(), true);
            using var fill = new SKPaint { IsAntialias = true, Color = SKColors.Black.WithAlpha(95) };
            c.DrawPath(area, fill);
            using var dash = SKPathEffect.CreateDash([7, 5], 0);
            fill.Color = TTGold; fill.Style = SKPaintStyle.Stroke; fill.StrokeWidth = 1.6f; fill.PathEffect = dash;
            c.DrawPath(area, fill);
            if (Recording is { } areaLayout) areaLayout.Rects["reachable"] = area.Bounds;
        }
        using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Butt, StrokeJoin = SKStrokeJoin.Round };
        using var centerDash = SKPathEffect.CreateDash([5, 4], 0);
        foreach (var row in movement.Orders.OrderBy(o => o.Unit.Id == movement.Selected ? 1 : 0))
        {
            if (row.Path.IsDefaultOrEmpty) continue;
            bool selected = row.Unit.Id == movement.Selected;
            var points = row.Path.Select(f.View.Project).ToArray();
            using var path = new SKPath(); path.AddPoly(points, false);
            float pathWidth = selected ? 14.5f : 6;
            paint.PathEffect = null; paint.Color = TTEdge.WithAlpha(selected ? (byte)230 : (byte)105); paint.StrokeWidth = pathWidth + 2.5f; c.DrawPath(path, paint);
            paint.Color = selected ? TTSelected : TTCard.WithAlpha(115); paint.StrokeWidth = pathWidth; c.DrawPath(path, paint);
            paint.Color = TTEdge.WithAlpha(selected ? (byte)200 : (byte)105); paint.StrokeWidth = 1.1f; paint.PathEffect = centerDash; c.DrawPath(path, paint);
            for (int i = 0; i < points.Length; i += 2) occupied.Add(SKRect.Create(points[i].X - pathWidth / 2, points[i].Y - pathWidth / 2, pathWidth, pathWidth));
            var mid = points[points.Length / 2];
            Circle(c, mid, 9, TTBadge, TTGold, 1.4f);
            T(c, Math.Round(row.Order.Speed).ToString(CultureInfo.InvariantCulture), mid.X, mid.Y + 3.6f, 10, SKColors.White, true, SKTextAlign.Center, "field.template.text");
            occupied.Add(SKRect.Create(mid.X - 10, mid.Y - 10, 20, 20));
            var end = points[^1];
            if (!TacticalShipSprites.Draw(c, row.Unit, end, row.Unit.Side == f.Battle.PlayerSide, row.Path[^1].Course, 140, shadow: false, unitScale: f.View.UnitScale))
                Triangle(c, end, 5, row.Path[^1].Course, TTGold);
            Brackets(c, end, selected ? TTGold : TTTan, selected ? 2 : 1.4f);
            occupied.Add(SKRect.Create(end.X - 22, end.Y - 22, 44, 44));
            if (Recording is { } layout) layout.Rects["endpoint:" + row.Unit.Id] = SKRect.Create(end.X - 6, end.Y - 6, 12, 12);
            if (!selected) continue;
            string text = M("Map.TemplateTip", PilotCardModel.Name(row.Unit), F(row.Order.Speed), Deg(row.Order.Turn)[..^1]);
            float width = Measure(text, 11.5f, true) + 16;
            var tip = SKRect.Create(end.X + 30, end.Y - 32, width, 21);
            if (tip.Right > Safe(f.View).Right) tip = SKRect.Create(end.X - 30 - width, end.Y - 46, width, 21);
            using var box = new SKPaint { IsAntialias = true, Color = TTTan };
            c.DrawRect(tip, box); box.Style = SKPaintStyle.Stroke; box.Color = TTEdge; box.StrokeWidth = 1.5f; c.DrawRect(tip, box);
            T(c, text, tip.Left + 8, tip.MidY + 11.5f * .36f, 11.5f, TTInk, true, role: "field.template.text");
            occupied.Add(tip);
            if (Recording is { } tipLayout) tipLayout.Rects["template-tip"] = tip;
            if (AnomalyPresentation.Inside(f.Battle, DeepSpaceSaga.Motion.TacticalBattle.TacticalAnomalyKind.Dust, row.Path[^1]))
            {
                var dust = Callout(c, end.X, end.Y + 34, M("MoveEndInDust"), Dust, Dust); occupied.Add(dust);
                if (Recording is { } dustLayout) dustLayout.Rects["callout:endpoint"] = dust;
            }
        }
    }

    private static void Brackets(SKCanvas c, SKPoint point, SKColor color, float width)
    {
        foreach (int x in new[] { -1, 1 })
            foreach (int y in new[] { -1, 1 })
            {
                var corner = point + new SKPoint(x * 21, y * 21);
                Stroke(c, corner, corner - new SKPoint(x * 8.4f, 0), color, width);
                Stroke(c, corner, corner - new SKPoint(0, y * 8.4f), color, width);
            }
    }
}
