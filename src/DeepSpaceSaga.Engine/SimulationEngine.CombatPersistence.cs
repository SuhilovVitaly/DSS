using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Combat;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    // Retired serialization envelopes are locked empty/zero; no retired RNG object exists.
    private CountermeasureStateData CaptureDefenseState() => new(3, _countermeasureSequence, 1, 0, 0, _combatJournalSequence,
        _defenses.OrderBy(p => p.Key.ObjectId, StringComparer.Ordinal).ThenBy(p => p.Key.ModuleId, StringComparer.Ordinal)
            .Select(p => new DefenseLauncherSaveData(p.Key.ObjectId, p.Key.ModuleId, p.Value)).ToArray(), [], _combatJournal.ToArray());

    private HostileCombatSaveData CaptureHostileState(long time) => new(HostileCombatSaveData.CurrentVersion,
        _nextHostileDecisionMs, _hostileCombat.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p =>
        {
            var active = _objects.First(o => o.InitialMotion.ObjectId == p.Key).InitialMotion.HostileCombat;
            return new HostileCombatActorSaveData(p.Key, active is null ? p.Value : active with
            { Segment = active.Segment with { ElapsedMs = time - active.Segment.EpochMs } }, active is not null);
        }).ToArray());

    private void StageHostileRestore(HostileCombatSaveData? saved, List<SpaceObjectRuntime> objects,
        long restoredMotionTimeMs)
    {
        if (saved is null) return;
        foreach (var row in saved.Actors)
        {
            int index = objects.FindIndex(o => o.InitialMotion.ObjectId == row.ObjectId);
            var owner = objects[index];
            var module = owner.Modules.Single(m => m.ModuleId == row.State.EngineModuleId);
            var definition = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
            if (!IsEngineCategory(definition) || definition.MaxSpeedMps is not > 0 || definition.LinearInertiaMps2 is not > 0 ||
                definition.AngularInertiaDegPerSec is not > 0 || definition.TurnStepDegrees is not > 0 ||
                row.ActiveMotion && !CanExecuteEngineCommand(module, definition, restoredMotionTimeMs))
                throw new ScenarioException("Invalid hostile combat save: assigned engine is unavailable or not an engine.");
            objects[index] = owner with { InitialMotion = owner.InitialMotion with { HostileCombat = row.ActiveMotion ? row.State : null } };
        }
    }

    private void StageDefenseRestore(CountermeasureStateData? saved, List<SpaceObjectRuntime> objects,
        Dictionary<(string ObjectId, string ModuleId), DefenseSnapshot> defenses)
    {
        if (saved is null) return;
        if (defenses.Count != saved.Launchers.Count || saved.Launchers.Any(l => !defenses.ContainsKey((l.OwnerObjectId, l.ModuleId))))
            throw new ScenarioException("Saved defense launchers do not match installed modules.");
        foreach (var row in saved.Launchers)
        {
            var current = defenses[(row.OwnerObjectId, row.ModuleId)];
            if (row.State.Profiles is not { IsDefaultOrEmpty: false })
                throw new ScenarioException("Invalid defense save: retired guided state.");
            defenses[(row.OwnerObjectId, row.ModuleId)] = current with
            {
                State = row.State.State,
                ActiveProjectileId = row.State.ActiveProjectileId,
                ActiveProjectileIds = row.State.ActiveProjectileIds,
                ReadyAtMotionTimeMs = row.State.ReadyAtMotionTimeMs,
                ReloadDueMotionTimeMs = row.State.ReloadDueMotionTimeMs
            };
        }
    }

    private void RestoreDefenseState(CountermeasureStateData? saved)
    {
        if (saved is null) return;
        _countermeasureSequence = saved.ProjectileSequence;
        _combatJournalSequence = saved.JournalSequence;
        _combatJournal.AddRange(saved.Journal);
    }

    private CombatStateData CaptureCombatState(long motionTimeMs) => new(
        2, motionTimeMs, _torpedoSequence, _combatImpactSequence, _wreckSequence, long.MaxValue,
        _launcherCombat.OrderBy(p => p.Key.ObjectId, StringComparer.Ordinal).ThenBy(p => p.Key.ModuleId, StringComparer.Ordinal)
            .Select(p => new LauncherSaveData(p.Key.ObjectId, p.Key.ModuleId, p.Value)).ToArray(), [],
        _processedLaunchCommandIds.Order(StringComparer.Ordinal).ToArray(),
        _moduleHitLocationRng.State, _moduleHitLocationRng.Counter, 0, 0, 0, 0, []);

    // Validate content compatibility before publishing any runtime mutation.
    private void StageCombatRestore(CombatStateData? saved, List<SpaceObjectRuntime> objects,
        Dictionary<(string ObjectId, string ModuleId), LauncherCombatSnapshot> launchers)
    {
        if (saved is null) return;
        if (launchers.Count != saved.Launchers.Count || saved.Launchers.Any(l => !launchers.ContainsKey((l.OwnerObjectId, l.ModuleId))))
            throw new ScenarioException("Saved combat launchers do not match installed weapon modules.");
        foreach (var row in saved.Launchers)
        {
            var current = launchers[(row.OwnerObjectId, row.ModuleId)];
            if (row.State.Profiles is not { IsDefaultOrEmpty: false })
                throw new ScenarioException("Invalid combat save: retired guided state.");
            launchers[(row.OwnerObjectId, row.ModuleId)] = current with
            { ActiveTorpedoIds = row.State.ActiveTorpedoIds, ReadyAtMotionTimeMs = row.State.ReadyAtMotionTimeMs };
        }
    }

    private void RestoreCombatState(CombatStateData? saved)
    {
        _processedLaunchCommandIds.Clear();
        if (saved is null) return;
        _torpedoSequence = saved.ProjectileSequence;
        _combatImpactSequence = saved.ImpactSequence;
        _wreckSequence = saved.WreckSequence;
        if (saved.HitLocationRngState is { } state) _moduleHitLocationRng = new(state, saved.HitLocationRngCounter);
        _processedLaunchCommandIds.UnionWith(saved.ProcessedLaunchCommandIds);
    }
}
