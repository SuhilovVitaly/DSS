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
    /// Explicit target object id, required for <see cref="ShipEngineCommandTypes.SpeedSynchronization"/>
    /// and <see cref="ShipEngineCommandTypes.DirectionSynchronization"/>. UI selection is not an implicit
    /// authoritative target — the target must always be passed explicitly in the command.
    /// The engine validates this value authoritatively for match commands: a command without
    /// a target (or with a target that does not exist in the world) is rejected with
    /// <see cref="CommandReasonCodes.MissingTarget"/> / <see cref="CommandReasonCodes.UnknownTarget"/>.
    /// Null when the command has no target.
    /// </summary>
    string? TargetObjectId = null,
    /// <summary>
    /// Explicit world-coordinate target for <see cref="ShipEngineCommandTypes.Orbit"/>
    /// (world units, same coordinate system as <see cref="ObjectMotionSnapshot.X"/>).
    /// Both coordinates are required and must be finite for orbit; the engine
    /// validates this authoritatively and rejects the command with
    /// <see cref="CommandReasonCodes.InvalidTargetCoordinates"/> otherwise. Null when the
    /// command has no world target.
    /// </summary>
    double? TargetWorldX = null,
    /// <summary>
    /// Explicit world-coordinate target for <see cref="ShipEngineCommandTypes.Orbit"/>
    /// (world units). See <see cref="TargetWorldX"/>.
    /// </summary>
    double? TargetWorldY = null,
    /// <summary>
    /// Item type id being traded, required for <see cref="TradeCommandTypes.Buy"/>,
    /// <see cref="TradeCommandTypes.Sell"/> and <see cref="TradeCommandTypes.Refuel"/>.
    /// The engine validates this value authoritatively and rejects the command with
    /// <see cref="CommandReasonCodes.UnknownItemType"/> when it does not resolve to a
    /// known tradeable item. Null when the command is not a trade command.
    /// </summary>
    string? ItemTypeId = null,
    /// <summary>
    /// Requested quantity for <see cref="TradeCommandTypes.Buy"/>,
    /// <see cref="TradeCommandTypes.Sell"/> and <see cref="TradeCommandTypes.Refuel"/>.
    /// The engine validates this value authoritatively against the player's Credits,
    /// the station's stock, and the target module's capacity, and rejects or partially
    /// executes the command accordingly (see <see cref="CommandResult.ExecutedQuantity"/>).
    /// Null when the command is not a trade command.
    /// </summary>
    long? Quantity = null,
    /// <summary>
    /// Opaque id of the authoritative trade quote this command executes. The quote was issued
    /// by the engine for an exact binding — station, <see cref="ObjectId"/>, <see cref="ModuleId"/>,
    /// <see cref="CommandType"/>, <see cref="ItemTypeId"/> and <see cref="Quantity"/> — and carries the
    /// price; the client never supplies or invents a price. Set together with <see cref="MarketRevision"/>
    /// for a quoted trade; both null for legacy/non-trade commands. A partially specified binding is
    /// still representable so the engine can reject it with <see cref="CommandReasonCodes.InvalidQuote"/>.
    /// This DTO does not validate the binding; the engine does.
    /// </summary>
    string? QuoteId = null,
    /// <summary>
    /// Station market revision at which the quote <see cref="QuoteId"/> was issued. The engine rejects a
    /// quote whose revision is no longer current with <see cref="CommandReasonCodes.StaleQuote"/>.
    /// Null for legacy/non-trade commands (null means "no revision", not revision zero).
    /// </summary>
    long? MarketRevision = null);
