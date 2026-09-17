using System.Diagnostics;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

/// <summary>
/// Immutable snapshot of clock state, captured atomically under lock.
/// </summary>
public readonly record struct SimulationClockState(long GameTimeMs, SimulationSpeed Speed,
    long? SimulationTimeMs = null)
{
    public long MotionTimeMs => SimulationTimeMs ?? GameTimeMs;
}

/// <summary>
/// One authoritative clock advances calendar and motion time from the same elapsed interval.
/// Thread-safe: all public methods are atomic via internal lock.
/// At Speed0, neither timestamp advances. Calendar time runs 300 times faster than motion.
/// </summary>
public sealed class SimulationClock
{
    private readonly object _lock = new();
    private long _lastRealTick;
    private readonly Func<long> _realTimeMs;

    public SimulationClock(SimulationSpeed initialSpeed = SimulationSpeed.Speed1)
        // Match the client's high-resolution monotonic source; coarse platform ticks
        // otherwise become large reconciliation errors at x100.
        : this(initialSpeed, () => (long)(Stopwatch.GetTimestamp() * (1000.0 / Stopwatch.Frequency))) { }

    internal SimulationClock(SimulationSpeed initialSpeed, Func<long> realTimeMs)
    {
        _realTimeMs = realTimeMs;
        Speed = initialSpeed;
        _lastRealTick = _realTimeMs();
    }

    /// <summary>Accumulated calendar time in milliseconds (five minutes per real second at Speed1).</summary>
    public long GameTimeMs { get; private set; }

    /// <summary>Time for motion and ship cycles, at one second per real second at Speed1.</summary>
    public long SimulationTimeMs { get; private set; }

    /// <summary>Current simulation speed.</summary>
    public SimulationSpeed Speed { get; private set; }

    /// <summary>
    /// Advance the clock by real time elapsed since last Update/SetSpeed/ResetRealBaseline,
    /// multiplied by the current speed. At Speed0 this adds zero.
    /// </summary>
    public void Update()
    {
        lock (_lock)
        {
            long now = _realTimeMs();
            long deltaReal = now - _lastRealTick;
            _lastRealTick = now;

            GameTimeMs += deltaReal * Speed.GameTimeMultiplier();
            SimulationTimeMs += deltaReal * (int)Speed;
        }
    }

    /// <summary>
    /// Atomically advance the clock AND capture the resulting GameTimeMs + Speed.
    /// Use this when building snapshots from the delay loop.
    /// </summary>
    public SimulationClockState UpdateAndCapture()
    {
        lock (_lock)
        {
            long now = _realTimeMs();
            long deltaReal = now - _lastRealTick;
            _lastRealTick = now;

            GameTimeMs += deltaReal * Speed.GameTimeMultiplier();
            SimulationTimeMs += deltaReal * (int)Speed;
            return new SimulationClockState(GameTimeMs, Speed, SimulationTimeMs);
        }
    }

    /// <summary>
    /// Atomically capture the current GameTimeMs + Speed WITHOUT advancing the clock.
    /// Use for the initial snapshot (before any time has passed).
    /// </summary>
    public SimulationClockState Capture()
    {
        lock (_lock)
        {
            return new SimulationClockState(GameTimeMs, Speed, SimulationTimeMs);
        }
    }

    /// <summary>
    /// Reset the clock to a known initial state for New Game.
    /// Sets GameTimeMs and Speed directly, without accumulating any real time.
    /// Resets the real-time baseline so subsequent Update() calls measure from now.
    /// </summary>
    public void Reset(long gameTimeMs, SimulationSpeed speed, long? simulationTimeMs = null)
    {
        lock (_lock)
        {
            GameTimeMs = gameTimeMs;
            // Legacy saves used one timestamp for both domains. Preserve their baselines.
            SimulationTimeMs = simulationTimeMs ?? gameTimeMs;
            Speed = speed;
            _lastRealTick = _realTimeMs();
        }
    }

    /// <summary>
    /// Change the simulation speed.
    /// Atomically accumulates elapsed game time at the OLD speed first,
    /// then switches to the new speed. This prevents losing game time
    /// between the last snapshot and the speed change.
    /// </summary>
    public void SetSpeed(SimulationSpeed speed)
    {
        lock (_lock)
        {
            long now = _realTimeMs();
            long deltaReal = now - _lastRealTick;

            // Accumulate time at the current speed before switching
            GameTimeMs += deltaReal * Speed.GameTimeMultiplier();
            SimulationTimeMs += deltaReal * (int)Speed;

            // Switch to new speed and reset baseline
            _lastRealTick = now;
            Speed = speed;
        }
    }

    /// <summary>
    /// Reset the real-time baseline without advancing GameTimeMs.
    /// Use at the start of a new loop to prevent counting backlog time.
    /// </summary>
    public void ResetRealBaseline()
    {
        lock (_lock)
        {
            _lastRealTick = _realTimeMs();
        }
    }
}
