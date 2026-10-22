using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private IEnumerable<SpaceObjectRuntime> CombatReportRangeTargets(SpaceObjectRuntime player, double motionTimeMs) =>
        CombatReportRangeTargets(player, motionTimeMs, _objects, _launcherCombat, GetRelationToPlayer);

    private IEnumerable<SpaceObjectRuntime> CombatReportRangeTargets(SpaceObjectRuntime player, double motionTimeMs,
        IEnumerable<SpaceObjectRuntime> objects,
        IReadOnlyDictionary<(string ObjectId, string ModuleId), LauncherCombatSnapshot> launchers,
        Func<SpaceObjectRuntime, string?> relation)
    {
        var ranges = player.Modules.Where(m => CanExecuteModuleCommand(m, motionTimeMs))
            .Select(m => _registry.ModuleTypes.GetDefinition(m.ModuleTypeIndex))
            .Where(t => t.IsPointLauncher
                ? t.CategoryTypeId == "module.torpedo.launcher" && t.CommandTypeIds.Contains(CombatCommandTypes.Launch) && t.RangeKm is > 0
                : t.CommandTypeIds.Contains(CombatCommandTypes.Fire) && t.TorpedoRangeKm is > 0)
            .Select(t => t.IsPointLauncher ? t.RangeKm!.Value : t.TorpedoRangeKm!.Value).ToArray();
        if (ranges.Length == 0) yield break;
        var origin = CombatPath(player).Position(motionTimeMs);
        foreach (var target in objects.Where(o => o.IsKnown && !o.IsDestroyed &&
            o.InitialMotion.ObjectId != player.InitialMotion.ObjectId &&
            o.ObjectType is SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip &&
            relation(o) == PlayerRelation.Enemy).OrderBy(o => o.InitialMotion.ObjectId, StringComparer.Ordinal))
        {
            var position = CombatPath(target).Position(motionTimeMs);
            if (ranges.Any(range => CombatRangeMath.IsWithinRange(
                new("origin", origin.X, origin.Y, 0, 0), new("target", position.X, position.Y, 0, 0), range)))
                yield return target;
        }
    }
}
