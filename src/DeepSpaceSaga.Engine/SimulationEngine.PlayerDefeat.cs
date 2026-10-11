using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private SessionOutcomeSnapshot _sessionOutcome = SessionOutcomeSnapshot.Running;
    private bool IsDefeated => _sessionOutcome.State == SessionOutcomeState.Defeated;
    private long _defeatFromCalendar, _defeatFromMotion, _defeatToCalendar, _defeatToMotion;

    private void PrepareDefeatTimeMapping(SimulationClockState clock)
    {
        _defeatFromCalendar = _processedWorldTimeMs;
        _defeatFromMotion = _processedSimulationTimeMs;
        _defeatToCalendar = clock.GameTimeMs;
        _defeatToMotion = clock.MotionTimeMs;
    }

    private void RecordPlayerDefeat(string playerId, string wreckId, double motionTime, long deathEventId)
    {
        if (playerId != PlayerShipObjectId || IsDefeated) return;
        decimal fraction = _defeatToMotion == _defeatFromMotion ? 0 :
            Math.Clamp(((decimal)motionTime - _defeatFromMotion) / (_defeatToMotion - (decimal)_defeatFromMotion), 0, 1);
        long calendar = _defeatFromCalendar + (long)decimal.Floor((_defeatToCalendar - (decimal)_defeatFromCalendar) * fraction);
        _sessionOutcome = new(SessionOutcomeState.Defeated, playerId, wreckId, deathEventId, motionTime, calendar);
        var stopped = DefeatedClock;
        _clock.Reset(stopped.GameTimeMs, SimulationSpeed.Speed0, stopped.MotionTimeMs);
    }

    private SimulationClockState DefeatedClock => new(_sessionOutcome.DeathGameTimeMs!.Value,
        SimulationSpeed.Speed0, (long)Math.Floor(_sessionOutcome.DeathMotionTimeMs!.Value));

    private bool CommitDefeatBoundary()
    {
        if (!IsDefeated) return false;
        _processedWorldTimeMs = DefeatedClock.GameTimeMs;
        _processedSimulationTimeMs = DefeatedClock.MotionTimeMs;
        _combatProcessedTimeMs = DefeatedClock.MotionTimeMs;
        return true;
    }

    private void ThrowIfDefeated()
    {
        if (IsDefeated) throw new InvalidOperationException(CommandReasonCodes.SessionDefeated);
    }

    private void RejectDefeatedDialogueCommands(long time)
    {
        while (_pendingDialogueCommands.TryDequeue(out var command))
        {
            if (string.IsNullOrWhiteSpace(command.CommandId)) continue;
            _dialogue.ProcessedCommandIds = _dialogue.ProcessedCommandIds.Add(command.CommandId);
            _dialogue.Emit(command.DialogueInstanceId, CommandReasonCodes.SessionDefeated, time, command.CommandId);
        }
    }
}
