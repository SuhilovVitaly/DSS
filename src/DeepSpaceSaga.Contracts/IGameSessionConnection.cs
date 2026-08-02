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
    /// Stream of authoritative snapshots from the engine to the client (~1 Hz).
    /// The client reads from this stream independently of the render loop.
    /// </summary>
    IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync(
        CancellationToken cancellationToken = default);
}
