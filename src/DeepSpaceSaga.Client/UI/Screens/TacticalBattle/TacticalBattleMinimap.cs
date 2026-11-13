using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;
using Skin = DeepSpaceSaga.Client.UI.Screens.TacticalBattle.TacticalBattleSkinRenderScope;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static class TacticalBattleMinimap
{
    internal const float Margin = 6;
    internal const float Height = 120;
    internal const float PhaseTop = Margin + Height + Margin;

    internal static SKRect Bounds(TacticalBattleFrame frame)
        => new(frame.Panel.Left, Margin, frame.Panel.Right, Margin + Height);

    internal static void Draw(SKCanvas canvas, TacticalBattleFrame frame, SKRect bounds)
    {
        var area = bounds;
        area.Inflate(-8, -8);
        if (area.Width <= 0 || area.Height <= 0) return;

        var view = frame.View;
        var (viewLeft, viewTop) = view.Unproject(0, 0);
        var (viewRight, viewBottom) = view.Unproject(view.PixelWidth, view.PixelHeight);
        double left = viewLeft, top = viewTop, right = viewRight, bottom = viewBottom;
        var units = frame.Battle.Units.Where(unit => unit.State != TacticalUnitState.Destroyed &&
            unit.Kind is TacticalUnitKind.Carrier or TacticalUnitKind.Drone).ToArray();
        foreach (var unit in units)
        {
            left = Math.Min(left, unit.Pose.X);
            top = Math.Min(top, unit.Pose.Y);
            right = Math.Max(right, unit.Pose.X);
            bottom = Math.Max(bottom, unit.Pose.Y);
        }

        double centerX = (left + right) * .5, centerY = (top + bottom) * .5;
        double scale = Math.Min(area.Width / (right - left), area.Height / (bottom - top));
        SKPoint Project(double x, double y) =>
            new(area.MidX + (float)((x - centerX) * scale), area.MidY + (float)((y - centerY) * scale));

        var upperLeft = Project(viewLeft, viewTop);
        var lowerRight = Project(viewRight, viewBottom);
        var viewport = new SKRect(upperLeft.X, upperLeft.Y, lowerRight.X, lowerRight.Y);
        if (Recording is { } layout)
        {
            layout.Rects["minimap"] = bounds;
            layout.Rects["minimap:viewport"] = viewport;
        }

        Round(canvas, bounds, MapBg, BoxEdge, radius: 4, role: "ui.minimap");
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            Color = Skin.Color("ui.minimap.viewport", "stroke", Text.WithAlpha(195))
        };
        canvas.DrawRect(viewport, paint);

        paint.Style = SKPaintStyle.Fill;
        foreach (var unit in units)
        {
            var point = Project(unit.Pose.X, unit.Pose.Y);
            bool own = unit.Side == frame.Battle.PlayerSide;
            paint.Color = Skin.Color(own ? "ui.minimap.own" : "ui.minimap.enemy", "fill", own ? Cyan : Orange);
            float radius = unit.Kind == TacticalUnitKind.Carrier ? 3.5f : 2;
            var marker = new SKRect(point.X - radius, point.Y - radius, point.X + radius, point.Y + radius);
            if (unit.Kind == TacticalUnitKind.Carrier) canvas.DrawRect(marker, paint);
            else canvas.DrawCircle(point, radius, paint);
            if (Recording is { } recorded) recorded.Rects["minimap:unit:" + unit.Id] = marker;
        }
    }
}
