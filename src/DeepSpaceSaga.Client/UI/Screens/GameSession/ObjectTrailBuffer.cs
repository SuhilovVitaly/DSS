using System.Collections;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>Chronological ring buffer with lazily recomputed conservative drawing bounds.</summary>
internal sealed class ObjectTrailBuffer : IReadOnlyList<ObjectTrailPoint>
{
    internal const int MaximumPoints = 512;
    private const int PageSize = 16;
    private ObjectTrailPoint[][] _pages = [new ObjectTrailPoint[PageSize]];
    private bool[] _shared = new bool[1];
    private bool _sharedRoot;
    private int _capacity = PageSize;
    private Frozen? _frozen;
    internal sealed class Frozen(ObjectTrailPoint[][] pages, int head, int count, int capacity, long revision) : IReadOnlyList<ObjectTrailPoint>
    {
        public int Count => count;
        internal int Capacity => capacity;
        internal long Revision => revision;
        public ObjectTrailPoint this[int index]
        {
            get
            {
                if ((uint)index >= (uint)count) throw new ArgumentOutOfRangeException(nameof(index));
                int physical = (head + index) % capacity;
                return pages[physical / PageSize][physical % PageSize];
            }
        }
        public IEnumerator<ObjectTrailPoint> GetEnumerator() { for (int i = 0; i < count; i++) yield return this[i]; }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    internal Frozen Freeze()
    {
        if (_frozen?.Revision == Revision) return _frozen;
        _sharedRoot = true;
        Array.Fill(_shared, true);
        return _frozen = new(_pages, _head, Count, _capacity, Revision);
    }
    private void Write(int physical, ObjectTrailPoint value)
    {
        if (_sharedRoot) { _pages = (ObjectTrailPoint[][])_pages.Clone(); _sharedRoot = false; }
        int page = physical / PageSize;
        if (_shared[page]) { _pages[page] = (ObjectTrailPoint[])_pages[page].Clone(); _shared[page] = false; }
        _pages[page][physical % PageSize] = value;
    }
    private ObjectTrailPoint Read(int physical) => _pages[physical / PageSize][physical % PageSize];
    private int _head;
    private bool _boundsDirty = true;
    private (double MinX, double MinY, double MaxX, double MaxY) _bounds;
    public int Count { get; private set; }
    internal int Capacity => _capacity;
    internal long Revision { get; private set; }

    public ObjectTrailPoint this[int index]
    {
        get => Read(PhysicalIndex(index));
        set { Write(PhysicalIndex(index), value); _boundsDirty = true; Revision++; }
    }

    private int PhysicalIndex(int index)
    {
        if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
        return (_head + index) % _capacity;
    }

    public void Add(ObjectTrailPoint point)
    {
        if (Count == MaximumPoints) RemoveFirst(1);
        if (Count == _capacity)
        {
            var old = this.ToArray();
            _capacity *= 2;
            _pages = Enumerable.Range(0, _capacity / PageSize).Select(_ => new ObjectTrailPoint[PageSize]).ToArray();
            _shared = new bool[_pages.Length];
            _sharedRoot = false;
            _head = 0;
            for (int i = 0; i < old.Length; i++) Write(i, old[i]);
        }
        Write((_head + Count++) % _capacity, point);
        _boundsDirty = true;
        Revision++;
    }

    public void RemoveFirst(int count)
    {
        if ((uint)count > (uint)Count) throw new ArgumentOutOfRangeException(nameof(count));
        _head = (_head + count) % _capacity;
        Count -= count;
        _boundsDirty = true;
        Revision++;
    }

    public (double MinX, double MinY, double MaxX, double MaxY) Bounds
    {
        get
        {
            if (!_boundsDirty) return _bounds;
            double minX = double.PositiveInfinity, minY = double.PositiveInfinity;
            double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity;
            for (int i = 0; i < Count; i++)
            {
                var point = this[i];
                minX = Math.Min(minX, point.X); minY = Math.Min(minY, point.Y);
                maxX = Math.Max(maxX, point.X); maxY = Math.Max(maxY, point.Y);
            }
            _bounds = (minX, minY, maxX, maxY);
            _boundsDirty = false;
            return _bounds;
        }
    }

    public IEnumerator<ObjectTrailPoint> GetEnumerator()
    {
        for (int i = 0; i < Count; i++) yield return this[i];
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
