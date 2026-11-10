using DeepSpaceSaga.Motion.TacticalBattle;
using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;
using Skin = DeepSpaceSaga.Client.UI.Screens.TacticalBattle.TacticalBattleSkinRenderScope;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static partial class TacticalBattleMapRenderer
{
    private const float DroneSilhouetteScale = .60f;
    private static readonly SKPath LightDrone = DronePath("M0 -11 4 -5 4 8 -4 8 -4 -5Z M-6 -2H-9V8H-6Z M6 -2H9V8H6Z");
    private static readonly SKPath HeavyDrone = DronePath("M-4 -9H4L7 -5V9H-7V-5Z M-9 -5H-12V9H-9Z M9 -5H12V9H9Z");
    private static readonly SKPath ThugDrone = DronePath("M-3 -11 4 -7 5 6 2 9 -5 8 -6 -3Z M-8 -4 -11 -1 -10 7 -7 5Z M7 -2H11V6H7Z");

    private static readonly AnomalyFieldLayer AnomalyLayer = new();
    internal static readonly SKColor Own = Cyan, Enemy = Orange, Background = MapBg, Inactive = Muted.WithAlpha(140), Jammed = Yellow;
    internal static double RulerLength(double pixelsPerUnit, float uiScale) => 100;
    internal static ImmutableArray<TacticalUnitSnapshot> VisibleUnits(TacticalBattleFrame f) => f.Battle.Units;
    internal static bool IsEvading(TacticalBattleFrame frame, string id) =>
        frame.FirePlan?.Rows.Any(r => r.Drone.Id == id && r.Evade) == true || frame.Salvo?.Evading.Contains(id) == true;
    internal static bool HasDecoy(TacticalBattleFrame frame, TacticalUnitSnapshot unit) =>
        unit.Side == frame.Battle.PlayerSide && frame.FirePlan?.Rows.Any(r => r.Drone.Id == unit.Id && r.Decoy) == true
        || frame.Salvo is { } salvo && salvo.DecoysLaunched.Contains(unit.Id) && (!salvo.InFlight || unit.Side == frame.Battle.PlayerSide);
    internal static int TargetCount(TacticalBattleFrame f, TacticalUnitSnapshot u) => u.Jammed || u.InDust || u.State != TacticalUnitState.Air || f.FirePlan is null ? 0 : u.Side == f.Battle.PlayerSide ? f.FirePlan.Rows.FirstOrDefault(r => r.Drone.Id == u.Id)?.Group.List.Length ?? 0 : f.Battle.Incoming.Count(i => i.ShooterId == u.Id);
    internal static string? Selected(TacticalBattleFrame f) => f.Salvo is { InFlight: true } ? PilotCardModel.ActiveShooter(f) : f.Salvo is not null ? PilotCardModel.ResultShot(f)?.ShooterId : f.SelectedDroneId;
    internal static SKRect Safe(TacticalBattleView v) => new(202, 6, v.PixelWidth - 246, 714);
    internal static SKPoint MarkerPoint(TacticalBattleView view, TacticalUnitSnapshot unit)
    {
        var p = view.Project(unit.Pose);
        if (unit.Kind != TacticalUnitKind.Carrier) return p;
        var safe = Safe(view); safe.Inflate(-34, -34);
        return new((float)PyMath.Clamp(p.X, safe.Left, safe.Right), (float)PyMath.Clamp(p.Y, safe.Top, safe.Bottom));
    }
    internal static void Draw(SKCanvas c, TacticalBattleFrame f, InteractiveAnomalyField? interactiveField = null)
    {
        var v = f.View; var units = VisibleUnits(f).Where(u => u.State != TacticalUnitState.Destroyed).ToArray();
        Skin.Box(c, "field.background", SKRect.Create(v.PixelWidth, v.PixelHeight), MapBg, null, 1);
        if (interactiveField is null || interactiveField.Prepare(c, f))
            DrawAnomalies(c, f, interactiveField);
        bool field = !f.Battle.Anomalies.IsDefaultOrEmpty && f.Battle.Rules.Anomalies is not null;
        using var grid = new SKPaint
        {
            IsAntialias = true,
            Color = Skin.Color("field.grid", "stroke", Grid),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            StrokeCap = SKStrokeCap.Round,
            BlendMode = field ? SKBlendMode.Lighten : SKBlendMode.SrcOver
        };
        using var gridPath = new SKPath();
        for (float x = 26; x < v.PixelWidth; x += 26)
        {
            gridPath.MoveTo(x, 0);
            gridPath.LineTo(x, v.PixelHeight);
        }
        for (float y = 26; y < v.PixelHeight; y += 26)
        {
            gridPath.MoveTo(0, y);
            gridPath.LineTo(v.PixelWidth, y);
        }
        c.DrawPath(gridPath, grid);
        float len = (float)(100 * v.PixelsPerUnit);
        Stroke(c, new(14, 706), new(14 + len, 706), Yellow, 1.3f);
        Stroke(c, new(14, 701), new(14, 711), Yellow, 1.3f); Stroke(c, new(14 + len, 701), new(14 + len, 711), Yellow, 1.3f);
        T(c, "100 " + BattleFormat.Localize("MapUnits"), 14 + len * .5f, 699, 10.5f, Text, align: SKTextAlign.Center);
        var occupied = new List<SKRect>(units.Length);
        using var rotatedHull = new SKPath();
        foreach (var unit in units)
        {
            var p = v.Project(unit.Pose);
            if (unit.Kind == TacticalUnitKind.Drone && DroneSilhouette(unit.FrameId) is { } hull)
            {
                hull.Transform(SKMatrix.CreateRotationDegrees(DroneRotation(unit)), rotatedHull);
                var bounds = rotatedHull.Bounds;
                bounds.Offset(p.X, p.Y);
                occupied.Add(bounds);
            }
            else
            {
                float radius = unit.Kind == TacticalUnitKind.Carrier ? 16 : 7;
                occupied.Add(new(p.X - radius, p.Y - radius, p.X + radius, p.Y + radius));
            }
        }
        occupied.Add(new(0, 0, 202, 6 + 87 * f.Battle.Units.Count(u => u.Kind == TacticalUnitKind.Drone && u.Side == f.Battle.PlayerSide)));
        occupied.Add(new(v.PixelWidth - 246, 0, v.PixelWidth, f.Panel.Bottom + (HoverCard.Build(f) is not null ? 306 : f.HoverUnitId is null && AnomalyPresentation.HoverKind(f) is null ? 6 : 116)));
        occupied.Add(new(10, 686, 18 + len, 714));
        occupied.AddRange([new(0, 0, v.PixelWidth, 2), new(0, 718, v.PixelWidth, 720), new(0, 0, 2, 720), new(v.PixelWidth - 2, 0, v.PixelWidth, 720)]);
        var wrecks = f.Battle.Units.Where(u => u.State == TacticalUnitState.Destroyed).ToArray();
        foreach (var wreck in wrecks)
        {
            var point = v.Project(wreck.Pose);
            occupied.Add(new(point.X - 7, point.Y - 7, point.X + 7, point.Y + 7));
        }
        DrawTraversedPaths(c, f);
        DrawMovement(c, f, occupied); DrawPhaseOverlays(c, f, occupied, interactiveField);
        var inside = Safe(v); inside.Inflate(-34, -34);
        using var hullPaint = new SKPaint { IsAntialias = true, StrokeWidth = .8f };
        var seamColor = Skin.Color("field.background", "fill", MapBg).WithAlpha(40);
        foreach (var u in units)
        {
            var p = v.Project(u.Pose); bool own = u.Side == f.Battle.PlayerSide;
            string role = "field.unit." + (u.Kind == TacticalUnitKind.Carrier ? "carrier" : "drone") + (own ? ".own" : ".enemy");
            var color = Skin.Color(role, "fill", own ? Cyan : Orange);
            if (u.Kind == TacticalUnitKind.Carrier) { if (inside.Contains(p)) Hex(c, p, 15, color); else Edge(c, f, u, p, inside, occupied, color); }
            else
            {
                if (DroneSilhouette(u.FrameId) is { } hull)
                {
                    c.Save();
                    c.Translate(p.X, p.Y);
                    c.RotateDegrees(DroneRotation(u));
                    hullPaint.Color = color;
                    c.DrawPath(hull, hullPaint);
                    hullPaint.Color = seamColor;
                    c.DrawLine(0, -5 * DroneSilhouetteScale, 0, 4 * DroneSilhouetteScale, hullPaint);
                    c.Restore();
                }
                else if (own) Diamond(c, p, 6, color);
                else Triangle(c, p, 7, u.Pose.Course, color);
                if (u.Id == Selected(f)) Circle(c, p, 10, SKColors.Transparent, Text, 1.2f);
            }
            if (u.Kind == TacticalUnitKind.Drone && IsEvading(f, u.Id))
                EvasionRing(c, p);
        }
        foreach (var u in f.Battle.Units.Where(u => u.Kind == TacticalUnitKind.Drone && HasDecoy(f, u)))
        {
            var p = v.Project(u.Pose);
            DecoyMark(c, p, u.Pose.Course);
            var q = DecoyPoint(p, u.Pose.Course);
            var r = new SKRect(q.X - 8, q.Y - 8, q.X + 8, q.Y + 8);
            occupied.Add(r);
            if (Recording is { } layout) layout.Rects["decoy:" + u.Id] = r;
        }
        foreach (var u in units.Where(u => u.Kind != TacticalUnitKind.Carrier || inside.Contains(v.Project(u.Pose)))
                     .OrderBy(u => u.Id == Selected(f) ? 0 : u.Kind == TacticalUnitKind.Carrier ? 1 : 2).ThenBy(u => v.Project(u.Pose).Y)) Label(c, f, u, occupied);
        foreach (var wreck in wrecks)
        {
            var point = v.Project(wreck.Pose);
            Stroke(c, point + new SKPoint(-6, -6), point + new SKPoint(6, 6), Muted, 1.5f);
            Stroke(c, point + new SKPoint(-6, 6), point + new SKPoint(6, -6), Muted, 1.5f);
            if (wreck.Kind == TacticalUnitKind.Drone) Label(c, f, wreck, occupied);
        }
        DrawResultHighlights(c, f); DrawHover(c, f);
    }

    private static SKPath DronePath(string svg)
    {
        var path = SKPath.ParseSvgPathData(svg);
        path.Transform(SKMatrix.CreateScale(DroneSilhouetteScale, DroneSilhouetteScale));
        return path;
    }

    private static SKPath? DroneSilhouette(string frameId) => frameId switch
    {
        "light" => LightDrone,
        "heavy" => HeavyDrone,
        "thug" => ThugDrone,
        _ => null
    };

    // The paths point up, while course zero points right.
    private static float DroneRotation(TacticalUnitSnapshot unit) => (float)(unit.Pose.Course + 90);

    private static void Label(SKCanvas c, TacticalBattleFrame f, TacticalUnitSnapshot u, List<SKRect> occupied)
    {
        bool carrier = u.Kind == TacticalUnitKind.Carrier;
        bool wreck = u.State == TacticalUnitState.Destroyed;
        var p = f.View.Project(u.Pose); float size = carrier ? 13.5f : 10, bw = carrier ? 54 : wreck ? 0 : 25, gap = carrier ? 20 : 9;
        string text = carrier ? u.Id : PilotCardModel.Name(u); float w = (float)PyMath.Max(Measure(text, size), bw), h = size + (carrier ? 11 : wreck ? 2 : 9);
        string[] sides = carrier ? ["d", "u", "l", "r"] : u.Side == f.Battle.PlayerSide ? ["l", "u", "d", "r"] : ["r", "u", "d", "l"];
        SKRect rect = default; int chosenOffset = 0; bool found = false;
        foreach (int offset in new[] { 0, 10, 20, 32, 46, 62, 80 })
        {
            foreach (string side in sides)
            {
                rect = side switch { "l" => SKRect.Create(p.X - gap - offset - w, p.Y - h * .5f, w, h), "r" => SKRect.Create(p.X + gap + offset, p.Y - h * .5f, w, h), "u" => SKRect.Create(p.X - w * .5f, p.Y - gap - offset - h, w, h), _ => SKRect.Create(p.X - w * .5f, p.Y + gap + offset, w, h) };
                chosenOffset = offset; var expanded = rect; expanded.Inflate(2, 1);
                if (occupied.Any(r => r.IntersectsWith(expanded))) continue; found = true; break;
            }
            if (found) break;
        }
        occupied.Add(rect); if (Recording is { } layout) layout.Rects["label:" + u.Id] = rect;
        if (chosenOffset > 0)
        { var q = new SKPoint((float)PyMath.Clamp(p.X, rect.Left, rect.Right), (float)PyMath.Clamp(p.Y, rect.Top, rect.Bottom)); float distance = SKPoint.Distance(p, q); if (distance > 0) Stroke(c, p + Scale(q - p, (float)(10d / distance)), q, Muted.WithAlpha(140), .9f); }
        float y = rect.Top + size - 1;
        T(c, text, rect.MidX, y, size, wreck ? Muted : Text, align: SKTextAlign.Center);
        if (wreck) return;
        float x0 = rect.MidX - bw * .5f;
        float thickness = carrier ? 2 : 1.5f;
        void Health(float yy, double fraction, SKColor color) { Stroke(c, new(x0, yy), new(x0 + bw, yy), Track, thickness); if (fraction > 0) Stroke(c, new(x0, yy), new(x0 + bw * (float)PyMath.Clamp(fraction, 0, 1), yy), color, thickness); }
        double hull = u.MaxHull > 0 ? u.Hull / (double)u.MaxHull : 0;
        Health(y + (carrier ? 5 : 4), carrier ? hull : u.MaxArmor > 0 ? u.Armor / (double)u.MaxArmor : 0, carrier ? Cyan : Purple);
        Health(y + (carrier ? 9 : 7), hull, Cyan);
    }
    private static void Edge(SKCanvas c, TacticalBattleFrame f, TacticalUnitSnapshot u, SKPoint p, SKRect safe, List<SKRect> occupied, SKColor color)
    {
        var q = MarkerPoint(f.View, u);
        Hex(c, q, 11, color.WithAlpha(200)); float distance = SKPoint.Distance(q, p); var direction = Scale(p - q, (float)(1d / PyMath.Max(1, distance)));
        var tip = q + Scale(direction, 24); var side = new SKPoint(-direction.Y * 6, direction.X * 6);
        Stroke(c, tip, tip - Scale(direction, 7) + side, color, 2); Stroke(c, tip, tip - Scale(direction, 7) - side, color, 2);
        string text = u.Id + " · " + Math.Round((double)distance / f.View.PixelsPerUnit).ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + BattleFormat.Localize("MapUnits");
        float w = Measure(text, 14); SKRect rect = default; float y = 0;
        foreach (float shift in new float[] { 0, -24, 26, -44, 46 })
        { bool left = p.X < q.X; float x = shift == 0 ? left ? q.X + 18 : q.X - 18 - w : left ? q.X - 12 : q.X + 12 - w; y = q.Y + shift; rect = new(x - 2, y - 12, x + w + 2, y + 9); if (!occupied.Any(r => r.IntersectsWith(rect))) break; }
        occupied.Add(rect); if (Recording is { } layout) layout.Rects["edge:" + u.Id] = rect; T(c, text, rect.Left + 2, y + 5, 14, Text);
    }
    private static void DrawTraversedPaths(SKCanvas canvas, TacticalBattleFrame frame)
    {
        if (frame.Battle.Paths.IsDefaultOrEmpty) return;
        using var path = new SKPath();
        foreach (var dronePaths in frame.Battle.Paths.GroupBy(trail => trail.DroneId))
        {
            TacticalPoseSnapshot? last = null;
            foreach (var trail in dronePaths.OrderBy(trail => trail.Turn))
            {
                if (trail.Points.Length < 2) continue;
                if (last != trail.Points[0]) path.MoveTo(frame.View.Project(trail.Points[0]));
                for (int i = 1; i < trail.Points.Length; i++) path.LineTo(frame.View.Project(trail.Points[i]));
                last = trail.Points[^1];
            }
        }
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            Color = Text.WithAlpha(135),
            StrokeWidth = 1.2f,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };
        canvas.DrawPath(path, paint);
    }

    static partial void DrawMovement(SKCanvas c, TacticalBattleFrame f, List<SKRect> occupied);
    static partial void DrawPhaseOverlays(SKCanvas c, TacticalBattleFrame f, List<SKRect> occupied, InteractiveAnomalyField? interactiveField);

    private static void DrawAnomalies(SKCanvas canvas, TacticalBattleFrame frame, InteractiveAnomalyField? interactiveField)
    {
        if (interactiveField is null) AnomalyLayer.Draw(canvas, frame);
        else if (!frame.Battle.Anomalies.IsDefaultOrEmpty && frame.Battle.Rules.Anomalies is not null) interactiveField.Draw(canvas);
    }
    static partial void DrawResultHighlights(SKCanvas c, TacticalBattleFrame f);
    static partial void DrawHover(SKCanvas c, TacticalBattleFrame f);
}
