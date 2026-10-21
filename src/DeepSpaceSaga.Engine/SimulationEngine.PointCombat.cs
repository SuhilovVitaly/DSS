using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Combat;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private LaunchScatterRng _launchScatterRng = new(0);

    internal (ulong State, ulong Counter) CaptureLaunchScatterState()
    {
        lock (_worldStateLock) return (_launchScatterRng.State, _launchScatterRng.Counter);
    }

    private CommandStartOutcome TryStartPointLaunch(PlayerCommand command, long motionTimeMs, ProjectileKind kind = ProjectileKind.Torpedo)
    {
        var owner = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == command.ObjectId &&
            o.InitialMotion.ObjectId == PlayerShipObjectId && o.ObjectType == SpaceObjectType.PlayerShip && !o.IsDestroyed);
        if (owner is null) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownObject);
        // A durable successful launch identity outlives the bounded receipt window.
        if (_processedLaunchCommandIds.Contains(command.CommandId))
        {
            RecordCommandResult(command, CommandResultStatus.Executed, motionTimeMs);
            return CommandStartOutcome.Started;
        }
        var result = TryLaunchPointProjectile(owner, command.ModuleId, command.TargetWorldX, command.TargetWorldY,
            command.ProfileId, motionTimeMs, kind);
        if (result.Disposition == CommandStartDisposition.Started)
        {
            _processedLaunchCommandIds.Add(command.CommandId);
            RecordCommandResult(command, CommandResultStatus.Executed, motionTimeMs);
        }
        return result;
    }

    internal CommandStartOutcome TryStartHostilePointLaunch(string ownerId, string moduleId,
        double x, double y, string profileId, long motionTimeMs, ProjectileKind kind = ProjectileKind.Torpedo)
    {
        lock (_worldStateLock)
        {
            var owner = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == ownerId &&
                o.ObjectType == SpaceObjectType.NpcShip && !o.IsDestroyed);
            return owner is null ? CommandStartOutcome.Rejected(CommandReasonCodes.UnknownObject) :
                TryLaunchPointProjectile(owner, moduleId, x, y, profileId, motionTimeMs, kind);
        }
    }

    private CommandStartOutcome TryLaunchPointProjectile(SpaceObjectRuntime owner, string moduleId,
        double? targetX, double? targetY, string? profileId, long motionTimeMs, ProjectileKind kind)
    {
        int index = FindModuleIndex(owner.Modules, moduleId);
        if (index < 0) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownModule);
        var module = owner.Modules[index];
        var type = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
        bool torpedo = kind == ProjectileKind.Torpedo;
        string category = torpedo ? "module.torpedo.launcher" : "module.countermeasure.launcher";
        string commandType = torpedo ? CombatCommandTypes.Launch : DefenseCommandTypes.Launch;
        if (type.CategoryTypeId != category || !type.IsPointLauncher || !type.CommandTypeIds.Contains(commandType))
            return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownCommandType);
        var key = (owner.InitialMotion.ObjectId, moduleId);
        _launcherCombat.TryGetValue(key, out var launcher);
        _defenses.TryGetValue(key, out var defense);
        if (torpedo ? launcher is null : defense is null)
            return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownModule);
        if (!CanExecuteModuleCommand(module, motionTimeMs))
            return CommandStartOutcome.Rejected(ModuleCombatUnavailableReason(module, motionTimeMs) ?? CommandReasonCodes.ModuleUnavailable);
        if (module.ActiveCycle is not null) return CommandStartOutcome.Rejected(CommandReasonCodes.Busy);
        double? readyAt = torpedo ? launcher!.ReadyAtMotionTimeMs : defense!.ReadyAtMotionTimeMs ?? defense.ReloadDueMotionTimeMs;
        if (readyAt is { } ready && motionTimeMs < ready)
            return CommandStartOutcome.Rejected(CommandReasonCodes.LauncherReloading);
        if (string.IsNullOrWhiteSpace(profileId)) return CommandStartOutcome.Rejected(CommandReasonCodes.MissingProfile);
        var profile = type.Profiles.FirstOrDefault(p => p.ProfileId == profileId);
        if (profile is null) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownProfile);
        if (targetX is not { } x || targetY is not { } y || !double.IsFinite(x) || !double.IsFinite(y))
            return CommandStartOutcome.Rejected(CommandReasonCodes.InvalidTargetCoordinates);
        var weaponOperator = ResolveWeaponOperator(owner, module, torpedo ? WeaponSkillType.TorpedoAttack : WeaponSkillType.CountermeasureDefense);
        if (weaponOperator is null) return CommandStartOutcome.Rejected(CommandReasonCodes.NoWeaponOperator);
        var origin = PredictMotion(owner, Math.Max(0, motionTimeMs - owner.StartGameTimeMs));
        double dx = (x - origin.X) / WorldUnitsPerKm, dy = (y - origin.Y) / WorldUnitsPerKm;
        double range = Math.Sqrt(dx * dx + dy * dy);
        if (range > type.RangeKm!.Value) return CommandStartOutcome.Rejected(CommandReasonCodes.LaunchOutOfRange);
        if (motionTimeMs < 0 || motionTimeMs > long.MaxValue - type.ReloadMs!.Value)
            return CommandStartOutcome.Rejected(CommandReasonCodes.InvalidTime);
        if (_registry.CombatRules?.Scatter is not { } rules)
            return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownCommandType);
        bool allocated = torpedo ? TryGetNextTorpedoId(out string id, out long sequence) : TryGetNextCountermeasureId(out id, out sequence);
        if (!allocated)
            return CommandStartOutcome.Rejected(CommandReasonCodes.ProjectileIdExhausted);

        // Stage the draw and geometry. Any numeric rejection leaves RNG, sequence and reload untouched.
        var random = new LaunchScatterRng(_launchScatterRng.State, _launchScatterRng.Counter);
        var normal = random.NextPair();
        double sigma = BlastMath.ScatterSigma(range, type.ScatterPercent!.Value, weaponOperator.Skill,
            rules.SkillPivot, rules.SkillSlope, rules.MinMultiplier);
        double endpointX = x + normal.X * sigma * WorldUnitsPerKm;
        double endpointY = y + normal.Y * sigma * WorldUnitsPerKm;
        double finalDx = (endpointX - origin.X) / WorldUnitsPerKm, finalDy = (endpointY - origin.Y) / WorldUnitsPerKm;
        double distance = Math.Sqrt(finalDx * finalDx + finalDy * finalDy);
        if (!double.IsFinite(endpointX) || !double.IsFinite(endpointY) || !double.IsFinite(distance))
            return CommandStartOutcome.Rejected(CommandReasonCodes.InvalidTargetCoordinates);
        double deadline = BlastMath.DetonationTimeMs(motionTimeMs, distance, profile.SpeedKmS);
        if (deadline > long.MaxValue) return CommandStartOutcome.Rejected(CommandReasonCodes.InvalidTime);
        double direction = (Math.Atan2(finalDx, -finalDy) * 180 / Math.PI + 360) % 360;
        var flight = new OwnProjectileSnapshot(kind, owner.InitialMotion.ObjectId, moduleId, origin.X, origin.Y,
            origin.X, origin.Y, endpointX, endpointY, motionTimeMs, deadline, profile.SpeedKmS,
            profile.ProfileId, profile.BlastRadiusKm, torpedo ? type.TorpedoDamage!.Value : 0,
            torpedo ? type.ArmorPiercingPercent!.Value : 0, type.ArmorDamagePercent ?? 100, _registry.CombatRules.Blast!.CoreFraction,
            owner.InitialMotion.ObjectId == PlayerShipObjectId ? CaptureReportIdentity(owner, moduleId,
                torpedo ? BattleReportReporterRole.TorpedoOperator : BattleReportReporterRole.CountermeasureOperator) : null,
            GetRelationToPlayer(owner));
        var motion = new ObjectMotionSnapshot(id, origin.X, origin.Y, profile.SpeedKmS, direction, OwnProjectile: flight);
        _objects.Add(new(motion, torpedo ? SpaceObjectType.Missile : SpaceObjectType.Countermeasure,
            motionTimeMs, [], Name: torpedo ? "Torpedo" : "Countermissile", IsKnown: true));
        if (torpedo)
        {
            var active = launcher!.ActiveTorpedoIds.IsDefault ? [] : launcher.ActiveTorpedoIds;
            _launcherCombat[key] = launcher with
            {
                ActiveTorpedoIds = active.Add(id),
                ReadyAtMotionTimeMs = motionTimeMs + (double)type.ReloadMs.Value,
                Operator = weaponOperator
            };
            _torpedoSequence = sequence;
        }
        else
        {
            var active = defense!.ActiveProjectileIds ?? [];
            active = active.Add(id);
            _defenses[key] = defense with
            {
                AutoEnabled = false,
                State = type.ReloadMs.Value > 0 ? DefenseState.Reloading : DefenseState.Ready,
                ActiveProjectileId = active[0],
                ActiveProjectileIds = active,
                PendingBurstShots = [],
                ReadyAtMotionTimeMs = motionTimeMs + (double)type.ReloadMs.Value,
                ReloadDueMotionTimeMs = type.ReloadMs.Value > 0 ? motionTimeMs + (double)type.ReloadMs.Value : null,
                Operator = weaponOperator
            };
            _countermeasureSequence = sequence;
        }
        _launchScatterRng = random;
        AppendCombatEvent(new(0, motionTimeMs, CombatEventType.Launch, owner.InitialMotion.ObjectId, string.Empty, id,
            origin.X, origin.Y, Result: torpedo ? "torpedo" : "countermissile", ProjectileKind: kind, ProfileId: profile.ProfileId, BlastRadiusKm: profile.BlastRadiusKm));
        ObserveNpcDodgeThreats(motionTimeMs);
        return CommandStartOutcome.Started;
    }

    private ObservedProjectileSnapshot? ProjectObservedProjectile(ObjectMotionSnapshot motion)
    {
        if (motion.OwnProjectile is not { } flight) return motion.ObservedProjectile;
        if (flight.OwnerObjectId == PlayerShipObjectId) return null;
        double dx = flight.DetonationX - flight.StartX, dy = flight.DetonationY - flight.StartY;
        double length = Math.Sqrt(dx * dx + dy * dy);
        return new(flight.Kind, motion.X, motion.Y, length == 0 ? 0 : dx / length,
            length == 0 ? -1 : dy / length, flight.SpeedKmS, flight.ProfileId, flight.BlastRadiusKm);
    }

    private EnemyShipStatusSnapshot? ProjectPointEnemyStatus(SpaceObjectRuntime obj, long motionTimeMs)
    {
        if (_registry.CombatRules?.IsPointAim != true || !obj.IsKnown || obj.InitialMotion.ObjectId == PlayerShipObjectId) return null;
        if (obj.ObjectType == SpaceObjectType.Wreck)
            return GetRelationToPlayer(obj) == PlayerRelation.Enemy ? new(true, null, true) : null;
        if (obj.ObjectType != SpaceObjectType.NpcShip) return null;
        bool damaged = obj.Modules.Any(m => m.Armor < m.ArmorMax || m.StructurePoints < _registry.ModuleTypes.GetDefinition(m.ModuleTypeIndex).StructurePointsMax);
        double until = obj.Modules.Where(m => _registry.ModuleTypes.GetDefinition(m.ModuleTypeIndex).CategoryTypeId == "module.engine")
            .Select(m => m.DisabledUntilMotionTimeMs ?? 0).DefaultIfEmpty(0).Max();
        long? immobilized = until > motionTimeMs ? until >= long.MaxValue ? long.MaxValue : (long)Math.Ceiling(until) : null;
        return new(damaged, immobilized, obj.IsDestroyed);
    }

    private double NextPointDetonationTime() => _objects
        .Where(o => o.InitialMotion.OwnProjectile is not null)
        .Select(o => o.InitialMotion.OwnProjectile!.DetonationMotionTimeMs)
        .DefaultIfEmpty(double.PositiveInfinity).Min();

    private CommandStartOutcome TryStartPointDetonation(PlayerCommand command, long motionTimeMs)
    {
        if (command.ObjectId != PlayerShipObjectId) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownObject);
        var projectile = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == command.TargetObjectId && !o.IsDestroyed);
        if (projectile is null) return CommandStartOutcome.Rejected(CommandReasonCodes.StaleProjectile);
        if (projectile.InitialMotion.OwnProjectile is not { } flight || flight.OwnerObjectId != command.ObjectId ||
            flight.LauncherModuleId != command.ModuleId)
            return CommandStartOutcome.Rejected(CommandReasonCodes.NotOwnProjectile);
        if (flight.Kind != ProjectileKind.Torpedo) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownCommandType);
        var owner = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == command.ObjectId && !o.IsDestroyed);
        var module = owner?.Modules.FirstOrDefault(m => m.ModuleId == command.ModuleId);
        if (module is null) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownModule);
        if (!_registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex).CommandTypeIds.Contains(CombatCommandTypes.Detonate))
            return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownCommandType);
        ResolvePointDetonations(motionTimeMs, projectile);
        RecordCommandResult(command, CommandResultStatus.Executed, motionTimeMs);
        return CommandStartOutcome.Started;
    }

    private void ResolvePointDetonations(double motionTimeMs, SpaceObjectRuntime? manualProjectile = null)
    {
        // All simultaneous blasts observe the same physical ship positions, even if a preceding hit interrupts an engine.
        var shipPositions = _objects.Where(o => !o.IsDestroyed && o.ObjectType is SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip)
            .ToDictionary(o => o.InitialMotion.ObjectId, o => CombatPath(o).Position(motionTimeMs), StringComparer.Ordinal);
        var deaths = new Dictionary<string, (string Cause, string Owner, string Projectile)>(StringComparer.Ordinal);
        var projectiles = manualProjectile is not null ? [manualProjectile] :
            _objects.Where(o => o.InitialMotion.OwnProjectile is { } point && point.DetonationMotionTimeMs == motionTimeMs)
                .OrderBy(o => o.InitialMotion.OwnProjectile!.Kind == ProjectileKind.Countermissile ? 0 : 1)
                .ThenBy(o => o.InitialMotion.ObjectId, StringComparer.Ordinal).ToArray();
        foreach (var projectile in projectiles)
        {
            if (!_objects.Any(o => o.InitialMotion.ObjectId == projectile.InitialMotion.ObjectId)) continue;
            var flight = projectile.InitialMotion.OwnProjectile!;
            var position = CombatPath(projectile).Position(motionTimeMs);
            if (flight.Kind == ProjectileKind.Countermissile)
            {
                ResolveCountermissileBlast(projectile, motionTimeMs, position);
                continue;
            }
            AppendCombatEvent(new(0, motionTimeMs, manualProjectile is null ? CombatEventType.TorpedoDetonated : CombatEventType.ManuallyDetonated, flight.OwnerObjectId,
                string.Empty, projectile.InitialMotion.ObjectId, position.X, position.Y,
                ProjectileKind: flight.Kind, ProfileId: flight.ProfileId, BlastRadiusKm: flight.BlastRadiusKm));
            bool anyHit = false;
            foreach (var target in _objects.Where(o => !o.IsDestroyed && o.ObjectType is SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip)
                         .OrderBy(o => o.InitialMotion.ObjectId, StringComparer.Ordinal).ToArray())
            {
                var ship = shipPositions[target.InitialMotion.ObjectId];
                double dx = (ship.X - position.X) / WorldUnitsPerKm, dy = (ship.Y - position.Y) / WorldUnitsPerKm;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                double k = BlastMath.Falloff(distance, flight.BlastRadiusKm, flight.CoreFraction);
                int damage = (int)Math.Round(flight.Damage * k, MidpointRounding.AwayFromZero);
                if (damage == 0) continue;
                var hit = ApplyModuleHit(target, damage, flight.ArmorPiercingPercent, flight.ArmorDamagePercent, motionTimeMs, requireShipClass: false);
                if (hit is not { } result) continue;
                anyHit = true;
                bool player = target.InitialMotion.ObjectId == PlayerShipObjectId;
                var outcome = k == 1 ? ProjectileHitResult.Core : ProjectileHitResult.Edge;
                AppendCombatEvent(new(0, motionTimeMs, CombatEventType.Hit, flight.OwnerObjectId, target.InitialMotion.ObjectId,
                    projectile.InitialMotion.ObjectId, position.X, position.Y, Damage: player ? damage : null,
                    ProjectileKind: flight.Kind, ProfileId: flight.ProfileId, BlastRadiusKm: flight.BlastRadiusKm, HitResult: outcome));
                if (player)
                    AppendCombatEvent(new(0, motionTimeMs, CombatEventType.ModuleHit, flight.OwnerObjectId, target.InitialMotion.ObjectId,
                        projectile.InitialMotion.ObjectId, position.X, position.Y, Damage: damage, HitModuleId: result.ModuleId,
                        ArmorBefore: result.ArmorBefore, ArmorAfter: result.ArmorAfter,
                        StructureBefore: result.StructureBefore, StructureAfter: result.StructureAfter));
                bool fatal = result.StructureAfter == 0 && ShipDestroyReason(result.UpdatedShip, result.ModuleId) is { } cause &&
                    deaths.TryAdd(target.InitialMotion.ObjectId, (cause, flight.OwnerObjectId, projectile.InitialMotion.ObjectId));
                EmitPointContactReport(projectile, target, motionTimeMs, result, outcome, fatal);
            }
            if (!anyHit) AppendPointMiss(projectile, motionTimeMs, position);
            RemovePointFlight(projectile, motionTimeMs);
        }
        foreach (var (id, death) in deaths.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            DestroyShip(id, death.Cause, motionTimeMs, death.Owner, death.Projectile, shipPositions[id]);
        EvaluateEncounter((long)Math.Ceiling(motionTimeMs));
    }

    private void ResolveCountermissileBlast(SpaceObjectRuntime projectile, double motionTimeMs, (double X, double Y) centre)
    {
        var flight = projectile.InitialMotion.OwnProjectile!;
        var victims = _objects.Where(o => o.InitialMotion.ObjectId != projectile.InitialMotion.ObjectId && !o.IsDestroyed &&
                o.ObjectType is SpaceObjectType.Missile or SpaceObjectType.Countermeasure)
            .Where(o =>
            {
                var p = CombatPath(o).Position(motionTimeMs);
                double dx = (p.X - centre.X) / WorldUnitsPerKm, dy = (p.Y - centre.Y) / WorldUnitsPerKm;
                return Math.Sqrt(dx * dx + dy * dy) <= flight.BlastRadiusKm;
            }).OrderBy(o => o.InitialMotion.ObjectId, StringComparer.Ordinal).ToArray();
        AppendCombatEvent(new(0, motionTimeMs, CombatEventType.CountermissileDetonated, flight.OwnerObjectId,
            string.Empty, projectile.InitialMotion.ObjectId, centre.X, centre.Y,
            ProjectileKind: flight.Kind, ProfileId: flight.ProfileId, BlastRadiusKm: flight.BlastRadiusKm,
            DestroyedProjectileIds: victims.Select(o => o.InitialMotion.ObjectId).ToImmutableArray()));
        if (victims.Length == 0) AppendPointMiss(projectile, motionTimeMs, centre);
        foreach (var victim in victims)
        {
            var point = CombatPath(victim).Position(motionTimeMs);
            AppendCombatEvent(new(0, motionTimeMs, CombatEventType.ShotDown, flight.OwnerObjectId,
                victim.InitialMotion.ObjectId, victim.InitialMotion.ObjectId, point.X, point.Y, Result: projectile.InitialMotion.ObjectId,
                ProjectileKind: victim.InitialMotion.OwnProjectile?.Kind ??
                    (victim.ObjectType == SpaceObjectType.Missile ? ProjectileKind.Torpedo : ProjectileKind.Countermissile),
                ProfileId: victim.InitialMotion.OwnProjectile?.ProfileId,
                BlastRadiusKm: victim.InitialMotion.OwnProjectile?.BlastRadiusKm, HitResult: ProjectileHitResult.ShotDown));
            RemovePointFlight(victim, motionTimeMs);
        }
        RemovePointFlight(projectile, motionTimeMs);
    }

    private void AppendPointMiss(SpaceObjectRuntime projectile, double time, (double X, double Y) position)
    {
        var flight = projectile.InitialMotion.OwnProjectile!;
        AppendCombatEvent(new(0, time, CombatEventType.Miss, flight.OwnerObjectId, string.Empty,
            projectile.InitialMotion.ObjectId, position.X, position.Y, ProjectileKind: flight.Kind,
            ProfileId: flight.ProfileId, BlastRadiusKm: flight.BlastRadiusKm, HitResult: ProjectileHitResult.Miss));
    }

    private void RemovePointFlight(SpaceObjectRuntime projectile, double motionTimeMs)
    {
        string id = projectile.InitialMotion.ObjectId;
        _objects.RemoveAll(o => o.InitialMotion.ObjectId == id);
        var flight = projectile.InitialMotion.OwnProjectile ?? throw new InvalidOperationException("Missing captured point flight.");
        string owner = flight.OwnerObjectId, module = flight.LauncherModuleId;
        var key = (owner, module);
        if (_launcherCombat.TryGetValue(key, out var launcher))
            _launcherCombat[key] = launcher with { ActiveTorpedoIds = launcher.ActiveTorpedoIds.Remove(id) };
        if (!_defenses.TryGetValue(key, out var defense)) return;
        var active = (defense.ActiveProjectileIds ?? (defense.ActiveProjectileId is { } first ? [first] : [])).Remove(id);
        double ready = defense.ReadyAtMotionTimeMs ?? defense.ReloadDueMotionTimeMs ?? motionTimeMs;
        var state = ready > motionTimeMs ? DefenseState.Reloading : defense.Operator is null ? DefenseState.NoOperator : DefenseState.Ready;
        _defenses[key] = defense with
        {
            ActiveProjectileId = active.IsEmpty ? null : active[0],
            ActiveProjectileIds = active,
            State = state,
            ReloadDueMotionTimeMs = ready > motionTimeMs ? ready : null
        };
    }
}
