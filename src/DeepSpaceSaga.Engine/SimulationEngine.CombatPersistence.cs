using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private CountermeasureStateData CaptureDefenseState() => new(1, _countermeasureSequence,
        Combat.CountermeasureRng.AlgorithmVersion, _countermeasureRng.State, _countermeasureRng.Counter, _combatJournalSequence,
        _defenses.OrderBy(p => p.Key.ObjectId, StringComparer.Ordinal).ThenBy(p => p.Key.ModuleId, StringComparer.Ordinal)
            .Select(p => new DefenseLauncherSaveData(p.Key.ObjectId, p.Key.ModuleId, p.Value)).ToArray(),
        _objects.Where(o => o.InitialMotion.Countermeasure is not null).OrderBy(o => o.InitialMotion.ObjectId, StringComparer.Ordinal)
            .Select(o => new CountermeasureProjectileSaveData(o.InitialMotion.ObjectId, o.InitialMotion.Countermeasure!,
                _countermeasureTargetPlans.GetValueOrDefault(o.InitialMotion.ObjectId))).ToArray(),
        _objects.Where(o => o.InitialMotion.Torpedo is not null && o.InitialMotion.CountermeasureAttempted)
            .Select(o => o.InitialMotion.ObjectId).Order(StringComparer.Ordinal).ToArray(), _combatJournal.ToArray(),
        _objects.Any(o => o.InitialMotion.ObjectId == SelectedObjectId && o.InitialMotion.Countermeasure is not null) ? SelectedObjectId : null);

    private static void StageDefenseRestore(CountermeasureStateData? saved, List<SpaceObjectRuntime> objects,
        Dictionary<(string ObjectId, string ModuleId), DefenseSnapshot> defenses)
    {
        if (saved is null) return;
        if (defenses.Count != saved.Launchers.Count || saved.Launchers.Any(l => !defenses.ContainsKey((l.OwnerObjectId, l.ModuleId))))
            throw new ScenarioException("Saved defense launchers do not match installed modules.");
        foreach (var row in saved.Launchers) defenses[(row.OwnerObjectId, row.ModuleId)] = row.State;
        foreach (var row in saved.Projectiles)
        {
            int i = objects.FindIndex(o => o.InitialMotion.ObjectId == row.ObjectId);
            objects[i] = objects[i] with { InitialMotion = objects[i].InitialMotion with { Countermeasure = row.Flight } };
        }
        foreach (var id in saved.AttemptedTorpedoIds)
        {
            int i = objects.FindIndex(o => o.InitialMotion.ObjectId == id);
            objects[i] = objects[i] with { InitialMotion = objects[i].InitialMotion with { CountermeasureAttempted = true } };
        }
    }

    private void RestoreDefenseState(CountermeasureStateData? saved)
    {
        if (saved is null) return;
        _countermeasureSequence = saved.ProjectileSequence;
        _countermeasureRng = new(saved.RngState, saved.RngCounter);
        _combatJournalSequence = saved.JournalSequence;
        _combatJournal.AddRange(saved.Journal);
        SelectedObjectId = saved.SelectedProjectileId;
        foreach (var row in saved.Projectiles) _countermeasureTargetPlans.Add(row.ObjectId, row.TargetPlanStartMotionTimeMs);
    }

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
