using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Combat;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

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
        if (!definition.IsPointLauncher)
            throw new ScenarioException($"Module '{module.ModuleId}' uses a retired guided weapon definition.");
        return new WeaponOperatorSnapshot(crew.Id, crew.DisplayName, skillType, crew.AccuracySkill, 0, crew.AccuracySkill);
    }

    private LauncherCombatSnapshot? ProjectWeaponLauncher(SpaceObjectRuntime owner, InstalledModuleRuntime module)
    {
        var launcher = _launcherCombat.GetValueOrDefault((owner.InitialMotion.ObjectId, module.ModuleId));
        if (launcher is null) return null;
        var definition = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
        var op = ResolveWeaponOperator(owner, module, WeaponSkillType.TorpedoAttack);
        return launcher with
        {
            Operator = op,
            EffectiveScatterPercent = op is not null && _registry.CombatRules?.Scatter is { } scatter
                ? BlastMath.ScatterSigma(1, definition.ScatterPercent!.Value, op.Skill,
                    scatter.SkillPivot, scatter.SkillSlope, scatter.MinMultiplier) : null,
            CoreFraction = definition.IsPointLauncher ? _registry.CombatRules?.Blast?.CoreFraction : null,
            ArmorPiercingPercent = definition.ArmorPiercingPercent,
            ReloadDurationMs = definition.ReloadMs
        };
    }

    private int? CaptureOwnDestroyedModuleLimit()
    {
        if (PlayerShipObjectId is null || _registry.CombatRules?.ShipDestruction is not { } rules)
            return null;
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        return ship is null || ship.Modules.IsDefaultOrEmpty ? null :
            (int)decimal.Ceiling(ship.Modules.Length * rules.DestroyedModuleFraction);
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
