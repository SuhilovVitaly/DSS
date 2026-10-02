using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private Dictionary<string, HullCombatSnapshot> _hullCombat = new(StringComparer.Ordinal);
    private Dictionary<(string ObjectId, string ModuleId), LauncherCombatSnapshot> _launcherCombat = new();
    private long _torpedoSequence;
    private long _combatImpactSequence;
    private long _wreckSequence;
    private readonly List<CombatImpactSnapshot> _combatImpacts = new();
    private long _nextCombatGuidanceMs = long.MaxValue;
    private readonly Dictionary<string, (ObjectMotionSnapshot Pose, long Time)> _torpedoTargets = new(StringComparer.Ordinal);
    private bool HasActiveTorpedoes => _objects.Any(o => o.InitialMotion.Torpedo is not null);

    private CommandStartOutcome TryStartTorpedoFire(PlayerCommand command, long motionTimeMs)
    {
        var owner = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == command.ObjectId &&
            o.InitialMotion.ObjectId == PlayerShipObjectId && o.ObjectType == SpaceObjectType.PlayerShip && !o.IsDestroyed);
        if (owner is null) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownObject);
        int index = FindModuleIndex(owner.Modules, command.ModuleId);
        if (index < 0) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownModule);
        var module = owner.Modules[index];
        var key = (command.ObjectId, command.ModuleId);
        if (!_launcherCombat.TryGetValue(key, out var launcher) ||
            !_registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex).CommandTypeIds.Contains(CombatCommandTypes.Fire))
            return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownCommandType);
        if (!CanExecuteModuleCommand(module)) return CommandStartOutcome.Rejected(CommandReasonCodes.ModuleUnavailable);
        if (launcher.ActiveTorpedoObjectId is not null || module.ActiveCycle is not null)
            return CommandStartOutcome.Rejected(CommandReasonCodes.Busy);
        if (string.IsNullOrWhiteSpace(command.TargetObjectId)) return CommandStartOutcome.Rejected(CommandReasonCodes.MissingTarget);
        var target = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == command.TargetObjectId && !o.IsDestroyed);
        if (target is null || command.TargetObjectId == command.ObjectId)
            return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownTarget);

        var origin = PredictMotion(owner, motionTimeMs - owner.StartGameTimeMs);
        var targetPose = PredictMotion(target, motionTimeMs - target.StartGameTimeMs);
        var route = TorpedoGuidanceMath.Plan(origin, targetPose, launcher.SpeedKmS, launcher.TurnRateDegPerSec, motionTimeMs);
        long sequence = _torpedoSequence;
        string id;
        do
        {
            if (sequence == long.MaxValue) return CommandStartOutcome.Rejected("projectile_id_exhausted");
            id = "torpedo-" + (++sequence).ToString(System.Globalization.CultureInfo.InvariantCulture);
        } while (_objects.Any(o => o.InitialMotion.ObjectId == id));
        var flight = new TorpedoSnapshot(command.ObjectId, command.ModuleId, command.TargetObjectId!, motionTimeMs,
            launcher.SpeedKmS, launcher.TurnRateDegPerSec, launcher.Damage, 0, route, [], ImpactTime(route));
        var motion = new ObjectMotionSnapshot(id, origin.X, origin.Y, launcher.SpeedKmS, origin.Direction, Torpedo: flight);
        _objects.Add(new(motion, SpaceObjectType.Missile, motionTimeMs, [], Name: "Torpedo", IsKnown: true));
        _launcherCombat[key] = launcher with { ActiveTorpedoObjectId = id };
        _torpedoTargets[id] = (targetPose, motionTimeMs);
        _torpedoSequence = sequence;
        _nextCombatGuidanceMs = Math.Min(_nextCombatGuidanceMs, NextGuidanceBoundary(motionTimeMs));
        RecordCommandResult(command, CommandResultStatus.Executed, motionTimeMs);
        return CommandStartOutcome.Started;
    }

    private static long NextGuidanceBoundary(long time) => time > long.MaxValue - 100
        ? long.MaxValue : time - time % 100 + 100;

    private static long? ImpactTime(TorpedoRoute route)
    {
        double duration = route.Segments.Sum(s => s.DurationMs);
        return route.HasIntercept && duration < long.MaxValue - route.StartMotionTimeMs
            ? route.StartMotionTimeMs + (long)Math.Ceiling(duration) : null;
    }

    private void AdvanceCombatTo(long motionTimeMs)
    {
        // Recompute after each mutation: a previous contact may remove an obstacle
        // (or, in the wreck lifecycle, replace it) inside this same physical interval.
        double cursor = 0;
        while (FindFirstImpact(cursor, motionTimeMs) is { } impact)
        {
            ApplyImpact(impact.Projectile, impact.Target, impact.Contact);
            cursor = impact.Contact.MotionTimeMs;
        }
        for (int i = 0; i < _objects.Count; i++)
        {
            var obj = _objects[i];
            if (obj.InitialMotion.Torpedo is not { } flight || motionTimeMs <= obj.StartGameTimeMs) continue;
            long elapsed = motionTimeMs - obj.StartGameTimeMs;
            var predicted = TorpedoGuidanceMath.Predict(obj.InitialMotion, elapsed);
            predicted = predicted with { Torpedo = CloseFlightHistory(obj, motionTimeMs, predicted.Torpedo!) };
            _objects[i] = obj with { InitialMotion = predicted, StartGameTimeMs = motionTimeMs };
        }
    }

    private (SpaceObjectRuntime Projectile, SpaceObjectRuntime Target, TorpedoContact Contact)? FindFirstImpact(
        double from, long to)
    {
        (SpaceObjectRuntime Projectile, SpaceObjectRuntime Target, TorpedoContact Contact)? first = null;
        foreach (var projectile in _objects)
        {
            // Initial overlap is resolved on the first physical advance (epic A03),
            // never by repeated snapshots or calendar-only advancement on pause.
            if (projectile.InitialMotion.Torpedo is not { } flight || to <= projectile.StartGameTimeMs) continue;
            var path = CombatPath(projectile);
            foreach (var target in _objects)
            {
                if (target.IsDestroyed || target.InitialMotion.ObjectId == projectile.InitialMotion.ObjectId ||
                    target.InitialMotion.ObjectId == flight.OwnerObjectId) continue;
                double start = Math.Max(from, Math.Max(projectile.StartGameTimeMs, target.StartGameTimeMs));
                if (start > to) continue;
                var hit = TorpedoCollisionMath.FirstContact(path, CombatPath(target), start, to);
                if (hit is not { } contact) continue;
                if (first is not { } previous || contact.MotionTimeMs < previous.Contact.MotionTimeMs ||
                    (contact.MotionTimeMs == previous.Contact.MotionTimeMs &&
                     (string.CompareOrdinal(projectile.InitialMotion.ObjectId, previous.Projectile.InitialMotion.ObjectId) < 0 ||
                      (projectile.InitialMotion.ObjectId == previous.Projectile.InitialMotion.ObjectId &&
                       string.CompareOrdinal(target.InitialMotion.ObjectId, previous.Target.InitialMotion.ObjectId) < 0))))
                    first = (projectile, target, contact);
            }
        }
        return first;
    }

    private static CollisionPath CombatPath(SpaceObjectRuntime obj)
    {
        if (obj.InitialMotion.Torpedo is { } flight)
            return new(t =>
            {
                var p = TorpedoGuidanceMath.PredictPose(flight.Route,
                    flight.Route.ElapsedMs + t - obj.StartGameTimeMs);
                return (p.X, p.Y);
            }, flight.SpeedKmS / 100, flight.SpeedKmS / 100 * flight.TurnRateDegPerSec * Math.PI / 180000);
        foreach (var module in obj.Modules)
        {
            if (module.ActiveCycle is { CommandType: NavigationComputerCommandTypes.Approach, ApproachRoute: { } route } cycle)
                return new(t =>
                {
                    var p = ApproachLineCaptureMath.PredictPose(route, route.ElapsedMs + Math.Max(0, t - cycle.StartedGameTimeMs));
                    return (p.X, p.Y);
                }, route.SpeedKmS / 100);
        }
        if (LinearMotionPredictor.IsLinear(obj.InitialMotion))
            return new(t =>
            {
                var p = TorpedoGuidanceMath.PredictSegment(new(obj.InitialMotion.X, obj.InitialMotion.Y,
                    obj.InitialMotion.Direction, obj.InitialMotion.SpeedKmS, 0, 0), t - obj.StartGameTimeMs);
                return (p.X, p.Y);
            }, obj.InitialMotion.SpeedKmS / 100, 0);
        // Legacy discrete-turn motion has integer-ms boundaries. Interpolate only
        // inside one such interval, preserving its authoritative piecewise path.
        return new(t =>
        {
            long a = (long)Math.Floor(t);
            var p = RuntimeMotion.At(obj, a);
            if (t == a || a == long.MaxValue) return (p.X, p.Y);
            var q = RuntimeMotion.At(obj, a + 1);
            return (p.X + (q.X - p.X) * (t - a), p.Y + (q.Y - p.Y) * (t - a));
        }, obj.InitialMotion.SpeedKmS / 100);
    }

    private void ApplyImpact(SpaceObjectRuntime projectile, SpaceObjectRuntime target, TorpedoContact contact)
    {
        var flight = projectile.InitialMotion.Torpedo!;
        var finalFlight = CloseFlightHistory(projectile, contact.MotionTimeMs, flight);
        int damage = 0;
        string? wreckId = null;
        string hitId = target.InitialMotion.ObjectId;
        if (_hullCombat.TryGetValue(hitId, out var hull) && hull.ShipClassId == "ship.tetrarch")
        {
            damage = Math.Min(hull.CurrentHp, flight.Damage);
            _hullCombat[hitId] = hull with { CurrentHp = hull.CurrentHp - damage };
            if (hull.CurrentHp == damage)
                wreckId = CreateWreck(target, contact.MotionTimeMs);
        }
        _objects.Remove(projectile);
        _torpedoTargets.Remove(projectile.InitialMotion.ObjectId);
        var key = (flight.OwnerObjectId, flight.LauncherModuleId);
        if (_launcherCombat.TryGetValue(key, out var launcher))
            _launcherCombat[key] = launcher with { ActiveTorpedoObjectId = null };
        _combatImpacts.Add(new(checked(++_combatImpactSequence), projectile.InitialMotion.ObjectId,
            flight.OwnerObjectId, flight.LauncherModuleId, flight.TargetObjectId, hitId,
            contact.MotionTimeMs, contact.X, contact.Y, finalFlight.Trail, damage,
            wreckId is null ? null : hitId, wreckId));
    }

    private string CreateWreck(SpaceObjectRuntime destroyed, double impactMotionTimeMs)
    {
        string id;
        do
        {
            id = "wreck-" + checked(++_wreckSequence).ToString(System.Globalization.CultureInfo.InvariantCulture);
        } while (_objects.Any(o => string.Equals(o.InitialMotion.ObjectId, id, StringComparison.OrdinalIgnoreCase)));
        string destroyedId = destroyed.InitialMotion.ObjectId;
        var point = CombatPath(destroyed).Position(impactMotionTimeMs);
        _objects.Remove(destroyed);
        _hullCombat.Remove(destroyedId);
        foreach (var key in _launcherCombat.Keys.Where(k => k.ObjectId == destroyedId).ToArray())
            _launcherCombat.Remove(key);
        _objects.Add(new(new(id, point.X, point.Y, 0, 0), SpaceObjectType.Wreck,
            (long)Math.Floor(impactMotionTimeMs), [], Name: "Wreck", IsKnown: destroyed.IsKnown));
        if (SelectedObjectId == destroyedId) SelectedObjectId = null;
        if (ActiveObjectId == destroyedId) ActiveObjectId = null;
        ClearDestroyedTargetNavigation(destroyedId, impactMotionTimeMs);
        return id;
    }

    private void ClearDestroyedTargetNavigation(string destroyedId, double motionTimeMs)
    {
        long anchor = (long)Math.Floor(motionTimeMs);
        long eventTime = (long)Math.Ceiling(motionTimeMs);
        for (int i = 0; i < _objects.Count; i++)
        {
            var obj = _objects[i];
            var modules = obj.Modules;
            bool changed = false;
            var motion = RuntimeMotion.At(obj, anchor);
            foreach (var module in obj.Modules)
            {
                if (module.ActiveCycle is not { } cycle || cycle.TargetObjectId != destroyedId) continue;
                if (cycle.CommandType == NavigationComputerCommandTypes.Approach && cycle.ApproachRoute is { } route)
                {
                    var pose = ApproachLineCaptureMath.PredictPose(route, route.ElapsedMs + motionTimeMs - cycle.StartedGameTimeMs);
                    // Anchor the new straight continuation on the integer clock without
                    // moving the ship to a rounded contact position or extending its turn.
                    var atAnchor = TorpedoGuidanceMath.PredictSegment(new(pose.X, pose.Y, pose.Direction,
                        route.SpeedKmS, 0, 0), anchor - motionTimeMs);
                    motion = new(obj.InitialMotion.ObjectId, atAnchor.X, atAnchor.Y, route.SpeedKmS, atAnchor.Direction);
                }
                RecordCommandResultFromCycle(cycle, CommandResultStatus.Cancelled, eventTime, CommandReasonCodes.UnknownTarget);
                RecordShipEvent(obj.InitialMotion.ObjectId, module.ModuleId, ShipEventTypes.CycleInterrupted,
                    CommandReasonCodes.UnknownTarget, eventTime);
                modules = modules.SetItem(FindModuleIndex(modules, module.ModuleId), module with { ActiveCycle = null });
                changed = true;
            }
            if (changed)
                _objects[i] = obj with { InitialMotion = motion, StartGameTimeMs = anchor, Modules = modules };
        }
    }

    private static TorpedoSnapshot CloseFlightHistory(SpaceObjectRuntime obj, double motionTimeMs, TorpedoSnapshot predicted)
    {
        var flight = obj.InitialMotion.Torpedo!;
        var history = flight.Trail.ToBuilder();
        double from = flight.Route.ElapsedMs, to = from + motionTimeMs - obj.StartGameTimeMs, cursor = 0;
        foreach (var segment in flight.Route.Segments)
        {
            double start = Math.Max(from, cursor), end = Math.Min(to, cursor + segment.DurationMs);
            if (end > start)
            {
                var pose = TorpedoGuidanceMath.PredictSegment(segment, start - cursor);
                Append(new(flight.Route.StartMotionTimeMs + start,
                    new(pose.X, pose.Y, pose.Direction, segment.SpeedKmS, segment.AngularVelocityDegPerSec, end - start),
                    flight.Route.PlannerVersion));
            }
            cursor += segment.DurationMs;
        }
        if (to > Math.Max(from, cursor))
        {
            double start = Math.Max(from, cursor);
            var pose = TorpedoGuidanceMath.PredictPose(flight.Route, start);
            Append(new(flight.Route.StartMotionTimeMs + start,
                new(pose.X, pose.Y, pose.Direction, flight.SpeedKmS, 0, to - start), flight.Route.PlannerVersion));
        }
        return predicted with
        {
            DistanceTravelledWorldUnits = (motionTimeMs - flight.LaunchMotionTimeMs) * flight.SpeedKmS / 100,
            Trail = history.ToImmutable()
        };

        void Append(TrailSegment segment)
        {
            if (history.Count > 0)
            {
                var previous = history[^1];
                var end = TorpedoGuidanceMath.PredictSegment(previous.Segment, previous.Segment.DurationMs);
                if (previous.PlannerVersion == segment.PlannerVersion &&
                    previous.Segment.AngularVelocityDegPerSec == segment.Segment.AngularVelocityDegPerSec &&
                    previous.Segment.SpeedKmS == segment.Segment.SpeedKmS &&
                    Math.Abs(previous.StartMotionTimeMs + previous.Segment.DurationMs - segment.StartMotionTimeMs) < 1e-6 &&
                    Math.Abs(end.X - segment.Segment.X) < 1e-6 && Math.Abs(end.Y - segment.Segment.Y) < 1e-6 &&
                    Math.Abs((end.Direction - segment.Segment.Direction + 540) % 360 - 180) < 1e-8)
                {
                    history[^1] = previous with
                    {
                        Segment = previous.Segment with
                        { DurationMs = previous.Segment.DurationMs + segment.Segment.DurationMs }
                    };
                    return;
                }
            }
            history.Add(segment);
        }
    }

    private void RefreshCombatGuidance(long motionTimeMs)
    {
        bool boundary = motionTimeMs >= _nextCombatGuidanceMs;
        for (int i = 0; i < _objects.Count; i++)
        {
            var obj = _objects[i];
            if (obj.InitialMotion.Torpedo is not { } flight) continue;
            var target = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == flight.TargetObjectId && !o.IsDestroyed);
            TorpedoRoute route;
            if (target is null)
            {
                if (!_torpedoTargets.Remove(obj.InitialMotion.ObjectId)) continue;
                route = new(motionTimeMs, TorpedoGuidanceMath.PlannerVersion, TorpedoRoutePhase.Straight, false,
                    [new(obj.InitialMotion.X, obj.InitialMotion.Y, obj.InitialMotion.Direction, flight.SpeedKmS, 0, 0)]);
            }
            else
            {
                // Guidance decisions occur only on the physical 100 ms grid,
                // independent of snapshot/calendar partitions and render cadence.
                if (!boundary) continue;
                var pose = PredictMotion(target, motionTimeMs - target.StartGameTimeMs);
                bool changed = !_torpedoTargets.TryGetValue(obj.InitialMotion.ObjectId, out var baseline);
                if (!changed)
                {
                    // The intercept assumes constant target velocity. Compare against
                    // that assumption, not a curved predictor that could hide a turn.
                    var expected = TorpedoGuidanceMath.PredictSegment(new(baseline.Pose.X, baseline.Pose.Y,
                        baseline.Pose.Direction, baseline.Pose.SpeedKmS, 0, 0), motionTimeMs - baseline.Time);
                    changed = Math.Abs(expected.X - pose.X) > 1e-6 || Math.Abs(expected.Y - pose.Y) > 1e-6 ||
                        Math.Abs((expected.Direction - pose.Direction + 540) % 360 - 180) > 1e-8 || baseline.Pose.SpeedKmS != pose.SpeedKmS;
                }
                if (!changed && !(boundary && (!flight.Route.HasIntercept ||
                    flight.Route.ElapsedMs >= flight.Route.Segments.Sum(s => s.DurationMs)))) continue;
                route = TorpedoGuidanceMath.Plan(obj.InitialMotion, pose, flight.SpeedKmS, flight.TurnRateDegPerSec, motionTimeMs);
                _torpedoTargets[obj.InitialMotion.ObjectId] = (pose, motionTimeMs);
            }
            _objects[i] = obj with
            {
                InitialMotion = obj.InitialMotion with
                { Torpedo = flight with { Route = route, PredictedImpactMotionTimeMs = ImpactTime(route) } },
                StartGameTimeMs = motionTimeMs
            };
        }
        if (boundary) _nextCombatGuidanceMs = NextGuidanceBoundary(motionTimeMs);
    }

    // Stage all combat state before publishing the candidate world. Failed loads leave
    // the current session intact; successful loads replace both maps, including empty ones.
    private (Dictionary<string, HullCombatSnapshot> Hulls,
        Dictionary<(string ObjectId, string ModuleId), LauncherCombatSnapshot> Launchers) BuildCombatState(
        IReadOnlyList<SpaceObjectData> objects, IReadOnlyList<SpaceObjectRuntime> runtimeObjects)
    {
        var hulls = new Dictionary<string, HullCombatSnapshot>(StringComparer.Ordinal);
        var launchers = new Dictionary<(string ObjectId, string ModuleId), LauncherCombatSnapshot>();
        foreach (var obj in objects)
        {
            if (obj.ShipClassId is not { } classId)
                continue;
            if (!_registry.ShipClasses.Contains(classId))
                throw new ScenarioException($"Object '{obj.ObjectId}' references unknown shipClassId '{classId}'.");
            var definition = _registry.ShipClasses.GetDefinition(_registry.ShipClasses.GetIndex(classId));
            int currentHp = obj.HullHitPoints ?? definition.HullHitPointsMax;
            if (currentHp <= 0 || currentHp > definition.HullHitPointsMax)
                throw new ScenarioException($"Object '{obj.ObjectId}' hullHitPoints must be in 1..{definition.HullHitPointsMax}.");
            hulls.Add(obj.ObjectId, new HullCombatSnapshot(classId, currentHp, definition.HullHitPointsMax));
        }

        foreach (var obj in runtimeObjects)
        {
            foreach (var module in obj.Modules)
            {
                var definition = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
                if (definition.TorpedoDamage is not { } damage)
                    continue;
                if (obj.ObjectType is not (SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip))
                    throw new ScenarioException($"Launcher '{module.ModuleId}' must be installed on a ship.");
                launchers.Add((obj.InitialMotion.ObjectId, module.ModuleId), new LauncherCombatSnapshot(
                    ActiveTorpedoObjectId: null,
                    SpeedKmS: definition.TorpedoSpeedKmS!.Value,
                    TurnRateDegPerSec: definition.TorpedoTurnRateDegPerSec!.Value,
                    Damage: damage));
            }
        }

        return (hulls, launchers);
    }
}
