using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace DeepSpaceSaga.TetrarchDeck;

internal static class Ink
{
    public static readonly Color Space = Color.FromArgb(10, 16, 21), Panel = Color.FromArgb(16, 27, 33),
        Rule = Color.FromArgb(45, 65, 72), Text = Color.FromArgb(232, 238, 235), Muted = Color.FromArgb(155, 177, 181),
        Cyan = Color.FromArgb(92, 218, 214), Gold = Color.FromArgb(244, 191, 89);
    public static readonly Color[] Uniforms = [Gold, Color.Coral, Color.CadetBlue, Color.Thistle, Color.LightBlue, Color.LightSalmon, Color.DarkSeaGreen];

    public static void Fill(Graphics g, Color color, float x, float y, float w, float h)
    {
        using var b = new SolidBrush(color);
        g.FillRectangle(b, x, y, w, h);
    }
    public static void Line(Graphics g, Color color, float width, PointF a, PointF b)
    {
        using var p = new Pen(color, width);
        g.DrawLine(p, a, b);
    }
    public static void Poly(Graphics g, Color color, params PointF[] points)
    {
        using var b = new SolidBrush(color);
        g.FillPolygon(b, points);
    }
    public static void Oval(Graphics g, Color color, float x, float y, float w, float h)
    {
        using var b = new SolidBrush(color);
        g.FillEllipse(b, x, y, w, h);
    }
    public static void TextAt(Graphics g, string text, float x, float y, float size, Color color, bool bold = false, bool mono = false)
    {
        using var f = new Font(mono ? "Consolas" : "Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
        using var b = new SolidBrush(color);
        g.DrawString(text, f, b, x, y, StringFormat.GenericTypographic);
    }
}

internal sealed class DeckPainter : IDisposable
{
    private readonly Deck deck;
    private readonly CrewFigure figure = new(Path.Combine(AppContext.BaseDirectory, "Art", "crew-motion.bin"));
    private readonly Dictionary<string, Bitmap> art;
    private readonly Fixture[] fixtures;
    private readonly Bitmap floor;
    private Bitmap? scenery;
    private (Rectangle Viewport, float Scale, PointF Offset) cachedCamera;
    private readonly RectangleF bounds;
    public float Zoom { get; set; } = 1;
    public PointF Pan { get; set; }
    public bool Labels { get; set; } = true;
    private float scale;
    private PointF offset;
    public static PointF Iso(float x, float y) => new((x - y) * 16, (x + y) * 8);
    private static PointF Lift(PointF p, float z) => new(p.X, p.Y - z);
    private static PointF Mid(Tile t) => Iso(t.X + .5f, t.Y + .5f);
    private static PointF Mix(PointF a, PointF b, float t) => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
    private static PointF Feet(Walker w) => Iso(w.Position.X, w.Position.Y);
    private static PointF Mount(Fixture f) =>
        Iso(f.Footprint.X + f.Footprint.Width * .5f, f.Footprint.Y + f.Footprint.Height * .5f);
    internal const float HumanPixels = Deck.HumanHeight * 17.5f;

    public DeckPainter(Deck deck, Dictionary<string, Bitmap> art)
    {
        this.deck = deck;
        this.art = art;
        fixtures = deck.Fixtures.OrderBy(f => Mount(f).Y).ToArray();
        var points = deck.Hull.Select(Mid).ToArray();
        bounds = RectangleF.FromLTRB(points.Min(p => p.X) - 34, points.Min(p => p.Y) - 83,
            points.Max(p => p.X) + 34, points.Max(p => p.Y) + 42);
        floor = new Bitmap((int)bounds.Width * 2, (int)bounds.Height * 2, PixelFormat.Format32bppPArgb);
        using var g = Graphics.FromImage(floor);
        g.ScaleTransform(2, 2);
        g.TranslateTransform(-bounds.X, -bounds.Y);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        PaintFloor(g);
    }

    private void PaintFloor(Graphics g)
    {
        foreach (var t in deck.Hull.Where(t => !deck.Floor.ContainsKey(t)).OrderBy(t => t.X + t.Y))
        {
            int x = t.X, y = t.Y;
            var a = Iso(x, y); var b = Iso(x + 1, y); var c = Iso(x + 1, y + 1); var d = Iso(x, y + 1);
            Ink.Poly(g, Color.FromArgb(39, 50, 57), a, b, c, d);
            if (!deck.Hull.Contains(t.Offset(1, 0))) Ink.Poly(g, Ink.Panel, b, c, Lift(c, -17), Lift(b, -17));
            if (!deck.Hull.Contains(t.Offset(0, 1))) Ink.Poly(g, Ink.Rule, d, c, Lift(c, -17), Lift(d, -17));
        }
        foreach (var tile in deck.Floor.Keys.OrderBy(t => t.X + t.Y))
        {
            int x = tile.X, y = tile.Y;
            var a = Iso(x, y); var b = Iso(x + 1, y); var c = Iso(x + 1, y + 1); var d = Iso(x, y + 1);
            var room = deck.Floor[tile];
            Ink.Poly(g, Color.FromArgb(79, 86, 88), a, b, c, d);
            var seam = Color.FromArgb(63, 70, 73);
            Ink.Line(g, seam, .65f, d, c);
            Ink.Line(g, seam, .65f, b, c);

            void Edge(Tile other, PointF p, PointF q, bool front)
            {
                if (deck.OpenEdge(tile, other))
                {
                    return;
                }
                bool outer = !deck.Floor.ContainsKey(other);
                if (front && !outer) return; // The adjacent tile owns this cutaway wall.
                if (front)
                {
                    var bottomP = Lift(p, -18); var bottomQ = Lift(q, -18);
                    Ink.Poly(g, Color.FromArgb(33, 47, 54), p, q, bottomQ, bottomP);
                    Ink.Line(g, Color.FromArgb(94, 110, 116), 1.4f, p, q);
                    Ink.Line(g, Color.FromArgb(79, 94, 99), 1, bottomP, bottomQ);
                    Ink.Line(g, Ink.Space, 1, bottomP, p);
                    var m = Mix(p, q, .3f);
                    Ink.Line(g, (x + y) % 4 == 0 ? Ink.Gold : Color.FromArgb(82, 102, 107), 2, Lift(m, -8), Lift(Mix(p, q, .68f), -8));
                    if ((x + y) % 3 == 0)
                    {
                        var center = Mix(p, q, .5f);
                        Ink.Poly(g, Color.FromArgb(58, 73, 79), Lift(p, -2), Lift(q, -2), Lift(Mix(p, q, .76f), -14), Lift(Mix(p, q, .24f), -14));
                        Ink.Line(g, Color.FromArgb(131, 143, 143), 1, Lift(p, -2), Lift(center, -5));
                    }
                    if (other.Y > y && room?.Type.Kind == "module.engine" && x - room.Origin.X is >= 3 and <= 8)
                    {
                        Ink.Line(g, Color.FromArgb(117, 89, 231, 242), 7, Lift(Mix(p, q, .1f), -12), Lift(Mix(p, q, .92f), -12));
                        Ink.Line(g, Color.FromArgb(202, 250, 248), 2, Lift(Mix(p, q, .1f), -12), Lift(Mix(p, q, .92f), -12));
                    }
                }
                else
                {
                    bool hab = room?.Type.Cabins > 0;
                    float h = outer ? 32 : hab ? 20 : 13;
                    var topP = Lift(p, h); var topQ = Lift(q, h);
                    var finish = hab ? (x % 2 == y % 2 ? Color.FromArgb(142, 135, 117) : Color.FromArgb(153, 146, 129))
                        : x % 2 == y % 2 ? Color.FromArgb(53, 67, 72) : Color.FromArgb(60, 75, 78);
                    Ink.Poly(g, finish, p, q, topQ, topP);
                    Ink.Line(g, hab ? Color.FromArgb(211, 193, 163) : Color.FromArgb(132, 145, 142), 1.8f, topP, topQ);
                    Ink.Line(g, Color.FromArgb(32, 46, 50), 2, topP, p);
                    Ink.Line(g, Color.FromArgb(26, 39, 44), 1, Lift(p, 3), Lift(q, 3));
                    if (outer)
                    {
                        Ink.Poly(g, hab ? Color.FromArgb(155, 143, 122) : Color.FromArgb(74, 88, 92), Lift(Mix(p, q, .12f), h - 2), Lift(Mix(p, q, .88f), h - 2),
                            Lift(Mix(p, q, .81f), h - 7), Lift(Mix(p, q, .17f), h - 7));
                        Ink.Line(g, Color.FromArgb(107, 125, 127), 1.1f, Lift(Mix(p, q, .18f), 6), Lift(Mix(p, q, .79f), 6));
                        if (room?.Type.Kind == "module.bridge.navigation.computer")
                        {
                            Ink.Poly(g, Color.FromArgb(20, 47, 58), Lift(Mix(p, q, .08f), h - 2), Lift(Mix(p, q, .92f), h - 2),
                                Lift(Mix(p, q, .9f), 7), Lift(Mix(p, q, .1f), 7));
                            Ink.Line(g, Color.FromArgb(136, 218, 225), 1.2f, Lift(Mix(p, q, .12f), 8), Lift(Mix(p, q, .88f), 8));
                        }
                    }
                    if ((outer || hab) && (x + y) % 3 != 0)
                        Ink.Line(g, hab ? Ink.Gold : Ink.Cyan, 2.3f, Lift(Mix(p, q, .14f), h - 5), Lift(Mix(p, q, .86f), h - 5));
                }
            }
            Edge(tile.Offset(-1, 0), a, d, false);
            Edge(tile.Offset(0, -1), a, b, false);
            Edge(tile.Offset(1, 0), b, c, true);
            Edge(tile.Offset(0, 1), d, c, true);
        }
    }

    public void Fit(Rectangle viewport)
    {
        scale = Math.Max(.05f, Math.Min((viewport.Width - 36) / bounds.Width, (viewport.Height - 56) / bounds.Height)) * Zoom;
        offset = new(viewport.Left + viewport.Width / 2f - (bounds.Left + bounds.Width / 2) * scale + Pan.X,
            viewport.Top + viewport.Height / 2f - (bounds.Top + bounds.Height / 2) * scale + Pan.Y);
    }
    public PointF Screen(PointF p) => new(offset.X + p.X * scale, offset.Y + p.Y * scale);
    public PointF Screen(Walker w) => Screen(Feet(w));
    public PointF Screen(Tile t) => Screen(Mid(t));
    public Tile Pick(PointF p)
    {
        float x = (p.X - offset.X) / scale / 16, y = (p.Y - offset.Y) / scale / 8;
        return new((int)MathF.Floor((y + x) / 2), (int)MathF.Floor((y - x) / 2));
    }
    public Walker? ActorAt(DeckSession s, PointF p) => s.Crew.OrderByDescending(w => Feet(w).Y).FirstOrDefault(w =>
    {
        var f = Screen(w);
        return Math.Abs(p.X - f.X) < Math.Max(5, 9 * scale) && p.Y > f.Y - Math.Max(16, 41 * scale) && p.Y < f.Y + 3 * scale;
    });
    public Fixture? FixtureAt(PointF pixel)
    {
        if (scale == 0) return null;
        var p = new PointF((pixel.X - offset.X) / scale, (pixel.Y - offset.Y) / scale);
        for (int i = fixtures.Length - 1; i >= 0; i--)
        {
            var f = fixtures[i];
            var box = FixtureBounds(f);
            if (!box.Contains(p)) continue;
            var sprite = art[f.Art];
            int x = Math.Clamp((int)((p.X - box.Left) / box.Width * sprite.Width), 0, sprite.Width - 1);
            int y = Math.Clamp((int)((p.Y - box.Top) / box.Height * sprite.Height), 0, sprite.Height - 1);
            if (sprite.GetPixel(x, y).A >= 120) return f;
        }
        return null;
    }

    public void Draw(Graphics g, Rectangle viewport, DeckSession session, Tile? hover, double time)
    {
        Fit(viewport);
        DrawScenery(g, viewport);
        var state = g.Save();
        g.SetClip(viewport);
        g.TranslateTransform(offset.X, offset.Y);
        g.ScaleTransform(scale, scale);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBilinear;
        if (hover is Tile h && deck.Floor.ContainsKey(h) && !deck.Equipment.Contains(h))
            Ink.Poly(g, Color.FromArgb(82, Ink.Cyan), Iso(h.X, h.Y), Iso(h.X + 1, h.Y), Iso(h.X + 1, h.Y + 1), Iso(h.X, h.Y + 1));
        if (session.Selected is { } selected && selected.Route.Count > 0)
        {
            var prev = Feet(selected);
            foreach (var step in selected.Route)
            {
                var next = Mid(step);
                Ink.Line(g, Color.FromArgb(185, Ink.Gold), 1.7f, prev, next);
                Ink.Oval(g, Ink.Gold, next.X - 1.6f, next.Y - 1.6f, 3.2f, 3.2f);
                prev = next;
            }
            using var target = new Pen(Ink.Gold, 2);
            g.DrawEllipse(target, prev.X - 8, prev.Y - 4, 16, 8);
        }

        // Furniture is baked into the scenery. Animate powered instruments, then
        // composite each actor with just the overlapping foreground equipment.
        foreach (var fixture in fixtures)
            DrawSignal(g, fixture, time);
        foreach (var w in session.Crew.OrderBy(w => Feet(w).Y))
        {
            var feet = Feet(w);
            var box = new RectangleF(feet.X - 23, feet.Y - 48, 46, 57);
            var at = Screen(new PointF(box.Left, box.Top));
            if (!viewport.IntersectsWith(Rectangle.Ceiling(new RectangleF(at.X, at.Y, box.Width * scale, box.Height * scale)))) continue;
            var actorState = g.Save();
            g.SetClip(box, CombineMode.Intersect);
            DrawPerson(g, w, session.Selected, time);
            foreach (var fixture in fixtures)
                if (Behind(w, fixture) && FixtureBounds(fixture).IntersectsWith(box))
                {
                    g.DrawImage(art[fixture.Art], FixtureBounds(fixture));
                    DrawSignal(g, fixture, time);
                }
            g.Restore(actorState);
        }
        if (session.Selected is { } tracked)
        {
            var feet = Feet(tracked);
            float top = feet.Y - HumanPixels - 6;
            Ink.Poly(g, Ink.Space, new(feet.X - 4.5f, top - 2), new(feet.X + 4.5f, top - 2), new(feet.X, top + 3));
            Ink.Poly(g, Ink.Gold, new(feet.X - 3, top - 1), new(feet.X + 3, top - 1), new(feet.X, top + 2));
        }
        g.Restore(state);
        if (Labels) DrawLabels(g, viewport);
    }

    private static bool Behind(Walker w, Fixture f)
    {
        var p = w.Position;
        if (p.X >= f.Footprint.Right || p.Y >= f.Footprint.Bottom) return false;
        if (p.X <= f.Footprint.Left || p.Y <= f.Footprint.Top) return true;
        return Feet(w).Y < Mount(f).Y;
    }

    private RectangleF FixtureBounds(Fixture f)
    {
        var p = Mount(f);
        var sprite = art[f.Art];
        float w = f.Width, h = w * sprite.Height / sprite.Width;
        float baseDepth = (f.Footprint.Width + f.Footprint.Height) * 8;
        if (f.Art == "drone-utility")
        {
            w = (Deck.DroneLength + Deck.DroneBeam) * 16;
            h = baseDepth + Deck.DroneHeight * HumanPixels / Deck.HumanHeight;
        }
        return new RectangleF(p.X - w / 2, p.Y + baseDepth / 2 - h, w, h);
    }

    private void DrawSignal(Graphics g, Fixture f, double time)
    {
        if (f.Room.Module.PowerState == "On" && f.Art is "bridge" or "scanner" or "generator" or "sensor")
        {
            var dest = FixtureBounds(f);
            float w = dest.Width, h = dest.Height;
            float pulse = (float)(.5 + .5 * Math.Sin(time * 3 + f.Footprint.X));
            var light = f.Art == "generator" ? Ink.Gold : Ink.Cyan;
            float lx = dest.Left + w * (f.Art == "sensor" ? .68f : .43f);
            float ly = dest.Top + h * (f.Art == "generator" ? .58f : f.Art == "scanner" ? .48f : .32f);
            Ink.Oval(g, Color.FromArgb((int)(50 * pulse), light), lx - 5, ly - 3, 10, 6);
            Ink.Line(g, Color.FromArgb((int)(175 * pulse), light), 1.4f, new(lx - 3, ly), new(lx + 3, ly - 1));
            if (f.Art is "scanner" or "bridge")
            {
                bool table = f.Art == "scanner";
                float angle = (float)(time * 1.8 + f.Footprint.Y);
                var origin = new PointF(dest.X + w * (table ? .50f : .69f), dest.Y + h * (table ? .46f : .20f));
                float u = MathF.Cos(angle), v = MathF.Sin(angle);
                var sweep = new PointF(origin.X + u * w * (table ? .16f : .050f),
                    origin.Y + v * h * (table ? .065f : .059f) - u * h * (table ? .012f : .029f));
                Ink.Line(g, Color.FromArgb(200, 160, 255, 246), 1.15f, origin, sweep);
            }
        }
    }

    private void DrawPerson(Graphics g, Walker w, Walker? selected, double time)
    {
        var feet = Feet(w);
        Ink.Oval(g, Color.FromArgb(108, 0, 0, 0), feet.X - 8, feet.Y - 2, 17, 6);
        if (w == selected)
        {
            using var ring = new Pen(Ink.Gold, 1.8f);
            g.DrawEllipse(ring, feet.X - 11, feet.Y - 4, 22, 9);
        }
        figure.Draw(g, art["heads"], w, feet, time);
        // Small transponder above the head keeps the uniform visible against machinery.
        if (w != selected)
            Ink.Line(g, Ink.Uniforms[w.Index % Ink.Uniforms.Length], 1.8f, new(feet.X - 3, feet.Y - HumanPixels - 4), new(feet.X + 3, feet.Y - HumanPixels - 4));
    }

    private void DrawLabels(Graphics g, Rectangle viewport)
    {
        var saved = g.Save();
        g.SetClip(viewport);
        var occupied = new List<RectangleF>();
        foreach (var room in deck.Rooms)
        {
            var anchor = Screen(Iso(room.Origin.X + 8f, room.Origin.Y + 9.4f));
            string label = room.Label.ToUpperInvariant();
            using var font = new Font("Consolas", 10, FontStyle.Bold, GraphicsUnit.Pixel);
            float width = g.MeasureString(label, font).Width + 10;
            var box = new RectangleF(anchor.X - width / 2, anchor.Y, width, 17);
            if (occupied.Any(r => r.IntersectsWith(box))) continue;
            occupied.Add(box);
            Ink.Fill(g, Color.FromArgb(218, 15, 27, 31), box.X, box.Y, box.Width, box.Height);
            Ink.Fill(g, Ink.Cyan, box.Left, box.Top + 3, 2, 11);
            Ink.TextAt(g, label, box.Left + 5, box.Top + 2, 10, Ink.Text, true, true);
        }
        g.Restore(saved);
    }

    private void DrawScenery(Graphics g, Rectangle viewport)
    {
        var camera = (viewport, scale, offset);
        if (scenery == null || cachedCamera != camera)
        {
            scenery?.Dispose();
            scenery = new Bitmap(viewport.Width, viewport.Height, PixelFormat.Format32bppPArgb);
            using var layer = Graphics.FromImage(scenery);
            var rectangle = new Rectangle(Point.Empty, viewport.Size);
            using var wash = new LinearGradientBrush(rectangle, Color.FromArgb(25, 40, 47), Ink.Space, 65f);
            layer.FillRectangle(wash, rectangle);
            var stars = new Random(7919);
            for (int i = 0; i < 180; i++)
            {
                int x = stars.Next(viewport.Width), y = stars.Next(viewport.Height);
                Ink.Fill(layer, Color.FromArgb(stars.Next(40, 132), 145, 172, 181), x, y, i % 9 == 0 ? 2 : 1, 1);
            }
            for (int x = 25; x < viewport.Width; x += 55)
                for (int y = 25; y < viewport.Height; y += 55)
                    Ink.Fill(layer, Color.FromArgb(70, 105, 137, 144), x, y, 2, 2);
            layer.TranslateTransform(offset.X - viewport.X, offset.Y - viewport.Y);
            layer.ScaleTransform(scale, scale);
            layer.InterpolationMode = InterpolationMode.HighQualityBilinear;
            layer.DrawImage(floor, bounds);
            foreach (var f in fixtures)
            {
                var b = f.Footprint;
                Ink.Poly(layer, Color.FromArgb(64, 0, 0, 0), Iso(b.Left, b.Top), Iso(b.Right, b.Top), Iso(b.Right, b.Bottom), Iso(b.Left, b.Bottom));
                layer.DrawImage(art[f.Art], FixtureBounds(f));
            }
            cachedCamera = camera;
        }
        g.DrawImageUnscaled(scenery, viewport.Location);
    }

    public void Dispose()
    {
        scenery?.Dispose();
        floor.Dispose();
        figure.Dispose();
    }
}
