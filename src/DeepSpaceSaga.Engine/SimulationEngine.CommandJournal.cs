using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    internal const int CommandReceiptLimit = 4096;
    private readonly HashSet<string> _knownCommands = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CommandResult> _commandReceipts = new(StringComparer.Ordinal);
    private readonly Queue<string> _receiptOrder = new();
    private readonly Dictionary<string, CommandResult> _replayedResults = new(StringComparer.Ordinal);

    private void RememberResult(CommandResult result)
    {
        if (result.Status == CommandResultStatus.Deferred) return;
        lock (_commandGate)
        {
            if (!_commandReceipts.ContainsKey(result.CommandId)) _receiptOrder.Enqueue(result.CommandId);
            _commandReceipts[result.CommandId] = result;
            _knownCommands.Add(result.CommandId);
            while (_receiptOrder.Count > CommandReceiptLimit)
            {
                string id = _receiptOrder.Dequeue();
                _commandReceipts.Remove(id);
                if (!_objects.Any(o => o.Modules.Any(m => m.ActiveCycle?.CommandId == id)))
                    _knownCommands.Remove(id);
            }
        }
    }

    private void RestoreCommandJournal(GameStateData state)
    {
        lock (_commandGate)
        {
            _knownCommands.Clear(); _commandReceipts.Clear(); _receiptOrder.Clear();
            _pendingCommands.Clear(); _commandResults.Clear(); _replayedResults.Clear();
            foreach (var result in state.CommandReceipts ?? []) RememberResult(result);
            foreach (var cycle in _objects.SelectMany(o => o.Modules).Select(m => m.ActiveCycle))
                if (cycle?.CommandId is { } id) _knownCommands.Add(id);
            foreach (var command in state.PendingCommands ?? [])
                if (_knownCommands.Add(command.CommandId)) _pendingCommands.Add(command);
        }
    }

    private CommandResult[] CaptureCommandReceipts()
    {
        lock (_commandGate) return _receiptOrder.Select(id => _commandReceipts[id]).ToArray();
    }

    private PlayerCommand[] CapturePendingCommands()
    {
        lock (_commandGate) return _pendingCommands.ToArray();
    }
}
