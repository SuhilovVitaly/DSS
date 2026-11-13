using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed record TacticalFireBlocker(TacticalUnitSnapshot Target, double RangeGap, double AngleGap)
{
    internal static TacticalFireBlocker? Nearest(TacticalBattleSnapshot battle, TacticalUnitSnapshot shooter)
    {
        if (shooter.Build is not { Range: > 0, ArcDegrees: > 0 } build) return null;
        var from = shooter.Pose;
        return battle.Units.Where(unit => unit.Side != shooter.Side &&
                unit.State is TacticalUnitState.Air or TacticalUnitState.Alive)
            .Select(unit =>
            {
                var to = unit.Pose;
                double distance = TacticalGeometry.Dist(from.X, from.Y, to.X, to.Y);
                double bearing = TacticalGeometry.Bearing(from.X, from.Y, to.X, to.Y);
                double angle = Math.Abs(TacticalGeometry.Norm(bearing - from.Course));
                return (Hint: new TacticalFireBlocker(unit, Math.Max(0, distance - build.Range),
                    Math.Max(0, angle - build.ArcDegrees)), Distance: distance);
            })
            .OrderBy(pair => TacticalGeometry.Hypot(pair.Hint.RangeGap / build.Range, pair.Hint.AngleGap / build.ArcDegrees))
            .ThenBy(pair => pair.Distance)
            .ThenBy(pair => pair.Hint.Target.Id, StringComparer.Ordinal)
            .Select(pair => pair.Hint).FirstOrDefault();
    }
}
