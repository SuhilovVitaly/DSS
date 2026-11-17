using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private void ValidateSensorRanges(IEnumerable<SpaceObjectRuntime> objects)
    {
        foreach (var ship in objects)
        foreach (var module in ship.Modules)
        {
            var definition = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
            if (definition.CategoryTypeId != "module.sensor" || definition.SensorRadiusWorldUnits is not { } radius) continue;
            var crew = ship.Crew.FirstOrDefault(c => c.Id == module.OperatorCrewId);
            if (crew is not null && (crew.SensorRangeBonusPercent < 0 || !double.IsFinite(radius * (1 + crew.SensorRangeBonusPercent / 100.0))))
                throw new Scenario.ScenarioException("Effective sensor radius must be finite with a nonnegative skill.");
        }
    }

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
