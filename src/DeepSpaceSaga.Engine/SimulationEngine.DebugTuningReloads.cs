using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private sealed record DebugReloadRetiming(
        Dictionary<(string ObjectId, string ModuleId), LauncherCombatSnapshot> Launchers,
        Dictionary<(string ObjectId, string ModuleId), DefenseSnapshot> Defenses,
        List<string> CompletedPlayerModules);

    private bool TryStageDebugReloads(GameDataRegistry candidate, long time, out DebugReloadRetiming staged)
    {
        staged = new(new(_launcherCombat), new(_defenses), []);
        foreach (var owner in _objects)
            foreach (var module in owner.Modules.OrderBy(m => m.ModuleId, StringComparer.Ordinal))
            {
                var old = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
                var next = candidate.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
                if (old.ReloadMs is not { } previous || next.ReloadMs is not { } duration || previous == duration) continue;
                var key = (owner.InitialMotion.ObjectId, module.ModuleId);
                if (staged.Launchers.TryGetValue(key, out var launcher) && launcher.ReadyAtMotionTimeMs is { } ready && ready > time)
                {
                    double replacement = Math.Max(time, ready - previous + duration);
                    if (!double.IsFinite(replacement) || replacement >= long.MaxValue) return false;
                    staged.Launchers[key] = launcher with { ReadyAtMotionTimeMs = replacement };
                    if (replacement == time && key.ObjectId == PlayerShipObjectId) staged.CompletedPlayerModules.Add(module.ModuleId);
                }
                if (staged.Defenses.TryGetValue(key, out var defense) &&
                    (defense.ReadyAtMotionTimeMs ?? defense.ReloadDueMotionTimeMs) is { } defenseReady && defenseReady > time)
                {
                    double replacement = Math.Max(time, defenseReady - previous + duration);
                    if (!double.IsFinite(replacement) || replacement >= long.MaxValue) return false;
                    staged.Defenses[key] = defense with
                    {
                        ReadyAtMotionTimeMs = replacement,
                        ReloadDueMotionTimeMs = replacement > time ? replacement : null,
                        State = replacement > time ? DefenseState.Reloading : defense.Operator is null ? DefenseState.NoOperator : DefenseState.Ready
                    };
                }
            }
        return true;
    }
}
