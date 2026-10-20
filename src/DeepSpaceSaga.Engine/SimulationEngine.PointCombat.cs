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

    private CommandStartOutcome TryStartPointLaunch(PlayerCommand command, long motionTimeMs)
    {
        var owner = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == command.ObjectId &&
            o.InitialMotion.ObjectId == PlayerShipObjectId && o.ObjectType == SpaceObjectType.PlayerShip && !o.IsDestroyed);
        if (owner is null) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownObject);
        var result = TryLaunchPointProjectile(owner, command.ModuleId, command.TargetWorldX, command.TargetWorldY,
            command.ProfileId, motionTimeMs, ProjectileKind.Torpedo);
        if (result.Disposition == CommandStartDisposition.Started)
        {
            _processedLaunchCommandIds.Add(command.CommandId);
            RecordCommandResult(command, CommandResultStatus.Executed, motionTimeMs);
        }
        return result;
    }

    internal CommandStartOutcome TryStartHostilePointLaunch(string ownerId, string moduleId,
        double x, double y, string profileId, long motionTimeMs)
    {
        lock (_worldStateLock)
        {
            var owner = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == ownerId &&
                o.ObjectType == SpaceObjectType.NpcShip && !o.IsDestroyed);
            return owner is null ? CommandStartOutcome.Rejected(CommandReasonCodes.UnknownObject) :
                TryLaunchPointProjectile(owner, moduleId, x, y, profileId, motionTimeMs, ProjectileKind.Torpedo);
        }
    }

    private CommandStartOutcome TryLaunchPointProjectile(SpaceObjectRuntime owner, string moduleId,
        double? targetX, double? targetY, string? profileId, long motionTimeMs, ProjectileKind kind)
    {
        int index = FindModuleIndex(owner.Modules, moduleId);
        if (index < 0) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownModule);
        var module = owner.Modules[index];
        var type = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
        if (kind != ProjectileKind.Torpedo || type.CategoryTypeId != "module.torpedo.launcher" ||
            !type.IsPointLauncher || !type.CommandTypeIds.Contains(CombatCommandTypes.Launch))
            return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownCommandType);
        var key = (owner.InitialMotion.ObjectId, moduleId);
        if (!_launcherCombat.TryGetValue(key, out var launcher))
            return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownModule);
        if (!CanExecuteModuleCommand(module, motionTimeMs))
            return CommandStartOutcome.Rejected(ModuleCombatUnavailableReason(module, motionTimeMs) ?? CommandReasonCodes.ModuleUnavailable);
        if (module.ActiveCycle is not null) return CommandStartOutcome.Rejected(CommandReasonCodes.Busy);
        if (launcher.ReadyAtMotionTimeMs is { } ready && motionTimeMs < ready)
            return CommandStartOutcome.Rejected(CommandReasonCodes.LauncherReloading);
        if (string.IsNullOrWhiteSpace(profileId)) return CommandStartOutcome.Rejected(CommandReasonCodes.MissingProfile);
        var profile = type.Profiles.FirstOrDefault(p => p.ProfileId == profileId);
        if (profile is null) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownProfile);
        if (targetX is not { } x || targetY is not { } y || !double.IsFinite(x) || !double.IsFinite(y))
            return CommandStartOutcome.Rejected(CommandReasonCodes.InvalidTargetCoordinates);
        var weaponOperator = ResolveWeaponOperator(owner, module, WeaponSkillType.TorpedoAttack);
        if (weaponOperator is null) return CommandStartOutcome.Rejected(CommandReasonCodes.NoWeaponOperator);
        var origin = PredictMotion(owner, Math.Max(0, motionTimeMs - owner.StartGameTimeMs));
        double dx = (x - origin.X) / WorldUnitsPerKm, dy = (y - origin.Y) / WorldUnitsPerKm;
        double range = Math.Sqrt(dx * dx + dy * dy);
        if (range > type.RangeKm!.Value) return CommandStartOutcome.Rejected(CommandReasonCodes.LaunchOutOfRange);
        if (motionTimeMs < 0 || motionTimeMs > long.MaxValue - type.ReloadMs!.Value)
            return CommandStartOutcome.Rejected(CommandReasonCodes.InvalidTime);
        if (_registry.CombatRules?.Scatter is not { } rules)
            return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownCommandType);
        if (!TryGetNextTorpedoId(out string id, out long sequence))
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
            profile.ProfileId, profile.BlastRadiusKm, type.TorpedoDamage!.Value,
            type.ArmorPiercingPercent!.Value, type.ArmorDamagePercent ?? 100);
        var motion = new ObjectMotionSnapshot(id, origin.X, origin.Y, profile.SpeedKmS, direction, OwnProjectile: flight);
        _objects.Add(new(motion, SpaceObjectType.Missile, motionTimeMs, [], Name: "Torpedo", IsKnown: true));
        var active = launcher.ActiveTorpedoIds.IsDefault ? [] : launcher.ActiveTorpedoIds;
        _launcherCombat[key] = launcher with
        {
            ActiveTorpedoIds = active.Add(id),
            ReadyAtMotionTimeMs = motionTimeMs + (double)type.ReloadMs.Value,
            Operator = weaponOperator
        };
        _torpedoSequence = sequence;
        _launchScatterRng = random;
        AppendCombatEvent(new(0, motionTimeMs, CombatEventType.Launch, owner.InitialMotion.ObjectId, string.Empty, id,
            origin.X, origin.Y, Result: "torpedo", ProjectileKind: kind, ProfileId: profile.ProfileId, BlastRadiusKm: profile.BlastRadiusKm));
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
}
