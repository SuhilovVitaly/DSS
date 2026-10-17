using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private CombatReportsSaveData CaptureCombatReportsState() =>
        new(1, _combatReportSequence, _reportedRangeTargetIds.Order(StringComparer.Ordinal).ToArray());

    private CombatReportsSaveData StageCombatReportsRestore(GameStateData state, bool loadingSave,
        IReadOnlyList<SpaceObjectRuntime> objects,
        IReadOnlyDictionary<(string ObjectId, string ModuleId), LauncherCombatSnapshot> launchers)
    {
        if (!loadingSave) return new(1, 0, []);
        var saved = state.CombatReportsState;
        var ids = new HashSet<string>(saved?.ReportedRangeTargetIds ?? [], StringComparer.Ordinal);
        var player = objects.FirstOrDefault(o => o.InitialMotion.ObjectId == state.PlayerShipObjectId && !o.IsDestroyed);
        if (player is not null && CombatReportScanner(player, state.MotionTimeMs) is not null)
        {
            string? Relation(SpaceObjectRuntime o) => state.AiMap is { } map &&
                map.Bases.Any(b => string.Equals(b.ObjectId, o.InitialMotion.ObjectId, StringComparison.OrdinalIgnoreCase))
                ? PlayerRelation.Enemy : o.RelationToPlayer ?? PlayerRelation.Neutral;
            foreach (var target in CombatReportRangeTargets(player, state.MotionTimeMs, objects, launchers, Relation))
                ids.Add(target.InitialMotion.ObjectId);
        }
        return new(1, saved?.Sequence ?? 0, ids.Order(StringComparer.Ordinal).ToArray());
    }

    private void RestoreCombatReports(CombatReportsSaveData state)
    {
        ResetCombatReports();
        _combatReportBaseline = _combatReportSequence = state.Sequence;
        _reportedRangeTargetIds.UnionWith(state.ReportedRangeTargetIds);
    }
}
