using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private static bool HasManualHolds(ObjectMotionSnapshot motion) => motion.TurnHoldRateDegPerSec is not null || motion.SpeedHoldAccelerationKmS2 is not null;

    private void StopManualHolds(int index, long time)
    {
        var owner = _objects[index];
        _objects[index] = owner with { InitialMotion = ClearEvasionControls(RuntimeMotion.At(owner, time)), StartGameTimeMs = time };
    }

    private CommandStartOutcome TrySpeedHold(PlayerCommand command, long time)
    {
        if (command.SpeedDirection is not { } direction || !Enum.IsDefined(direction) || command.Active is null ||
            command.Latched == true && (direction != SpeedDirection.Accelerate || command.Active == false))
            return CommandStartOutcome.Rejected(CommandReasonCodes.InvalidSpeedPayload);
        int index = _objects.FindIndex(o => o.InitialMotion.ObjectId == command.ObjectId && o.InitialMotion.ObjectId == PlayerShipObjectId && !o.IsDestroyed);
        if (index < 0) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownObject);
        var owner = _objects[index];
        var module = owner.Modules.FirstOrDefault(m => m.ModuleId == command.ModuleId);
        if (module is null) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownModule);
        var type = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
        if (!IsEngineCategory(type)) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownCommandType);
        var pose = RuntimeMotion.At(owner, time);
        if (command.Active == false)
        {
            if (pose.SpeedHoldAccelerationKmS2 is { } held && (held > 0) == (direction == SpeedDirection.Accelerate))
                _objects[index] = owner with
                {
                    InitialMotion = pose with { SpeedHoldAccelerationKmS2 = null, SpeedHoldLatched = false },
                    StartGameTimeMs = time
                };
        }
        else
        {
            if (owner.IsDocked || !CanExecuteEngineCommand(module, type, time) || type.LinearInertiaMps2 is not > 0 || type.MaxSpeedMps is not > 0)
                return CommandStartOutcome.Rejected(CommandReasonCodes.EngineDisabled);
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
            var motion = ClearEvasionControls(pose) with
            {
                TurnHoldRateDegPerSec = pose.TurnHoldRateDegPerSec,
                SpeedHoldAccelerationKmS2 = type.LinearInertiaMps2.Value / 1000.0 * (direction == SpeedDirection.Accelerate ? 1 : -1),
                SpeedHoldLatched = command.Latched == true,
                MaxSpeedKmS = type.MaxSpeedMps.Value / 1000.0
            };
            _objects[index] = owner with { InitialMotion = motion, StartGameTimeMs = time, Modules = modules };
        }
        RecordCommandResult(command, CommandResultStatus.Executed, time);
        return CommandStartOutcome.Started;
    }
}
