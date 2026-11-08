using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static partial class TacticalBattleMapRenderer
{
    internal static IEnumerable<TacticalShotSnapshot> DecoyMisses(TacticalBattleFrame frame) =>
        frame.Salvo is { InFlight: false } salvo ? salvo.Flights.Where(f => f.Shot.Decoyed && !f.Shot.Hit).Select(f => f.Shot) : [];
    static partial void DrawPhaseOverlays(SKCanvas c, TacticalBattleFrame f, List<SKRect> occupied)
    {
        if (f.Movement is not null) return;
        foreach (var trail in f.Battle.Paths)
            for (int i = 1; i < trail.Points.Length; i++) Stroke(c, f.View.Project(trail.Points[i - 1]), f.View.Project(trail.Points[i]), Text.WithAlpha(150), 1.2f, [4, 4]);
        if (f.FirePlan is { } plan)
        {
            if (plan.Selected?.Drone.Build is { } build)
            {
                var unit = plan.Selected.Drone; var p = f.View.Project(unit.Pose); float r = (float)(build.Range * f.View.PixelsPerUnit);
                using var path = new SKPath(); path.MoveTo(p); path.ArcTo(SKRect.Create(p.X - r, p.Y - r, 2 * r, 2 * r), (float)(unit.Pose.Course - build.ArcDegrees / 2), (float)build.ArcDegrees, false); path.Close();
                using var paint = new SKPaint { IsAntialias = true, Color = ArcFill }; c.DrawPath(path, paint);
                paint.Color = ArcEdge; paint.Style = SKPaintStyle.Stroke; paint.StrokeWidth = 1.1f; c.DrawPath(path, paint);
            }
            foreach (var row in plan.Rows.OrderBy(r => r.Drone.Id == plan.SelectedDroneId ? 1 : 0))
            {
                var target = f.Battle.Units.FirstOrDefault(u => u.Id == row.TargetId); if (target is null) continue;
                var a = f.View.Project(row.Drone.Pose); var b = f.View.Project(target.Pose); bool selected = row.Drone.Id == plan.SelectedDroneId;
                Arrow(c, a, b, selected ? Yellow : Text.WithAlpha(170), selected ? 2.6f : 1.3f, selected ? 12 : 9);
                if (selected && row.Target is { } candidate)
                {
                    string text = Pct(candidate.Chance); var p = new SKPoint((a.X + b.X) / 2, (a.Y + b.Y) / 2); float w = Measure(text, 16, true);
                    var rect = new SKRect(p.X - w / 2, p.Y - 25, p.X + w / 2, p.Y - 7); occupied.Add(rect); if (Recording is { } l) l.Rects["chance"] = rect;
                    T(c, text, p.X, p.Y - 9, 16, Yellow, true, SKTextAlign.Center);
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
                var p = f.View.Project(damage.Unit.Pose); string text = U("ArmorLoss", damage.Armor); float half = (Measure(text, 12) + 14) / 2;
                SKPoint q = default;
                foreach (var delta in new SKPoint[] { new(0, 32), new(half + 16, 10), new(0, -20), new(-half - 16, 10) })
                { q = p + delta; var bounds = new SKRect(q.X - half - 10, q.Y - 27, q.X + half + 10, q.Y + 12); if (!markers.Any(bounds.Contains)) break; }
                var rect = Callout(c, q.X, q.Y, text, Yellow, Yellow); occupied.Add(rect); if (Recording is { } l) l.Rects["callout:" + damage.Unit.Id] = rect;
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
                float half = (Measure(text, 12) + 14) / 2;
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
    static partial void DrawResultHighlights(SKCanvas c, TacticalBattleFrame f)
    {
        if (f.Salvo is { InFlight: false } results)
            foreach (var d in results.Damage.Where(d => d.Armor > 0 && d.Unit.Side == f.Battle.PlayerSide)) Diamond(c, f.View.Project(d.Unit.Pose), 10, SKColors.Transparent, Yellow, 1.5f);
        if (f.Salvo is { } salvo)
            foreach (var flight in salvo.Flights.Where(s => s.Flash)) Circle(c, f.View.Project(flight.Shot.To), 15, SKColors.Transparent, flight.Shot.Hit ? Yellow : Red, 2);
        // The admitted projection changes State only at impact, so wrecks cannot reveal a future kill.
        foreach (var u in f.Battle.Units.Where(u => u.State == TacticalUnitState.Destroyed))
        { var p = f.View.Project(u.Pose); Stroke(c, new(p.X - 6, p.Y - 6), new(p.X + 6, p.Y + 6), Muted, 1.5f); Stroke(c, new(p.X - 6, p.Y + 6), new(p.X + 6, p.Y - 6), Muted, 1.5f); }
    }
}
