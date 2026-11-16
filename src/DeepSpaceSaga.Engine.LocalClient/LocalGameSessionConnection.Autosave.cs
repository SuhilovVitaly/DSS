namespace DeepSpaceSaga.Engine.LocalClient;

public sealed partial class LocalGameSessionConnection
{
    private readonly object _autosaveGate = new();
    private long _autosaveGeneration;
    private long _autosaveSequence;
    private Task? _autosaveTask;

    public ValueTask RequestAutosaveAsync(long committedSequence, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentOutOfRangeException.ThrowIfNegative(committedSequence);
        ObjectDisposedException.ThrowIf(_disposed, this);
        lock (_autosaveGate)
        {
            _autosaveGeneration++;
            _autosaveSequence = Math.Max(_autosaveSequence, committedSequence);
            if (_autosaveTask is null || _autosaveTask.IsCompleted)
                _autosaveTask = Task.Run(WriteAutosavesAsync);
            return new(_autosaveTask.WaitAsync(cancellationToken));
        }
    }

    private async Task WriteAutosavesAsync()
    {
        while (true)
        {
            long generation, sequence;
            lock (_autosaveGate) { generation = _autosaveGeneration; sequence = _autosaveSequence; }
            await WriteSaveAsync(SaveSlotNaming.ContinuationSlotId, sequence, _cts.Token).ConfigureAwait(false);
            lock (_autosaveGate)
            {
                if (generation != _autosaveGeneration) continue;
                _autosaveTask = null;
                return;
            }
        }
    }
}
