using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private sealed record EvasionState(string EngineModuleId, long StartedAt, EvasionSnapshot Motion,
        ActiveCycleData? OrbitCycle, long? LastTurnTime);
    private readonly Dictionary<string, EvasionState> _evasions = new(StringComparer.Ordinal);
    private long _playerEvasionEpisodes;
    private bool HasEvasionActivity => _evasions.Count != 0;

    internal long CaptureEvasionEpisodeCount() { lock (_worldStateLock) return _playerEvasionEpisodes; }

    private CommandStartOutcome TryStartEvade(PlayerCommand command, long time)
    {
        var owner = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == command.ObjectId &&
            o.InitialMotion.ObjectId == PlayerShipObjectId && !o.IsDestroyed);
        if (owner is null) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownObject);
        var outcome = TryEvade(owner, command.ModuleId, command.ThreatProjectileId, command.EvadeSide, time);
        if (outcome.Disposition == CommandStartDisposition.Started)
            RecordCommandResult(command, CommandResultStatus.Executed, time);
        return outcome;
    }

    internal CommandStartOutcome TryStartNpcEvade(string ownerId, string moduleId, string threatId, EvadeSide side, long time)
    {
        lock (_worldStateLock)
        {
            var owner = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == ownerId && o.ObjectType == SpaceObjectType.NpcShip && !o.IsDestroyed);
            return owner is null ? CommandStartOutcome.Rejected(CommandReasonCodes.UnknownObject) : TryEvade(owner, moduleId, threatId, side, time);
        }
    }

    private CommandStartOutcome TryEvade(SpaceObjectRuntime owner, string moduleId, string? threatId, EvadeSide? side, long time)
    {
        if (side is null || !Enum.IsDefined(side.Value)) return CommandStartOutcome.Rejected(CommandReasonCodes.InvalidEvadeSide);
        var threat = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == threatId && !o.IsDestroyed);
        if (threat?.InitialMotion.OwnProjectile is not { Kind: ProjectileKind.Torpedo } flight ||
            flight.OwnerObjectId == owner.InitialMotion.ObjectId)
            return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownThreat);
        var engine = owner.Modules.FirstOrDefault(m => m.ModuleId == moduleId);
        if (engine is null || owner.IsDocked) return CommandStartOutcome.Rejected(CommandReasonCodes.EngineDisabled);
        var type = _registry.ModuleTypes.GetDefinition(engine.ModuleTypeIndex);
        if (!CanExecuteEngineCommand(engine, type, time)) return CommandStartOutcome.Rejected(CommandReasonCodes.EngineDisabled);
        var pose = RuntimeMotion.At(owner, time);
        var observed = RuntimeMotion.At(threat, time);
        double shipAngle = pose.Direction * Math.PI / 180, threatAngle = observed.Direction * Math.PI / 180;
        var assessment = BlastThreatMath.Evaluate(pose.X, pose.Y, pose.SpeedKmS * Math.Sin(shipAngle), -pose.SpeedKmS * Math.Cos(shipAngle),
            observed.X, observed.Y, Math.Sin(threatAngle), -Math.Cos(threatAngle), observed.SpeedKmS, flight.BlastRadiusKm);
        double until = time + (assessment.ClosestAfterSeconds + 0.5) * 1000;
        if (time < 0 || !double.IsFinite(until) || until >= long.MaxValue)
            return CommandStartOutcome.Rejected(CommandReasonCodes.InvalidTime);
        bool continuing = _evasions.TryGetValue(owner.InitialMotion.ObjectId, out var previous);
        if (!continuing && owner.InitialMotion.ObjectId == PlayerShipObjectId && _playerEvasionEpisodes == long.MaxValue)
            return CommandStartOutcome.Rejected(CommandReasonCodes.InvalidTime);
        var basis = previous?.Motion.BaseMotion ?? CaptureEvasionBase(owner, engine, pose, time);
        var orbit = previous?.OrbitCycle ?? (engine.ActiveCycle?.CommandType == ShipEngineCommandTypes.Orbit ? engine.ActiveCycle : null);
        var schedule = new EvasionSnapshot(time, until, threatId!, side.Value, basis);
        var modules = owner.Modules;
        for (int i = 0; i < modules.Length; i++)
        {
            var module = modules[i];
            if (module.ActiveCycle is not { } cycle) continue;
            var definition = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
            if (!IsEngineCategory(definition) && cycle.CommandType != NavigationComputerCommandTypes.Approach) continue;
            if (cycle != orbit) RecordCommandResultFromCycle(cycle, CommandResultStatus.Cancelled, time);
            modules = modules.SetItem(i, module with { ActiveCycle = null });
        }
        double direction = (observed.Direction + (side == EvadeSide.Left ? -90 : 90) + 360) % 360;
        var moving = ClearEvasionControls(pose) with
        {
            Direction = direction,
            SpeedKmS = type.MaxSpeedMps!.Value / 1000.0,
            MaxSpeedKmS = type.MaxSpeedMps.Value / 1000.0,
            Evasion = schedule
        };
        _objects[_objects.IndexOf(owner)] = owner with { InitialMotion = moving, StartGameTimeMs = time, Modules = modules };
        _evasions[owner.InitialMotion.ObjectId] = new(moduleId, previous?.StartedAt ?? time, schedule, orbit, previous?.LastTurnTime ?? engine.LastTurnGameTimeMs);
        if (!continuing && owner.InitialMotion.ObjectId == PlayerShipObjectId) _playerEvasionEpisodes++;
        return CommandStartOutcome.Started;
    }

    private ObjectMotionSnapshot CaptureEvasionBase(SpaceObjectRuntime owner, InstalledModuleRuntime engine, ObjectMotionSnapshot pose, long time)
    {
        var basis = ClearEvasionControls(pose);
        if (engine.ActiveCycle?.CommandType != ShipEngineCommandTypes.Orbit) return basis;
        var cycle = GetActiveEngineCycleMotion(owner, time);
        return basis with
        {
            ActiveEngineCommandType = cycle.CommandType,
            TurnStepDegrees = cycle.TurnStepDegrees,
            TurnStepRemainingMs = cycle.TurnStepRemainingMs,
            TurnStepIntervalMs = cycle.TurnStepIntervalMs,
            NavigationTargetX = cycle.NavigationTargetX,
            NavigationTargetY = cycle.NavigationTargetY,
            NavigationAngularInertiaDegPerSec = cycle.NavigationAngularInertiaDegPerSec,
            NavigationLockedCourseDegrees = cycle.NavigationLockedCourseDegrees,
            NavigationPhase = cycle.NavigationPhase,
            NavigationEscapeCourseDegrees = cycle.NavigationEscapeCourseDegrees,
            NavigationRequiredDepartureDistance = cycle.NavigationRequiredDepartureDistance
        };
    }

    private static ObjectMotionSnapshot ClearEvasionControls(ObjectMotionSnapshot pose) => pose with
    {
        Evasion = null,
        HostileCombat = null,
        EngineManeuver = null,
        Orbit = null,
        OrbitSampleSimulationTimeMs = null,
        ActiveEngineCommandType = null,
        TurnStepDegrees = 0,
        TurnStepRemainingMs = 0,
        TurnStepIntervalMs = 0,
        NavigationTargetX = null,
        NavigationTargetY = null,
        NavigationAngularInertiaDegPerSec = 0,
        NavigationLockedCourseDegrees = null,
        NavigationPhase = null,
        NavigationEscapeCourseDegrees = null,
        NavigationRequiredDepartureDistance = null,
        NavigationTargetSpeedKmS = null,
        NavigationTargetDirectionDegrees = null,
        NavigationApproachTrailDistanceWorldUnits = null,
        ApproachRoute = null,
        NavigationTargetObjectId = null
    };

    private long NextEvasionBoundary() => _evasions.Values.Select(e => (long)Math.Ceiling(e.Motion.EndsAtMotionTimeMs))
        .DefaultIfEmpty(long.MaxValue).Min();

    private void CompleteEvasions(long time)
    {
        foreach (var (id, state) in _evasions.OrderBy(p => p.Key, StringComparer.Ordinal).ToArray())
        {
            long due = (long)Math.Ceiling(state.Motion.EndsAtMotionTimeMs);
            if (due > time) continue;
            int index = _objects.FindIndex(o => o.InitialMotion.ObjectId == id);
            if (index < 0) { _evasions.Remove(id); continue; }
            var owner = _objects[index];
            var pose = RuntimeMotion.At(owner, due) with { Evasion = null };
            var modules = owner.Modules;
            int moduleIndex = FindModuleIndex(modules, state.EngineModuleId);
            if (moduleIndex >= 0 && CanExecuteEngineCommand(modules[moduleIndex], _registry.ModuleTypes.GetDefinition(modules[moduleIndex].ModuleTypeIndex), due))
            {
                long shift = due - state.StartedAt;
                var module = modules[moduleIndex];
                modules = modules.SetItem(moduleIndex, module with
                {
                    ActiveCycle = state.OrbitCycle is { } cycle
                    ? cycle with { StartedGameTimeMs = checked(cycle.StartedGameTimeMs + shift) } : null,
                    LastTurnGameTimeMs = state.LastTurnTime is { } turn ? checked(turn + shift) : module.LastTurnGameTimeMs
                });
            }
            // Projected orbit controls belong to the outgoing snapshot; runtime advances the resumed cycle once.
            _objects[index] = owner with { InitialMotion = ClearEvasionControls(pose), StartGameTimeMs = due, Modules = modules };
            _evasions.Remove(id);
            if (owner.ObjectType == SpaceObjectType.NpcShip) _nextHostileDecisionMs = Math.Min(_nextHostileDecisionMs, due);
        }
    }

    private CommandStartOutcome ExecuteEvasionOverride(PlayerCommand command, long time, Func<CommandStartOutcome> execute)
    {
        if (!_evasions.TryGetValue(command.ObjectId, out var state)) return execute();
        int index = _objects.FindIndex(o => o.InitialMotion.ObjectId == command.ObjectId);
        if (index < 0) return execute();
        var original = _objects[index];
        var pose = ClearEvasionControls(RuntimeMotion.At(original, time));
        _objects[index] = original with { InitialMotion = pose, StartGameTimeMs = time };
        var result = execute();
        if (result.Disposition != CommandStartDisposition.Started) _objects[index] = original;
        else CancelEvasionState(command.ObjectId, time);
        return result;
    }

    private void CancelEvasionState(string id, long time, string? reason = null)
    {
        if (_evasions.Remove(id, out var state) && state.OrbitCycle is { } cycle)
            RecordCommandResultFromCycle(cycle, CommandResultStatus.Cancelled, time, reason);
    }

    private SpaceObjectRuntime InterruptDamagedEvasion(SpaceObjectRuntime ship, double time)
    {
        if (ship.InitialMotion.Evasion is not { } evasion) return ship;
        var pose = CurrentManeuverForecast.Sample(ship.InitialMotion, Math.Max(0, time - evasion.SampleMotionTimeMs));
        long anchor = (long)Math.Floor(time);
        var atAnchor = TorpedoGuidanceMath.PredictSegment(new(pose.X, pose.Y, pose.Direction, pose.SpeedKmS, 0, 0), anchor - time);
        CancelEvasionState(ship.InitialMotion.ObjectId, (long)Math.Ceiling(time), CommandReasonCodes.ModuleDisabled);
        return ship with { InitialMotion = ClearEvasionControls(pose) with { X = atAnchor.X, Y = atAnchor.Y, MaxSpeedKmS = null }, StartGameTimeMs = anchor };
    }
}
