using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

/// <summary>
/// Authoritative simulation clock that accumulates game time based on speed.
/// Thread-safe: all public methods are atomic via internal lock.
/// At Speed0, GameTimeMs does not advance regardless of real time passage.
/// </summary>
public sealed class SimulationClock
{
    private readonly object _lock = new();
    private long _lastRealTick;

    public SimulationClock(SimulationSpeed initialSpeed = SimulationSpeed.Speed1)
    {
        Speed = initialSpeed;
        _lastRealTick = Environment.TickCount64;
    }

    /// <summary>Accumulated game time in milliseconds.</summary>
    public long GameTimeMs { get; private set; }

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
            long now = Environment.TickCount64;
            long deltaReal = now - _lastRealTick;
            _lastRealTick = now;

            GameTimeMs += deltaReal * (int)Speed;
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
            long now = Environment.TickCount64;
            long deltaReal = now - _lastRealTick;

            // Accumulate time at the current speed before switching
            GameTimeMs += deltaReal * (int)Speed;

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
            _lastRealTick = Environment.TickCount64;
        }
    }
}
