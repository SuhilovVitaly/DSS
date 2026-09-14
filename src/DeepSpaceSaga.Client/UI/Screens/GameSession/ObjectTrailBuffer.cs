using System.Collections;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>Chronological ring buffer with lazily recomputed conservative drawing bounds.</summary>
internal sealed class ObjectTrailBuffer : IReadOnlyList<ObjectTrailPoint>
{
    private ObjectTrailPoint[] _points = new ObjectTrailPoint[256];
    private int _head;
    private bool _boundsDirty = true;
    private (double MinX, double MinY, double MaxX, double MaxY) _bounds;
    public int Count { get; private set; }
    internal int Capacity => _points.Length;

    public ObjectTrailPoint this[int index]
    {
        get => _points[PhysicalIndex(index)];
        set { _points[PhysicalIndex(index)] = value; _boundsDirty = true; }
    }

    private int PhysicalIndex(int index)
    {
        if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
        return (_head + index) % _points.Length;
    }

    public void Add(ObjectTrailPoint point)
    {
        if (Count == _points.Length)
        {
            var expanded = new ObjectTrailPoint[_points.Length * 2];
            for (int i = 0; i < Count; i++) expanded[i] = this[i];
            _points = expanded;
            _head = 0;
        }
        _points[(_head + Count++) % _points.Length] = point;
        _boundsDirty = true;
    }

    public void RemoveFirst(int count)
    {
        if ((uint)count > (uint)Count) throw new ArgumentOutOfRangeException(nameof(count));
        _head = (_head + count) % _points.Length;
        Count -= count;
        _boundsDirty = true;
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
