using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace DeepSpaceSaga.TetrarchDeck;

internal static class DeckChecks
{
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    private static void Click(Control control, PointF pixel)
    {
        var point = Point.Round(pixel);
        var position = (IntPtr)((point.Y << 16) | (point.X & 0xffff));
        SendMessage(control.Handle, 0x0201, (IntPtr)1, position);
        SendMessage(control.Handle, 0x0202, IntPtr.Zero, position);
    }

    // Run without showing a window. Exercise every installed scenario and the real paint path.
    public static void Run(string root, ScenarioChoice[] scenarios, Dictionary<string, ModuleType> types, string output, string? frameDirectory)
    {
        static void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }
        foreach (var scenario in scenarios)
        {
            var deck = new Deck(scenario, types);
            Check(deck.Floor.Keys.All(deck.Hull.Contains), scenario.Name + " hull");
            Check(deck.Rooms.Count == scenario.Ship.Modules!.Count, scenario.Name + " modules");
            foreach (var room in deck.Rooms)
            {
                var tiles = deck.Floor.Where(p => p.Value == room).Select(p => p.Key).ToArray();
                Check(tiles.Length == 144 && tiles.Max(t => t.X) - tiles.Min(t => t.X) == 11 &&
                    tiles.Max(t => t.Y) - tiles.Min(t => t.Y) == 11, scenario.Name + " 12×12 " + room.Label);
            }
            foreach (var fixture in deck.Fixtures)
                for (int y = fixture.Footprint.Top; y < fixture.Footprint.Bottom; y++)
                    for (int x = fixture.Footprint.Left; x < fixture.Footprint.Right; x++)
                        Check(deck.Equipment.Contains(new(x, y)) && deck.Floor[new(x, y)] == fixture.Room, "Fixture footprint");
            Check(deck.Fixtures.Count(f => f.Art == "bunk") == deck.Rooms.Sum(r => r.Type.Cabins), scenario.Name + " cabins");
            var drones = deck.Fixtures.Where(f => f.Art == "drone-utility").ToArray();
            Check(drones.Length == (scenario.Ship.CarrierState?.OwnedDrones?.Count(d => !d.Destroyed) ?? 0), scenario.Name + " drones");
            Check(drones.All(d => d.Footprint.Width == 5 * Deck.HumanHeight && d.Footprint.Height == Deck.DroneBeam)
                && Deck.DroneHeight == 1.5 * Deck.HumanHeight, "Drone dimensions");
            var reachable = new HashSet<Tile>();
            var queue = new Queue<Tile>();
            var start = deck.Floor.Keys.First(t => !deck.Equipment.Contains(t));
            reachable.Add(start); queue.Enqueue(start);
            while (queue.TryDequeue(out var at))
                foreach (var next in deck.Neighbours(at))
                    if (reachable.Add(next)) queue.Enqueue(next);
            Check(reachable.Count == deck.Floor.Count - deck.Equipment.Count, scenario.Name + " isolated floor " +
                string.Join(", ", deck.Floor.Keys.Where(t => !deck.Equipment.Contains(t) && !reachable.Contains(t)).Take(12)));
            for (int seed = 0; seed < 8; seed++)
            {
                var s = new DeckSession(deck, new Random(seed));
                Check(s.Crew.Count == scenario.Ship.Crew!.Count && s.Crew.All(w => deck.Floor[w.Cell] != null && reachable.Contains(w.Cell))
                    && s.Crew.Select(w => w.Cell).Distinct().Count() == s.Crew.Count, scenario.Name + " spawn");
                var w = s.Crew[0];
                var goal = reachable.Where(t => t != w.Cell).OrderByDescending(t => Math.Abs(t.X - w.Cell.X) + Math.Abs(t.Y - w.Cell.Y)).First(t => s.Move(w, t));
                w.Advance(.14f);
                var pos = w.Position;
                Check(s.Move(w, goal) && w.Position == pos, scenario.Name + " continuous retarget");
                var next = w.Next;
                Check(!s.Move(w, new(-1, -1)) && !s.Move(w, deck.Equipment.First()) && w.Next == next, scenario.Name + " invalid target");
                if (s.Crew.Count > 1) Check(!s.Move(s.Crew[1], goal), scenario.Name + " destination conflict");
                var intermediate = w.Route.ElementAt(w.Route.Count / 2);
                Check(s.Move(w, intermediate) && w.Position == pos && w.Route.Last() == intermediate, scenario.Name + " new destination");
                Check(s.Move(w, goal), scenario.Name + " restore distant destination");
                w.Advance(600);
                Check(w.Cell == goal && w.Route.Count == 0 && w.Progress == 0, scenario.Name + " arrival");
            }
            Console.WriteLine($"{scenario.Name}: {deck.Rooms.Count} modules, {scenario.Ship.Crew!.Count} crew, {reachable.Count} reachable tiles.");
        }
        using var host = new Form();
        using var view = new DeckView(root, scenarios, types, new Random(13)) { Size = new(1440, 850) };
        host.Controls.Add(view);
        using var frame = new Bitmap(view.Width, view.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(frame);
        view.Camera.Labels = false;
        view.PaintDeck(g, 1.25);
        var session = view.Session;
        foreach (var area in session.Deck.Floor.GroupBy(p => p.Value))
        {
            bool PlainInterior(Tile tile)
            {
                foreach (float x in new[] { .27f, .5f, .73f })
                    foreach (float y in new[] { .27f, .5f, .73f })
                    {
                        var point = Point.Round(view.Camera.Screen(DeckPainter.Iso(tile.X + x, tile.Y + y)));
                        var color = frame.GetPixel(point.X, point.Y);
                        if (color.R != 79 || color.G != 86 || color.B != 88) return false;
                    }
                return true;
            }
            Check(area.Any(p => PlainInterior(p.Key)), "Uniform floor in " + (area.Key?.Label ?? "corridor"));
        }
        view.Camera.Labels = true;
        CheckStandingAnimation(session);
        Check(session.Crew.Where(w => w.PortraitId != null).All(w => CrewArt.PortraitForColumn(w.HeadColumn) == Path.GetFileNameWithoutExtension(w.PortraitId)), "Portrait-specific crew sprites");
        var actor = session.Crew.First(w => w != session.Selected && session.Deck.Floor[w.Cell]?.Type.Kind != "module.bridge.navigation.computer");
        var hit = view.Camera.Screen(actor);
        Click(view, new PointF(hit.X, hit.Y - 8));
        Check(session.Selected == actor, "Native mouse selection");
        var bridgeRoom = session.Deck.Rooms.First(r => r.Type.Kind == "module.bridge.navigation.computer");
        var bridge = session.Deck.Floor.Keys.Where(t => session.Deck.Floor[t] == bridgeRoom && !session.Deck.Equipment.Contains(t)
            && view.Camera.ActorAt(session, view.Camera.Screen(t)) == null && view.Camera.FixtureAt(view.Camera.Screen(t)) == null)
            .OrderBy(t => Math.Abs(t.X - bridgeRoom.Origin.X - 6) + Math.Abs(t.Y - bridgeRoom.Origin.Y - 6)).First();
        Click(view, view.Camera.Screen(bridge));
        Check(actor.Route.Count > 0 && actor.Route.Last() == bridge, "Native mouse order");
        actor.Advance(1.7f);
        view.PaintDeck(g, 1.75);
        frame.Save(output, ImageFormat.Png);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        const int samples = 48;
        for (int i = 0; i < samples; i++)
        {
            actor.Advance(1f / 60);
            view.PaintDeck(g, 1.75 + i / 60d);
        }
        Console.WriteLine($"1440×850 offscreen paint: {clock.Elapsed.TotalMilliseconds / samples:0.0} ms/frame.");
        actor.Advance(600);
        Check(actor.Cell == bridge, "Native mouse order arrival");
        var resident = session.Crew.First(w => session.Deck.Floor[w.Cell]?.Type.Cabins > 0);
        // Native roster click must zoom to the selected person even when their room is offscreen.
        Click(view, new PointF(92, 143 + resident.Index * 62 + 20));
        view.PaintDeck(g, 3);
        Check(session.Selected == resident && view.Camera.Zoom >= 2 && view.ClientRectangle.Contains(Point.Round(view.Camera.Screen(resident))), "Roster focus");
        frame.Save(Path.Combine(Path.GetDirectoryName(output)!, Path.GetFileNameWithoutExtension(output) + "-cabins.png"), ImageFormat.Png);
        if (frameDirectory != null)
        {
            Directory.CreateDirectory(frameDirectory);
            var quarters = session.Deck.Floor[resident.Cell];
            Tile destination = resident.Cell;
            float best = float.NegativeInfinity;
            // Prefer a route with a turn and a clear sightline to the boots and torso.
            // Furniture can hide a walkable tile in this cutaway projection.
            float unit = (view.Camera.Screen(resident.Cell.Offset(1, 0)).X - view.Camera.Screen(resident.Cell).X) / 16;
            foreach (var (tile, room) in session.Deck.Floor)
            {
                var pixel = view.Camera.Screen(tile);
                if (room != quarters || view.Camera.FixtureAt(pixel) != null || view.Camera.ActorAt(session, pixel) != null
                    || !session.Move(resident, tile) || resident.Route.Count is < 5 or > 9) continue;
                var route = resident.Route.Prepend(resident.Cell).ToArray();
                var steps = route.Zip(route.Skip(1), (from, to) => new Tile(to.X - from.X, to.Y - from.Y)).ToArray();
                int turns = steps.Zip(steps.Skip(1)).Count(pair => pair.First != pair.Second);
                if (turns == 0) continue;
                float score = 0;
                foreach (var step in route)
                {
                    var floor = view.Camera.Screen(step);
                    foreach (int height in new[] { 1, 15, 29 })
                        if (view.Camera.FixtureAt(new(floor.X, floor.Y - unit * height)) == null) score++;
                }
                score /= route.Length;
                if (steps[^1].X + steps[^1].Y > 0) score += .2f;
                score -= .04f * turns;
                if (score > best) { best = score; destination = tile; }
            }
            Check(float.IsFinite(best) && session.Move(resident, destination), "Record a route with a turn");
            view.Camera.Zoom = 4.6f;
            view.PaintDeck(g, 3);
            var projected = resident.Route.Prepend(resident.Cell).Select(t => view.Camera.Screen(t)).ToArray();
            var crop = new Rectangle(506, 230, 720, 440);
            view.Camera.Pan = new(view.Camera.Pan.X + crop.Left + crop.Width / 2 - (projected.Min(p => p.X) + projected.Max(p => p.X)) / 2,
                view.Camera.Pan.Y + crop.Top + crop.Height / 2 + 54 - (projected.Min(p => p.Y) + projected.Max(p => p.Y)) / 2);
            resident.Route.Clear();
            view.PaintDeck(g, 3);
            int idleFrames = 0;
            const int fps = 30, startFrame = 25;
            for (int i = 0; idleFrames < 3 * fps && i < 20 * fps; i++)
            {
                if (i == startFrame)
                {
                    Click(view, view.Camera.Screen(destination));
                    Check(resident.Route.Count > 0 && resident.Route.Last() == destination, "Recorded mouse command");
                }
                view.PaintDeck(g, 3 + i / (double)fps);
                using var detail = frame.Clone(crop, PixelFormat.Format24bppRgb);
                detail.Save(Path.Combine(frameDirectory, $"{i:D4}.png"), ImageFormat.Png);
                resident.Advance(1f / fps);
                if (i >= startFrame && resident.Route.Count == 0) idleFrames++;
            }
            Check(resident.Cell == destination, "Recorded arrival");
        }
        Console.WriteLine("Native mouse selection, move order and arrival passed.");
    }

    private static void CheckStandingAnimation(DeckSession session)
    {
        var art = Path.Combine(AppContext.BaseDirectory, "Art");
        using var model = new CrewFigure(Path.Combine(art, "crew-motion.bin"));
        using var heads = new Bitmap(Path.Combine(art, "heads.png"));
        using var first = new Bitmap(190, 208);
        using var second = new Bitmap(190, 208);
        void Draw(Bitmap image, Walker actor, double time)
        {
            using var g = Graphics.FromImage(image);
            g.Clear(Color.Transparent);
            g.ScaleTransform(4, 4);
            model.Draw(g, heads, actor, new(23, 44), time);
        }
        foreach (var actor in session.Crew)
        {
            Draw(first, actor, .13);
            Draw(second, actor, 1.19);
            int bottom = -1, upperChanges = 0;
            for (int y = 0; y < first.Height; y++)
                for (int x = 0; x < first.Width; x++)
                {
                    var a = first.GetPixel(x, y);
                    var b = second.GetPixel(x, y);
                    if (a.A > 96 || b.A > 96) bottom = y;
                    if (y < 118 && a.ToArgb() != b.ToArgb()) upperChanges++;
                }
            if (bottom < 0 || upperChanges < 35) throw new InvalidOperationException("Idle upper body did not move");
            for (int y = bottom - 5; y <= bottom; y++)
                for (int x = 0; x < first.Width; x++)
                    if (first.GetPixel(x, y).A != second.GetPixel(x, y).A)
                        throw new InvalidOperationException("Idle sole moved");
        }
        Console.WriteLine($"Idle sway and stationary soles verified for {session.Crew.Count} crew; uniform tiles sampled in all modules and corridors.");
    }
}
