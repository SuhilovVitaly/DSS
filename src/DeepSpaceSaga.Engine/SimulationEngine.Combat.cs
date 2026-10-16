using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Combat;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private ModuleHitLocationRng _moduleHitLocationRng = new(0);
    private CountermeasureRng _torpedoHitRng = new(0, 0);
    private CountermeasureRng _launcherBreakdownRng = new(0, 0);
    private Dictionary<(string ObjectId, string ModuleId), LauncherCombatSnapshot> _launcherCombat = new();
    private long _torpedoSequence;
    private long _combatImpactSequence;
    private long _wreckSequence;
    private readonly List<CombatImpactSnapshot> _combatImpacts = new();
    private long _nextCombatGuidanceMs = long.MaxValue;
    private readonly Dictionary<string, (ObjectMotionSnapshot Pose, long Time)> _torpedoTargets = new(StringComparer.Ordinal);
    private readonly HashSet<string> _processedLaunchCommandIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _processedModeCommandIds = new(StringComparer.Ordinal);
    private bool HasActiveTorpedoes => _objects.Any(o => o.InitialMotion.Torpedo is not null);

    private CommandStartOutcome TryStartTorpedoFire(PlayerCommand command, long motionTimeMs)
    {
        var owner = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == command.ObjectId &&
            o.InitialMotion.ObjectId == PlayerShipObjectId && o.ObjectType == SpaceObjectType.PlayerShip && !o.IsDestroyed);
        if (owner is null) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownObject);
        // Player receipts are never synthesized by the internal hostile actor.
        if (_processedLaunchCommandIds.Contains(command.CommandId))
        {
            RecordCommandResult(command, CommandResultStatus.Executed, motionTimeMs);
            return CommandStartOutcome.Started;
        }
        var outcome = TryLaunchTorpedo(owner, command.ModuleId, command.TargetObjectId, motionTimeMs);
        if (outcome.Disposition == CommandStartDisposition.Started)
        {
            _processedLaunchCommandIds.Add(command.CommandId);
            RecordCommandResult(command, CommandResultStatus.Executed, motionTimeMs);
        }
        return outcome;
    }

    private CommandStartOutcome TrySetFireMode(PlayerCommand command, long motionTimeMs)
    {
        if (command.ObjectId != PlayerShipObjectId) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownObject);
        var owner = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == command.ObjectId && !o.IsDestroyed);
        var module = owner?.Modules.FirstOrDefault(m => m.ModuleId == command.ModuleId);
        if (module is null) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownModule);
        var type = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
        if (type.CategoryTypeId != "module.torpedo.launcher") return CommandStartOutcome.Rejected("mode_not_applicable");
        if (!type.CommandTypeIds.Contains(TorpedoLauncherCommandTypes.SetMode))
            return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownCommandType);
        var key = (command.ObjectId, command.ModuleId);
        if (!_launcherCombat.TryGetValue(key, out var launcher)) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownModule);
        if (_processedModeCommandIds.Contains(command.CommandId))
        {
            RecordCommandResult(command, CommandResultStatus.Executed, motionTimeMs);
            return CommandStartOutcome.Started;
        }
        if (!CanExecuteModuleCommand(module, motionTimeMs))
            return CommandStartOutcome.Rejected(ModuleCombatUnavailableReason(module, motionTimeMs) ?? CommandReasonCodes.ModuleUnavailable);
        if (string.IsNullOrWhiteSpace(command.ModeId) || !_registry.FireModes.Any(mode =>
            mode.ModeId == command.ModeId && mode.AppliesTo.Contains(type.CategoryTypeId)))
            return CommandStartOutcome.Rejected("unknown_fire_mode");
        bool inCombat = _objects.Any(o => o.InitialMotion.Torpedo is not null) ||
            _objects.Any(o => o.ObjectType is SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip &&
                o.InitialMotion.ObjectId != owner!.InitialMotion.ObjectId && !o.IsDestroyed &&
                GetRelationToPlayer(o) == PlayerRelation.Enemy &&
                CombatRangeMath.IsWithinRange(PredictMotion(owner!, motionTimeMs - owner.StartGameTimeMs),
                    PredictMotion(o, motionTimeMs - o.StartGameTimeMs), launcher.RangeKm!.Value));
        _launcherCombat[key] = launcher with
        {
            ModeId = command.ModeId!,
            ReadyAtMotionTimeMs = inCombat
                ? Math.Max(launcher.ReadyAtMotionTimeMs ?? motionTimeMs, motionTimeMs) + type.LauncherReloadMs!.Value
                : launcher.ReadyAtMotionTimeMs
        };
        _processedModeCommandIds.Add(command.CommandId);
        RecordCommandResult(command, CommandResultStatus.Executed, motionTimeMs);
        return CommandStartOutcome.Started;
    }

    private CommandStartOutcome TryLaunchTorpedo(SpaceObjectRuntime owner, string moduleId, string? targetObjectId, long motionTimeMs)
    {
        int index = FindModuleIndex(owner.Modules, moduleId);
        if (index < 0) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownModule);
        var module = owner.Modules[index];
        var key = (owner.InitialMotion.ObjectId, moduleId);
        if (!_launcherCombat.TryGetValue(key, out var launcher) ||
            !_registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex).CommandTypeIds.Contains(CombatCommandTypes.Fire))
            return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownCommandType);
        if (launcher.IsBrokenUntilDock) return CommandStartOutcome.Rejected("launcher_broken_until_dock");
        if (!CanExecuteModuleCommand(module, motionTimeMs))
            return CommandStartOutcome.Rejected(ModuleCombatUnavailableReason(module, motionTimeMs) ?? CommandReasonCodes.ModuleUnavailable);
        if (module.ActiveCycle is not null)
            return CommandStartOutcome.Rejected(CommandReasonCodes.Busy);
        if (launcher.ReadyAtMotionTimeMs is { } readyAt && motionTimeMs < readyAt)
            return CommandStartOutcome.Rejected("launcher_reloading");
        if (string.IsNullOrWhiteSpace(targetObjectId)) return CommandStartOutcome.Rejected(CommandReasonCodes.MissingTarget);
        var target = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == targetObjectId && !o.IsDestroyed);
        if (target is null || target.ObjectType == SpaceObjectType.Countermeasure || targetObjectId == owner.InitialMotion.ObjectId)
            return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownTarget);

        var weaponOperator = ResolveWeaponOperator(owner, module, WeaponSkillType.TorpedoAttack);
        if (weaponOperator is null) return CommandStartOutcome.Rejected("no_weapon_operator");

        var origin = PredictMotion(owner, motionTimeMs - owner.StartGameTimeMs);
        var targetPose = PredictMotion(target, motionTimeMs - target.StartGameTimeMs);
        if (!CombatRangeMath.IsWithinRange(origin, targetPose, launcher.RangeKm!.Value))
            return CommandStartOutcome.Rejected("target_out_of_range");
        var moduleType = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
        long lifetime = moduleType.TorpedoMaxFlightTimeMs ?? 120000;
        if (motionTimeMs > long.MaxValue - lifetime) return CommandStartOutcome.Rejected("invalid_time");
        var route = TorpedoGuidanceMath.Plan(origin, targetPose, launcher.SpeedKmS, launcher.TurnRateDegPerSec, motionTimeMs);
        if (!TryGetNextTorpedoId(out string id, out long sequence))
            return CommandStartOutcome.Rejected("projectile_id_exhausted");
        var mode = FireModeMath.Resolve(_registry, launcher.ModeId);
        decimal attack = (moduleType.Accuracy ?? moduleType.TorpedoManeuverability ?? 0) +
            (owner.Crew.FirstOrDefault(c => c.Id == module.OperatorCrewId)?.AccuracySkill ?? 0) + mode.AccuracyBonus;
        decimal maneuverability = weaponOperator.EffectiveRating + mode.ManeuverabilityBonus;
        var flight = new TorpedoSnapshot(owner.InitialMotion.ObjectId, moduleId, targetObjectId!, motionTimeMs,
            launcher.SpeedKmS, launcher.TurnRateDegPerSec, launcher.Damage, 0, route, [], ImpactTime(route),
            TorpedoRating: maneuverability, RatingBreakdown: weaponOperator,
            CapturedManeuverability: maneuverability,
            OwnerPlayerRelationAtLaunch: GetRelationToPlayer(owner) ?? PlayerRelation.Neutral,
            CapturedAttack: attack,
            HitChanceTenths: CombatChanceMath.ChanceTenths(attack, TorpedoDefenseRating(target, motionTimeMs), ChanceRules),
            ArmorPiercingPercent: moduleType.ArmorPiercingPercent ?? 0,
            ArmorDamagePercent: moduleType.ArmorDamagePercent ?? 100,
            ModeId: mode.ModeId,
            ModeManeuverabilityBonus: mode.ManeuverabilityBonus,
            ExpiresAtMotionTimeMs: motionTimeMs + lifetime);
        var motion = new ObjectMotionSnapshot(id, origin.X, origin.Y, launcher.SpeedKmS, origin.Direction, Torpedo: flight);
        _objects.Add(new(motion, SpaceObjectType.Missile, motionTimeMs, [], Name: "Torpedo", IsKnown: true));
        var activeIds = launcher.ActiveTorpedoIds.IsDefault ? ImmutableArray<string>.Empty : launcher.ActiveTorpedoIds;
        _launcherCombat[key] = launcher with
        {
            ActiveTorpedoIds = activeIds.Add(id),
            ReadyAtMotionTimeMs = motionTimeMs + (double)(moduleType.LauncherReloadMs!.Value * mode.ReloadMultiplier),
            Operator = weaponOperator
        };
        _torpedoTargets[id] = (targetPose, motionTimeMs);
        _torpedoSequence = sequence;
        AppendCombatEvent(new(0, motionTimeMs, CombatEventType.Launch, owner.InitialMotion.ObjectId, targetObjectId!, id,
            origin.X, origin.Y, TorpedoOperator: weaponOperator, Result: "torpedo", LauncherModuleId: moduleId));
        if (mode.BreakdownChancePercent > 0)
        {
            int roll = _launcherBreakdownRng.NextRoll();
            int chanceTenths = (int)(mode.BreakdownChancePercent * 10);
            if (roll <= chanceTenths)
            {
                _launcherCombat[key] = _launcherCombat[key] with { IsBrokenUntilDock = true };
                AppendCombatEvent(new(0, motionTimeMs, CombatEventType.LauncherBreakdown,
                    owner.InitialMotion.ObjectId, targetObjectId!, id, origin.X, origin.Y,
                    ChanceTenths: chanceTenths, Roll: roll, Result: mode.ModeId,
                    LauncherModuleId: moduleId));
            }
        }
        _nextCombatGuidanceMs = Math.Min(_nextCombatGuidanceMs, NextGuidanceBoundary(motionTimeMs));
        return CommandStartOutcome.Started;
    }

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

    private CommandStartOutcome TrySelfDestruct(PlayerCommand command, long motionTimeMs)
    {
        if (command.ObjectId != PlayerShipObjectId) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownObject);
        var key = (command.ObjectId, command.ModuleId);
        if (!_launcherCombat.TryGetValue(key, out var launcher)) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownModule);
        if (command.TargetObjectId is null || launcher.ActiveTorpedoIds.IsDefaultOrEmpty ||
            !launcher.ActiveTorpedoIds.Contains(command.TargetObjectId))
            return CommandStartOutcome.Rejected("stale_projectile");
        var projectile = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == command.TargetObjectId &&
            o.InitialMotion.Torpedo is { } flight && flight.OwnerObjectId == command.ObjectId && flight.LauncherModuleId == command.ModuleId);
        if (projectile is null) return CommandStartOutcome.Rejected("stale_projectile");
        TerminateTorpedo(projectile, motionTimeMs, TorpedoTerminationKind.SelfDestruct);
        RemoveLostCountermeasures(motionTimeMs);
        RecordCommandResult(command, CommandResultStatus.Executed, motionTimeMs);
        return CommandStartOutcome.Started;
    }

    private void TerminateTorpedo(SpaceObjectRuntime projectile, double motionTimeMs, TorpedoTerminationKind kind)
    {
        var flight = projectile.InitialMotion.Torpedo!;
        var point = CombatPath(projectile).Position(motionTimeMs);
        var final = CloseFlightHistory(projectile, motionTimeMs, flight);
        _objects.Remove(projectile);
        _torpedoTargets.Remove(projectile.InitialMotion.ObjectId);
        var key = (flight.OwnerObjectId, flight.LauncherModuleId);
        if (_launcherCombat.TryGetValue(key, out var launcher))
            _launcherCombat[key] = launcher with { ActiveTorpedoIds = launcher.ActiveTorpedoIds.Remove(projectile.InitialMotion.ObjectId) };
        if (kind == TorpedoTerminationKind.SelfDestruct)
            AppendCombatEvent(new(0, motionTimeMs, CombatEventType.SelfDestruct, flight.OwnerObjectId, flight.TargetObjectId,
                projectile.InitialMotion.ObjectId, point.X, point.Y, TorpedoOperator: flight.RatingBreakdown, Damage: 0));
        if (kind == TorpedoTerminationKind.Expired)
            AppendCombatEvent(new(0, motionTimeMs, CombatEventType.Expired, flight.OwnerObjectId, flight.TargetObjectId,
                projectile.InitialMotion.ObjectId, point.X, point.Y, TorpedoOperator: flight.RatingBreakdown, Damage: 0));
        _combatImpacts.Add(new(checked(++_combatImpactSequence), projectile.InitialMotion.ObjectId,
            flight.OwnerObjectId, flight.LauncherModuleId, flight.TargetObjectId, string.Empty,
            motionTimeMs, point.X, point.Y, final.Trail, TerminationKind: kind));
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
        if (IsDefeated) return;
        // Recompute after each mutation: a previous contact may remove an obstacle
        // (or, in the wreck lifecycle, replace it) inside this same physical interval.
        if (motionTimeMs <= _combatProcessedTimeMs) return;
        double cursor = _combatProcessedTimeMs;
        while (true)
        {
            var impact = FindFirstImpact(cursor, motionTimeMs);
            var launch = NextDefenseLaunch(cursor, motionTimeMs);
            var pendingBurst = NextPendingBurst(motionTimeMs);
            var hostileLaunch = NextHostileLaunch(cursor, motionTimeMs);
            var intercept = FindFirstIntercept(cursor, motionTimeMs);
            var expiry = _objects.Where(o => o.InitialMotion.Countermeasure is not null)
                .OrderBy(o => CountermeasureExpiry(o.InitialMotion.Countermeasure!))
                .ThenBy(o => o.InitialMotion.ObjectId, StringComparer.Ordinal).FirstOrDefault();
            double expiryTime = expiry is null ? double.PositiveInfinity : CountermeasureExpiry(expiry.InitialMotion.Countermeasure!);
            var torpedoExpiry = _objects.Where(o => o.InitialMotion.Torpedo?.ExpiresAtMotionTimeMs is not null)
                .OrderBy(o => o.InitialMotion.Torpedo!.ExpiresAtMotionTimeMs)
                .ThenBy(o => o.InitialMotion.ObjectId, StringComparer.Ordinal).FirstOrDefault();
            double torpedoExpiryTime = torpedoExpiry?.InitialMotion.Torpedo?.ExpiresAtMotionTimeMs ?? double.PositiveInfinity;
            double eventTime = Math.Min(launch?.Time ?? double.PositiveInfinity,
                Math.Min(intercept?.Contact.MotionTimeMs ?? double.PositiveInfinity, expiryTime));
            eventTime = Math.Min(eventTime, hostileLaunch?.Time ?? double.PositiveInfinity);
            eventTime = Math.Min(eventTime, pendingBurst?.Shot.DueMotionTimeMs ?? double.PositiveInfinity);
            eventTime = Math.Min(eventTime, torpedoExpiryTime);
            if (impact is { } hit && hit.Contact.MotionTimeMs <= eventTime)
            {
                ApplyImpact(hit.Projectile, hit.Target, hit.Contact);
                cursor = hit.Contact.MotionTimeMs;
                RemoveLostCountermeasures(cursor);
                if (IsDefeated) return;
                continue;
            }
            if (intercept is { } encounter && encounter.Contact.MotionTimeMs <= eventTime)
            {
                ResolveFirstIntercept(encounter.Projectile, encounter.Target, encounter.Contact);
                cursor = encounter.Contact.MotionTimeMs;
                RemoveLostCountermeasures(cursor);
                continue;
            }
            if (expiry is not null && expiryTime <= eventTime && expiryTime <= motionTimeMs)
            {
                ExpireCountermeasure(expiry, expiryTime);
                cursor = expiryTime;
                continue;
            }
            if (torpedoExpiry is not null && torpedoExpiryTime <= eventTime && torpedoExpiryTime <= motionTimeMs)
            {
                TerminateTorpedo(torpedoExpiry, torpedoExpiryTime, TorpedoTerminationKind.Expired);
                RemoveLostCountermeasures(torpedoExpiryTime);
                cursor = torpedoExpiryTime;
                continue;
            }
            if (pendingBurst is { } burst && burst.Shot.DueMotionTimeMs <= eventTime)
            {
                ProcessPendingBurst(burst.Key, burst.Shot);
                cursor = burst.Shot.DueMotionTimeMs;
                continue;
            }
            if (launch is not null && launch.Time <= eventTime)
            {
                LaunchCountermeasure(launch);
                cursor = launch.Time;
                continue;
            }
            if (hostileLaunch is not null)
            {
                var result = TryStartHostileTorpedoFire(hostileLaunch.OwnerId, hostileLaunch.ModuleId, hostileLaunch.Time);
                if (result.Disposition != CommandStartDisposition.Started)
                    throw new InvalidOperationException($"Validated hostile launch was rejected: {result.ReasonCode}.");
                cursor = hostileLaunch.Time;
                continue;
            }
            break;
        }
        CompleteDefenseReloads(motionTimeMs);
        _combatProcessedTimeMs = motionTimeMs;
        for (int i = 0; i < _objects.Count; i++)
        {
            var obj = _objects[i];
            if (obj.InitialMotion.Countermeasure is { } defenseFlight && motionTimeMs > obj.StartGameTimeMs)
            {
                var predictedDefense = CountermeasureGuidanceMath.Predict(obj.InitialMotion, motionTimeMs - obj.StartGameTimeMs);
                _objects[i] = obj with
                {
                    InitialMotion = predictedDefense with
                    { Countermeasure = predictedDefense.Countermeasure! with { Trail = CloseCountermeasureHistory(obj, motionTimeMs) } },
                    StartGameTimeMs = motionTimeMs
                };
                continue;
            }
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
                // A02: only the object selected at launch can terminate this flight.
                if (target.IsDestroyed || target.InitialMotion.ObjectId != flight.TargetObjectId) continue;
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

    internal static CollisionPath CombatPath(SpaceObjectRuntime obj)
    {
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
        if (obj.InitialMotion.Countermeasure is { } defenseFlight)
            return new(t =>
            {
                var p = TorpedoGuidanceMath.PredictPose(defenseFlight.Route, defenseFlight.Route.ElapsedMs + t - obj.StartGameTimeMs);
                return (p.X, p.Y);
            }, defenseFlight.Route.Segments[^1].SpeedKmS / 100,
                defenseFlight.Route.Segments[^1].SpeedKmS / 100 *
                defenseFlight.Route.Segments.Max(s => Math.Abs(s.AngularVelocityDegPerSec)) * Math.PI / 180000);
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
        if (target.ObjectType is SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip)
        {
            decimal attack = flight.CapturedAttack ?? flight.RatingBreakdown?.EffectiveRating ?? 0;
            decimal defense = TorpedoDefenseRating(target, contact.MotionTimeMs);
            int chance = CombatChanceMath.ChanceTenths(attack, defense, ChanceRules);
            int roll = _torpedoHitRng.NextRoll();
            bool hit = roll <= chance;
            AppendCombatEvent(new(0, contact.MotionTimeMs, CombatEventType.TorpedoHitRoll,
                flight.OwnerObjectId, target.InitialMotion.ObjectId, projectile.InitialMotion.ObjectId,
                contact.X, contact.Y, ChanceTenths: chance, Roll: roll, TorpedoOperator: flight.RatingBreakdown,
                Result: hit ? "hit" : "miss", Attack: attack, Defense: defense));
            if (!hit)
            {
                AppendCombatEvent(new(0, contact.MotionTimeMs, CombatEventType.Miss,
                    flight.OwnerObjectId, target.InitialMotion.ObjectId, projectile.InitialMotion.ObjectId,
                    contact.X, contact.Y, ChanceTenths: chance, Damage: 0));
                TerminateTorpedo(projectile, contact.MotionTimeMs, TorpedoTerminationKind.Miss);
                return;
            }
        }
        var finalFlight = CloseFlightHistory(projectile, contact.MotionTimeMs, flight);
        string? wreckId = null;
        string hitId = target.InitialMotion.ObjectId;
        var moduleHit = ApplyModuleHit(target, flight, contact.MotionTimeMs);
        _objects.Remove(projectile);
        _torpedoTargets.Remove(projectile.InitialMotion.ObjectId);
        var key = (flight.OwnerObjectId, flight.LauncherModuleId);
        if (_launcherCombat.TryGetValue(key, out var launcher))
            _launcherCombat[key] = launcher with { ActiveTorpedoIds = launcher.ActiveTorpedoIds.Remove(projectile.InitialMotion.ObjectId) };
        AppendCombatEvent(new(0, contact.MotionTimeMs, CombatEventType.Hit, flight.OwnerObjectId, hitId,
            projectile.InitialMotion.ObjectId, contact.X, contact.Y, TorpedoOperator: flight.RatingBreakdown,
            Damage: moduleHit is null ? 0 : flight.Damage));
        if (moduleHit is { } moduleDamage)
        {
            AppendCombatEvent(new(0, contact.MotionTimeMs, CombatEventType.ModuleHit, flight.OwnerObjectId, hitId,
                projectile.InitialMotion.ObjectId, contact.X, contact.Y, Damage: flight.Damage,
                HitModuleId: moduleDamage.ModuleId, ArmorBefore: moduleDamage.ArmorBefore,
                ArmorAfter: moduleDamage.ArmorAfter, StructureBefore: moduleDamage.StructureBefore,
                StructureAfter: moduleDamage.StructureAfter));
            if (moduleDamage.StructureAfter == 0)
            {
                AppendCombatEvent(new(0, contact.MotionTimeMs, CombatEventType.ModuleDestroyed, flight.OwnerObjectId, hitId,
                    projectile.InitialMotion.ObjectId, contact.X, contact.Y,
                    HitModuleId: moduleDamage.ModuleId, StructureAfter: 0));
                if (ShipDestroyReason(moduleDamage.UpdatedShip, moduleDamage.ModuleId) is { } reason)
                    wreckId = DestroyShip(hitId, reason, contact.MotionTimeMs,
                        flight.OwnerObjectId, projectile.InitialMotion.ObjectId);
            }
        }
        _combatImpacts.Add(new(checked(++_combatImpactSequence), projectile.InitialMotion.ObjectId,
            flight.OwnerObjectId, flight.LauncherModuleId, flight.TargetObjectId, hitId,
            contact.MotionTimeMs, contact.X, contact.Y, finalFlight.Trail, moduleHit is null ? 0 : flight.Damage,
            wreckId is null ? null : hitId, wreckId));
        EmitCombatContactReport(projectile, target, contact.MotionTimeMs, moduleHit, wreckId is not null);
    }

    private decimal TorpedoDefenseRating(SpaceObjectRuntime target, double motionTimeMs)
    {
        if (target.ObjectType is not (SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip)) return 0;
        var engine = target.Modules.FirstOrDefault(module =>
        {
            var definition = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
            return definition.CombatRole == "engine" || definition.CategoryTypeId == "module.engine";
        });
        if (engine is null) return 0;
        var definition = _registry.ModuleTypes.GetDefinition(engine.ModuleTypeIndex);
        int pilotSkill = target.Crew.FirstOrDefault(c => c.Id == engine.OperatorCrewId)?.ManeuverabilitySkill ?? 0;
        return (CanExecuteEngineCommand(engine, definition, motionTimeMs) ? definition.Maneuverability ?? 0 : 0)
            + pilotSkill;
    }

    private readonly record struct ModuleHitOutcome(SpaceObjectRuntime UpdatedShip, string ModuleId,
        int ArmorBefore, int ArmorAfter, int StructureBefore, int StructureAfter);

    private ModuleHitOutcome? ApplyModuleHit(SpaceObjectRuntime target, TorpedoSnapshot flight, double motionTimeMs)
    {
        int targetIndex = _objects.FindIndex(o => o.InitialMotion.ObjectId == target.InitialMotion.ObjectId);
        if (targetIndex < 0) return null;
        target = _objects[targetIndex];
        if (target.ObjectType is not (SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip) ||
            target.ShipClassId is null) return null;
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

        var result = ModuleDamageMath.Apply(flight.Damage, flight.ArmorPiercingPercent,
            flight.ArmorDamagePercent, selected.Armor, selected.StructurePoints);
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
        _objects[targetIndex] = updatedShip;
        if (selected.ActiveCycle is { } cycle)
        {
            string reason = updated.Destroyed ? CommandReasonCodes.ModuleDestroyed : CommandReasonCodes.ModuleDisabled;
            RecordCommandResultFromCycle(cycle, CommandResultStatus.Cancelled, interruptTime, reason);
            RecordShipEvent(target.InitialMotion.ObjectId, selected.ModuleId,
                ShipEventTypes.CycleInterrupted, reason, interruptTime);
        }
        var defenseKey = (target.InitialMotion.ObjectId, selected.ModuleId);
        if (_defenses.TryGetValue(defenseKey, out var defense))
            _defenses[defenseKey] = defense with { PendingBurstShots = [] };
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
        string? actorObjectId = null, string? projectileObjectId = null)
    {
        var destroyed = _objects.First(o => o.InitialMotion.ObjectId == objectId);
        string id;
        do
        {
            id = "wreck-" + checked(++_wreckSequence).ToString(System.Globalization.CultureInfo.InvariantCulture);
        } while (_objects.Any(o => string.Equals(o.InitialMotion.ObjectId, id, StringComparison.OrdinalIgnoreCase)));
        var point = CombatPath(destroyed).Position(impactMotionTimeMs);
        _objects.Remove(destroyed);
        _hostileCombat.Remove(objectId);
        foreach (var defenseKey in _defenses.Keys.Where(k => k.ObjectId == objectId).ToArray()) _defenses.Remove(defenseKey);
        foreach (var key in _launcherCombat.Keys.Where(k => k.ObjectId == objectId).ToArray())
            _launcherCombat.Remove(key);
        _objects.Add(new(new(id, point.X, point.Y, 0, 0), SpaceObjectType.Wreck,
            (long)Math.Floor(impactMotionTimeMs), [], Name: "Wreck", IsKnown: destroyed.IsKnown));
        if (SelectedObjectId == objectId) SelectedObjectId = null;
        if (ActiveObjectId == objectId) ActiveObjectId = null;
        ClearDestroyedTargetNavigation(objectId, impactMotionTimeMs);
        AppendCombatEvent(new(0, impactMotionTimeMs, CombatEventType.ShipDestroyed,
            actorObjectId ?? objectId, objectId, projectileObjectId ?? string.Empty,
            point.X, point.Y, Result: id, DestroyReason: cause));
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
        if (history.Count > 0)
            from = Math.Max(from, history[^1].StartMotionTimeMs + history[^1].Segment.DurationMs - flight.Route.StartMotionTimeMs);
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
                if (definition.CountermeasureAccuracy is not null)
                    _ = ResolveWeaponOperator(obj, module, WeaponSkillType.CountermeasureDefense);
                if (definition.TorpedoDamage is not { } damage)
                    continue;
                if (obj.ObjectType is not (SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip))
                    throw new ScenarioException($"Launcher '{module.ModuleId}' must be installed on a ship.");
                launchers.Add((obj.InitialMotion.ObjectId, module.ModuleId), new LauncherCombatSnapshot(
                    SpeedKmS: definition.TorpedoSpeedKmS!.Value,
                    TurnRateDegPerSec: definition.TorpedoTurnRateDegPerSec!.Value,
                    Damage: damage,
                    Operator: ResolveWeaponOperator(obj, module, WeaponSkillType.TorpedoAttack),
                    RangeKm: definition.TorpedoRangeKm,
                    Maneuverability: definition.TorpedoManeuverability,
                    ActiveTorpedoIds: []));
            }
        }

        return launchers;
    }
}
