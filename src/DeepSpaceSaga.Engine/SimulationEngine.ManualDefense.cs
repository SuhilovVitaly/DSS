using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Combat;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private sealed record ManualDefenseContext(SpaceObjectRuntime Owner, InstalledModuleRuntime Module,
        SpaceObjectRuntime Target, DefenseSnapshot Defense, WeaponOperatorSnapshot Operator);

    private CommandStartOutcome TryManualDefenseFire(PlayerCommand command, long time)
    {
        if (command.ObjectId != PlayerShipObjectId) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownObject);
        if (_processedLaunchCommandIds.Contains(command.CommandId))
        {
            RecordCommandResult(command, CommandResultStatus.Executed, time);
            return CommandStartOutcome.Started;
        }
        string? reason = ValidateManualDefense(command.ObjectId, command.ModuleId, command.TargetObjectId, time, out var context);
        if (reason is not null) return CommandStartOutcome.Rejected(reason);
        var c = context!;
        var type = _registry.ModuleTypes.GetDefinition(c.Module.ModuleTypeIndex);
        long lifetime = type.CountermeasureMaxFlightTimeMs!.Value;
        if (time > long.MaxValue - lifetime) return CommandStartOutcome.Rejected("invalid_time");
        if (!TryGetNextCountermeasureId(out _, out _)) return CommandStartOutcome.Rejected("projectile_id_exhausted");
        var torpedo = c.Target.InitialMotion.Torpedo!;
        var origin = PredictMotion(c.Owner, time - c.Owner.StartGameTimeMs);
        var plan = CountermeasureGuidanceMath.PlanManual(origin, torpedo.Route, time, lifetime,
            type.CountermeasureSpeedKmS!.Value, type.CountermeasureTurnRateDegPerSec!.Value,
            FirstPredictedTorpedoContact(c.Target, time, time + lifetime));
        int chance = CombatChanceMath.ChanceTenths(c.Operator.EffectiveRating,
            torpedo.CapturedManeuverability ?? torpedo.TorpedoRating!.Value, ChanceRules);
        var launch = new DefenseLaunch(c.Owner, c.Module, c.Target, plan.Route, time,
            double.PositiveInfinity, chance, c.Operator);
        if (!LaunchCountermeasure(launch, LaunchMode.Manual, plan.PredictedEncounterMotionTimeMs))
            return CommandStartOutcome.Rejected("projectile_id_exhausted");
        _processedLaunchCommandIds.Add(command.CommandId);
        RecordCommandResult(command, CommandResultStatus.Executed, time);
        return CommandStartOutcome.Started;
    }

    private string? ValidateManualDefense(string ownerId, string moduleId, string? targetId, long time,
        out ManualDefenseContext? context)
    {
        context = null;
        var owner = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == ownerId && ownerId == PlayerShipObjectId && !o.IsDestroyed);
        if (owner is null) return CommandReasonCodes.UnknownObject;
        var module = owner.Modules.FirstOrDefault(m => m.ModuleId == moduleId);
        if (module is null || !_defenses.TryGetValue((ownerId, moduleId), out var defense)) return CommandReasonCodes.UnknownModule;
        if (!CanExecuteModuleCommand(module, time))
            return ModuleCombatUnavailableReason(module, time) ?? CommandReasonCodes.ModuleUnavailable;
        if (module.ActiveCycle is not null || defense.ActiveProjectileId is not null ||
            defense.State == DefenseState.Guiding || defense.ReloadDueMotionTimeMs > time) return CommandReasonCodes.Busy;
        var op = ResolveWeaponOperator(owner, module, WeaponSkillType.CountermeasureDefense);
        if (op is null) return "no_weapon_operator";
        if (string.IsNullOrWhiteSpace(targetId)) return CommandReasonCodes.MissingTarget;
        var target = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == targetId && !o.IsDestroyed && o.InitialMotion.Torpedo is not null);
        if (target is null) return CommandReasonCodes.UnknownTarget;
        var flight = target.InitialMotion.Torpedo!;
        var shooter = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == flight.OwnerObjectId && !o.IsDestroyed);
        string? relation = shooter is null ? flight.OwnerPlayerRelationAtLaunch : GetRelationToPlayer(shooter);
        if (flight.OwnerObjectId == ownerId || relation != PlayerRelation.Enemy) return "target_not_hostile";
        if (defense.ManualRangeKm is not { } range || !CombatRangeMath.IsWithinRange(
            PredictMotion(owner, time - owner.StartGameTimeMs), PredictMotion(target, time - target.StartGameTimeMs), range))
            return "target_out_of_range";
        context = new(owner, module, target, defense, op);
        return null;
    }

    private double? FirstPredictedTorpedoContact(SpaceObjectRuntime projectile, long time, long until)
    {
        var flight = projectile.InitialMotion.Torpedo!;
        double? first = null;
        foreach (var target in _objects)
        {
            if (target.IsDestroyed || target.ObjectType == SpaceObjectType.Countermeasure ||
                target.InitialMotion.ObjectId == projectile.InitialMotion.ObjectId || target.InitialMotion.ObjectId == flight.OwnerObjectId) continue;
            var hit = TorpedoCollisionMath.FirstContact(CombatPath(projectile), CombatPath(target), time, until);
            if (hit is { } contact && (first is null || contact.MotionTimeMs < first)) first = contact.MotionTimeMs;
        }
        return first;
    }

    private bool TryGetNextCountermeasureId(out string id, out long sequence)
    {
        sequence = _countermeasureSequence;
        id = string.Empty;
        while (sequence < long.MaxValue)
        {
            string candidate = "countermeasure-" + (++sequence).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (_objects.Any(o => string.Equals(o.InitialMotion.ObjectId, candidate, StringComparison.OrdinalIgnoreCase))) continue;
            id = candidate;
            return true;
        }
        return false;
    }
}
