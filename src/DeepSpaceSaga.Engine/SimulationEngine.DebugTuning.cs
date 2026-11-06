using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private CommandStartOutcome TryApplyDebugTuning(PlayerCommand command, long time)
    {
        if (!_hasLoadedScenario || _disposed || IsStoppedByStationSecurity)
            return CommandStartOutcome.Rejected(DebugTuningReasonCodes.SessionNotRunning);
        if (!_debugTuning.TryApply(command.DebugTuning, out var candidate, out var reason))
            return CommandStartOutcome.Rejected(reason!);
        if (!TryStageDebugShips(candidate.Registry, time, out var ships))
            return CommandStartOutcome.Rejected(DebugTuningReasonCodes.ValueOutOfRange);
        _registry = candidate.Registry;
        _debugTuning = candidate;
        _objects.Clear();
        _objects.AddRange(ships);
        RecordCommandResult(command, CommandResultStatus.Executed, time);
        return CommandStartOutcome.Started;
    }

    private DebugTuningSnapshot CaptureDebugTuning() => _debugStartupError is null
        ? _debugTuning.Snapshot
        : _debugTuning.Snapshot with { StartupError = _debugStartupError };
}
