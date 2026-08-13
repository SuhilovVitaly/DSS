namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Async/message-oriented contract between client and authoritative game session.
/// Same shape for in-process and remote (network) implementations.
///
/// Critical: the render loop must never await this connection synchronously.
/// Snapshots arrive independently and asynchronously via ReadSnapshotsAsync.
/// </summary>
public interface IGameSessionConnection : IAsyncDisposable
{
    /// <summary>Send a player command to the authoritative session.</summary>
    ValueTask SendCommandAsync(
        PlayerCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Set the authoritative simulation speed (e.g. pause / resume).
    /// Session-control — separate from gameplay module commands.
    /// </summary>
    ValueTask SetSimulationSpeedAsync(
        SimulationSpeed speed,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Report the client's current tactical-map ActiveObjectId (hover) and
    /// SelectedObjectId (click) so the engine can hold them as authoritative state.
    /// Session-control — not a module command, not a ShipEngineCommandType. The
    /// caller always sends the full pair; the engine independently normalizes each
    /// id to null if it doesn't reference an object currently in the world.
    /// </summary>
    ValueTask SetObjectInteractionStateAsync(
        string? activeObjectId,
        string? selectedObjectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stream of authoritative snapshots from the engine to the client (~1 Hz).
    /// The client reads from this stream independently of the render loop.
    /// </summary>
    IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Capture the current authoritative world state and persist it (quicksave).
    /// There is no matching LoadAsync — loading a save always goes through the
    /// session-factory bootstrap path, replacing the whole session/connection.
    /// </summary>
    ValueTask SaveAsync(CancellationToken cancellationToken = default);
}
