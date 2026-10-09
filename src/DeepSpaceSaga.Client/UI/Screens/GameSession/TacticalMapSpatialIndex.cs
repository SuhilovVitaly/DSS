namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

internal readonly record struct TacticalMapBounds(double Left, double Top, double Right, double Bottom)
{
    internal bool Intersects(TacticalMapBounds other) => Right >= other.Left && Left <= other.Right && Bottom >= other.Top && Top <= other.Bottom;
}

/// <summary>Bounded session-local uniform grid; large queries and overflow retain a complete fallback.</summary>
internal sealed class TacticalMapSpatialIndex
{
    private const double CellSize = 1024;
    private readonly int _capacity;
    private readonly Dictionary<string, TacticalMapBounds> _entries = new(StringComparer.Ordinal);
    private readonly Dictionary<(double X, double Y), HashSet<string>> _cells = new();
    private readonly HashSet<string> _large = new(StringComparer.Ordinal);
    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
    private readonly List<string> _removed = new();
    internal int Count => _entries.Count;
    internal long BoundsUpdates { get; private set; }
    internal bool Overflow { get; private set; }
    internal TacticalMapSpatialIndex(int capacity = 10000) => _capacity = Math.Max(1, capacity);

    private static bool Small(TacticalMapBounds bounds) =>
        double.IsFinite(bounds.Left) && double.IsFinite(bounds.Right) && double.IsFinite(bounds.Top) && double.IsFinite(bounds.Bottom) &&
        (Math.Floor(bounds.Right / CellSize) - Math.Floor(bounds.Left / CellSize) + 1) *
        (Math.Floor(bounds.Bottom / CellSize) - Math.Floor(bounds.Top / CellSize) + 1) <= 64;

    private void VisitCells(TacticalMapBounds bounds, Action<(double X, double Y)> visit)
    {
        double left = Math.Floor(bounds.Left / CellSize), top = Math.Floor(bounds.Top / CellSize);
        int width = (int)(Math.Floor(bounds.Right / CellSize) - left);
        int height = (int)(Math.Floor(bounds.Bottom / CellSize) - top);
        for (int x = 0; x <= width; x++) for (int y = 0; y <= height; y++) visit((left + x, top + y));
    }

    private void Remove(string id, TacticalMapBounds bounds)
    {
        if (!_large.Remove(id)) VisitCells(bounds, cell =>
        {
            if (!_cells.TryGetValue(cell, out var ids)) return;
            ids.Remove(id);
            if (ids.Count == 0) _cells.Remove(cell);
        });
        _entries.Remove(id);
    }

    internal void Clear() { _entries.Clear(); _cells.Clear(); _large.Clear(); _seen.Clear(); _removed.Clear(); Overflow = false; }

    internal void BeginUpdate() { _seen.Clear(); Overflow = false; }
    internal void Include(string id, TacticalMapBounds bounds)
    {
        if (_entries.TryGetValue(id, out var previous))
        {
            _seen.Add(id);
            if (previous == bounds) return;
            Remove(id, previous);
        }
        if (_entries.Count >= _capacity) { Overflow = true; return; }
        _seen.Add(id);
        _entries[id] = bounds;
        BoundsUpdates++;
        if (!Small(bounds)) { _large.Add(id); return; }
        VisitCells(bounds, cell =>
        {
            if (!_cells.TryGetValue(cell, out var ids)) _cells[cell] = ids = new(StringComparer.Ordinal);
            ids.Add(id);
        });
    }
    internal void EndUpdate()
    {
        _removed.Clear();
        foreach (string id in _entries.Keys) if (!_seen.Contains(id)) _removed.Add(id);
        foreach (string id in _removed) Remove(id, _entries[id]);
    }
    internal bool Query(TacticalMapBounds bounds, HashSet<string> result)
    {
        result.Clear();
        if (Overflow) return false; // Caller scans authoritative frame; no contact is dropped.
        if (!Small(bounds))
        {
            foreach (var (id, entry) in _entries) if (entry.Intersects(bounds)) result.Add(id);
        }
        else
        {
            VisitCells(bounds, cell =>
            {
                if (_cells.TryGetValue(cell, out var ids))
                    foreach (string id in ids) if (_entries[id].Intersects(bounds)) result.Add(id);
            });
            foreach (string id in _large) if (_entries[id].Intersects(bounds)) result.Add(id);
        }
        return true;
    }
}
