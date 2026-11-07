using System.Collections.Immutable;
using System.Threading.Channels;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.LocalClient;

/// <summary>One latest world state, with undelivered events retained during coalescing.</summary>
internal sealed class SnapshotMailbox
{
    internal const int EventLimit = 4096;
    private readonly Channel<AuthoritativeSnapshot> _channel = Channel.CreateBounded<AuthoritativeSnapshot>(
        new BoundedChannelOptions(1) { SingleReader = false, SingleWriter = true });

    internal void Publish(AuthoritativeSnapshot snapshot)
    {
        if ((!snapshot.CommandResults.IsDefault && snapshot.CommandResults.Length > EventLimit) ||
            (!snapshot.ShipEvents.IsDefault && snapshot.ShipEvents.Length > EventLimit) ||
            (!snapshot.DialogueEvents.IsDefault && snapshot.DialogueEvents.Length > EventLimit))
            throw new InvalidOperationException("Snapshot event limit exceeded.");
        if (_channel.Reader.TryRead(out var previous))
            snapshot = snapshot with
            {
                CommandResults = Merge(previous.CommandResults, snapshot.CommandResults, r => r.CommandId),
                ShipEvents = Merge(previous.ShipEvents, snapshot.ShipEvents, e => e.EventId),
                DialogueEvents = Merge(previous.DialogueEvents, snapshot.DialogueEvents, e => e.EventId)
            };
        if (!_channel.Writer.TryWrite(snapshot)) throw new InvalidOperationException("Snapshot mailbox is closed.");
    }

    private static ImmutableArray<T> Merge<T>(ImmutableArray<T> older, ImmutableArray<T> newer, Func<T, string> key)
    {
        if (older.IsDefaultOrEmpty) return newer.IsDefault ? ImmutableArray<T>.Empty : newer;
        if (newer.IsDefaultOrEmpty) return older;
        var merged = new Dictionary<string, T>(StringComparer.Ordinal);
        if (!older.IsDefault) foreach (var item in older) merged[key(item)] = item;
        if (!newer.IsDefault) foreach (var item in newer) merged[key(item)] = item;
        if (merged.Count > EventLimit)
            throw new InvalidOperationException("Snapshot consumer is too slow: undelivered event limit exceeded.");
        return merged.Values.ToImmutableArray();
    }

    internal IAsyncEnumerable<AuthoritativeSnapshot> ReadAllAsync(CancellationToken token) => _channel.Reader.ReadAllAsync(token);
    internal void Complete(Exception? error = null) => _channel.Writer.TryComplete(error);
}
