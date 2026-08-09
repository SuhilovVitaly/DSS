namespace DeepSpaceSaga.Contracts;

/// <summary>
/// A command from the player, addressed to a specific module on an object.
/// </summary>
public sealed record PlayerCommand(
    string CommandId,
    ulong ClientSequence,
    string ObjectId,
    string ModuleId,
    string CommandType,
    /// <summary>
    /// Explicit target object id, required for <see cref="ShipEngineCommandTypes.MatchTargetSpeed"/>
    /// and <see cref="ShipEngineCommandTypes.MatchTargetCourse"/>. UI selection is not an implicit
    /// authoritative target — the target must always be passed explicitly in the command.
    /// Filled by future iterations; null when the command has no target.
    /// </summary>
    string? TargetObjectId = null);
