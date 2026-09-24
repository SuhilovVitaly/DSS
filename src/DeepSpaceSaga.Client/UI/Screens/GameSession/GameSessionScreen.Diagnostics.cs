using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    private string? _lastTacticalMapSnapshotPath;

    internal string? LastTacticalMapSnapshotPath => _lastTacticalMapSnapshotPath;

    private void CaptureTacticalMapSnapshot()
    {
        try
        {
            SnapshotPrediction? prediction = _buffer.LatestPrediction;
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

            var trajectories = CaptureTrajectories();
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
                    _timestampProvider(),
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
                    reconciliation));

            _lastTacticalMapSnapshotPath = TacticalMapSnapshotWriter.Write(document, _tacticalMapSnapshotDirectory);
            InterfaceLog.Write($"Tactical map snapshot saved: {_lastTacticalMapSnapshotPath}");
        }
        catch (Exception ex)
        {
            _lastTacticalMapSnapshotPath = null;
            InterfaceLog.Write($"Tactical map snapshot failed: {ex}");
        }
    }

    private TacticalMapTrajectory[] CaptureTrajectories()
    {
        var trajectories = new List<TacticalMapTrajectory>();
        foreach (var state in _renderStates)
        {
            bool isTarget = state.Pose.ObjectId == _activeObjectId ||
                            state.Pose.ObjectId == _selectedObjectId ||
                            state.Pose.ObjectId == _navigationTargetId;
            if (!state.IsPlayerShip && !isTarget)
                continue;

            List<FutureTrajectoryPoint> points;
            string kind;
            if (state.IsPlayerShip && state.Pose.NavigationTargetX is not null)
            {
                points = _navigationTrajectoryProjector.Project(state.Pose.ToSnapshot());
                kind = "navigation";
            }
            else if (FutureTrajectoryProjector.ShouldDraw(state.Pose.ToSnapshot()))
            {
                points = _futureTrajectoryProjector.Project(state.Pose.ToSnapshot());
                kind = "future";
            }
            else
            {
                continue;
            }

            trajectories.Add(new(
                state.Pose.ObjectId,
                kind,
                points.Select(point => new TacticalMapPoint(point.X, point.Y)).ToArray()));
        }

        return trajectories.ToArray();
    }
}
