using System.Globalization;
using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static partial class TacticalBattleMapRenderer
{
    private static void Plate(SKCanvas c, TacticalBattleFrame f, TacticalUnitSnapshot u, SKPoint p, List<SKRect> occupied)
    {
        bool carrier = u.Kind == TacticalUnitKind.Carrier;
        float size = carrier ? 13 : 11, boxes = carrier ? 30 : 44, gap = carrier ? 20 : 15;
        string name = carrier ? u.Id : PilotCardModel.Name(u);
        float width = Math.Max(boxes, Measure(name, size, true)) + 4, height = size + 16;
        var rect = SKRect.Create(p.X - width / 2, p.Y + gap, width, height);
        int chosenOffset = 0;
        bool found = false;
        foreach (int offset in new[] { 0, 10, 20, 32, 46, 62 })
        {
            foreach (var candidate in new[] {
                SKRect.Create(p.X - width / 2, p.Y + gap + offset, width, height),
                SKRect.Create(p.X - gap - 8 - offset - width, p.Y - height / 2, width, height),
                SKRect.Create(p.X + gap + 8 + offset, p.Y - height / 2, width, height),
                SKRect.Create(p.X - width / 2, p.Y - gap - offset - height, width, height) })
            {
                var expanded = candidate; expanded.Inflate(2, 1);
                if (occupied.Any(r => r.IntersectsWith(expanded))) continue;
                rect = candidate; chosenOffset = offset; found = true; break;
            }
            if (found) break;
        }
        occupied.Add(rect);
        if (Recording is { } layout) layout.Rects["label:" + u.Id] = rect;
        if (chosenOffset > 0)
        {
            var q = new SKPoint(Math.Clamp(p.X, rect.Left, rect.Right), Math.Clamp(p.Y, rect.Top, rect.Bottom));
            float distance = SKPoint.Distance(p, q);
            if (distance > 12) Stroke(c, p + Scale(q - p, 12 / distance), q, SKColor.Parse("#CDBF9C").WithAlpha(170), 1);
        }
        var gold = SKColor.Parse("#FFE27A");
        T(c, name, rect.MidX, rect.Top + size, size, carrier ? gold : SKColor.Parse("#F6EFD8"), true, SKTextAlign.Center, "field.plate.name");
        var damage = f.Salvo is { InFlight: false } salvo ? salvo.Damage.FirstOrDefault(d => d.Unit.Id == u.Id) : null;
        float x = rect.MidX - boxes / 2;
        void Box(string kind, int value, float boxWidth, SKColor fill, bool damaged)
        {
            var box = SKRect.Create(x, rect.Top + size + 3, boxWidth, 13);
            using var paint = new SKPaint { Color = fill, IsAntialias = false };
            c.DrawRect(box, paint);
            paint.Color = SKColors.Black; paint.Style = SKPaintStyle.Stroke; paint.StrokeWidth = 1;
            c.DrawRect(box, paint);
            T(c, value.ToString(CultureInfo.InvariantCulture), box.MidX, box.MidY + 9.5f * .36f, 9.5f, SKColors.White, true, SKTextAlign.Center, "field.plate.value");
            if (Recording is { } record) record.Rects["plate:" + kind + ":" + u.Id] = box;
            if (damaged)
            {
                var border = box; border.Inflate(1, 1);
                paint.Color = gold; paint.StrokeWidth = 2; c.DrawRect(border, paint);
                if (Recording is { } recordDamage) recordDamage.Rects["plate:damage:" + kind + ":" + u.Id] = border;
            }
            x += boxWidth;
        }
        if (!carrier) Box("armor", u.Armor, 22, SKColor.Parse("#3A5A8A"), damage?.Armor > 0);
        Box("hull", u.Hull, carrier ? 30 : 22, SKColor.Parse(u.Side == f.Battle.PlayerSide ? "#3F6A2C" : "#8A1E14"), damage?.Hull > 0);
    }
}
