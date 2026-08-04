using System.Diagnostics;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

internal readonly record struct ObjectTrailPoint(
    double X,
    double Y,
    long Timestamp);

internal sealed class ObjectTrailStore
{
    internal const int TrailHistorySeconds = 10;
    internal const int TrailSampleIntervalMs = 50;
    private const double TrailMinSampleDistanceWorldUnits = 1.0;

    private const long HistoryGameTimeMs = TrailHistorySeconds * 1000L;
    private const long SampleIntervalGameTimeMs = TrailSampleIntervalMs;
    private static readonly long SampleIntervalTicks = MillisecondsToTicks(TrailSampleIntervalMs);

    private readonly Dictionary<string, List<ObjectTrailPoint>> _trails = new();
    private readonly Dictionary<string, long> _lastSampleTimestamps = new();
    private readonly HashSet<string> _currentObjectIds = new(StringComparer.Ordinal);
    private readonly List<string> _objectIdsToRemove = new();
    private readonly IMotionPredictor _predictor;
    private readonly Func<long> _timestampProvider;

    internal ObjectTrailStore()
        : this(new LinearMotionPredictor(), Stopwatch.GetTimestamp)
    {
    }

    internal ObjectTrailStore(Func<long> timestampProvider)
        : this(new LinearMotionPredictor(), timestampProvider)
    {
    }

    internal ObjectTrailStore(IMotionPredictor predictor, Func<long> timestampProvider)
    {
        _predictor = predictor;
        _timestampProvider = timestampProvider;
    }

    internal IEnumerable<IReadOnlyList<ObjectTrailPoint>> Trails => _trails.Values;

    internal void Update(
        IReadOnlyList<ObjectRenderState> renderStates,
        SimulationSpeed currentSpeed,
        long currentGameTimeMs,
        bool bootstrapMissingTrails = false)
    {
        PruneMissingObjects(renderStates);

        long rawNow = _timestampProvider();
        foreach (var state in renderStates)
        {
            var obj = state.Predicted;
            if (obj.SpeedKmS <= 0)
            {
                RemoveObjectTrail(obj.ObjectId);
                continue;
            }

            if (!_trails.TryGetValue(obj.ObjectId, out var points))
            {
                points = new List<ObjectTrailPoint>();
                _trails[obj.ObjectId] = points;
                if (bootstrapMissingTrails)
                    BootstrapTrail(points, obj, currentGameTimeMs, rawNow);
                else
                    AddCurrentPoint(points, obj, currentGameTimeMs, rawNow);

                continue;
            }

            if (currentSpeed == SimulationSpeed.Speed0)
                continue;

            long visualGameTimeMs = GetMonotonicGameTimeMs(points, currentGameTimeMs);
            PruneOldPoints(points, visualGameTimeMs);

            if (ShouldAddPoint(obj.ObjectId, points, obj.X, obj.Y, visualGameTimeMs, rawNow))
            {
                points.Add(new ObjectTrailPoint(obj.X, obj.Y, visualGameTimeMs));
                _lastSampleTimestamps[obj.ObjectId] = rawNow;
            }
        }
    }

    internal IReadOnlyList<ObjectTrailPoint> GetTrail(string objectId)
    {
        return _trails.TryGetValue(objectId, out var points)
            ? points
            : Array.Empty<ObjectTrailPoint>();
    }

    private void PruneMissingObjects(IReadOnlyList<ObjectRenderState> renderStates)
    {
        _currentObjectIds.Clear();
        for (int i = 0; i < renderStates.Count; i++)
            _currentObjectIds.Add(renderStates[i].Predicted.ObjectId);

        _objectIdsToRemove.Clear();
        foreach (string objectId in _trails.Keys)
        {
            if (!_currentObjectIds.Contains(objectId))
                _objectIdsToRemove.Add(objectId);
        }

        for (int i = 0; i < _objectIdsToRemove.Count; i++)
            RemoveObjectTrail(_objectIdsToRemove[i]);
    }

    private void RemoveObjectTrail(string objectId)
    {
        _trails.Remove(objectId);
        _lastSampleTimestamps.Remove(objectId);
    }

    private static void PruneOldPoints(List<ObjectTrailPoint> points, long currentGameTimeMs)
    {
        long cutoff = currentGameTimeMs - HistoryGameTimeMs;
        int removeCount = 0;

        while (removeCount < points.Count && points[removeCount].Timestamp < cutoff)
            removeCount++;

        if (removeCount > 0)
            points.RemoveRange(0, removeCount);
    }

    private void BootstrapTrail(
        List<ObjectTrailPoint> points,
        ObjectMotionSnapshot obj,
        long currentGameTimeMs,
        long rawNow)
    {
        long oldestGameTimeMs = currentGameTimeMs - HistoryGameTimeMs;

        for (long sampleGameTimeMs = oldestGameTimeMs;
             sampleGameTimeMs <= currentGameTimeMs;
             sampleGameTimeMs += SampleIntervalGameTimeMs)
        {
            long ageMs = currentGameTimeMs - sampleGameTimeMs;
            var projected = _predictor.Predict(obj, -ageMs);
            points.Add(new ObjectTrailPoint(projected.X, projected.Y, sampleGameTimeMs));
        }

        if (points[^1].Timestamp != currentGameTimeMs)
            AddCurrentPoint(points, obj, currentGameTimeMs, rawNow);
        else
            _lastSampleTimestamps[obj.ObjectId] = rawNow;
    }

    private void AddCurrentPoint(
        List<ObjectTrailPoint> points,
        ObjectMotionSnapshot obj,
        long currentGameTimeMs,
        long rawNow)
    {
        points.Add(new ObjectTrailPoint(obj.X, obj.Y, currentGameTimeMs));
        _lastSampleTimestamps[obj.ObjectId] = rawNow;
    }

    private static long GetMonotonicGameTimeMs(List<ObjectTrailPoint> points, long currentGameTimeMs)
    {
        return points.Count == 0
            ? currentGameTimeMs
            : Math.Max(currentGameTimeMs, points[^1].Timestamp);
    }

    private bool ShouldAddPoint(
        string objectId,
        List<ObjectTrailPoint> points,
        double x,
        double y,
        long currentGameTimeMs,
        long rawNow)
    {
        if (points.Count == 0)
            return true;

        var last = points[^1];
        if (last.Timestamp == currentGameTimeMs)
            return false;

        if (!_lastSampleTimestamps.TryGetValue(objectId, out long lastSampleTimestamp) ||
            rawNow - lastSampleTimestamp >= SampleIntervalTicks)
            return true;

        double dx = x - last.X;
        double dy = y - last.Y;
        return dx * dx + dy * dy >= TrailMinSampleDistanceWorldUnits * TrailMinSampleDistanceWorldUnits;
    }

    private static long MillisecondsToTicks(int milliseconds)
    {
        return (long)(milliseconds * Stopwatch.Frequency / 1000.0);
    }
}
