using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Engine.TacticalBattle.Ai;

internal static class TacticalDustAvoidance
{
    public static TacticalOrder Apply(TacticalBattle battle, TacticalDrone drone, TacticalOrder proposed)
    {
        if (battle.Anomalies.IsDefaultOrEmpty || !drone.IsActive) return proposed;
        var limits = drone.Limits;
        double speed = limits.ClampSpeed(proposed.Speed, drone.Speed);
        double turnLimit = limits.TurnLimit(speed, battle.Catalog.Rules.TurnAtMaxSpeedFraction);
        double turn = PyMath.Clamp(proposed.Turn, -turnLimit, turnLimit);
        var end = End(speed, turn);
        if (!Inside(end) || TacticalBattleSalvo.Candidates(battle, drone, ignoreDust: true, pose: end).IsEmpty) return proposed;
        int steps = battle.Catalog.Rules.Anomalies.AiDustSearch;
        foreach (double requested in new[] { speed, limits.MinSpeed, limits.MaxSpeed })
        {
            double candidateSpeed = limits.ClampSpeed(requested, drone.Speed);
            double limit = limits.TurnLimit(candidateSpeed, battle.Catalog.Rules.TurnAtMaxSpeedFraction);
            for (int k = 1; k <= steps; k++)
                foreach (int sign in new[] { 1, -1 })
                {
                    double candidateTurn = PyMath.Clamp(turn + sign * k * limit / steps, -limit, limit);
                    if (!Inside(End(candidateSpeed, candidateTurn))) return new(candidateSpeed, candidateTurn);
                }
        }
        return proposed;

        TacticalPose End(double v, double t) => TacticalGeometry.Integrate(drone.Pose, v, t, battle.Catalog.Rules.Substeps);
        bool Inside(TacticalPose pose) => TacticalAnomalyMath.Inside(battle.Anomalies, TacticalAnomalyKind.Dust,
            pose.X, pose.Y, battle.Catalog.Rules.Anomalies.Background, battle.Catalog.Rules.Anomalies.ZoneEdge);
    }
}
