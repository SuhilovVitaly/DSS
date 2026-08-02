using System.Diagnostics;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client;

/// <summary>
/// Thread-safe holder for the latest authoritative snapshot.
/// Snapshot and its local receipt timestamp are published atomically,
/// so renderer always reads a consistent pair.
/// </summary>
public sealed class SnapshotBuffer
{
    private BufferedSnapshot? _latest;

    /// <summary>Atomically replace the current snapshot with receipt timestamp.</summary>
    public void Update(AuthoritativeSnapshot snapshot)
    {
        var value = new BufferedSnapshot(snapshot, Stopwatch.GetTimestamp());
        Interlocked.Exchange(ref _latest, value);
    }

    /// <summary>Get the latest buffered snapshot, or null if none received yet.</summary>
    public BufferedSnapshot? Latest => Volatile.Read(ref _latest);
}
