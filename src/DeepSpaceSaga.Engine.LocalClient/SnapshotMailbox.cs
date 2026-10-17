using System.Collections.Immutable;
using System.Threading.Channels;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.LocalClient;

/// <summary>One latest world state, with undelivered events retained during coalescing.</summary>
internal sealed class SnapshotMailbox
{
    internal const int EventLimit = 4096;
    private long? _reportBaseline;
    private readonly Channel<AuthoritativeSnapshot> _channel = Channel.CreateBounded<AuthoritativeSnapshot>(
        new BoundedChannelOptions(1) { SingleReader = false, SingleWriter = true });

    internal void Publish(AuthoritativeSnapshot snapshot)
    {
        var reports = MergeReports(null, snapshot.CombatReports);
        if (reports is not null)
        {
            if (_reportBaseline is { } baseline && baseline != reports.BaselineSequence)
                throw new InvalidOperationException("Combat report baseline changed within a session.");
            _reportBaseline = reports.BaselineSequence;
            snapshot = snapshot with { CombatReports = reports };
        }
        if ((!snapshot.CommandResults.IsDefault && snapshot.CommandResults.Length > EventLimit) ||
            (!snapshot.ShipEvents.IsDefault && snapshot.ShipEvents.Length > EventLimit) ||
            (!snapshot.DialogueEvents.IsDefault && snapshot.DialogueEvents.Length > EventLimit))
            throw new InvalidOperationException("Snapshot event limit exceeded.");
        if (_channel.Reader.TryRead(out var previous))
            snapshot = snapshot with
            {
                CommandResults = Merge(previous.CommandResults, snapshot.CommandResults, r => r.CommandId),
                ShipEvents = Merge(previous.ShipEvents, snapshot.ShipEvents, e => e.EventId),
                DialogueEvents = Merge(previous.DialogueEvents, snapshot.DialogueEvents, e => e.EventId),
                CombatReports = MergeReports(previous.CombatReports, snapshot.CombatReports)
            };
        if (!_channel.Writer.TryWrite(snapshot)) throw new InvalidOperationException("Snapshot mailbox is closed.");
    }

    private static CombatReportBatchSnapshot? MergeReports(CombatReportBatchSnapshot? older, CombatReportBatchSnapshot? newer)
    {
        if (older is null && newer is null) return null;
        var batch = newer ?? older!;
        if (batch.BaselineSequence < 0 || batch.LatestSequence < batch.BaselineSequence ||
            older is not null && older.BaselineSequence != batch.BaselineSequence)
            throw new InvalidOperationException("Invalid combat report batch baseline.");
        var merged = new SortedDictionary<long, BattleReportSnapshot>();
        foreach (var part in new[] { older, newer })
        {
            if (part is null || part.Reports.IsDefaultOrEmpty) continue;
            foreach (var report in part.Reports)
            {
                if (report is null || report.Sequence <= batch.BaselineSequence || report.Sequence > part.LatestSequence ||
                    !double.IsFinite(report.MotionTimeMs) || report.MotionTimeMs < 0)
                    throw new InvalidOperationException("Invalid combat report sequence or time.");
                if (merged.TryGetValue(report.Sequence, out var previous) && previous != report)
                    throw new InvalidOperationException("Conflicting combat report payload.");
                merged[report.Sequence] = report;
                if (merged.Count > CombatReportBatchSnapshot.PendingLimit)
                    throw new InvalidOperationException("Undelivered combat report limit exceeded.");
            }
        }
        return new(batch.BaselineSequence, Math.Max(older?.LatestSequence ?? batch.LatestSequence, batch.LatestSequence),
            merged.Values.ToImmutableArray());
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
