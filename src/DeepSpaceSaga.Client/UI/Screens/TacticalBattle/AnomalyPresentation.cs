using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

/// <summary>Presentation geometry only. Chances and fire prohibitions come from Engine snapshots.</summary>
internal static class AnomalyPresentation
{
    internal static TacticalAnomalyKindCode? HoverKind(TacticalBattleFrame frame)
    {
        if (frame.HoverUnitId is not null || frame.HoverPoint is not { } pointer) return null;
        var w = frame.View.Unproject(pointer.X, pointer.Y); var point = new TacticalPoseSnapshot(w.X, w.Y, 0);
        return Inside(frame.Battle, TacticalAnomalyKind.Heat, point) ? TacticalAnomalyKindCode.Heat
            : Inside(frame.Battle, TacticalAnomalyKind.Dust, point) ? TacticalAnomalyKindCode.Dust : null;
    }
    internal static SKPoint AbsorptionPoint(TacticalBattleFrame frame, TacticalShotSnapshot shot)
    {
        var segments = Segments(frame.Battle, shot.From, shot.To);
        double t = segments.IsEmpty ? .5 : (segments[0].T0 + segments[0].T1) / 2.0;
        return frame.View.Project(shot.From.X + (shot.To.X - shot.From.X) * t, shot.From.Y + (shot.To.Y - shot.From.Y) * t);
    }
    internal static bool Inside(TacticalBattleSnapshot battle, TacticalAnomalyKind kind, TacticalPoseSnapshot point) =>
        battle.Rules.Anomalies is { } r && TacticalAnomalyMath.Inside(AnomalyFieldLayer.ToMotion(battle.Anomalies), kind,
            point.X, point.Y, r.Background, r.ZoneEdge);
    internal static ImmutableArray<TacticalAnomalySegment> Segments(TacticalBattleSnapshot battle,
        TacticalPoseSnapshot from, TacticalPoseSnapshot to) => battle.Rules.Anomalies is { } r
        ? TacticalAnomalyMath.Segments(AnomalyFieldLayer.ToMotion(battle.Anomalies), new(from.X, from.Y, from.Course),
            new(to.X, to.Y, to.Course), r.Background, r.ZoneEdge, r.AbsorbSamples) : [];
}
