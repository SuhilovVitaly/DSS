using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client;

public sealed partial class GameSessionHandle
{
    private readonly object _demoGate = new();
    private readonly Dictionary<string, (string Payload, TaskCompletionSource<DemoCampaignResponse> Completion)> _demoPending = new(StringComparer.Ordinal);
    private readonly HashSet<string> _demoPresented = new(StringComparer.Ordinal);
    private DemoCampaignSnapshot? _demoSnapshot;

    public DemoCampaignSnapshot? DemoCampaign => Volatile.Read(ref _demoSnapshot);
    public event Action<DemoCampaignResponse>? DemoCampaignResponded;
    public event Action<long>? DemoCampaignCommitted;

    public bool IsDemoCampaignPending(string commandId)
    {
        lock (_demoGate) return _demoPending.ContainsKey(commandId);
    }

    public ValueTask<DemoCampaignResponse> ExecuteDemoCampaignAsync(DemoCampaignCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrWhiteSpace(command.CommandId))
            return ValueTask.FromResult(new DemoCampaignResponse(command.CommandId ?? "", false, DemoCampaignReasons.InvalidIdentity, DemoCampaign?.Revision ?? 0));
        string payload = command.GetType().FullName + "\n" + JsonSerializer.Serialize(command, command.GetType());
        TaskCompletionSource<DemoCampaignResponse> completion;
        lock (_demoGate)
        {
            if (_demoPending.TryGetValue(command.CommandId, out var pending))
                return pending.Payload == payload
                    ? new(pending.Completion.Task.WaitAsync(cancellationToken))
                    : ValueTask.FromResult(new DemoCampaignResponse(command.CommandId, false, DemoCampaignReasons.IdentityConflict, DemoCampaign?.Revision ?? 0));
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _demoPending.Add(command.CommandId, (payload, completion));
        }
        _ = CompleteDemoCommandAsync(command, completion, cancellationToken);
        return new(completion.Task);
    }

    private async Task CompleteDemoCommandAsync(DemoCampaignCommand command,
        TaskCompletionSource<DemoCampaignResponse> completion, CancellationToken cancellationToken)
    {
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
        try
        {
            var response = await _connection.ExecuteDemoCampaignAsync(command, lifetime.Token).AsTask()
                .WaitAsync(lifetime.Token).ConfigureAwait(false);
            if (response.CommandId != command.CommandId) throw new IOException("Campaign response identity mismatch.");
            bool present;
            lock (_demoGate) present = _demoPresented.Add(command.CommandId);
            if (present && !_disposed && !lifetime.IsCancellationRequested)
            {
                try { DemoCampaignResponded?.Invoke(response); }
                catch (Exception error) { WriteDiagnostic("CAMPAIGN observer failed: " + error.Message); }
            }
            completion.TrySetResult(response);
        }
        catch (OperationCanceledException)
        {
            completion.TrySetCanceled(lifetime.Token);
        }
        catch (Exception error)
        {
            WriteDiagnostic("CAMPAIGN transport failed: " + error.Message);
            completion.TrySetResult(new(command.CommandId, false, DemoCampaignReasons.Unavailable, DemoCampaign?.Revision ?? 0));
        }
        finally
        {
            lock (_demoGate) _demoPending.Remove(command.CommandId);
        }
    }

    private void ObserveDemoCampaign(DemoCampaignSnapshot? snapshot)
    {
        if (snapshot is null) return;
        var previous = DemoCampaign;
        if (previous is not null && snapshot.CommitSequence < previous.CommitSequence) return;
        Volatile.Write(ref _demoSnapshot, snapshot);
        if (previous is null || snapshot.CommitSequence > previous.CommitSequence)
        {
            try { DemoCampaignCommitted?.Invoke(snapshot.CommitSequence); }
            catch (Exception error) { WriteDiagnostic("CAMPAIGN observer failed: " + error.Message); }
        }
    }
}
