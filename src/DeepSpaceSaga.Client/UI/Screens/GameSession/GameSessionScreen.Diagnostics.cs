using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    private readonly List<(string ObjectId, string Kind, IReadOnlyList<FutureTrajectoryPoint> Points)> _capturedTrajectories = new();
    private sealed record PresentedFrame(SnapshotPrediction? Prediction, long Timestamp, long Id,
        TacticalMapCameraSnapshot Camera, TacticalMapViewportSnapshot Viewport, TacticalMapUiSnapshot Ui,
        string? Active, string? Selected, string? Navigation);
    private PresentedFrame? _presentedFrame;
    private readonly HashSet<string> _presentedClusteredIds = new(StringComparer.Ordinal);
    internal Task<string?> SnapshotSaveTask { get; private set; } = Task.FromResult<string?>(null);
    private readonly CancellationTokenSource _ioStop = new();
    internal Func<TacticalMapSnapshotDocument, string, string>? SnapshotWriter { get; set; }
    internal string? LastTacticalMapSnapshotPath => SnapshotSaveTask.IsCompletedSuccessfully ? SnapshotSaveTask.Result : null;

    internal void RequestTacticalMapSnapshot()
    {
        // One detached document at a time; repeated clicks cannot grow a writer queue.
        if (!_disposed && SnapshotSaveTask.IsCompleted) CaptureTacticalMapSnapshot(_presentedFrame?.Prediction, _presentedFrame?.Timestamp ?? 0);
    }

    private void SealPresentedFrame(SnapshotPrediction? prediction, long timestamp)
    {
        _presentedClusteredIds.Clear();
        _presentedClusteredIds.UnionWith(_clusteredObjectIds);
        _presentedFrame = new(prediction, timestamp, _profileFrameId,
            new(_camera.FocusX, _camera.FocusY, _camera.PixelsPerWorldUnit, _isFocusAttachedToPlayer,
                _zoomTransition.Active, _zoomTransition.TargetPpu(_camera)),
            new(_viewportW, _viewportH, _uiScale, _mouseX, _mouseY, _uiMouseX, _uiMouseY, _hasMousePosition),
            new(_panelVisible, _isPanningMap, IsCtrlDown, _mapClusters.Count, _clusteredObjectIds.Count),
            _activeObjectId, _selectedObjectId, _navigationTargetId);
    }

    private void CaptureTacticalMapSnapshot(SnapshotPrediction? prediction, long frameTimestamp)
    {
        try
        {
            AuthoritativeSnapshot? authoritative = prediction?.BufferedSnapshot.Snapshot;
            var frame = _presentedFrame;
            var camera = frame?.Camera ?? new TacticalMapCameraSnapshot(0, 0, 1, false, false, 1);
            var viewport = frame?.Viewport ?? new TacticalMapViewportSnapshot(0, 0, 1, 0, 0, 0, 0, false);

            var objectFrames = new List<TacticalMapObjectFrame>(_renderStates.Count);
            foreach (var state in _renderStates)
            {
                float screenX = (float)(viewport.Width / 2.0 + (state.Pose.X - camera.FocusX) * camera.PixelsPerWorldUnit);
                float screenY = (float)(viewport.Height / 2.0 + (state.Pose.Y - camera.FocusY) * camera.PixelsPerWorldUnit);
                objectFrames.Add(new(
                    state.Source,
                    state.Predicted,
                    state.IsPlayerShip,
                    screenX,
                    screenY,
                    state.Pose.ObjectId == frame?.Active,
                    state.Pose.ObjectId == frame?.Selected,
                    _presentedClusteredIds.Contains(state.Pose.ObjectId)));
            }

            var trails = _trailStore.Trails
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new TacticalMapTrail(
                    pair.Key,
                    pair.Value.Capacity,
                    pair.Value.Select(point => new TacticalMapPoint(point.X, point.Y, point.Timestamp)).ToArray()))
                .ToArray();

            var trajectories = _capturedTrajectories.Select(t => new TacticalMapTrajectory(t.ObjectId, t.Kind,
                t.Points.Select(p => new TacticalMapPoint(p.X, p.Y)).ToArray())).ToArray();
            var reconciliation = new TacticalMapReconciliationSnapshot(
                _hasSnapshotBaseline,
                _lastSnapshotBaselineSequence,
                _lastSnapshotBaselineGameTimeMs,
                _lastObservedForwardJumpMs,
                _previousRenderSpeed,
                _lastSnapshotBaselineObjects
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => pair.Value)
                    .ToArray(),
                _visualCorrections
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => new TacticalMapCorrection(
                        pair.Key,
                        pair.Value.OffsetX,
                        pair.Value.OffsetY,
                        pair.Value.DirectionOffset,
                        pair.Value.ElapsedSeconds))
                    .ToArray(),
                _pausedVisualAnchors
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => new TacticalMapAnchoredPose(pair.Key, pair.Value.ToSnapshot()))
                    .ToArray());

            var profile = _frameRecorder.Capture();
            if (profile.Frames.Length > 0) profile.Frames[^1] = profile.Frames[^1] with { CaptureRequested = true };
            var document = new TacticalMapSnapshotDocument(
                TacticalMapSnapshotWriter.CurrentSchemaVersion,
                DateTimeOffset.UtcNow,
                new TacticalMapSnapshotState(
                    authoritative,
                    frameTimestamp,
                    prediction?.BufferedSnapshot.ReceivedAtTimestamp,
                    prediction?.EffectivePredictionDeltaMs,
                    prediction is null ? null : GetPredictedGameTimeMs(prediction),
                    prediction?.CurrentSpeed,
                    prediction?.ReconciliationForwardJumpMs,
                    prediction?.TotalReconciliationForwardJumpMs,
                    authoritative?.PlayerShipObjectId,
                    frame?.Active,
                    frame?.Selected,
                    frame?.Navigation,
                    camera,
                    viewport,
                    frame?.Ui ?? new TacticalMapUiSnapshot(false, false, false, 0, 0),
                    _mapSettings with { MetersPerPixel = (double[])_mapSettings.MetersPerPixel.Clone() },
                    objectFrames,
                    trails,
                    trajectories,
                    reconciliation),
                frame?.Id ?? 0,
                _showTrajectoryPrediction,
                profile,
                frame is not null);

            // Only owned DTOs cross the thread boundary, never live render collections/projectors.
            SnapshotSaveTask = SaveSnapshotAsync(document, _tacticalMapSnapshotDirectory, SnapshotWriter, _ioStop.Token);
        }
        catch (Exception ex)
        {
            SnapshotSaveTask = Task.FromResult<string?>(null);
            InterfaceLog.Write($"Tactical map snapshot failed: {ex}");
        }
    }

    private static Task<string?> SaveSnapshotAsync(TacticalMapSnapshotDocument document, string directory,
        Func<TacticalMapSnapshotDocument, string, string>? writer, CancellationToken token) => Task.Run<string?>(() =>
    {
        try
        {
            token.ThrowIfCancellationRequested();
            string path = writer is null ? TacticalMapSnapshotWriter.Write(document, directory, token) : writer(document, directory);
            token.ThrowIfCancellationRequested();
            InterfaceLog.Write($"Tactical map snapshot saved: {path}");
            return path;
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception ex)
        {
            InterfaceLog.Write($"Tactical map snapshot failed: {ex}");
            return null;
        }
    });

    private void CaptureDrawnTrajectory(string objectId, string kind, IReadOnlyList<FutureTrajectoryPoint> points, bool isPlayer = false)
    {
        _profileForecastPointCount += points.Count;
        if (isPlayer && points.Count > 0) _profilePlayerTrajectoryStart = points[0];
        _capturedTrajectories.Add((objectId, kind, points));
    }
}
