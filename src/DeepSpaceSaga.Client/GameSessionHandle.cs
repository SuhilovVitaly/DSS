using System.Threading.Channels;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client;

/// <summary>
/// Owns the lifetime of a game session.
/// Starts a background receive loop that reads snapshots from the connection
/// and feeds them into the SnapshotBuffer.
/// </summary>
public sealed class GameSessionHandle : IAsyncDisposable
{
    private readonly IGameSessionConnection _connection;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _receiveTask;
    private readonly Task _interactionStateSenderTask;
    private readonly Channel<(string? ActiveObjectId, string? SelectedObjectId)> _interactionStateChannel =
        Channel.CreateBounded<(string?, string?)>(new BoundedChannelOptions(1)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });
    private bool _hasSentInteractionState;
    private string? _lastSentActiveObjectId;
    private string? _lastSentSelectedObjectId;
    private long _nextClientSequence;
    private bool _disposed;

    public GameSessionHandle(IGameSessionConnection connection)
    {
        _connection = connection;

        Buffer = new SnapshotBuffer();

        // Background loop: connection → buffer
        _receiveTask = Task.Run(() => ReceiveLoopAsync(_cts.Token));

        // Background loop: coalesced ActiveObjectId/SelectedObjectId → connection
        _interactionStateSenderTask = Task.Run(() => InteractionStateSenderLoopAsync(_cts.Token));
    }

    public IGameSessionConnection Connection => _connection;
    public SnapshotBuffer Buffer { get; }

    /// <summary>
    /// Send a module-addressed command (Commands Panel). The target object id is
    /// passed explicitly — UI selection is never an implicit authoritative target
    /// (the engine validates targetObjectId for target-requiring commands).
    /// </summary>
    public ValueTask SendCommandAsync(
        string objectId,
        string moduleId,
        string commandType,
        string? targetObjectId = null,
        CancellationToken cancellationToken = default)
    {
        ulong sequence = (ulong)Interlocked.Increment(ref _nextClientSequence);
        var command = new PlayerCommand(
            CommandId: $"CMD-{sequence:D8}-{Guid.NewGuid():N}",
            ClientSequence: sequence,
            ObjectId: objectId,
            ModuleId: moduleId,
            CommandType: commandType,
            TargetObjectId: targetObjectId);

        return _connection.SendCommandAsync(command, cancellationToken);
    }

    public ValueTask SendEngineCommandAsync(
        string objectId,
        string moduleId,
        string commandType,
        CancellationToken cancellationToken = default)
    {
        return SendEngineCommandAsync(objectId, moduleId, commandType, targetWorldX: null, targetWorldY: null, cancellationToken);
    }

    /// <summary>
    /// Send an engine command with an explicit world-coordinate target
    /// (engine.navigate-to-point). The target is validated authoritatively by the
    /// engine (missing/non-finite → invalid_target_coordinates).
    /// </summary>
    public ValueTask SendEngineCommandAsync(
        string objectId,
        string moduleId,
        string commandType,
        double? targetWorldX,
        double? targetWorldY,
        CancellationToken cancellationToken = default)
    {
        ulong sequence = (ulong)Interlocked.Increment(ref _nextClientSequence);
        var command = new PlayerCommand(
            CommandId: $"CMD-{sequence:D8}-{Guid.NewGuid():N}",
            ClientSequence: sequence,
            ObjectId: objectId,
            ModuleId: moduleId,
            CommandType: commandType,
            TargetWorldX: targetWorldX,
            TargetWorldY: targetWorldY);

        return _connection.SendCommandAsync(command, cancellationToken);
    }

    /// <summary>
    /// Set the simulation speed and immediately update the client-side tracker.
    /// Properly awaits the connection call — for local connections this completes
    /// synchronously; for network connections the speed is confirmed before the
    /// client-side state is updated.
    /// </summary>
    public async ValueTask SetSpeedAsync(SimulationSpeed speed)
    {
        await _connection.SetSimulationSpeedAsync(speed);
        Buffer.CurrentSpeed = speed;
    }

    /// <summary>Capture and persist the current authoritative world state (quicksave).</summary>
    public ValueTask SaveAsync(CancellationToken cancellationToken = default)
    {
        return _connection.SaveAsync(cancellationToken);
    }

    /// <summary>
    /// Report the tactical map's current ActiveObjectId/SelectedObjectId to the engine.
    /// Non-blocking — never awaited by the render/input loop. Queued into a 1-slot
    /// channel that keeps only the latest full state (DropOldest): if the background
    /// sender hasn't caught up, intermediate states are coalesced away, but delivery
    /// order of whatever IS sent is preserved (single reader, processed strictly
    /// sequentially). The sender additionally dedups against the last state it actually
    /// sent, so redundant calls with an unchanged pair never reach the connection.
    /// </summary>
    public void UpdateObjectInteractionState(string? activeObjectId, string? selectedObjectId)
    {
        _interactionStateChannel.Writer.TryWrite((activeObjectId, selectedObjectId));
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var snapshot in _connection.ReadSnapshotsAsync(ct))
            {
                Buffer.Update(snapshot);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task InteractionStateSenderLoopAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var state in _interactionStateChannel.Reader.ReadAllAsync(ct))
            {
                if (_hasSentInteractionState &&
                    state.ActiveObjectId == _lastSentActiveObjectId &&
                    state.SelectedObjectId == _lastSentSelectedObjectId)
                {
                    continue;
                }

                await SendWithRetryAsync(state, ct);
            }
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>
    /// Send one interaction-state pair, retrying with capped exponential backoff on
    /// failure instead of silently dropping it — a transport error must not leave the
    /// engine stuck on a stale ActiveObjectId/SelectedObjectId forever when the client
    /// doesn't change state again afterwards. If a newer pair has already been queued
    /// (channel capacity 1) while this one was retrying, this attempt is abandoned in
    /// favor of the newer one — coalescing to "the latest full state" per §54 takes
    /// priority over finishing a retry of a now-superseded value.
    /// </summary>
    private async Task SendWithRetryAsync(
        (string? ActiveObjectId, string? SelectedObjectId) state, CancellationToken ct)
    {
        var delay = TimeSpan.FromMilliseconds(200);
        const int maxDelayMs = 5000;

        while (true)
        {
            try
            {
                await _connection.SetObjectInteractionStateAsync(
                    state.ActiveObjectId, state.SelectedObjectId, ct);
                _lastSentActiveObjectId = state.ActiveObjectId;
                _lastSentSelectedObjectId = state.SelectedObjectId;
                _hasSentInteractionState = true;
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                InterfaceLog.Write($"SetObjectInteractionStateAsync failed, retrying: {ex.Message}");

                if (_interactionStateChannel.Reader.TryPeek(out _))
                    return; // a fresher state already superseded this one

                await Task.Delay(delay, ct);
                delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, maxDelayMs));
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        _cts.Cancel();
        _interactionStateChannel.Writer.TryComplete();

        try { await _receiveTask; } catch { }
        try { await _interactionStateSenderTask; } catch { }

        _cts.Dispose();

        if (_connection is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
    }
}
