using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Engine.TacticalBattle.Ai;

internal static class TacticalTail
{
    public static TacticalOrder Apply(TacticalBattle battle, TacticalDrone drone, string effectiveRole, TacticalOrder proposed) =>
        effectiveRole != "striker" || battle.Catalog.Ai.Striker.TakesTail ? Find(battle, drone) ?? proposed : proposed;

    public static TacticalOrder? Find(TacticalBattle battle, TacticalDrone drone)
    {
        if (!drone.IsActive || drone.Jammed) return null;
        var settings = battle.Catalog.Ai.Tail;
        var limits = drone.Limits;
        double reach = drone.Weapon.Range * settings.RangeFactor;
        double cone = drone.Weapon.ArcDeg * settings.ConeFactor;
        var speeds = PyMath.OrderedUnique(Enumerable.Range(-settings.SpeedSteps, 2 * settings.SpeedSteps + 1)
            .Select(k => limits.ClampSpeed(drone.Speed + limits.Change * k / (double)settings.SpeedSteps, drone.Speed))).OrderBy(v => v).ToArray();
        TacticalOrder? best = null;
        double bestScore = double.PositiveInfinity;
        foreach (var enemy in battle.ActiveDrones(battle.EnemySide(drone.Side)))
        {
            if (TacticalGeometry.Dist(drone.Pose.X, drone.Pose.Y, enemy.Pose.X, enemy.Pose.Y) > (limits.MaxSpeed + reach) + enemy.Speed) continue;
            var predicted = TacticalSteering.Predict(enemy);
            foreach (double speed in speeds)
            {
                double turnLimit = limits.TurnLimit(speed, battle.Catalog.Rules.TurnAtMaxSpeedFraction);
                for (int k = -settings.TurnSteps; k <= settings.TurnSteps; k++)
                {
                    double turn = turnLimit * k / (double)settings.TurnSteps;
                    var end = TacticalGeometry.Integrate(drone.Pose, speed, turn, battle.Catalog.Rules.Substeps);
                    double distance = TacticalGeometry.Dist(end.X, end.Y, predicted.X, predicted.Y);
                    if (distance > reach) continue;
                    double rear = Math.Abs(TacticalGeometry.Norm(TacticalGeometry.Bearing(predicted.X, predicted.Y, end.X, end.Y)
                        - (predicted.Course + TacticalBattleConstants.RearHeadingOffset)));
                    if (rear > battle.Catalog.Rules.Rear.ArcHalfDeg) continue;
                    double angle = Math.Abs(TacticalGeometry.Norm(TacticalGeometry.Bearing(end.X, end.Y, predicted.X, predicted.Y) - end.Course));
                    if (angle > cone) continue;
                    double score = distance / (double)reach;
                    if (score < bestScore) { bestScore = score; best = new(speed, turn); }
                }
            }
        }
        return best is not null && battle.Random.Random() < settings.Chance ? best : null;
    }
}
