using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

/// <summary>
/// Authoritative game simulation.
/// Produces immutable snapshots on a fixed interval (1 Hz by default).
/// Uses DeepSpaceSaga.Motion for deterministic position calculation —
/// the same library the client uses for prediction.
/// </summary>
public sealed class SimulationEngine : IDisposable
{
    public const int SimulationTickMs = 100;
    public const int SnapshotIntervalMs = 1000;

    private readonly LinearMotionPredictor _motion = new();
    private readonly List<(ObjectMotionSnapshot Initial, long StartTimeMs)> _testObjects = new();
    private readonly List<PlayerCommand> _pendingCommands = new();
    private ulong _nextSequence;
    private bool _disposed;

    /// <summary>Number of commands received (test seam).</summary>
    internal int ReceivedCommandCount => _pendingCommands.Count;

    public void ReceiveCommand(PlayerCommand command)
    {
        _pendingCommands.Add(command);
    }

    /// <summary>Add a test object whose position is advanced deterministically each snapshot.</summary>
    public void AddTestObject(ObjectMotionSnapshot initial)
    {
        _testObjects.Add((initial, 0)); // startTime set when engine runs
    }

    public async IAsyncEnumerable<AuthoritativeSnapshot> RunAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        long startTime = Environment.TickCount64;

        // Record start time for each test object
        for (int i = 0; i < _testObjects.Count; i++)
        {
            var (initial, _) = _testObjects[i];
            _testObjects[i] = (initial, startTime);
        }

        while (!cancellationToken.IsCancellationRequested && !_disposed)
        {
            await Task.Delay(SnapshotIntervalMs, cancellationToken);

            long gameTimeMs = Environment.TickCount64 - startTime;

            // Advance each test object from its initial state to gameTimeMs
            var objects = ImmutableArray.CreateBuilder<ObjectMotionSnapshot>(_testObjects.Count);
            foreach (var (initial, objStartTime) in _testObjects)
            {
                long elapsed = gameTimeMs - (objStartTime - startTime);
                // elapsed = gameTimeMs (all objects start at engine startTime)
                objects.Add(_motion.Predict(initial, elapsed));
            }

            var snapshot = new AuthoritativeSnapshot(
                SnapshotSequence: _nextSequence++,
                GameTimeMs: gameTimeMs,
                Objects: objects.MoveToImmutable());

            yield return snapshot;
        }
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
