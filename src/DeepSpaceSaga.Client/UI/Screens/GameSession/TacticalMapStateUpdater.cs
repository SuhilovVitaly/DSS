using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>Single writer for presentation motion. No rendering, clocks, I/O or commands.</summary>
internal sealed class TacticalMapStateUpdater
{
    private readonly IMotionPredictor _predictor;
    private readonly List<ObjectRenderState> _renderStates = new();
    private readonly HashSet<string> _combatPoseObjectIds = new(StringComparer.Ordinal);
    private readonly List<string> _diagnostics = new();
    private long _frameId;
    private ImmutableArray<ObjectRenderState> _publishedObjects = [];
    private ImmutableDictionary<string, RenderMotion> _publishedAnchors = ImmutableDictionary<string, RenderMotion>.Empty;
    private ImmutableDictionary<string, VisualCorrection> _publishedCorrections = ImmutableDictionary<string, VisualCorrection>.Empty;
    internal int PoseDtoMaterializations { get; private set; }
    internal long ContactMembershipBuilds { get; private set; }
    internal string? PreviewTargetId { get; set; }
    internal string? ProfileTargetId { get; set; }
    private RenderMotion? _profilePlayerRaw;
    private RenderMotion? _profileTargetRaw;
    internal bool CaptureDiagnostics { get; set; }

    internal IReadOnlyDictionary<string, RenderMotion> PausedVisualAnchors => _pausedVisualAnchors;
    internal IReadOnlyDictionary<string, VisualCorrection> VisualCorrections => _visualCorrections;
    internal IReadOnlyDictionary<string, ObjectMotionSnapshot> BaselineObjects => _lastSnapshotBaselineObjects;
    internal ulong BaselineSequence => _lastSnapshotBaselineSequence;
    internal long BaselineMotionTimeMs => _lastSnapshotBaselineGameTimeMs;
    internal long ObservedForwardJumpMs => _lastObservedForwardJumpMs;
    internal bool HasBaseline => _hasSnapshotBaseline;
    internal IReadOnlySet<string> ContactIds => _currentVisualObjectIds;
    internal SimulationSpeed PreviousSpeed => _previousRenderSpeed;

    internal TacticalMapStateUpdater(IMotionPredictor predictor) => _predictor = predictor;

    private TacticalMapFrameState Publish(SnapshotPrediction? prediction, long timestamp, bool rebased)
    {
        if (!_publishedObjects.SequenceEqual(_renderStates)) _publishedObjects = _renderStates.ToImmutableArray();
        if (_publishedAnchors.Count != _pausedVisualAnchors.Count ||
            _pausedVisualAnchors.Any(p => !_publishedAnchors.TryGetValue(p.Key, out var value) || value != p.Value))
            _publishedAnchors = _pausedVisualAnchors.ToImmutableDictionary(StringComparer.Ordinal);
        if (_publishedCorrections.Count != _visualCorrections.Count ||
            _visualCorrections.Any(p => !_publishedCorrections.TryGetValue(p.Key, out var value) || value != p.Value))
            _publishedCorrections = _visualCorrections.ToImmutableDictionary(StringComparer.Ordinal);
        return new(++_frameId, timestamp, prediction, _publishedObjects, rebased,
            _profilePlayerRaw, _profileTargetRaw, _diagnostics.ToImmutableArray())
        {
            Reconciliation = new(_hasSnapshotBaseline, _lastSnapshotBaselineSequence, _lastSnapshotBaselineGameTimeMs,
                _lastObservedForwardJumpMs, _previousRenderSpeed, prediction?.BufferedSnapshot.Snapshot.Objects ?? [],
                _publishedCorrections, _publishedAnchors)
        };
    }

    private static long CombatPredictionDelta(SnapshotPrediction prediction) =>
        prediction.CurrentSpeed == SimulationSpeed.Speed0 && prediction.BufferedSnapshot.Snapshot.CurrentSpeed == SimulationSpeed.Speed0
            ? 0 : prediction.EffectivePredictionDeltaMs;

    private readonly Dictionary<string, RenderMotion> _pausedVisualAnchors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, VisualCorrection> _visualCorrections = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ObjectMotionSnapshot> _lastSnapshotBaselineObjects = new(StringComparer.Ordinal);
    private ulong _lastSnapshotBaselineSequence;
    private long _lastSnapshotBaselineGameTimeMs;
    private long _lastObservedForwardJumpMs;
    private bool _hasSnapshotBaseline;
    private readonly HashSet<string> _currentVisualObjectIds = new(StringComparer.Ordinal);
    private readonly List<string> _visualObjectIdsToRemove = new();
    private SimulationSpeed _previousRenderSpeed = SimulationSpeed.Speed1;
    internal const double VisualReconciliationDurationSeconds = 0.3;
    private const double ReconciliationCorrectionToleranceWorldUnitsSq = 0.25; // 0.5 world unit (~50 m)
    private const double ReconciliationCorrectionToleranceDegrees = 0.25;

    internal TacticalMapFrameState Update(SnapshotPrediction? prediction, long timestamp, double deltaSeconds)
    {
        PoseDtoMaterializations = 0;
        _profilePlayerRaw = _profileTargetRaw = null;
        _diagnostics.Clear();
        bool rebased = false;
        if (prediction is null)
        {
            _renderStates.Clear();
            return Publish(prediction, timestamp, rebased);
        }

        bool isPaused = prediction.CurrentSpeed == SimulationSpeed.Speed0;
        bool enteringPause = isPaused && _previousRenderSpeed != SimulationSpeed.Speed0;
        bool resuming = !isPaused && _previousRenderSpeed == SimulationSpeed.Speed0;

        if (enteringPause)
        {
            _pausedVisualAnchors.Clear();
            for (int i = 0; i < _renderStates.Count; i++)
            {
                var state = _renderStates[i];
                _pausedVisualAnchors[state.Pose.ObjectId] = state.Pose;
            }

            _visualCorrections.Clear();
        }

        _renderStates.Clear();

        long ed = prediction.EffectivePredictionDeltaMs;
        var snapshot = prediction.BufferedSnapshot.Snapshot;
        bool membershipChanged = !_hasSnapshotBaseline || snapshot.SnapshotSequence != _lastSnapshotBaselineSequence;
        if (membershipChanged)
        {
            _currentVisualObjectIds.Clear();
            ContactMembershipBuilds++;
        }
        string? playerShipObjectId = snapshot.PlayerShipObjectId;
        UpdateCombatPoseObjects(snapshot);
        long combatDelta = CombatPredictionDelta(prediction);

        // A fresh authoritative snapshot can reveal that the object's real trajectory
        // (velocity/heading) differed from what the client had been extrapolating from
        // the PREVIOUS snapshot — e.g. an engine command or turn cycle progressed while
        // paused/off-screen, or the engine's and client's clocks simply disagree by a few
        // ms (amplified hugely at Speed4). Either way, "what the client was already
        // showing, carried forward to the same target time" is the previous baseline
        // object extrapolated to now — NOT the new snapshot's own object (which is the
        // discontinuity itself, not a continuity reference). Must apply exactly once (the
        // first frame that observes this snapshot as latest) and smooth like a resume
        // correction, otherwise it snaps instantly on whichever frame receives it — not
        // necessarily the pause/resume transition frame at all.
        bool newSnapshotArrived = _hasSnapshotBaseline && snapshot.SnapshotSequence != _lastSnapshotBaselineSequence;
        if (isPaused && !enteringPause && newSnapshotArrived && snapshot.MotionTimeMs != _lastSnapshotBaselineGameTimeMs)
        {
            // A station action can advance the authoritative world while Speed0 stays
            // selected. This is a new physical baseline, not pause/resume smoothing.
            _pausedVisualAnchors.Clear();
            _visualCorrections.Clear();
            rebased = true;
        }
        long targetGameTimeMs = snapshot.MotionTimeMs + ed;

        foreach (var obj in snapshot.Objects)
        {
            bool hasCombatPose = _combatPoseObjectIds.Contains(obj.ObjectId);
            var predicted = PredictRenderMotion(obj, hasCombatPose ? combatDelta : ed);
            if (obj.ObjectId == playerShipObjectId) _profilePlayerRaw = predicted;
            if (obj.ObjectId == ProfileTargetId) _profileTargetRaw = predicted;
            if (membershipChanged) _currentVisualObjectIds.Add(obj.ObjectId);

            // Combat participants share their confirmed display time with the launch
            // preview and target path. A frozen/corrected marker would detach the line.
            if (hasCombatPose || predicted.HasAbsoluteOrbit)
            {
                _visualCorrections.Remove(obj.ObjectId);
                _pausedVisualAnchors.Remove(obj.ObjectId);
            }
            else if (isPaused)
            {
                if (!_pausedVisualAnchors.TryGetValue(obj.ObjectId, out var anchor))
                {
                    anchor = predicted;
                    _pausedVisualAnchors[obj.ObjectId] = anchor;
                }

                predicted = ApplyVisualPose(predicted, anchor);
            }
            else
            {
                bool correctionCreated = false;
                if (resuming && _pausedVisualAnchors.TryGetValue(obj.ObjectId, out var anchor))
                {
                    var newCorrection = CreateVisualCorrection(anchor, predicted);
                    if (newCorrection.HasOffset)
                    {
                        _visualCorrections[obj.ObjectId] = newCorrection;
                        correctionCreated = true;
                    }
                }
                else if (newSnapshotArrived &&
                         _lastSnapshotBaselineObjects.TryGetValue(obj.ObjectId, out var prevBaseline))
                {
                    long unseenForwardJump = prediction.TotalReconciliationForwardJumpMs - _lastObservedForwardJumpMs;
                    long elapsedFromPrevBaseline = targetGameTimeMs - unseenForwardJump - _lastSnapshotBaselineGameTimeMs;
                    var continuityExpected = elapsedFromPrevBaseline > 0
                        ? PredictRenderMotion(prevBaseline, elapsedFromPrevBaseline)
                        : new RenderMotion(prevBaseline);

                    // Carry any unfinished correction into this rebase; otherwise a
                    // second snapshot during smoothing would snap back to raw motion.
                    if (_visualCorrections.TryGetValue(obj.ObjectId, out var previousCorrection))
                        continuityExpected = ApplyVisualCorrection(continuityExpected,
                            previousCorrection with { ElapsedSeconds = previousCorrection.ElapsedSeconds + deltaSeconds });
                    var newCorrection = CreateVisualCorrection(continuityExpected, predicted);
                    if (IsMeaningfulCorrection(newCorrection))
                    {
                        _visualCorrections[obj.ObjectId] = newCorrection;
                        correctionCreated = true;
                    }
                    else
                    {
                        // The new baseline may already include the old correction.
                        // Keeping it here would apply that offset for a second time.
                        _visualCorrections.Remove(obj.ObjectId);
                    }
                }

                if (_visualCorrections.TryGetValue(obj.ObjectId, out var correction))
                {
                    if (!correctionCreated)
                        correction = correction with { ElapsedSeconds = correction.ElapsedSeconds + deltaSeconds };

                    predicted = ApplyVisualCorrection(predicted, correction);
                    if (correction.ElapsedSeconds >= VisualReconciliationDurationSeconds)
                        _visualCorrections.Remove(obj.ObjectId);
                    else
                        _visualCorrections[obj.ObjectId] = correction;
                }
            }

            if (CaptureDiagnostics && obj.ObjectId == playerShipObjectId &&
                (enteringPause || resuming || newSnapshotArrived ||
                 _visualCorrections.ContainsKey(obj.ObjectId) || isPaused))
            {
                _diagnostics.Add(
                    $"OBJECT id={obj.ObjectId} isPaused={isPaused} enteringPause={enteringPause} resuming={resuming} " +
                    $"newSnapshotArrived={newSnapshotArrived} " +
                    $"snapSeq={snapshot.SnapshotSequence} snapGameTimeMs={snapshot.MotionTimeMs} ed={ed} " +
                    $"authX={obj.X:F3} authY={obj.Y:F3} authDir={obj.Direction:F3} " +
                    $"visualX={predicted.X:F3} visualY={predicted.Y:F3} visualDir={predicted.Direction:F3} " +
                    $"turnStepDeg={obj.TurnStepDegrees} turnStepRemainingMs={obj.TurnStepRemainingMs} " +
                    $"correctionActive={_visualCorrections.ContainsKey(obj.ObjectId)}");
            }

            if (membershipChanged) _lastSnapshotBaselineObjects[obj.ObjectId] = obj;

            // Every client-visible contact stays available. Labels, compact markers and
            // clusters reduce detail without removing selected/navigation targets.

            _renderStates.Add(new ObjectRenderState(obj, predicted, obj.ObjectId == playerShipObjectId));
        }

        if (membershipChanged)
        {
            RemoveMissingVisualStates(_pausedVisualAnchors);
            RemoveMissingVisualStates(_visualCorrections);
            RemoveMissingVisualStates(_lastSnapshotBaselineObjects);
        }

        if (resuming)
            _pausedVisualAnchors.Clear();

        _lastObservedForwardJumpMs = prediction.TotalReconciliationForwardJumpMs;
        _lastSnapshotBaselineGameTimeMs = snapshot.MotionTimeMs;
        _lastSnapshotBaselineSequence = snapshot.SnapshotSequence;
        _hasSnapshotBaseline = true;

        _previousRenderSpeed = prediction.CurrentSpeed;
        return Publish(prediction, timestamp, rebased);
    }

    private RenderMotion PredictRenderMotion(ObjectMotionSnapshot state, long elapsedMs)
    {
        if (elapsedMs == 0 && state.Orbit is null) return new(state);
        if (_predictor is LinearMotionPredictor &&
            LinearMotionPredictor.TryPredictLinearPosition(state, elapsedMs, out double x, out double y))
            return new(state, x, y, state.Direction);
        PoseDtoMaterializations++;
        return new(_predictor.Predict(state, elapsedMs));
    }
    private static RenderMotion ApplyVisualPose(
        RenderMotion target,
        RenderMotion visualPose)
    {
        if (target.X == visualPose.X && target.Y == visualPose.Y && target.Direction == visualPose.Direction)
            return target;
        return target with
        {
            X = visualPose.X,
            Y = visualPose.Y,
            Direction = visualPose.Direction
        };
    }

    private static VisualCorrection CreateVisualCorrection(
        RenderMotion visualPose,
        RenderMotion target)
    {
        return new VisualCorrection(
            visualPose.X - target.X,
            visualPose.Y - target.Y,
            ShortestDirectionDelta(visualPose.Direction, target.Direction),
            ElapsedSeconds: 0);
    }

    private static RenderMotion ApplyVisualCorrection(
        RenderMotion target,
        VisualCorrection correction)
    {
        double progress = Math.Clamp(
            correction.ElapsedSeconds / VisualReconciliationDurationSeconds,
            0,
            1);
        double smoothProgress = progress * progress * (3 - 2 * progress);
        double remaining = 1 - smoothProgress;

        return target with
        {
            X = target.X + correction.OffsetX * remaining,
            Y = target.Y + correction.OffsetY * remaining,
            Direction = NormalizeDirection(target.Direction + correction.DirectionOffset * remaining)
        };
    }

    private static bool IsMeaningfulCorrection(VisualCorrection correction)
    {
        double distanceSq = correction.OffsetX * correction.OffsetX + correction.OffsetY * correction.OffsetY;
        return distanceSq > ReconciliationCorrectionToleranceWorldUnitsSq ||
               Math.Abs(correction.DirectionOffset) > ReconciliationCorrectionToleranceDegrees;
    }

    private static double ShortestDirectionDelta(double visualDirection, double targetDirection)
    {
        double delta = (visualDirection - targetDirection) % 360;
        if (delta > 180)
            delta -= 360;
        else if (delta < -180)
            delta += 360;

        return delta;
    }

    private static double NormalizeDirection(double direction)
    {
        double normalized = direction % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }

    private void RemoveMissingVisualStates<T>(Dictionary<string, T> states)
    {
        _visualObjectIdsToRemove.Clear();
        foreach (string objectId in states.Keys)
        {
            if (!_currentVisualObjectIds.Contains(objectId))
                _visualObjectIdsToRemove.Add(objectId);
        }

        for (int i = 0; i < _visualObjectIdsToRemove.Count; i++)
            states.Remove(_visualObjectIdsToRemove[i]);
    }

    private void UpdateCombatPoseObjects(AuthoritativeSnapshot snapshot)
    {
        _combatPoseObjectIds.Clear();
        foreach (var obj in snapshot.Objects)
        {
            if (obj.Countermeasure is { } defenseFlight)
            {
                _combatPoseObjectIds.Add(obj.ObjectId);
                _combatPoseObjectIds.Add(defenseFlight.TargetTorpedoId);
            }
            if (obj.Torpedo is not { } flight) continue;
            _combatPoseObjectIds.Add(obj.ObjectId);
            _combatPoseObjectIds.Add(flight.TargetObjectId);
        }
        if (PreviewTargetId is not null)
        {
            if (snapshot.PlayerShipObjectId is { } playerId) _combatPoseObjectIds.Add(playerId);
            if (PreviewTargetId is { } targetId) _combatPoseObjectIds.Add(targetId);
        }
    }

    internal readonly record struct VisualCorrection(
        double OffsetX,
        double OffsetY,
        double DirectionOffset,
        double ElapsedSeconds)
    {
        internal bool HasOffset => OffsetX != 0 || OffsetY != 0 || DirectionOffset != 0;
    }
}
