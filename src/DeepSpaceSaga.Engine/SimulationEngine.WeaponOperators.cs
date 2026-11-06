using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    internal static void ValidateWeaponAssignments(SpaceObjectData obj)
    {
        var crewIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var crew in obj.Crew ?? [])
        {
            if (crew is null)
                throw new ScenarioException($"Ship '{obj.ObjectId}' has a null crew member.");
            crewIds.Add(crew.CrewId);
            if (crew.AccuracySkill is < 0 or > 30)
                throw new ScenarioException($"Crew '{crew.CrewId}' accuracySkill must be in 0..30.");
            if (crew.ManeuverabilitySkill is < 0 or > 30)
                throw new ScenarioException($"Crew '{crew.CrewId}' maneuverabilitySkill must be in 0..30.");
        }
        var assigned = new HashSet<string>(StringComparer.Ordinal);
        foreach (var module in obj.Modules ?? [])
        {
            if (module?.OperatorCrewId is not { } crewId) continue;
            if (!crewIds.Contains(crewId))
                throw new ScenarioException($"Module '{module.ModuleId}' operator '{crewId}' must belong to ship '{obj.ObjectId}'.");
            if (!assigned.Add(crewId))
                throw new ScenarioException($"Crew '{crewId}' cannot operate two modules on ship '{obj.ObjectId}'.");
        }
    }
}
