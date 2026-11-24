using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.TetrarchDeck;

internal sealed class DeckView : Control
{
    private readonly Dictionary<string, ModuleType> types;
    private readonly Dictionary<string, Bitmap> art;
    private readonly Dictionary<string, Bitmap> portraits = [];
    private readonly Random random;
    private readonly string root;
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
    private readonly CrewArt wardrobe;
    private readonly Stopwatch watch = Stopwatch.StartNew();
    private double last;
    private Point mouse, drag;
    private bool dragging;
    private string? notice;
    private double noticeUntil;
    private DeckPainter painter;
    internal DeckPainter Camera => painter;
    internal DeckSession Session { get; private set; }
    private readonly ContextMenuStrip scenarioMenu = new();
    private readonly List<(Rectangle Bounds, Action Click)> buttons = [];
    private int sidebar => Math.Clamp(Width / 4, 225, 292);
    private Rectangle Map => new(sidebar, 63, Math.Max(1, Width - sidebar), Math.Max(1, Height - 99));

    public DeckView(string root, ScenarioChoice[] scenarios, Dictionary<string, ModuleType> types, Random random)
    {
        this.root = root;
        this.types = types;
        this.random = random;
        DoubleBuffered = true;
        ResizeRedraw = true;
        TabStop = true;
        art = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Art"), "*.png")
            .ToDictionary(p => Path.GetFileNameWithoutExtension(p), p => new Bitmap(p));
        Session = new(new(scenarios[0], types), random);
        wardrobe = new(scenarios);
        wardrobe.Dress(Session);
        painter = new(Session.Deck, art);
        foreach (var item in scenarios) scenarioMenu.Items.Add(item.Name, null, (_, _) => Change(item));
        timer.Tick += (_, _) =>
        {
            double now = watch.Elapsed.TotalSeconds;
            foreach (var w in Session.Crew) w.Advance((float)Math.Min(.1, now - last));
            last = now;
            Invalidate();
        };
    }
    public void Start() { last = watch.Elapsed.TotalSeconds; timer.Start(); }
    private void Change(ScenarioChoice choice)
    {
        var session = new DeckSession(new(choice, types), random);
        wardrobe.Dress(session);
        var next = new DeckPainter(session.Deck, art) { Labels = painter.Labels };
        painter.Dispose();
        painter = next; Session = session; notice = null;
        Invalidate();
    }
    private void Fit() { painter.Zoom = 1; painter.Pan = PointF.Empty; Invalidate(); }
    private void Select(Walker w, bool focusRoom = false)
    {
        Session.Selected = w;
        if (focusRoom)
        {
            painter.Zoom = Math.Max(2.5f, painter.Zoom);
            painter.Fit(Map);
        }
        var pos = painter.Screen(w);
        if (focusRoom || !Map.Contains(Point.Round(pos)))
            painter.Pan = new(painter.Pan.X + Map.Left + Map.Width / 2 - pos.X, painter.Pan.Y + Map.Top + Map.Height / 2 - pos.Y + 24);
        Invalidate();
    }
    private Bitmap? Portrait(Walker w)
    {
        var id = w.PortraitId;
        if (id == null) return null;
        if (portraits.TryGetValue(id, out var cached)) return cached;
        var path = Path.Combine(root, id);
        if (!File.Exists(path)) return null;
        using var original = new Bitmap(path);
        using var thumb = new Bitmap(original, 128, 128);
        int left = 128, top = 128, right = 0, bottom = 0;
        for (int y = 0; y < thumb.Height; y++)
            for (int x = 0; x < thumb.Width; x++)
                if (thumb.GetPixel(x, y).A > 96)
                {
                    left = Math.Min(left, x); top = Math.Min(top, y);
                    right = Math.Max(right, x + 1); bottom = Math.Max(bottom, y + 1);
                }
        if (right <= left) return null;
        int side = Math.Max(right - left, bottom - top) + 4;
        var portrait = new Bitmap(side, side);
        using var g = Graphics.FromImage(portrait);
        g.DrawImageUnscaled(thumb, (side - left - right) / 2, (side - top - bottom) / 2);
        return portraits[id] = portrait;
    }
    private static string Role(ShipCrewMemberData member) => member.Role switch
    {
        "Captain" => "Капитан",
        "Pilot" => "Пилот",
        "Engineer" => "Инженер",
        "Officer" => "Офицер",
        null or "" => "Экипаж",
        _ => member.Role
    };
    protected override void OnPaint(PaintEventArgs e) => PaintDeck(e.Graphics, watch.Elapsed.TotalSeconds);
    internal void PaintDeck(Graphics g, double time)
    {
        g.Clear(Ink.Space);
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        buttons.Clear();
        var map = Map;

        painter.Fit(map);
        Tile? tile = map.Contains(mouse) ? painter.Pick(mouse) : null;
        var fixture = tile != null ? painter.FixtureAt(mouse) : null;
        painter.Draw(g, map, Session, fixture == null ? tile : null, time);
        Ink.Fill(g, Ink.Panel, 0, 0, Width, 62);
        Ink.Fill(g, Ink.Rule, 0, 62, Width, 1);
        Ink.Poly(g, Ink.Cyan, new(21, 35), new(36, 16), new(51, 35), new(36, 29));
        Ink.Poly(g, Ink.Gold, new(27, 40), new(36, 33), new(45, 40), new(36, 44));
        Ink.TextAt(g, "ТЕТРАРХ", 64, 9, 24, Ink.Text, true);
        Ink.TextAt(g, "DEEP SPACE SAGA", 66, 39, 10, Ink.Muted, false, true);
        Caption(g, "СЦЕНАРИЙ", sidebar + 20, 9);
        int menuX = sidebar + 17;
        Button(g, new(menuX, 25, 143, 26), Session.Deck.Scenario.Name + "   ▾", () => scenarioMenu.Show(this, new Point(menuX, 52)));
        int legendsX = Width - 248;
        Button(g, new(legendsX, 18, 108, 28), painter.Labels ? "✓  Подписи" : "Подписи", () => { painter.Labels = !painter.Labels; Invalidate(); });
        Button(g, new(Width - 129, 18, 110, 28), "Весь корабль", Fit);
        Ink.Fill(g, Ink.Panel, 0, 63, sidebar, Height - 63);
        Ink.Fill(g, Ink.Rule, sidebar - 1, 63, 1, Height - 63);
        var deck = Session.Deck;
        int y0 = 81;
        Caption(g, "НОСИТЕЛЬ  /  " + (deck.Scenario.Ship.Name ?? "").ToUpperInvariant(), 18, y0);
        Ink.TextAt(g, "Экипаж", 17, y0 + 22, 23, Ink.Text, true);
        Ink.TextAt(g, Session.Crew.Count.ToString("00"), sidebar - 48, y0 + 25, 17, Ink.Cyan, true, true);
        int rowY = y0 + 62;
        int rowH = Math.Clamp((Height - 275) / Math.Max(1, Session.Crew.Count), 38, 62);
        foreach (var w in Session.Crew)
        {
            var row = new Rectangle(9, rowY, sidebar - 19, rowH - 3);
            bool active = w == Session.Selected;
            Ink.Fill(g, active ? Color.FromArgb(46, 49, 41) : row.Contains(mouse) ? Color.FromArgb(30, 46, 51) : Ink.Panel, row.X, row.Y, row.Width, row.Height);
            Ink.Fill(g, active ? Ink.Gold : Ink.Rule, row.X, row.Y + 3, active ? 3 : 1, row.Height - 6);
            int h = Math.Min(42, row.Height - 5);
            var pic = new Rectangle(row.Left + 9, row.Top + (row.Height - h) / 2, h, h);
            Ink.Fill(g, Ink.Space, pic.X, pic.Y, pic.Width, pic.Height);
            if (Portrait(w) is { } image) g.DrawImage(image, pic);
            else Ink.TextAt(g, string.Concat(w.Member.DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(n => n[0])), pic.X + 5, pic.Y + 7, 15, Ink.Muted, true, true);
            Ink.Fill(g, Ink.Uniforms[w.Index % Ink.Uniforms.Length], pic.X, pic.Bottom - 2, pic.Width, 2);
            float left = pic.Right + 9;
            Ink.TextAt(g, w.Member.DisplayName, left, row.Top + 3, 15, active ? Ink.Gold : Ink.Text, true);
            Ink.TextAt(g, Role(w.Member), left, row.Top + 22, 12, Ink.Muted);
            buttons.Add((row, () => Select(w, focusRoom: true)));
            rowY += rowH;
        }
        int briefY = rowY + 9;
        Ink.Fill(g, Ink.Rule, 18, briefY, sidebar - 38, 1);
        if (Session.Selected is { } selected)
        {
            Caption(g, selected.Route.Count > 0 ? "НА МАРШРУТЕ" : "МЕСТОПОЛОЖЕНИЕ", 18, briefY + 13);
            Ink.TextAt(g, deck.Location(selected.Cell), 18, briefY + 32, 14, Ink.Text, true);
        }
        var room = fixture?.Room ?? (tile.HasValue ? deck.Floor.GetValueOrDefault(tile.Value) : null);
        if (room != null && briefY + 95 < Height - 36)
        {
            Caption(g, "ПОД КУРСОРОМ", 18, briefY + 62);
            Ink.TextAt(g, fixture?.Description ?? deck.Location(tile!.Value), 18, briefY + 79, 14, Ink.Cyan);
            var m = room.Module;
            var stores = m.Cargo?.Select(c => $"{c.ItemTypeId.Replace("item.", "")} × {c.Quantity}").ToList() ?? [];
            if (m.FuelAmountKg is long fuel) stores.Add($"Топливо · {fuel} кг");
            if (room.Type.Cabins > 0) stores.Add($"Каюты · {room.Type.Cabins}");
            if (room.Type.Kind == "module.drone-bay")
                stores.AddRange((deck.Scenario.Ship.CarrierState?.OwnedDrones ?? []).Where(d => d.HangarModuleId == m.ModuleId).Select(d => d.DroneId + " · " + d.FrameId));
            int lineY = briefY + 101;
            foreach (var line in stores)
            {
                if (lineY > Height - 50) break;
                Ink.TextAt(g, line, 18, lineY, 11, Ink.Muted, false, true);
                lineY += 16;
            }
        }
        else if (Height - briefY > 175)
        {
            var hull = art["tetrarch"];
            int imageHeight = Math.Min(132, Height - briefY - 98);
            int imageWidth = imageHeight * hull.Width / hull.Height;
            g.DrawImage(hull, new Rectangle((sidebar - imageWidth) / 2, Height - 44 - imageHeight, imageWidth, imageHeight));
        }

        int barY = Height - 36;
        Ink.Fill(g, Ink.Panel, 0, barY, Width, 36);
        Ink.Fill(g, Ink.Rule, 0, barY, Width, 1);
        Ink.TextAt(g, $"{deck.Rooms.Count} модулей   /   {deck.Rooms.Sum(r => r.Type.Cabins)} кают", 17, barY + 9, 12, Ink.Muted, false, true);
        string help = notice != null && time < noticeUntil ? notice : Map.Width < 760
            ? "ЛКМ  выбор / ход    ·    Колесо  масштаб    ·    ПКМ  сдвиг"
            : "ЛКМ  выбор / приказ     ·     Колесо  масштаб     ·     ПКМ + мышь  сдвиг";
        Ink.TextAt(g, help, sidebar + 14, barY + 8, 13, notice != null && time < noticeUntil ? Ink.Gold : Ink.Muted);
        Ink.TextAt(g, $"{painter.Zoom * 100:0}%", Width - 51, barY + 8, 12, Ink.Cyan, false, true);
    }

    private static void Caption(Graphics g, string text, int x, int y) => Ink.TextAt(g, text, x, y, 10, Ink.Muted, true, true);
    private void Button(Graphics g, Rectangle r, string text, Action action)
    {
        Ink.Fill(g, Ink.Rule, r.X, r.Y, r.Width, r.Height);
        Ink.Fill(g, r.Contains(mouse) ? Color.FromArgb(41, 62, 66) : Color.FromArgb(25, 40, 45), r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2);
        Ink.TextAt(g, text, r.X + 9, r.Y + 4, 13, Ink.Text);
        buttons.Add((r, action));
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus(); mouse = e.Location;
        if (e.Button is MouseButtons.Right or MouseButtons.Middle && Map.Contains(mouse))
        {
            drag = mouse; dragging = true; Capture = true; Cursor = Cursors.SizeAll; return;
        }
        if (e.Button != MouseButtons.Left) return;
        foreach (var button in buttons)
            if (button.Bounds.Contains(mouse)) { button.Click(); return; }
        if (!Map.Contains(mouse)) return;
        painter.Fit(Map);
        if (painter.ActorAt(Session, mouse) is { } person) { Select(person); return; }
        if (painter.FixtureAt(mouse) is { } fixture)
        {
            notice = $"{fixture.Room.Label} · {fixture.Description}";
            noticeUntil = watch.Elapsed.TotalSeconds + 2.5;
            Invalidate();
            return;
        }
        Tile goal = painter.Pick(mouse);
        if (Session.Selected is not { } w || !Session.Deck.Floor.ContainsKey(goal)) return;
        bool moved = Session.Move(w, goal);
        notice = moved ? $"{w.Member.DisplayName}  →  {Session.Deck.Location(goal)}" : "Проход занят или цель недоступна";
        noticeUntil = watch.Elapsed.TotalSeconds + 2.5;
        Invalidate();
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e); mouse = e.Location;
        if (dragging)
        {
            painter.Pan = new(painter.Pan.X + mouse.X - drag.X, painter.Pan.Y + mouse.Y - drag.Y); drag = mouse;
        }
        else Cursor = buttons.Any(b => b.Bounds.Contains(mouse)) || (Map.Contains(mouse) && painter.ActorAt(Session, mouse) != null) ? Cursors.Hand : Cursors.Default;
        Invalidate();
    }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); mouse = new(-1, -1); Invalidate(); }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); dragging = false; Capture = false; Cursor = Cursors.Default; }
    protected override void OnMouseCaptureChanged(EventArgs e) { base.OnMouseCaptureChanged(e); if (!Capture) dragging = false; }
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (!Map.Contains(e.Location)) return;
        float old = painter.Zoom;
        painter.Zoom = Math.Clamp(old * MathF.Pow(1.16f, e.Delta / 120f), .65f, 8f);
        float ratio = painter.Zoom / old;
        var center = new PointF(Map.Left + Map.Width / 2f, Map.Top + Map.Height / 2f);
        painter.Pan = new((1 - ratio) * (e.X - center.X) + ratio * painter.Pan.X, (1 - ratio) * (e.Y - center.Y) + ratio * painter.Pan.Y);
        Invalidate();
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer.Dispose(); scenarioMenu.Dispose(); painter.Dispose();
            foreach (var image in art.Values.Concat(portraits.Values)) image.Dispose();
        }
        base.Dispose(disposing);
    }
}
