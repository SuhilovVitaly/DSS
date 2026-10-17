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
        var scanner = CombatReportScanner(player, motionTimeMs);
        if (scanner is null) return;
        var reporter = CaptureReportIdentity(player, scanner.ModuleId, BattleReportReporterRole.ScannerOperator);
        foreach (var target in CombatReportRangeTargets(player, motionTimeMs))
        {
            if (_reportedRangeTargetIds.Contains(target.InitialMotion.ObjectId)) continue;
            var source = CaptureKnownParticipant(player.InitialMotion.ObjectId);
            var destination = CaptureKnownParticipant(target.InitialMotion.ObjectId);
            EmitCombatReport(new(0, motionTimeMs, BattleReportKind.EnemyInRange,
                $"range:{target.InitialMotion.ObjectId}", reporter,
                SourceObjectId: source.Id, SourceDisplayName: source.Name,
                TargetObjectId: destination.Id, TargetDisplayName: destination.Name));
            _reportedRangeTargetIds.Add(target.InitialMotion.ObjectId);
        }
    }

    private void ObserveCombatReportFact(CombatJournalEntry entry)
    {
        if (entry.Type == CombatEventType.Intercept)
        {
            ObserveCombatReportIntercept(entry);
            return;
        }
        if (entry.Type != CombatEventType.Launch) return;
        var projectile = _objects.Find(o => o.InitialMotion.ObjectId == entry.ProjectileObjectId);
        var owner = _objects.Find(o => o.InitialMotion.ObjectId == entry.ActorObjectId);
        if (projectile is null || owner is null) return;
        BattleReportKind kind;
        BattleReportReporterSnapshot reporter;
        if (projectile.InitialMotion.Torpedo is { } torpedo)
        {
            if (owner.InitialMotion.ObjectId == PlayerShipObjectId)
            {
                kind = BattleReportKind.PlayerTorpedoLaunch;
                reporter = CaptureReportIdentity(owner, torpedo.LauncherModuleId, BattleReportReporterRole.TorpedoOperator);
            }
            else
            {
                if (!owner.IsKnown || GetRelationToPlayer(owner) != PlayerRelation.Enemy || torpedo.TargetObjectId != PlayerShipObjectId) return;
                var player = _objects.Find(o => o.InitialMotion.ObjectId == PlayerShipObjectId && !o.IsDestroyed);
                var scanner = player is null ? null : CombatReportScanner(player, entry.MotionTimeMs);
                if (scanner is null) return;
                kind = BattleReportKind.EnemyTorpedoLaunch;
                reporter = CaptureReportIdentity(player!, scanner.ModuleId, BattleReportReporterRole.ScannerOperator);
            }
        }
        else if (projectile.InitialMotion.Countermeasure is { } defense && owner.InitialMotion.ObjectId == PlayerShipObjectId)
        {
            kind = BattleReportKind.PlayerCountermeasureLaunch;
            reporter = CaptureReportIdentity(owner, defense.LauncherModuleId, BattleReportReporterRole.CountermeasureOperator);
        }
        else return;
        var source = CaptureKnownParticipant(entry.ActorObjectId);
        var target = CaptureKnownParticipant(entry.TargetObjectId);
        EmitCombatReport(new(0, entry.MotionTimeMs, kind, entry.ProjectileObjectId, reporter,
            source.Id, source.Name, target.Id, target.Name, entry.ProjectileObjectId, entry.LauncherModuleId));
    }

    private void ObserveCombatReportIntercept(CombatJournalEntry entry)
    {
        var projectile = _objects.Find(o => o.InitialMotion.ObjectId == entry.ProjectileObjectId);
        var target = _objects.Find(o => o.InitialMotion.ObjectId == entry.TargetObjectId);
        if (projectile?.InitialMotion.Countermeasure is not { } defense || defense.OwnerObjectId != PlayerShipObjectId ||
            target?.InitialMotion.Torpedo is not { OwnerPlayerRelationAtLaunch: PlayerRelation.Enemy }) return;
        var reporter = CaptureWeaponReportIdentity(defense.RatingBreakdown.DefenseOperator,
            BattleReportReporterRole.CountermeasureOperator);
        var source = CaptureKnownParticipant(defense.OwnerObjectId);
        var destination = CaptureKnownParticipant(defense.TargetTorpedoId);
        EmitCombatReport(new(0, entry.MotionTimeMs, BattleReportKind.EnemyTorpedoIntercepted,
            $"intercept:{entry.ProjectileObjectId}:{entry.TargetObjectId}", reporter,
            source.Id, source.Name, destination.Id, destination.Name, entry.ProjectileObjectId, defense.LauncherModuleId));
    }

    private void EmitCombatContactReport(SpaceObjectRuntime projectile, SpaceObjectRuntime target,
        double motionTimeMs, ModuleHitOutcome? hit, bool destroyed)
    {
        var flight = projectile.InitialMotion.Torpedo!;
        bool playerDamage = target.InitialMotion.ObjectId == PlayerShipObjectId;
        if (playerDamage)
        {
            if (hit is not { } damage || damage.ArmorAfter >= damage.ArmorBefore && damage.StructureAfter >= damage.StructureBefore) return;
        }
        else if (flight.OwnerObjectId != PlayerShipObjectId || GetRelationToPlayer(target) != PlayerRelation.Enemy) return;
        var reporter = playerDamage
            ? CaptureReportIdentity(target, null, BattleReportReporterRole.Captain)
            : CaptureWeaponReportIdentity(flight.RatingBreakdown, BattleReportReporterRole.TorpedoOperator);
        var source = CaptureKnownParticipant(flight.OwnerObjectId);
        bool targetKnown = playerDamage || target.IsKnown;
        BattleReportDamageSnapshot? facts = null;
        if (targetKnown && hit is { } moduleHit)
        {
            var module = target.Modules.First(m => m.ModuleId == moduleHit.ModuleId);
            facts = new(moduleHit.ModuleId, _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex).DisplayName,
                moduleHit.ArmorBefore, moduleHit.ArmorAfter, moduleHit.StructureBefore, moduleHit.StructureAfter);
        }
        string? targetId = targetKnown ? target.InitialMotion.ObjectId : null;
        EmitCombatReport(new(0, motionTimeMs,
            playerDamage ? BattleReportKind.PlayerShipDamaged : BattleReportKind.EnemyShipHit,
            $"contact:{projectile.InitialMotion.ObjectId}:{targetId ?? "unknown"}", reporter,
            source.Id, source.Name, targetId, targetKnown ? target.Name : null,
            projectile.InitialMotion.ObjectId, source.Id is null ? null : flight.LauncherModuleId,
            playerDamage ? BattleReportVariant.None : destroyed ? BattleReportVariant.Destroyed : BattleReportVariant.Hit, facts));
    }
}
