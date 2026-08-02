using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client;

/// <summary>
/// Thread-safe holder for the latest authoritative snapshot.
/// Updated by the receive loop, read by the render thread.
/// Uses immutable snapshot + atomic reference swap.
/// </summary>
public sealed class SnapshotBuffer
{
    private AuthoritativeSnapshot? _latest;

    /// <summary>Atomically replace the current snapshot.</summary>
    public void Update(AuthoritativeSnapshot snapshot)
    {
        Interlocked.Exchange(ref _latest, snapshot);
    }

    /// <summary>Get the latest snapshot, or null if none received yet.</summary>
    public AuthoritativeSnapshot? Latest => Volatile.Read(ref _latest);
}
