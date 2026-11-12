using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;
using Skin = DeepSpaceSaga.Client.UI.Screens.TacticalBattle.TacticalBattleSkinRenderScope;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static partial class TacticalBattleMapRenderer
{
    internal static IEnumerable<TacticalShotSnapshot> DecoyMisses(TacticalBattleFrame frame) =>
        frame.Salvo is { InFlight: false } salvo ? salvo.Flights.Where(f => f.Shot.Decoyed && !f.Shot.Hit && !f.Shot.Absorbed).Select(f => f.Shot) : [];
    static partial void DrawPhaseOverlays(SKCanvas c, TacticalBattleFrame f, List<SKRect> occupied, InteractiveAnomalyField? interactiveField)
    {
        if (f.Movement is not null) return;
        if (f.FirePlan is { } plan)
        {
            if (plan.Selected?.Drone.Build is { } build)
            {
                using var path = CreateFireSector(f.View, plan.Selected.Drone.Pose, build.Range, 2 * build.ArcDegrees);
                DrawFireSector(c, path, Skin.Color("ui.map.fire.sector", "fill", ArcFill),
                    Skin.Color("ui.map.fire.sector", "stroke", ArcEdge));
                if (!Skin.Tabletop)
                {
                    c.Save();
                    try { c.ClipPath(path, SKClipOperation.Intersect, true); DrawAnomalies(c, f, interactiveField); }
                    finally { c.Restore(); }
                }
            }
            foreach (var row in plan.Rows.Where(r => r.Drone.InDust))
            {
                var p = f.View.Project(row.Drone.Pose);
                var box = Callout(c, p.X, p.Y + 34, M("InDustNoFire"), Dust, Dust); occupied.Add(box);
                if (Recording is { } l) l.Rects["dust:" + row.Drone.Id] = box;
            }
            if (plan.Selected is { } shooter)
                DrawAlternativeTargets(c, f, shooter, occupied);
            foreach (var row in plan.Rows.OrderBy(r => r.Drone.Id == plan.SelectedDroneId ? 1 : 0))
            {
                var target = f.Battle.Units.FirstOrDefault(u => u.Id == row.TargetId); if (target is null || row.Drone.InDust) continue;
                var a = f.View.Project(row.Drone.Pose); var b = f.View.Project(target.Pose); bool selected = row.Drone.Id == plan.SelectedDroneId;
                var lineColor = selected ? Skin.Color("ui.map.fire.assigned", "stroke", Yellow) : Text.WithAlpha(170);
                Arrow(c, a, b, lineColor, selected ? 2.6f : 1.3f, selected ? 12 : 9);
                if (selected && row.Target is { } candidate)
                {
                    var targetPoint = MarkerPoint(f.View, target);
                    Skin.Image(c, "ui.map.fire.assigned.art", SKRect.Create(targetPoint.X - 11, targetPoint.Y - 11, 22, 22));
                    string text = Pct(candidate.Chance); var p = new SKPoint((a.X + b.X) * .5f, (a.Y + b.Y) * .5f); float w = Measure(text, 16, true);
                    var rect = new SKRect(p.X - w * .5f, p.Y - 25, p.X + w * .5f, p.Y - 7); occupied.Add(rect); if (Recording is { } l) l.Rects["chance"] = rect;
                    T(c, text, p.X, p.Y - 9, 16, Yellow, true, SKTextAlign.Center, "ui.map.fire.assigned");
                    if (candidate.Absorption > 0)
                    {
                        var segments = AnomalyPresentation.Segments(f.Battle, row.Drone.Pose, target.Pose);
                        using var effect = SKPathEffect.CreateDash([6, 3], 0);
                        using var heat = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, Color = Heat, StrokeWidth = 3.2f, PathEffect = effect };
                        foreach (var segment in segments) c.DrawLine(a + Scale(b - a, (float)segment.T0), a + Scale(b - a, (float)segment.T1), heat);
                        if (!segments.IsEmpty)
                        {
                            var longest = segments.OrderByDescending(s => s.T1 - s.T0).First();
                            var mid = a + Scale(b - a, (float)((longest.T0 + longest.T1) / 2.0));
                            var box = Callout(c, mid.X, mid.Y + 26, M("Absorption", Math.Round(candidate.Absorption * 100)), Heat, Heat);
                            occupied.Add(box); if (Recording is { } record) record.Rects["absorption:" + row.Drone.Id] = box;
                        }
                    }
                }
            }
        }
        if (f.Salvo is { InFlight: true } salvo)
            foreach (var flight in salvo.Flights.Where(s => s.Flying))
            {
                var a = f.View.Project(flight.Shot.From); var b = f.View.Project(flight.Shot.To);
                Stroke(c, a, b, Yellow.WithAlpha(70), 1.1f, [5, 5]); var head = a + Scale(b - a, (float)flight.Progress);
                using var shader = SKShader.CreateLinearGradient(a, head, new SKColor[] { Yellow.WithAlpha(0), Yellow }, null, SKShaderTileMode.Clamp);
                using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3, StrokeCap = SKStrokeCap.Round, Shader = shader }; c.DrawLine(a, head, paint);
                Circle(c, head, 8, Yellow.WithAlpha(70)); Circle(c, head, TacticalBattleSkinRenderScope.Number("shot.projectile", "size", 3.5f), TacticalBattleSkinRenderScope.Color("shot.projectile", "fill", Shell));
                if (Recording is { } l) l.Rects["projectile"] = SKRect.Create(head.X - 3.5f, head.Y - 3.5f, 7, 7);
            }
        if (f.Salvo is { InFlight: false, ShowResultCallouts: true } results)
        {
            var markers = f.Battle.Units.Where(u => u.State != TacticalUnitState.Destroyed).Select(u => f.View.Project(u.Pose)).ToArray();
            foreach (var damage in results.Damage.Where(d => !Skin.Tabletop && d.Armor > 0 && d.Unit.Side == f.Battle.PlayerSide))
            {
                var p = f.View.Project(damage.Unit.Pose); string text = U("ArmorLoss", damage.Armor); float half = (Measure(text, 12) + 14) * .5f;
                SKPoint q = default;
                foreach (var delta in new SKPoint[] { new(0, 32), new(half + 16, 10), new(0, -20), new(-half - 16, 10) })
                { q = p + delta; var bounds = new SKRect(q.X - half - 10, q.Y - 27, q.X + half + 10, q.Y + 12); if (!markers.Any(bounds.Contains)) break; }
                var rect = Callout(c, q.X, q.Y, text, Yellow, Yellow); occupied.Add(rect); if (Recording is { } l) l.Rects["callout:" + damage.Unit.Id] = rect;
            }
            foreach (var shot in results.Flights.Select(flight => flight.Shot).Where(s => s.Absorbed))
            {
                var q = AnomalyPresentation.AbsorptionPoint(f, shot);
                Circle(c, q, 6, SKColors.Transparent, Heat, 1.6f); Circle(c, q, 10, SKColors.Transparent, Heat, 1.6f);
                var shooter = f.Battle.Units.FirstOrDefault(u => u.Id == shot.ShooterId);
                string text = M("AbsorbedCallout", shooter is null ? shot.ShooterId : PilotCardModel.Name(shooter));
                float half = (Measure(text, 12) + 14) * .5f;
                var absorptionMarkers = f.Battle.Units.Where(u => u.State != TacticalUnitState.Destroyed).Select(u => MarkerPoint(f.View, u))
                    .Concat(f.Battle.Units.Where(u => HasDecoy(f, u)).Select(u => DecoyPoint(f.View.Project(u.Pose), u.Pose.Course))).ToArray();
                var callout = q + new SKPoint(0, -16);
                foreach (var delta in new SKPoint[] { new(0, -16), new(0, -38), new(0, 38), new(half + 20, -16), new(-half - 20, -16), new(0, -60), new(0, 60) })
                {
                    var p = q + delta; var rect = new SKRect(p.X - half, p.Y - 17, p.X + half, p.Y + 2); var expanded = rect; expanded.Inflate(10, 10);
                    if (occupied.Any(r => r.IntersectsWith(rect)) || absorptionMarkers.Any(expanded.Contains)) continue;
                    callout = p; break;
                }
                var box = Callout(c, callout.X, callout.Y, text, Heat, Heat);
                occupied.Add(box);
                if (Recording is { } layout) { layout.Rects["absorbed:" + shot.ShooterId] = box; layout.Rects["absorption-flash:" + shot.ShooterId] = new(q.X - 10, q.Y - 10, q.X + 10, q.Y + 10); }
            }
            var allMarkers = f.Battle.Units.Select(u => MarkerPoint(f.View, u)).ToArray();
            var decoys = f.Battle.Units.Where(u => HasDecoy(f, u)).Select(u => DecoyPoint(f.View.Project(u.Pose), u.Pose.Course)).ToArray();
            int index = 0;
            foreach (var shot in DecoyMisses(f))
            {
                var target = f.Battle.Units.First(u => u.Id == shot.TargetId);
                var shooter = f.Battle.Units.FirstOrDefault(u => u.Id == shot.ShooterId);
                var p = f.View.Project(target.Pose);
                string text = M("DecoyMiss", shooter is null ? shot.ShooterId : PilotCardModel.Name(shooter));
                float half = (Measure(text, 12) + 14) * .5f;
                SKPoint q = p + new SKPoint(0, -38);
                foreach (var delta in new SKPoint[] { new(0, -38), new(0, 44), new(half + 20, 10), new(-half - 20, 10),
                    new(.6f * half, -38), new(.6f * half, -60), new(0, -60), new(0, 64) })
                {
                    var candidate = p + delta;
                    var rect = new SKRect(candidate.X - half, candidate.Y - 17, candidate.X + half, candidate.Y + 2);
                    var expanded = rect; expanded.Inflate(10, 10);
                    if (occupied.Any(r => r.IntersectsWith(rect)) || allMarkers.Any(expanded.Contains) || decoys.Any(expanded.Contains)) continue;
                    q = candidate; break;
                }
                var bounds = Callout(c, q.X, q.Y, text, Green, Green);
                occupied.Add(bounds);
                if (Recording is { } layout) layout.Rects["decoy-callout:" + index++] = bounds;
            }
        }
        if (f.Salvo is { PlaybackSkipped: false } feedback)
            DrawFloatingShotResults(c, f, feedback, occupied);
    }

    private static void DrawFloatingShotResults(SKCanvas canvas, TacticalBattleFrame frame, TacticalSalvoFrame salvo, List<SKRect> occupied)
    {
        const double durationMs = 1400;
        foreach (var flight in salvo.Flights)
        {
            double age = flight.ImpactAgeMs;
            if (age < 0 || age >= durationMs) continue;
            var shot = flight.Shot;
            var damage = frame.Battle.FireSteps.FirstOrDefault(step => step.ShooterId == shot.ShooterId)?.Events
                .FirstOrDefault(entry => entry.Kind == TacticalBattleLogKind.DamageApplied)?.Damage;
            string text = shot.Absorbed ? M("Absorbed") : shot.Hit ? $"−{damage?.Hull + damage?.Armor ?? shot.Damage}" : U("MissChip");
            float progress = (float)(age / durationMs);
            var color = shot.Absorbed ? Heat : shot.Hit ? Yellow : Text;
            color = color.WithAlpha((byte)(255 * Math.Min(1, (1 - progress) * 2)));
            var point = frame.View.Project(shot.To) + new SKPoint((flight.Index % 3 - 1) * 14, -19 - 44 * progress);
            float width = Measure(text, 14, true);
            var bounds = new SKRect(point.X - width / 2, point.Y - 14, point.X + width / 2, point.Y + 2);
            occupied.Add(bounds);
            if (Recording is { } layout) layout.Rects["floating-shot:" + flight.Index] = bounds;
            T(canvas, text, point.X, point.Y, 14, color, true, SKTextAlign.Center);
        }
    }
    private static void DrawAlternativeTargets(SKCanvas canvas, TacticalBattleFrame frame, TacticalFirePlanRow shooter, List<SKRect> occupied)
    {
        var origin = frame.View.Project(shooter.Drone.Pose);
        var classicColor = Yellow.WithAlpha(155);
        var color = Skin.Color("ui.map.fire.candidate", "stroke", classicColor);
        foreach (var candidate in shooter.Group.List)
        {
            if (candidate.TargetId == shooter.TargetId) continue;
            var target = frame.Battle.Units.FirstOrDefault(unit => unit.Id == candidate.TargetId);
            if (target is null) continue;
            var point = MarkerPoint(frame.View, target);
            const float radius = 12;
            float length = SKPoint.Distance(origin, point);
            if (length > 2 * radius)
            {
                var offset = Scale(point - origin, radius / length);
                Stroke(canvas, origin + offset, point - offset, color, 1, [4, 4]);
            }
            Circle(canvas, point, radius, SKColors.Transparent, color, 1.2f);
            var ring = new SKRect(point.X - radius, point.Y - radius, point.X + radius, point.Y + radius);
            Skin.Image(canvas, "ui.map.fire.candidate.art", ring);
            occupied.Add(ring);
            if (Recording is { } layout) layout.Rects["candidate:" + target.Id] = ring;

            string chance = Pct(candidate.Chance);
            float halfWidth = (Measure(chance, 12) + 14) * .5f;
            foreach (float y in new[] { point.Y - 15, point.Y + 32 })
            {
                var bounds = new SKRect(point.X - halfWidth, y - 17, point.X + halfWidth, y + 2);
                if (!Safe(frame.View).Contains(bounds) || occupied.Any(rect => rect.IntersectsWith(bounds))) continue;
                occupied.Add(Callout(canvas, point.X, y, chance, classicColor, Text));
                break;
            }
        }
    }

    private static SKPath CreateFireSector(TacticalBattleView view, TacticalPoseSnapshot pose, double range, double arcDegrees)
    {
        var origin = view.Project(pose);
        float radius = (float)(range * view.PixelsPerUnit);
        var path = new SKPath();
        path.MoveTo(origin);
        path.ArcTo(SKRect.Create(origin.X - radius, origin.Y - radius, 2 * radius, 2 * radius),
            (float)(pose.Course - arcDegrees * .5), (float)arcDegrees, false);
        path.Close();
        return path;
    }

    private static void DrawFireSector(SKCanvas canvas, SKPath path, SKColor fill, SKColor edge)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = fill };
        canvas.DrawPath(path, paint);
        paint.Color = edge;
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 1.1f;
        canvas.DrawPath(path, paint);
    }

    static partial void DrawResultHighlights(SKCanvas c, TacticalBattleFrame f)
    {
        if (!Skin.Tabletop && f.Salvo is { InFlight: false, ShowResultCallouts: true } results)
            foreach (var d in results.Damage.Where(d => d.Armor > 0 && d.Unit.Side == f.Battle.PlayerSide)) Diamond(c, f.View.Project(d.Unit.Pose), 10, SKColors.Transparent, Yellow, 1.5f);
        if (f.Salvo is { } salvo)
            foreach (var flight in salvo.Flights.Where(s => s.Flash))
                if (flight.Shot.Absorbed)
                {
                    var q = AnomalyPresentation.AbsorptionPoint(f, flight.Shot);
                    Circle(c, q, 6, SKColors.Transparent, Heat, 1.6f); Circle(c, q, 10, SKColors.Transparent, Heat, 1.6f);
                }
                else Circle(c, f.View.Project(flight.Shot.To), 15, SKColors.Transparent, flight.Shot.Hit ? Yellow : Red, 2);
    }
}
