using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>UI-owned bounded history, independent of map selection and saved simulation state.</summary>
internal sealed class CombatReportStore
{
    internal const int HistoryLimit = 200;
    private readonly List<BattleReportSnapshot> _reports = [];
    private long _latestSequence;
    internal IReadOnlyList<BattleReportSnapshot> Reports => _reports;
    internal long? ExpandedSequence { get; private set; }
    internal long? KeyboardSelection { get; private set; }
    internal float ScrollOffset { get; set; }

    internal bool Receive(IEnumerable<BattleReportSnapshot> reports)
    {
        bool changed = false;
        foreach (var report in reports.OrderBy(r => r.Sequence))
        {
            if (report.Sequence <= _latestSequence) continue;
            _reports.Insert(0, report);
            _latestSequence = report.Sequence;
            ExpandedSequence = KeyboardSelection = report.Sequence;
            changed = true;
            if (_reports.Count > HistoryLimit) _reports.RemoveAt(_reports.Count - 1);
        }
        if (changed) ScrollOffset = 0;
        return changed;
    }

    internal bool Expand(long sequence)
    {
        if (!_reports.Any(r => r.Sequence == sequence)) return false;
        ExpandedSequence = KeyboardSelection = sequence;
        return true;
    }

    internal void MoveSelection(int delta)
    {
        if (_reports.Count == 0) return;
        int index = _reports.FindIndex(r => r.Sequence == KeyboardSelection);
        index = Math.Clamp((index < 0 ? 0 : index) + delta, 0, _reports.Count - 1);
        KeyboardSelection = _reports[index].Sequence;
    }

    internal void Reset()
    {
        _reports.Clear();
        _latestSequence = 0;
        ExpandedSequence = KeyboardSelection = null;
        ScrollOffset = 0;
    }
}
