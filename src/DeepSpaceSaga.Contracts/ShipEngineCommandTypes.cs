namespace DeepSpaceSaga.Contracts;

/// <summary>Stable command type IDs for the first player ship engine module.</summary>
public static class ShipEngineCommandTypes
{
    public const string Accelerate = "engine.accelerate";
    public const string Brake = "engine.brake";
    public const string TurnLeftStep = "engine.turn-left-step";
    public const string TurnRightStep = "engine.turn-right-step";
    public const string TurnLeftUntilCancel = "engine.turn-left-until-cancel";
    public const string TurnRightUntilCancel = "engine.turn-right-until-cancel";
    public const string CancelAll = "engine.cancel-all";
}
