using System.Text.Json;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.TetrarchDeck;

internal sealed record ScenarioChoice(string Name, SpaceObjectData Ship)
{
    public override string ToString() => Name;
}

internal sealed record ModuleType(string Kind, string Name, int Cabins);
internal sealed record Room(ShipModuleData Module, ModuleType Type, string Label, Tile Origin);
internal readonly record struct Tile(int X, int Y)
{
    public Tile Offset(int x, int y) => new(X + x, Y + y);
}

internal sealed record Fixture(string Art, Rectangle Footprint, int Width, Room Room, string Description);

internal static class Content
{
    public static ScenarioChoice[] Scenarios(string root)
    {
        var choices = new List<ScenarioChoice>();
        foreach (string path in Directory.GetFiles(Path.Combine(root, "Scenarios"), "scenario.json", SearchOption.AllDirectories))
        {
            var state = ScenarioLoader.LoadFromFile(path, contentRoot: root).GameState;
            var ship = state.SpaceObjects?.SingleOrDefault(s => s.ObjectId == state.PlayerShipObjectId);
            if (ship?.ShipClassId == "ship.tetrarch" && ship.HullLayout != null)
                choices.Add(new(Directory.GetParent(path)!.Name, ship));
        }
        return choices.OrderBy(s => s.Name != "Demo").ThenBy(s => s.Name).ToArray();
    }

    public static Dictionary<string, ModuleType> ModuleTypes(string root)
    {
        var result = new Dictionary<string, ModuleType>();
        foreach (var file in Directory.GetFiles(Path.Combine(root, "Data", "Modules"), "*.json", SearchOption.AllDirectories))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            foreach (var m in doc.RootElement.GetProperty("moduleImplementations").EnumerateArray())
            {
                var kind = m.GetProperty("type").GetString()!;
                result.Add(m.GetProperty("typeId").GetString()!, new(kind, kind switch
                {
                    "module.bridge.navigation.computer" => "Мостик",
                    "living.quarters" => "Жилой блок",
                    "module.engine" => "Машинное отделение",
                    "module.generator" => "Генераторная",
                    "module.container" => "Грузовой отсек",
                    "module.sensor" => "Сенсорный пост",
                    "module.scanner" => "Сканерная",
                    "module.drone-bay" => "Ангар",
                    _ => m.GetProperty("displayName").GetString()!
                }, m.TryGetProperty("cabines", out var cabins) ? cabins.GetInt32() : 0));
            }
        }
        return result;
    }
}

internal sealed class Deck
{
    public const int Span = 12;
    // The drawing uses a 2:1 floor grid and measures height in the same world units.
    public const int HumanHeight = 2, DroneLength = 5 * HumanHeight, DroneHeight = 3 * HumanHeight / 2, DroneBeam = 2;
    public readonly Dictionary<Tile, Room?> Floor = [];
    public readonly HashSet<Tile> Hull = [];
    public readonly HashSet<Tile> Equipment = [];
    public readonly List<Fixture> Fixtures = [];
    public readonly List<Room> Rooms = [];
    private readonly Dictionary<Tile, int> cabin = [];
    public static readonly Tile[] Directions = [new(1, 0), new(0, 1), new(-1, 0), new(0, -1)];
    public ScenarioChoice Scenario { get; }

    public Deck(ScenarioChoice scenario, Dictionary<string, ModuleType> types)
    {
        Scenario = scenario;
        var hull = scenario.Ship.HullLayout!;
        var modules = scenario.Ship.Modules ?? [];
        // An unoccupied structural band needs only a passage. A two-cell hangar shares
        // its twelve interior tiles between its structural columns.
        int[] Axis(int count, Func<HullCellCoordinate, int> coordinate)
        {
            var widths = Enumerable.Repeat(3, count).ToArray();
            foreach (var module in modules)
            {
                var indices = module.OccupiedCells.Select(coordinate).Distinct().ToArray();
                foreach (int i in indices) widths[i] = Math.Max(widths[i], (Span + indices.Length - 1) / indices.Length);
            }
            var edges = new int[count + 1];
            for (int i = 0; i < count; i++) edges[i + 1] = edges[i] + widths[i];
            return edges;
        }
        int[] xs = Axis(hull.Width, c => c.X), ys = Axis(hull.Height, c => c.Y);
        var nodes = new Dictionary<Tile, (Tile Center, Room? Room)>();
        foreach (var c in hull.Cells)
        {
            for (int y = ys[c.Y]; y < ys[c.Y + 1]; y++)
                for (int x = xs[c.X]; x < xs[c.X + 1]; x++) Hull.Add(new(x, y));
            nodes[new(c.X, c.Y)] = (new((xs[c.X] + xs[c.X + 1]) / 2, (ys[c.Y] + ys[c.Y + 1]) / 2), null);
        }
        foreach (var m in modules)
        {
            var type = types[m.ModuleTypeId];
            int number = Rooms.Count(r => r.Type.Kind == type.Kind) + 1;
            string label = modules.Count(s => types[s.ModuleTypeId].Kind == type.Kind) > 1 ? $"{type.Name} {number}" : type.Name;
            var cells = m.OccupiedCells;
            var center = new Tile((xs[cells.Min(c => c.X)] + xs[cells.Max(c => c.X) + 1]) / 2,
                (ys[cells.Min(c => c.Y)] + ys[cells.Max(c => c.Y) + 1]) / 2);
            var room = new Room(m, type, label, center.Offset(-Span / 2, -Span / 2));
            Rooms.Add(room);
            foreach (var c in cells) nodes[new(c.X, c.Y)] = (center, room);
            for (int y = 0; y < Span; y++)
                for (int x = 0; x < Span; x++) Floor.Add(room.Origin.Offset(x, y), room);
        }

        void Strip(Tile a, Tile b, bool horizontal)
        {
            for (int y = Math.Min(a.Y, b.Y); y <= Math.Max(a.Y, b.Y); y++)
                for (int x = Math.Min(a.X, b.X); x <= Math.Max(a.X, b.X); x++)
                {
                    AddPassage(new(x, y));
                    AddPassage(horizontal ? new(x, y - 1) : new(x - 1, y));
                }
        }
        void AddPassage(Tile t) { if (Hull.Contains(t)) Floor.TryAdd(t, null); }
        foreach (var (cell, a) in nodes)
        {
            foreach (var dir in Directions.Take(2))
            {
                if (!nodes.TryGetValue(cell.Offset(dir.X, dir.Y), out var b) || a.Center == b.Center) continue;
                bool horizontal = dir.X != 0;
                // Make the turn in the connecting passage, leaving the door at the room's midpoint.
                int seam = horizontal ? xs[cell.X + 1] : ys[cell.Y + 1];
                if (b.Room != null && a.Room == null) seam--;
                var first = horizontal ? new Tile(seam, a.Center.Y) : new Tile(a.Center.X, seam);
                var second = horizontal ? new Tile(seam, b.Center.Y) : new Tile(b.Center.X, seam);
                Strip(a.Center, first, horizontal);
                Strip(first, second, !horizontal);
                Strip(second, b.Center, horizontal);
            }
        }
        foreach (var room in Rooms) Furnish(room);
    }

    private void Furnish(Room room)
    {
        void Place(string art, int x, int y, int width, int depth, int pixels, string caption)
        {
            var origin = room.Origin.Offset(x, y);
            var footprint = new Rectangle(origin.X, origin.Y, width, depth);
            Fixtures.Add(new(art, footprint, pixels, room, caption));
            for (int v = 0; v < depth; v++)
                for (int u = 0; u < width; u++) Equipment.Add(origin.Offset(u, v));
        }
        if (room.Type.Cabins > 0)
        {
            for (int i = 0; i < room.Type.Cabins; i++)
            {
                int x = i % 2 * 7, y = i / 2 * 7;
                for (int v = 0; v < 5; v++)
                    for (int u = 0; u < 5; u++) cabin[room.Origin.Offset(x + u, y + v)] = i + 1;
                Place("bunk", x, y, 2, 4, 78, $"Койка · каюта {i + 1}");
                Place("desk", x + 3, y, 2, 2, 45, $"Стол · каюта {i + 1}");
            }
            return;
        }
        if (room.Type.Kind == "module.drone-bay")
        {
            var wing = Scenario.Ship.CarrierState?.OwnedDrones ?? [];
            foreach (var d in wing.Where(d => d.HangarModuleId == room.Module.ModuleId && !d.Destroyed))
                Place("drone-utility", 1, d.SlotIndex == 0 ? 1 : 8, DroneLength, DroneBeam, 0, $"{d.DroneId} · {d.FrameId}");
            Place("bench", 1, 4, 3, 2, 65, "Пост обслуживания");
            return;
        }

        switch (room.Type.Kind)
        {
            case "module.bridge.navigation.computer":
                Place("bridge", 1, 1, 4, 3, 103, "Навигационный пост");
                Place("bridge", 7, 1, 4, 3, 94, "Пульт управления");
                Place("scanner", 2, 7, 3, 3, 80, "Тактический стол");
                Place("desk", 8, 8, 3, 2, 51, "Пост связи");
                break;
            case "module.container":
                Place("cargo", 1, 1, 4, 4, 102, "Грузовые кассеты");
                Place("cargo", 7, 1, 4, 3, 87, "Грузовой стеллаж");
                Place("cargo", 1, 7, 4, 4, 90, "Контейнеры");
                Place("bench", 8, 8, 3, 2, 72, "Приёмка груза");
                break;
            case "module.engine":
            case "module.generator":
                bool engine = room.Type.Kind == "module.engine";
                Place(engine ? "engine" : "generator", 1, 1, 4, 9, 141, room.Label);
                Place("bench", 7, 1, 4, 3, 78, "Стойка обслуживания");
                Place(engine ? "generator" : "engine", 7, 7, 4, 4, 91, engine ? "Топливная магистраль" : "Контур охлаждения");
                break;
            default:
                string instrument = room.Type.Kind switch { "module.sensor" => "sensor", "module.scanner" => "scanner", _ => "bench" };
                Place(instrument, 1, 1, 4, 4, 95, room.Label);
                Place("bridge", 7, 1, 4, 3, 90, "Пульт оператора");
                Place("bench", 1, 8, 4, 3, 82, "Аппаратная стойка");
                Place("cargo", 8, 8, 3, 3, 69, "Запасные блоки");
                break;
        }
    }

    public string Location(Tile tile) => cabin.TryGetValue(tile, out int number)
        ? $"{Floor[tile]!.Label} · каюта {number}"
        : Floor.GetValueOrDefault(tile)?.Label ?? "Переход";

    public bool OpenEdge(Tile a, Tile b)
    {
        if (!Floor.TryGetValue(a, out var from) || !Floor.TryGetValue(b, out var to)) return false;
        int Along(Room room) => a.X == b.X ? a.X - room.Origin.X : a.Y - room.Origin.Y;
        bool Door(Room? room) => room == null || Along(room) is 5 or 6;
        if (from != to) return Door(from) && Door(to);
        if (cabin.GetValueOrDefault(a) != cabin.GetValueOrDefault(b)) return Along(from!) is 2 or 9;
        return true;
    }

    public IEnumerable<Tile> Neighbours(Tile tile)
    {
        foreach (var d in Directions)
        {
            var next = tile.Offset(d.X, d.Y);
            if (OpenEdge(tile, next) && !Equipment.Contains(next)) yield return next;
        }
    }
}
