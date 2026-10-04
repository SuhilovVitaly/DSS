using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Combat;
using DeepSpaceSaga.Motion;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private readonly Dictionary<string, long> _countermeasureTargetPlans = new(StringComparer.Ordinal);
    private long _countermeasureSequence;
    private long _combatProcessedTimeMs;
    private CountermeasureRng _countermeasureRng = new(0);
    private bool HasDefenseActivity => _objects.Any(o => o.InitialMotion.Countermeasure is not null) ||
        _defenses.Values.Any(d => d.State == DefenseState.Reloading);

    private CommandStartOutcome TrySetDefense(PlayerCommand command, long motionTimeMs)
    {
        if (command.ObjectId != PlayerShipObjectId) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownObject);
        var key = (command.ObjectId, command.ModuleId);
        if (!_defenses.TryGetValue(key, out var defense)) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownModule);
        _defenses[key] = defense with { AutoEnabled = command.CommandType == DefenseCommandTypes.Enable };
        RecordCommandResult(command, CommandResultStatus.Executed, motionTimeMs);
        return CommandStartOutcome.Started;
    }

    private sealed record DefenseLaunch(SpaceObjectRuntime Owner, InstalledModuleRuntime Module,
        SpaceObjectRuntime Target, TorpedoRoute Route, long Time, double HullContact, int Chance, WeaponOperatorSnapshot Operator);

    private DefenseLaunch? NextDefenseLaunch(double from, long to)
    {
        DefenseLaunch? first = null;
        foreach (var owner in _objects)
            foreach (var module in owner.Modules)
            {
                if (!_defenses.TryGetValue((owner.InitialMotion.ObjectId, module.ModuleId), out var defense) ||
                    !defense.AutoEnabled || defense.State is DefenseState.Guiding or DefenseState.NoOperator ||
                    !CanExecuteModuleCommand(module) || module.ActiveCycle is not null) continue;
                var op = ResolveWeaponOperator(owner, module, WeaponSkillType.CountermeasureDefense);
                if (op is null) continue;
                var type = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
                double readyAt = Math.Max(from, defense.ReloadDueMotionTimeMs ?? from);
                if (readyAt > to) continue;
                foreach (var target in _objects)
                {
                    if (target.InitialMotion.Torpedo is not { } torpedo || target.InitialMotion.CountermeasureAttempted ||
                        torpedo.TargetObjectId != owner.InitialMotion.ObjectId) continue;
                    int chance = InterceptionMath.ChanceTenths(op.EffectiveRating, torpedo.TorpedoRating!.Value);
                    if (chance == 0) continue;
                    double start = Math.Max(readyAt, Math.Max(target.StartGameTimeMs, owner.StartGameTimeMs));
                    if (start > to) continue;
                    var entry = TorpedoCollisionMath.FirstContact(CombatPath(target), CombatPath(owner), start, to, defense.RangeKm * 10);
                    if (entry is null) continue;
                    long launchAt = (long)Math.Ceiling(entry.Value.MotionTimeMs);
                    if (launchAt > to) continue;
                    double end = torpedo.Route.StartMotionTimeMs + torpedo.Route.Segments.Sum(s => s.DurationMs);
                    if (end <= launchAt) continue;
                    var hull = TorpedoCollisionMath.FirstContact(CombatPath(target), CombatPath(owner), launchAt, end + 1);
                    if (hull is null) continue;
                    var origin = PredictMotion(owner, launchAt - owner.StartGameTimeMs);
                    var route = CountermeasureGuidanceMath.Plan(origin, torpedo.Route, launchAt, hull.Value.MotionTimeMs,
                        type.CountermeasureSpeedKmS!.Value, type.CountermeasureTurnRateDegPerSec!.Value);
                    if (route is null) continue;
                    var candidate = new DefenseLaunch(owner, module, target, route, launchAt, hull.Value.MotionTimeMs, chance, op);
                    if (first is null || candidate.Time < first.Time || candidate.Time == first.Time &&
                        (candidate.HullContact < first.HullContact || candidate.HullContact == first.HullContact &&
                         string.CompareOrdinal(candidate.Target.InitialMotion.ObjectId, first.Target.InitialMotion.ObjectId) < 0)) first = candidate;
                }
            }
        return first;
    }

    private void LaunchCountermeasure(DefenseLaunch launch)
    {
        string id;
        do { id = "countermeasure-" + checked(++_countermeasureSequence).ToString(System.Globalization.CultureInfo.InvariantCulture); }
        while (_objects.Any(o => string.Equals(o.InitialMotion.ObjectId, id, StringComparison.OrdinalIgnoreCase)));
        var torpedo = launch.Target.InitialMotion.Torpedo!;
        var flight = new CountermeasureSnapshot(launch.Owner.InitialMotion.ObjectId, launch.Module.ModuleId,
            launch.Target.InitialMotion.ObjectId, CountermeasurePhase.Guiding, launch.Route, launch.Time, launch.Chance,
            new(launch.Operator, torpedo.TorpedoRating!.Value, torpedo.RatingBreakdown), [],
            PredictedEncounterMotionTimeMs: launch.Time + launch.Route.Segments.Sum(s => s.DurationMs));
        int index = _objects.IndexOf(launch.Target);
        _objects[index] = launch.Target with { InitialMotion = launch.Target.InitialMotion with { CountermeasureAttempted = true } };
        var origin = launch.Route.Segments[0];
        var motion = new ObjectMotionSnapshot(id, origin.X, origin.Y, origin.SpeedKmS, origin.Direction, Countermeasure: flight);
        _objects.Add(new(motion, SpaceObjectType.Countermeasure, launch.Time, [], Name: "Countermeasure", IsKnown: true));
        _countermeasureTargetPlans[id] = torpedo.Route.StartMotionTimeMs;
        AppendCombatEvent(new(0, launch.Time, CombatEventType.Launch, flight.OwnerObjectId, flight.TargetTorpedoId, id,
            origin.X, origin.Y, flight.FrozenChanceTenths, RatingBreakdown: flight.RatingBreakdown, Result: "countermeasure"));
        var key = (flight.OwnerObjectId, flight.LauncherModuleId);
        _defenses[key] = _defenses[key] with
        {
            State = DefenseState.Guiding,
            ActiveProjectileId = id,
            ReloadDueMotionTimeMs = null,
            Operator = launch.Operator
        };
    }

    private static System.Collections.Immutable.ImmutableArray<TrailSegment> CloseCountermeasureHistory(SpaceObjectRuntime obj, double time)
    {
        var flight = obj.InitialMotion.Countermeasure!;
        var adapter = new TorpedoSnapshot(flight.OwnerObjectId, flight.LauncherModuleId, flight.TargetTorpedoId,
            flight.LaunchMotionTimeMs, flight.Route.Segments[^1].SpeedKmS, 90, 0, 0, flight.Route, flight.Trail);
        return CloseFlightHistory(obj with { InitialMotion = obj.InitialMotion with { Torpedo = adapter } }, time, adapter).Trail;
    }

    private void CompleteDefenseReloads(double time)
    {
        foreach (var key in _defenses.Keys.ToArray())
        {
            var defense = _defenses[key];
            if (defense.State == DefenseState.Reloading && defense.ReloadDueMotionTimeMs <= time)
                _defenses[key] = defense with
                {
                    State = defense.Operator is null ? DefenseState.NoOperator : DefenseState.Ready,
                    ReloadDueMotionTimeMs = null
                };
        }
    }

    private Dictionary<(string ObjectId, string ModuleId), DefenseSnapshot> _defenses = new();

    private Dictionary<(string ObjectId, string ModuleId), DefenseSnapshot> BuildDefenseState(IEnumerable<SpaceObjectRuntime> objects)
    {
        var result = new Dictionary<(string ObjectId, string ModuleId), DefenseSnapshot>();
        foreach (var owner in objects)
            foreach (var module in owner.Modules)
            {
                var type = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
                if (type.CountermeasureBaseRating is null) continue;
                if (owner.ObjectType is not (SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip))
                    throw new ScenarioException($"Defense module '{module.ModuleId}' must be installed on a ship.");
                var weaponOperator = ResolveWeaponOperator(owner, module, WeaponSkillType.CountermeasureDefense);
                result.Add((owner.InitialMotion.ObjectId, module.ModuleId), new DefenseSnapshot(
                    module.AutoDefenseEnabled, weaponOperator, weaponOperator is null ? DefenseState.NoOperator : DefenseState.Ready,
                    RangeKm: type.CountermeasureRangeKm!.Value));
            }
        return result;
    }

    private (SpaceObjectRuntime Projectile, SpaceObjectRuntime Target, TorpedoContact Contact)? FindFirstIntercept(double from, long to)
    {
        (SpaceObjectRuntime Projectile, SpaceObjectRuntime Target, TorpedoContact Contact)? first = null;
        foreach (var projectile in _objects.Where(o => o.InitialMotion.Countermeasure is { Phase: CountermeasurePhase.Guiding })
                     .OrderBy(o => o.InitialMotion.ObjectId, StringComparer.Ordinal))
        {
            var flight = projectile.InitialMotion.Countermeasure!;
            var target = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == flight.TargetTorpedoId && o.InitialMotion.Torpedo is not null);
            if (target is null) continue;
            double start = Math.Max(from, Math.Max(projectile.StartGameTimeMs, target.StartGameTimeMs));
            if (start > to) continue;
            var hit = TorpedoCollisionMath.FirstContact(CombatPath(projectile), CombatPath(target), start, to);
            if (hit is { } contact && (first is null || contact.MotionTimeMs < first.Value.Contact.MotionTimeMs))
                first = (projectile, target, contact);
        }
        return first;
    }

    private void ResolveFirstIntercept(SpaceObjectRuntime projectile, SpaceObjectRuntime target, TorpedoContact contact)
    {
        var flight = projectile.InitialMotion.Countermeasure!;
        int roll = _countermeasureRng.NextRoll();
        bool success = roll <= flight.FrozenChanceTenths;
        AppendCombatEvent(new(0, contact.MotionTimeMs,
            success ? CombatEventType.Intercept : CombatEventType.Miss, flight.OwnerObjectId, flight.TargetTorpedoId,
            projectile.InitialMotion.ObjectId, contact.X, contact.Y, flight.FrozenChanceTenths, roll, flight.RatingBreakdown));
        if (success)
        {
            RemoveCountermeasure(projectile, contact.MotionTimeMs);
            TerminateTorpedo(target, contact.MotionTimeMs, TorpedoTerminationKind.Intercept);
            return;
        }
        var trail = CloseCountermeasureHistory(projectile, contact.MotionTimeMs);
        var pose = TorpedoGuidanceMath.PredictPose(flight.Route, flight.Route.ElapsedMs + contact.MotionTimeMs - projectile.StartGameTimeMs);
        long anchor = (long)Math.Floor(contact.MotionTimeMs);
        var segment = new TorpedoRouteSegment(pose.X, pose.Y, pose.Direction, projectile.InitialMotion.SpeedKmS, 0, 0);
        var origin = TorpedoGuidanceMath.PredictSegment(segment, anchor - contact.MotionTimeMs);
        var route = new TorpedoRoute(anchor, 1, TorpedoRoutePhase.Straight, false,
            [segment with { X = origin.X, Y = origin.Y }]);
        _objects[_objects.IndexOf(projectile)] = projectile with
        {
            StartGameTimeMs = anchor,
            InitialMotion = projectile.InitialMotion with
            {
                X = origin.X,
                Y = origin.Y,
                Direction = pose.Direction,
                Countermeasure = flight with
                {
                    Phase = CountermeasurePhase.MissedCoast,
                    Route = route,
                    Trail = trail,
                    MissExpiresAtMotionTimeMs = contact.MotionTimeMs + 2000,
                    ResolutionRoll = roll,
                    ResolvedAtMotionTimeMs = contact.MotionTimeMs,
                    PredictedEncounterMotionTimeMs = null
                }
            }
        };
    }

    private void RefreshCountermeasureGuidance(long time)
    {
        if (time % 100 != 0) return;
        foreach (var projectile in _objects.Where(o => o.InitialMotion.Countermeasure is { Phase: CountermeasurePhase.Guiding }).ToArray())
        {
            var flight = projectile.InitialMotion.Countermeasure!;
            var target = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == flight.TargetTorpedoId);
            if (target?.InitialMotion.Torpedo is not { } torpedo) continue;
            if (_countermeasureTargetPlans.GetValueOrDefault(projectile.InitialMotion.ObjectId, -1) == torpedo.Route.StartMotionTimeMs &&
                flight.Route.HasIntercept) continue;
            double deadline = torpedo.Route.StartMotionTimeMs + torpedo.Route.Segments.Sum(s => s.DurationMs);
            var owner = _objects.First(o => o.InitialMotion.ObjectId == flight.OwnerObjectId);
            var module = owner.Modules.First(m => m.ModuleId == flight.LauncherModuleId);
            double turnRate = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex).CountermeasureTurnRateDegPerSec!.Value;
            double speed = projectile.InitialMotion.SpeedKmS;
            var route = CountermeasureGuidanceMath.Plan(projectile.InitialMotion, torpedo.Route, time, deadline, speed, turnRate) ??
                TorpedoGuidanceMath.Plan(projectile.InitialMotion, target.InitialMotion, speed, turnRate, time);
            _objects[_objects.IndexOf(projectile)] = projectile with
            {
                InitialMotion = projectile.InitialMotion with
                { Countermeasure = flight with { Route = route, PredictedEncounterMotionTimeMs = ImpactTime(route) } },
                StartGameTimeMs = time
            };
            _countermeasureTargetPlans[projectile.InitialMotion.ObjectId] = torpedo.Route.StartMotionTimeMs;
        }
    }

    private void RemoveLostCountermeasures(double motionTimeMs)
    {
        foreach (var projectile in _objects.Where(o => o.InitialMotion.Countermeasure is not null).ToArray())
        {
            var flight = projectile.InitialMotion.Countermeasure!;
            bool ownerExists = _objects.Any(o => o.InitialMotion.ObjectId == flight.OwnerObjectId && !o.IsDestroyed);
            bool targetExists = _objects.Any(o => o.InitialMotion.ObjectId == flight.TargetTorpedoId && o.InitialMotion.Torpedo is not null);
            if (!ownerExists || (flight.Phase == CountermeasurePhase.Guiding && !targetExists))
            {
                var point = CombatPath(projectile).Position(motionTimeMs);
                AppendCombatEvent(new(0, motionTimeMs, CombatEventType.TargetLost, flight.OwnerObjectId, flight.TargetTorpedoId,
                    projectile.InitialMotion.ObjectId, point.X, point.Y, flight.FrozenChanceTenths, RatingBreakdown: flight.RatingBreakdown,
                    Result: ownerExists ? "target_lost" : "owner_lost"));
                RemoveCountermeasure(projectile, motionTimeMs, ownerExists);
            }
        }
    }

    private void RemoveCountermeasure(SpaceObjectRuntime projectile, double motionTimeMs, bool reload = true)
    {
        var flight = projectile.InitialMotion.Countermeasure!;
        _objects.Remove(projectile);
        _countermeasureTargetPlans.Remove(projectile.InitialMotion.ObjectId);
        var key = (flight.OwnerObjectId, flight.LauncherModuleId);
        if (!_defenses.TryGetValue(key, out var defense)) return;
        var owner = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == key.OwnerObjectId);
        var module = owner?.Modules.FirstOrDefault(m => m.ModuleId == key.LauncherModuleId);
        if (!reload || module is null) { _defenses.Remove(key); return; }
        var type = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
        double due = motionTimeMs + type.CountermeasureReloadMs!.Value;
        _defenses[key] = defense with
        {
            ActiveProjectileId = null,
            State = DefenseState.Reloading,
            ReloadDueMotionTimeMs = due
        };
    }

    private DefenseSnapshot? ProjectDefense(SpaceObjectRuntime owner) => owner.Modules
        .Select(m => _defenses.GetValueOrDefault((owner.InitialMotion.ObjectId, m.ModuleId)))
        .FirstOrDefault(d => d is not null);
}
