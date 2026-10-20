using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Combat;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private CombatChanceRules ChanceRules => _registry.CombatRules?.Chance ?? CombatChanceMath.DefaultRules;

    private WeaponOperatorSnapshot? ResolveWeaponOperator(
        SpaceObjectRuntime owner, InstalledModuleRuntime module, WeaponSkillType skillType)
    {
        if (module.OperatorCrewId is not { } id || owner.Crew.IsDefaultOrEmpty) return null;
        var crew = owner.Crew.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.Ordinal));
        if (crew is null) return null;
        var definition = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
        if (definition.IsPointLauncher)
            return new WeaponOperatorSnapshot(crew.Id, crew.DisplayName, skillType, crew.AccuracySkill, 0, crew.AccuracySkill);
        int skill = skillType == WeaponSkillType.TorpedoAttack
            ? crew.ManeuverabilitySkill : crew.AccuracySkill;
        decimal baseRating = skillType == WeaponSkillType.TorpedoAttack
            ? definition.TorpedoManeuverability ?? throw new ScenarioException($"Module '{module.ModuleId}' lacks torpedoManeuverability.")
            : definition.CountermeasureAccuracy ?? throw new ScenarioException($"Module '{module.ModuleId}' lacks countermeasureAccuracy.");
        // Torpedo hit accuracy is captured separately. This rating is the
        // projectile's interception resistance, including its operator's skill.
        return new WeaponOperatorSnapshot(crew.Id, crew.DisplayName, skillType, skill,
            baseRating, baseRating + skill);
    }

    private LauncherCombatSnapshot? ProjectWeaponLauncher(SpaceObjectRuntime owner, InstalledModuleRuntime module)
    {
        var launcher = _launcherCombat.GetValueOrDefault((owner.InitialMotion.ObjectId, module.ModuleId));
        return launcher is null ? null : launcher with
        {
            Operator = ResolveWeaponOperator(owner, module, WeaponSkillType.TorpedoAttack),
            ReloadDurationMs = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex) is { IsPointLauncher: true } point
                ? point.ReloadMs : (double?)(_registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex).LauncherReloadMs *
                    FireModeMath.Resolve(_registry, launcher.ModeId).ReloadMultiplier)
        };
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
