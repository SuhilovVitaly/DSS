namespace DeepSpaceSaga.Contracts;

/// <summary>Stable command type IDs for the first player ship engine module.</summary>
public static class ShipEngineCommandTypes
{
    public const string Accelerate = "engine.accelerate";
    public const string Brake = "engine.brake";
    public const string MaintainSpeed = "engine.maintain-speed";
    public const string TurnLeftStep = "engine.turn-left-step";
    public const string TurnRightStep = "engine.turn-right-step";
    public const string TurnLeftUntilCancel = "engine.turn-left-until-cancel";
    public const string TurnRightUntilCancel = "engine.turn-right-until-cancel";
    public const string MaintainCourse = "engine.maintain-course";
    public const string MatchTargetSpeed = "engine.match-target-speed";
    public const string MatchTargetCourse = "engine.match-target-course";

    /// <summary>
    /// Legacy command type ID. Not part of the canonical set from §56.8; kept only for
    /// backward-compatible handling of old data inside the Engine. Never used by canonical
    /// command definitions or the command panel UI.
    /// </summary>
    public const string CancelAll = "engine.cancel-all";
}
