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
        if (UsesPointEncounters) report = report with
        {
            LauncherModuleId = report.SourceObjectId == PlayerShipObjectId ? report.LauncherModuleId : null,
            Damage = report.Kind == BattleReportKind.PlayerShipDamaged ? report.Damage : null
        };
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
        if (entry.Type == CombatEventType.ShotDown && entry.ProjectileKind == ProjectileKind.Torpedo)
        {
            ObservePointInterceptReport(entry);
            return;
        }
        if (entry.Type == CombatEventType.Launch &&
            _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == entry.ProjectileObjectId) is { InitialMotion.OwnProjectile: not null } point)
        {
            ObservePointLaunchReport(point, entry);
            return;
        }
    }

    private void ObservePointLaunchReport(SpaceObjectRuntime projectile, CombatJournalEntry entry)
    {
        var flight = projectile.InitialMotion.OwnProjectile!;
        var owner = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == flight.OwnerObjectId);
        if (owner is null) return;
        bool own = flight.OwnerObjectId == PlayerShipObjectId;
        BattleReportKind kind;
        BattleReportReporterSnapshot reporter;
        if (own)
        {
            kind = flight.Kind == ProjectileKind.Torpedo ? BattleReportKind.PlayerTorpedoLaunch : BattleReportKind.PlayerCountermeasureLaunch;
            reporter = PointWeaponReporter(projectile);
        }
        else
        {
            if (flight.Kind != ProjectileKind.Torpedo || !owner.IsKnown || GetRelationToPlayer(owner) != PlayerRelation.Enemy) return;
            var player = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId && !o.IsDestroyed);
            var scanner = player is null ? null : CombatReportScanner(player, entry.MotionTimeMs);
            if (scanner is null) return;
            kind = BattleReportKind.EnemyTorpedoLaunch;
            reporter = CaptureReportIdentity(player!, scanner.ModuleId, BattleReportReporterRole.ScannerOperator);
        }
        var source = CaptureKnownParticipant(flight.OwnerObjectId);
        EmitCombatReport(new(0, entry.MotionTimeMs, kind, entry.ProjectileObjectId, reporter,
            SourceObjectId: source.Id, SourceDisplayName: source.Name, ProjectileObjectId: entry.ProjectileObjectId,
            LauncherModuleId: own ? flight.LauncherModuleId : null,
            X: entry.X, Y: entry.Y, ProjectileKind: flight.Kind, ProfileId: flight.ProfileId, BlastRadiusKm: flight.BlastRadiusKm));
    }

    private void ObservePointInterceptReport(CombatJournalEntry entry)
    {
        if (entry.ActorObjectId != PlayerShipObjectId) return;
        var counter = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == entry.Result &&
            o.InitialMotion.OwnProjectile is { Kind: ProjectileKind.Countermissile });
        var victim = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == entry.ProjectileObjectId);
        string? victimOwner = victim?.InitialMotion.OwnProjectile?.OwnerObjectId;
        string? relation = victim?.InitialMotion.OwnProjectile?.OwnerPlayerRelationAtLaunch;
        bool hostile = relation is not null ? relation == PlayerRelation.Enemy : victimOwner is not null &&
            (_encounterEnemyIds.Contains(victimOwner) || _objects.Any(o => o.InitialMotion.ObjectId == victimOwner && IsHostileActor(o)));
        if (counter is null || victimOwner is null || !hostile) return;
        var flight = counter.InitialMotion.OwnProjectile!;
        var source = CaptureKnownParticipant(flight.OwnerObjectId);
        EmitCombatReport(new(0, entry.MotionTimeMs, BattleReportKind.EnemyTorpedoIntercepted,
            $"intercept:{counter.InitialMotion.ObjectId}:{entry.ProjectileObjectId}", PointWeaponReporter(counter),
            SourceObjectId: source.Id, SourceDisplayName: source.Name, TargetObjectId: entry.ProjectileObjectId,
            TargetDisplayName: victim!.Name, ProjectileObjectId: counter.InitialMotion.ObjectId, LauncherModuleId: flight.LauncherModuleId,
            X: entry.X, Y: entry.Y, ProjectileKind: flight.Kind, ProfileId: flight.ProfileId,
            BlastRadiusKm: flight.BlastRadiusKm, HitResult: ProjectileHitResult.ShotDown));
    }

    private void EmitPointContactReport(SpaceObjectRuntime projectile, SpaceObjectRuntime target, double time,
        ModuleHitOutcome hit, ProjectileHitResult result, bool destroyed)
    {
        var flight = projectile.InitialMotion.OwnProjectile!;
        bool ownDamage = target.InitialMotion.ObjectId == PlayerShipObjectId;
        if (!ownDamage && (flight.OwnerObjectId != PlayerShipObjectId || GetRelationToPlayer(target) != PlayerRelation.Enemy)) return;
        var reporter = ownDamage ? CaptureReportIdentity(target, null, BattleReportReporterRole.Captain) : PointWeaponReporter(projectile);
        var source = CaptureKnownParticipant(flight.OwnerObjectId);
        bool known = ownDamage || target.IsKnown;
        BattleReportDamageSnapshot? damage = ownDamage ? new(hit.ModuleId,
            _registry.ModuleTypes.GetDefinition(target.Modules.First(m => m.ModuleId == hit.ModuleId).ModuleTypeIndex).DisplayName,
            hit.ArmorBefore, hit.ArmorAfter, hit.StructureBefore, hit.StructureAfter) : null;
        EmitCombatReport(new(0, time, ownDamage ? BattleReportKind.PlayerShipDamaged : BattleReportKind.EnemyShipHit,
            $"contact:{projectile.InitialMotion.ObjectId}:{(known ? target.InitialMotion.ObjectId : "unknown")}", reporter,
            SourceObjectId: source.Id, SourceDisplayName: source.Name,
            TargetObjectId: known ? target.InitialMotion.ObjectId : null, TargetDisplayName: known ? target.Name : null,
            ProjectileObjectId: projectile.InitialMotion.ObjectId, LauncherModuleId: flight.OwnerObjectId == PlayerShipObjectId ? flight.LauncherModuleId : null,
            Variant: ownDamage ? BattleReportVariant.None : destroyed ? BattleReportVariant.Destroyed : BattleReportVariant.Hit,
            Damage: damage, X: CombatPath(projectile).Position(time).X, Y: CombatPath(projectile).Position(time).Y,
            ProjectileKind: flight.Kind, ProfileId: flight.ProfileId, BlastRadiusKm: flight.BlastRadiusKm, HitResult: result));
    }

}
