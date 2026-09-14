using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client;

/// <summary>Receipt-time history independent of whether any UI frame was rendered.</summary>
internal sealed class SessionEventHistory
{
    internal const int Limit = 4096;
    private readonly Dictionary<string, CommandResult> _commands = new(StringComparer.Ordinal);
    private readonly Queue<string> _commandOrder = new();
    private readonly Dictionary<string, ShipEvent> _events = new(StringComparer.Ordinal);
    private readonly Queue<string> _eventOrder = new();

    internal void Receive(AuthoritativeSnapshot snapshot)
    {
        if (!snapshot.CommandResults.IsDefault)
            foreach (var result in snapshot.CommandResults)
            {
                if (!_commands.ContainsKey(result.CommandId)) _commandOrder.Enqueue(result.CommandId);
                _commands[result.CommandId] = result;
            }
        if (!snapshot.ShipEvents.IsDefault)
            foreach (var item in snapshot.ShipEvents)
            {
                if (!_events.ContainsKey(item.EventId)) _eventOrder.Enqueue(item.EventId);
                _events[item.EventId] = item;
            }
        while (_commandOrder.Count > Limit) _commands.Remove(_commandOrder.Dequeue());
        while (_eventOrder.Count > Limit) _events.Remove(_eventOrder.Dequeue());
    }

    internal CommandResult? Find(string commandId) => _commands.GetValueOrDefault(commandId);
    internal ShipEvent[] ReadEvents() => _eventOrder.Select(id => _events[id]).ToArray();
}
