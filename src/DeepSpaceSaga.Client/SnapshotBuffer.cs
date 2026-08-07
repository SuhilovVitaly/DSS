using System.Diagnostics;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client;

public sealed record SnapshotPrediction(
    BufferedSnapshot BufferedSnapshot,
    long EffectivePredictionDeltaMs,
    SimulationSpeed CurrentSpeed,
    long ReconciliationForwardJumpMs);

/// <summary>
/// Thread-safe holder for the latest authoritative snapshot.
/// Snapshot and its local receipt timestamp are published atomically,
/// so renderer always reads a consistent pair.
/// Also tracks the client-side authoritative simulation speed
/// (updated immediately on speed changes, without waiting for the next snapshot).
/// </summary>
public sealed class SnapshotBuffer
{
    private readonly object _sync = new();
    private readonly Func<long> _timestampProvider;
    private BufferedSnapshot? _latest;
    private SimulationSpeed _currentSpeed = SimulationSpeed.Speed1;
    private SimulationSpeed? _pendingConfirmedSpeed;
    private long _predictionSegmentStartedAtTimestamp;
    private long _accumulatedPredictionGameTimeMs;
    private long _lastReconciliationForwardJumpMs;
    private bool _awaitingFirstSnapshotAfterResume;

    public SnapshotBuffer()
        : this(Stopwatch.GetTimestamp)
    {
    }

    internal SnapshotBuffer(Func<long> timestampProvider)
    {
        _timestampProvider = timestampProvider;
        _predictionSegmentStartedAtTimestamp = timestampProvider();
    }

    /// <summary>
    /// Client-side authoritative simulation speed.
    /// Updated immediately after a confirmed speed change so the renderer
    /// can gate prediction without waiting for the next 1 Hz snapshot.
    /// </summary>
    public SimulationSpeed CurrentSpeed
    {
        get
        {
            lock (_sync)
            {
                return _currentSpeed;
            }
        }
        set => SetCurrentSpeed(value);
    }

    /// <summary>Atomically replace the current snapshot with receipt timestamp.</summary>
    public void Update(AuthoritativeSnapshot snapshot)
    {
        long now = _timestampProvider();
        var value = new BufferedSnapshot(snapshot, now);

        lock (_sync)
        {
            long previousPredictedGameTimeMs = snapshot.GameTimeMs;
            if (_latest is not null)
            {
                long previousPredictionDeltaMs = _accumulatedPredictionGameTimeMs
                    + RealTicksToGameMs(now - _predictionSegmentStartedAtTimestamp, _currentSpeed);
                previousPredictedGameTimeMs = _latest.Snapshot.GameTimeMs + previousPredictionDeltaMs;
            }

            _latest = value;

            // A newer authoritative baseline must not make visual game time run backward.
            long rawDeltaMs = previousPredictedGameTimeMs - snapshot.GameTimeMs;
            _accumulatedPredictionGameTimeMs = Math.Max(0, rawDeltaMs);

            // The opposite case: the new snapshot's GameTimeMs lands AHEAD of what the
            // client had already predicted (e.g. the engine's real-time clock and the
            // client's prediction clock disagree by a few real ms — amplified by the
            // current speed multiplier). Unlike a rewind, a forward jump is not clamped
            // away here, so the renderer must know about it to smooth it visually instead
            // of snapping straight to it.
            _lastReconciliationForwardJumpMs = Math.Max(0, -rawDeltaMs);
            _predictionSegmentStartedAtTimestamp = now;

            if (_pendingConfirmedSpeed is { } pendingSpeed)
            {
                // A snapshot captured before the latest confirmed speed command may
                // still be queued. It must not roll the renderer back to stale speed.
                if (snapshot.CurrentSpeed == pendingSpeed)
                {
                    _currentSpeed = pendingSpeed;
                    _pendingConfirmedSpeed = null;
                }
            }
            else
            {
                _currentSpeed = snapshot.CurrentSpeed;
            }

            if (PauseResumeDiagnostics.Enabled)
            {
                PauseResumeDiagnostics.Write(
                    $"SNAPSHOT seq={snapshot.SnapshotSequence} gameTimeMs={snapshot.GameTimeMs} " +
                    $"snapshotSpeed={snapshot.CurrentSpeed} rawDeltaMs={rawDeltaMs} " +
                    $"accumMs={_accumulatedPredictionGameTimeMs} fwdJumpMs={_lastReconciliationForwardJumpMs} " +
                    $"pendingConfirmedSpeed={_pendingConfirmedSpeed} -> currentSpeed={_currentSpeed}");

                if (_awaitingFirstSnapshotAfterResume)
                {
                    _awaitingFirstSnapshotAfterResume = false;
                    long effectiveDelta = _accumulatedPredictionGameTimeMs
                        + RealTicksToGameMs(now - _predictionSegmentStartedAtTimestamp, _currentSpeed);
                    PauseResumeDiagnostics.Write(
                        $"RESUME STATE: first snapshot after resume{Environment.NewLine}" +
                        FormatSnapshotState(snapshot, effectiveDelta, now));
                }
            }
        }
    }

    /// <summary>Get the latest buffered snapshot, or null if none received yet.</summary>
    public BufferedSnapshot? Latest
    {
        get
        {
            lock (_sync)
            {
                return _latest;
            }
        }
    }

    /// <summary>
    /// Get the latest snapshot plus the client-predicted game-time delta since it.
    /// The delta is accumulated in speed segments so changing speed between
    /// snapshots does not retroactively apply the new speed to old real time.
    /// </summary>
    public SnapshotPrediction? LatestPrediction
    {
        get
        {
            long now = _timestampProvider();

            lock (_sync)
            {
                if (_latest is null)
                    return null;

                long effectiveDelta = _accumulatedPredictionGameTimeMs
                    + RealTicksToGameMs(now - _predictionSegmentStartedAtTimestamp, _currentSpeed);

                return new SnapshotPrediction(_latest, effectiveDelta, _currentSpeed, _lastReconciliationForwardJumpMs);
            }
        }
    }

    internal long EffectivePredictionDeltaMs
    {
        get
        {
            long now = _timestampProvider();

            lock (_sync)
            {
                return _accumulatedPredictionGameTimeMs
                    + RealTicksToGameMs(now - _predictionSegmentStartedAtTimestamp, _currentSpeed);
            }
        }
    }

    private void SetCurrentSpeed(SimulationSpeed speed)
    {
        long now = _timestampProvider();

        lock (_sync)
        {
            _pendingConfirmedSpeed = speed;

            if (speed == _currentSpeed)
            {
                if (PauseResumeDiagnostics.Enabled)
                    PauseResumeDiagnostics.Write($"SETSPEED requested={speed} (no-op, already current)");
                return;
            }

            if (PauseResumeDiagnostics.Enabled)
            {
                long frozenAccumMs = _accumulatedPredictionGameTimeMs + RealTicksToGameMs(
                    now - _predictionSegmentStartedAtTimestamp,
                    _currentSpeed);
                PauseResumeDiagnostics.Write(
                    $"SETSPEED {_currentSpeed} -> {speed}  frozenAccumMs={frozenAccumMs} " +
                    $"baselineGameTimeMs={_latest?.Snapshot.GameTimeMs}");

                if (speed == SimulationSpeed.Speed0 && _latest is not null)
                {
                    PauseResumeDiagnostics.Write(
                        $"PAUSE STATE: last snapshot at moment of pause{Environment.NewLine}" +
                        FormatSnapshotState(_latest.Snapshot, frozenAccumMs, now));
                }
                else if (_currentSpeed == SimulationSpeed.Speed0)
                {
                    // Resuming from pause — the interesting state is the FIRST snapshot
                    // received after this point, not this instant (nothing new has
                    // arrived yet). Update() logs it once that snapshot lands.
                    _awaitingFirstSnapshotAfterResume = true;
                }
            }

            _accumulatedPredictionGameTimeMs += RealTicksToGameMs(
                now - _predictionSegmentStartedAtTimestamp,
                _currentSpeed);

            _predictionSegmentStartedAtTimestamp = now;
            _currentSpeed = speed;
        }
    }

    private static string FormatSnapshotState(AuthoritativeSnapshot snapshot, long effectiveDeltaMs, long receiptTimestamp)
    {
        long predictedGameTimeMs = snapshot.GameTimeMs + effectiveDeltaMs;
        var lines = new List<string>
        {
            $"  seq={snapshot.SnapshotSequence} gameTimeMs={snapshot.GameTimeMs} speed={snapshot.CurrentSpeed} " +
            $"effectiveDeltaMs={effectiveDeltaMs} predictedGameTimeMs={predictedGameTimeMs} " +
            $"playerShipObjectId={snapshot.PlayerShipObjectId}"
        };

        foreach (var obj in snapshot.Objects)
        {
            lines.Add(
                $"  OBJ id={obj.ObjectId} X={obj.X:F3} Y={obj.Y:F3} speedKmS={obj.SpeedKmS:F3} " +
                $"direction={obj.Direction:F3} activeEngineCommand={obj.ActiveEngineCommandType} " +
                $"turnStepDeg={obj.TurnStepDegrees} turnStepRemainingMs={obj.TurnStepRemainingMs} " +
                $"turnStepIntervalMs={obj.TurnStepIntervalMs}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static long RealTicksToGameMs(long elapsedTicks, SimulationSpeed speed)
    {
        if (elapsedTicks <= 0 || speed == SimulationSpeed.Speed0)
            return 0;

        long realMs = (long)(elapsedTicks * 1000.0 / Stopwatch.Frequency);
        return realMs * (int)speed;
    }
}
