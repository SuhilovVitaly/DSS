using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    private readonly record struct GeometryView(double X, double Y, double Zoom, int Width, int Height);
    private readonly record struct TrajectoryKey(RenderMotion Pose, GeometryView View);
    private sealed class CachedTrajectory
    {
        internal TrajectoryKey Key;
        internal bool HasValue;
        internal readonly List<FutureTrajectoryPoint> Points = new();
        internal bool Confirmed;
        internal FutureTrajectoryPoint Intercept;
    }
    private readonly Dictionary<string, ObjectTrailGeometry> _trailGeometryCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CachedTrajectory> _futureGeometryCache = new(StringComparer.Ordinal);
    private readonly CachedTrajectory _navigationGeometryCache = new();
    private readonly List<string> _staleGeometryIds = new();
    private (ulong Snapshot, GeometryView View, string? Selected, string? Active, string? Navigation, float Scale, SKRect Free)? _labelGeometryKey;
    internal long TrailGeometryBuilds { get; private set; }
    internal long FutureGeometryBuilds { get; private set; }
    internal long NavigationGeometryBuilds { get; private set; }
    internal long LabelGeometryBuilds { get; private set; }
    private GeometryView CurrentGeometryView => new(_camera.FocusX, _camera.FocusY, _camera.PixelsPerWorldUnit, _viewportW, _viewportH);

    private void PruneGeometryCaches()
    {
        _staleGeometryIds.Clear();
        foreach (string id in _trailGeometryCache.Keys)
            if (!_trailStore.Trails.ContainsKey(id)) _staleGeometryIds.Add(id);
        foreach (string id in _staleGeometryIds) _trailGeometryCache.Remove(id);
        _staleGeometryIds.Clear();
        foreach (string id in _futureGeometryCache.Keys)
            if (!_currentVisualObjectIds.Contains(id) ||
                (id != _selectedObjectId && id != _navigationTargetId && id != _buffer.Latest?.Snapshot.PlayerShipObjectId))
                _staleGeometryIds.Add(id);
        foreach (string id in _staleGeometryIds) _futureGeometryCache.Remove(id);
    }

    private List<FutureTrajectoryPoint> CachedFuture(RenderMotion pose)
    {
        if (!_futureGeometryCache.TryGetValue(pose.ObjectId, out var entry))
            _futureGeometryCache[pose.ObjectId] = entry = new();
        var key = new TrajectoryKey(pose, CurrentGeometryView);
        if (!entry.HasValue || entry.Key != key)
        {
            _futureTrajectoryProjector.ProjectViewportInto(pose.ToSnapshot(), entry.Points, _camera, _viewportW, _viewportH);
            entry.Key = key;
            entry.HasValue = true;
            FutureGeometryBuilds++;
        }
        return entry.Points;
    }

    private CachedTrajectory CachedNavigation(RenderMotion pose)
    {
        var entry = _navigationGeometryCache;
        var key = new TrajectoryKey(pose, CurrentGeometryView);
        if (!entry.HasValue || entry.Key != key)
        {
            _navigationTrajectoryProjector.ProjectPlayerInto(pose.ToSnapshot(), entry.Points,
                _camera, _viewportW, _viewportH, out entry.Confirmed, out entry.Intercept);
            entry.Key = key;
            entry.HasValue = true;
            NavigationGeometryBuilds++;
        }
        return entry;
    }

    private void PrepareLabelGeometry(SnapshotPrediction prediction, double deltaSeconds, bool resetSmoothing)
    {
        var free = AvailableMapRect();
        var key = (prediction.BufferedSnapshot.Snapshot.SnapshotSequence, CurrentGeometryView,
            _selectedObjectId, _activeObjectId, _navigationTargetId, _uiScale, free);
        if (prediction.CurrentSpeed == SimulationSpeed.Speed0 && !resetSmoothing && _labelGeometryKey == key) return;
        _labelRenderer.ComputeGeometries(_renderStates, deltaSeconds, _viewportW, _viewportH, _camera, resetSmoothing,
            _mapSettings, IsImportantMapObject, _clusteredObjectIds, free, _selectedObjectId, _navigationTargetId);
        _labelGeometryKey = prediction.CurrentSpeed == SimulationSpeed.Speed0 ? key : null;
        LabelGeometryBuilds++;
    }
}
