using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static class TacticalBattleMapRenderer
{
    internal static readonly SKColor Own = new(47, 191, 143);
    internal static readonly SKColor Enemy = new(232, 98, 58);
    internal static readonly SKColor Inactive = new(85, 85, 85);
    internal static readonly SKColor Jammed = new(247, 207, 77);
    private static readonly SKColor Grid = new(17, 37, 54);
    private static readonly SKColor Ruler = new(135, 175, 197);

    internal static double GridStep(double width) => 40 * Math.Pow(2, Math.Round(Math.Log2(width / 1800.0), MidpointRounding.ToEven));

    internal static double RulerLength(double pixelsPerUnit, float uiScale)
    {
        double limit = 120 * (double)uiScale / pixelsPerUnit;
        double decade = Math.Pow(10, Math.Floor(Math.Log10(limit)));
        return limit >= 5 * decade ? 5 * decade : limit >= 2 * decade ? 2 * decade : decade;
    }

    internal static void Draw(SKCanvas canvas, TacticalBattleFrame frame)
    {
        var view = frame.View;
        float scale = frame.UiScale;
        var battle = frame.Battle;
        using var paint = new SKPaint { IsAntialias = true, StrokeWidth = scale };
        DrawGrid(canvas, view, paint);
        bool flight = frame.Presentation == TacticalBattlePresentation.SalvoFlight && !battle.BeforeSalvoUnits.IsDefaultOrEmpty;
        var units = flight ? battle.BeforeSalvoUnits : battle.Units;
        if (frame.Presentation == TacticalBattlePresentation.Current)
        {
            DrawTrails(canvas, frame, paint);
            foreach (var unit in units)
            {
                if (unit.Side == battle.PlayerSide || unit.State != TacticalUnitState.Air) continue;
                if (frame.EnemyForecast && frame.FirePlan is null)
                {
                    var vector = TacticalGeometry.Vec(unit.Pose.Course, unit.Speed);
                    var end = unit.Pose with { X = unit.Pose.X + vector.X, Y = unit.Pose.Y + vector.Y };
                    paint.Color = Enemy.WithAlpha(112);
                    canvas.DrawLine(view.Project(unit.Pose), view.Project(end), paint);
                    DrawArc(canvas, view, end, 55, battle.Rules.RearArc, end.Course + 180, paint, filled: false);
                }
                if (frame.EnemySectors && !unit.Jammed && unit.Build is { } build)
                {
                    paint.Color = Enemy.WithAlpha(40);
                    DrawArc(canvas, view, unit.Pose, build.Range, build.ArcDegrees, unit.Pose.Course, paint, filled: true);
                }
            }
            if (frame.Movement is { } movement) DrawMovement(canvas, frame, movement, paint);
            if (frame.FirePlan is { } fire) DrawFire(canvas, frame, fire, paint);
        }

        DrawLabels(canvas, frame, units);
        foreach (var unit in units)
            DrawUnit(canvas, view.Project(unit.Pose), unit, unit.Side == battle.PlayerSide, unit.Id == frame.SelectedDroneId, scale, paint);
        DrawRuler(canvas, view, scale, paint);
    }

    private static void DrawFire(SKCanvas canvas, TacticalBattleFrame frame, TacticalFirePlan plan, SKPaint paint)
    {
        var view = frame.View;
        float s = frame.UiScale;
        foreach (var row in plan.Rows)
        {
            if (row.Drone.Jammed || row.Drone.Build is not { } gun) continue;
            paint.Color = Own.WithAlpha(row.Drone.Id == plan.SelectedDroneId ? (byte)52 : (byte)18);
            DrawArc(canvas, view, row.Drone.Pose, gun.Range, gun.ArcDegrees, row.Drone.Pose.Course, paint, filled: true);
        }
        foreach (var enemy in frame.Battle.Units)
        {
            if (enemy.Side == frame.Battle.PlayerSide || enemy.State != TacticalUnitState.Air) continue;
            paint.Color = Enemy.WithAlpha(112);
            DrawArc(canvas, view, enemy.Pose, 70, frame.Battle.Rules.RearArc, enemy.Pose.Course + 180, paint, filled: false);
        }
        if (plan.Selected is not { } selected) return;
        var source = view.Project(selected.Drone.Pose);
        foreach (var candidate in selected.Group.List)
        {
            var target = frame.Battle.Units.FirstOrDefault(u => u.Id == candidate.TargetId);
            if (target is null) continue;
            var endpoint = view.Project(target.Pose);
            bool chosen = candidate.TargetId == selected.TargetId;
            paint.Color = chosen && candidate.Rear ? new(253, 143, 104) : new(145, 224, 238);
            paint.StrokeWidth = (chosen ? 1.7f : 1) * s;
            paint.Style = SKPaintStyle.Stroke;
            canvas.DrawCircle(endpoint, (target.Kind == TacticalUnitKind.Carrier ? 20 : 14) * s, paint);
            paint.Style = SKPaintStyle.Fill;
            if (!chosen) continue;
            canvas.DrawLine(source, endpoint, paint);
            float x = (source.X + endpoint.X) * 0.5f;
            float y = (source.Y + endpoint.Y) * 0.5f;
            string label = BattleFormat.Probability(candidate.Chance) + (candidate.Rear ? " " + BattleFormat.Localize("FireRearShort") : "");
            ControlVisuals.Label(canvas, label, new(x, y - 23 * s, x + 104 * s, y - 3 * s), true, s);
        }
    }

    private static void DrawMovement(SKCanvas canvas, TacticalBattleFrame frame, TacticalMovementFrame movement, SKPaint paint)
    {
        var view = frame.View;
        if (!movement.Reachable.IsDefaultOrEmpty)
        {
            using var area = new SKPath();
            area.MoveTo(view.Project(movement.Reachable[0]));
            for (int i = 1; i < movement.Reachable.Length; i++) area.LineTo(view.Project(movement.Reachable[i]));
            area.Close();
            paint.Color = new SKColor(73, 160, 250, 45);
            canvas.DrawPath(area, paint);
            paint.Color = new SKColor(73, 160, 250, 125);
            paint.Style = SKPaintStyle.Stroke;
            canvas.DrawPath(area, paint);
            paint.Style = SKPaintStyle.Fill;
        }
        if (movement.SelectedOrder is { Unit.Jammed: false, Unit.Build: { } weapon } selected && !selected.Path.IsDefaultOrEmpty)
        {
            var end = selected.Path[^1];
            paint.Color = Own.WithAlpha(35);
            DrawArc(canvas, view, end, weapon.Range, weapon.ArcDegrees, end.Course, paint, filled: true);
        }
        foreach (var row in movement.Orders)
        {
            if (row.Path.IsDefaultOrEmpty) continue;
            bool selectedRow = row.Unit.Id == movement.Selected;
            paint.Color = selectedRow ? new SKColor(154, 235, 246) : new SKColor(126, 187, 196, 170);
            paint.StrokeWidth = (selectedRow ? 1.6f : 1) * frame.UiScale;
            paint.Style = SKPaintStyle.Stroke;
            using var path = new SKPath();
            path.MoveTo(view.Project(row.Path[0]));
            for (int i = 1; i < row.Path.Length; i++) path.LineTo(view.Project(row.Path[i]));
            canvas.DrawPath(path, paint);
            var end = row.Path[^1];
            var p = view.Project(end);
            canvas.DrawCircle(p, 7 * frame.UiScale, paint);
            paint.Style = SKPaintStyle.Fill;
            DrawDrone(canvas, p, end.Course, row.Unit.FrameId, frame.UiScale, paint);
        }
    }

    private static void DrawGrid(SKCanvas canvas, TacticalBattleView view, SKPaint paint)
    {
        double step = GridStep(view.WorldWidth);
        var (left, top) = view.Unproject(0, 0);
        var (right, bottom) = view.Unproject(view.PixelWidth, view.PixelHeight);
        paint.Color = Grid;
        // Caps also keep a pathological one-pixel viewport bounded.
        double firstX = Math.Ceiling(left / (double)step) * step;
        double firstY = Math.Ceiling(top / (double)step) * step;
        for (int i = 0; i < 2048 && firstX + i * step <= right; i++)
        {
            float x = view.Project(firstX + i * step, 0).X;
            canvas.DrawLine(x, 0, x, view.PixelHeight, paint);
        }
        for (int i = 0; i < 2048 && firstY + i * step <= bottom; i++)
        {
            float y = view.Project(0, firstY + i * step).Y;
            canvas.DrawLine(0, y, view.PixelWidth, y, paint);
        }
    }

    private static void DrawTrails(SKCanvas canvas, TacticalBattleFrame frame, SKPaint paint)
    {
        float s = frame.UiScale;
        using var dashed = SKPathEffect.CreateDash([4 * s, 4 * s], 0);
        paint.PathEffect = dashed; paint.Style = SKPaintStyle.Stroke;
        foreach (var trail in frame.Battle.Paths)
        {
            if (trail.Points.IsDefaultOrEmpty) continue;
            var unit = frame.Battle.Units.FirstOrDefault(u => u.Id == trail.DroneId);
            if (unit is null) continue;
            paint.Color = (unit.Side == frame.Battle.PlayerSide ? Own : Enemy).WithAlpha(100);
            using var path = new SKPath();
            path.MoveTo(frame.View.Project(trail.Points[0]));
            for (int i = 1; i < trail.Points.Length; i++) path.LineTo(frame.View.Project(trail.Points[i]));
            canvas.DrawPath(path, paint);
        }
        paint.PathEffect = null; paint.Style = SKPaintStyle.Fill;
    }

    internal static void DrawArc(SKCanvas canvas, TacticalBattleView view, TacticalPoseSnapshot pose, double radius,
        double halfAngle, double course, SKPaint paint, bool filled)
    {
        var center = view.Project(pose);
        float r = (float)(radius * view.PixelsPerUnit);
        paint.Style = filled ? SKPaintStyle.Fill : SKPaintStyle.Stroke;
        canvas.DrawArc(SKRect.Create(center.X - r, center.Y - r, 2 * r, 2 * r),
            (float)(course - halfAngle), (float)(2 * halfAngle), filled, paint);
        paint.Style = SKPaintStyle.Fill;
    }

    private static void DrawUnit(SKCanvas canvas, SKPoint p, TacticalUnitSnapshot unit, bool own, bool selected, float scale, SKPaint paint)
    {
        paint.Color = unit.State is TacticalUnitState.Air or TacticalUnitState.Alive ? own ? Own : Enemy : Inactive;
        paint.StrokeWidth = 1.5f * scale;
        if (unit.Kind == TacticalUnitKind.Drone && unit.State == TacticalUnitState.Destroyed)
        {
            float r = 6 * scale;
            canvas.DrawLine(p.X - r, p.Y - r, p.X + r, p.Y + r, paint);
            canvas.DrawLine(p.X - r, p.Y + r, p.X + r, p.Y - r, paint);
            return;
        }
        if (unit.Kind == TacticalUnitKind.Carrier)
        {
            using var hex = new SKPath();
            for (int i = 0; i < 6; i++)
            {
                var (dx, dy) = TacticalGeometry.Vec(unit.Pose.Course + i * 60, 16 * scale);
                var point = new SKPoint(p.X + (float)dx, p.Y + (float)dy);
                if (i == 0) hex.MoveTo(point); else hex.LineTo(point);
            }
            hex.Close();
            canvas.DrawPath(hex, paint);
            return;
        }

        if (unit.State == TacticalUnitState.Disabled) paint.Style = SKPaintStyle.Stroke;
        DrawDrone(canvas, p, unit.Pose.Course, unit.FrameId, scale, paint);
        paint.Style = SKPaintStyle.Stroke;
        if (unit.Jammed && unit.State == TacticalUnitState.Air)
        {
            paint.Color = Jammed;
            canvas.DrawCircle(p, 10 * scale, paint);
        }
        if (selected && unit.State == TacticalUnitState.Air)
        {
            paint.Color = SKColors.White.WithAlpha(220);
            canvas.DrawCircle(p, 12 * scale, paint);
        }
        paint.Style = SKPaintStyle.Fill;
    }

    internal static void DrawDrone(SKCanvas canvas, SKPoint p, double course, string frameId, float scale, SKPaint paint)
    {
        float size = (frameId == "heavy" ? 1.25f : 1f) * scale;
        using var path = new SKPath();
        path.MoveTo(8 * size, 0);
        path.LineTo(-5 * size, 5 * size);
        path.LineTo(-5 * size, -5 * size);
        path.Close();
        canvas.Save();
        canvas.Translate(p.X, p.Y);
        canvas.RotateDegrees((float)course);
        canvas.DrawPath(path, paint);
        canvas.Restore();
    }

    private static void DrawLabels(SKCanvas canvas, TacticalBattleFrame frame, ImmutableArray<TacticalUnitSnapshot> units)
    {
        float s = frame.UiScale;
        var view = frame.View;
        using var text = ControlText.Paint(s);
        text.TextSize = 11 * s;
        using var bars = new SKPaint { IsAntialias = true, StrokeWidth = s };
        var viewport = new SKRect(2 * s, 2 * s, view.PixelWidth - 2 * s, view.PixelHeight - 2 * s);
        var occupied = new List<SKRect>(units.Length * 2 + 1)
        {
            SKRect.Create(12 * s, view.PixelHeight - 46 * s, 126 * s, 36 * s)
        };
        foreach (var unit in units)
        {
            var p = view.Project(unit.Pose);
            float radius = (unit.Kind == TacticalUnitKind.Carrier ? 17 : 12) * s;
            occupied.Add(SKRect.Create(p.X - radius, p.Y - radius, 2 * radius, 2 * radius));
        }
        foreach (var unit in units)
        {
            bool carrier = unit.Kind == TacticalUnitKind.Carrier;
            if (!carrier && unit.State == TacticalUnitState.Destroyed) continue;
            var p = view.Project(unit.Pose);
            if (!viewport.Contains(p.X, p.Y)) continue;
            bool disabled = unit.State == TacticalUnitState.Disabled;
            string value = unit.Id + " " + BattleFormat.Number(unit.Hull);
            value += carrier ? "/" + BattleFormat.Number(unit.MaxHull) : disabled
                ? " · " + BattleFormat.Localize("UnitDisabledShort") : "+" + BattleFormat.Number(unit.Armor);
            value = BattleFormat.Text(value);
            float w = (float)PyMath.Max(30 * s, text.MeasureText(value)) + 4 * s;
            float h = (carrier ? 15 : 25) * s;
            if (!TryPlace(p, w, h, viewport, occupied, s, out var rect)) continue;
            text.Color = disabled || unit.State == TacticalUnitState.Destroyed ? new(144, 151, 159) : new(226, 235, 242);
            bars.Color = new(116, 145, 164, 100);
            canvas.DrawLine(p, new SKPoint(rect.MidX, rect.Top), bars);
            canvas.DrawText(value, rect.Left + 2 * s, rect.Top - text.FontMetrics.Ascent, text);
            if (carrier) continue;
            DrawBar(rect.Top + 15 * s, unit.Armor, unit.MaxArmor, disabled ? Inactive : new(126, 159, 181));
            DrawBar(rect.Top + 20 * s, unit.Hull, unit.MaxHull, disabled ? Inactive : Own);

            void DrawBar(float y, int amount, int max, SKColor color)
            {
                float x = rect.Left + 2 * s;
                bars.Color = new(30, 50, 65);
                canvas.DrawRect(x, y, 30 * s, 3 * s, bars);
                if (max <= 0) return;
                bars.Color = color;
                canvas.DrawRect(x, y, 30 * s * (float)PyMath.Clamp(amount / (double)max, 0, 1), 3 * s, bars);
            }
        }
    }

    private static bool TryPlace(SKPoint point, float width, float height, SKRect viewport, List<SKRect> occupied, float scale, out SKRect result)
    {
        for (int row = 0; row < 12; row++)
        {
            float offset = 14 * scale + row * (height + 2 * scale);
            for (int direction = 0; direction < 2; direction++)
                for (int column = 0; column < 3; column++)
                {
                    float x = column switch { 0 => point.X - width * 0.5f, 1 => point.X + 14 * scale, _ => point.X - width - 14 * scale };
                    float y = direction == 0 ? point.Y + offset : point.Y - offset - height;
                    x = (float)PyMath.Clamp(x, viewport.Left, viewport.Right - width);
                    var candidate = SKRect.Create(x, y, width, height);
                    if (!viewport.Contains(candidate) || occupied.Any(other => other.IntersectsWith(candidate))) continue;
                    occupied.Add(candidate);
                    result = candidate;
                    return true;
                }
        }
        result = SKRect.Empty;
        return false;
    }

    private static void DrawRuler(SKCanvas canvas, TacticalBattleView view, float scale, SKPaint paint)
    {
        double distance = RulerLength(view.PixelsPerUnit, scale);
        float x = 16 * scale, y = view.PixelHeight - 18 * scale;
        float end = x + (float)(distance * view.PixelsPerUnit);
        paint.Color = Ruler; paint.StrokeWidth = scale;
        canvas.DrawLine(x, y, end, y, paint);
        canvas.DrawLine(x, y - 4 * scale, x, y + 4 * scale, paint);
        canvas.DrawLine(end, y - 4 * scale, end, y + 4 * scale, paint);
        string label = BattleFormat.Number(distance, distance >= 1 ? 0 : 2) + " " + BattleFormat.Localize("MapUnits");
        ControlVisuals.Label(canvas, label, new(x, y - 22 * scale, x + 130 * scale, y - 5 * scale), true, scale);
    }
}
