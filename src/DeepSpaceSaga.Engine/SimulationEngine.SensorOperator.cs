using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private SensorOperatorSnapshot? BuildSensorOperatorProjection()
    {
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (ship is null || EvaluateCarrierSensor(ship) is not { } sensor) return null;
        var module = ship.Modules.Single(m => m.ModuleId == sensor.ModuleId);
        double baseRadius = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex).SensorRadiusWorldUnits!.Value;
        string? reason = !sensor.IsOperational ? SensorOperatorReasons.ModuleUnavailable
            : module.OperatorCrewId is null ? SensorOperatorReasons.Unassigned
            : !ship.Crew.Any(c => c.Id == module.OperatorCrewId) ? SensorOperatorReasons.CrewMissing : null;
        return new(module.ModuleId, module.OperatorCrewId, reason, baseRadius,
            sensor.RadiusWorldUnits, sensor.RadiusWorldUnits - baseRadius);
    }
}
