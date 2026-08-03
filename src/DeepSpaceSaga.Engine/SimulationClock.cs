using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

/// <summary>
/// Authoritative simulation clock that accumulates game time based on speed.
/// At Speed0, GameTimeMs does not advance regardless of real time passage.
/// </summary>
public sealed class SimulationClock
{
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
    /// Advance the clock by real time elapsed since last Update or SetSpeed,
    /// multiplied by the current speed. At Speed0 this adds zero.
    /// </summary>
    public void Update()
    {
        long now = Environment.TickCount64;
        long deltaReal = now - _lastRealTick;
        _lastRealTick = now;

        int multiplier = (int)Speed;
        GameTimeMs += deltaReal * multiplier;
    }

    /// <summary>
    /// Change the simulation speed.
    /// Resets the real-time baseline to prevent accumulating a time jump
    /// from real time that passed while the clock was at a different speed.
    /// </summary>
    public void SetSpeed(SimulationSpeed speed)
    {
        _lastRealTick = Environment.TickCount64;
        Speed = speed;
    }

    /// <summary>
    /// Reset the real-time baseline without advancing GameTimeMs.
    /// Use at the start of a new loop to prevent counting backlog time.
    /// </summary>
    public void ResetRealBaseline()
    {
        _lastRealTick = Environment.TickCount64;
    }
}
