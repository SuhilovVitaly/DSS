namespace DeepSpaceSaga.Contracts;

/// <summary>Explicit continuous-turn direction, separate from the evade side.</summary>
public enum TurnDirection { Left, Right }

/// <summary>Independent continuous longitudinal control direction.</summary>
public enum SpeedDirection { Accelerate, Brake }

/// <summary>Stable command type IDs for the first player ship engine module.</summary>
public static class ShipEngineCommandTypes
{
    public const string Accelerate = "engine.accelerate";
    public const string Brake = "engine.brake";
    public const string MaintainSpeed = "engine.maintainSpeed";
    public const string TurnLeftStep = "engine.turnLeftStep";
    public const string TurnRightStep = "engine.turnRightStep";
    public const string TurnLeftUntilCancel = "engine.turnLeftUntilCancel";
    public const string TurnRightUntilCancel = "engine.turnRightUntilCancel";
    public const string TurnHold = "engine.turnHold";
    public const string SpeedHold = "engine.speedHold";
    public const string MaintainCourse = "engine.maintainCourse";
    public const string SpeedSynchronization = "engine.speedSynchronization";
    public const string DirectionSynchronization = "engine.directionSynchronization";
    public const string Orbit = "engine.orbit";

    /// <summary>
    /// Legacy command type ID. Not part of the canonical set from §56.8; kept only for
    /// backward-compatible handling of old data inside the Engine. Never used by canonical
    /// command definitions or the command panel UI.
    /// </summary>
    public const string CancelAll = "engine.cancelAll";
}
