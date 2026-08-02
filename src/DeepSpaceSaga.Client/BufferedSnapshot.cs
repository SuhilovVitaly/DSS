using System.Diagnostics;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client;

/// <summary>
/// An authoritative snapshot bundled with the local monotonic timestamp
/// of when it was received. The pair is published atomically so the
/// renderer always sees a consistent snapshot + clock pair.
/// </summary>
public sealed record BufferedSnapshot(
    AuthoritativeSnapshot Snapshot,
    long ReceivedAtTimestamp)
{
    /// <summary>
    /// Prediction delta: milliseconds elapsed since this snapshot was received,
    /// calculated from the embedded Stopwatch timestamp.
    /// </summary>
    public long PredictionDeltaMs
    {
        get
        {
            long ticksElapsed = Stopwatch.GetTimestamp() - ReceivedAtTimestamp;
            return (long)(ticksElapsed * 1000.0 / Stopwatch.Frequency);
        }
    }
}
