using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Combat;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private long _countermeasureSequence;
    private long _combatProcessedTimeMs;
    // Transitional Save18 envelope only; no gameplay interception draws remain.
    private CountermeasureRng _countermeasureRng = new(0);
    private Dictionary<(string ObjectId, string ModuleId), DefenseSnapshot> _defenses = new();
    private bool HasDefenseActivity => _defenses.Values.Any(d => d.State == DefenseState.Reloading);

    private bool TryGetNextCountermeasureId(out string id, out long sequence)
    {
        sequence = _countermeasureSequence;
        id = string.Empty;
        while (sequence < long.MaxValue)
        {
            string candidate = "countermeasure-" + (++sequence).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (_objects.Any(o => string.Equals(o.InitialMotion.ObjectId, candidate, StringComparison.OrdinalIgnoreCase))) continue;
            id = candidate;
            return true;
        }
        return false;
    }

    private void CompleteDefenseReloads(double time)
    {
        foreach (var key in _defenses.Keys.ToArray())
        {
            var defense = _defenses[key];
            if (defense.State == DefenseState.Reloading && defense.ReloadDueMotionTimeMs <= time)
                _defenses[key] = defense with
                {
                    State = defense.Operator is null ? DefenseState.NoOperator : DefenseState.Ready,
                    ReloadDueMotionTimeMs = null
                };
        }
    }

    private Dictionary<(string ObjectId, string ModuleId), DefenseSnapshot> BuildDefenseState(IEnumerable<SpaceObjectRuntime> objects)
    {
        var result = new Dictionary<(string ObjectId, string ModuleId), DefenseSnapshot>();
        foreach (var owner in objects)
            foreach (var module in owner.Modules)
            {
                var type = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
                if (type.IsPointLauncher && type.CategoryTypeId == "module.countermeasure.launcher")
                {
                    if (owner.ObjectType is not (SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip))
                        throw new ScenarioException($"Defense module '{module.ModuleId}' must be installed on a ship.");
                    var op = ResolveWeaponOperator(owner, module, WeaponSkillType.CountermeasureDefense);
                    result.Add((owner.InitialMotion.ObjectId, module.ModuleId), new(false, op,
                        op is null ? DefenseState.NoOperator : DefenseState.Ready, RangeKm: type.RangeKm!.Value,
                        PendingBurstShots: [], ActiveProjectileIds: [], ReloadDurationMs: type.ReloadMs,
                        Profiles: type.Profiles, ScatterPercent: type.ScatterPercent));
                    continue;
                }
                if (type.CountermeasureAccuracy is null) continue;
                if (owner.ObjectType is not (SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip))
                    throw new ScenarioException($"Defense module '{module.ModuleId}' must be installed on a ship.");
                var weaponOperator = ResolveWeaponOperator(owner, module, WeaponSkillType.CountermeasureDefense);
                result.Add((owner.InitialMotion.ObjectId, module.ModuleId), new DefenseSnapshot(
                    false, weaponOperator, weaponOperator is null ? DefenseState.NoOperator : DefenseState.Ready,
                    RangeKm: type.CountermeasureRangeKm!.Value,
                    ManualRangeKm: type.CountermeasureManualRangeKm,
                    Accuracy: type.CountermeasureAccuracy,
                    SpeedKmS: type.CountermeasureSpeedKmS,
                    TurnRateDegPerSec: type.CountermeasureTurnRateDegPerSec,
                    MaxFlightTimeMs: type.CountermeasureMaxFlightTimeMs,
                    Barrels: type.Barrels ?? 1));
            }
        return result;
    }

    private DefenseSnapshot? ProjectDefense(SpaceObjectRuntime owner) => owner.Modules
        .Select(m => ProjectModuleDefense(owner, m))
        .FirstOrDefault(d => d is not null);

    private DefenseSnapshot? ProjectModuleDefense(SpaceObjectRuntime owner, InstalledModuleRuntime module)
    {
        if (_defenses.GetValueOrDefault((owner.InitialMotion.ObjectId, module.ModuleId)) is not { } defense) return null;
        var definition = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
        var op = definition.IsPointLauncher ? ResolveWeaponOperator(owner, module, WeaponSkillType.CountermeasureDefense) : defense.Operator;
        bool own = owner.InitialMotion.ObjectId == PlayerShipObjectId;
        return defense with
        {
            Operator = op,
            EffectiveScatterPercent = own && definition.IsPointLauncher && op is not null && _registry.CombatRules?.Scatter is { } scatter
                ? BlastMath.ScatterSigma(1, definition.ScatterPercent!.Value, op.Skill,
                    scatter.SkillPivot, scatter.SkillSlope, scatter.MinMultiplier) : null,
            ReloadDurationMs = definition.IsPointLauncher ? definition.ReloadMs : definition.CountermeasureReloadMs
        };
    }
}
