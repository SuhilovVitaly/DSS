using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private Dictionary<string, HullCombatSnapshot> _hullCombat = new(StringComparer.Ordinal);
    private Dictionary<(string ObjectId, string ModuleId), LauncherCombatSnapshot> _launcherCombat = new();

    // Stage all combat state before publishing the candidate world. Failed loads leave
    // the current session intact; successful loads replace both maps, including empty ones.
    private (Dictionary<string, HullCombatSnapshot> Hulls,
        Dictionary<(string ObjectId, string ModuleId), LauncherCombatSnapshot> Launchers) BuildCombatState(
        IReadOnlyList<SpaceObjectData> objects, IReadOnlyList<SpaceObjectRuntime> runtimeObjects)
    {
        var hulls = new Dictionary<string, HullCombatSnapshot>(StringComparer.Ordinal);
        var launchers = new Dictionary<(string ObjectId, string ModuleId), LauncherCombatSnapshot>();
        foreach (var obj in objects)
        {
            if (obj.ShipClassId is not { } classId)
                continue;
            if (!_registry.ShipClasses.Contains(classId))
                throw new ScenarioException($"Object '{obj.ObjectId}' references unknown shipClassId '{classId}'.");
            var definition = _registry.ShipClasses.GetDefinition(_registry.ShipClasses.GetIndex(classId));
            int currentHp = obj.HullHitPoints ?? definition.HullHitPointsMax;
            if (currentHp <= 0 || currentHp > definition.HullHitPointsMax)
                throw new ScenarioException($"Object '{obj.ObjectId}' hullHitPoints must be in 1..{definition.HullHitPointsMax}.");
            hulls.Add(obj.ObjectId, new HullCombatSnapshot(classId, currentHp, definition.HullHitPointsMax));
        }

        foreach (var obj in runtimeObjects)
        {
            foreach (var module in obj.Modules)
            {
                var definition = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
                if (definition.TorpedoDamage is not { } damage)
                    continue;
                if (obj.ObjectType is not (SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip))
                    throw new ScenarioException($"Launcher '{module.ModuleId}' must be installed on a ship.");
                launchers.Add((obj.InitialMotion.ObjectId, module.ModuleId), new LauncherCombatSnapshot(
                    ActiveTorpedoObjectId: null,
                    SpeedKmS: definition.TorpedoSpeedKmS!.Value,
                    TurnRateDegPerSec: definition.TorpedoTurnRateDegPerSec!.Value,
                    Damage: damage));
            }
        }

        return (hulls, launchers);
    }
}
