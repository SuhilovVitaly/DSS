using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client;

/// <summary>Owned and synchronized by one session's SnapshotBuffer.</summary>
internal sealed class CombatReportInbox
{
    private long? _baseline;
    private long _delivered;
    private readonly SortedDictionary<long, BattleReportSnapshot> _pending = [];
    private readonly Dictionary<long, BattleReportSnapshot> _recent = [];
    private readonly Queue<long> _recentOrder = [];
    internal int PendingCount => _pending.Count;
    internal int RecentCount => _recent.Count;

    internal void Receive(CombatReportBatchSnapshot? batch)
    {
        if (batch is null) return;
        if (batch.BaselineSequence < 0 || batch.LatestSequence < batch.BaselineSequence ||
            _baseline is { } baseline && baseline != batch.BaselineSequence)
            throw new InvalidOperationException("Combat report session baseline mismatch.");
        if (_baseline is null) _delivered = batch.BaselineSequence;
        _baseline = batch.BaselineSequence;
        if (batch.Reports.IsDefaultOrEmpty) return;
        foreach (var report in batch.Reports)
        {
            if (report is null || report.Sequence <= batch.BaselineSequence || report.Sequence > batch.LatestSequence ||
                !double.IsFinite(report.MotionTimeMs) || report.MotionTimeMs < 0)
                throw new InvalidOperationException("Invalid combat report sequence or time.");
            if ((_pending.TryGetValue(report.Sequence, out var previous) || _recent.TryGetValue(report.Sequence, out previous)) && previous != report)
                throw new InvalidOperationException("Conflicting combat report payload.");
            if (report.Sequence <= _delivered || _pending.ContainsKey(report.Sequence)) continue;
            if (_pending.Count >= CombatReportBatchSnapshot.PendingLimit)
                throw new InvalidOperationException("Combat report inbox overflow.");
            _pending.Add(report.Sequence, report);
        }
    }

    internal ImmutableArray<BattleReportSnapshot> Drain()
    {
        var reports = ImmutableArray.CreateBuilder<BattleReportSnapshot>();
        // Do not acknowledge across a gap: an older snapshot may still carry it.
        while (_delivered < long.MaxValue && _pending.Remove(_delivered + 1, out var report))
        {
            _delivered++;
            reports.Add(report);
            _recent.Add(report.Sequence, report);
            _recentOrder.Enqueue(report.Sequence);
            if (_recent.Count > CombatReportBatchSnapshot.PendingLimit) _recent.Remove(_recentOrder.Dequeue());
        }
        return reports.ToImmutable();
    }
}
