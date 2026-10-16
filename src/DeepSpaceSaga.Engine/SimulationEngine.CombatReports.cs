using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private long _combatReportSequence;
    private long _combatReportBaseline;
    private readonly List<BattleReportSnapshot> _pendingCombatReports = [];
    private readonly HashSet<string> _reportedRangeTargetIds = new(StringComparer.Ordinal);

    private void ResetCombatReports()
    {
        _combatReportSequence = 0;
        _combatReportBaseline = 0;
        _pendingCombatReports.Clear();
        _reportedRangeTargetIds.Clear();
    }

    // Called only while holding the world lock, at the authoritative physical event.
    private void EmitCombatReport(BattleReportSnapshot report)
    {
        if (_pendingCombatReports.Count >= CombatReportBatchSnapshot.PendingLimit)
            throw new InvalidOperationException("Combat report outbox overflow: session cannot continue without losing reports.");
        if (!double.IsFinite(report.MotionTimeMs) || report.MotionTimeMs < 0)
            throw new ArgumentOutOfRangeException(nameof(report));
        var sequence = checked(_combatReportSequence + 1);
        _pendingCombatReports.Add(report with { Sequence = sequence });
        _combatReportSequence = sequence;
    }

    private CombatReportBatchSnapshot CaptureCombatReportBatch() =>
        new(_combatReportBaseline, _combatReportSequence, _pendingCombatReports.ToImmutableArray());

    // Filled by the following range and confirmed-fact tickets.
    private void EvaluateCombatReportRange(double motionTimeMs) { }
    private void ObserveCombatReportFact(CombatJournalEntry entry) { }
}
