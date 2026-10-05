using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private WeaponOperatorSnapshot? ResolveWeaponOperator(
        SpaceObjectRuntime owner, InstalledModuleRuntime module, WeaponSkillType skillType)
    {
        if (module.OperatorCrewId is not { } id || owner.Crew.IsDefaultOrEmpty) return null;
        var crew = owner.Crew.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.Ordinal));
        if (crew is null) return null;
        var definition = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
        int skill = (skillType == WeaponSkillType.TorpedoAttack ? crew.TorpedoSkill : crew.CountermeasureSkill) ?? 0;
        decimal baseRating = skillType == WeaponSkillType.TorpedoAttack
            ? definition.TorpedoBaseRating ?? 30m : definition.CountermeasureBaseRating ?? 30m;
        // Divide the bounded integer first to avoid overflowing base*skill unnecessarily.
        decimal effectiveRating;
        try { effectiveRating = baseRating * (skill / 50m); }
        catch (OverflowException ex)
        {
            throw new ScenarioException($"Module '{module.ModuleId}' effective weapon rating exceeds decimal range.", ex);
        }
        return new WeaponOperatorSnapshot(crew.Id, crew.DisplayName, skillType, skill, baseRating, effectiveRating);
    }

    private LauncherCombatSnapshot? ProjectWeaponLauncher(SpaceObjectRuntime owner, InstalledModuleRuntime module)
    {
        var launcher = _launcherCombat.GetValueOrDefault((owner.InitialMotion.ObjectId, module.ModuleId));
        return launcher is null ? null : launcher with { Operator = ResolveWeaponOperator(owner, module, WeaponSkillType.TorpedoAttack) };
    }

    private static void StageLegacyWeaponRatings(List<SpaceObjectRuntime> objects)
    {
        for (int i = 0; i < objects.Count; i++)
        {
            var obj = objects[i];
            if (obj.InitialMotion.Torpedo is not { } flight) continue;
            if (flight.TorpedoRating is < 0)
                throw new ScenarioException("Captured torpedo rating must be nonnegative.");
            if (flight.TorpedoRating is not null) continue;
            objects[i] = obj with
            {
                InitialMotion = obj.InitialMotion with
                {
                    Torpedo = flight with { TorpedoRating = 30m, RatingBreakdown = null, RatingMigratedFromLegacySave = true }
                }
            };
        }
    }

    internal static void ValidateWeaponAssignments(SpaceObjectData obj)
    {
        var crewIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var crew in obj.Crew ?? [])
        {
            if (crew is null)
                throw new ScenarioException($"Ship '{obj.ObjectId}' has a null crew member.");
            crewIds.Add(crew.CrewId);
            if (crew.TorpedoSkill is < 0 or > 100 || crew.CountermeasureSkill is < 0 or > 100)
                throw new ScenarioException($"Crew '{crew.CrewId}' skills must be in 0..100.");
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
