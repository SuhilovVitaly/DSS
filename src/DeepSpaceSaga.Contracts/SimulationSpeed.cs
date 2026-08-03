namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Authoritative simulation speed levels.
/// Multiplier is the enum's underlying int value.
/// </summary>
public enum SimulationSpeed
{
    /// <summary>Pause — game time does not advance.</summary>
    Speed0 = 0,

    /// <summary>Normal speed — 1 game second per real second.</summary>
    Speed1 = 1,

    /// <summary>5× acceleration.</summary>
    Speed2 = 5,

    /// <summary>20× acceleration.</summary>
    Speed3 = 20,

    /// <summary>100× acceleration.</summary>
    Speed4 = 100,
}
