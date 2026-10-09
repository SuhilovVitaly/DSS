using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    private readonly List<(string ObjectId, string Kind, IReadOnlyList<FutureTrajectoryPoint> Points)> _capturedTrajectories = new();
    private sealed record PresentedFrame(SnapshotPrediction? Prediction, long Timestamp, long Id,
        TacticalMapCameraSnapshot Camera, TacticalMapViewportSnapshot Viewport, TacticalMapUiSnapshot Ui,
        string? Active, string? Selected, string? Navigation, TacticalMapSceneGeometry Scene,
        ImmutableArray<(string Id, ObjectTrailBuffer.Frozen Points)> Trails);
    private PresentedFrame? _presentedFrame;
    private PresentedFrame? _preparedPresentation;
    internal long PresentedFrameId => _presentedFrame?.Id ?? 0;
    internal long LastInputFrameId { get; private set; }
    private void SealPresentedFrame(SnapshotPrediction? prediction, long timestamp) => _presentedFrame = _preparedPresentation;
    internal Task<string?> SnapshotSaveTask { get; private set; } = Task.FromResult<string?>(null);
    private readonly CancellationTokenSource _ioStop = new();
    internal Func<TacticalMapSnapshotDocument, string, string>? SnapshotWriter { get; set; }
    internal string? LastTacticalMapSnapshotPath => SnapshotSaveTask.IsCompletedSuccessfully ? SnapshotSaveTask.Result : null;

    internal void RequestTacticalMapSnapshot()
    {
        // One detached document at a time; repeated clicks cannot grow a writer queue.
        if (!_disposed && SnapshotSaveTask.IsCompleted) CaptureTacticalMapSnapshot(_presentedFrame?.Prediction, _presentedFrame?.Timestamp ?? 0);
    }

    private void PreparePresentation(SnapshotPrediction? prediction, long timestamp)
    {
        var scene = PreparedScene!;
        var view = scene.View;
        var trails = _trailStore.Trails.OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => (p.Key, p.Value.Freeze())).ToImmutableArray();
        _preparedPresentation = new(prediction, timestamp, scene.Frame.FrameId,
            new(view.Camera.X, view.Camera.Y, view.Camera.PixelsPerWorldUnit, _isFocusAttachedToPlayer,
                _zoomTransition.Active, _zoomTransition.TargetPpu(_camera)),
            new(view.Width, view.Height, view.UiScale, _mouseX, _mouseY, _uiMouseX, _uiMouseY, _hasMousePosition),
            new(_panelVisible, _isPanningMap, IsCtrlDown, view.Clusters.Length, view.ClusteredIds.Count),
            view.Active, view.Selected, view.Navigation, scene, trails);
    }

    private void CaptureTacticalMapSnapshot(SnapshotPrediction? prediction, long frameTimestamp)
    {
        try
        {
            AuthoritativeSnapshot? authoritative = prediction?.BufferedSnapshot.Snapshot;
            var frame = _presentedFrame;
            var camera = frame?.Camera ?? new TacticalMapCameraSnapshot(0, 0, 1, false, false, 1);
            var viewport = frame?.Viewport ?? new TacticalMapViewportSnapshot(0, 0, 1, 0, 0, 0, 0, false);

            var objectFrames = new List<TacticalMapObjectFrame>(frame?.Scene.Frame.Objects.Length ?? 0);
            foreach (var state in frame?.Scene.Frame.Objects ?? [])
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
                    frame!.Scene.View.ClusteredIds.Contains(state.Pose.ObjectId)));
            }

            var trails = (frame?.Trails ?? []).Select(t => new TacticalMapTrail(t.Id, t.Points.Capacity,
                t.Points.Select(p => new TacticalMapPoint(p.X, p.Y, p.Timestamp)).ToArray())).ToArray();
            var trajectories = (frame?.Scene.View.Paths ?? []).Select(t => new TacticalMapTrajectory(t.ObjectId, t.Kind,
                t.WorldPoints.Select(p => new TacticalMapPoint(p.X, p.Y)).ToArray())).ToArray();
            var source = frame?.Scene.Frame.Reconciliation;
            var reconciliation = new TacticalMapReconciliationSnapshot(
                source?.HasBaseline ?? false, source?.Sequence ?? 0, source?.MotionTime ?? 0,
                source?.ForwardJump ?? 0, source?.PreviousSpeed ?? SimulationSpeed.Speed1,
                source?.Baseline ?? [],
                source?.Corrections.OrderBy(p => p.Key, StringComparer.Ordinal)
                    .Select(p => new TacticalMapCorrection(p.Key, p.Value.OffsetX, p.Value.OffsetY,
                        p.Value.DirectionOffset, p.Value.ElapsedSeconds)).ToArray() ?? [],
                source?.Anchors.OrderBy(p => p.Key, StringComparer.Ordinal)
                    .Select(p => new TacticalMapAnchoredPose(p.Key, p.Value.ToSnapshot())).ToArray() ?? []);

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
