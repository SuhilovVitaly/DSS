using DeepSpaceSaga.Motion.TacticalBattle;
using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;
using Skin = DeepSpaceSaga.Client.UI.Screens.TacticalBattle.TacticalBattleSkinRenderScope;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static partial class TacticalBattleMapRenderer
{
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
        var occupied = units.Select(u => { var p = v.Project(u.Pose); float r = u.Kind == TacticalUnitKind.Carrier ? 16 : 9; return new SKRect(p.X - r, p.Y - r, p.X + r, p.Y + r); }).ToList();
        occupied.Add(new(0, 0, 202, 6 + 87 * f.Battle.Units.Count(u => u.Kind == TacticalUnitKind.Drone && u.Side == f.Battle.PlayerSide)));
        occupied.Add(new(v.PixelWidth - 246, 0, v.PixelWidth, f.Panel.Bottom + (HoverCard.Build(f) is not null ? 306 : f.HoverUnitId is null && AnomalyPresentation.HoverKind(f) is null ? 6 : 116)));
        occupied.Add(new(10, 686, 18 + len, 714));
        occupied.AddRange([new(0, 0, v.PixelWidth, 2), new(0, 718, v.PixelWidth, 720), new(0, 0, 2, 720), new(v.PixelWidth - 2, 0, v.PixelWidth, 720)]);
        DrawMovement(c, f, occupied); DrawPhaseOverlays(c, f, occupied, interactiveField);
        var inside = Safe(v); inside.Inflate(-34, -34);
        foreach (var u in units)
        {
            var p = v.Project(u.Pose); bool own = u.Side == f.Battle.PlayerSide;
            string role = "field.unit." + (u.Kind == TacticalUnitKind.Carrier ? "carrier" : "drone") + (own ? ".own" : ".enemy");
            var color = Skin.Color(role, "fill", own ? Cyan : Orange);
            if (u.Kind == TacticalUnitKind.Carrier) { if (inside.Contains(p)) Hex(c, p, 15, color); else Edge(c, f, u, p, inside, occupied, color); }
            else { if (own) Diamond(c, p, 7.5f, color); else Triangle(c, p, 9, u.Pose.Course, color); if (u.Id == Selected(f)) Circle(c, p, 12, SKColors.Transparent, Text, 1.2f); }
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
        DrawResultHighlights(c, f); DrawHover(c, f);
    }
    private static void Label(SKCanvas c, TacticalBattleFrame f, TacticalUnitSnapshot u, List<SKRect> occupied)
    {
        bool carrier = u.Kind == TacticalUnitKind.Carrier;
        var p = f.View.Project(u.Pose); float size = carrier ? 13.5f : 12.5f, bw = carrier ? 54 : 32, gap = carrier ? 20 : 11;
        string text = carrier ? u.Id : PilotCardModel.Name(u); float w = (float)PyMath.Max(Measure(text, size), bw), h = size + 11;
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
        float y = rect.Top + size - 1; T(c, text, rect.MidX, y, size, Text, align: SKTextAlign.Center); float x0 = rect.MidX - bw * .5f;
        void Health(float yy, double fraction, SKColor color) { Stroke(c, new(x0, yy), new(x0 + bw, yy), Track, 2); if (fraction > 0) Stroke(c, new(x0, yy), new(x0 + bw * (float)PyMath.Clamp(fraction, 0, 1), yy), color, 2); }
        double hull = u.MaxHull > 0 ? u.Hull / (double)u.MaxHull : 0;
        Health(y + 5, carrier ? hull : u.MaxArmor > 0 ? u.Armor / (double)u.MaxArmor : 0, carrier ? Cyan : Purple); Health(y + 9, hull, Cyan);
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
