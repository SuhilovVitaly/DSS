using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    private bool _snapshotCaptureRequested;
    private bool _captureThisFrame;
    private readonly List<TacticalMapTrajectory> _capturedTrajectories = new();
    internal Task<string?> SnapshotSaveTask { get; private set; } = Task.FromResult<string?>(null);
    internal Func<TacticalMapSnapshotDocument, string, string> SnapshotWriter { get; set; } = TacticalMapSnapshotWriter.Write;
    internal string? LastTacticalMapSnapshotPath => SnapshotSaveTask.IsCompletedSuccessfully ? SnapshotSaveTask.Result : null;

    private void RequestTacticalMapSnapshot()
    {
        // One detached document at a time; repeated clicks cannot grow a writer queue.
        if (SnapshotSaveTask.IsCompleted) _snapshotCaptureRequested = true;
    }

    private void CaptureTacticalMapSnapshot(SnapshotPrediction? prediction, long frameTimestamp)
    {
        try
        {
            AuthoritativeSnapshot? authoritative = prediction?.BufferedSnapshot.Snapshot;

            var objectFrames = new List<TacticalMapObjectFrame>(_renderStates.Count);
            foreach (var state in _renderStates)
            {
                var (screenX, screenY) = _camera.WorldToScreen(state.Pose.X, state.Pose.Y, _viewportW, _viewportH);
                objectFrames.Add(new(
                    state.Source,
                    state.Predicted,
                    state.IsPlayerShip,
                    screenX,
                    screenY,
                    state.Pose.ObjectId == _activeObjectId,
                    state.Pose.ObjectId == _selectedObjectId,
                    _clusteredObjectIds.Contains(state.Pose.ObjectId)));
            }

            var trails = _trailStore.Trails
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new TacticalMapTrail(
                    pair.Key,
                    pair.Value.Capacity,
                    pair.Value.Select(point => new TacticalMapPoint(point.X, point.Y, point.Timestamp)).ToArray()))
                .ToArray();

            var trajectories = _capturedTrajectories.ToArray();
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
                    _activeObjectId,
                    _selectedObjectId,
                    _navigationTargetId,
                    new TacticalMapCameraSnapshot(
                        _camera.FocusX,
                        _camera.FocusY,
                        _camera.PixelsPerWorldUnit,
                        _isFocusAttachedToPlayer,
                        _zoomTransition.Active,
                        _zoomTransition.TargetPpu(_camera)),
                    new TacticalMapViewportSnapshot(
                        _viewportW,
                        _viewportH,
                        _uiScale,
                        _mouseX,
                        _mouseY,
                        _uiMouseX,
                        _uiMouseY,
                        _hasMousePosition),
                    new TacticalMapUiSnapshot(
                        _panelVisible,
                        _isPanningMap,
                        IsCtrlDown,
                        _mapClusters.Count,
                        _clusteredObjectIds.Count),
                    _mapSettings with { MetersPerPixel = (double[])_mapSettings.MetersPerPixel.Clone() },
                    objectFrames,
                    trails,
                    trajectories,
                    reconciliation),
                _profileFrameId,
                _showTrajectoryPrediction,
                _frameRecorder.Capture());

            // Only owned DTOs cross the thread boundary, never live render collections/projectors.
            SnapshotSaveTask = SaveSnapshotAsync(document, _tacticalMapSnapshotDirectory, SnapshotWriter);
        }
        catch (Exception ex)
        {
            SnapshotSaveTask = Task.FromResult<string?>(null);
            InterfaceLog.Write($"Tactical map snapshot failed: {ex}");
        }
    }

    private static Task<string?> SaveSnapshotAsync(TacticalMapSnapshotDocument document, string directory,
        Func<TacticalMapSnapshotDocument, string, string> writer) => Task.Run<string?>(() =>
    {
        try
        {
            string path = writer(document, directory);
            InterfaceLog.Write($"Tactical map snapshot saved: {path}");
            return path;
        }
        catch (Exception ex)
        {
            InterfaceLog.Write($"Tactical map snapshot failed: {ex}");
            return null;
        }
    });

    private void CaptureDrawnTrajectory(string objectId, string kind, List<FutureTrajectoryPoint> points, bool isPlayer = false)
    {
        _profileForecastPointCount += points.Count;
        if (isPlayer && points.Count > 0) _profilePlayerTrajectoryStart = points[0];
        if (!_captureThisFrame) return;
        _capturedTrajectories.Add(new(objectId, kind,
            points.Select(point => new TacticalMapPoint(point.X, point.Y)).ToArray()));
    }
}
