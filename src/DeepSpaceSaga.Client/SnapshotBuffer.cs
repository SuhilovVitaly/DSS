using System.Diagnostics;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client;

public sealed record SnapshotPrediction(
    BufferedSnapshot BufferedSnapshot,
    long EffectivePredictionDeltaMs,
    SimulationSpeed CurrentSpeed);

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
    private long _predictionSegmentStartedAtTimestamp;
    private long _accumulatedPredictionGameTimeMs;

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
            _latest = value;

            // A new authoritative snapshot is the new prediction baseline.
            _accumulatedPredictionGameTimeMs = 0;
            _predictionSegmentStartedAtTimestamp = now;

            // Sync client-side speed tracker from the authoritative snapshot.
            // We intentionally update every snapshot so that speed changes
            // (including Speed2/Speed3/Speed4 applied outside modal-pause)
            // are reflected without waiting for a SetSpeedAsync round-trip.
            _currentSpeed = snapshot.CurrentSpeed;
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

                return new SnapshotPrediction(_latest, effectiveDelta, _currentSpeed);
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
            if (speed == _currentSpeed)
                return;

            _accumulatedPredictionGameTimeMs += RealTicksToGameMs(
                now - _predictionSegmentStartedAtTimestamp,
                _currentSpeed);

            _predictionSegmentStartedAtTimestamp = now;
            _currentSpeed = speed;
        }
    }

    private static long RealTicksToGameMs(long elapsedTicks, SimulationSpeed speed)
    {
        if (elapsedTicks <= 0 || speed == SimulationSpeed.Speed0)
            return 0;

        long realMs = (long)(elapsedTicks * 1000.0 / Stopwatch.Frequency);
        return realMs * (int)speed;
    }
}
