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
    public const int SnapshotIntervalMs = 1000;

    private readonly SimulationClock _clock = new(SimulationSpeed.Speed1);
    private readonly LinearMotionPredictor _motion = new();
    private readonly List<(ObjectMotionSnapshot Initial, long StartGameTimeMs)> _testObjects = new();
    private readonly List<PlayerCommand> _pendingCommands = new();
    private ulong _nextSequence;
    private bool _disposed;

    /// <summary>Number of commands received (test seam).</summary>
    internal int ReceivedCommandCount => _pendingCommands.Count;

    /// <summary>Current authoritative simulation speed.</summary>
    public SimulationSpeed CurrentSpeed => _clock.Speed;

    public void ReceiveCommand(PlayerCommand command)
    {
        _pendingCommands.Add(command);
    }

    /// <summary>Set the authoritative simulation speed (e.g. Speed0 for pause).</summary>
    public void SetSpeed(SimulationSpeed speed)
    {
        _clock.SetSpeed(speed);
    }

    /// <summary>Add a test object whose position is advanced deterministically each snapshot.</summary>
    public void AddTestObject(ObjectMotionSnapshot initial)
    {
        _testObjects.Add((initial, 0)); // startGameTime set when engine runs
    }

    public async IAsyncEnumerable<AuthoritativeSnapshot> RunAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Stamp each test object with the current game time at engine start.
        // Reset real baseline so the first Update() in the loop measures from now.
        _clock.ResetRealBaseline();
        long engineStartGameTime = _clock.GameTimeMs;

        for (int i = 0; i < _testObjects.Count; i++)
        {
            var (initial, _) = _testObjects[i];
            _testObjects[i] = (initial, engineStartGameTime);
        }

        while (!cancellationToken.IsCancellationRequested && !_disposed)
        {
            await Task.Delay(SnapshotIntervalMs, cancellationToken);

            _clock.Update();
            long gameTimeMs = _clock.GameTimeMs;

            // Advance each test object from its initial state by elapsed game time
            var objects = ImmutableArray.CreateBuilder<ObjectMotionSnapshot>(_testObjects.Count);
            foreach (var (initial, objStartGameTime) in _testObjects)
            {
                long elapsed = gameTimeMs - objStartGameTime;
                objects.Add(_motion.Predict(initial, elapsed));
            }

            var snapshot = new AuthoritativeSnapshot(
                SnapshotSequence: _nextSequence++,
                GameTimeMs: gameTimeMs,
                CurrentSpeed: _clock.Speed,
                Objects: objects.MoveToImmutable());

            yield return snapshot;
        }
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
