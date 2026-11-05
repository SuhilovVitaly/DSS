using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Engine.TacticalBattle.Ai;

internal abstract record TacticalAiEvent(string DroneId);
internal sealed record TacticalFighterBreak(string DroneId) : TacticalAiEvent(DroneId);
internal sealed record TacticalRoleChanged(string DroneId, string Role, bool NoAttackers) : TacticalAiEvent(DroneId);

internal static class TacticalRoles
{
    public static TacticalOrder Striker(TacticalBattle battle, TacticalDrone drone)
    {
        var target = battle.Carrier(battle.EnemySide(drone.Side));
        var settings = battle.Catalog.Ai.Striker;
        double distance = TacticalGeometry.Dist(drone.Pose.X, drone.Pose.Y, target.Pose.X, target.Pose.Y);
        if (drone.StrikeMode == "in" && distance < settings.BreakDistance) drone.StrikeMode = "out";
        else if (drone.StrikeMode == "out" && distance >= settings.ExtendDistance) drone.StrikeMode = "in";
        if (drone.StrikeMode == "in") return TacticalSteering.Steer(battle, drone, target.Pose.X, target.Pose.Y, settings.AttackSpeed);
        var delta = TacticalGeometry.Vec(TacticalGeometry.Bearing(target.Pose.X, target.Pose.Y, drone.Pose.X, drone.Pose.Y),
            settings.ExtendDistance + settings.BreakDistance);
        return TacticalSteering.Steer(battle, drone, target.Pose.X + delta.X, target.Pose.Y + delta.Y, drone.Limits.MaxSpeed);
    }

    public static TacticalOrder Fighter(TacticalBattle battle, TacticalDrone drone)
    {
        if (drone.Extend > 0)
        {
            drone.Extend--;
            return new(drone.Limits.ClampSpeed(drone.Limits.MaxSpeed, drone.Speed), 0);
        }
        var settings = battle.Catalog.Ai.Fighter;
        if (drone.Dry >= settings.BreakAfterTurns)
        {
            drone.Dry = 0;
            drone.Extend = settings.ExtendTurns - 1;
            battle.AiEvents.Add(new TacticalFighterBreak(drone.Id));
            return new(drone.Limits.ClampSpeed(drone.Limits.MaxSpeed, drone.Speed), 0);
        }
        string enemySide = battle.EnemySide(drone.Side);
        var enemies = battle.ActiveDrones(enemySide).ToArray();
        var target = enemies.FirstOrDefault(e => e.Id == drone.Hunt);
        target ??= PyMath.MinByFirst(enemies, e => TacticalGeometry.Dist(drone.Pose.X, drone.Pose.Y, e.Pose.X, e.Pose.Y));
        drone.Hunt = target.Id;
        return TacticalSteering.Pursue(battle, drone, target);
    }
}
