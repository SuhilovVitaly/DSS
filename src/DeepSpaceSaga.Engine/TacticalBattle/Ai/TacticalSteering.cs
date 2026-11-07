using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Engine.TacticalBattle.Ai;

internal static class TacticalSteering
{
    public static TacticalOrder Steer(TacticalBattle battle, TacticalDrone drone, double x, double y, double requestedSpeed)
    {
        var midpoint = battle.Midpoint;
        if (TacticalGeometry.Dist(drone.Pose.X, drone.Pose.Y, midpoint.X, midpoint.Y) > battle.Catalog.Ai.Leash.Radius)
        {
            x = midpoint.X;
            y = midpoint.Y;
        }
        double error = TacticalGeometry.Norm(TacticalGeometry.Bearing(drone.Pose.X, drone.Pose.Y, x, y) - drone.Pose.Course);
        var limits = drone.Limits;
        double speed = limits.ClampSpeed(requestedSpeed, drone.Speed);
        double turn = limits.TurnLimit(speed, battle.Catalog.Rules.TurnAtMaxSpeedFraction);
        var pursuit = battle.Catalog.Ai.Pursuit;
        if (pursuit.SlowForTurn && turn < Math.Abs(error))
        {
            double lower = limits.ClampSpeed(drone.Speed - limits.Change, drone.Speed);
            for (int k = 1; k <= pursuit.SlowForTurnSteps; k++)
            {
                speed = speed + (lower - speed) * k / (double)pursuit.SlowForTurnSteps;
                turn = limits.TurnLimit(speed, battle.Catalog.Rules.TurnAtMaxSpeedFraction);
                if (turn >= Math.Abs(error)) break;
            }
        }
        return new(speed, PyMath.Clamp(error, -turn, turn));
    }

    public static TacticalPose Predict(TacticalDrone enemy)
    {
        var delta = TacticalGeometry.Vec(enemy.Pose.Course, enemy.Speed);
        return new(enemy.Pose.X + delta.X, enemy.Pose.Y + delta.Y, enemy.Pose.Course);
    }

    public static TacticalOrder Pursue(TacticalBattle battle, TacticalDrone drone, TacticalDrone enemy)
    {
        var predicted = Predict(enemy);
        var pursuit = battle.Catalog.Ai.Pursuit;
        var tail = TacticalGeometry.Vec(predicted.Course, -pursuit.TailOffset);
        double radius = (1.0 - TacticalBattleConstants.AimErrorMultiplier * (drone.Accuracy - pursuit.AimAccuracyRef)
            / (double)TacticalBattleConstants.Percent) * pursuit.AimErrorScale;
        double x = predicted.X + tail.X + battle.Random.Uniform(-radius, radius);
        double y = predicted.Y + tail.Y + battle.Random.Uniform(-radius, radius);
        double speed = pursuit.SpeedFactor * TacticalGeometry.Dist(drone.Pose.X, drone.Pose.Y, x, y);
        return Steer(battle, drone, x, y, speed);
    }

    public static string EffectiveRole(TacticalBattle battle, TacticalDrone drone) =>
        drone.Role is "fighter" or "interceptor" && !battle.ActiveDrones(battle.EnemySide(drone.Side)).Any()
            ? "striker" : drone.Role;
}
