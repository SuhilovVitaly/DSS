using System.Diagnostics;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    private readonly TacticalMapFrameRecorder _frameRecorder = new();
    private long _profileFrameId;
    private long _profilePreviousTimestamp;
    private long _profileStartedAt;
    private long _profileStageStartedAt;
    private long _profileAllocatedAtStart;
    private long _profilePreviousAllocated;
    private int _profileGen0 = GC.CollectionCount(0);
    private int _profileGen1 = GC.CollectionCount(1);
    private int _profileGen2 = GC.CollectionCount(2);
    private TimeSpan _profileGcPause = GC.GetTotalPauseDuration();
    private double _profilePreviousCameraX;
    private double _profilePreviousCameraY;
    private ulong? _profilePreviousSequence;
    private TacticalMapStageTimes _profileStages;
    private RenderMotion? _profilePlayerRaw;
    private RenderMotion? _profileTargetRaw;
    private string? _profileTargetId;
    private FutureTrajectoryPoint? _profilePlayerTrajectoryStart;
    private int _profileForecastPointCount;

    private void BeginFrameProfile()
    {
        _profileStartedAt = _profileStageStartedAt = Stopwatch.GetTimestamp();
        _profileAllocatedAtStart = GC.GetAllocatedBytesForCurrentThread();
        _profileStages = default;
        _profilePlayerTrajectoryStart = null;
        _profileForecastPointCount = 0;
        _profilePlayerRaw = _profileTargetRaw = null;
        _profileTargetId = _selectedObjectId ?? _navigationTargetId ?? _activeObjectId;
        _captureThisFrame = _snapshotCaptureRequested;
        _snapshotCaptureRequested = false;
        _capturedTrajectories.Clear();
    }

    private void CompleteRenderStage(string stage)
    {
        long now = Stopwatch.GetTimestamp();
        double ms = Stopwatch.GetElapsedTime(_profileStageStartedAt, now).TotalMilliseconds;
        _profileStageStartedAt = now;
        _profileStages = stage switch
        {
            "coordinates_and_hit_test" => _profileStages with { CoordinatesAndHitTestMs = ms },
            "grid" => _profileStages with { GridMs = ms },
            "trails" => _profileStages with { TrailsMs = ms },
            "forecasts" => _profileStages with { ForecastsMs = ms },
            "markers_and_labels" => _profileStages with { MarkersAndLabelsMs = ms },
            "command_panel" => _profileStages with { CommandPanelMs = ms },
            "info_panels" => _profileStages with { InfoPanelsMs = ms },
            _ => _profileStages
        };
        RenderStageCompleted?.Invoke(stage);
    }

    private void FinishFrameProfile(SnapshotPrediction? prediction, long timestamp)
    {
        var snapshot = prediction?.BufferedSnapshot.Snapshot;
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
        TimeSpan pause = GC.GetTotalPauseDuration();
        double cameraStep = _profileFrameId == 0 ? 0 : Math.Sqrt(
            Math.Pow(_camera.FocusX - _profilePreviousCameraX, 2) +
            Math.Pow(_camera.FocusY - _profilePreviousCameraY, 2)) * _camera.PixelsPerWorldUnit;
        double maxCorrection = 0;
        foreach (var correction in _visualCorrections.Values)
        {
            double progress = Math.Clamp(correction.ElapsedSeconds / VisualReconciliationDurationSeconds, 0, 1);
            double remaining = 1 - progress * progress * (3 - 2 * progress);
            maxCorrection = Math.Max(maxCorrection,
                Math.Sqrt(correction.OffsetX * correction.OffsetX + correction.OffsetY * correction.OffsetY) * remaining);
        }
        TacticalMapTrackedObject? player = null, target = null;
        foreach (var state in _renderStates)
        {
            if (state.IsPlayerShip) player = TrackProfileObject(state, _profilePlayerRaw);
            if (state.Pose.ObjectId == _profileTargetId) target = TrackProfileObject(state, _profileTargetRaw);
        }
        _frameRecorder.Add(new(
            ++_profileFrameId, timestamp,
            _profileFrameId == 1 ? 0 : (timestamp - _profilePreviousTimestamp) * 1000.0 / Stopwatch.Frequency,
            Stopwatch.GetElapsedTime(_profileStartedAt).TotalMilliseconds, _profileStages,
            allocated - _profileAllocatedAtStart,
            _profileFrameId == 1 ? 0 : _profileAllocatedAtStart - _profilePreviousAllocated,
            gen0 - _profileGen0, gen1 - _profileGen1, gen2 - _profileGen2, (pause - _profileGcPause).TotalMilliseconds,
            snapshot?.SnapshotSequence, snapshot?.SnapshotSequence != _profilePreviousSequence,
            prediction?.BufferedSnapshot.ReceivedAtTimestamp,
            prediction is null ? null : (timestamp - prediction.BufferedSnapshot.ReceivedAtTimestamp) * 1000.0 / Stopwatch.Frequency,
            snapshot?.MotionTimeMs, snapshot?.GameTimeMs, prediction?.EffectivePredictionDeltaMs,
            prediction is null ? null : GetPredictedGameTimeMs(prediction), prediction?.CurrentSpeed,
            prediction?.ReconciliationForwardJumpMs, prediction?.TotalReconciliationForwardJumpMs,
            _camera.FocusX, _camera.FocusY, cameraStep, _camera.PixelsPerWorldUnit,
            _isFocusAttachedToPlayer, _zoomTransition.Active, _isPanningMap, _viewportW, _viewportH,
            _renderStates.Count, _mapClusters.Count, TrailStatistics.Points, _profileForecastPointCount,
            _profilePlayerTrajectoryStart?.X, _profilePlayerTrajectoryStart?.Y,
            DisplayedPlayerTrajectoryEnd?.X, DisplayedPlayerTrajectoryEnd?.Y, _visualCorrections.Count,
            maxCorrection * _camera.PixelsPerWorldUnit, player, target, _captureThisFrame, !SnapshotSaveTask.IsCompleted));
        _profilePreviousTimestamp = timestamp;
        _profilePreviousAllocated = _profileAllocatedAtStart;
        _profilePreviousCameraX = _camera.FocusX;
        _profilePreviousCameraY = _camera.FocusY;
        _profilePreviousSequence = snapshot?.SnapshotSequence;
        _profileGen0 = gen0;
        _profileGen1 = gen1;
        _profileGen2 = gen2;
        _profileGcPause = pause;
        if (_captureThisFrame)
        {
            long start = Stopwatch.GetTimestamp();
            CaptureTacticalMapSnapshot(prediction, timestamp);
            _frameRecorder.RecordCaptureCost(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            _captureThisFrame = false;
            _capturedTrajectories.Clear();
        }
    }

    private TacticalMapTrackedObject TrackProfileObject(ObjectRenderState state, RenderMotion? raw)
    {
        var pose = state.Pose;
        var (x, y) = _camera.WorldToScreen(pose.X, pose.Y, _viewportW, _viewportH);
        _visualCorrections.TryGetValue(pose.ObjectId, out var correction);
        return new(pose.ObjectId, state.Source.X, state.Source.Y, state.Source.Direction,
            raw?.X ?? pose.X, raw?.Y ?? pose.Y, raw?.Direction ?? pose.Direction,
            pose.X, pose.Y, pose.Direction, pose.SpeedKmS, x, y, pose.ActiveEngineCommandType,
            pose.ApproachRoute?.ElapsedMs, pose.ApproachRoute?.DurationMs,
            correction.OffsetX, correction.OffsetY, correction.DirectionOffset, correction.ElapsedSeconds);
    }

    internal void CompleteWindowProfile(TacticalMapWindowTiming timing) => _frameRecorder.CompleteWindow(timing);
}
