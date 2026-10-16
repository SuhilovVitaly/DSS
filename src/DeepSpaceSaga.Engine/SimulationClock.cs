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
    private bool _terminalStopped;
    private long _motionQuarterRemainder;

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

    /// <summary>Accumulated calendar time in milliseconds (75 calendar seconds per real second at Speed1).</summary>
    public long GameTimeMs { get; private set; }

    /// <summary>Time for motion and ship cycles, at one physical second per four real seconds at Speed1.</summary>
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

            Advance(deltaReal);
        }
    }

    private void Advance(long deltaReal)
    {
        GameTimeMs += deltaReal * Speed.GameTimeMultiplier();
        // Preserve fractions across short updates and speed changes. Otherwise
        // quarter-speed updates shorter than 4 real ms would lose motion time.
        long quarters = deltaReal * (int)Speed + _motionQuarterRemainder;
        SimulationTimeMs += quarters / 4;
        _motionQuarterRemainder = quarters % 4;
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

            Advance(deltaReal);
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
            _terminalStopped = false;
            _motionQuarterRemainder = 0;
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
            if (_terminalStopped) return;
            long now = _realTimeMs();
            long deltaReal = now - _lastRealTick;

            // Accumulate time at the current speed before switching
            Advance(deltaReal);

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

    /// <summary>Latch the first terminal boundary until an explicit new-world Reset.</summary>
    internal void StopAtTerminal(long gameTimeMs, long simulationTimeMs)
    {
        lock (_lock)
        {
            if (_terminalStopped) return;
            _terminalStopped = true;
            _motionQuarterRemainder = 0;
            GameTimeMs = gameTimeMs;
            SimulationTimeMs = simulationTimeMs;
            Speed = SimulationSpeed.Speed0;
            _lastRealTick = _realTimeMs();
        }
    }
}
