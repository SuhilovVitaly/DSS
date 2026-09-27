using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Rng;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private bool HasResourceSurveys => _stationResourceFields?.Surveys.Count > 0;

    private string? ResourceSurveyModuleCommand(string objectId, string moduleId) =>
        _stationResourceFields?.Surveys.Any(j => j.ObjectId == objectId && j.ModuleId == moduleId) == true
            ? ScannerCommandTypes.StructuralScan : null;

    private bool IsResourceScanner(InstalledModuleRuntime module) =>
        _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex).CommandTypeIds.Contains(ScannerCommandTypes.StructuralScan);

    private static bool ResourceScannerAvailable(SpaceObjectRuntime actor, InstalledModuleRuntime module) =>
        !actor.IsDestroyed && module.PowerState == "On" && module.OperationalState == "Ready" && module.StructurePoints > 0;

    private CommandStartOutcome TryStartResourceSurvey(PlayerCommand command, long physicalTime)
    {
        var actor = _objects.Find(o => o.InitialMotion.ObjectId == command.ObjectId);
        if (command.ObjectId != PlayerShipObjectId || actor is null || actor.IsDestroyed)
            return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownObject);
        var module = actor.Modules.FirstOrDefault(m => m.ModuleId == command.ModuleId);
        if (module is null) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownModule);
        if (!IsResourceScanner(module)) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownCommandType);
        if (!ResourceScannerAvailable(actor, module))
            return CommandStartOutcome.Rejected(CommandReasonCodes.ModuleUnavailable);
        if (_dialogue.Active is not null) return CommandStartOutcome.Rejected("dialogue_active");
        if (string.IsNullOrWhiteSpace(command.TargetObjectId)) return CommandStartOutcome.Rejected(CommandReasonCodes.MissingTarget);
        var target = _objects.Find(o => o.InitialMotion.ObjectId == command.TargetObjectId && !o.IsDestroyed);
        if (target is null) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownTarget);
        if (!_resourceAsteroids.TryGetValue(command.TargetObjectId, out var manifest) || _stationResourceFields is null)
            return CommandStartOutcome.Rejected(ResourceSurveyReasonCodes.UnsupportedTarget);
        if (!target.IsKnown) return CommandStartOutcome.Rejected(ResourceSurveyReasonCodes.TargetNotIdentified);
        if (manifest.CompositionKnown) return CommandStartOutcome.Rejected(ResourceSurveyReasonCodes.AlreadyKnown);
        if (!ResourceSurveyInRange(actor, target, physicalTime, _stationResourceFields.Rules.StructuralScan.RangeKm))
            return CommandStartOutcome.Rejected(ResourceSurveyReasonCodes.OutOfRange);
        if (module.ActiveCycle is not null || _stationResourceFields.Surveys.Any(j =>
            (j.ObjectId == command.ObjectId && j.ModuleId == command.ModuleId) || j.TargetObjectId == command.TargetObjectId))
            return CommandStartOutcome.Rejected(CommandReasonCodes.Busy);
        long start = _processedWorldTimeMs;
        long duration = _stationResourceFields.Rules.StructuralScan.DurationGameTimeMs;
        if (start < 0 || physicalTime < 0 || start > long.MaxValue - duration)
            return CommandStartOutcome.Rejected(ResourceSurveyReasonCodes.InvalidTime);
        var job = new ResourceSurveyJobData(command.CommandId, command.ObjectId, command.ModuleId,
            command.TargetObjectId, start, checked(start + duration), physicalTime);
        _stationResourceFields = _stationResourceFields with
        {
            Surveys = _stationResourceFields.Surveys.Append(job).OrderBy(j => j.CommandId, StringComparer.Ordinal).ToImmutableArray()
        };
        return CommandStartOutcome.Started;
    }

    private static bool ResourceSurveyInRange(SpaceObjectRuntime actor, SpaceObjectRuntime target, long time, double rangeKm)
    {
        var a = RuntimeMotion.At(actor, time);
        var b = RuntimeMotion.At(target, time);
        return !OutsideSurveyRange(a.X - b.X, a.Y - b.Y, rangeKm * WorldUnitsPerKm);
    }

    // Only absorb floating point noise at exact tangency (world coordinates are doubles).
    private static bool OutsideSurveyRange(double dx, double dy, double radius) =>
        dx * dx + dy * dy > radius * radius + 1e-9;

    private AsteroidSurveySnapshot? ProjectResourceSurvey(SpaceObjectRuntime obj, ResourceFieldAsteroidData? manifest, long time)
    {
        if (manifest is null) return null;
        var player = _objects.Find(o => o.InitialMotion.ObjectId == PlayerShipObjectId && !o.IsDestroyed);
        bool canScan = !manifest.CompositionKnown && obj.IsKnown && !obj.IsDestroyed && player is not null &&
            !_stationResourceFields!.Surveys.Any(j => j.TargetObjectId == manifest.ObjectId) &&
            ResourceSurveyInRange(player, obj, time, _stationResourceFields.Rules.StructuralScan.RangeKm);
        return new(obj.MassKg!.Value, manifest.CompositionKnown, canScan,
            manifest.CompositionKnown ? obj.CompositionType : null,
            manifest.CompositionKnown ? manifest.Resources.Select(r => new ResourceFractionSnapshot(r.ItemTypeId, r.Permille)).ToImmutableArray() : []);
    }

    private long NextResourceSurveyTime() => HasResourceSurveys
        ? _stationResourceFields!.Surveys.Min(j => j.DueGameTimeMs) : long.MaxValue;

    private static PlayerCommand ResourceSurveyCommand(ResourceSurveyJobData job) =>
        new(job.CommandId, 0, job.ObjectId, job.ModuleId, ScannerCommandTypes.StructuralScan, job.TargetObjectId);

    private void FinishResourceSurvey(ResourceSurveyJobData job, CommandResultStatus status, long calendarTime, string? reason = null)
    {
        _stationResourceFields = _stationResourceFields! with
        {
            Surveys = _stationResourceFields.Surveys.Where(j => j.CommandId != job.CommandId).ToImmutableArray()
        };
        RecordCommandResult(ResourceSurveyCommand(job), status, calendarTime, reason);
    }

    private void ValidateResourceSurveys(long physicalTime, Func<long, long> calendarAt)
    {
        if (!HasResourceSurveys) return;
        foreach (var job in _stationResourceFields!.Surveys)
        {
            var actor = _objects.Find(o => o.InitialMotion.ObjectId == job.ObjectId);
            var module = actor?.Modules.FirstOrDefault(m => m.ModuleId == job.ModuleId);
            if (actor is null || module is null || !IsResourceScanner(module) ||
                !ResourceScannerAvailable(actor, module) || module.ActiveCycle is not null)
            {
                FinishResourceSurvey(job, CommandResultStatus.Cancelled, calendarAt(physicalTime), CommandReasonCodes.ModuleUnavailable);
                continue;
            }
            var target = _objects.Find(o => o.InitialMotion.ObjectId == job.TargetObjectId && !o.IsDestroyed);
            if (target is null)
            {
                FinishResourceSurvey(job, CommandResultStatus.Failed, calendarAt(physicalTime), ResourceSurveyReasonCodes.TargetLost);
                continue;
            }
            long from = job.LastValidatedSimulationTimeMs;
            if (physicalTime < from) continue;
            long? crossing = FirstSurveyRangeExit(actor, target, from, physicalTime, _stationResourceFields.Rules.StructuralScan.RangeKm);
            if (crossing is { } at)
            {
                FinishResourceSurvey(job, CommandResultStatus.Failed, calendarAt(at), ResourceSurveyReasonCodes.OutOfRange);
                continue;
            }
            _stationResourceFields = _stationResourceFields with
            {
                Surveys = _stationResourceFields.Surveys.Select(j => j.CommandId == job.CommandId
                    ? j with { LastValidatedSimulationTimeMs = physicalTime } : j).ToImmutableArray()
            };
        }
    }

    private void CompleteResourceSurveys(long calendarTime)
    {
        if (!HasResourceSurveys) return;
        foreach (var job in _stationResourceFields!.Surveys.Where(j => j.DueGameTimeMs <= calendarTime))
        {
            string name = $"ResourceSurvey:{job.ObjectId}:{job.ModuleId}";
            var saved = _stationResourceFields.RngStreams.FirstOrDefault(s => s.Name == name) ??
                new ResourceFieldRngData(name, RngStreamSeedDerivation.DeriveStreamSeed(MasterSeed, name), 10000);
            var random = new ResourceFieldRandom(saved);
            bool success = Math.Floor(random.NextDouble() * 100) < _stationResourceFields.Rules.StructuralScan.SuccessChancePercent;
            var manifest = _resourceAsteroids[job.TargetObjectId] with { CompositionKnown = success };
            // Stage every part of the outcome before publishing under the world lock.
            var updated = _stationResourceFields with
            {
                RngStreams = _stationResourceFields.RngStreams.Where(s => s.Name != name).Append(random.Capture())
                    .OrderBy(s => s.Name, StringComparer.Ordinal).ToImmutableArray(),
                Asteroids = _stationResourceFields.Asteroids.Select(a => a.ObjectId == job.TargetObjectId ? manifest : a).ToImmutableArray()
            };
            _resourceAsteroids = _resourceAsteroids.SetItem(job.TargetObjectId, manifest);
            _stationResourceFields = updated;
            FinishResourceSurvey(job, success ? CommandResultStatus.Executed : CommandResultStatus.Failed,
                job.DueGameTimeMs, success ? null : ResourceSurveyReasonCodes.ScanFailed);
        }
    }

    private void ValidateRestoredResourceSurveys(GameStateData state, List<SpaceObjectRuntime> objects)
    {
        if (state.StationResourceFields is not { } fields) return;
        foreach (var job in fields.Surveys)
        {
            var actor = objects.Find(o => o.InitialMotion.ObjectId == state.PlayerShipObjectId && o.InitialMotion.ObjectId == job.ObjectId);
            var module = actor?.Modules.FirstOrDefault(m => m.ModuleId == job.ModuleId);
            var target = objects.Find(o => o.InitialMotion.ObjectId == job.TargetObjectId && !o.IsDestroyed && o.IsKnown);
            bool valid = actor is not null && module is not null && IsResourceScanner(module) &&
                ResourceScannerAvailable(actor, module) && module.ActiveCycle is null && target is not null &&
                fields.Asteroids.Any(a => a.ObjectId == job.TargetObjectId && !a.CompositionKnown) &&
                job.StartedGameTimeMs >= 0 && job.StartedGameTimeMs <= state.GameTimeMs && state.GameTimeMs < job.DueGameTimeMs &&
                job.StartedGameTimeMs <= long.MaxValue - fields.Rules.StructuralScan.DurationGameTimeMs &&
                job.DueGameTimeMs == job.StartedGameTimeMs + fields.Rules.StructuralScan.DurationGameTimeMs &&
                job.LastValidatedSimulationTimeMs == state.MotionTimeMs && state.MotionTimeMs >= 0 &&
                ResourceSurveyInRange(actor, target, state.MotionTimeMs, fields.Rules.StructuralScan.RangeKm) &&
                !(state.CommandReceipts ?? []).Any(r => r.CommandId == job.CommandId) &&
                !(state.PendingCommands ?? []).Any(c => c.CommandId == job.CommandId) &&
                !objects.SelectMany(o => o.Modules).Any(m => m.ActiveCycle?.CommandId == job.CommandId);
            if (!valid) throw new ScenarioException($"Invalid saved resource survey '{job.CommandId}': eligibility, clock or command conflict.");
        }
    }

    private void RestoreResourceSurveyCommandIds()
    {
        lock (_commandGate)
            foreach (var job in _stationResourceFields?.Surveys ?? []) _knownCommands.Add(job.CommandId);
    }

    /// <summary>
    /// Motion callers split at steering/effect boundaries before mutating the trajectory.
    /// A straight segment has a convex squared distance. For each Approach arc, include
    /// its first radial maximum (also covers full revolutions), then its end. An exit
    /// bracket contains only one outward crossing, even if it starts before a minimum.
    /// </summary>
    private static long? FirstSurveyRangeExit(SpaceObjectRuntime actor, SpaceObjectRuntime target,
        long from, long to, double rangeKm)
    {
        var targetPose = RuntimeMotion.At(target, from); // Generated targets are stationary.
        double radius = rangeKm * WorldUnitsPerKm;
        var cycle = actor.Modules.Select(m => m.ActiveCycle).FirstOrDefault(c =>
            c?.CommandType == NavigationComputerCommandTypes.Approach && c.ApproachRoute is not null);
        var route = cycle?.ApproachRoute;
        double routeFrom = route is null ? 0 : route.ElapsedMs + (from - cycle!.StartedGameTimeMs);
        double span = to - from;
        var checkpoints = new List<double> { 0, span };
        (double X, double Y) Position(double offset)
        {
            if (route is not null)
            {
                var pose = ApproachLineCaptureMath.PredictPose(route, routeFrom + offset);
                return (pose.X, pose.Y);
            }
            var initial = actor.InitialMotion;
            double distance = ((from - actor.StartGameTimeMs) + offset) / 1000 * initial.SpeedKmS * WorldUnitsPerKm;
            double heading = initial.Direction * Math.PI / 180;
            return (initial.X + distance * Math.Sin(heading), initial.Y - distance * Math.Cos(heading));
        }
        bool Outside(double offset)
        {
            var p = Position(offset);
            return OutsideSurveyRange(p.X - targetPose.X, p.Y - targetPose.Y, radius);
        }
        if (route is { SpeedKmS: > 0, TurnRate: > 0 })
        {
            double segmentStart = 0;
            double arcRadius = route.SpeedKmS * WorldUnitsPerKm / (route.TurnRate * Math.PI / 180);
            double omega = route.TurnRate * Math.PI / 180 / 1000;
            double[] lengths = [route.First, route.Second, route.Third];
            for (int i = 0; i < 3; i++)
            {
                double segmentEnd = segmentStart + lengths[i] / (route.SpeedKmS * WorldUnitsPerKm) * 1000;
                double lo = Math.Max(routeFrom, segmentStart), hi = Math.Min(routeFrom + span, segmentEnd);
                if (hi >= lo)
                {
                    checkpoints.Add(lo - routeFrom);
                    checkpoints.Add(hi - routeFrom);
                    int sign = route.Type[i] == 'L' ? -1 : route.Type[i] == 'R' ? 1 : 0;
                    if (sign != 0 && hi > lo)
                    {
                        var p = ApproachLineCaptureMath.PredictPose(route, lo);
                        double heading = p.Direction * Math.PI / 180;
                        double cx = p.X + arcRadius / sign * Math.Cos(heading);
                        double cy = p.Y + arcRadius / sign * Math.Sin(heading);
                        double radial = Math.Atan2(p.Y - cy, p.X - cx);
                        double farthest = Math.Atan2(cy - targetPose.Y, cx - targetPose.X);
                        double angle = ((sign * (farthest - radial)) % (2 * Math.PI) + 2 * Math.PI) % (2 * Math.PI);
                        double maximumAt = lo + angle / omega;
                        if (maximumAt <= hi) checkpoints.Add(maximumAt - routeFrom);
                    }
                }
                segmentStart = segmentEnd;
            }
        }
        checkpoints.Sort();
        double previous = 0;
        foreach (double point in checkpoints)
        {
            if (Outside(point))
            {
                double lo = previous, hi = point;
                // Sub-ms precision keeps ceil stable across fine/coarse snapshot cadence.
                while (hi - lo > 1e-7)
                {
                    double mid = lo + (hi - lo) / 2;
                    if (mid == lo || mid == hi) break;
                    if (Outside(mid)) hi = mid; else lo = mid;
                }
                return from + (long)Math.Ceiling(hi);
            }
            previous = point;
        }
        return null;
    }
}
