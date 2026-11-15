using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client;

public sealed partial class GameSessionHandle
{
    private readonly object _rearmPendingGate = new();
    private readonly HashSet<string> _rearmPending = new(StringComparer.Ordinal);

    /// <summary>Latest authoritative carrier surface from the snapshot stream; null before the first snapshot or on a legacy producer.</summary>
    public CarrierOperationsSnapshot? CarrierOperations => Buffer.Presented?.Snapshot.CarrierOperations;

    public bool IsRearmPending(string commandId)
    {
        lock (_rearmPendingGate) return _rearmPending.Contains(commandId);
    }

    /// <summary>
    /// Transfer torpedoes from cargo into one drone. The caller owns the stable CommandId, so a retry
    /// after an ambiguous failure reuses the same id and the engine de-duplicates it. The handle never
    /// computes the transfer locally — the authoritative result (and the updated carrier surface) comes
    /// from the engine via the snapshot stream.
    /// </summary>
    public async ValueTask<CarrierCommandResponse> RearmDroneAsync(RearmDroneRequest request, CancellationToken cancellationToken = default)
    {
        lock (_rearmPendingGate) _rearmPending.Add(request.CommandId);
        try
        {
            return await _connection.RearmDroneAsync(request, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            lock (_rearmPendingGate) _rearmPending.Remove(request.CommandId);
        }
    }
}
