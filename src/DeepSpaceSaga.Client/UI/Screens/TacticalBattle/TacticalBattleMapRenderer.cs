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
        var units = VisibleUnits(frame);
        if (frame.Presentation == TacticalBattlePresentation.Current)
        {
            DrawTrails(canvas, frame, paint);
            foreach (var unit in units)
            {
                if (unit.Side == battle.PlayerSide || unit.Kind != TacticalUnitKind.Drone || unit.State != TacticalUnitState.Air) continue;
                var end = unit.Pose;
                if (frame.EnemyForecast)
                {
                    var vector = TacticalGeometry.Vec(unit.Pose.Course, unit.Speed);
                    end = unit.Pose with { X = unit.Pose.X + vector.X, Y = unit.Pose.Y + vector.Y };
                }
                if (frame.EnemySectors)
                {
                    bool firing = frame.FirePlan is not null;
                    DrawRearSector(canvas, view, firing ? unit.Pose : end, firing ? 70 : 55, battle.Rules.RearArc, paint);
                }
                if (frame.EnemyForecast)
                {
                    paint.Color = Enemy.WithAlpha(112);
                    canvas.DrawLine(view.Project(unit.Pose), view.Project(end), paint);
                    paint.Color = Enemy.WithAlpha(210);
                    canvas.DrawCircle(view.Project(end), 3 * scale, paint);
                }
            }
            if (frame.Movement is { } movement) DrawMovement(canvas, frame, movement, paint);
            if (frame.FirePlan is { } fire) DrawFire(canvas, frame, fire, paint);
        }

        DrawLabels(canvas, frame, units);
        if (frame.Salvo is { } salvo) DrawSalvo(canvas, frame, salvo, paint);
        var map = SKRect.Create(view.PixelWidth, view.PixelHeight);
        foreach (var unit in units)
        {
            var point = view.Project(unit.Pose);
            if (map.Contains(point.X, point.Y))
                DrawUnit(canvas, point, unit, unit.Side == battle.PlayerSide, unit.Id == frame.SelectedDroneId, scale, paint);
        }
        DrawRuler(canvas, view, scale, paint);
    }

    private static ImmutableArray<TacticalUnitSnapshot> VisibleUnits(TacticalBattleFrame frame)
    {
        var battle = frame.Battle;
        if (frame.Salvo is not { InFlight: true } salvo || battle.BeforeSalvoUnits.IsDefaultOrEmpty)
            return battle.Units;

        var units = battle.BeforeSalvoUnits;
        foreach (var flight in salvo.Flights)
        {
            if (!flight.Shot.LethalImpact || flight.ElapsedMs < flight.LaunchMs + TacticalBattleResults.FlightMs) continue;
            var unit = units.FirstOrDefault(u => u.Id == flight.Shot.TargetId);
            if (unit is not { Kind: TacticalUnitKind.Drone, State: TacticalUnitState.Air }) continue;
            units = units.Replace(unit, unit with { State = TacticalUnitState.Destroyed, Hull = 0 });
        }
        return units;
    }

    private static void DrawSalvo(SKCanvas canvas, TacticalBattleFrame frame, TacticalSalvoFrame salvo, SKPaint paint)
    {
        float scale = frame.UiScale;
        using var text = ControlText.Paint(scale);
        text.TextSize = 12 * scale;
        foreach (var flight in salvo.Flights)
        {
            if (!flight.Flying && !flight.Flash) continue;
            var shot = flight.Shot;
            var from = frame.View.Project(shot.From);
            var to = TacticalBattleResults.Endpoint(from, frame.View.Project(shot.To), shot.Hit, flight.Index, scale);
            if (flight.Flying)
            {
                var head = Lerp(from, to, flight.Progress);
                var tail = Lerp(from, to, PyMath.Max(0, flight.Progress - 0.10));
                paint.Color = new(255, 220, 149, 135);
                paint.StrokeWidth = 2 * scale;
                canvas.DrawLine(tail, head, paint);
                paint.Color = new(255, 246, 209);
                canvas.DrawCircle(head, 3.1f * scale, paint);
                continue;
            }
            double fraction = (flight.ElapsedMs - flight.LaunchMs - TacticalBattleResults.FlightMs) / (double)TacticalBattleResults.FlashMs;
            byte alpha = (byte)(255 * (1 - fraction));
            if (shot.Hit)
            {
                paint.Color = (shot.Kill || shot.LethalImpact ? new SKColor(255, 72, 65) : new SKColor(255, 216, 110)).WithAlpha(alpha);
                canvas.DrawCircle(to, (5 + 8 * (float)fraction) * scale, paint);
            }
            text.Color = (shot.Hit ? new SKColor(255, 224, 173) : new SKColor(191, 206, 218)).WithAlpha(alpha);
            string caption = shot.Hit ? BattleFormat.Signed(-shot.Damage) : BattleFormat.Localize("SalvoMiss");
            canvas.DrawText(caption, to.X + 6 * scale, to.Y - (11 + 14 * (float)fraction) * scale, text);
        }
        static SKPoint Lerp(SKPoint from, SKPoint to, double fraction) =>
            new(from.X + (float)((to.X - from.X) * fraction), from.Y + (float)((to.Y - from.Y) * fraction));
    }

    private static void DrawFire(SKCanvas canvas, TacticalBattleFrame frame, TacticalFirePlan plan, SKPaint paint)
    {
        var view = frame.View;
        float s = frame.UiScale;
        foreach (var row in plan.Rows)
        {
            if (row.Drone.Jammed || row.Drone.Build is not { } gun) continue;
            bool selectedSector = row.Drone.Id == plan.SelectedDroneId;
            if (!frame.AllFireSectors && !selectedSector) continue;
            paint.Color = Own.WithAlpha(selectedSector ? (byte)52 : (byte)18);
            DrawArc(canvas, view, row.Drone.Pose, gun.Range, gun.ArcDegrees, row.Drone.Pose.Course, paint, filled: true);
        }
        if (plan.Selected is not { } selected) return;
        var source = view.Project(selected.Drone.Pose);
        foreach (var candidate in selected.Group.List.OrderBy(c => c.TargetId == selected.TargetId))
        {
            var target = frame.Battle.Units.FirstOrDefault(u => u.Id == candidate.TargetId);
            if (target is null) continue;
            var endpoint = view.Project(target.Pose);
            bool chosen = candidate.TargetId == selected.TargetId;
            paint.Color = !chosen ? new(74, 80, 87) : candidate.Rear ? new(253, 143, 104) : new(145, 224, 238);
            paint.StrokeWidth = (chosen ? 1.7f : 1) * s;
            float radius = (target.Kind == TacticalUnitKind.Carrier ? 20 : 14) * s;
            paint.Style = SKPaintStyle.Stroke;
            canvas.DrawCircle(endpoint, radius, paint);
            paint.Style = SKPaintStyle.Fill;
            DrawTargetArrow(canvas, source, endpoint, radius, s, paint);
            float x = (source.X + endpoint.X) * 0.5f;
            float y = (source.Y + endpoint.Y) * 0.5f;
            string label = BattleFormat.Probability(candidate.Chance) + (candidate.Rear ? " " + BattleFormat.Localize("FireRearShort") : "");
            ControlVisuals.Label(canvas, label, new(x, y - 23 * s, x + 104 * s, y - 3 * s), true, s);
        }
    }

    private static void DrawTargetArrow(SKCanvas canvas, SKPoint source, SKPoint target, float radius, float scale, SKPaint paint)
    {
        float dx = target.X - source.X, dy = target.Y - source.Y;
        float distance = (float)TacticalGeometry.Hypot(dx, dy);
        if (distance <= radius) return;
        float ux = dx / distance, uy = dy / distance;
        var tip = new SKPoint(target.X - ux * radius, target.Y - uy * radius);
        float length = Math.Min(8 * scale, distance - radius);
        var basePoint = new SKPoint(tip.X - ux * length, tip.Y - uy * length);
        canvas.DrawLine(source, tip, paint);
        canvas.DrawLine(tip, new SKPoint(basePoint.X - uy * length * 0.55f, basePoint.Y + ux * length * 0.55f), paint);
        canvas.DrawLine(tip, new SKPoint(basePoint.X + uy * length * 0.55f, basePoint.Y - ux * length * 0.55f), paint);
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

    private static void DrawRearSector(SKCanvas canvas, TacticalBattleView view, TacticalPoseSnapshot pose,
        double radius, double halfAngle, SKPaint paint)
    {
        var center = view.Project(pose);
        float r = (float)(radius * view.PixelsPerUnit);
        using var sector = new SKPath();
        sector.MoveTo(center);
        sector.ArcTo(SKRect.Create(center.X - r, center.Y - r, 2 * r, 2 * r),
            (float)(pose.Course + 180 - halfAngle), (float)(2 * halfAngle), false);
        sector.Close();
        var red = new SKColor(127, 27, 35);
        paint.Color = red.WithAlpha(100);
        canvas.DrawPath(sector, paint);
        paint.Color = red.WithAlpha(190);
        paint.Style = SKPaintStyle.Stroke;
        canvas.DrawPath(sector, paint);
        paint.Style = SKPaintStyle.Fill;
    }

    private static void DrawUnit(SKCanvas canvas, SKPoint p, TacticalUnitSnapshot unit, bool own, bool selected, float scale, SKPaint paint)
    {
        paint.Color = unit.State is TacticalUnitState.Air or TacticalUnitState.Alive ? own ? Own : Enemy : Inactive;
        paint.StrokeWidth = 1.5f * scale;
        if (unit.Kind == TacticalUnitKind.Drone && unit.State == TacticalUnitState.Destroyed)
        {
            DrawCross(canvas, p, 6 * scale, paint);
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

    private static void DrawCross(SKCanvas canvas, SKPoint center, float radius, SKPaint paint)
    {
        canvas.DrawLine(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius, paint);
        canvas.DrawLine(center.X - radius, center.Y + radius, center.X + radius, center.Y - radius, paint);
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
        DrawOffscreenMarkers(canvas, frame, units, occupied, text, bars);
        foreach (var unit in units)
        {
            bool carrier = unit.Kind == TacticalUnitKind.Carrier;
            bool wreck = !carrier && unit.State == TacticalUnitState.Destroyed;
            var p = view.Project(unit.Pose);
            if (!viewport.Contains(p.X, p.Y)) continue;
            bool disabled = unit.State == TacticalUnitState.Disabled;
            string value = unit.Id;
            if (!wreck)
            {
                value += " " + BattleFormat.Number(unit.Hull);
                value += carrier ? "/" + BattleFormat.Number(unit.MaxHull) : disabled
                    ? " · " + BattleFormat.Localize("UnitDisabledShort") : "+" + BattleFormat.Number(unit.Armor);
            }
            value = BattleFormat.Text(value);
            bool disabledDrone = !carrier && disabled;
            int targets = unit.State == TacticalUnitState.Air
                ? frame.FirePlan?.Rows.FirstOrDefault(row => row.Drone.Id == unit.Id)?.Group.List.Length ?? 0 : 0;
            string badge = targets > 0 ? BattleFormat.Number(targets) : "";
            float badgeSize = disabledDrone || targets > 0 ? Math.Max(16 * s, text.MeasureText(badge) + 4 * s) : 0;
            float badgeSpace = badgeSize > 0 ? badgeSize + 4 * s : 0;
            float w = (float)PyMath.Max(30 * s, text.MeasureText(value)) + 4 * s + badgeSpace;
            float h = (carrier || wreck ? 15 : 25) * s;
            if (!TryPlace(p, w, h, viewport, occupied, s, out var rect)) continue;
            bars.Color = new(116, 145, 164, 100);
            canvas.DrawLine(p, new SKPoint(rect.MidX, rect.Top), bars);
            if (disabledDrone)
            {
                bars.Color = new(231, 67, 70);
                bars.StrokeWidth = 2 * s;
                DrawCross(canvas, new(rect.Left + badgeSize * 0.5f, rect.Top + badgeSize * 0.5f),
                    badgeSize * 0.5f - s, bars);
                bars.StrokeWidth = s;
            }
            else if (targets > 0)
            {
                var square = SKRect.Create(rect.Left, rect.Top, badgeSize, badgeSize);
                bars.Color = new(243, 146, 49);
                canvas.DrawRect(square, bars);
                text.Color = new(21, 22, 26);
                canvas.DrawText(badge, square.MidX - text.MeasureText(badge) * 0.5f,
                    square.MidY - (text.FontMetrics.Ascent + text.FontMetrics.Descent) * 0.5f, text);
            }
            float labelX = rect.Left + badgeSpace + 2 * s;
            text.Color = disabled || unit.State == TacticalUnitState.Destroyed ? new(144, 151, 159) : new(226, 235, 242);
            canvas.DrawText(value, labelX, rect.Top - text.FontMetrics.Ascent, text);
            if (carrier || wreck) continue;
            DrawBar(rect.Top + 15 * s, unit.Armor, unit.MaxArmor, disabled ? Inactive : new(126, 159, 181));
            DrawBar(rect.Top + 20 * s, unit.Hull, unit.MaxHull, disabled ? Inactive : Own);

            void DrawBar(float y, int amount, int max, SKColor color)
            {
                bars.Color = new(30, 50, 65);
                canvas.DrawRect(labelX, y, 30 * s, 3 * s, bars);
                if (max <= 0) return;
                bars.Color = color;
                canvas.DrawRect(labelX, y, 30 * s * (float)PyMath.Clamp(amount / (double)max, 0, 1), 3 * s, bars);
            }
        }
    }

    private static void DrawOffscreenMarkers(SKCanvas canvas, TacticalBattleFrame frame,
        ImmutableArray<TacticalUnitSnapshot> units, List<SKRect> occupied, SKPaint text, SKPaint paint)
    {
        var view = frame.View;
        float scale = frame.UiScale;
        var map = SKRect.Create(view.PixelWidth, view.PixelHeight);
        var edge = map;
        edge.Inflate(-6 * scale, -6 * scale);
        float height = 22 * scale, iconWidth = 24 * scale;
        if (edge.Height < height || edge.Width < iconWidth) return;

        foreach (var unit in units)
        {
            if (unit.State is not (TacticalUnitState.Air or TacticalUnitState.Disabled or TacticalUnitState.Alive)) continue;
            var target = view.Project(unit.Pose);
            if (!float.IsFinite(target.X) || !float.IsFinite(target.Y) || map.Contains(target.X, target.Y)) continue;
            string name = ControlText.Ellipsize(BattleFormat.Text(unit.Id), edge.Width - iconWidth, text);
            float width = iconWidth + text.MeasureText(name);
            float dx = target.X - edge.MidX, dy = target.Y - edge.MidY;
            float horizontal = edge.Width * 0.5f / Math.Abs(dx);
            float vertical = edge.Height * 0.5f / Math.Abs(dy);
            bool side = horizontal <= vertical;
            float reach = Math.Min(horizontal, vertical);
            float left = edge.MidX + dx * reach - width * 0.5f;
            float top = edge.MidY + dy * reach - height * 0.5f;
            float step = (side ? height : width) + 3 * scale;
            for (int i = 0; i < 2 * units.Length + 1; i++)
            {
                // Search either side of the ray's intersection with this edge.
                float shift = ((i + 1) / 2) * (i % 2 == 0 ? -step : step);
                var rect = SKRect.Create(
                    Math.Clamp(left + (side ? 0 : shift), edge.Left, Math.Max(edge.Left, edge.Right - width)),
                    Math.Clamp(top + (side ? shift : 0), edge.Top, edge.Bottom - height), width, height);
                if (occupied.Any(o => o.IntersectsWith(rect))) continue;
                occupied.Add(rect);
                DrawMarker(rect);
                break;
            }

            void DrawMarker(SKRect rect)
            {
                paint.Color = new(10, 24, 36, 240);
                canvas.DrawRect(rect, paint);
                var center = new SKPoint(rect.Left + 10 * scale, rect.MidY);
                float length = (float)TacticalGeometry.Hypot(target.X - center.X, target.Y - center.Y);
                var delta = new SKPoint((target.X - center.X) / length * 6 * scale,
                    (target.Y - center.Y) / length * 6 * scale);
                paint.Color = unit.State == TacticalUnitState.Disabled ? new(151, 158, 167)
                    : unit.Side == frame.Battle.PlayerSide ? Own : Enemy;
                paint.StrokeWidth = 1.6f * scale;
                DrawTargetArrow(canvas, new(center.X - delta.X, center.Y - delta.Y),
                    new(center.X + delta.X, center.Y + delta.Y), 0, scale, paint);
                text.Color = paint.Color;
                canvas.DrawText(name, rect.Left + 20 * scale,
                    rect.MidY - (text.FontMetrics.Ascent + text.FontMetrics.Descent) * 0.5f, text);
            }
        }
        paint.StrokeWidth = scale;
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
