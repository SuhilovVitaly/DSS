using System.Runtime.CompilerServices;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

/// <summary>
/// Authoritative game simulation.
/// Produces immutable snapshots on a fixed interval (1 Hz by default).
/// No graphics, no UI, no network dependencies.
/// </summary>
public sealed class SimulationEngine : IDisposable
{
    /// <summary>Internal simulation tick: 100 ms.</summary>
    public const int SimulationTickMs = 100;

    /// <summary>Authoritative snapshot interval: 1000 ms.</summary>
    public const int SnapshotIntervalMs = 1000;

    private ulong _nextSequence;
    private readonly List<ObjectMotionSnapshot> _objects = new();
    private bool _disposed;

    /// <summary>Add a test object for the render/prediction pipeline demo.</summary>
    public void AddTestObject(ObjectMotionSnapshot obj)
    {
        _objects.Add(obj);
    }

    /// <summary>
    /// Run the simulation loop. Produces snapshots at SnapshotIntervalMs.
    /// This is a minimal demo loop — a full authoritative turn pipeline
    /// (CommandInbox, ConflictResolver, etc.) is out of scope for P003.
    /// </summary>
    public async IAsyncEnumerable<AuthoritativeSnapshot> RunAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        long startTime = Environment.TickCount64;

        while (!cancellationToken.IsCancellationRequested && !_disposed)
        {
            await Task.Delay(SnapshotIntervalMs, cancellationToken);

            long gameTimeMs = Environment.TickCount64 - startTime;
            var snapshot = new AuthoritativeSnapshot(
                SnapshotSequence: _nextSequence++,
                GameTimeMs: gameTimeMs,
                Objects: _objects.ToList()); // immutable copy

            yield return snapshot;
        }
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
