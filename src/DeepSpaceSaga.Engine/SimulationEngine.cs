using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
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

    private readonly SimulationClock _clock;
    private readonly LinearMotionPredictor _motion = new();
    private readonly List<(ObjectMotionSnapshot Initial, long StartGameTimeMs)> _objects = new();
    private readonly List<PlayerCommand> _pendingCommands = new();
    private ulong _nextSequence;
    private bool _disposed;

    /// <summary>Number of commands received (test seam).</summary>
    internal int ReceivedCommandCount => _pendingCommands.Count;

    /// <summary>Current authoritative simulation speed.</summary>
    public SimulationSpeed CurrentSpeed => _clock.Speed;

    /// <summary>ObjectId of the player ship (set by scenario).</summary>
    public string? PlayerShipObjectId { get; private set; }

    public SimulationEngine()
    {
        _clock = new SimulationClock(SimulationSpeed.Speed1);
    }

    public void ReceiveCommand(PlayerCommand command)
    {
        _pendingCommands.Add(command);
    }

    /// <summary>Set the authoritative simulation speed (e.g. Speed0 for pause).</summary>
    public void SetSpeed(SimulationSpeed speed)
    {
        _clock.SetSpeed(speed);
    }

    /// <summary>
    /// Load initial state from a scenario file. Replaces any previously added objects.
    /// Sets the clock speed and game time from scenario data.
    /// </summary>
    public void LoadScenario(ScenarioFile scenario)
    {
        var gs = scenario.GameState;

        PlayerShipObjectId = gs.PlayerShipObjectId;
        _clock.Reset(gs.GameTimeMs, ScenarioLoader.ParseSpeed(gs.CurrentSpeed));

        _objects.Clear();

        foreach (var obj in gs.SpaceObjects)
        {
            // Convert m/s to km/s for the existing motion system
            double speedKmS = (double)obj.SpeedMps / 1000.0;

            _objects.Add((new ObjectMotionSnapshot(
                ObjectId: obj.ObjectId,
                X: obj.PositionX,
                Y: obj.PositionY,
                SpeedKmS: speedKmS,
                Direction: obj.DirectionDegrees), 0)); // startGameTime stamped at RunAsync
        }
    }

    /// <summary>Add a test object (legacy — prefer LoadScenario for production).</summary>
    public void AddTestObject(ObjectMotionSnapshot initial)
    {
        _objects.Add((initial, 0));
    }

    public async IAsyncEnumerable<AuthoritativeSnapshot> RunAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Stamp objects with the current game time at engine start.
        _clock.ResetRealBaseline();
        long engineStartGameTime = _clock.GameTimeMs;

        for (int i = 0; i < _objects.Count; i++)
        {
            var (initial, _) = _objects[i];
            _objects[i] = (initial, engineStartGameTime);
        }

        // Yield the initial snapshot immediately (before any delay).
        // Capture atomically — no time has passed, so we read without advancing.
        yield return BuildSnapshot(_clock.Capture());

        while (!cancellationToken.IsCancellationRequested && !_disposed)
        {
            await Task.Delay(SnapshotIntervalMs, cancellationToken);

            yield return BuildSnapshot(_clock.UpdateAndCapture());
        }
    }

    private AuthoritativeSnapshot BuildSnapshot(SimulationClockState clockState)
    {
        long gameTimeMs = clockState.GameTimeMs;

        var objects = ImmutableArray.CreateBuilder<ObjectMotionSnapshot>(_objects.Count);
        foreach (var (initial, objStartGameTime) in _objects)
        {
            long elapsed = gameTimeMs - objStartGameTime;
            objects.Add(_motion.Predict(initial, elapsed));
        }

        return new AuthoritativeSnapshot(
            SnapshotSequence: _nextSequence++,
            GameTimeMs: gameTimeMs,
            CurrentSpeed: clockState.Speed,
            Objects: objects.MoveToImmutable(),
            PlayerShipObjectId: PlayerShipObjectId);
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
