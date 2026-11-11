using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static class BuildCard
{
    internal static SKRect Bounds(TacticalBattleFrame f) => new(f.Panel.Left, f.Panel.Bottom + 6, f.Panel.Right, f.Panel.Bottom + 306);
    internal static SKRect CellBounds(BuildCardModel model, BuildSlotCell cell, SKRect r)
    {
        float top = r.Top + 88, mid = r.MidX;
        return cell.Kind switch
        {
            TacticalSlotKind.Weapon => new(mid - 62, top + 6, mid + 62, top + 36),
            TacticalSlotKind.Armor => new(mid - 70, top + 44, mid + 70, top + 74),
            TacticalSlotKind.Shield when cell.Index == 0 => new(r.Left + 10, top + 82, r.Left + 74, top + 108),
            TacticalSlotKind.Shield => new(r.Right - 74, top + 82, r.Right - 10, top + 108),
            TacticalSlotKind.Module => new(r.Left + 78, top + 82, r.Right - 78, top + 108),
            _ when model.Cells.Count(c => c.Kind == TacticalSlotKind.Engine) == 1 => new(mid - 52, top + 114, mid + 52, top + 144),
            _ => new(r.Left + 10 + cell.Index * 109, top + 114, r.Left + 105 + cell.Index * 109, top + 144)
        };
    }
    internal static IReadOnlyList<SKPoint> Silhouette(string frame) => frame switch
    {
        "heavy" => [new(-58, -2), new(58, -2), new(106, 38), new(106, 124), new(82, 150), new(-82, 150), new(-106, 124), new(-106, 38)],
        "thug" => [new(-36, 0), new(48, 8), new(100, 52), new(92, 142), new(40, 150), new(-96, 148), new(-106, 64), new(-70, 24)],
        _ => [new(0, -4), new(40, 36), new(104, 96), new(96, 118), new(68, 150), new(-68, 150), new(-96, 118), new(-104, 96), new(-40, 36)]
    };
    public static void Draw(SKCanvas c, BuildCardModel model, SKRect r, PilotPortraitCatalog portraits)
    {
        var u = model.Unit; float top = r.Top + 88;
        if (Recording is { } layout) layout.Rects["build"] = r;
        PanelBox(c, r, false, 6, "ui.panel.build");
        if (model.Hovered) CursorGlyph(c, r.Left + 9, r.Top + 8, .6f);
        T(c, model.Heading, r.Left + (model.Hovered ? 24 : 9), r.Top + 18, 10.5f, Muted, true);
        PilotCardColumn.Portrait(c, portraits, u.Build?.PilotId ?? "", PilotCardModel.Name(u), SKRect.Create(r.Left + 9, r.Top + 25, 32, 32));
        T(c, PilotCardModel.Name(u), r.Left + 48, r.Top + 38, 14, Cyan, true);
        T(c, model.Frame, r.Right - 8, r.Top + 38, 10.5f, Muted, align: SKTextAlign.Right);
        T(c, U("HullValue", u.Hull, u.MaxHull), r.Left + 48, r.Top + 56, 10.5f, Text);
        Bar(c, r.Left + 130, r.Top + 51, r.Width - 138, 5, u.MaxHull > 0 ? u.Hull / (double)u.MaxHull : 0, Cyan);
        T(c, U("ArmorValue", u.Armor, u.MaxArmor), r.Left + 9, r.Top + 75, 10.5f, Text);
        Bar(c, r.Left + 130, r.Top + 70, r.Width - 138, 5, u.MaxArmor > 0 ? u.Armor / (double)u.MaxArmor : 0, Purple);
        Polygon(c, Silhouette(u.FrameId).Select(p => new SKPoint(r.MidX + p.X, top + p.Y)), SKColor.Parse("#122024"), BoxEdge, 1);
        foreach (var cell in model.Cells)
        {
            var box = CellBounds(model, cell, r);
            if (Recording is { } recorded) recorded.Rects[$"build:{cell.Kind}:{cell.Index}"] = box;
            DrawCell(c, cell, box);
        }
        Line(c, r.Left + 8, r.Right - 8, top + 156);
        T(c, model.Mass, r.Left + 9, top + 172, FitSize(model.Mass, 10.5f, r.Width - 18), Text);
        T(c, model.Motion, r.Left + 9, top + 188, FitSize(model.Motion, 10.5f, r.Width - 18), Text);
        T(c, model.Legend, r.Left + 9, top + 204, 9.5f, Muted);
    }
    private static void DrawCell(SKCanvas c, BuildSlotCell cell, SKRect r)
    {
        if (cell.Empty)
        {
            using var effect = SKPathEffect.CreateDash([3, 3], 0);
            using var paint = new SKPaint { IsAntialias = true, Color = cell.NoSlot ? Track : BoxEdge, Style = SKPaintStyle.Stroke, StrokeWidth = 1, PathEffect = effect };
            c.DrawRoundRect(r, 3, 3, paint);
            T(c, cell.Name, r.MidX, r.MidY + 3.2f, 9, Muted.WithAlpha(cell.NoSlot ? (byte)120 : (byte)200), align: SKTextAlign.Center);
            return;
        }
        Round(c, r, Box, cell.Alert ? Red : BoxEdge, cell.Alert ? 1.4f : 1, 3,
            cell.Alert ? "ui.box.build-slot.alert" : "ui.box.build-slot");
        var tone = cell.Kind switch { TacticalSlotKind.Weapon => Yellow, TacticalSlotKind.Armor => Purple, TacticalSlotKind.Shield => Cyan, TacticalSlotKind.Module => Green, _ => Text };
        using (var stripe = new SKPaint { Color = tone }) c.DrawRect(new(r.Left + 1, r.Top + 4, r.Left + 4, r.Bottom - 4), stripe);
        T(c, BuildCardModel.Ellipsize(cell.Name, r.Width - 12), r.Left + 8, r.Top + 12, 9.5f, Text, true);
        T(c, BuildCardModel.Ellipsize(cell.Detail, r.Width - 12, 9, false), r.Left + 8, r.Top + 24, 9, cell.Alert ? Red : Muted);
    }
}
