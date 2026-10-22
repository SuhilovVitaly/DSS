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
        _registry = candidate.Registry;
        _debugTuning = candidate;
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
        RecordCommandResult(command, CommandResultStatus.Executed, time);
        return CommandStartOutcome.Started;
    }

    private DebugTuningSnapshot? CaptureDebugTuning(bool compact)
    {
        var snapshot = _debugTuning.Snapshot;
        if (!compact) return snapshot;
        if (_lastDebugPresentationRevision == snapshot.Revision) return null;
        _lastDebugPresentationRevision = snapshot.Revision;
        return snapshot;
    }
}
