using System.Collections.Immutable;
using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Engine.TacticalBattle.Ai;

internal static class TacticalCommander
{
    public static ImmutableArray<TacticalDrone> Threats(TacticalBattle battle, string side)
    {
        var carrier = battle.Carrier(side);
        var tuning = battle.Catalog.Ai.Commander;
        return battle.ActiveDrones(battle.EnemySide(side)).Where(enemy =>
        {
            double distance = Distance(enemy, carrier);
            return distance <= tuning.ThreatRadius
                || Math.Abs(TacticalGeometry.Norm(TacticalGeometry.Bearing(enemy.Pose.X, enemy.Pose.Y, carrier.Pose.X, carrier.Pose.Y)
                    - enemy.Pose.Course)) <= tuning.ThreatHeadingDeg && distance <= tuning.ThreatTurns * enemy.Limits.MaxSpeed;
        }).OrderBy(enemy => Distance(enemy, carrier)).ToImmutableArray();
    }

    public static TacticalOrder Interceptor(TacticalBattle battle, TacticalDrone drone)
    {
        var carrier = battle.Carrier(drone.Side);
        var tuning = battle.Catalog.Ai.Interceptor;
        var close = battle.ActiveDrones(battle.EnemySide(drone.Side)).Where(e => Distance(e, carrier) <= tuning.PatrolRadius)
            .OrderBy(e => Distance(e, carrier)).FirstOrDefault();
        if (close is not null) return TacticalSteering.Pursue(battle, drone, close);
        var threats = Threats(battle, drone.Side);
        var limits = drone.Limits;
        if (!threats.IsEmpty)
        {
            var enemy = threats[0];
            if (Distance(drone, enemy) <= drone.Weapon.Range * tuning.EngageFactor) return TacticalSteering.Pursue(battle, drone, enemy);
            double bearing = TacticalGeometry.Bearing(carrier.Pose.X, carrier.Pose.Y, enemy.Pose.X, enemy.Pose.Y);
            var interceptors = battle.ActiveDrones(drone.Side).Where(d => d.Role == "interceptor").ToList();
            int index = interceptors.IndexOf(drone);
            int sign = index % 2 == 0 ? 1 : -1;
            var screen = TacticalGeometry.Vec(bearing, tuning.ScreenDistance);
            var flank = TacticalGeometry.Vec(bearing + TacticalBattleConstants.ScreenFlankOffset * sign, tuning.FlankOffset);
            double x = carrier.Pose.X + screen.X + flank.X;
            double y = carrier.Pose.Y + screen.Y + flank.Y;
            return TacticalGeometry.Dist(drone.Pose.X, drone.Pose.Y, x, y) <= tuning.ScreenHold
                ? TacticalSteering.Steer(battle, drone, enemy.Pose.X, enemy.Pose.Y, limits.MinSpeed)
                : TacticalSteering.Steer(battle, drone, x, y, limits.MaxSpeed);
        }
        // The turn limit uses raw patrolSpeed before steer clamps the requested speed.
        double step = PyMath.Min(tuning.PatrolOrbitStepDeg, tuning.OrbitTurnFraction * limits.TurnLimit(tuning.PatrolSpeed, battle.Catalog.Rules.TurnAtMaxSpeedFraction));
        var orbit = TacticalGeometry.Vec(TacticalGeometry.Bearing(carrier.Pose.X, carrier.Pose.Y, drone.Pose.X, drone.Pose.Y) + step, tuning.PatrolOrbitRadius);
        return TacticalSteering.Steer(battle, drone, carrier.Pose.X + orbit.X, carrier.Pose.Y + orbit.Y, tuning.PatrolSpeed);
    }

    public static void Assign(TacticalBattle battle, string side)
    {
        var own = battle.ActiveDrones(side).ToArray();
        if (own.Length == 0) return;
        var tuning = battle.Catalog.Ai.Commander;
        int threats = Threats(battle, side).Length;
        int interceptors = threats == 0 ? 0 : (int)PyMath.Min(tuning.MaxInterceptors, PyMath.Min((long)threats + tuning.ExtraInterceptors, own.Length));
        int rest = own.Length - interceptors;
        int enemies = battle.ActiveDrones(battle.EnemySide(side)).Count();
        int strikers = enemies == 0 ? rest : (int)PyMath.Min(rest, PyMath.Max(0, own.Length - enemies));
        var enemyCarrier = battle.Carrier(battle.EnemySide(side));
        if (strikers == 0 && enemies > 0 && rest > 0 && enemyCarrier.Hull < tuning.StrikeWhenEnemyCarrierBelow * enemyCarrier.MaxHull) strikers = 1;
        var free = own.ToList();
        var planned = new List<(TacticalDrone Drone, string Role)>();
        AssignSlots("interceptor", interceptors, battle.Carrier(side));
        AssignSlots("striker", strikers, enemyCarrier);
        planned.AddRange(free.Select(d => (d, "fighter")));
        foreach (var drone in own)
        {
            string role = planned.First(p => ReferenceEquals(p.Drone, drone)).Role;
            if (role == drone.Role || battle.Turn - drone.RoleSince < tuning.MinRoleTurns) continue;
            ChangeRole(battle, drone, role, false);
        }

        void AssignSlots(string role, int count, TacticalCarrier anchor)
        {
            for (int i = 0; i < count; i++)
            {
                var drone = free.OrderBy(d => d.Role != role).ThenBy(d => Distance(d, anchor)).First();
                free.Remove(drone);
                planned.Add((drone, role));
            }
        }
    }

    public static void ChangeRole(TacticalBattle battle, TacticalDrone drone, string role, bool noAttackers)
    {
        if (drone.Role == role) return;
        drone.Role = role;
        drone.RoleSince = battle.Turn;
        battle.RoleChanges++;
        battle.AiEvents.Add(new TacticalRoleChanged(drone.Id, role, noAttackers));
    }
    private static double Distance(TacticalUnit first, TacticalUnit second) => TacticalGeometry.Dist(first.Pose.X, first.Pose.Y, second.Pose.X, second.Pose.Y);
}

internal static class TacticalBattleAi
{
    public static void Plan(TacticalBattle battle)
    {
        battle.AiEvents.Clear();
        for (int i = 0; i < TacticalBattleConstants.SideOrder.Length; i++)
        {
            string side = TacticalBattleConstants.SideOrder[i];
            if (battle.Catalog.Ai.Tactics[battle.Tactics[i]].Adaptive) TacticalCommander.Assign(battle, side);
            var own = battle.ActiveDrones(side).ToArray();
            if (own.Length > 0 && own.All(d => d.Role == "interceptor"))
                foreach (var drone in own) TacticalCommander.ChangeRole(battle, drone, "fighter", true);
        }
        foreach (var drone in battle.Drones)
        {
            if (!drone.IsActive) continue;
            string role = TacticalSteering.EffectiveRole(battle, drone);
            TacticalOrder proposed = role switch
            {
                "striker" => TacticalRoles.Striker(battle, drone),
                "interceptor" => TacticalCommander.Interceptor(battle, drone),
                "fighter" => TacticalRoles.Fighter(battle, drone),
                _ => throw new InvalidOperationException("Unknown battle role: " + role)
            };
            drone.Order = TacticalTail.Apply(battle, drone, role, proposed);
        }
    }
}
