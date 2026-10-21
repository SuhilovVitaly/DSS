using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private sealed record HostileLaunch(string OwnerId, string ModuleId, long Time);
    private bool HasHostileFireCandidates => _objects.Any(o => IsHostileActor(o) && o.AttackAi is not null &&
        o.Modules.Any(m => _launcherCombat.ContainsKey((o.InitialMotion.ObjectId, m.ModuleId))));
    private bool IsHostileActor(SpaceObjectRuntime owner) => owner.ObjectType == SpaceObjectType.NpcShip &&
        !owner.IsDestroyed && GetRelationToPlayer(owner) == PlayerRelation.Enemy;
    private static long NextNpcWeaponCheck(long time) => time > long.MaxValue - HostileOrbitMath.DecisionIntervalMs
        ? long.MaxValue : time - time % HostileOrbitMath.DecisionIntervalMs + HostileOrbitMath.DecisionIntervalMs;

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
                var definition = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
                if (!_launcherCombat.TryGetValue((owner.InitialMotion.ObjectId, module.ModuleId), out var launcher) ||
                    module.ActiveCycle is not null || !definition.CommandTypeIds.Contains(CombatCommandTypes.Launch) ||
                    owner.AttackAi is null || ResolveWeaponOperator(owner, module, WeaponSkillType.TorpedoAttack) is null) continue;
                double earliest = Math.Max(Math.Max(Math.Ceiling(cursor), _combatProcessedTimeMs + 1),
                    Math.Max(Math.Ceiling(module.DisabledUntilMotionTimeMs ?? cursor), Math.Ceiling(launcher.ReadyAtMotionTimeMs ?? cursor)));
                if (!CanExecuteModuleCommand(module, earliest) || earliest > to) continue;
                while (earliest <= to)
                {
                    var entry = TorpedoCollisionMath.FirstContact(CombatPath(owner), CombatPath(player), earliest, to,
                        launcher.RangeKm!.Value * WorldUnitsPerKm);
                    if (entry is null || entry.Value.MotionTimeMs > long.MaxValue) break;
                    long time = (long)Math.Ceiling(entry.Value.MotionTimeMs);
                    if (time > to) break;
                    if (FindNpcPointAim(owner, module, player, time) is not null)
                    {
                        if (first is null || time < first.Time) first = new(owner.InitialMotion.ObjectId, module.ModuleId, time);
                        break;
                    }
                    long retry = NextNpcWeaponCheck(time);
                    if (retry <= time) break;
                    earliest = retry;
                }
            }
        return first;
    }

    // Authoritative NPC actor entry point; never submits commands or player receipts.
    internal CommandStartOutcome TryStartHostileTorpedoFire(string ownerId, string moduleId, long motionTimeMs)
    {
        lock (_worldStateLock)
        {
            var owner = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == ownerId && IsHostileActor(o));
            if (owner is null) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownObject);
            var player = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId &&
                o.ObjectType == SpaceObjectType.PlayerShip && !o.IsDestroyed);
            if (player is null) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownTarget);
            var module = owner.Modules.FirstOrDefault(m => m.ModuleId == moduleId);
            if (module is null || !_launcherCombat.ContainsKey((ownerId, moduleId)))
                return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownModule);
            var aim = FindNpcPointAim(owner, module, player, motionTimeMs);
            return aim is null ? CommandStartOutcome.Rejected("target_unreachable") :
                TryLaunchPointProjectile(owner, moduleId, aim.X, aim.Y, aim.ProfileId, motionTimeMs, ProjectileKind.Torpedo);
        }
    }
}
