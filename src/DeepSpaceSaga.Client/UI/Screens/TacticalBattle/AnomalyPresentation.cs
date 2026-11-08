using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

/// <summary>Presentation geometry only. Chances and fire prohibitions come from Engine snapshots.</summary>
internal static class AnomalyPresentation
{
    internal static bool Inside(TacticalBattleSnapshot battle, TacticalAnomalyKind kind, TacticalPoseSnapshot point) =>
        battle.Rules.Anomalies is { } r && TacticalAnomalyMath.Inside(AnomalyFieldLayer.ToMotion(battle.Anomalies), kind,
            point.X, point.Y, r.Background, r.ZoneEdge);
    internal static ImmutableArray<TacticalAnomalySegment> Segments(TacticalBattleSnapshot battle,
        TacticalPoseSnapshot from, TacticalPoseSnapshot to) => battle.Rules.Anomalies is { } r
        ? TacticalAnomalyMath.Segments(AnomalyFieldLayer.ToMotion(battle.Anomalies), new(from.X, from.Y, from.Course),
            new(to.X, to.Y, to.Course), r.Background, r.ZoneEdge, r.AbsorbSamples) : [];
}
