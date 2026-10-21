using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private CommandStartOutcome TryTurnHold(PlayerCommand command, long time)
    {
        if (command.TurnDirection is not { } direction || !Enum.IsDefined(direction) || command.Active is null)
            return CommandStartOutcome.Rejected(CommandReasonCodes.InvalidTurnPayload);
        int index = _objects.FindIndex(o => o.InitialMotion.ObjectId == command.ObjectId &&
            o.InitialMotion.ObjectId == PlayerShipObjectId && !o.IsDestroyed);
        if (index < 0) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownObject);
        var owner = _objects[index];
        var module = owner.Modules.FirstOrDefault(m => m.ModuleId == command.ModuleId);
        if (module is null) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownModule);
        var type = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
        if (!IsEngineCategory(type)) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownCommandType);
        if (command.Active == false)
        {
            // A delayed release of the other key must not cancel the current direction.
            if (owner.InitialMotion.TurnHoldRateDegPerSec is { } held &&
                (held < 0) == (direction == TurnDirection.Left)) StopTurnHold(index, time);
        }
        else
        {
            if (owner.IsDocked || !CanExecuteEngineCommand(module, type, time) || type.AngularInertiaDegPerSec is not > 0)
                return CommandStartOutcome.Rejected(CommandReasonCodes.EngineDisabled);
            var pose = ClearEvasionControls(RuntimeMotion.At(owner, time));
            var modules = owner.Modules;
            for (int i = 0; i < modules.Length; i++)
            {
                var current = modules[i];
                if (current.ActiveCycle is not { } cycle) continue;
                var definition = _registry.ModuleTypes.GetDefinition(current.ModuleTypeIndex);
                if (!IsEngineCategory(definition) && cycle.CommandType != NavigationComputerCommandTypes.Approach) continue;
                RecordCommandResultFromCycle(cycle, CommandResultStatus.Cancelled, time);
                modules = modules.SetItem(i, current with { ActiveCycle = null });
            }
            _objects[index] = owner with
            {
                InitialMotion = pose with
                {
                    TurnHoldRateDegPerSec =
                    type.AngularInertiaDegPerSec.Value * (direction == TurnDirection.Left ? -1 : 1)
                },
                StartGameTimeMs = time,
                Modules = modules
            };
        }
        RecordCommandResult(command, CommandResultStatus.Executed, time);
        return CommandStartOutcome.Started;
    }

    private void StopTurnHold(int index, long time)
    {
        var owner = _objects[index];
        _objects[index] = owner with
        {
            InitialMotion = ClearEvasionControls(RuntimeMotion.At(owner, time)),
            StartGameTimeMs = time
        };
    }

    private void StopAllTurnHolds()
    {
        for (int i = 0; i < _objects.Count; i++)
            if (_objects[i].InitialMotion.TurnHoldRateDegPerSec is not null) StopTurnHold(i, _combatProcessedTimeMs);
    }

    private CommandStartOutcome ExecuteTurnHoldOverride(PlayerCommand command, long time, Func<CommandStartOutcome> execute)
    {
        int index = _objects.FindIndex(o => o.InitialMotion.ObjectId == command.ObjectId);
        if (index < 0 || _objects[index].InitialMotion.TurnHoldRateDegPerSec is null) return execute();
        var original = _objects[index];
        StopTurnHold(index, time);
        var result = execute();
        if (result.Disposition != CommandStartDisposition.Started) _objects[index] = original;
        return result;
    }

    private static SpaceObjectRuntime InterruptDamagedTurnHold(SpaceObjectRuntime ship, double time)
    {
        if (ship.InitialMotion.TurnHoldRateDegPerSec is null) return ship;
        var pose = CurrentManeuverForecast.Sample(ship.InitialMotion, Math.Max(0, time - ship.StartGameTimeMs));
        long anchor = (long)Math.Floor(time);
        var p = LinearMotionPredictor.PredictStraightPose(pose.X, pose.Y, pose.Direction, pose.SpeedKmS, anchor - time);
        return ship with
        {
            InitialMotion = ClearEvasionControls(pose) with { X = p.X, Y = p.Y },
            StartGameTimeMs = anchor
        };
    }
}
