using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private CommandStartOutcome TryApplyDebugTuning(PlayerCommand command, long time)
    {
        if (!_hasLoadedScenario || _disposed || IsDefeated)
            return CommandStartOutcome.Rejected(DebugTuningReasonCodes.SessionNotRunning);
        if (!_debugTuning.TryApply(command.DebugTuning, out var candidate, out var reason))
            return CommandStartOutcome.Rejected(reason!);
        if (!TryStageDebugShips(candidate.Registry, time, out var ships))
            return CommandStartOutcome.Rejected(DebugTuningReasonCodes.ValueOutOfRange);
        if (!TryStageDebugProjectiles(candidate.Registry, time, ships.Objects, out var originals, out var launcherTypes))
            return CommandStartOutcome.Rejected(DebugTuningReasonCodes.ValueOutOfRange);
        if (!TryStageDebugReloads(candidate.Registry, time, out var reloads))
            return CommandStartOutcome.Rejected(DebugTuningReasonCodes.ValueOutOfRange);
        _registry = candidate.Registry;
        _debugTuning = candidate;
        _objects.Clear();
        _objects.AddRange(ships.Objects);
        _evasions.Clear();
        foreach (var pair in ships.Evasions) _evasions.Add(pair.Key, pair.Value);
        _hostileCombat.Clear();
        foreach (var pair in ships.Hostiles) _hostileCombat.Add(pair.Key, pair.Value);
        _debugOriginalFlights.Clear();
        foreach (var pair in originals) _debugOriginalFlights.Add(pair.Key, pair.Value);
        _debugProjectileTypes.Clear();
        foreach (var pair in launcherTypes) _debugProjectileTypes.Add(pair.Key, pair.Value);
        _launcherCombat = reloads.Launchers;
        _defenses = reloads.Defenses;
        foreach (var owner in _objects)
            foreach (var module in owner.Modules)
            {
                var type = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
                if (!type.IsPointLauncher) continue;
                var key = (owner.InitialMotion.ObjectId, module.ModuleId);
                if (_launcherCombat.TryGetValue(key, out var launcher))
                    _launcherCombat[key] = launcher with { Profiles = type.Profiles, ReloadDurationMs = type.ReloadMs };
                if (_defenses.TryGetValue(key, out var defense))
                    _defenses[key] = defense with { Profiles = type.Profiles, ReloadDurationMs = type.ReloadMs };
            }
        while (NextPointDetonationTime() is var due && due <= time) ResolvePointDetonations(due);
        foreach (string moduleId in reloads.CompletedPlayerModules) CompletePlayerTorpedoReloads(time, moduleId);
        RecordCommandResult(command, CommandResultStatus.Executed, time);
        return CommandStartOutcome.Started;
    }

    private DebugTuningSnapshot? CaptureDebugTuning(bool compact)
    {
        var snapshot = _debugStartupError is null ? _debugTuning.Snapshot : _debugTuning.Snapshot with { StartupError = _debugStartupError };
        if (!compact) return snapshot;
        if (_lastDebugPresentationRevision == snapshot.Revision) return null;
        _lastDebugPresentationRevision = snapshot.Revision;
        return snapshot;
    }
}
