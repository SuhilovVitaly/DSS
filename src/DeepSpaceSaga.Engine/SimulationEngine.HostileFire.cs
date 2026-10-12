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
                    launcher.ActiveTorpedoObjectId is not null || module.ActiveCycle is not null ||
                !_registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex).CommandTypeIds.Contains(CombatCommandTypes.Fire) ||
                    ResolveWeaponOperator(owner, module, WeaponSkillType.TorpedoAttack) is null) continue;
                double earliest = Math.Max(Math.Max(Math.Ceiling(cursor), _combatProcessedTimeMs + 1),
                    Math.Ceiling(module.DisabledUntilMotionTimeMs ?? cursor));
                if (!CanExecuteModuleCommand(module, earliest)) continue;
                var previous = _combatImpacts.LastOrDefault(i => i.OwnerObjectId == owner.InitialMotion.ObjectId && i.LauncherModuleId == module.ModuleId);
                if (previous is not null) earliest = Math.Max(earliest, Math.Floor(previous.MotionTimeMs) + 1);
                if (earliest > to) continue;
                while (earliest <= to)
                {
                    var entry = TorpedoCollisionMath.FirstContact(CombatPath(owner), CombatPath(player), earliest, to,
                        launcher.RangeKm!.Value * WorldUnitsPerKm);
                    if (entry is null || entry.Value.MotionTimeMs > long.MaxValue) break;
                    long time = (long)Math.Ceiling(entry.Value.MotionTimeMs);
                    if (time > to) break;
                    // A sub-ms grazing contact need not contain an eligible integer launch instant.
                    if (CombatRangeMath.IsWithinRange(PredictMotion(owner, time - owner.StartGameTimeMs),
                        PredictMotion(player, time - player.StartGameTimeMs), launcher.RangeKm.Value))
                    {
                        if (first is null || time < first.Time) first = new(owner.InitialMotion.ObjectId, module.ModuleId, time);
                        break;
                    }
                    if (time == long.MaxValue) break;
                    earliest = time + 1;
                }
            }
        return first;
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
            return TryLaunchTorpedo(owner, moduleId, player.InitialMotion.ObjectId, motionTimeMs);
        }
    }
}
