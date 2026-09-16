namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Authoritative simulation speed levels.
/// Enum values are relative to the base pace; use GameTimeMultiplier for real-to-game time.
/// </summary>
public enum SimulationSpeed
{
    /// <summary>Pause — game time does not advance.</summary>
    Speed0 = 0,

    /// <summary>Normal speed — 5 game minutes per real second.</summary>
    Speed1 = 1,

    /// <summary>5× the base pace (25 game minutes per real second).</summary>
    Speed2 = 5,

    /// <summary>20× the base pace (100 game minutes per real second).</summary>
    Speed3 = 20,

    /// <summary>100× the base pace (500 game minutes per real second).</summary>
    Speed4 = 100,
}

public static class SimulationSpeedExtensions
{
    public const int BaseGameSecondsPerRealSecond = 300;

    /// <summary>Shared conversion for authoritative clocks and client motion prediction.</summary>
    public static int GameTimeMultiplier(this SimulationSpeed speed) =>
        checked((int)speed * BaseGameSecondsPerRealSecond);
}
