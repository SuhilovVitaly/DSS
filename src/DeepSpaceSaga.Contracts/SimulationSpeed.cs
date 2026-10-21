namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Authoritative simulation speed levels.
/// Enum values are stable serialization identifiers; use the multiplier methods for elapsed time.
/// </summary>
public enum SimulationSpeed
{
    /// <summary>Pause — game time does not advance.</summary>
    Speed0 = 0,

    /// <summary>Base pace — 75 calendar seconds per real second.</summary>
    Speed1 = 1,

    /// <summary>2× the base pace.</summary>
    SpeedDouble = 2,

    /// <summary>10× the base pace.</summary>
    SpeedTen = 10,

    /// <summary>5× the base pace (375 calendar seconds per real second).</summary>
    Speed2 = 5,

    /// <summary>20× the base pace (25 calendar minutes per real second).</summary>
    Speed3 = 20,

    /// <summary>100× the base pace (125 calendar minutes per real second).</summary>
    Speed4 = 100,
}

public static class SimulationSpeedExtensions
{
    public const int BaseGameSecondsPerRealSecond = 300;

    public static bool IsAllowedInCombat(this SimulationSpeed speed) => speed is SimulationSpeed.Speed0 or
        SimulationSpeed.Speed1 or SimulationSpeed.SpeedDouble or SimulationSpeed.Speed2 or SimulationSpeed.SpeedTen;

    /// <summary>Physical motion and ship-cycle seconds per real second.</summary>
    public static double MotionTimeMultiplier(this SimulationSpeed speed) => (int)speed / 4.0;

    /// <summary>Calendar conversion only; the 300:1 calendar-to-motion ratio is preserved.</summary>
    public static int GameTimeMultiplier(this SimulationSpeed speed) =>
        checked((int)speed * BaseGameSecondsPerRealSecond / 4);
}
