using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private CarrierSensorSnapshot? BuildPlayerSensorProjection()
    {
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        return ship is null ? null : EvaluateCarrierSensor(ship);
    }

    private CarrierSensorSnapshot? EvaluateCarrierSensor(SpaceObjectRuntime ship)
    {
        CarrierSensorSnapshot? best = null;
        foreach (var module in ship.Modules.OrderBy(m => m.ModuleId, StringComparer.Ordinal))
        {
            var definition = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
            if (definition.CategoryTypeId != "module.sensor" || definition.SensorRadiusWorldUnits is not { } radius)
                continue;
            bool operational = !ship.IsDestroyed && !module.Destroyed && module.StructurePoints > 0 &&
                module.PowerState == "On" && module.OperationalState == "Ready";
            double effective = operational ? radius : 0;
            if (operational && ship.InitialMotion.ObjectId == PlayerShipObjectId)
            {
                var crew = ship.Crew.FirstOrDefault(c => c.Id == module.OperatorCrewId);
                effective = radius * (crew is null ? 0.8 : 1 + crew.SensorRangeBonusPercent / 100.0);
                if (!double.IsFinite(effective))
                    throw new Scenario.ScenarioException("Effective sensor radius must be finite.");
            }
            if (best is null || effective > best.RadiusWorldUnits)
                best = new(module.ModuleId, effective, operational);
        }
        return best;
    }

    // Shared by world-group observation and pursuit. No fog flag, RNG or hidden orders.
    internal static bool SensorCovers(double radius, double observerX, double observerY, double targetX, double targetY)
    {
        if (!double.IsFinite(radius) || radius <= 0 ||
            !double.IsFinite(observerX) || !double.IsFinite(observerY) ||
            !double.IsFinite(targetX) || !double.IsFinite(targetY)) return false;
        double dx = Math.Abs(targetX - observerX), dy = Math.Abs(targetY - observerY);
        if (dx > radius || dy > radius) return false;
        // Normalize before squaring so finite large coordinates cannot overflow the radius square.
        return (dx / radius) * (dx / radius) + (dy / radius) * (dy / radius) <= 1;
    }

    internal SensorContactSnapshot ObserveSensorContact(string observerId, string targetId)
    {
        lock (_worldStateLock)
        {
            long time = _processedSimulationTimeMs;
            var observer = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == observerId);
            var target = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == targetId);
            if (observer is null || target is null || target.IsDestroyed)
                return new(observerId, targetId, false, time);
            var sensor = EvaluateCarrierSensor(observer);
            var from = PredictMotion(observer, Math.Max(0, time - observer.StartGameTimeMs));
            var to = PredictMotion(target, Math.Max(0, time - target.StartGameTimeMs));
            return new(observerId, targetId, SensorCovers(sensor?.RadiusWorldUnits ?? 0, from.X, from.Y, to.X, to.Y), time);
        }
    }
}
