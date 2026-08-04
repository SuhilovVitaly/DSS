using System.Diagnostics;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client;

/// <summary>
/// Thread-safe holder for the latest authoritative snapshot.
/// Snapshot and its local receipt timestamp are published atomically,
/// so renderer always reads a consistent pair.
/// Also tracks the client-side authoritative simulation speed
/// (updated immediately on speed changes, without waiting for the next snapshot).
/// </summary>
public sealed class SnapshotBuffer
{
    private BufferedSnapshot? _latest;

    /// <summary>
    /// Client-side authoritative simulation speed.
    /// Updated immediately after a confirmed speed change so the renderer
    /// can gate prediction without waiting for the next 1 Hz snapshot.
    /// </summary>
    public SimulationSpeed CurrentSpeed { get; set; } = SimulationSpeed.Speed1;

    /// <summary>Atomically replace the current snapshot with receipt timestamp.</summary>
    public void Update(AuthoritativeSnapshot snapshot)
    {
        var value = new BufferedSnapshot(snapshot, Stopwatch.GetTimestamp());
        Interlocked.Exchange(ref _latest, value);

        // Sync client-side speed tracker from the authoritative snapshot.
        // We intentionally update every snapshot so that speed changes
        // (including Speed2/Speed3/Speed4 applied outside modal-pause)
        // are reflected without waiting for a SetSpeedAsync round-trip.
        CurrentSpeed = snapshot.CurrentSpeed;
    }

    /// <summary>Get the latest buffered snapshot, or null if none received yet.</summary>
    public BufferedSnapshot? Latest => Volatile.Read(ref _latest);
}
