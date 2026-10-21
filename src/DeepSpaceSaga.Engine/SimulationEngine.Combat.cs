using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Combat;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private ModuleHitLocationRng _moduleHitLocationRng = new(0);
    private Dictionary<(string ObjectId, string ModuleId), LauncherCombatSnapshot> _launcherCombat = new();
    private long _torpedoSequence;
    private long _combatImpactSequence;
    private long _wreckSequence;
    private readonly List<CombatImpactSnapshot> _combatImpacts = new();
    private readonly HashSet<string> _processedLaunchCommandIds = new(StringComparer.Ordinal);
    private bool HasActiveTorpedoes => _objects.Any(o => o.InitialMotion.OwnProjectile is not null);

    private bool TryGetNextTorpedoId(out string id, out long sequence)
    {
        sequence = _torpedoSequence;
        id = string.Empty;
        while (sequence < long.MaxValue)
        {
            string candidate = "torpedo-" + (++sequence).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (_objects.Any(o => string.Equals(o.InitialMotion.ObjectId, candidate, StringComparison.OrdinalIgnoreCase))) continue;
            id = candidate;
            return true;
        }
        return false;
    }

    private void AdvanceCombatTo(long motionTimeMs, bool processPlayerBeforeNpc = false)
    {
        if (IsDefeated || motionTimeMs <= _combatProcessedTimeMs) return;
        double cursor = _combatProcessedTimeMs;
        bool processedPlayerBoundary = false;
        while (true)
        {
            var hostileLaunch = NextHostileLaunch(cursor, motionTimeMs);
            var npcDodge = NextNpcDodge(cursor, motionTimeMs);
            double eventTime = Math.Min(hostileLaunch?.Time ?? double.PositiveInfinity,
                npcDodge?.DueAt ?? double.PositiveInfinity);
            double pointTime = NextPointDetonationTime();
            double reloadTime = NextPlayerTorpedoReloadTime(cursor);
            if (reloadTime <= motionTimeMs && reloadTime <= eventTime && reloadTime <= pointTime)
            {
                CompletePlayerTorpedoReloads(reloadTime);
                cursor = reloadTime;
                CapturePresentationFrame(cursor);
                continue;
            }
            if (pointTime <= motionTimeMs && pointTime <= eventTime)
            {
                ResolvePointDetonations(pointTime);
                cursor = pointTime;
                CapturePresentationFrame(cursor);
                if (IsDefeated) return;
                continue;
            }
            // Commands already queued for this update precede NPC actions at its
            // endpoint. Earlier NPC deadlines and scheduled blasts/deaths above
            // retain their physical order. Recompute candidates after commands:
            // a manual blast can remove the actor or end the session immediately.
            if (processPlayerBeforeNpc && !processedPlayerBoundary && eventTime == motionTimeMs)
            {
                processedPlayerBoundary = true;
                ApplyPendingCommands(motionTimeMs);
                if (IsDefeated) return;
                continue;
            }
            if (npcDodge is not null && npcDodge.DueAt <= eventTime)
            {
                ExecuteNpcDodge(npcDodge);
                cursor = npcDodge.DueAt;
                CapturePresentationFrame(cursor);
                continue;
            }
            if (hostileLaunch is not null)
            {
                var result = TryStartHostileTorpedoFire(hostileLaunch.OwnerId, hostileLaunch.ModuleId, hostileLaunch.Time);
                if (result.Disposition != CommandStartDisposition.Started)
                    throw new InvalidOperationException($"Validated hostile launch was rejected: {result.ReasonCode}.");
                cursor = hostileLaunch.Time;
                CapturePresentationFrame(cursor);
                continue;
            }
            break;
        }
        CompleteDefenseReloads(motionTimeMs);
        _combatProcessedTimeMs = motionTimeMs;
    }

    internal static CollisionPath CombatPath(SpaceObjectRuntime obj)
    {
        if (obj.InitialMotion.TurnHoldRateDegPerSec is { } heldRate)
            return new(t =>
            {
                var p = CurrentManeuverForecast.Sample(obj.InitialMotion, Math.Max(0, t - obj.StartGameTimeMs));
                return (p.X, p.Y);
            }, obj.InitialMotion.SpeedKmS / 100,
                obj.InitialMotion.SpeedKmS / 100 * Math.Abs(heldRate) * Math.PI / 180_000);
        if (obj.InitialMotion.Evasion is { } evasion)
            return new(t =>
            {
                var p = CurrentManeuverForecast.Sample(obj.InitialMotion, Math.Max(0, t - evasion.SampleMotionTimeMs));
                return (p.X, p.Y);
            }, Math.Max(Math.Max(obj.InitialMotion.SpeedKmS, evasion.BaseMotion.SpeedKmS), evasion.Steering?.MaxSpeedKmS ?? 0) / 100,
                evasion.Steering is { } steering ? steering.AccelerationKmS2 / 100_000 + steering.MaxSpeedKmS / 100 *
                    steering.TurnRateDegPerSec * Math.PI / 180_000 : 0);
        if (obj.InitialMotion.OwnProjectile is { } point)
            return new(t =>
            {
                double duration = point.DetonationMotionTimeMs - point.LaunchMotionTimeMs;
                return BlastMath.PositionAt(point.StartX, point.StartY, point.DetonationX, point.DetonationY,
                    duration == 0 ? 1 : (t - point.LaunchMotionTimeMs) / duration);
            }, point.SpeedKmS / 100, 0);
        if (obj.InitialMotion.HostileCombat is { Segment: var segment })
        {
            double maxSpeed = Math.Max(segment.StartSpeedKmS,
                segment.StartSpeedKmS + segment.AccelerationKmS2 * segment.DurationMs / 1000.0);
            return new(t =>
            {
                var p = HostileOrbitMath.Sample(segment, t - segment.EpochMs);
                // Defense forecasts may look beyond the confirmed segment. Continue its
                // terminal velocity for that estimate, never freeze the moving hull or run AI.
                // Actual outcomes are searched only up to the next controller boundary.
                double coastMs = t - segment.EpochMs - segment.DurationMs;
                if (coastMs > 0)
                {
                    double angle = p.Direction * Math.PI / 180;
                    return (p.X + p.SpeedKmS / 100 * coastMs * Math.Sin(angle),
                        p.Y - p.SpeedKmS / 100 * coastMs * Math.Cos(angle));
                }
                return (p.X, p.Y);
            }, maxSpeed / 100,
                Math.Abs(segment.AccelerationKmS2) / 100_000 + maxSpeed / 100 * Math.Abs(segment.TurnRateDegPerSec) * Math.PI / 180_000);
        }
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
                var p = LinearMotionPredictor.PredictStraightPose(obj.InitialMotion.X, obj.InitialMotion.Y,
                    obj.InitialMotion.Direction, obj.InitialMotion.SpeedKmS, t - obj.StartGameTimeMs);
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

    private readonly record struct ModuleHitOutcome(SpaceObjectRuntime UpdatedShip, string ModuleId,
        int ArmorBefore, int ArmorAfter, int StructureBefore, int StructureAfter);

    private ModuleHitOutcome? ApplyModuleHit(SpaceObjectRuntime target, int damage,
        decimal armorPiercingPercent, decimal armorDamagePercent, double motionTimeMs, bool requireShipClass = true)
    {
        int targetIndex = _objects.FindIndex(o => o.InitialMotion.ObjectId == target.InitialMotion.ObjectId);
        if (targetIndex < 0) return null;
        target = _objects[targetIndex];
        if (target.ObjectType is not (SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip) ||
            requireShipClass && target.ShipClassId is null) return null;
        var candidates = target.Modules.Where(m => !m.Destroyed && m.StructurePoints > 0)
            .OrderBy(m => m.ModuleId, StringComparer.Ordinal)
            .Select(m => (Module: m, Mass: _registry.ModuleTypes.GetDefinition(m.ModuleTypeIndex).MassKg))
            .ToArray();
        if (candidates.Length == 0) return null;

        ulong totalMass = 0;
        foreach (var candidate in candidates)
            totalMass = checked(totalMass + checked((ulong)candidate.Mass));
        ulong draw = _moduleHitLocationRng.NextBelow(totalMass);
        InstalledModuleRuntime selected = candidates[^1].Module;
        foreach (var candidate in candidates)
        {
            if (draw < (ulong)candidate.Mass)
            {
                selected = candidate.Module;
                break;
            }
            draw -= (ulong)candidate.Mass;
        }

        var result = ModuleDamageMath.Apply(damage, armorPiercingPercent,
            armorDamagePercent, selected.Armor, selected.StructurePoints);
        double? disabledUntil = result.StructureAfter == 0 ? null : Math.Max(
            selected.DisabledUntilMotionTimeMs ?? 0,
            motionTimeMs + (_registry.CombatRules?.Damage.HitDisableMs ?? 60000));
        var updated = selected with
        {
            Armor = result.ArmorAfter,
            StructurePoints = result.StructureAfter,
            DisabledUntilMotionTimeMs = disabledUntil,
            Destroyed = result.StructureAfter == 0,
            ActiveCycle = null
        };
        long interruptTime = (long)Math.Floor(motionTimeMs);
        var type = _registry.ModuleTypes.GetDefinition(selected.ModuleTypeIndex);
        bool interruptMotion = selected.ActiveCycle is not null ||
            (type.CombatRole == "engine" && target.InitialMotion.HostileCombat is not null);
        var updatedShip = target with { Modules = target.Modules.SetItem(target.Modules.IndexOf(selected), updated) };
        if (interruptMotion)
        {
            var pose = RuntimeMotion.At(target, interruptTime) with
            {
                ActiveEngineCommandType = null,
                HostileCombat = null,
                ApproachRoute = null
            };
            updatedShip = updatedShip with { InitialMotion = pose, StartGameTimeMs = interruptTime };
        }
        if (type.CategoryTypeId == "module.engine") updatedShip = InterruptDamagedEvasion(updatedShip, motionTimeMs);
        _objects[targetIndex] = updatedShip;
        if (selected.ActiveCycle is { } cycle)
        {
            string reason = updated.Destroyed ? CommandReasonCodes.ModuleDestroyed : CommandReasonCodes.ModuleDisabled;
            RecordCommandResultFromCycle(cycle, CommandResultStatus.Cancelled, interruptTime, reason);
            RecordShipEvent(target.InitialMotion.ObjectId, selected.ModuleId,
                ShipEventTypes.CycleInterrupted, reason, interruptTime);
        }
        return new(updatedShip, selected.ModuleId, selected.Armor, result.ArmorAfter,
            selected.StructurePoints, result.StructureAfter);
    }

    private string? ShipDestroyReason(SpaceObjectRuntime ship, string destroyedModuleId)
    {
        var rules = _registry.CombatRules?.ShipDestruction;
        if (rules is null) return null;
        return ShipDestructionMath.Cause(ship.Modules.Select(module => new ShipModuleLife(module.ModuleId,
            _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex).CombatRole, module.Destroyed)).ToArray(),
            destroyedModuleId, rules.CriticalRoles, rules.DestroyedModuleFraction);
    }

    private string DestroyShip(string objectId, string cause, double impactMotionTimeMs,
        string? actorObjectId = null, string? projectileObjectId = null, (double X, double Y)? positionOverride = null)
    {
        var destroyed = _objects.First(o => o.InitialMotion.ObjectId == objectId);
        string id;
        do
        {
            id = "wreck-" + checked(++_wreckSequence).ToString(System.Globalization.CultureInfo.InvariantCulture);
        } while (_objects.Any(o => string.Equals(o.InitialMotion.ObjectId, id, StringComparison.OrdinalIgnoreCase)));
        var point = positionOverride ?? CombatPath(destroyed).Position(impactMotionTimeMs);
        CancelEvasionState(objectId, (long)Math.Ceiling(impactMotionTimeMs), CommandReasonCodes.ModuleDestroyed);
        RecordEncounterEnemyDeath(destroyed, projectileObjectId);
        _objects.Remove(destroyed);
        _hostileCombat.Remove(objectId);
        foreach (var defenseKey in _defenses.Keys.Where(k => k.ObjectId == objectId).ToArray()) _defenses.Remove(defenseKey);
        foreach (var key in _launcherCombat.Keys.Where(k => k.ObjectId == objectId).ToArray())
            _launcherCombat.Remove(key);
        _objects.Add(new(new(id, point.X, point.Y, 0, 0), SpaceObjectType.Wreck,
            (long)Math.Floor(impactMotionTimeMs), [], Name: "Wreck", IsKnown: destroyed.IsKnown,
            RelationToPlayer: UsesPointEncounters && destroyed.InitialMotion.ObjectId != PlayerShipObjectId ? GetRelationToPlayer(destroyed) : null));
        if (SelectedObjectId == objectId) SelectedObjectId = null;
        if (ActiveObjectId == objectId) ActiveObjectId = null;
        ClearDestroyedTargetNavigation(objectId, impactMotionTimeMs);
        AppendCombatEvent(new(0, impactMotionTimeMs, CombatEventType.ShipDestroyed,
            actorObjectId ?? objectId, objectId, projectileObjectId ?? string.Empty,
            point.X, point.Y, Result: id,
            DestroyReason: objectId == PlayerShipObjectId || _registry.CombatRules?.IsPointAim != true ? cause : null));
        RecordPlayerDefeat(objectId, id, impactMotionTimeMs, _combatJournalSequence);
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
                    var atAnchor = LinearMotionPredictor.PredictStraightPose(pose.X, pose.Y, pose.Direction,
                        route.SpeedKmS, anchor - motionTimeMs);
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

    // Stage all combat state before publishing the candidate world. Failed loads leave
    // the current session intact; successful loads replace both maps, including empty ones.
    private Dictionary<(string ObjectId, string ModuleId), LauncherCombatSnapshot> BuildCombatState(
        IReadOnlyList<SpaceObjectData> objects, IReadOnlyList<SpaceObjectRuntime> runtimeObjects)
    {
        var launchers = new Dictionary<(string ObjectId, string ModuleId), LauncherCombatSnapshot>();
        foreach (var obj in objects)
        {
            if (obj.ShipClassId is not { } classId)
                continue;
            if (!_registry.ShipClasses.Contains(classId))
                throw new ScenarioException($"Object '{obj.ObjectId}' references unknown shipClassId '{classId}'.");
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
                if (!definition.IsPointLauncher || definition.CategoryTypeId != "module.torpedo.launcher")
                    throw new ScenarioException($"Launcher '{module.ModuleId}' uses a retired guided weapon definition.");
                if (_registry.CombatRules is not { IsPointAim: true })
                    throw new ScenarioException("Point launcher requires current combatRules.");
                launchers.Add((obj.InitialMotion.ObjectId, module.ModuleId), new LauncherCombatSnapshot(
                    definition.Profiles[0].SpeedKmS, 0, damage,
                    Operator: ResolveWeaponOperator(obj, module, WeaponSkillType.TorpedoAttack),
                    RangeKm: definition.RangeKm, ActiveTorpedoIds: [], ReloadDurationMs: definition.ReloadMs,
                    Profiles: definition.Profiles, ScatterPercent: definition.ScatterPercent));
            }
        }

        return launchers;
    }
}
