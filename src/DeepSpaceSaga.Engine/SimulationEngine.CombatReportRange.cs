using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private IEnumerable<SpaceObjectRuntime> CombatReportRangeTargets(SpaceObjectRuntime player, double motionTimeMs)
    {
        var ranges = player.Modules.Where(m => CanExecuteModuleCommand(m, motionTimeMs))
            .Where(m => !_launcherCombat.TryGetValue((player.InitialMotion.ObjectId, m.ModuleId), out var launcher) || !launcher.IsBrokenUntilDock)
            .Select(m => _registry.ModuleTypes.GetDefinition(m.ModuleTypeIndex))
            .Where(t => t.CommandTypeIds.Contains(CombatCommandTypes.Fire) && t.TorpedoRangeKm is > 0)
            .Select(t => t.TorpedoRangeKm!.Value).ToArray();
        if (ranges.Length == 0) yield break;
        var origin = CombatPath(player).Position(motionTimeMs);
        foreach (var target in _objects.Where(o => o.IsKnown && !o.IsDestroyed &&
            o.InitialMotion.ObjectId != player.InitialMotion.ObjectId &&
            o.ObjectType is SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip &&
            GetRelationToPlayer(o) == PlayerRelation.Enemy).OrderBy(o => o.InitialMotion.ObjectId, StringComparer.Ordinal))
        {
            var position = CombatPath(target).Position(motionTimeMs);
            if (ranges.Any(range => CombatRangeMath.IsWithinRange(
                new("origin", origin.X, origin.Y, 0, 0), new("target", position.X, position.Y, 0, 0), range)))
                yield return target;
        }
    }
}
