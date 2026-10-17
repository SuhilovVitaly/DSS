using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private sealed record HostileLaunch(string OwnerId, string ModuleId, long Time);

    private bool HasHostileFireCandidates => _objects.Any(o => IsHostileActor(o) &&
        o.Modules.Any(m => _launcherCombat.ContainsKey((o.InitialMotion.ObjectId, m.ModuleId))));

    private bool IsHostileActor(SpaceObjectRuntime owner) => owner.ObjectType == SpaceObjectType.NpcShip &&
        !owner.IsDestroyed && GetRelationToPlayer(owner) == PlayerRelation.Enemy;

    private HostileLaunch? NextHostileLaunch(double cursor, long to)
    {
        if (to <= _combatProcessedTimeMs || cursor > to || _combatProcessedTimeMs == long.MaxValue || !TryGetNextTorpedoId(out _, out _)) return null;
        var player = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId &&
            o.ObjectType == SpaceObjectType.PlayerShip && !o.IsDestroyed);
        if (player is null) return null;
        HostileLaunch? first = null;
        foreach (var owner in _objects.Where(IsHostileActor).OrderBy(o => o.InitialMotion.ObjectId, StringComparer.Ordinal))
            foreach (var module in owner.Modules.OrderBy(m => m.ModuleId, StringComparer.Ordinal))
            {
                if (!_launcherCombat.TryGetValue((owner.InitialMotion.ObjectId, module.ModuleId), out var launcher) ||
                    launcher.IsBrokenUntilDock || module.ActiveCycle is not null ||
                !_registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex).CommandTypeIds.Contains(CombatCommandTypes.Fire) ||
                    ResolveWeaponOperator(owner, module, WeaponSkillType.TorpedoAttack) is null)
                { ClearCombatReactions(owner.InitialMotion.ObjectId, module.ModuleId); continue; }
                if (module.DisabledUntilMotionTimeMs > cursor || !CanExecuteModuleCommand(module, cursor) ||
                    !CombatRangeMath.IsWithinRange(PredictMotion(owner, (long)Math.Ceiling(cursor) - owner.StartGameTimeMs),
                        PredictMotion(player, (long)Math.Ceiling(cursor) - player.StartGameTimeMs), launcher.RangeKm!.Value))
                    ClearCombatReactions(owner.InitialMotion.ObjectId, module.ModuleId);
                double earliest = Math.Max(Math.Max(Math.Ceiling(cursor), _combatProcessedTimeMs + 1),
                    Math.Max(Math.Ceiling(module.DisabledUntilMotionTimeMs ?? cursor),
                        Math.Ceiling(launcher.ReadyAtMotionTimeMs ?? cursor)));
                if (!CanExecuteModuleCommand(module, earliest)) continue;
                if (earliest > to) continue;
                while (earliest <= to)
                {
                    var entry = TorpedoCollisionMath.FirstContact(CombatPath(owner), CombatPath(player), earliest, to,
                        launcher.RangeKm!.Value * WorldUnitsPerKm);
                    if (entry is null || entry.Value.MotionTimeMs > long.MaxValue) break;
                    long time = (long)Math.Ceiling(entry.Value.MotionTimeMs);
                    if (time > to) break;
                    var delayed = ReactionTime("hostile", owner.InitialMotion.ObjectId, module.ModuleId,
                        player.InitialMotion.ObjectId, time, launcher.ReadyAtMotionTimeMs ?? 0);
                    if (delayed is null || delayed > to) break;
                    time = delayed.Value;
                    // A sub-ms grazing contact need not contain an eligible integer launch instant.
                    if (CombatRangeMath.IsWithinRange(PredictMotion(owner, time - owner.StartGameTimeMs),
                        PredictMotion(player, time - player.StartGameTimeMs), launcher.RangeKm.Value))
                    {
                        if (CanHostileIntercept(owner, module, player, launcher, time))
                        {
                            if (first is null || time < first.Time) first = new(owner.InitialMotion.ObjectId, module.ModuleId, time);
                            break;
                        }
                        long nextCheck = NextGuidanceBoundary(time);
                        if (nextCheck <= time) break;
                        earliest = nextCheck;
                        continue;
                    }
                    if (time == long.MaxValue) break;
                    earliest = time + 1;
                }
            }
        return first;
    }

    private bool CanHostileIntercept(SpaceObjectRuntime owner, InstalledModuleRuntime module,
        SpaceObjectRuntime player, LauncherCombatSnapshot launcher, long time)
    {
        var origin = PredictMotion(owner, time - owner.StartGameTimeMs);
        var target = PredictMotion(player, time - player.StartGameTimeMs);
        var route = TorpedoGuidanceMath.Plan(origin, target, launcher.SpeedKmS, launcher.TurnRateDegPerSec, time);
        long lifetime = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex).TorpedoMaxFlightTimeMs ?? 120000;
        return ImpactTime(route) is { } impact && impact - time <= lifetime * 0.8;
    }

    // Internal actor entry point; it cannot submit commands or write player receipts.
    internal CommandStartOutcome TryStartHostileTorpedoFire(string ownerId, string moduleId, long motionTimeMs)
    {
        lock (_worldStateLock)
        {
            var owner = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == ownerId &&
                IsHostileActor(o));
            if (owner is null) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownObject);
            var player = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId &&
                o.ObjectType == SpaceObjectType.PlayerShip && !o.IsDestroyed);
            if (player is null) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownTarget);
            var module = owner.Modules.FirstOrDefault(m => m.ModuleId == moduleId);
            if (module is null || !_launcherCombat.TryGetValue((ownerId, moduleId), out var launcher))
                return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownModule);
            if (!CanHostileIntercept(owner, module, player, launcher, motionTimeMs))
                return CommandStartOutcome.Rejected("target_unreachable");
            var result = TryLaunchTorpedo(owner, moduleId, player.InitialMotion.ObjectId, motionTimeMs);
            if (result.Disposition == CommandStartDisposition.Started) ClearCombatReactions(ownerId, moduleId);
            return result;
        }
    }
}
