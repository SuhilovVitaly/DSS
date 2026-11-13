using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static partial class TacticalBattleMapRenderer
{
    private static readonly SKColor TTRust = SKColor.Parse("#AD5C44"), TTRustEdge = SKColor.Parse("#5A2416"),
        TTReticle = SKColor.Parse("#FF4628"), TTAlt = SKColor.Parse("#FF9A6A");
    private static readonly SKColor TTMiss = SKColor.Parse("#969CA8"), TTMissEdge = SKColor.Parse("#3C404A");

    private static string ExplosionName(TacticalShotFlight flight) => flight.Shot.Killed ? "boom-kill" :
        (flight.Shot.Hit || flight.InterceptedByDecoy) && !flight.Shot.Absorbed ? "boom-hit" : "puff";

    private const float DecoyLaunchProgress = .45f;

    private static SKPoint DecoyOffset(SKPoint from, SKPoint target, int index, float scale)
    {
        var direction = target - from;
        float length = SKPoint.Distance(from, target);
        direction = length > .001f ? Scale(direction, 1 / length) : new SKPoint(1, 0);
        var lateral = new SKPoint(-direction.Y, direction.X);
        // Keep the flare clear of the pilot label beneath the drone.
        float side = Math.Abs(lateral.Y) > .3f ? -Math.Sign(lateral.Y) : index % 2 == 0 ? 1 : -1;
        return Scale(direction, 14 * scale) + Scale(lateral, side * Math.Max(38, 58 * scale));
    }

    private static float DecoyTravel(float progress) =>
        Math.Clamp((progress - DecoyLaunchProgress) / (1 - DecoyLaunchProgress), 0, 1);

    private static SKPoint TorpedoPoint(SKPoint from, SKPoint target, SKPoint offset, float progress)
    {
        float turn = DecoyTravel(progress);
        return from + Scale(target - from, progress) + Scale(offset, turn * turn);
    }

    private static void DrawTabletopProjectiles(SKCanvas c, TacticalBattleFrame f, TacticalSalvoFrame salvo)
    {
        foreach (var flight in salvo.Flights.Where(flight => flight.Flying))
        {
            var a = f.View.Project(flight.Shot.From); var b = f.View.Project(flight.Shot.To);
            Stroke(c, a, b, Yellow.WithAlpha(70), 1.1f, [5, 5]);
            float progress = (float)flight.Progress, scale = f.View.UnitScale;
            var offset = flight.DecoyTriggered ? DecoyOffset(a, b, flight.Index, scale) : SKPoint.Empty;
            var redirect = flight.InterceptedByDecoy ? offset : SKPoint.Empty;
            if (flight.DecoyTriggered && progress >= DecoyLaunchProgress)
            {
                float travel = DecoyTravel(progress);
                var decoy = b + Scale(offset, 1 - (1 - travel) * (1 - travel));
                Stroke(c, b, decoy, Green.WithAlpha(145), 1, [3, 3]);
                float flareRadius = Math.Max(4, 6 * scale);
                Circle(c, decoy, flareRadius + 3, Green.WithAlpha(65));
                Diamond(c, decoy, flareRadius, MapBg, Green, 1.5f);
                Circle(c, decoy, flareRadius / 3, Green);
                if (Recording is { } decoyLayout)
                    decoyLayout.Rects["decoy-flight:" + flight.Index] =
                        SKRect.Create(decoy.X - flareRadius, decoy.Y - flareRadius, 2 * flareRadius, 2 * flareRadius);
            }
            var head = TorpedoPoint(a, b, redirect, progress);
            var tangent = head - TorpedoPoint(a, b, redirect, Math.Max(0, progress - .001f));
            float length = Math.Max(.001f, SKPoint.Distance(a, b));
            for (int i = 1; i <= 9 && 6 + 7 * i + 6 < length * progress; i++)
                TacticalShipSprites.DrawEffect(c, "trail", (i - 1) * TacticalShipSprites.EffectFrames("trail") / 9,
                    TorpedoPoint(a, b, redirect, progress - (6 + 7 * i) / length), (byte)(255 - 18 * i));
            bool own = f.Battle.Units.FirstOrDefault(u => u.Id == flight.Shot.ShooterId)?.Side == f.Battle.PlayerSide;
            if (progress == 0) tangent = b - a;
            float course = MathF.Atan2(tangent.Y, tangent.X) * 180 / MathF.PI;
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
            var point = TabletopShotEndpoint(f, flight);
            if (flight.InterceptedByDecoy && flight.ImpactAgeMs < 220)
            {
                float fade = (float)(flight.ImpactAgeMs / 220);
                Circle(c, point, 6 + 12 * fade, SKColors.Transparent, Yellow.WithAlpha((byte)(210 * (1 - fade))), 1.5f);
            }
            if (!TacticalShipSprites.DrawEffect(c, name, index, point)) continue;
            if (Recording is { } layout)
            {
                float scale = TacticalShipSprites.CanvasScale(c);
                layout.Rects[$"explosion:{flight.Index}:{name}:{index}"] = TacticalShipSprites.Bounds(point, strip.Value.Cell, scale, TacticalShipSprites.Factor(scale));
            }
        }
    }

    private static SKPoint TabletopShotEndpoint(TacticalBattleFrame frame, TacticalShotFlight flight)
    {
        if (flight.Shot.Absorbed) return AnomalyPresentation.AbsorptionPoint(frame, flight.Shot);
        var target = frame.View.Project(flight.Shot.To);
        return flight.InterceptedByDecoy
            ? target + DecoyOffset(frame.View.Project(flight.Shot.From), target, flight.Index, frame.View.UnitScale)
            : target;
    }

    private static void DrawHoveredSalvoResult(SKCanvas canvas, TacticalBattleFrame frame, TacticalSalvoFrame salvo, List<SKRect> occupied)
    {
        if (frame.HoverUnitId is not { } id) return;
        var shooter = frame.Battle.Units.FirstOrDefault(unit => unit.Id == id &&
            unit.Kind == TacticalUnitKind.Drone && unit.State != TacticalUnitState.Destroyed);
        if (shooter is null) return;
        var flight = salvo.Flights.FirstOrDefault(flight => flight.Shot.ShooterId == id &&
            (salvo.PlaybackSkipped || flight.Arrived));
        if (flight is null) return;

        var shot = flight.Shot;
        var from = frame.View.Project(shot.From);
        var to = flight.InterceptedByDecoy ? TabletopShotEndpoint(frame, flight) : frame.View.Project(shot.To);
        bool hit = shot.Hit && !shot.Absorbed;
        bool own = shooter.Side == frame.Battle.PlayerSide;
        var face = hit ? own ? TTSelected : TTRust : TTMiss;
        var edge = hit ? own ? TTEdge : TTRustEdge : TTMissEdge;
        var line = Ribbon(canvas, from, to, hit ? 5 : 3.5f, face.WithAlpha(95), edge.WithAlpha(80), 16, 24, occupied);
        if (line == (from, to)) Stroke(canvas, from, to, face.WithAlpha(180), 1.4f);
        if (Recording is { } layout) layout.Rects["result-ribbon:" + flight.Index] =
            new(Math.Min(line.A.X, line.B.X), Math.Min(line.A.Y, line.B.Y), Math.Max(line.A.X, line.B.X), Math.Max(line.A.Y, line.B.Y));

        var target = frame.Battle.Units.FirstOrDefault(unit => unit.Id == shot.TargetId);
        string outcome = shot.Absorbed ? M("Map.ShotAbsorbed") : hit
            ? U("HitChip") + " · " + M(shot.Killed ? "Map.ShotKilled" : "Map.ShotDamage", AppliedDamage(frame, shot))
            : M(flight.InterceptedByDecoy ? "Map.ShotDecoyMiss" : "Map.ShotMiss");
        if (hit && shot.Rear) outcome = M("Map.RearHit", outcome);
        string detail = $"{PilotCardModel.Name(shooter)} → {(target is null ? shot.TargetId : PilotCardModel.Name(target))} · {outcome}";
        string chance = M("Map.ResultChance", Pct(shot.Chance));
        if (!salvo.InFlight)
        {
            var roll = shot.Absorbed ? shot.AbsorptionRoll : shot.HitRoll;
            if (roll is { } value)
                chance += " · " + M(shot.Absorbed ? "Map.AbsorptionRoll" : "Map.HitRoll", BattleFormat.Probability(value, 2));
        }
        var field = Safe(frame.View);
        if (field.Width <= 24) return;
        const float size = 9, lineHeight = 12;
        detail = BuildCardModel.Ellipsize(detail, field.Width - 24, size);
        chance = BuildCardModel.Ellipsize(chance, field.Width - 24, size);
        var lines = new List<string> { detail, chance };
        if (!salvo.InFlight && shot.Factors is { } factors)
            lines.AddRange(PilotCardFireBlock.WrapSlim(BattleFormat.HitFactors(factors), Math.Min(270, field.Width - 24), size));
        float width = lines.Max(lineText => Measure(lineText, size)) + 16;
        float height = lines.Count * lineHeight + 8;
        var anchor = frame.View.Project(shooter.Pose);
        float x = Math.Clamp(anchor.X - width / 2, field.Left, field.Right - width);
        float y = anchor.Y - height - 26;
        if (y < field.Top) y = anchor.Y + 32;
        var bounds = SKRect.Create(x, Math.Clamp(y, field.Top, field.Bottom - height), width, height);
        Round(canvas, bounds, Box, BoxEdge);
        for (int i = 0; i < lines.Count; i++)
            T(canvas, lines[i], x + 8, bounds.Top + 12 + i * lineHeight, size,
                i == 0 && shot.Absorbed ? Heat : i == 0 && hit ? TTGold : Text, role: "field.ribbon.text");
        occupied.Add(bounds);
        if (Recording is { } resultLayout) resultLayout.Rects["result-detail:" + flight.Index] = bounds;
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
