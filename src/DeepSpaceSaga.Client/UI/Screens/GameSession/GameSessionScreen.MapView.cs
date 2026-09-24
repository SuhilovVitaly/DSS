using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

internal enum MapFitMode { Target, Route, System }

public sealed partial class GameSessionScreen
{
    private readonly TacticalMapSettings _mapSettings;
    private readonly CameraZoomTransition _zoomTransition = new();
    private SKRect _mapToolbarRect;
    private readonly SKRect[] _mapViewButtons = new SKRect[5];
    private readonly SKPaint _mapMarkerPaint = new() { IsAntialias = true };
    private readonly HashSet<string> _clusteredObjectIds = new(StringComparer.Ordinal);
    private readonly List<MapCluster> _mapClusters = new();
    private readonly Dictionary<(double X, double Y), List<ObjectRenderState>> _clusterCells = new();
    private readonly Stack<List<ObjectRenderState>> _clusterCellPool = new();
    private readonly List<SKRect> _mapObstacles = new();
    private int _freeViewportHash;
    private SKRect _freeViewport;
    private string? _navigationTargetId;
    internal IReadOnlyList<SKRect> MapViewButtonRects => _mapViewButtons;
    internal SKRect MapToolbarRect => _mapToolbarRect;
    internal int MapClusterCount => _mapClusters.Count;
    internal bool IsZoomAnimating => _zoomTransition.Active;
    private readonly record struct MapCluster(double X, double Y, int Count, MapWorldBounds Bounds);

    private bool IsImportantMapObject(string id) => id == _selectedObjectId || id == _navigationTargetId ||
        id == _buffer.Latest?.Snapshot.PlayerShipObjectId;

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
        if (_camera.PixelsPerWorldUnit > _mapSettings.ClusterPpu) return;
        double cellSize = _mapSettings.ClusterCellPixels / _camera.PixelsPerWorldUnit;
        foreach (var state in _renderStates)
        {
            var p = state.Pose;
            if (IsImportantMapObject(p.ObjectId) || p.RenderObjectType is SpaceObjectType.Sun or SpaceObjectType.Planet) continue;
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

    private void DrawMapClusters(SKCanvas canvas)
    {
        foreach (var cluster in _mapClusters)
        {
            var (x, y) = _camera.WorldToScreen(cluster.X, cluster.Y, _viewportW, _viewportH);
            _mapMarkerPaint.Color = new SKColor(22, 45, 60);
            canvas.DrawCircle(x, y, 15, _mapMarkerPaint);
            string count = cluster.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            canvas.DrawText(count, x - _panelTextPaint.MeasureText(count) / 2, y + 4, _panelTextPaint);
        }
    }

    private bool TryExpandMapCluster(float x, float y)
    {
        // Explicit targets win over a nearby aggregate.
        if (_renderStates.Any(s => IsImportantMapObject(s.Pose.ObjectId) && Near(s.Pose.X, s.Pose.Y))) return false;
        foreach (var cluster in _mapClusters)
        {
            if (!Near(cluster.X, cluster.Y)) continue;
            FitMapBounds(cluster.Bounds);
            return true;
        }
        return false;
        bool Near(double wx, double wy)
        {
            var p = _camera.WorldToScreen(wx, wy, _viewportW, _viewportH);
            return (p.X - x) * (p.X - x) + (p.Y - y) * (p.Y - y) <= 225;
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
        Add(_lastScalePanelRect); Add(_lastSpeedPanelRect); Add(_lastMechanicsPanelRect); Add(_mapToolbarRect);
        var hash = new HashCode(); hash.Add(_viewportW); hash.Add(_viewportH);
        foreach (var r in _mapObstacles) hash.Add(r);
        int value = hash.ToHashCode();
        if (value != _freeViewportHash || _freeViewport.IsEmpty)
        {
            _freeViewport = MapViewGeometry.FreeViewport(_viewportW, _viewportH, _mapObstacles);
            _freeViewportHash = value;
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

    internal bool FitMapView(MapFitMode mode)
    {
        var ship = FindPlayerShip(_renderStates)?.Predicted;
        if (ship is null) return false;
        MapWorldBounds bounds = new();
        bounds.Include(ship.X, ship.Y);
        if (mode == MapFitMode.System)
        {
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
            else CaptureTacticalMapSnapshot();
            return true;
        }
        return _mapToolbarRect.Contains(x, y);
    }

    private void DrawMapToolbar(SKCanvas canvas)
    {
        float width = Math.Min(640, Math.Max(260, _uiViewportW - 16));
        float left = (_uiViewportW - width) / 2, top = ComputeScaleSpeedRowY() - 62;
        _mapToolbarRect = new(left, top, left + width, top + 58);
        canvas.DrawRect(_mapToolbarRect, _panelBgPaint);
        string[] keys = ["Map.Follow", "Map.ShipTarget", "Map.Route", "Map.System", "Map.Snapshot"];
        float buttonWidth = (width - 12) / keys.Length;
        for (int i = 0; i < keys.Length; i++)
        {
            var r = new SKRect(left + 4 + i * buttonWidth, top + 4, left + 2 + (i + 1) * buttonWidth, top + 27);
            _mapViewButtons[i] = r;
            bool enabled = i == 4 || IsMapViewAvailable(i);
            canvas.DrawRect(r, i == 0 && _isFocusAttachedToPlayer ? _scaleBtnActivePaint : _scaleBtnNormalPaint);
            canvas.DrawRect(r, _panelBorderPaint);
            _scaleBtnTextPaint.Color = enabled ? new SKColor(180, 180, 180) : new SKColor(80, 80, 80);
            canvas.DrawText(Localization.Get(keys[i]), r.MidX, r.MidY + 4, _scaleBtnTextPaint);
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
            _ => true
        };
    }
}
