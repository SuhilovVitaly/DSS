using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using System.Globalization;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

internal enum MapFitMode { Target, Route, System }
[Flags]
internal enum MapLayerFlags { Orbits = 1, Territories = 2, Fields = 4, PointsOfInterest = 8, All = 15 }

public sealed partial class GameSessionScreen
{
    private readonly TacticalBattleInputSettings _tacticalBattleInputs;
    private readonly ShortcutButton _tacticalBattleEntry = new("");
    private bool CanOpenTacticalBattle => _handle is { Failure: null };
    private float TacticalBattleEntryHeight => CanOpenTacticalBattle ? 32 : 0;
    internal SKRect TacticalBattleEntryBounds => _tacticalBattleEntry.Bounds;
    internal string TacticalBattleHint => _tacticalBattleInputs.ShortcutAvailable
        ? string.Format(CultureInfo.InvariantCulture, BattleFormat.Localize("OpenHint"), _tacticalBattleInputs.TacticalBattle)
        : BattleFormat.Localize("BattleTitle");
    private readonly TacticalMapSettings _mapSettings;
    private readonly CameraZoomTransition _zoomTransition = new();
    private bool _mapCameraInitialized;
    private AuthoritativeSnapshot? _mapPresentationSnapshot;
    private MapWorldIdentity? _mapWorldIdentity;
    private long _mapWorldSnapshotTime;
    private ulong _mapWorldSnapshotSequence;
    internal MapDetailLevel MapDetail { get; private set; } = MapDetailLevel.Local;
    private double MapReferenceSpeed => MapScalePolicy.ReferenceSpeed(_mapSettings,
        _mapPresentationSnapshot?.ClusterMap?.Geography?.ReferenceSpeedKmS ?? _mapSettings.ReferenceSpeedKmS);
    internal double LocalMaximumPpu => MapScalePolicy.MaximumPpu(_mapSettings, _viewportW, _viewportH, MapReferenceSpeed);
    internal string RulerReferenceText(double meters) => string.Format(CultureInfo.InvariantCulture,
        Localization.Get("Map.ReferenceTime"), meters / 1000 / MapReferenceSpeed * 300 / 3600, MapReferenceSpeed);

    private bool UpdateMapWorld(AuthoritativeSnapshot? snapshot)
    {
        if (snapshot is null) return false;
        var identity = MapDetailPolicy.IdentifyWorld(snapshot);
        bool changed = _mapWorldIdentity is { } previous && (previous != identity ||
            snapshot.SolarSystemMap is not null && snapshot.SnapshotSequence != _mapWorldSnapshotSequence && snapshot.MotionTimeMs < _mapWorldSnapshotTime);
        _mapWorldIdentity = identity;
        _mapWorldSnapshotTime = snapshot.MotionTimeMs;
        _mapWorldSnapshotSequence = snapshot.SnapshotSequence;
        if (!changed) return false;
        _zoomTransition.Cancel(); _mapCameraInitialized = false; MapDetail = MapDetailLevel.Local;
        _mapClusters.Clear(); _mapSummaryLabels.Clear(); _visibleSummaryLabels.Clear(); _mapSummaryLabelRects.Clear();
        _clusteredObjectIds.Clear(); _clusterCells.Clear(); _clusterCellPool.Clear(); _districtOrbitMemberIds.Clear();
        _selectionCycle = []; _selectionCycleIndex = 0;
        _fittedBeltId = null; _fittedClusterId = null; _navigationTargetId = null;
        _selectedFieldId = null; _selectedPoiId = null; _selectedObjectId = snapshot.SelectedObjectId; _activeObjectId = null;
        _isPanningMap = false; _hasMousePosition = false; _isFocusAttachedToPlayer = true;
        _pausedVisualAnchors.Clear(); _visualCorrections.Clear(); _lastSnapshotBaselineObjects.Clear(); _hasSnapshotBaseline = false;
        _initialTrailBootstrapObjectIds.Clear(); _trailStore = new ObjectTrailStore(_predictor, _timestampProvider);
        return true;
    }
    private SKRect _mapToolbarRect;
    private readonly SKRect[] _mapViewButtons = new SKRect[12];
    private readonly SKPaint _mapMarkerPaint = new() { IsAntialias = true };
    private readonly HashSet<string> _clusteredObjectIds = new(StringComparer.Ordinal);
    private readonly List<MapCluster> _mapClusters = new();
    private readonly HashSet<string> _districtOrbitMemberIds = new(StringComparer.Ordinal);
    private readonly List<SummaryLabel> _mapSummaryLabels = new();
    private readonly List<SummaryLabel> _visibleSummaryLabels = new();
    private readonly List<SKRect> _mapSummaryLabelRects = new();
    private readonly record struct SummaryLabel(int ClusterIndex, string Text, SKRect Bounds);
    internal IReadOnlyList<SKRect> MapSummaryLabels => _visibleSummaryLabels.Select(l => l.Bounds).ToArray();
    private readonly Dictionary<(double X, double Y), List<ObjectRenderState>> _clusterCells = new();
    private readonly Stack<List<ObjectRenderState>> _clusterCellPool = new();
    private readonly List<SKRect> _mapObstacles = new();
    private int _freeViewportHash;
    private SKRect _freeViewport;
    private string? _navigationTargetId;
    private string? _fittedBeltId;
    private string? _fittedClusterId;
    internal MapLayerFlags MapLayers { get; private set; } = MapLayerFlags.All & ~MapLayerFlags.PointsOfInterest;
    internal bool ShowOrbits => MapLayers.HasFlag(MapLayerFlags.Orbits);
    private void ToggleMapLayer(MapLayerFlags layer)
    {
        MapLayers ^= layer;
        if (!MapLayers.HasFlag(MapLayerFlags.Fields)) _selectedFieldId = null;
        if (!MapLayers.HasFlag(MapLayerFlags.PointsOfInterest)) _selectedPoiId = null;
        _selectionCycle = []; _selectionCycleIndex = 0;
    }
    internal IReadOnlyDictionary<string, ObjectLabelGeometry> MapLabels => _labelRenderer.Geometries;
    internal IReadOnlyList<SKRect> MapViewButtonRects => _mapViewButtons;
    internal SKRect MapToolbarRect => _mapToolbarRect;
    internal int MapClusterCount => _mapClusters.Count;
    internal IReadOnlySet<string> AggregatedMapObjectIds => _clusteredObjectIds;
    internal bool IsZoomAnimating => _zoomTransition.Active;
    private readonly record struct MapCluster(double X, double Y, int Count, MapWorldBounds Bounds, string? Name = null,
        string? ClusterId = null, MapOrbitCurve? Orbit = null);
    private string[] _selectionCycle = [];
    private int _selectionCycleIndex;
    private float _selectionCycleX, _selectionCycleY;

    private (string? Id, int Kind) PickMapItem(float x, float y)
    {
        var candidates = new List<(string Id, int Kind, int Priority, double Distance)>();
        var labelCandidates = new List<(string Id, int Kind, int Priority, double Distance)>();
        foreach (var state in _renderStates)
        {
            if (_clusteredObjectIds.Contains(state.Pose.ObjectId)) continue;
            var p = _camera.WorldToScreen(state.Pose.X, state.Pose.Y, _viewportW, _viewportH);
            double distance = double.Hypot(p.X - x, p.Y - y);
            if (distance <= ObjectHitTestRadiusPx)
                candidates.Add((state.Pose.ObjectId, 0, GetClickPriority(state), distance));
            else if (HasSelectableMapLabel(state.Source) && _labelRenderer.Geometries.TryGetValue(state.Source.ObjectId, out var label) && label.PlaqueRect.Contains(x, y))
                labelCandidates.Add((state.Pose.ObjectId, 0, GetClickPriority(state), 0));
        }
        // A compact plaque may overlap another object: actual marker hits win.
        if (candidates.Count == 0) candidates.AddRange(labelCandidates);
        if (MapLayers.HasFlag(MapLayerFlags.PointsOfInterest))
            foreach (var point in _poiGeometry)
            {
                var p = _camera.WorldToScreen(point.X, point.Y, _viewportW, _viewportH);
                double distance = double.Hypot(p.X - x, p.Y - y);
                if (distance <= 15) candidates.Add((point.Data.ObjectId, 1, 4, distance));
            }
        if (!IsCtrlDown && MapLayers.HasFlag(MapLayerFlags.Fields))
        {
            var p = _camera.ScreenToWorld(x, y, _viewportW, _viewportH);
            foreach (var field in _fieldGeometry.Where(f => f.Contains(p.Item1, p.Item2))) candidates.Add((field.Data.Id, 2, 5, 0));
        }
        var sorted = candidates.OrderBy(c => c.Priority).ThenBy(c => c.Distance).ThenBy(c => c.Id, StringComparer.Ordinal).ToArray();
        var ids = sorted.Select(c => c.Id).ToArray();
        bool repeat = double.Hypot(x - _selectionCycleX, y - _selectionCycleY) <= 3 && ids.SequenceEqual(_selectionCycle);
        _selectionCycleIndex = repeat && ids.Length > 0 ? (_selectionCycleIndex + 1) % ids.Length : 0;
        _selectionCycle = ids; _selectionCycleX = x; _selectionCycleY = y;
        return sorted.Length == 0 ? (null, -1) : (sorted[_selectionCycleIndex].Id, sorted[_selectionCycleIndex].Kind);
    }

    private bool IsImportantMapObject(string id) => id == _selectedObjectId || id == _activeObjectId || id == _navigationTargetId ||
        id == _mapPresentationSnapshot?.PlayerShipObjectId;
    private bool IsPrimaryMapLabel(string id) => id == _selectedObjectId || id == _navigationTargetId ||
        id == _mapPresentationSnapshot?.PlayerShipObjectId;
    private static bool HasSelectableMapLabel(ObjectMotionSnapshot source) => ObjectLabelRenderer.HasSelectableShipLabel(source) ||
        source.RenderObjectType is SpaceObjectType.Station or SpaceObjectType.Asteroid or SpaceObjectType.Planet or SpaceObjectType.Sun;

    private void SetFollowPlayer()
    {
        _zoomTransition.Cancel();
        _isFocusAttachedToPlayer = true;
        UpdateCameraFocusFromPlayer(_renderStates);
    }

    private void ZoomBy(double steps, float x, float y)
    {
        if (_viewportW <= 0 || _viewportH <= 0 || !double.IsFinite(steps)) return;
        double previous = _zoomTransition.TargetPpu(_camera);
        double target = Math.Clamp(previous * Math.Pow(_mapSettings.WheelFactor, Math.Clamp(steps, -32, 32)),
            _mapSettings.MinimumPpu, LocalMaximumPpu);
        if (target == previous) return;
        _zoomTransition.Start(_camera, target, _isFocusAttachedToPlayer ? _viewportW / 2f : x,
            _isFocusAttachedToPlayer ? _viewportH / 2f : y, _mapSettings, _viewportW, _viewportH, LocalMaximumPpu);
        InterfaceLog.Write($"Scale → PPU={target:G6}");
    }

    private void UpdateMapClusters()
    {
        _navigationTargetId = FindPlayerShip(_renderStates)?.Pose.NavigationTargetObjectId;
        foreach (var cell in _clusterCells.Values)
        {
            cell.Clear();
            _clusterCellPool.Push(cell);
        }
        _mapClusters.Clear(); _clusteredObjectIds.Clear(); _clusterCells.Clear(); _districtOrbitMemberIds.Clear();
        if (_mapPresentationSnapshot?.AiMap is { Bases.IsDefaultOrEmpty: false } ai)
        {
            foreach (var aiBase in ai.Bases) _clusteredObjectIds.Add(aiBase.ObjectId);
            if (_selectedObjectId is { } id && _clusteredObjectIds.Contains(id)) SetSelectedObjectId(null);
        }
        MapDetail = MapDetailPolicy.Resolve(_viewportW, _viewportH, _camera.PixelsPerWorldUnit, MapReferenceSpeed, MapDetail);
        var map = _mapPresentationSnapshot?.ClusterMap;
        if (map is not null && !map.Clusters.IsDefaultOrEmpty)
        {
            foreach (var state in _renderStates)
            {
                var pose = state.Pose;
                if (pose.RenderObjectType != SpaceObjectType.Factory || IsImportantMapObject(pose.ObjectId)) continue;
                const double minimumFactorySeparationPx = 12;
                double separationPx = double.Hypot(state.Source.WorldOffsetX, state.Source.WorldOffsetY) * _camera.PixelsPerWorldUnit;
                if (MapDetail == MapDetailLevel.System || separationPx < minimumFactorySeparationPx)
                    _clusteredObjectIds.Add(pose.ObjectId);
            }
            var poses = _renderStates.Select(s => s.Predicted);
            if (MapDetail == MapDetailLevel.System)
            {
                foreach (var marker in ClusterMapPresentation.DomainMarkers(map, poses))
                {
                    const double districtSummaryDiameterPx = 220;
                    if (marker.Diameter * _camera.PixelsPerWorldUnit > districtSummaryDiameterPx) continue;
                    _mapClusters.Add(new(marker.X, marker.Y, marker.MemberIds.Length, marker.Bounds, marker.Name, marker.Id, marker.Orbit));
                    foreach (string id in marker.MemberIds)
                    {
                        _districtOrbitMemberIds.Add(id);
                        if (!IsPrimaryMapLabel(id)) _clusteredObjectIds.Add(id);
                    }
                }
                foreach (var state in _renderStates)
                    if (state.Pose.RenderObjectType == SpaceObjectType.Asteroid && !IsImportantMapObject(state.Pose.ObjectId))
                        _clusteredObjectIds.Add(state.Pose.ObjectId);
            }
            else if (MapDetail is MapDetailLevel.Region or MapDetailLevel.Cluster)
            {
                foreach (var marker in ClusterMapPresentation.ResourceMarkers(map, poses))
                {
                    foreach (string id in marker.MemberIds) if (!IsImportantMapObject(id)) _clusteredObjectIds.Add(id);
                    _mapClusters.Add(new(marker.X, marker.Y, marker.MemberIds.Length, marker.Bounds));
                }
            }
        }
        if (_camera.PixelsPerWorldUnit > _mapSettings.ClusterPpu) return;
        double cellSize = _mapSettings.ClusterCellPixels / _camera.PixelsPerWorldUnit;
        foreach (var state in _renderStates)
        {
            var p = state.Pose;
            if (_clusteredObjectIds.Contains(p.ObjectId) || map?.Stations.Any(s => s.ObjectId == p.ObjectId) == true) continue;
            if (IsImportantMapObject(p.ObjectId) || p.RenderObjectType is SpaceObjectType.Sun or SpaceObjectType.Planet or SpaceObjectType.Factory or SpaceObjectType.Station) continue;
            if (p is { RenderObjectType: SpaceObjectType.NpcShip, RelationToPlayer: PlayerRelation.Enemy }) continue;
            var (sx, sy) = _camera.WorldToScreen(p.X, p.Y, _viewportW, _viewportH);
            if (sx < -40 || sy < -40 || sx > _viewportW + 40 || sy > _viewportH + 40) continue;
            var key = (Math.Floor(p.X / cellSize), Math.Floor(p.Y / cellSize));
            if (!_clusterCells.TryGetValue(key, out var cell))
                _clusterCells[key] = cell = _clusterCellPool.TryPop(out var reused) ? reused : new();
            cell.Add(state);
        }
        foreach (var cell in _clusterCells.Values)
        {
            if (cell.Count < 2) continue;
            MapWorldBounds bounds = new();
            foreach (var state in cell)
            {
                bounds.Include(state.Pose.X, state.Pose.Y);
                _clusteredObjectIds.Add(state.Pose.ObjectId);
            }
            _mapClusters.Add(new(bounds.MinX + (bounds.MaxX - bounds.MinX) / 2,
                bounds.MinY + (bounds.MaxY - bounds.MinY) / 2, cell.Count, bounds));
        }
    }

    private IEnumerable<MapOrbitCurve> VisibleDistrictOrbits()
    {
        foreach (var cluster in _mapClusters)
        {
            if (cluster.Orbit is not { } orbit) continue;
            var (x, y) = _camera.WorldToScreen(cluster.X, cluster.Y, _viewportW, _viewportH);
            if (TacticalMapMarkerPolicy.IsInViewport(x, y, 15, _viewportW, _viewportH))
                yield return orbit;
        }
    }

    private void PrepareMapSummaryLabels()
    {
        _mapSummaryLabels.Clear(); _mapSummaryLabelRects.Clear();
        var available = AvailableMapRect();
        if (available.Width < 32 || available.Height < 24) available = SKRect.Create(_viewportW, _viewportH);
        for (int i = 0; i < _mapClusters.Count; i++)
        {
            var cluster = _mapClusters[i];
            if (cluster.Name is null) continue;
            var (x, y) = _camera.WorldToScreen(cluster.X, cluster.Y, _viewportW, _viewportH);
            if (x < -15 || y < -15 || x > _viewportW + 15 || y > _viewportH + 15) continue;
            string text = ObjectLabelRenderer.FitText(cluster.Name, _panelTextPaint, available.Width - 12);
            float width = _panelTextPaint.MeasureText(text) + 8;
            var desired = SKRect.Create(x - width / 2, y - 36, width, 18);
            if (!ObjectLabelLayout.TryPlace(desired, _mapSummaryLabelRects, available, out var bounds)) continue;
            _mapSummaryLabels.Add(new(i, text, bounds)); _mapSummaryLabelRects.Add(bounds);
        }
    }

    private void DrawMapClusters(SKCanvas canvas)
    {
        _visibleSummaryLabels.Clear();
        foreach (var cluster in _mapClusters)
        {
            var (x, y) = _camera.WorldToScreen(cluster.X, cluster.Y, _viewportW, _viewportH);
            _mapMarkerPaint.Color = TacticalMapPalette.LabelBackground;
            canvas.DrawCircle(x, y, 15, _mapMarkerPaint);
            string count = cluster.Count.ToString(CultureInfo.InvariantCulture);
            canvas.DrawText(count, x - _panelTextPaint.MeasureText(count) / 2, y + 4, _panelTextPaint);
        }
        foreach (var label in _mapSummaryLabels)
        {
            // Explicit target plaques win when a very crowded viewport cannot fit both.
            if (_labelRenderer.Geometries.Values.Any(g => g.PlaqueRect.IntersectsWith(label.Bounds))) continue;
            _mapMarkerPaint.Color = TacticalMapPalette.LabelBackground;
            canvas.DrawRect(label.Bounds, _mapMarkerPaint);
            canvas.DrawText(label.Text, label.Bounds.Left + 4, label.Bounds.Bottom - 4, _panelTextPaint);
            _visibleSummaryLabels.Add(label);
        }
    }

    private bool TryExpandMapCluster(float x, float y)
    {
        // Visible canonical markers win over aggregates beside their owners.
        if (_renderStates.Any(s => !_clusteredObjectIds.Contains(s.Pose.ObjectId) && MarkerHit(s.Pose.X, s.Pose.Y))) return false;
        if (MapLayers.HasFlag(MapLayerFlags.PointsOfInterest) && _poiGeometry.Any(p => Near(p.X, p.Y))) return false;
        if (_renderStates.Any(s => HasSelectableMapLabel(s.Source) && _labelRenderer.Geometries.TryGetValue(s.Pose.ObjectId, out var g) && g.PlaqueRect.Contains(x, y))) return false;
        for (int i = 0; i < _mapClusters.Count; i++)
        {
            var cluster = _mapClusters[i];
            if (!Near(cluster.X, cluster.Y) && !_visibleSummaryLabels.Any(l => l.ClusterIndex == i && l.Bounds.Contains(x, y))) continue;
            if (cluster.ClusterId is { } id) FitCluster(id); else FitMapBounds(cluster.Bounds);
            return true;
        }
        return false;
        bool Near(double wx, double wy)
        {
            var p = _camera.WorldToScreen(wx, wy, _viewportW, _viewportH);
            return (p.X - x) * (p.X - x) + (p.Y - y) * (p.Y - y) <= 225;
        }
        bool MarkerHit(double wx, double wy)
        {
            var p = _camera.WorldToScreen(wx, wy, _viewportW, _viewportH);
            return double.Hypot(p.X - x, p.Y - y) <= ObjectHitTestRadiusPx;
        }
    }

    internal SKRect AvailableMapRect()
    {
        _mapObstacles.Clear();
        void Add(SKRect r)
        {
            if (r.Width > 0 && r.Height > 0) _mapObstacles.Add(new(r.Left * _uiScale, r.Top * _uiScale, r.Right * _uiScale, r.Bottom * _uiScale));
        }
        Add(_commandsPanel.CaptionRect); Add(_commandsPanel.BodyRect);
        Add(_objectInfoPanel.CaptionRect); Add(_objectInfoPanel.BodyRect);
        if (_panelVisible) Add(_lastPanelRect);
        Add(_lastScalePanelRect); Add(_lastSpeedPanelRect); Add(_lastMechanicsPanelRect); Add(_mapToolbarRect); Add(_lastGameTimeRect);
        var hash = new HashCode(); hash.Add(_viewportW); hash.Add(_viewportH);
        foreach (var r in _mapObstacles) hash.Add(r);
        int value = hash.ToHashCode();
        if (value != _freeViewportHash || _freeViewport.IsEmpty)
        {
            _freeViewport = MapViewGeometry.FreeViewport(_viewportW, _viewportH, _mapObstacles, 100, 100);
            if (_freeViewport.IsEmpty)
                _freeViewport = MapViewGeometry.FreeViewport(_viewportW, _viewportH, _mapObstacles);
            _freeViewportHash = value;
        }
        return _freeViewport;
    }

    private void FitMapBounds(MapWorldBounds bounds)
    {
        if (!bounds.HasValue) return;
        _zoomTransition.Cancel(); _isFocusAttachedToPlayer = false; _isPanningMap = false;
        MapViewGeometry.Fit(_camera, bounds, AvailableMapRect(), _viewportW, _viewportH, _mapSettings, LocalMaximumPpu);
        UpdateMapClusters(); RecomputeActiveObjectId();
    }

    internal bool FitBelt(string beltId)
    {
        var belt = _mapPresentationSnapshot?.SolarSystemMap?.Belts.FirstOrDefault(b => b.Id == beltId);
        if (belt is null || _viewportW <= 0 || _viewportH <= 0) return false;
        MapWorldBounds bounds = new();
        bounds.Include(-belt.OuterRadius, -belt.OuterRadius);
        bounds.Include(belt.OuterRadius, belt.OuterRadius);
        _fittedBeltId = belt.Id;
        FitMapBounds(bounds);
        return true;
    }

    internal bool FitCluster(string clusterId)
    {
        var cluster = _mapPresentationSnapshot?.ClusterMap?.Clusters.FirstOrDefault(c => c.Id == clusterId);
        if (cluster is null || _viewportW <= 0 || _viewportH <= 0) return false;
        var bounds = ClusterMapPresentation.Bounds(cluster, _renderStates.Select(s => s.Predicted));
        if (!bounds.HasValue) return false;
        _fittedClusterId = clusterId;
        FitMapBounds(bounds);
        return true;
    }

    internal bool FocusStation(string stationId)
    {
        if (_viewportW <= 0 || _viewportH <= 0) return false;
        var bounds = ClusterMapPresentation.StationBounds(stationId, _renderStates.Select(s => s.Predicted));
        if (!bounds.HasValue) return false;
        _selectionCycle = []; _selectionCycleIndex = 0;
        _visibleSummaryLabels.Clear();
        if (bounds.MinX != bounds.MaxX || bounds.MinY != bounds.MaxY)
        {
            FitMapBounds(bounds);
            return true;
        }
        _zoomTransition.Cancel(); _isFocusAttachedToPlayer = false; _isPanningMap = false;
        double ppu = LocalMaximumPpu;
        _camera.SetZoom(ppu, _mapSettings.MinimumPpu, ppu);
        var free = AvailableMapRect();
        _camera.SetFocus(bounds.MinX - (free.MidX - _viewportW / 2d) / ppu,
            bounds.MinY - (free.MidY - _viewportH / 2d) / ppu);
        UpdateMapClusters();
        return true;
    }

    private void FitNextBelt()
    {
        if (_mapPresentationSnapshot?.SolarSystemMap is not { } map || map.Belts.IsEmpty) return;
        int index = -1;
        for (int i = 0; i < map.Belts.Length; i++) if (map.Belts[i].Id == _fittedBeltId) index = i;
        FitBelt(map.Belts[(index + 1) % map.Belts.Length].Id);
    }

    private void FitNextCluster()
    {
        if (_mapPresentationSnapshot?.ClusterMap is not { } map || map.Clusters.IsDefaultOrEmpty) return;
        int index = -1;
        for (int i = 0; i < map.Clusters.Length; i++) if (map.Clusters[i].Id == _fittedClusterId) index = i;
        FitCluster(map.Clusters[(index + 1) % map.Clusters.Length].Id);
    }

    internal bool FitMapView(MapFitMode mode)
    {
        var ship = FindPlayerShip(_renderStates)?.Predicted;
        if (ship is null) return false;
        MapWorldBounds bounds = new();
        bounds.Include(ship.X, ship.Y);
        if (mode == MapFitMode.System)
        {
            if (_mapPresentationSnapshot?.SolarSystemMap is { } system)
            {
                bounds.Include(-system.SystemRadius, -system.SystemRadius);
                bounds.Include(system.SystemRadius, system.SystemRadius);
                FitMapBounds(bounds);
                return true;
            }
            // Fit only known celestial/installation metadata; unknown types never become known through map framing.
            var sun = _renderStates.FirstOrDefault(s => s.Pose.RenderObjectType == SpaceObjectType.Sun).Predicted;
            double radius = 0;
            foreach (var s in _renderStates)
            {
                var p = s.Pose;
                if (p.RenderObjectType is not (SpaceObjectType.Sun or SpaceObjectType.Planet or SpaceObjectType.Station)) continue;
                bounds.Include(p.X, p.Y);
                if (sun is not null && p.RenderObjectType == SpaceObjectType.Planet)
                    radius = Math.Max(radius, Math.Sqrt(Math.Pow(p.X - sun.X, 2) + Math.Pow(p.Y - sun.Y, 2)));
            }
            if (sun is not null && radius > 0)
            {
                bounds.Include(sun.X - radius * 1.1, sun.Y - radius * 1.1);
                bounds.Include(sun.X + radius * 1.1, sun.Y + radius * 1.1);
            }
        }
        else if (mode == MapFitMode.Route)
        {
            if (ship.NavigationTargetX is null || ship.NavigationTargetY is null) return false;
            foreach (var p in _navigationTrajectoryProjector.ProjectInto(ship, _futureTrajectoryPoints, out _, out _)) bounds.Include(p.X, p.Y);
            bounds.Include(ship.NavigationTargetX.Value, ship.NavigationTargetY.Value);
        }
        else
        {
            var target = _renderStates.FirstOrDefault(s => s.Pose.ObjectId == _selectedObjectId).Predicted;
            if (target is not null && target.ObjectId != ship.ObjectId) bounds.Include(target.X, target.Y);
            else if (ship.NavigationTargetX is { } x && ship.NavigationTargetY is { } y) bounds.Include(x, y);
            else return false;
        }
        FitMapBounds(bounds);
        return true;
    }

    internal readonly List<string> OffscreenMarkerIds = new();
    private void DrawOffscreenTargets(SKCanvas canvas)
    {
        OffscreenMarkerIds.Clear();
        var rect = AvailableMapRect(); rect.Inflate(-18, -18);
        if (rect.Width <= 0 || rect.Height <= 0) return;
        var screenBounds = new SKRect(0, 0, _viewportW, _viewportH);
        var emitted = new HashSet<string>(StringComparer.Ordinal);
        var ship = FindPlayerShip(_renderStates)?.Predicted;
        foreach (var state in _renderStates)
            if (!state.IsPlayerShip && IsImportantMapObject(state.Pose.ObjectId) && !_clusteredObjectIds.Contains(state.Pose.ObjectId))
                Draw(state.Pose.ObjectId, state.Pose.X, state.Pose.Y, state.Pose.ObjectId == _selectedObjectId ? "Map.Selected" : "Map.Target");
        if (ship?.NavigationTargetX is { } tx && ship.NavigationTargetY is { } ty &&
            (_navigationTargetId is null || !_renderStates.Any(s => s.Pose.ObjectId == _navigationTargetId)))
            Draw(_navigationTargetId ?? "navigation-point", tx, ty, "Map.Target");

        void Draw(string id, double worldX, double worldY, string labelKey)
        {
            var (sx, sy) = _camera.WorldToScreen(worldX, worldY, _viewportW, _viewportH);
            if (screenBounds.Contains(sx, sy) || !emitted.Add(id)) return;
            OffscreenMarkerIds.Add(id);
            double dx = sx - rect.MidX, dy = sy - rect.MidY;
            double scale = Math.Min(rect.Width / 2 / Math.Max(.001, Math.Abs(dx)), rect.Height / 2 / Math.Max(.001, Math.Abs(dy)));
            float x = rect.MidX + (float)(dx * scale), y = rect.MidY + (float)(dy * scale);
            float angle = (float)(Math.Atan2(dy, dx) * 180 / Math.PI);
            canvas.Save(); canvas.Translate(x, y); canvas.RotateDegrees(angle);
            _mapMarkerPaint.Color = TacticalMapPalette.Target;
            using var arrow = new SKPath(); arrow.MoveTo(8, 0); arrow.LineTo(-5, -5); arrow.LineTo(-5, 5); arrow.Close();
            canvas.DrawPath(arrow, _mapMarkerPaint); canvas.Restore();
            double meters = ship is null ? 0 : Math.Sqrt(Math.Pow(worldX - ship.X, 2) + Math.Pow(worldY - ship.Y, 2)) * 100;
            string text = $"{Localization.Get(labelKey)} · {TacticalMapSettings.FormatDistance(meters)}";
            float textX = Math.Clamp(x - _panelTextPaint.MeasureText(text) / 2, rect.Left, Math.Max(rect.Left, rect.Right - _panelTextPaint.MeasureText(text)));
            canvas.DrawText(text, textX, y < rect.MidY ? y + 22 : y - 12, _panelTextPaint);
        }
    }

    private bool HandleMapToolbarClick(float x, float y)
    {
        for (int i = 0; i < _mapViewButtons.Length; i++)
        {
            if (!_mapViewButtons[i].Contains(x, y)) continue;
            if (i == 0) SetFollowPlayer();
            else if (i < 4) FitMapView((MapFitMode)(i - 1));
            else if (i == 4) RequestTacticalMapSnapshot();
            else if (i == 5 && IsMapViewAvailable(i)) ToggleMapLayer(MapLayerFlags.Orbits);
            else if (i == 6) FitNextBelt();
            else if (i == 7) FitNextCluster();
            else if (i == 11) { if (IsMapViewAvailable(i)) FocusStation(_selectedObjectId!); }
            else if (i >= 8) ToggleMapLayer((MapLayerFlags)(1 << (i - 7)));
            return true;
        }
        return _mapToolbarRect.Contains(x, y);
    }

    private void DrawMapToolbar(SKCanvas canvas)
    {
        bool hasMapLayers = _mapPresentationSnapshot?.AiMap is not null;
        bool hasDetailRow = hasMapLayers || _mapPresentationSnapshot?.SolarSystemMap is not null ||
            _mapPresentationSnapshot?.ClusterMap is not null || _renderStates.Any(s => s.Pose.RenderObjectType == SpaceObjectType.Station);
        float layerRowHeight = hasDetailRow ? 28 : 0;
        float width = Math.Min(640, Math.Max(260, _uiViewportW - 16));
        float left = (_uiViewportW - width) / 2, top = ComputeScaleSpeedRowY() - 78 - layerRowHeight;
        float battleRow = TacticalBattleEntryHeight;
        if (_commandsPanel.BodyRect.Bottom > top - 27 - battleRow)
        {
            left = Math.Max(left, _commandsPanel.BodyRect.Right + 8);
            width = Math.Min(width, _uiViewportW - left - 8);
        }
        int columns = width < 520 ? 4 : 8;
        float wrappedRowHeight = columns == 4 ? 27 : 0;
        top -= wrappedRowHeight;
        _mapToolbarRect = new(left, top - battleRow, left + width, top + 74 + layerRowHeight + wrappedRowHeight);
        canvas.DrawRect(_mapToolbarRect, _panelBgPaint);
        float battleWidth = Math.Min(280, width - 8);
        _tacticalBattleEntry.Bounds = battleRow > 0 && battleWidth > 0
            ? SKRect.Create(left + (width - battleWidth) / 2, top - 29, battleWidth, 26) : SKRect.Empty;
        _tacticalBattleEntry.Enabled = CanOpenTacticalBattle;
        if (!_tacticalBattleEntry.Bounds.IsEmpty)
        {
            _tacticalBattleEntry.Label = TacticalBattleHint;
            _tacticalBattleEntry.Tooltip = BattleFormat.Localize("OpenTooltip");
            _tacticalBattleEntry.Draw(canvas);
        }
        string[] keys = ["Map.Follow", "Map.ShipTarget", "Map.Route", "Map.System", "Map.Snapshot", "Map.Orbits", "Map.Belt", "Map.Cluster"];
        float buttonWidth = (width - 12) / columns;
        for (int i = 0; i < keys.Length; i++)
        {
            var r = new SKRect(left + 4 + (i % columns) * buttonWidth, top + 4 + (i / columns) * 27, left + 2 + (i % columns + 1) * buttonWidth, top + 27 + (i / columns) * 27);
            _mapViewButtons[i] = r;
            bool enabled = i == 4 ? SnapshotSaveTask.IsCompleted && !_snapshotCaptureRequested && !_captureThisFrame : IsMapViewAvailable(i);
            canvas.DrawRect(r, (i == 0 && _isFocusAttachedToPlayer || i == 5 && ShowOrbits && IsMapViewAvailable(i)) ? _scaleBtnActivePaint : _scaleBtnNormalPaint);
            canvas.DrawRect(r, _panelBorderPaint);
            _scaleBtnTextPaint.Color = enabled ? new SKColor(180, 180, 180) : new SKColor(80, 80, 80);
            string label = Localization.Get(keys[i]) + (i == 4 && !enabled ? "…" : "");
            canvas.DrawText(label, r.MidX, r.MidY + 4, _scaleBtnTextPaint);
        }
        string[] layers = ["Territories", "Fields", "POI", Localization.Get("Map.StationLocal")];
        Array.Clear(_mapViewButtons, 8, 4);
        for (int i = 0; hasDetailRow && i < layers.Length; i++)
        {
            if (i < 3 && !hasMapLayers) continue;
            var r = new SKRect(left + 4 + i * (width - 8) / 4, top + 31 + wrappedRowHeight, left + 2 + (i + 1) * (width - 8) / 4, top + 54 + wrappedRowHeight);
            _mapViewButtons[8 + i] = r;
            bool enabled = i < 3 || IsMapViewAvailable(11);
            canvas.DrawRect(r, i < 3 && MapLayers.HasFlag((MapLayerFlags)(1 << (i + 1))) ? _scaleBtnActivePaint : _scaleBtnNormalPaint);
            canvas.DrawRect(r, _panelBorderPaint);
            _scaleBtnTextPaint.Color = enabled ? new SKColor(180, 180, 180) : new SKColor(80, 80, 80);
            canvas.DrawText(ObjectLabelRenderer.FitText(layers[i], _scaleBtnTextPaint, Math.Max(0, r.Width - 8)), r.MidX, r.MidY + 4, _scaleBtnTextPaint);
        }
        _scaleBtnTextPaint.Color = new SKColor(180, 180, 180);
        string scale = $"1 px = {TacticalMapSettings.FormatDistance(100 / _camera.PixelsPerWorldUnit)}";
        canvas.DrawText(scale, left + 8, top + 46 + layerRowHeight + wrappedRowHeight, _panelTextPaint);
        // Ruler uses raw map pixels even though this canvas is in logical UI coordinates.
        double rawLength = Math.Min(120 * _uiScale, width * _uiScale * .24);
        double meters = rawLength / _camera.PixelsPerWorldUnit * 100;
        double power = Math.Pow(10, Math.Floor(Math.Log10(meters)));
        double nice = new[] { 1d, 2, 5 }.LastOrDefault(m => m * power <= meters, 1) * power;
        float rulerWidth = (float)(nice / 100 * _camera.PixelsPerWorldUnit / _uiScale);
        float rx = left + width - rulerWidth - 10, ry = top + 49 + layerRowHeight + wrappedRowHeight;
        _mapMarkerPaint.Color = TacticalMapPalette.Annotation; _mapMarkerPaint.StrokeWidth = 1;
        canvas.DrawLine(rx, ry, rx + rulerWidth, ry, _mapMarkerPaint);
        canvas.DrawLine(rx, ry - 3, rx, ry + 3, _mapMarkerPaint);
        canvas.DrawLine(rx + rulerWidth, ry - 3, rx + rulerWidth, ry + 3, _mapMarkerPaint);
        canvas.DrawText(TacticalMapSettings.FormatDistance(nice), rx + rulerWidth / 2, ry - 5, _scaleBtnTextPaint);
        canvas.DrawText(RulerReferenceText(nice), left + 8, top + 66 + layerRowHeight + wrappedRowHeight, _panelTextPaint);
        int hovered = HitTestScalePanel(_uiMouseX, _uiMouseY);
        if (hovered >= 0)
        {
            string hint = $"{ScaleLabels[hovered]}: 1 px = {TacticalMapSettings.FormatDistance(_mapSettings.MetersPerPixel[hovered])}";
            float hintWidth = _panelTextPaint.MeasureText(hint) + 16;
            var r = new SKRect(_lastScalePanelRect.MidX - hintWidth / 2, top - 26, _lastScalePanelRect.MidX + hintWidth / 2, top - 3);
            canvas.DrawRect(r, _panelBgPaint); canvas.DrawText(hint, r.Left + 8, r.Bottom - 6, _panelTextPaint);
        }
    }

    private bool IsMapViewAvailable(int index)
    {
        var ship = FindPlayerShip(_renderStates)?.Predicted;
        if (ship is null) return false;
        return index switch
        {
            1 => (_selectedObjectId is not null && _selectedObjectId != ship.ObjectId) || ship.NavigationTargetX is not null,
            2 => ship.NavigationTargetX is not null,
            5 => _mapPresentationSnapshot?.SolarSystemMap is not null || _renderStates.Any(s =>
                s.Source.Orbit is not null && s.Pose.RenderObjectType != SpaceObjectType.Factory),
            6 => _mapPresentationSnapshot?.SolarSystemMap?.Belts.Length > 0,
            7 => _mapPresentationSnapshot?.ClusterMap?.Clusters.Length > 0,
            11 => _selectedObjectId is not null && _renderStates.Any(s => s.Pose.ObjectId == _selectedObjectId && s.Pose.RenderObjectType == SpaceObjectType.Station),
            _ => true
        };
    }
}
