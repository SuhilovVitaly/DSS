using System.Diagnostics;

namespace DeepSpaceSaga.Client;

/// <summary>
/// Bridges authoritative game time and local monotonic time.
/// When a snapshot arrives, its GameTimeMs is recorded alongside a Stopwatch timestamp.
/// At render time, the estimated current game time = snapshot time + local elapsed.
/// Prediction delta = estimated game time - snapshot time (i.e., time since last snapshot).
/// </summary>
public sealed class ClientGameClock
{
    private long _snapshotGameTimeMs;
    private long _receivedAtTimestamp;

    /// <summary>Notify the clock that a new authoritative snapshot has arrived.</summary>
    public void OnSnapshotReceived(long snapshotGameTimeMs)
    {
        _snapshotGameTimeMs = snapshotGameTimeMs;
        _receivedAtTimestamp = Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Milliseconds elapsed since the last snapshot was received.
    /// This is the prediction delta: how far ahead of the authoritative state
    /// the renderer should predict.
    /// </summary>
    public long PredictionDeltaMs
    {
        get
        {
            if (_receivedAtTimestamp == 0)
                return 0;

            long ticksElapsed = Stopwatch.GetTimestamp() - _receivedAtTimestamp;
            return (long)(ticksElapsed * 1000.0 / Stopwatch.Frequency);
        }
    }
}
