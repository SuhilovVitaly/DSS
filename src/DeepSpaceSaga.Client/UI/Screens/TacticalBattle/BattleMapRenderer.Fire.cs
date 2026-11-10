using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static partial class TacticalBattleMapRenderer
{
    internal static IEnumerable<TacticalShotSnapshot> DecoyMisses(TacticalBattleFrame frame) =>
        frame.Salvo is { InFlight: false } salvo ? salvo.Flights.Where(f => f.Shot.Decoyed && !f.Shot.Hit && !f.Shot.Absorbed).Select(f => f.Shot) : [];
    static partial void DrawPhaseOverlays(SKCanvas c, TacticalBattleFrame f, List<SKRect> occupied, InteractiveAnomalyField? interactiveField)
    {
        if (f.Movement is not null) return;
        foreach (var trail in f.Battle.Paths)
            for (int i = 1; i < trail.Points.Length; i++) Stroke(c, f.View.Project(trail.Points[i - 1]), f.View.Project(trail.Points[i]), Text.WithAlpha(150), 1.2f, [4, 4]);
        if (f.FirePlan is { } plan)
        {
            if (plan.Selected?.Drone.Build is { } build)
            {
                using var path = CreateFireSector(f.View, plan.Selected.Drone.Pose, build.Range, build.ArcDegrees);
                DrawFireSector(c, path, ArcFill, ArcEdge);
                c.Save();
                try { c.ClipPath(path, SKClipOperation.Intersect, true); DrawAnomalies(c, f, interactiveField); }
                finally { c.Restore(); }
            }
            foreach (var row in plan.Rows.Where(r => r.Drone.InDust))
            {
                var p = f.View.Project(row.Drone.Pose);
                var box = Callout(c, p.X, p.Y + 34, M("InDustNoFire"), Dust, Dust); occupied.Add(box);
                if (Recording is { } l) l.Rects["dust:" + row.Drone.Id] = box;
            }
            foreach (var row in plan.Rows.OrderBy(r => r.Drone.Id == plan.SelectedDroneId ? 1 : 0))
            {
                var target = f.Battle.Units.FirstOrDefault(u => u.Id == row.TargetId); if (target is null || row.Drone.InDust) continue;
                var a = f.View.Project(row.Drone.Pose); var b = f.View.Project(target.Pose); bool selected = row.Drone.Id == plan.SelectedDroneId;
                Arrow(c, a, b, selected ? Yellow : Text.WithAlpha(170), selected ? 2.6f : 1.3f, selected ? 12 : 9);
                if (selected && row.Target is { } candidate)
                {
                    string text = Pct(candidate.Chance); var p = new SKPoint((a.X + b.X) * .5f, (a.Y + b.Y) * .5f); float w = Measure(text, 16, true);
                    var rect = new SKRect(p.X - w * .5f, p.Y - 25, p.X + w * .5f, p.Y - 7); occupied.Add(rect); if (Recording is { } l) l.Rects["chance"] = rect;
                    T(c, text, p.X, p.Y - 9, 16, Yellow, true, SKTextAlign.Center);
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
        if (f.Salvo is { InFlight: false } results)
        {
            var markers = f.Battle.Units.Where(u => u.State != TacticalUnitState.Destroyed).Select(u => f.View.Project(u.Pose)).ToArray();
            foreach (var damage in results.Damage.Where(d => d.Armor > 0 && d.Unit.Side == f.Battle.PlayerSide))
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
        if (f.Salvo is { InFlight: false } results)
            foreach (var d in results.Damage.Where(d => d.Armor > 0 && d.Unit.Side == f.Battle.PlayerSide)) Diamond(c, f.View.Project(d.Unit.Pose), 10, SKColors.Transparent, Yellow, 1.5f);
        if (f.Salvo is { } salvo)
            foreach (var flight in salvo.Flights.Where(s => s.Flash))
                if (flight.Shot.Absorbed)
                {
                    var q = AnomalyPresentation.AbsorptionPoint(f, flight.Shot);
                    Circle(c, q, 6, SKColors.Transparent, Heat, 1.6f); Circle(c, q, 10, SKColors.Transparent, Heat, 1.6f);
                }
                else Circle(c, f.View.Project(flight.Shot.To), 15, SKColors.Transparent, flight.Shot.Hit ? Yellow : Red, 2);
        // The admitted projection changes State only at impact, so wrecks cannot reveal a future kill.
        foreach (var u in f.Battle.Units.Where(u => u.State == TacticalUnitState.Destroyed))
        { var p = f.View.Project(u.Pose); Stroke(c, new(p.X - 6, p.Y - 6), new(p.X + 6, p.Y + 6), Muted, 1.5f); Stroke(c, new(p.X - 6, p.Y + 6), new(p.X + 6, p.Y - 6), Muted, 1.5f); }
    }
}
