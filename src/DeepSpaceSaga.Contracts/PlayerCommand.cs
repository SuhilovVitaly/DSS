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
    /// The engine validates this value authoritatively for match commands: a command without
    /// a target (or with a target that does not exist in the world) is rejected with
    /// <see cref="CommandReasonCodes.MissingTarget"/> / <see cref="CommandReasonCodes.UnknownTarget"/>.
    /// Null when the command has no target.
    /// </summary>
    string? TargetObjectId = null,
    /// <summary>
    /// Explicit world-coordinate target for <see cref="ShipEngineCommandTypes.NavigateToPoint"/>
    /// (world units, same coordinate system as <see cref="ObjectMotionSnapshot.X"/>).
    /// Both coordinates are required and must be finite for navigate-to-point; the engine
    /// validates this authoritatively and rejects the command with
    /// <see cref="CommandReasonCodes.InvalidTargetCoordinates"/> otherwise. Null when the
    /// command has no world target.
    /// </summary>
    double? TargetWorldX = null,
    /// <summary>
    /// Explicit world-coordinate target for <see cref="ShipEngineCommandTypes.NavigateToPoint"/>
    /// (world units). See <see cref="TargetWorldX"/>.
    /// </summary>
    double? TargetWorldY = null);
