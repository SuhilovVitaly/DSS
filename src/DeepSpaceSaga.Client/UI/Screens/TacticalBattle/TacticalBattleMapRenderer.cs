using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;
using SkiaSharp;
using SkinStyle = DeepSpaceSaga.Client.UI.Screens.TacticalBattle.TacticalBattleSkinRenderScope;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static class TacticalBattleMapRenderer
{
    internal static readonly SKColor Own = new(47, 191, 143);
    internal static readonly SKColor Enemy = new(232, 98, 58);
    internal static readonly SKColor Inactive = new(85, 85, 85);
    internal static readonly SKColor Jammed = new(247, 207, 77);
    private static readonly TacticalMapSettings GridSettings = new();
    internal static readonly SKColor Background = new(2, 12, 22);
    private static readonly SKColor Ruler = new(135, 175, 197);

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
        if (SkinStyle.HasBoxStyle("field.background"))
            SkinStyle.Box(canvas, "field.background", SKRect.Create(view.PixelWidth, view.PixelHeight), Background, null, scale);
        using (SkinStyle.Stroke(paint, "field.grid", Background, scale, scale))
            GridRenderer.DrawLines(canvas, view.CenterX, view.CenterY, view.PixelsPerUnit, view.PixelWidth, view.PixelHeight,
                GridSettings, paint, Background, baseStep: 8, GridColor);
        paint.PathEffect = null;
        paint.StrokeWidth = scale;
        var units = VisibleUnits(frame);
        if (frame.Presentation == TacticalBattlePresentation.Current)
        {
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
                    DrawRearSector(canvas, view, firing ? unit.Pose : end, firing ? 70 : 55, battle.Rules.RearArc, scale, paint);
                }
                if (frame.EnemyForecast)
                {
                    using (SkinStyle.Stroke(paint, "field.move.forecast", Enemy.WithAlpha(112), scale, scale))
                        canvas.DrawLine(view.Project(unit.Pose), view.Project(end), paint);
                    paint.PathEffect = null;
                    DrawPoint(canvas, view.Project(end), "field.move.forecast.endpoint", Enemy.WithAlpha(210), 3, scale, paint);
                }
            }
            if (frame.Movement is { } movement) DrawMovement(canvas, frame, movement, paint);
            if (frame.FirePlan is { } fire) DrawFire(canvas, frame, fire, paint);
        }

        DrawTrails(canvas, frame, paint);
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

    private static SKColor GridColor(SKColor fallback) => SkinStyle.Color("field.grid", "stroke", fallback);

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
        using var text = ControlText.Paint(scale, "shot.caption");
        text.TextSize = 12 * scale;
        foreach (var flight in salvo.Flights)
        {
            if (!flight.Flying && !flight.Flash) continue;
            var shot = flight.Shot;
            var from = frame.View.Project(shot.From);
            var to = TacticalBattleResults.Endpoint(from, frame.View.Project(shot.To), shot.Hit, flight.Index, scale);
            string state = shot.Kill || shot.LethalImpact ? "kill" : shot.Hit ? "hit" : "miss";
            if (flight.Flying)
            {
                var head = Lerp(from, to, flight.Progress);
                string trail = $"shot.trail.{state}";
                double tailFraction = 0.10;
                if (SkinStyle.Has(trail, "metrics.tail-length"))
                {
                    double length = TacticalGeometry.Hypot(to.X - from.X, to.Y - from.Y);
                    tailFraction = PyMath.Clamp(SkinStyle.Number(trail, "metrics.tail-length", 0), 0, 256) * scale / (double)PyMath.Max(1, length);
                }
                var tail = Lerp(from, to, PyMath.Max(0, flight.Progress - tailFraction));
                using (SkinStyle.Stroke(paint, trail, new(255, 220, 149, 135), 2 * scale, scale))
                    canvas.DrawLine(tail, head, paint);
                paint.PathEffect = null;
                DrawPoint(canvas, head, $"shot.projectile.{state}", new(255, 246, 209), 3.1f, scale, paint,
                    (float)TacticalGeometry.Bearing(from.X, from.Y, to.X, to.Y));
                continue;
            }
            double fraction = (flight.ElapsedMs - flight.LaunchMs - TacticalBattleResults.FlightMs) / (double)TacticalBattleResults.FlashMs;
            byte alpha = (byte)(255 * (1 - fraction));
            if (shot.Hit)
            {
                string flash = $"shot.flash.{state}";
                float growth = (5 + 8 * (float)fraction) * (float)PyMath.Clamp(SkinStyle.Number(flash, "metrics.flash-scale", 1), 0.25f, 4);
                var color = state == "kill" ? new SKColor(255, 72, 65) : new SKColor(255, 216, 110);
                DrawPoint(canvas, to, flash, color, growth, scale, paint, alpha: alpha);
            }
            string caption = shot.Hit ? BattleFormat.Signed(-shot.Damage) : BattleFormat.Localize("SalvoMiss");
            text.Color = shot.Hit ? new(255, 224, 173) : new(191, 206, 218);
            text.TextSize = 12 * scale;
            text.Typeface = MenuStyle.TypefaceRegular;
            SkinStyle.Text(text, new SkinStyle.Role(shot.Hit ? $"shot.damage.{state}" : $"shot.caption.{state}",
                $"shot.caption.{state}"), scale, caption);
            text.Color = Fade(text.Color, alpha);
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
            DrawStyledSector(canvas, view, row.Drone.Pose, gun.Range, gun.ArcDegrees,
                selectedSector ? "field.fire.sector.own.selected" : "field.fire.sector.own",
                Own.WithAlpha(selectedSector ? (byte)52 : (byte)18), s, paint);
        }
        if (plan.Selected is not { } selected) return;
        var source = view.Project(selected.Drone.Pose);
        foreach (var candidate in selected.Group.List.OrderBy(c => c.TargetId == selected.TargetId))
        {
            var target = frame.Battle.Units.FirstOrDefault(u => u.Id == candidate.TargetId);
            if (target is null) continue;
            var endpoint = view.Project(target.Pose);
            bool chosen = candidate.TargetId == selected.TargetId;
            string state = chosen ? candidate.Rear ? "assigned.rear" : "assigned" : "candidate";
            var color = !chosen ? new SKColor(74, 80, 87) : candidate.Rear ? new(253, 143, 104) : new(145, 224, 238);
            float width = (chosen ? 1.7f : 1) * s;
            string ring = $"field.fire.target.{state}";
            float radius = (float)PyMath.Clamp(SkinStyle.Number(ring, "radius", target.Kind == TacticalUnitKind.Carrier ? 20 : 14), 2, 28) * s;
            paint.Style = SKPaintStyle.Stroke;
            using (SkinStyle.Stroke(paint, ring, color, width, s))
                canvas.DrawCircle(endpoint, radius, paint);
            paint.Style = SKPaintStyle.Fill;
            string arrow = $"field.fire.line.{state}";
            using (SkinStyle.Stroke(paint, arrow, color, width, s))
                DrawTargetArrow(canvas, source, endpoint, radius, s, paint, arrow);
            paint.PathEffect = null;
            float x = (source.X + endpoint.X) * 0.5f;
            float y = (source.Y + endpoint.Y) * 0.5f;
            string label = BattleFormat.Probability(candidate.Chance) + (candidate.Rear ? " " + BattleFormat.Localize("FireRearShort") : "");
            ControlVisuals.Label(canvas, label, new(x, y - 23 * s, x + 104 * s, y - 3 * s), true, s,
                role: $"field.fire.label.chance.{state}");
        }
    }

    private static void DrawTargetArrow(SKCanvas canvas, SKPoint source, SKPoint target, float radius, float scale, SKPaint paint, string role)
    {
        float dx = target.X - source.X, dy = target.Y - source.Y;
        float distance = (float)TacticalGeometry.Hypot(dx, dy);
        if (distance <= radius) return;
        float ux = (float)((double)dx / distance), uy = (float)((double)dy / distance);
        var tip = new SKPoint(target.X - ux * radius, target.Y - uy * radius);
        float maximumLength = (float)PyMath.Clamp(SkinStyle.Number(role, "metrics.arrow-length", 8), 1, 24) * scale;
        float length = (float)PyMath.Min(maximumLength, distance - radius);
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
            paint.Color = SkinStyle.Color("field.move.area", "fill", new(73, 160, 250, 45));
            canvas.DrawPath(area, paint);
            paint.Style = SKPaintStyle.Stroke;
            using (SkinStyle.Stroke(paint, "field.move.area", new(73, 160, 250, 125), frame.UiScale, frame.UiScale))
                canvas.DrawPath(area, paint);
            paint.PathEffect = null;
            paint.Style = SKPaintStyle.Fill;
        }
        if (movement.SelectedOrder is { Unit.Jammed: false, Unit.Build: { } weapon } selected && !selected.Path.IsDefaultOrEmpty)
        {
            var end = selected.Path[^1];
            DrawStyledSector(canvas, view, end, weapon.Range, weapon.ArcDegrees,
                "field.fire.sector.own.forecast", Own.WithAlpha(35), frame.UiScale, paint);
        }
        foreach (var row in movement.Orders)
        {
            if (row.Path.IsDefaultOrEmpty) continue;
            bool selectedRow = row.Unit.Id == movement.Selected;
            float s = frame.UiScale;
            string state = selectedRow ? "selected" : "normal";
            var color = selectedRow ? new SKColor(154, 235, 246) : new SKColor(126, 187, 196, 170);
            float width = (selectedRow ? 1.6f : 1) * s;
            paint.Style = SKPaintStyle.Stroke;
            using var path = new SKPath();
            path.MoveTo(view.Project(row.Path[0]));
            for (int i = 1; i < row.Path.Length; i++) path.LineTo(view.Project(row.Path[i]));
            using (SkinStyle.Stroke(paint, $"field.move.path.{state}", color, width, s))
                canvas.DrawPath(path, paint);
            paint.PathEffect = null;
            var end = row.Path[^1];
            var p = view.Project(end);
            string handle = $"field.move.handle.{state}";
            using (SkinStyle.Stroke(paint, handle, color, width, s))
                DrawPoint(canvas, p, handle, paint.Color, 7, s, paint);
            paint.PathEffect = null;
            paint.Style = SKPaintStyle.Fill;
            string ghost = $"field.move.ghost.{state}";
            paint.Color = SkinStyle.Color(ghost, "fill", color);
            if (!DrawStyledShape(canvas, p, ghost, 8 * s, s, paint, (float)end.Course))
                DrawDrone(canvas, p, end.Course, row.Unit.FrameId, s, paint, ghost);
        }
    }

    private static void DrawTrails(SKCanvas canvas, TacticalBattleFrame frame, SKPaint paint)
    {
        if (frame.Battle.Paths.IsDefaultOrEmpty) return;
        float s = frame.UiScale;
        float[] classicDash = [4 * s, 4 * s];
        paint.Style = SKPaintStyle.Stroke;
        foreach (var trail in frame.Battle.Paths)
        {
            if (trail.Points.IsDefaultOrEmpty || trail.Points.Length == 1) continue;
            var unit = frame.Battle.Units.FirstOrDefault(u => u.Id == trail.DroneId);
            if (unit is null) continue;
            bool own = unit.Side == frame.Battle.PlayerSide;
            string role = own ? "field.move.trail.own" : "field.move.trail.enemy";
            using var effect = SkinStyle.Stroke(paint, role, (own ? Own : Enemy).WithAlpha(100), s, s, classicDash);
            using var path = new SKPath();
            path.MoveTo(frame.View.Project(trail.Points[0]));
            for (int i = 1; i < trail.Points.Length; i++) path.LineTo(frame.View.Project(trail.Points[i]));
            canvas.DrawPath(path, paint);
        }
        paint.PathEffect = null; paint.Style = SKPaintStyle.Fill;
    }

    private static void DrawStyledSector(SKCanvas canvas, TacticalBattleView view, TacticalPoseSnapshot pose, double radius,
        double halfAngle, string role, SKColor fallback, float scale, SKPaint paint)
    {
        paint.Color = SkinStyle.Color(role, "fill", fallback);
        DrawArc(canvas, view, pose, radius, halfAngle, pose.Course, paint, filled: true);
        if (!SkinStyle.Has(role, "stroke")) return;
        using (SkinStyle.Stroke(paint, role, fallback, scale, scale))
            DrawArc(canvas, view, pose, radius, halfAngle, pose.Course, paint, filled: false);
        paint.PathEffect = null;
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
        double radius, double halfAngle, float scale, SKPaint paint)
    {
        var center = view.Project(pose);
        float r = (float)(radius * view.PixelsPerUnit);
        using var sector = new SKPath();
        sector.MoveTo(center);
        sector.ArcTo(SKRect.Create(center.X - r, center.Y - r, 2 * r, 2 * r),
            (float)(pose.Course + 180 - halfAngle), (float)(2 * halfAngle), false);
        sector.Close();
        var red = new SKColor(127, 27, 35);
        const string role = "field.fire.sector.rear";
        paint.Color = SkinStyle.Color(role, "fill", red.WithAlpha(100));
        canvas.DrawPath(sector, paint);
        paint.Style = SKPaintStyle.Stroke;
        using (SkinStyle.Stroke(paint, role, red.WithAlpha(190), scale, scale))
            canvas.DrawPath(sector, paint);
        paint.PathEffect = null;
        paint.Style = SKPaintStyle.Fill;
    }

    private static void DrawUnit(SKCanvas canvas, SKPoint p, TacticalUnitSnapshot unit, bool own, bool selected, float scale, SKPaint paint)
    {
        bool carrier = unit.Kind == TacticalUnitKind.Carrier;
        bool active = unit.State is TacticalUnitState.Air or TacticalUnitState.Alive;
        var color = active ? own ? Own : Enemy : Inactive;
        string family = carrier ? "field.unit.carrier" : "field.unit.drone";
        string side = own ? "own" : "enemy";
        string state = unit.State switch
        {
            TacticalUnitState.Disabled => ".inactive.disabled",
            TacticalUnitState.Destroyed => ".inactive.destroyed",
            _ => "",
        };
        var role = new SkinStyle.Role($"{family}.{side}{state}.{unit.FrameId}", family + state);
        paint.StrokeWidth = 1.5f * scale;
        if (!carrier && unit.State == TacticalUnitState.Destroyed)
        {
            string wreck = $"field.unit.drone.wreck.{side}";
            using (SkinStyle.Stroke(paint, wreck, color, 1.5f * scale, scale))
            {
                float radius = MarkerRadius(wreck, 6, scale);
                if (!DrawStyledShape(canvas, p, wreck, radius, scale, paint, 0))
                    DrawCross(canvas, p, radius, paint);
            }
            paint.PathEffect = null;
            return;
        }

        paint.Color = SkinStyle.Color(role, "fill", color);
        bool outlineOnly = !carrier && unit.State == TacticalUnitState.Disabled;
        if (!outlineOnly)
            DrawBody(paint);

        if (outlineOnly || SkinStyle.Has(role, "stroke"))
        {
            paint.Style = SKPaintStyle.Stroke;
            using (SkinStyle.Stroke(paint, role, color, 1.5f * scale, scale))
                DrawBody(paint, allowImage: false);
            paint.PathEffect = null;
        }

        if (!carrier && unit.State == TacticalUnitState.Air)
        {
            if (unit.Jammed) DrawRing("jammed", Jammed, 10);
            if (selected) DrawRing("selected", SKColors.White.WithAlpha(220), 12);
        }
        paint.Style = SKPaintStyle.Fill;

        void DrawRing(string name, SKColor fallback, float radius)
        {
            var ring = new SkinStyle.Role($"{family}.{side}.{name}", $"{family}.{name}");
            paint.Style = SKPaintStyle.Stroke;
            using (SkinStyle.Stroke(paint, ring, fallback, 1.5f * scale, scale))
                DrawPoint(canvas, p, ring, paint.Color, radius, scale, paint);
            paint.PathEffect = null;
        }

        void DrawBody(SKPaint marker, bool allowImage = true)
        {
            float radius = MarkerRadius(role, carrier ? 16 : 8, scale);
            if (DrawStyledShape(canvas, p, role, radius, scale, marker, (float)unit.Pose.Course, allowImage))
                return;
            if (!carrier)
            {
                DrawDrone(canvas, p, unit.Pose.Course, unit.FrameId, scale, marker, role);
                return;
            }
            using var hex = new SKPath();
            for (int i = 0; i < 6; i++)
            {
                var (dx, dy) = TacticalGeometry.Vec(unit.Pose.Course + i * 60, radius);
                var point = new SKPoint(p.X + (float)dx, p.Y + (float)dy);
                if (i == 0) hex.MoveTo(point); else hex.LineTo(point);
            }
            hex.Close();
            canvas.DrawPath(hex, marker);
        }
    }

    private static void DrawCross(SKCanvas canvas, SKPoint center, float radius, SKPaint paint)
    {
        canvas.DrawLine(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius, paint);
        canvas.DrawLine(center.X - radius, center.Y + radius, center.X + radius, center.Y - radius, paint);
    }

    private static void DrawDrone(SKCanvas canvas, SKPoint p, double course, string frameId, float scale, SKPaint paint, SkinStyle.Role role)
    {
        float size = (frameId == "heavy" ? 1.25f : 1f) * MarkerRadius(role, 8, scale) * 0.125f;
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

    private static byte FadeAlpha(byte alpha, byte progress) => (byte)((alpha * progress + 127) / 255d);
    private static SKColor Fade(SKColor color, byte alpha) => color.WithAlpha(FadeAlpha(color.Alpha, alpha));

    private static float MarkerRadius(SkinStyle.Role role, float radius, float scale) =>
        (float)PyMath.Clamp(SkinStyle.Number(role, "size", radius * 2) * 0.5f, 0.5f, 32) * scale;

    private static void DrawPoint(SKCanvas canvas, SKPoint point, SkinStyle.Role role, SKColor fallback,
        float radius, float scale, SKPaint paint, float angle = 0, byte alpha = 255)
    {
        string property = paint.Style == SKPaintStyle.Stroke || !SkinStyle.Has(role, "fill") ? "stroke" : "fill";
        var color = SkinStyle.Color(role, property, fallback);
        paint.Color = Fade(color, alpha);
        radius = MarkerRadius(role, radius, scale);
        if (!DrawStyledShape(canvas, point, role, radius, scale, paint, angle, alpha: alpha))
            canvas.DrawCircle(point, radius, paint);
    }

    private static bool DrawStyledShape(SKCanvas canvas, SKPoint point, SkinStyle.Role role, float radius,
        float scale, SKPaint paint, float angle, bool allowImage = true, byte alpha = 255)
    {
        string shape = SkinStyle.Shape(role);
        bool image = allowImage && SkinStyle.Has(role, "image");
        if (!image && shape == "native") return false;

        canvas.Save();
        canvas.Translate(point.X, point.Y);
        canvas.RotateDegrees(angle);
        var bounds = SKRect.Create(-radius, -radius, radius * 2, radius * 2);
        if (image && SkinStyle.Image(canvas, role, bounds, alpha))
        {
            canvas.Restore();
            return true;
        }

        if (shape == "circle")
            canvas.DrawCircle(0, 0, radius, paint);
        else if (shape == "square")
            canvas.DrawRect(bounds, paint);
        else
        {
            int sides = shape == "hexagon" ? 6 : shape == "triangle" ? 3 : 4;
            using var path = new SKPath();
            path.MoveTo(radius, 0);
            for (int i = 1; i < sides; i++)
            {
                float radians = (float)(MathF.Tau * i / (double)sides);
                path.LineTo(radius * MathF.Cos(radians), radius * MathF.Sin(radians));
            }
            path.Close();
            canvas.DrawPath(path, paint);
        }
        canvas.Restore();
        return true;
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
            string side = unit.Side == frame.Battle.PlayerSide ? "own" : "enemy";
            var labelRole = wreck
                ? new SkinStyle.Role("field.unit.drone.wreck.label", $"field.label.{side}.destroyed")
                : new SkinStyle.Role(disabled ? $"field.label.{side}.disabled" : $"field.label.{side}");
            SetMapText(text, labelRole, disabled || unit.State == TacticalUnitState.Destroyed ? new(144, 151, 159) : new(226, 235, 242), value, s);
            float labelWidth = text.MeasureText(value);
            int targets = TargetCount(frame, unit);
            string badge = targets > 0 ? BattleFormat.Number(targets) : "";
            string badgeRole = $"field.fire.target.badge.{side}";
            const string crossRole = "field.label.disabled-cross";
            if (targets > 0)
                SetMapText(text, badgeRole, new(21, 22, 26), badge, s);
            float badgeSize = disabledDrone ? (float)PyMath.Clamp(SkinStyle.Number(crossRole, "size", 10), 4, 15) * s
                : targets > 0 ? (float)PyMath.Max((float)PyMath.Clamp(SkinStyle.Number(badgeRole, "size", 16), 12, 24) * s, text.MeasureText(badge) + 4 * s) : 0;
            float badgeSpace = badgeSize > 0 ? badgeSize + 4 * s : 0;
            float w = (float)PyMath.Max(30 * s, labelWidth) + 4 * s + badgeSpace;
            float h = (carrier || wreck ? 15 : 25) * s;
            if (!TryPlace(p, w, h, viewport, occupied, s, out var rect)) continue;
            using (SkinStyle.Stroke(bars, labelRole, new(116, 145, 164, 100), s, s))
                canvas.DrawLine(p, new SKPoint(rect.MidX, rect.Top), bars);
            bars.PathEffect = null;
            if (disabledDrone)
            {
                using var effect = SkinStyle.Stroke(bars, crossRole, new(231, 67, 70), 1.5f * s, s);
                float half = badgeSize * 0.5f;
                float radius = (float)PyMath.Max(s, half - bars.StrokeWidth * 0.5f);
                float cx = rect.Left + half, cy = rect.Top + half;
                if (!DrawStyledShape(canvas, new(cx, cy), crossRole, radius, s, bars, 0))
                {
                    canvas.DrawLine(cx - radius, cy, cx + radius, cy, bars);
                    canvas.DrawLine(cx, cy - radius, cx, cy + radius, bars);
                }
                bars.PathEffect = null;
                bars.StrokeWidth = s;
            }
            else if (targets > 0)
            {
                var square = SKRect.Create(rect.Left, rect.Top, badgeSize, badgeSize);
                SkinStyle.Box(canvas, badgeRole, square, new(243, 146, 49), null, s);
                canvas.DrawText(badge, square.MidX - text.MeasureText(badge) * 0.5f,
                    square.MidY - (text.FontMetrics.Ascent + text.FontMetrics.Descent) * 0.5f, text);
            }
            float labelX = rect.Left + badgeSpace + 2 * s;
            SetMapText(text, labelRole, disabled || unit.State == TacticalUnitState.Destroyed ? new(144, 151, 159) : new(226, 235, 242), value, s);
            canvas.DrawText(value, labelX, rect.Top - text.FontMetrics.Ascent, text);
            if (carrier || wreck) continue;
            DrawBar("armor", rect.Top + 15 * s, unit.Armor, unit.MaxArmor, disabled ? Inactive : new(126, 159, 181));
            DrawBar("hull", rect.Top + 20 * s, unit.Hull, unit.MaxHull, disabled ? Inactive : Own);

            void DrawBar(string name, float y, int amount, int max, SKColor color)
            {
                string role = disabled ? $"field.bar.{name}.disabled" : $"field.bar.{name}.{side}";
                string track = $"field.bar.{name}.track";
                SkinStyle.Box(canvas, track, SKRect.Create(labelX, y, 30 * s, 3 * s), new(30, 50, 65), null, s);
                if (max <= 0) return;
                float width = 30 * s * (float)PyMath.Clamp(amount / (double)max, 0, 1);
                SkinStyle.Box(canvas, role, SKRect.Create(labelX, y, width, 3 * s), color, null, s);
            }
        }
    }

    private static void SetMapText(SKPaint paint, SkinStyle.Role role, SKColor fallback, string value, float scale)
    {
        paint.Typeface = MenuStyle.TypefaceRegular;
        paint.TextSize = 11 * scale;
        paint.Color = fallback;
        SkinStyle.Text(paint, role, scale, value);
        paint.TextSize = (float)PyMath.Min(paint.TextSize, 14 * scale);
    }

    internal static int TargetCount(TacticalBattleFrame frame, TacticalUnitSnapshot shooter)
    {
        if (frame.FirePlan is not { } plan || shooter.Kind != TacticalUnitKind.Drone ||
            shooter.State != TacticalUnitState.Air || shooter.Jammed) return 0;
        if (shooter.Side == frame.Battle.PlayerSide)
            return plan.Rows.FirstOrDefault(row => row.Drone.Id == shooter.Id)?.Group.List.Length ?? 0;
        if (shooter.Build is not { } weapon) return 0;

        // Only public geometry is needed for the count. Incoming describes assigned shots.
        int count = 0;
        foreach (var target in frame.Battle.Units)
        {
            if (target.Side == shooter.Side || target.State is not (TacticalUnitState.Air or TacticalUnitState.Alive)) continue;
            double distance = TacticalGeometry.Dist(shooter.Pose.X, shooter.Pose.Y, target.Pose.X, target.Pose.Y);
            double angle = TacticalGeometry.Norm(TacticalGeometry.Bearing(shooter.Pose.X, shooter.Pose.Y,
                target.Pose.X, target.Pose.Y) - shooter.Pose.Course);
            if (distance <= weapon.Range && Math.Abs(angle) <= weapon.ArcDegrees) count++;
        }
        return count;
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
            string sideRole = unit.Side == frame.Battle.PlayerSide ? "own" : "enemy";
            if (unit.State == TacticalUnitState.Disabled) sideRole += ".disabled";
            var fallback = unit.State == TacticalUnitState.Disabled ? new SKColor(151, 158, 167)
                : unit.Side == frame.Battle.PlayerSide ? Own : Enemy;
            string label = BattleFormat.Text(unit.Id);
            SetMapText(text, $"field.offscreen.label.{sideRole}", fallback, label, scale);
            string name = ControlText.Ellipsize(label, edge.Width - iconWidth, text);
            float width = iconWidth + text.MeasureText(name);
            float dx = target.X - edge.MidX, dy = target.Y - edge.MidY;
            float horizontal = (float)(edge.Width * 0.5f / (double)Math.Abs(dx));
            float vertical = (float)(edge.Height * 0.5f / (double)Math.Abs(dy));
            bool side = horizontal <= vertical;
            float reach = (float)PyMath.Min(horizontal, vertical);
            float left = edge.MidX + dx * reach - width * 0.5f;
            float top = edge.MidY + dy * reach - height * 0.5f;
            float step = (side ? height : width) + 3 * scale;
            for (int i = 0; i < 2 * units.Length + 1; i++)
            {
                // Search either side of the ray's intersection with this edge.
                float shift = ((i + 1) >> 1) * (i % 2 == 0 ? -step : step);
                var rect = SKRect.Create(
                    (float)PyMath.Clamp(left + (side ? 0 : shift), edge.Left, PyMath.Max(edge.Left, edge.Right - width)),
                    (float)PyMath.Clamp(top + (side ? shift : 0), edge.Top, edge.Bottom - height), width, height);
                if (occupied.Any(o => o.IntersectsWith(rect))) continue;
                occupied.Add(rect);
                DrawMarker(rect);
                break;
            }

            void DrawMarker(SKRect rect)
            {
                SkinStyle.Box(canvas, $"field.offscreen.background.{sideRole}", rect, new(10, 24, 36, 240), null, scale);
                var center = new SKPoint(rect.Left + 10 * scale, rect.MidY);
                float length = (float)TacticalGeometry.Hypot(target.X - center.X, target.Y - center.Y);
                string arrow = $"field.offscreen.arrow.{sideRole}";
                float radius = (float)PyMath.Clamp(SkinStyle.Number(arrow, "size", 12), 2, 17) * scale * 0.5f;
                var delta = new SKPoint((float)((target.X - center.X) / (double)length) * radius,
                    (float)((target.Y - center.Y) / (double)length) * radius);
                using (SkinStyle.Stroke(paint, arrow, fallback, 1.6f * scale, scale))
                {
                    if (!DrawStyledShape(canvas, center, arrow, radius, scale, paint,
                        (float)TacticalGeometry.Bearing(center.X, center.Y, target.X, target.Y)))
                        DrawTargetArrow(canvas, new(center.X - delta.X, center.Y - delta.Y),
                            new(center.X + delta.X, center.Y + delta.Y), 0, scale, paint, arrow);
                }
                paint.PathEffect = null;
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
        using (SkinStyle.Stroke(paint, "field.ruler", Ruler, scale, scale))
        {
            float tick = (float)PyMath.Clamp(SkinStyle.Number("field.ruler", "metrics.tick-size", 4), 1, 10) * scale;
            canvas.DrawLine(x, y, end, y, paint);
            canvas.DrawLine(x, y - tick, x, y + tick, paint);
            canvas.DrawLine(end, y - tick, end, y + tick, paint);
        }
        paint.PathEffect = null;
        string label = BattleFormat.Number(distance, distance >= 1 ? 0 : 2) + " " + BattleFormat.Localize("MapUnits");
        ControlVisuals.Label(canvas, label, new(x, y - 22 * scale, x + 130 * scale, y - 5 * scale), true, scale, role: "field.ruler");
    }
}
