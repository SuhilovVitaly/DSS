using DeepSpaceSaga.Motion.TacticalBattle;
using System.Globalization;
using SkiaSharp;
using Skin = DeepSpaceSaga.Client.UI.Screens.TacticalBattle.TacticalBattleSkinRenderScope;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal readonly record struct BattleViewport(float S, float Wd)
{
    public static BattleViewport From(int width, int height) => new((float)(height / 720d), (float)(width * 720f / (double)height));
}

internal enum EvadeButtonState { Unavailable, Available, Selected }

internal static class BattleDesign
{
    public static readonly SKColor Heat = SKColor.Parse("#E05A78"), HeatDark = SKColor.Parse("#3A101E"),
        Dust = SKColor.Parse("#B08CE8"), DustDark = SKColor.Parse("#24183A");
    public static readonly SKColor GreenDark = SKColor.Parse("#1E3A26"), DecoyIdle = SKColor.Parse("#142218");
    public static void DecoyRow(SKCanvas c, SKRect r, string text, int charges, int maxCharges, DecoyRowState state, bool compact = false)
    {
        bool active = state == DecoyRowState.Selected, enabled = state != DecoyRowState.Unavailable;
        string role = active ? "ui.box.decoy.selected" : enabled ? "ui.box.decoy" : "ui.box.decoy.unavailable";
        Round(c, r, active ? GreenDark : enabled ? DecoyIdle : Box,
            enabled ? Green.WithAlpha(active ? (byte)255 : (byte)170) : BoxEdge, active ? 1.6f : 1, role: role);
        float left = r.Right - 8 - (float)PyMath.Max(0, maxCharges - 1) * 9;
        float width = left - 8 - (r.Left + 7);
        var tone = active ? Text : enabled ? Green : Muted;
        if (compact) PilotCardColumn.FittedText(c, text, r.Left + 7, r.MidY + 3, 8.5f, width, tone, active);
        else
        {
            float size = FitSize(text, 11, width, active);
            T(c, text, r.Left + 7, r.MidY + size * .36f, size, tone, active);
        }
        for (int i = 0; i < maxCharges; i++)
        {
            bool full = i < charges - (active ? 1 : 0);
            Diamond(c, new(left + i * 9, r.MidY), 3.5f, full ? enabled ? Green : Muted : SKColors.Transparent,
                full ? null : enabled ? Green.WithAlpha(150) : BoxEdge, 1);
        }
    }
    public static SKPoint DecoyPoint(SKPoint point, double courseDeg) => new(
        point.X - 24 * (float)Math.Cos(courseDeg * Math.PI / 180.0), point.Y - 24 * (float)Math.Sin(courseDeg * Math.PI / 180.0));
    public static void DecoyMark(SKCanvas c, SKPoint point, double courseDeg)
    {
        var q = DecoyPoint(point, courseDeg);
        Stroke(c, point, q, Green.WithAlpha(130), 1, [2, 3]);
        Diamond(c, q, 6, MapBg, Green, 1.5f);
        Circle(c, q, 1.8f, Green);
    }
    public static readonly SKColor EvadeIdle = SKColor.Parse("#1A1A2A");
    public static string M(string key, params object?[] values) => values.Length == 0 ? Localization.Get("TacticalBattle." + key)
        : string.Format(CultureInfo.InvariantCulture, Localization.Get("TacticalBattle." + key), values);
    public static void EvadeButton(SKCanvas c, SKRect r, string text, EvadeButtonState state, bool compact = false)
    {
        bool active = state == EvadeButtonState.Selected, enabled = state != EvadeButtonState.Unavailable;
        string role = active ? "ui.box.evade.selected" : enabled ? "ui.box.evade" : "ui.box.evade.unavailable";
        Round(c, r, active ? PurpleDark : enabled ? EvadeIdle : Box,
            enabled ? Purple.WithAlpha(active ? (byte)255 : (byte)170) : BoxEdge, active ? 1.6f : 1, role: role);
        var tone = active ? Text : enabled ? Purple : Muted;
        if (compact) PilotCardColumn.FittedText(c, text, r.MidX, r.MidY + 3.4f, 9.5f, r.Width - 8, tone, active, SKTextAlign.Center);
        else
        {
            float size = FitSize(text, 12, r.Width - 10);
            T(c, text, r.MidX, r.MidY + size * .36f, size, tone, active, SKTextAlign.Center);
        }
    }
    public static void EvasionRing(SKCanvas c, SKPoint point, float radius = 15)
    {
        using var effect = SKPathEffect.CreateDash([3, 3], 3);
        using var paint = new SKPaint { IsAntialias = true, Color = Purple, Style = SKPaintStyle.Stroke, StrokeWidth = 1.6f, PathEffect = effect };
        c.DrawCircle(point, radius, paint);
    }
    public static readonly SKColor PurpleDark = SKColor.Parse("#2A2145");
    public static string U(string key, params object?[] values) => values.Length == 0 ? Localization.Get("TacticalBattle.Ui." + key) : string.Format(CultureInfo.InvariantCulture, Localization.Get("TacticalBattle.Ui." + key), values);
    public static readonly SKColor Bg = SKColor.Parse("#0A0E11"), MapBg = SKColor.Parse("#101518"), Grid = SKColor.Parse("#1C2428"),
        Panel = SKColor.Parse("#0E1B1F"), PanelSel = SKColor.Parse("#1A1E18"), PanelEdge = SKColor.Parse("#2BC7C9"),
        Box = SKColor.Parse("#16262A"), BoxEdge = SKColor.Parse("#2A454A"), ButtonFill = SKColor.Parse("#152A2D"),
        PortraitBg = SKColor.Parse("#222C30"), Cyan = SKColor.Parse("#22E3F0"), Purple = SKColor.Parse("#A98BF5"),
        Orange = SKColor.Parse("#FF7A2F"), Yellow = SKColor.Parse("#F5C04A"), YellowDark = SKColor.Parse("#3A2F10"),
        ButtonInk = SKColor.Parse("#1A1405"), Text = SKColor.Parse("#ECF2F4"), Muted = SKColor.Parse("#9FB3B8"),
        Track = SKColor.Parse("#2A3337"), Red = SKColor.Parse("#FF5A5A"), Green = SKColor.Parse("#6EE08C"),
        ArcFill = SKColor.Parse("#1F8F86").WithAlpha(110), ArcEdge = SKColor.Parse("#3FE0D0").WithAlpha(200), Shell = SKColor.Parse("#FFF4D0");
    [ThreadStatic] internal static BattleLayout? Recording;
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");
    public static string F(double value) => value.ToString("F1", Russian);
    public static string Deg(double value) => (value < 0 ? "−" : "+") + F(Math.Abs(value)) + "°";
    public static string Pct(double value) => Math.Round(value * 100).ToString(CultureInfo.InvariantCulture) + "%";
    public static string Plural(int n, string one, string few, string many) => n + " " +
        (n % 10 == 1 && n % 100 != 11 ? one : n % 10 is >= 2 and <= 4 && n % 100 is not (>= 12 and <= 14) ? few : many);
    public static string Genitive(string name) => name.Length > 1 && name.EndsWith('а') ? name[..^1] + ("гкхжшчщ".Contains(name[^2]) ? "и" : "ы") : name + "а";
    public static float Measure(string text, float size, bool bold = false)
    { using var p = BattleTypography.TextPaint(size, bold); return p.MeasureText(text); }
    public static float FitSize(string text, float size, float width, bool bold = false)
    { while (size > 9 && Measure(text, size, bold) > width) size -= 0.5f; return size; }
    public static void T(SKCanvas c, string text, float x, float y, float size, SKColor color, bool bold = false, SKTextAlign align = SKTextAlign.Left, string role = "ui.text")
    {
        using var p = BattleTypography.TextPaint(size, bold);
        p.Color = TacticalBattleSkinRenderScope.Color(role, "text", color); p.TextAlign = align;
        c.DrawText(text, x, y, p); Recording?.Texts.Add(new(text, p.Color));
    }
    public static void Pair(SKCanvas c, string label, string value, float x, float y, float size)
    { T(c, label, x, y, size, Text, true); T(c, value, x + Measure(label, size, true), y, size + 1, Text, true); Recording?.Texts.Add(new(label + value, Text)); }
    public static void Stroke(SKCanvas c, SKPoint a, SKPoint b, SKColor color, float width, float[]? dash = null)
    {
        using var p = new SKPaint { IsAntialias = true, Color = color, Style = SKPaintStyle.Stroke, StrokeWidth = width, StrokeCap = SKStrokeCap.Round };
        using var effect = dash is null ? null : SKPathEffect.CreateDash(dash, 0); p.PathEffect = effect; c.DrawLine(a, b, p);
    }
    public static void Round(SKCanvas c, SKRect r, SKColor fill, SKColor? edge = null, float width = 1, float radius = 3, string role = "ui.box")
    {
        using var p = new SKPaint { IsAntialias = true, Color = TacticalBattleSkinRenderScope.Color(role, "fill", fill) };
        c.DrawRoundRect(r, radius, radius, p);
        if (Skin.Current is { } skin && !ReferenceEquals(skin, TacticalBattleSkin.Classic) &&
            Skin.Has(role + ".art", "image") && !r.IsEmpty)
        {
            using var clip = new SKRoundRect(r, radius, radius);
            c.Save();
            c.ClipRoundRect(clip, antialias: true);
            Skin.Image(c, role + ".art", r);
            c.Restore();
        }
        if (edge is not null) { p.Color = TacticalBattleSkinRenderScope.Color(role, "stroke", edge.Value); p.Style = SKPaintStyle.Stroke; p.StrokeWidth = width; c.DrawRoundRect(r, radius, radius, p); }
    }
    public static void PanelBox(SKCanvas c, SKRect r, bool selected = false, float radius = 6, string role = "ui.panel")
    {
        if (selected) Round(c, r, SKColors.Transparent, Yellow.WithAlpha(50), 5, radius, role + ".glow");
        Round(c, r, selected ? PanelSel : Panel, selected ? Yellow : PanelEdge.WithAlpha(190), selected ? 1.8f : 1.2f, radius, role);
    }
    public static void Tag(SKCanvas c, SKRect r, string text, float size = 11, bool center = false)
    { Round(c, r, YellowDark, Yellow, 1.2f, 0, "ui.tag"); T(c, text, center ? r.MidX : r.Left + 7, r.MidY + size * 0.36f, size, Yellow, true, center ? SKTextAlign.Center : SKTextAlign.Left, "ui.tag"); }
    public static void BoxText(SKCanvas c, SKRect r, string text, bool center = false)
    { Round(c, r, Box, BoxEdge); float fs = FitSize(text, 11, r.Width - 14); T(c, text, center ? r.MidX : r.Left + 7, r.MidY + fs * 0.36f, fs, Text, align: center ? SKTextAlign.Center : SKTextAlign.Left); }
    public static void Button(SKCanvas c, SKRect r, string text, bool big = false, bool pending = false)
    {
        string role = big ? "ui.button.primary" : "ui.button";
        string pendingRole = role + ".pending";
        bool dimmed = pending && Skin.Current?.Find<object?>(pendingRole, "fill", null, pendingRole) is not null;
        if (big && !dimmed) Round(c, r, SKColors.Transparent, Yellow.WithAlpha(60), 4, 5, "ui.button.glow");
        if (dimmed) role = pendingRole;
        Round(c, r, big ? Yellow : ButtonFill, big ? null : PanelEdge.WithAlpha(150), radius: big ? 5 : 3, role: role);
        float fs = FitSize(text, big ? 14 : 12, r.Width - (big ? 16 : 10), big);
        T(c, text, r.MidX, r.MidY + fs * 0.36f, fs, big ? ButtonInk : Text, big, SKTextAlign.Center, role);
    }
    public static void BigButton(SKCanvas c, SKRect r, string text) => Button(c, r, text, true);
    public static void Bar(SKCanvas c, float x, float y, float w, float h, double frac, SKColor color)
    {
        var track = SKRect.Create(x, y, w, h);
        Round(c, track, Track, radius: h * .5f);
        if (!(frac > 0))
            return;

        var fill = SKRect.Create(x, y, w * (float)PyMath.Clamp(frac, 0, 1), h);
        Round(c, fill, color, radius: h * .5f, role: "ui.bar");
        if (Skin.Has("ui.bar.segments", "image"))
        {
            using var clip = new SKRoundRect(fill, h * .5f, h * .5f);
            c.Save();
            c.ClipRoundRect(clip, antialias: true);
            Skin.Image(c, "ui.bar.segments", track);
            c.Restore();
        }
    }
    public static void Slider(SKCanvas c, float x0, float x1, float y, double t, SKColor color)
    {
        float x = x0 + (x1 - x0) * (float)PyMath.Clamp(t, 0, 1);
        Stroke(c, new(x0, y), new(x1, y), Skin.Color("ui.slider.track", "stroke", Track), 4);
        Stroke(c, new(x0, y), new(x, y), color, 4);
        var edge = Skin.Color("ui.slider.thumb", "stroke", Text);
        if (Skin.Shape("ui.slider.thumb") == "diamond")
            Diamond(c, new(x, y), 6, color, edge, 1.2f);
        else
            Circle(c, new(x, y), 6, color, edge, 1.2f);
    }
    public static void Line(SKCanvas c, float x0, float x1, float y) => Stroke(c, new(x0, y), new(x1, y), BoxEdge, 1);
    public static SKRect Callout(SKCanvas c, float x, float y, string text, SKColor edge, SKColor color)
    { float w = Measure(text, 12) + 14; SKRect r = new(x - w * .5f, y - 17, x + w * .5f, y + 2); Round(c, r, edge == Yellow ? YellowDark : Panel, edge, 1.2f); T(c, text, x, y - 3.5f, 12, color, align: SKTextAlign.Center); return r; }
    public static void Circle(SKCanvas c, SKPoint p, float r, SKColor fill, SKColor? edge = null, float width = 1)
    { using var paint = new SKPaint { IsAntialias = true, Color = fill }; c.DrawCircle(p, r, paint); if (edge is not null) { paint.Color = edge.Value; paint.Style = SKPaintStyle.Stroke; paint.StrokeWidth = width; c.DrawCircle(p, r, paint); } }
    public static void Polygon(SKCanvas c, IEnumerable<SKPoint> points, SKColor fill, SKColor? edge = null, float width = 1)
    { using var path = new SKPath(); var pts = points.ToArray(); path.AddPoly(pts, true); using var p = new SKPaint { IsAntialias = true, Color = fill, StrokeCap = SKStrokeCap.Round }; c.DrawPath(path, p); if (edge is not null) { p.Color = edge.Value; p.Style = SKPaintStyle.Stroke; p.StrokeWidth = width; c.DrawPath(path, p); } }
    public static void Diamond(SKCanvas c, SKPoint p, float r, SKColor fill, SKColor? edge = null, float width = 1) => Polygon(c, [new(p.X, p.Y - r), new(p.X + r, p.Y), new(p.X, p.Y + r), new(p.X - r, p.Y)], fill, edge, width);
    public static void Hex(SKCanvas c, SKPoint p, float r, SKColor color) => Polygon(c, Enumerable.Range(0, 6).Select(i => new SKPoint(p.X + r * MathF.Cos((float)(i * MathF.PI / 3.0)), p.Y + r * MathF.Sin((float)(i * MathF.PI / 3.0)))), color);
    public static void Triangle(SKCanvas c, SKPoint p, float r, double course, SKColor color)
    { float a = (float)(course * Math.PI / 180.0), x = MathF.Cos(a), y = MathF.Sin(a); Polygon(c, [new(p.X + r * x, p.Y + r * y), new(p.X - .7f * r * (x + y), p.Y + .7f * r * (x - y)), new(p.X + .7f * r * (y - x), p.Y - .7f * r * (x + y))], color); }
    public static void Arrow(SKCanvas c, SKPoint a, SKPoint b, SKColor color, float width, float head)
    { float len = SKPoint.Distance(a, b); if (len < 1) return; SKPoint u = Scale(b - a, (float)(1d / len)), tip = b - Scale(u, 12), tail = tip - Scale(u, head), side = new(-u.Y * head * .45f, u.X * head * .45f); Stroke(c, a + Scale(u, 9), tip - Scale(u, .6f * head), color, width); Polygon(c, [tip, tail + side, tail - side], color); }
    public static SKPoint Scale(SKPoint p, float scale) => new(p.X * scale, p.Y * scale);
    public static void CursorGlyph(SKCanvas c, float x, float y, float k) => Polygon(c, new SKPoint[] { new(0, 0), new(0, 17), new(4.5f, 13), new(7.5f, 20), new(10, 19), new(7, 12), new(12.5f, 12) }.Select(p => new SKPoint(x + p.X * k, y + p.Y * k)), Text, Bg);
}
