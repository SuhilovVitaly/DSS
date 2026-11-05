using System.Collections.Immutable;
using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Engine.TacticalBattle;

internal sealed record TacticalMovement(string DroneId, TacticalOrder Order, ImmutableArray<TacticalPose> Path);

internal static class TacticalBattleMovement
{
    public static ImmutableArray<TacticalMovement> Move(TacticalBattle battle)
    {
        var movements = ImmutableArray.CreateBuilder<TacticalMovement>();
        foreach (var drone in battle.Drones)
        {
            if (!drone.IsActive) continue;
            if (!double.IsFinite(drone.Order.Speed) || !double.IsFinite(drone.Order.Turn))
                throw new ArgumentOutOfRangeException(nameof(battle), "Movement orders must be finite.");
            var limits = drone.Limits;
            double speed = limits.ClampSpeed(drone.Order.Speed, drone.Speed);
            double maximumTurn = limits.TurnLimit(speed, battle.Catalog.Rules.TurnAtMaxSpeedFraction);
            var order = new TacticalOrder(speed, PyMath.Clamp(drone.Order.Turn, -maximumTurn, maximumTurn));
            movements.Add(new(drone.Id, order, TacticalGeometry.Path(drone.Pose, order.Speed, order.Turn, battle.Catalog.Rules.Substeps)));
        }
        // Commit only after every active order/path has passed validation.
        foreach (var movement in movements)
        {
            var drone = battle.Drones.First(d => d.Id == movement.DroneId);
            drone.Pose = movement.Path[^1];
            drone.Speed = movement.Order.Speed;
            drone.PreviousTurn = Math.Abs(movement.Order.Turn);
            drone.Order = movement.Order;
        }
        return movements.ToImmutable();
    }
}
