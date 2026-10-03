using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private CombatStateData CaptureCombatState(long motionTimeMs) => new(
        1, motionTimeMs, _torpedoSequence, _combatImpactSequence, _wreckSequence, _nextCombatGuidanceMs,
        _launcherCombat.OrderBy(p => p.Key.ObjectId, StringComparer.Ordinal).ThenBy(p => p.Key.ModuleId, StringComparer.Ordinal)
            .Select(p => new LauncherSaveData(p.Key.ObjectId, p.Key.ModuleId, p.Value)).ToArray(),
        _objects.Where(o => o.InitialMotion.Torpedo is not null).OrderBy(o => o.InitialMotion.ObjectId, StringComparer.Ordinal)
            .Select(o =>
            {
                bool tracked = _torpedoTargets.TryGetValue(o.InitialMotion.ObjectId, out var target);
                return new ProjectileSaveData(o.InitialMotion.ObjectId, o.InitialMotion.Torpedo!, !tracked,
                    tracked ? new ObjectMotionSnapshot(target.Pose.ObjectId, target.Pose.X, target.Pose.Y,
                        target.Pose.SpeedKmS, target.Pose.Direction) : null, tracked ? target.Time : 0);
            }).ToArray(),
        _processedLaunchCommandIds.Order(StringComparer.Ordinal).ToArray());

    // Validate content compatibility and stage every runtime mutation before publishing the world.
    private static void StageCombatRestore(CombatStateData? saved, List<SpaceObjectRuntime> objects,
        Dictionary<(string ObjectId, string ModuleId), LauncherCombatSnapshot> launchers)
    {
        if (saved is null) return;
        if (launchers.Count != saved.Launchers.Count || saved.Launchers.Any(l => !launchers.ContainsKey((l.OwnerObjectId, l.ModuleId))))
            throw new ScenarioException("Saved combat launchers do not match installed weapon modules.");
        foreach (var launcher in saved.Launchers)
            launchers[(launcher.OwnerObjectId, launcher.ModuleId)] = launcher.State;
        foreach (var projectile in saved.Projectiles)
        {
            int index = objects.FindIndex(o => o.InitialMotion.ObjectId == projectile.ObjectId);
            var obj = objects[index];
            objects[index] = obj with { InitialMotion = obj.InitialMotion with { Torpedo = projectile.Flight } };
        }
    }

    // Assignment only: the caller holds the world lock and preflight has already succeeded.
    private void RestoreCombatState(CombatStateData? saved)
    {
        _processedLaunchCommandIds.Clear();
        if (saved is null) return;
        _torpedoSequence = saved.ProjectileSequence;
        _combatImpactSequence = saved.ImpactSequence;
        _wreckSequence = saved.WreckSequence;
        _nextCombatGuidanceMs = saved.NextGuidanceMotionTimeMs;
        _processedLaunchCommandIds.UnionWith(saved.ProcessedLaunchCommandIds);
        foreach (var projectile in saved.Projectiles)
            if (projectile.TargetBaseline is { } target)
                _torpedoTargets.Add(projectile.ObjectId, (target, projectile.TargetBaselineMotionTimeMs));
    }
}
