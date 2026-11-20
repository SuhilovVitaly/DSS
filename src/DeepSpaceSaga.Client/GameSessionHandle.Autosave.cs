using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client;

public sealed partial class GameSessionHandle
{
    private readonly object _autosaveStateGate = new();
    private bool _autosaveActive;
    private AuthoritativeSnapshot? _autosaveLatest;
    private long _autosaveObservedRevision = -1, _autosaveRequestGeneration;
    private string? _autosaveDock;
    private Task _pendingAutosave = Task.CompletedTask;
    private string _autosaveStatus = "";
    private bool _autosaveBaselinePending;
    private long _savedStatusUntil;
    public string AutosaveStatus => Volatile.Read(ref _autosaveStatus) is "Saved" && Environment.TickCount64 >= Interlocked.Read(ref _savedStatusUntil)
        ? "" : Volatile.Read(ref _autosaveStatus);

    public void ActivateAutosave(bool saveInitialState = true)
    {
        lock (_autosaveStateGate)
        {
            if (_disposed) return;
            _autosaveActive = true;
            if (!saveInitialState) _autosaveBaselinePending = true;
            if (_autosaveLatest is { } snapshot) ObserveAutosave(snapshot);
        }
    }

    private void ObserveAutosave(AuthoritativeSnapshot snapshot)
    {
        if (snapshot.DemoCampaign?.Start is null) return;
        lock (_autosaveStateGate)
        {
            _autosaveLatest = snapshot;
            var ship = snapshot.Objects.FirstOrDefault(o => o.ObjectId == snapshot.PlayerShipObjectId);
            string? dock = ship?.IsDocked == true ? ship.DockedStationObjectId : null;
            if (_autosaveBaselinePending)
            {
                _autosaveObservedRevision = snapshot.DemoCampaign.Revision;
                _autosaveDock = dock;
                _autosaveBaselinePending = false;
            }
            if (!_autosaveActive || _disposed || snapshot.IsTacticalBattleActive || ship?.IsDestroyed == true ||
                ResultsCheckpointOwnsAutosave(snapshot.DemoCampaign)) return;
            bool changed = snapshot.DemoCampaign.Revision != _autosaveObservedRevision || dock is not null && dock != _autosaveDock;
            _autosaveDock = dock;
            if (!changed) return;
            _autosaveObservedRevision = snapshot.DemoCampaign.Revision;
            QueueAutosave(snapshot.DemoCampaign.CommitSequence);
        }
    }

    public Task RetryAutosaveAsync()
    {
        lock (_autosaveStateGate)
        {
            if (!_autosaveActive || _disposed || _autosaveLatest is not { IsTacticalBattleActive: false, DemoCampaign: { } state } ||
                IsPlayerShipDestroyed || ResultsCheckpointOwnsAutosave(state)) return Task.CompletedTask;
            return QueueAutosave(state.CommitSequence);
        }
    }

    private static bool ResultsCheckpointOwnsAutosave(DemoCampaignSnapshot campaign) =>
        campaign.BattleResults is { Stage: not BattleResultsStage.Acknowledged } ||
        campaign.Rewards?.Loot is { Phase: LootPhases.Open };

    private Task QueueAutosave(long sequence)
    {
        long generation = ++_autosaveRequestGeneration;
        Volatile.Write(ref _autosaveStatus, "Saving");
        _pendingAutosave = Task.Run(async () =>
        {
            string status;
            try { await _connection.RequestAutosaveAsync(sequence).ConfigureAwait(false); status = "Saved"; }
            catch (Exception error) { WriteDiagnostic("CAMPAIGN autosave failed: " + error.Message); status = "Failed"; }
            lock (_autosaveStateGate)
                if (generation == _autosaveRequestGeneration && !_disposed)
                {
                    if (status == "Saved") Interlocked.Exchange(ref _savedStatusUntil, Environment.TickCount64 + 5000);
                    Volatile.Write(ref _autosaveStatus, status);
                }
        });
        return _pendingAutosave;
    }

    public async Task SuspendAutosaveAsync()
    {
        Task pending;
        lock (_autosaveStateGate) { _autosaveActive = false; pending = _pendingAutosave; }
        await pending.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        if (AutosaveStatus == "Failed") WriteDiagnostic("CAMPAIGN continuation has an unsaved change; last durable checkpoint retained.");
    }
}
