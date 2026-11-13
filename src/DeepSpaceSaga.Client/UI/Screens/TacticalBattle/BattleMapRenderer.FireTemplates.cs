using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static partial class TacticalBattleMapRenderer
{
    private static readonly SKColor TTRust = SKColor.Parse("#AD5C44"), TTRustEdge = SKColor.Parse("#5A2416"),
        TTReticle = SKColor.Parse("#FF4628"), TTAlt = SKColor.Parse("#FF9A6A");
    private static readonly SKColor TTMiss = SKColor.Parse("#969CA8"), TTMissEdge = SKColor.Parse("#3C404A");

    private static string ExplosionName(TacticalShotFlight flight) => flight.Shot.Killed ? "boom-kill" :
        flight.Shot.Hit && !flight.Shot.Absorbed ? "boom-hit" : "puff";

    private static double ExplosionDuration(TacticalShotFlight flight) =>
        TacticalBattleSkinRenderScope.EffectStrip(ExplosionName(flight)) is { } strip ? strip.Frames * strip.FrameMs : TacticalBattleResults.FlashMs;

    private static void DrawTabletopProjectiles(SKCanvas c, TacticalBattleFrame f, TacticalSalvoFrame salvo)
    {
        foreach (var flight in salvo.Flights.Where(flight => flight.Flying))
        {
            var a = f.View.Project(flight.Shot.From); var b = f.View.Project(flight.Shot.To);
            Stroke(c, a, b, Yellow.WithAlpha(70), 1.1f, [5, 5]);
            var head = a + Scale(b - a, (float)flight.Progress);
            float length = SKPoint.Distance(a, b);
            var dir = Scale(b - a, 1 / Math.Max(.001f, length));
            float travelled = SKPoint.Distance(a, head);
            for (int i = 1; i <= 9 && 6 + 7 * i + 6 < travelled; i++)
                TacticalShipSprites.DrawEffect(c, "trail", (i - 1) * TacticalShipSprites.EffectFrames("trail") / 9,
                    head - Scale(dir, 6 + 7 * i), (byte)(255 - 18 * i));
            bool own = f.Battle.Units.FirstOrDefault(u => u.Id == flight.Shot.ShooterId)?.Side == f.Battle.PlayerSide;
            float course = MathF.Atan2(b.Y - a.Y, b.X - a.X) * 180 / MathF.PI;
            float scale = f.View.UnitScale;
            if (!TacticalShipSprites.DrawTorpedo(c, head, course, own, scale)) Circle(c, head, 4 * scale, SKColor.Parse("#FFF2A0"));
            float radius = 3.5f * scale;
            if (Recording is { } layout) layout.Rects["projectile"] = SKRect.Create(head.X - radius, head.Y - radius, 2 * radius, 2 * radius);
        }
    }

    private static void DrawTabletopExplosions(SKCanvas c, TacticalBattleFrame f, TacticalSalvoFrame salvo)
    {
        if (salvo.PlaybackSkipped) return;
        foreach (var flight in salvo.Flights.Where(flight => flight.Arrived))
        {
            string name = ExplosionName(flight);
            var strip = TacticalBattleSkinRenderScope.EffectStrip(name);
            if (strip is null || flight.ImpactAgeMs >= strip.Value.Frames * strip.Value.FrameMs) continue;
            int index = (int)Math.Floor(flight.ImpactAgeMs / strip.Value.FrameMs);
            var point = flight.Shot.Absorbed ? AnomalyPresentation.AbsorptionPoint(f, flight.Shot) : f.View.Project(flight.Shot.To);
            if (!TacticalShipSprites.DrawEffect(c, name, index, point)) continue;
            if (Recording is { } layout)
            {
                float scale = TacticalShipSprites.CanvasScale(c);
                layout.Rects[$"explosion:{flight.Index}:{name}:{index}"] = TacticalShipSprites.Bounds(point, strip.Value.Cell, scale, TacticalShipSprites.Factor(scale));
            }
        }
    }

    private static void DrawSalvoTemplates(SKCanvas c, TacticalBattleFrame f, TacticalSalvoFrame salvo, List<SKRect> occupied)
    {
        var badges = new List<(SKRect Bounds, string ShooterId, string Detail, int Index)>();
        foreach (var flight in salvo.Flights)
        {
            if (!salvo.PlaybackSkipped && !flight.Arrived) continue;
            var shot = flight.Shot;
            var a = f.View.Project(shot.From); var b = f.View.Project(shot.To);
            bool hit = shot.Hit && !shot.Absorbed;
            var shooter = f.Battle.Units.FirstOrDefault(u => u.Id == shot.ShooterId);
            bool own = shooter?.Side == f.Battle.PlayerSide;
            var face = hit ? own ? TTSelected : TTRust : TTMiss;
            var edge = hit ? own ? TTEdge : TTRustEdge : TTMissEdge;
            var line = Ribbon(c, a, b, hit ? 5 : 3.5f, face.WithAlpha(95), edge.WithAlpha(80), 16, 24, occupied);
            if (Recording is { } layout) layout.Rects["result-ribbon:" + flight.Index] =
                new(Math.Min(line.A.X, line.B.X), Math.Min(line.A.Y, line.B.Y), Math.Max(line.A.X, line.B.X), Math.Max(line.A.Y, line.B.Y));
            string result = shot.Absorbed ? M("Map.ShotAbsorbed") : U(hit ? "HitChip" : "MissChip");
            string detail = shot.Absorbed ? M("Map.ShotAbsorbed") : hit ? M(shot.Killed ? "Map.ShotKilled" : "Map.ShotDamage", AppliedDamage(f, shot))
                : M(shot.Decoyed ? "Map.ShotDecoyMiss" : "Map.ShotMiss");
            if (hit && shot.Rear) detail = M("Map.RearHit", detail);
            var target = f.Battle.Units.FirstOrDefault(u => u.Id == shot.TargetId);
            detail = $"{(shooter is null ? shot.ShooterId : PilotCardModel.Name(shooter))} → " +
                $"{(target is null ? shot.TargetId : PilotCardModel.Name(target))} · {detail} · {Pct(shot.Chance)}";
            var anchor = shooter is null ? a : f.View.Project(shooter.Pose);
            var safe = Safe(f.View);
            if (!safe.Contains(anchor)) continue;
            const float size = 9.5f, height = size + 8;
            float half = (Measure(result, size, true) + 14) / 2;
            var point = anchor;
            foreach (var offset in new SKPoint[] { new(0, -28), new(0, 35), new(half + 24, 0), new(-half - 24, 0) })
            {
                point = anchor + offset;
                point.X = Math.Clamp(point.X, safe.Left + half, Math.Max(safe.Left + half, safe.Right - half));
                point.Y = Math.Clamp(point.Y, safe.Top + height, safe.Bottom - height);
                var candidate = SKRect.Create(point.X - half, point.Y - height / 2, 2 * half, height);
                if (!badges.Any(badge => badge.Bounds.IntersectsWith(candidate)) && !occupied.Any(box => box.IntersectsWith(candidate))) break;
            }
            var bounds = Pill(c, point, result, size,
                shot.Absorbed ? Heat : hit ? TTGold : Text, hit ? TTBadge : TTMissEdge, shot.Absorbed ? Heat : hit ? TTGold : Text);
            badges.Add((bounds, shot.ShooterId, detail, flight.Index));
            occupied.Add(bounds);
            if (Recording is { } recorded) recorded.Rects["result-badge:" + flight.Index] = bounds;
        }
        foreach (var group in salvo.Flights.Where(flight => salvo.PlaybackSkipped || flight.Arrived).GroupBy(flight => flight.Shot.TargetId))
        {
            if (!salvo.PlaybackSkipped && group.Any(flight => flight.ImpactAgeMs < ExplosionDuration(flight))) continue;
            if (f.Battle.Units.Any(u => u.Id == group.Key && u.State == DeepSpaceSaga.Contracts.TacticalUnitState.Destroyed)) continue;
            var point = f.View.Project(group.Last().Shot.To);
            if (group.Any(flight => flight.Shot.Hit && !flight.Shot.Absorbed))
            {
                var center = point + new SKPoint(-14, -12);
                Polygon(c, Enumerable.Range(0, 16).Select(i =>
                {
                    float angle = i * MathF.PI / 8 + .2f, radius = i % 2 == 0 ? 13 : 13 * .45f;
                    return center + new SKPoint(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
                }), SKColor.Parse("#FFA83C").WithAlpha(225), TTRustEdge, 1.6f);
                Circle(c, center, .3f * 13, SKColor.Parse("#FFF2A0"));
            }
            else Circle(c, point, 21, SKColors.Transparent, SKColor.Parse("#C8D2E4"), 1.8f);
            var bounds = SKRect.Create(point.X - 24, point.Y - 24, 48, 48); occupied.Add(bounds);
            if (Recording is { } layout) layout.Rects["result-marker:" + group.Key] = bounds;
        }
        var hovered = badges.Find(badge => f.HoverPoint is { } point && badge.Bounds.Contains(point));
        if (hovered.Bounds.IsEmpty && f.HoverUnitId is { } id)
            hovered = badges.Find(badge => badge.ShooterId == id);
        if (hovered.Bounds.IsEmpty) return;
        var field = Safe(f.View);
        float textSize = FitSize(hovered.Detail, 10.5f, Math.Max(1, field.Width - 18), true);
        float margin = (Measure(hovered.Detail, textSize, true) + 14) / 2;
        float x = Math.Clamp(hovered.Bounds.MidX, field.Left + margin, Math.Max(field.Left + margin, field.Right - margin));
        float y = hovered.Bounds.Top - 12;
        if (y < field.Top + 10) y = hovered.Bounds.Bottom + 12;
        var tooltip = Pill(c, new(x, y), hovered.Detail, textSize, Text, TTMissEdge, Text);
        occupied.Add(tooltip);
        if (Recording is { } resultLayout) resultLayout.Rects["result-detail:" + hovered.Index] = tooltip;
    }

    private sealed record RibbonPill(string Id, (SKPoint A, SKPoint B) Line, string Text, float Size,
        SKColor TextColor, SKColor Fill, SKColor Edge, SKPoint? Anchor = null);

    private static SKRect Pill(SKCanvas canvas, SKPoint center, string text, float size, SKColor color, SKColor fill, SKColor edge)
    {
        float width = Measure(text, size, true) + 14, height = size + 8;
        var rect = SKRect.Create(center.X - width / 2, center.Y - height / 2, width, height);
        using var paint = new SKPaint { IsAntialias = true, Color = fill };
        canvas.DrawRoundRect(rect, height / 2, height / 2, paint);
        paint.Color = edge; paint.Style = SKPaintStyle.Stroke; paint.StrokeWidth = 1.4f;
        canvas.DrawRoundRect(rect, height / 2, height / 2, paint);
        T(canvas, text, rect.MidX, rect.Bottom - 5, size, color, true, SKTextAlign.Center, "field.ribbon.text");
        return rect;
    }

    private static SKRect PillOnRibbon(SKCanvas canvas, RibbonPill pill, List<SKRect> placed, List<SKRect> occupied)
    {
        var (a, b) = pill.Line;
        float width = Measure(pill.Text, pill.Size, true) + 14, height = pill.Size + 8;
        var point = pill.Anchor ?? a + Scale(b - a, .5f);
        if (pill.Anchor is null)
        {
            var dir = Scale(b - a, 1 / Math.Max(1, SKPoint.Distance(a, b)));
            var normal = new SKPoint(-dir.Y, dir.X);
            var candidates = new List<SKPoint>();
            foreach (float t in new[] { .5f, .35f, .65f, .22f, .78f }) candidates.Add(a + Scale(b - a, t));
            foreach (float t in new[] { .5f, .35f, .65f })
                foreach (int sign in new[] { 1, -1 }) candidates.Add(a + Scale(b - a, t) + Scale(normal, sign * (height + 4)));
            foreach (var candidate in candidates)
            {
                var rect = SKRect.Create(candidate.X - width / 2, candidate.Y - height / 2, width, height); rect.Inflate(3, 2);
                if (placed.Any(r => r.IntersectsWith(rect))) continue;
                point = candidate; break;
            }
        }
        var bounds = Pill(canvas, point, pill.Text, pill.Size, pill.TextColor, pill.Fill, pill.Edge);
        placed.Add(bounds); occupied.Add(bounds);
        if (Recording is { } layout) layout.Rects[pill.Id] = bounds;
        return bounds;
    }

    private static (SKPoint A, SKPoint B) Ribbon(SKCanvas canvas, SKPoint a, SKPoint b, float width,
        SKColor face, SKColor edge, float trimA, float trimB, List<SKRect> occupied)
    {
        float length = SKPoint.Distance(a, b);
        if (length < trimA + trimB + width) return (a, b);
        var dir = Scale(b - a, 1 / length);
        var start = a + Scale(dir, trimA); var tip = b - Scale(dir, trimB);
        var end = tip - Scale(dir, .9f * width);
        using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Butt, Color = edge, StrokeWidth = width + 1 };
        canvas.DrawLine(start, end, paint);
        paint.Color = face; paint.StrokeWidth = width; canvas.DrawLine(start, end, paint);
        using var dash = SKPathEffect.CreateDash([5, 4], 0);
        paint.Color = edge; paint.StrokeWidth = 1.1f; paint.PathEffect = dash; canvas.DrawLine(start, end, paint);
        var side = new SKPoint(-dir.Y * .8f * width, dir.X * .8f * width);
        Polygon(canvas, [tip, end + side, end - side], face, edge, .8f);
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
        var pills = new List<RibbonPill>();
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
            var a = f.View.Project(shooter.Pose); var b = MarkerPoint(f.View, target);
            var line = Ribbon(c, a, b, 4, TTReticle.WithAlpha(125), TTRustEdge.WithAlpha(95), 16, 18, occupied);
            pills.Add(new("badge:threat:" + pills.Count, line, M("Map.ThreatBadge", Pct(incoming.Chance)), 10,
                SKColor.Parse("#FFD6C4"), TTRustEdge, SKColor.Parse("#FFB090"), SKPoint.Distance(a, b) < 60 ? b + new SKPoint(0, 40) : null));
        }
        foreach (var row in plan.Rows.OrderBy(r => r.Drone.Id == plan.SelectedDroneId ? 1 : 0))
        {
            var target = f.Battle.Units.FirstOrDefault(u => u.Id == row.TargetId);
            if (target is null || row.Drone.InDust || row.Evade) continue;
            var a = f.View.Project(row.Drone.Pose); var b = MarkerPoint(f.View, target);
            bool selected = row.Drone.Id == plan.SelectedDroneId;
            var line = Ribbon(c, a, b, selected ? 5.5f : 4, TTReticle.WithAlpha(selected ? (byte)165 : (byte)110),
                TTRustEdge.WithAlpha(95), 16, 24, occupied);
            if (row.Target is { } shot)
                pills.Add(new(selected ? "chance" : "badge:own:" + row.Drone.Id, line,
                    selected ? Pct(shot.Chance) : PilotCardModel.Name(row.Drone) + " " + Pct(shot.Chance), selected ? 13 : 10.5f,
                    selected ? TTGold : SKColor.Parse("#F6EFD8"), selected ? TTBadge : TTEdge, TTGold));
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
        var placed = new List<SKRect>();
        foreach (var pill in pills) PillOnRibbon(c, pill, placed, occupied);
        if (plan.Selected is { } selectedRow)
            foreach (var candidate in selectedRow.Group.List)
            {
                var target = f.Battle.Units.FirstOrDefault(u => u.Id == candidate.TargetId);
                if (target is null) continue;
                var point = MarkerPoint(f.View, target);
                Reticle(c, point, candidate.TargetId == selectedRow.TargetId);
                var bounds = SKRect.Create(point.X - 22, point.Y - 22, 44, 44); occupied.Add(bounds);
                if (Recording is { } layout) layout.Rects["candidate:" + target.Id] = bounds;
                if (candidate.TargetId != selectedRow.TargetId)
                {
                    var badge = Pill(c, point + new SKPoint(34, -20), Pct(candidate.Chance), 10, SKColor.Parse("#FFD6C4"), TTEdge, TTAlt);
                    occupied.Add(badge);
                    if (Recording is { } badgeLayout) badgeLayout.Rects["badge:alternative:" + target.Id] = badge;
                }
            }
        foreach (var row in plan.Rows.Where(r => r.Drone.InDust))
        {
            var p = f.View.Project(row.Drone.Pose);
            var box = Callout(c, p.X, p.Y + 40, M("InDustNoFire"), Dust, Dust); occupied.Add(box);
            if (Recording is { } layout) layout.Rects["dust:" + row.Drone.Id] = box;
        }
    }
}
