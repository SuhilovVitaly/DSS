using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

internal enum MapFitMode { Target, Route, System }

public sealed partial class GameSessionScreen
{
    private readonly TacticalMapSettings _mapSettings;
    private readonly TacticalMapSceneBuilder _sceneBuilder = new();
    internal TacticalMapSceneGeometry? PreparedScene { get; private set; }

    private void PublishSceneGeometry()
    {
        if (_mapFrame is not { } frame) return;
        var camera = new TacticalMapCamera(_camera.FocusX, _camera.FocusY, _camera.PixelsPerWorldUnit);
        var clusters = _mapClusters.Where(ClusterVisible).Select(c => new TacticalMapClusterGeometry(
            c.Level, c.CellX, c.CellY, c.X, c.Y, camera.Project(c.X, c.Y, _viewportW, _viewportH), c.Count, c.Bounds)).ToImmutableArray();
        var paths = _capturedTrajectories.Select(path => new TacticalMapPathGeometry(path.ObjectId, path.Kind,
            path.Points.ToImmutableArray(), path.Points.Select(p => camera.Project(p.X, p.Y, _viewportW, _viewportH)).ToImmutableArray())).ToImmutableArray();
        var trails = ImmutableArray.CreateBuilder<TacticalMapTrailSegment>();
        foreach (var (id, geometry) in _trailGeometryCache)
        {
            if (!_trailStore.Trails.TryGetValue(id, out var history) || history.Count < 2 ||
                (_camera.PixelsPerWorldUnit < _mapSettings.TrailDetailPpu && !IsImportantMapObject(id)) || _missileTrailIds.Contains(id)) continue;
            // Only a cache entry prepared for this camera and history may be published.
            if (!geometry.Matches(history, _camera, _viewportW, _viewportH, id == frame.Prediction?.BufferedSnapshot.Snapshot.PlayerShipObjectId)) continue;
            foreach (var segment in geometry.Segments)
            {
                var a = geometry.Points[segment.Start]; var b = geometry.Points[segment.End];
                if ((a.X < -2 && b.X < -2) || (a.Y < -2 && b.Y < -2) ||
                    (a.X > _viewportW + 2 && b.X > _viewportW + 2) || (a.Y > _viewportH + 2 && b.Y > _viewportH + 2)) continue;
                trails.Add(new(a, b, GetTrailSegmentColor((float)segment.End / (history.Count - 1),
                    id == frame.Prediction?.BufferedSnapshot.Snapshot.PlayerShipObjectId)));
            }
        }
        var free = AvailableMapRect();
        var view = new TacticalMapViewInput(camera, _viewportW, _viewportH, _uiScale, free, _mapObstacles.ToImmutableArray(),
            Localization.Revision, _mapSettings.CompactMarkerPpu, _selectedObjectId, _activeObjectId, _navigationTargetId,
            frame.Objects.Where(s => IsImportantMapObject(s.Pose.ObjectId)).Select(s => s.Pose.ObjectId).ToImmutableHashSet(StringComparer.Ordinal),
            _clusteredObjectIds.ToImmutableHashSet(StringComparer.Ordinal), clusters,
            _labelRenderer.CaptureGeometry(_renderStates), paths, trails.ToImmutable())
        {
            Settings = new(_mapSettings.LabelDetailPpu, _mapSettings.TrailDetailPpu, _mapSettings.MaximumLabels,
                _mapSettings.ClusterPpu, _mapSettings.ClusterCellPixels, _mapSettings.ClusterHysteresis,
                _mapSettings.GridBaseCellPixels, _mapSettings.GridMinimumPixels, _mapSettings.GridFadePixels)
        };
        PreparedScene = _sceneBuilder.Prepare(frame, view);
    }

    private readonly CameraZoomTransition _zoomTransition = new();
    private SKRect _mapToolbarRect;
    private readonly SKRect[] _mapViewButtons = new SKRect[8];
    private readonly SKPaint _mapMarkerPaint = new() { IsAntialias = true };
    private readonly HashSet<string> _clusteredObjectIds = new(StringComparer.Ordinal);
    private readonly List<MapCluster> _mapClusters = new();
    private readonly Dictionary<(double X, double Y), List<ObjectRenderState>> _clusterCells = new();
    private readonly Stack<List<ObjectRenderState>> _clusterCellPool = new();
    private readonly List<SKRect> _mapObstacles = new();
    private bool _hasFreeViewport;
    private (int Width, int Height) _freeViewportSize;
    private SKRect[] _freeViewportObstacles = [];
    internal long FreeViewportBuilds { get; private set; }
    private SKRect _freeViewport;
    private string? _navigationTargetId;
    private string? _fittedBeltId;
    private string? _fittedClusterId;
    internal bool ShowOrbits { get; private set; } = true;
    internal IReadOnlyDictionary<string, ObjectLabelGeometry> MapLabels => _labelRenderer.Geometries;
    internal IReadOnlyList<SKRect> MapViewButtonRects => _mapViewButtons;
    internal SKRect MapToolbarRect => _mapToolbarRect;
    internal int MapClusterCount => _mapClusters.Count;
    internal IReadOnlyList<(int Level, double X, double Y)> MapClusterIds => _mapClusters.Select(c => c.Id).ToArray();
    internal bool IsZoomAnimating => _zoomTransition.Active;
    private int? _clusterLevel;
    internal int? ClusterLevel => _clusterLevel;
    private readonly record struct MapCluster(double X, double Y, int Count, MapWorldBounds Bounds, double CellX, double CellY, int Level)
    {
        internal (int Level, double X, double Y) Id => (Level, CellX, CellY);
    }

    private double? ClusterCellSize()
    {
        double ppu = _camera.PixelsPerWorldUnit, threshold = _mapSettings.ClusterPpu;
        double hysteresis = _mapSettings.ClusterHysteresis;
        if (_clusterLevel is null)
        {
            if (ppu > threshold) return null;
            _clusterLevel = Math.Max(0, (int)Math.Floor(Math.Log2(threshold / ppu)));
        }
        if (ppu > threshold * (1 + hysteresis)) { _clusterLevel = null; return null; }
        int level = _clusterLevel.Value;
        while (ppu < threshold / Math.Pow(2, level + 1) * (1 - hysteresis)) level++;
        while (level > 0 && ppu > threshold / Math.Pow(2, level) * (1 + hysteresis)) level--;
        _clusterLevel = level;
        return _mapSettings.ClusterCellPixels / threshold * Math.Pow(2, level);
    }

    private bool IsImportantMapObject(string id) => id == _selectedObjectId || id == _activeObjectId || id == _navigationTargetId ||
        id == _buffer.Latest?.Snapshot.PlayerShipObjectId || _combatImportantIds.Contains(id);

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
            _mapSettings.MinimumPpu, _mapSettings.MaximumPpu);
        if (target == previous) return;
        _zoomTransition.Start(_camera, target, _isFocusAttachedToPlayer ? _viewportW / 2f : x,
            _isFocusAttachedToPlayer ? _viewportH / 2f : y, _mapSettings, _viewportW, _viewportH);
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
        _mapClusters.Clear(); _clusteredObjectIds.Clear(); _clusterCells.Clear();
        if (ClusterCellSize() is not { } cellSize) return;
        foreach (var state in _renderStates)
        {
            var p = state.Pose;
            if (IsImportantMapObject(p.ObjectId) || p.RenderObjectType is SpaceObjectType.Sun or SpaceObjectType.Planet) continue;
            if (p is { RenderObjectType: SpaceObjectType.NpcShip, RelationToPlayer: PlayerRelation.Enemy }) continue;
            var key = (Math.Floor(p.X / cellSize), Math.Floor(p.Y / cellSize));
            if (!_clusterCells.TryGetValue(key, out var cell))
                _clusterCells[key] = cell = _clusterCellPool.TryPop(out var reused) ? reused : new();
            cell.Add(state);
        }
        foreach (var (key, cell) in _clusterCells.OrderBy(c => c.Key.X).ThenBy(c => c.Key.Y))
        {
            if (cell.Count < 2) continue;
            MapWorldBounds bounds = new();
            foreach (var state in cell)
            {
                bounds.Include(state.Pose.X, state.Pose.Y);
            }
            var cluster = new MapCluster(bounds.MinX + (bounds.MaxX - bounds.MinX) / 2,
                bounds.MinY + (bounds.MaxY - bounds.MinY) / 2, cell.Count, bounds, key.X, key.Y, _clusterLevel!.Value);
            _mapClusters.Add(cluster);
            // An offscreen centroid cannot replace a visible contact with an invisible badge.
            if (ClusterVisible(cluster))
                foreach (var state in cell) _clusteredObjectIds.Add(state.Pose.ObjectId);
        }
    }

    private bool ClusterVisible(MapCluster cluster)
    {
        var (x, y) = _camera.WorldToScreen(cluster.X, cluster.Y, _viewportW, _viewportH);
        return x >= -15 && y >= -15 && x <= _viewportW + 15 && y <= _viewportH + 15;
    }

    private void DrawMapClusters(SKCanvas canvas)
    {
        foreach (var cluster in _mapClusters)
        {
            if (!ClusterVisible(cluster)) continue;
            var (x, y) = _camera.WorldToScreen(cluster.X, cluster.Y, _viewportW, _viewportH);
            _mapMarkerPaint.Color = new SKColor(22, 45, 60);
            canvas.DrawCircle(x, y, 15, _mapMarkerPaint);
            string count = cluster.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            canvas.DrawText(count, x - _panelTextPaint.MeasureText(count) / 2, y + 4, _panelTextPaint);
        }
    }

    private bool TryExpandMapCluster(float x, float y)
    {
        MapCluster? best = null;
        double bestDistance = double.MaxValue;
        foreach (var cluster in _mapClusters)
        {
            var p = _camera.WorldToScreen(cluster.X, cluster.Y, _viewportW, _viewportH);
            double distance = (p.X - x) * (p.X - x) + (p.Y - y) * (p.Y - y);
            if (distance > 225) continue;
            if (best is null || distance < bestDistance ||
                (distance == bestDistance && (cluster.CellX < best.Value.CellX ||
                    (cluster.CellX == best.Value.CellX && cluster.CellY < best.Value.CellY))))
            {
                best = cluster;
                bestDistance = distance;
            }
        }
        if (best is null) return false;
        FitMapBounds(best.Value.Bounds);
        return true;
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
        Add(_combatJournalPanel.Bounds);
        if (_panelVisible) Add(_lastPanelRect);
        Add(_lastScalePanelRect); Add(_lastSpeedPanelRect); Add(_lastMechanicsPanelRect); Add(_mapToolbarRect);
        Add(_gameTimeRect);
        return ResolveFreeViewport(_viewportW, _viewportH, _mapObstacles);
    }

    internal SKRect ResolveFreeViewport(int width, int height, IReadOnlyList<SKRect> obstacles)
    {
        if (!_hasFreeViewport || _freeViewportSize != (width, height) ||
            !obstacles.SequenceEqual(_freeViewportObstacles))
        {
            _freeViewport = MapViewGeometry.FreeViewport(width, height, obstacles);
            _freeViewportSize = (width, height);
            _freeViewportObstacles = obstacles.ToArray();
            _hasFreeViewport = true;
            FreeViewportBuilds++;
        }
        return _freeViewport;
    }

    private void FitMapBounds(MapWorldBounds bounds)
    {
        if (!bounds.HasValue) return;
        _zoomTransition.Cancel(); _isFocusAttachedToPlayer = false; _isPanningMap = false;
        MapViewGeometry.Fit(_camera, bounds, AvailableMapRect(), _viewportW, _viewportH, _mapSettings);
        UpdateMapClusters(); RecomputeActiveObjectId();
    }

    internal bool FitBelt(string beltId)
    {
        var belt = _buffer.Latest?.Snapshot.SolarSystemMap?.Belts.FirstOrDefault(b => b.Id == beltId);
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
        var cluster = _buffer.Latest?.Snapshot.ClusterMap?.Clusters.FirstOrDefault(c => c.Id == clusterId);
        if (cluster is null || _viewportW <= 0 || _viewportH <= 0) return false;
        var bounds = ClusterMapPresentation.Bounds(cluster, _renderStates.Select(s => s.Predicted));
        if (!bounds.HasValue) return false;
        _fittedClusterId = clusterId;
        FitMapBounds(bounds);
        return true;
    }

    private void FitNextBelt()
    {
        if (_buffer.Latest?.Snapshot.SolarSystemMap is not { } map || map.Belts.IsEmpty) return;
        int index = -1;
        for (int i = 0; i < map.Belts.Length; i++) if (map.Belts[i].Id == _fittedBeltId) index = i;
        FitBelt(map.Belts[(index + 1) % map.Belts.Length].Id);
    }

    private void FitNextCluster()
    {
        if (_buffer.Latest?.Snapshot.ClusterMap is not { } map || map.Clusters.IsDefaultOrEmpty) return;
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
            if (_buffer.Latest?.Snapshot.SolarSystemMap is { } system)
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

    private void DrawOffscreenTargets(SKCanvas canvas)
    {
        var rect = AvailableMapRect(); rect.Inflate(-18, -18);
        if (rect.Width <= 0 || rect.Height <= 0) return;
        var ship = FindPlayerShip(_renderStates)?.Predicted;
        foreach (var state in _renderStates)
            if (!state.IsPlayerShip && IsImportantMapObject(state.Pose.ObjectId))
                Draw(state.Pose.X, state.Pose.Y, state.Pose.ObjectId == _selectedObjectId ? "Map.Selected" : "Map.Target");
        if (ship?.NavigationTargetX is { } tx && ship.NavigationTargetY is { } ty &&
            (_navigationTargetId is null || !_renderStates.Any(s => s.Pose.ObjectId == _navigationTargetId)))
            Draw(tx, ty, "Map.Target");

        void Draw(double worldX, double worldY, string labelKey)
        {
            var (sx, sy) = _camera.WorldToScreen(worldX, worldY, _viewportW, _viewportH);
            if (rect.Contains(sx, sy)) return;
            double dx = sx - rect.MidX, dy = sy - rect.MidY;
            double scale = Math.Min(rect.Width / 2 / Math.Max(.001, Math.Abs(dx)), rect.Height / 2 / Math.Max(.001, Math.Abs(dy)));
            float x = rect.MidX + (float)(dx * scale), y = rect.MidY + (float)(dy * scale);
            float angle = (float)(Math.Atan2(dy, dx) * 180 / Math.PI);
            canvas.Save(); canvas.Translate(x, y); canvas.RotateDegrees(angle);
            _mapMarkerPaint.Color = new SKColor(240, 175, 75);
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
            else if (i == 5 && IsMapViewAvailable(i)) ShowOrbits = !ShowOrbits;
            else if (i == 6) FitNextBelt();
            else if (i == 7) FitNextCluster();
            return true;
        }
        return _mapToolbarRect.Contains(x, y);
    }

    private void LayoutMapToolbar()
    {
        float width = Math.Min(640, Math.Max(260, _uiViewportW - 16));
        float left = (_uiViewportW - width) / 2, top = ComputeScaleSpeedRowY() - 62;
        _mapToolbarRect = new(left, top, left + width, top + 58);
        float buttonWidth = (width - 12) / _mapViewButtons.Length;
        for (int i = 0; i < _mapViewButtons.Length; i++)
            _mapViewButtons[i] = new(left + 4 + i * buttonWidth, top + 4, left + 2 + (i + 1) * buttonWidth, top + 27);
    }

    private void DrawMapToolbar(SKCanvas canvas)
    {
        float width = _mapToolbarRect.Width, left = _mapToolbarRect.Left, top = _mapToolbarRect.Top;
        canvas.DrawRect(_mapToolbarRect, _panelBgPaint);
        string[] keys = ["Map.Follow", "Map.ShipTarget", "Map.Route", "Map.System", "Map.Snapshot", "Map.Orbits", "Map.Belt", "Map.Cluster"];
        for (int i = 0; i < keys.Length; i++)
        {
            var r = _mapViewButtons[i];
            bool enabled = i == 4 ? SnapshotSaveTask.IsCompleted : IsMapViewAvailable(i);
            canvas.DrawRect(r, (i == 0 && _isFocusAttachedToPlayer || i == 5 && ShowOrbits && IsMapViewAvailable(i)) ? _scaleBtnActivePaint : _scaleBtnNormalPaint);
            canvas.DrawRect(r, _panelBorderPaint);
            _scaleBtnTextPaint.Color = enabled ? new SKColor(180, 180, 180) : new SKColor(80, 80, 80);
            string label = Localization.Get(keys[i]) + (i == 4 && !enabled ? "…" : "");
            canvas.DrawText(label, r.MidX, r.MidY + 4, _scaleBtnTextPaint);
        }
        _scaleBtnTextPaint.Color = new SKColor(180, 180, 180);
        string scale = $"1 px = {TacticalMapSettings.FormatDistance(100 / _camera.PixelsPerWorldUnit)}";
        canvas.DrawText(scale, left + 8, top + 46, _panelTextPaint);
        // Ruler uses raw map pixels even though this canvas is in logical UI coordinates.
        double rawLength = Math.Min(120 * _uiScale, width * _uiScale * .24);
        double meters = rawLength / _camera.PixelsPerWorldUnit * 100;
        double power = Math.Pow(10, Math.Floor(Math.Log10(meters)));
        double nice = new[] { 1d, 2, 5 }.LastOrDefault(m => m * power <= meters, 1) * power;
        float rulerWidth = (float)(nice / 100 * _camera.PixelsPerWorldUnit / _uiScale);
        float rx = left + width - rulerWidth - 10, ry = top + 49;
        _mapMarkerPaint.Color = new SKColor(160, 175, 185); _mapMarkerPaint.StrokeWidth = 1;
        canvas.DrawLine(rx, ry, rx + rulerWidth, ry, _mapMarkerPaint);
        canvas.DrawLine(rx, ry - 3, rx, ry + 3, _mapMarkerPaint);
        canvas.DrawLine(rx + rulerWidth, ry - 3, rx + rulerWidth, ry + 3, _mapMarkerPaint);
        canvas.DrawText(TacticalMapSettings.FormatDistance(nice), rx + rulerWidth / 2, ry - 5, _scaleBtnTextPaint);
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
            5 => _buffer.Latest?.Snapshot.SolarSystemMap is not null,
            6 => _buffer.Latest?.Snapshot.SolarSystemMap?.Belts.Length > 0,
            7 => _buffer.Latest?.Snapshot.ClusterMap?.Clusters.Length > 0,
            _ => true
        };
    }
}
