using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.TetrarchDeck;

internal sealed class Walker(ShipCrewMemberData member, Tile start, int index)
{
    public ShipCrewMemberData Member { get; } = member;
    public int Index { get; } = index;
    public string? PortraitId { get; set; }
    public int HeadColumn { get; set; }
    public bool Female { get; set; }
    public Tile Cell { get; private set; } = start;
    public Queue<Tile> Route { get; } = new();
    public float Heading { get; private set; }
    public float WalkWeight { get; private set; }
    public float Progress { get; private set; }
    public float Distance { get; private set; }
    private float speed;
    public Tile Next => Route.TryPeek(out var next) ? next : Cell;
    public PointF Position => new(Cell.X + .5f + (Next.X - Cell.X) * Progress,
        Cell.Y + .5f + (Next.Y - Cell.Y) * Progress);

    public void Advance(float seconds)
    {
        while (seconds > 0 && Route.Count > 0)
        {
            float dt = Math.Min(seconds, 1f / 60);
            seconds -= dt;
            const float acceleration = 7f;
            float target = Math.Min(1.46f, MathF.Sqrt(2 * acceleration * (Route.Count - Progress)));
            if (Route.Count > 1)
            {
                var next = Next;
                var ahead = Route.ElementAt(1);
                if (next.X - Cell.X != ahead.X - next.X || next.Y - Cell.Y != ahead.Y - next.Y)
                    target = Math.Min(target, MathF.Sqrt(.85f * .85f + 2 * acceleration * (1 - Progress)));
            }
            float direction = MathF.Atan2(Next.Y - Cell.Y, Next.X - Cell.X);
            float angle = MathF.IEEERemainder(direction - Heading, MathF.Tau);
            Heading += Math.Clamp(angle, -6f * dt, 6f * dt);
            target *= Math.Clamp((MathF.Cos(angle) + .35f) / 1.15f, .06f, 1f);
            float weight = Math.Min(Math.Min(1, speed / .82f), (Route.Count - Progress) / .36f);
            WalkWeight += Math.Clamp(weight - WalkWeight, -5f * dt, 5f * dt);
            speed += Math.Clamp(target - speed, -acceleration * dt, acceleration * dt);
            float remaining = speed * dt;
            while (remaining > 0 && Route.TryPeek(out var next))
            {
                float step = Math.Min(remaining, 1 - Progress);
                Distance += step;
                Progress += step;
                remaining -= step;
                if (Progress < 1) break;
                Progress = 0;
                Cell = Route.Dequeue();
            }
        }
        if (Route.Count == 0)
        {
            speed = 0;
            WalkWeight = Math.Max(0, WalkWeight - 5 * seconds);
        }
    }
}

internal sealed class CrewArt
{
    // The columns are registered to the game's portrait resources. The same
    // face can belong to two crew members or appear in several scenarios.
    private static readonly string[] Portraits =
    [
        "CHR-20260915-105947-6DC202", "CHR-20260915-104309-12BAB0", "CHR-20260915-102445-3C5C5D",
        "CHR-20260915-102448-E2757F", "CHR-20260915-104315-4DF131", "CHR-20260915-102447-5F3DEF"
    ];
    private readonly Dictionary<string, (string? Portrait, string? Gender)> defaults = [];

    public CrewArt(ScenarioChoice[] scenarios)
    {
        foreach (var s in scenarios)
            foreach (var m in s.Ship.Crew ?? [])
            {
                string? portrait = PortraitFor(m, s);
                var known = defaults.GetValueOrDefault(m.CrewId);
                defaults[m.CrewId] = (known.Portrait ?? portrait, known.Gender ?? m.Gender);
            }
    }

    private static string? PortraitFor(ShipCrewMemberData m, ScenarioChoice s) =>
        m.PortraitAssetId ?? (m.Role == "Captain" || m.DisplayName == s.Ship.CaptainDisplayName ? s.Ship.CaptainPortraitImage : null);

    public void Dress(DeckSession session)
    {
        foreach (var w in session.Crew)
        {
            var known = defaults.GetValueOrDefault(w.Member.CrewId);
            w.PortraitId = PortraitFor(w.Member, session.Deck.Scenario) ?? known.Portrait;
            w.Female = (w.Member.Gender ?? known.Gender) == "Female";
            int column = Array.IndexOf(Portraits, Path.GetFileNameWithoutExtension(w.PortraitId));
            w.HeadColumn = column >= 0 ? column : w.Female ? 1 : 0;
        }
    }

    internal static string PortraitForColumn(int column) => Portraits[column];
}

internal sealed class DeckSession
{
    public Deck Deck { get; }
    public List<Walker> Crew { get; } = [];
    public Walker? Selected { get; set; }

    public DeckSession(Deck deck, Random random)
    {
        Deck = deck;
        var roster = deck.Scenario.Ship.Crew ?? [];
        // Shuffle rooms first so the initial watch spreads throughout the ship.
        var rooms = deck.Floor.Where(p => p.Value != null && !deck.Equipment.Contains(p.Key)
                && p.Key.X - p.Value.Origin.X is > 0 and < Deck.Span - 1
                && p.Key.Y - p.Value.Origin.Y is > 0 and < Deck.Span - 1)
            .GroupBy(p => p.Value).Select(g => new Queue<Tile>(g.Select(p => p.Key).OrderBy(_ => random.Next())))
            .OrderBy(_ => random.Next()).ToList();
        foreach (var member in roster)
        {
            var room = rooms[0];
            rooms.RemoveAt(0);
            Crew.Add(new(member, room.Dequeue(), Crew.Count));
            if (room.Count > 0) rooms.Add(room);
        }
        Selected = Crew.FirstOrDefault();
    }

    public bool Move(Walker person, Tile goal)
    {
        if (!Deck.Floor.ContainsKey(goal) || Deck.Equipment.Contains(goal)) return false;
        var blocked = new HashSet<Tile>();
        foreach (var other in Crew.Where(c => c != person))
        {
            blocked.Add(other.Cell);
            blocked.UnionWith(other.Route);
        }
        if (blocked.Contains(goal)) return false;
        // Finish the current step before turning. An invalid order preserves the old route.
        bool inStep = person.Progress > 0;
        var start = inStep ? person.Next : person.Cell;
        var previous = new Dictionary<Tile, Tile> { [start] = start };
        var open = new Queue<Tile>();
        open.Enqueue(start);
        while (!previous.ContainsKey(goal) && open.TryDequeue(out var current))
            foreach (var next in Deck.Neighbours(current))
                if (!blocked.Contains(next) && previous.TryAdd(next, current)) open.Enqueue(next);
        if (!previous.ContainsKey(goal)) return false;
        var path = new Stack<Tile>();
        for (var at = goal; at != start; at = previous[at]) path.Push(at);
        person.Route.Clear();
        if (inStep) person.Route.Enqueue(start);
        foreach (var cell in path) person.Route.Enqueue(cell);
        return true;
    }
}
