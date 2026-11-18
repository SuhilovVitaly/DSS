using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Demo;

/// <summary>One shared pending gate for every action in the selected frozen battle.</summary>
public sealed class BattleLootPresentation(Func<DemoCampaignCommand, Task<DemoCampaignResponse>> execute)
{
    private readonly object _gate = new();
    private LootSessionSnapshot? _state;
    private string? _selected;
    private string? _observedBattleId;
    private long _revision;
    private int _pending;
    private DemoCampaignCommand? _retryCommand;
    public bool Pending => Volatile.Read(ref _pending) != 0;
    public string? LastReason { get; private set; }
    public LootSessionSnapshot? State { get { lock (_gate) return _state; } }
    public LootObjectSnapshot? Selected { get { lock (_gate) return _state?.Objects.FirstOrDefault(s => s.ObjectId == _selected); } }
    public DemoCampaignCommand? RetryCommand { get { lock (_gate) return _retryCommand; } }
    public Task<bool> RetryAsync() => SubmitAsync((_, _, _, _) => RetryCommand ?? throw new InvalidOperationException(), false);

    public void Observe(LootSessionSnapshot? state, long revision)
    {
        lock (_gate)
        {
            if (state?.BattleId == _observedBattleId && revision < _revision) return;
            if (state?.BattleId != _observedBattleId) _retryCommand = null;
            if (state?.BattleId != _state?.BattleId || state?.Phase != LootPhases.Open) _selected = null;
            _observedBattleId = state?.BattleId;
            _state = state?.Phase == LootPhases.Open ? state : null;
            _revision = revision;
        }
    }

    public bool Select(string objectId)
    {
        lock (_gate)
        {
            if (_state?.Objects.Any(s => s.ObjectId == objectId) != true) return false;
            _selected = objectId;
            return true;
        }
    }
    public void ClearSelection() { lock (_gate) _selected = null; }

    public Task<bool> CollectAsync(string itemId, long quantity) => SubmitAsync((id, revision, battle, source) =>
        new CollectLootConsumablesCommand(id, revision, battle, source, itemId, quantity));
    public Task<bool> ScrapAsync() => SubmitAsync((id, revision, battle, source) => new AttemptScrapCommand(id, revision, battle, source));

    public async Task<bool> SubmitAsync(Func<string, long, string, string, DemoCampaignCommand> commandFactory, bool needsSource = true)
    {
        if (Interlocked.CompareExchange(ref _pending, 1, 0) != 0) return false;
        string battle;
        try
        {
            DemoCampaignCommand command;
            lock (_gate)
            {
                if (_state is null || needsSource && _selected is null) return false;
                battle = _state.BattleId;
                var proposed = commandFactory("loot:" + Guid.NewGuid().ToString("N"), _revision, battle, _selected ?? "");
                if (_retryCommand is { } retry && proposed with { CommandId = retry.CommandId, ExpectedRevision = retry.ExpectedRevision } != retry)
                { LastReason = "loot_retry_pending"; return false; }
                command = _retryCommand ?? proposed;
                _retryCommand = command;
            }
            var response = await execute(command);
            lock (_gate)
            {
                if (_state?.BattleId != battle) return false;
                if (response.Reason != DemoCampaignReasons.Unavailable) _retryCommand = null;
                LastReason = response.Reason == DemoCampaignReasons.Unavailable ? "loot_unavailable" : response.Reason;
                if (response.Rewards?.Loot is { } authoritative && authoritative.BattleId == battle)
                    Observe(authoritative, response.Revision);
                else _revision = response.Revision;
            }
            return response.Applied;
        }
        catch (Exception)
        {
            LastReason = "loot_unavailable";
            return false;
        }
        finally { Volatile.Write(ref _pending, 0); }
    }
}
