using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private const string StationSecurityStoppedReason = "player_destroyed";
    private SimulationClockState? _stationSecurityStop;
    private bool IsStoppedByStationSecurity => _stationSecurityStop.HasValue;
    private long _securityFromCalendar, _securityFromMotion, _securityToCalendar, _securityToMotion;

    private void PrepareStationSecurityClock(long calendar, long motion)
    {
        _securityFromCalendar = _processedWorldTimeMs;
        _securityFromMotion = _processedSimulationTimeMs;
        _securityToCalendar = calendar;
        _securityToMotion = motion;
    }

    private void StopAtStationSecurityDeadline(long motion)
    {
        if (IsStoppedByStationSecurity) return;
        decimal fraction = _securityToMotion == _securityFromMotion ? 0 :
            Math.Clamp(((decimal)motion - _securityFromMotion) / (_securityToMotion - (decimal)_securityFromMotion), 0, 1);
        long calendar = _securityFromCalendar + (long)decimal.Floor((_securityToCalendar - (decimal)_securityFromCalendar) * fraction);
        _stationSecurityStop = new(calendar, SimulationSpeed.Speed0, motion);
        _clock.Reset(calendar, SimulationSpeed.Speed0, motion);
    }

    private bool ApplyStationSecurityStop()
    {
        if (_stationSecurityStop is not { } clock) return false;
        _processedWorldTimeMs = clock.GameTimeMs;
        _processedSimulationTimeMs = clock.MotionTimeMs;
        _appliedMotionTimeMs = clock.MotionTimeMs;
        return true;
    }

    private void ThrowIfStoppedByStationSecurity()
    {
        if (IsStoppedByStationSecurity) throw new InvalidOperationException(StationSecurityStoppedReason);
    }

    private void RejectCommandsAfterStationSecurityStop(long time)
    {
        while (_pendingDialogueCommands.TryDequeue(out var command))
        {
            if (string.IsNullOrWhiteSpace(command.CommandId)) continue;
            _dialogue.ProcessedCommandIds = _dialogue.ProcessedCommandIds.Add(command.CommandId);
            _dialogue.Emit(command.DialogueInstanceId, StationSecurityStoppedReason, time, command.CommandId);
        }
    }
}
