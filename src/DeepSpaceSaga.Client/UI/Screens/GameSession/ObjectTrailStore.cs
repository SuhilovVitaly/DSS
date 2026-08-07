using System.Diagnostics;
using DeepSpaceSaga.Client.UI;
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
    private SimulationSpeed _previousSpeed = SimulationSpeed.Speed1;
    private ulong? _previousSnapshotSequence;

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
        bool bootstrapMissingTrails = false,
        IReadOnlySet<string>? bootstrapObjectIds = null,
        ulong? snapshotSequence = null)
    {
        PruneMissingObjects(renderStates);

        bool transitionToPause = currentSpeed == SimulationSpeed.Speed0 && _previousSpeed != SimulationSpeed.Speed0;
        bool pausedSnapshotChanged = currentSpeed == SimulationSpeed.Speed0 &&
                                     snapshotSequence.HasValue &&
                                     _previousSnapshotSequence.HasValue &&
                                     snapshotSequence.Value != _previousSnapshotSequence.Value;

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
                bool shouldBootstrapObject = bootstrapMissingTrails &&
                                             (bootstrapObjectIds is null || bootstrapObjectIds.Contains(obj.ObjectId));
                if (shouldBootstrapObject)
                {
                    BootstrapTrail(points, obj, currentGameTimeMs, rawNow);
                }
                else
                {
                    AddCurrentPoint(points, obj, currentGameTimeMs, rawNow);
                }

                continue;
            }

            if (currentSpeed == SimulationSpeed.Speed0)
            {
                bool predictionBaselineRewound = points.Count > 0 && currentGameTimeMs < points[^1].Timestamp;
                bool endpointChanged = points.Count > 0 &&
                                       !EndpointMatches(points[^1], obj, currentGameTimeMs);

                // Keep the whole trail in the same visual baseline as the paused object.
                if ((transitionToPause || pausedSnapshotChanged || predictionBaselineRewound) && endpointChanged)
                {
                    if (PauseResumeDiagnostics.Enabled)
                    {
                        var oldEndpoint = points.Count > 0 ? points[^1] : default;
                        var oldOldest = points.Count > 0 ? points[0] : default;
                        PauseResumeDiagnostics.Write(
                            $"TRAIL TRANSLATE id={obj.ObjectId} transitionToPause={transitionToPause} " +
                            $"pausedSnapshotChanged={pausedSnapshotChanged} predictionBaselineRewound={predictionBaselineRewound} " +
                            $"oldCount={points.Count} " +
                            $"oldOldestX={oldOldest.X:F3} oldOldestY={oldOldest.Y:F3} oldOldestT={oldOldest.Timestamp} " +
                            $"oldEndpointX={oldEndpoint.X:F3} oldEndpointY={oldEndpoint.Y:F3} oldEndpointT={oldEndpoint.Timestamp} " +
                            $"objDir={obj.Direction:F3} objTurnStepDeg={obj.TurnStepDegrees} objTurnStepRemainingMs={obj.TurnStepRemainingMs} " +
                            $"newX={obj.X:F3} newY={obj.Y:F3} newT={currentGameTimeMs}");

                        TranslateTrail(points, obj, currentGameTimeMs);

                        var newOldest = points.Count > 0 ? points[0] : default;
                        var newEndpoint = points.Count > 0 ? points[^1] : default;
                        PauseResumeDiagnostics.Write(
                            $"TRAIL TRANSLATED id={obj.ObjectId} newCount={points.Count} " +
                            $"newOldestX={newOldest.X:F3} newOldestY={newOldest.Y:F3} newOldestT={newOldest.Timestamp} " +
                            $"newEndpointX={newEndpoint.X:F3} newEndpointY={newEndpoint.Y:F3} newEndpointT={newEndpoint.Timestamp}");
                    }
                    else
                    {
                        TranslateTrail(points, obj, currentGameTimeMs);
                    }
                }
                continue;
            }

            long visualGameTimeMs = GetMonotonicGameTimeMs(points, currentGameTimeMs);
            PruneOldPoints(points, visualGameTimeMs);

            if (ShouldAddPoint(obj.ObjectId, points, obj.X, obj.Y, visualGameTimeMs, rawNow))
            {
                points.Add(new ObjectTrailPoint(obj.X, obj.Y, visualGameTimeMs));
                _lastSampleTimestamps[obj.ObjectId] = rawNow;
            }
        }

        _previousSpeed = currentSpeed;
        if (snapshotSequence.HasValue)
            _previousSnapshotSequence = snapshotSequence.Value;
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

    /// <summary>
    /// Reconnect the trail's endpoint with the object's current authoritative pose by
    /// shifting every existing point by the same offset — preserving the actual recorded
    /// shape of the path. Rebuilding from scratch (projecting backward from the CURRENT
    /// turn state) would silently assume the object had been turning at its current rate
    /// for the entire history window, which is wrong whenever a turn started partway
    /// through it — producing a visibly different trail shape on every pause/resume for
    /// any object with an active turn cycle.
    /// </summary>
    private static void TranslateTrail(
        List<ObjectTrailPoint> points,
        ObjectMotionSnapshot obj,
        long currentGameTimeMs)
    {
        var last = points[^1];
        double offsetX = obj.X - last.X;
        double offsetY = obj.Y - last.Y;

        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            points[i] = p with { X = p.X + offsetX, Y = p.Y + offsetY };
        }

        points[^1] = points[^1] with { X = obj.X, Y = obj.Y, Timestamp = currentGameTimeMs };
    }

    private static bool EndpointMatches(
        ObjectTrailPoint endpoint,
        ObjectMotionSnapshot obj,
        long currentGameTimeMs)
    {
        return endpoint.Timestamp == currentGameTimeMs &&
               endpoint.X == obj.X &&
               endpoint.Y == obj.Y;
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
