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

    private void EvaluateCombatReportRange(double motionTimeMs)
    {
        var player = _objects.Find(o => o.InitialMotion.ObjectId == PlayerShipObjectId && !o.IsDestroyed);
        if (player is null) return;
        var scanner = player.Modules.Where(m => IsResourceScanner(m) && CanExecuteModuleCommand(m, motionTimeMs))
            .OrderBy(m => m.ModuleId, StringComparer.Ordinal).FirstOrDefault();
        if (scanner is null) return;
        var crew = player.Crew.IsDefaultOrEmpty ? null : player.Crew.FirstOrDefault(c => c.Id == scanner.OperatorCrewId);
        var reporter = new BattleReportReporterSnapshot(BattleReportReporterRole.ScannerOperator, crew?.Id, crew?.DisplayName);
        foreach (var target in CombatReportRangeTargets(player, motionTimeMs))
        {
            if (_reportedRangeTargetIds.Contains(target.InitialMotion.ObjectId)) continue;
            EmitCombatReport(new(0, motionTimeMs, BattleReportKind.EnemyInRange,
                $"range:{target.InitialMotion.ObjectId}", reporter,
                SourceObjectId: player.InitialMotion.ObjectId, SourceDisplayName: player.Name,
                TargetObjectId: target.InitialMotion.ObjectId, TargetDisplayName: target.Name));
            _reportedRangeTargetIds.Add(target.InitialMotion.ObjectId);
        }
    }

    private void ObserveCombatReportFact(CombatJournalEntry entry) { }
}
