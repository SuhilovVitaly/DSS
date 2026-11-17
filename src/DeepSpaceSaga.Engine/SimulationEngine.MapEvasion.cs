using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private const long PirateControlStepMs = 250;

    private long NextPirateControlTime() => _demoCampaign.PirateWorld is { } world
        ? world.Groups.Where(g => !g.Retired && g.Phase != "captured" && g.MotionPlan is not null && g.MotionPlan.EndMs > world.LastMotionTimeMs)
            .Select(g => g.MotionPlan!.EndMs).DefaultIfEmpty(checked(world.LastMotionTimeMs + PirateControlStepMs)).Min() : long.MaxValue;

    private (long Time, bool Stopped) AdvancePirateWorld(long motionTimeMs)
    {
        if (_demoCampaign.PirateWorld is not { } world || motionTimeMs <= world.LastMotionTimeMs) return (motionTimeMs, false);
        var player = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (player is null || player.IsDestroyed)
        {
            _demoCampaign = _demoCampaign with { PirateWorld = world with { LastMotionTimeMs = motionTimeMs } };
            return (motionTimeMs, false);
        }
        var start = _objects.Single(o => o.InitialMotion.ObjectId == _demoDefinitions.Cluster!.StartStationId).InitialMotion;
        var safe = new PiratePoint(start.X, start.Y);
        var pose = PredictMotion(player, motionTimeMs - player.StartGameTimeMs);
        var fromPlayer = PredictMotion(player, world.LastMotionTimeMs - player.StartGameTimeMs);
        var previousPose = new PiratePoint(fromPlayer.X, fromPlayer.Y);
        var position = new PiratePoint(pose.X, pose.Y);
        double dt = (motionTimeMs - world.LastMotionTimeMs) / 1000.0;
        var groups = world.Groups.ToBuilder();
        (int Index, double Time, string Id)? capture = null;
        var discoveries = new List<(string Id, double Time)>();
        double playerRadius = EvaluateCarrierSensor(player)?.RadiusWorldUnits ?? 0;
        bool alreadyCaptured = world.Groups.Any(g => !g.Retired && g.Phase == "captured");
        for (int i = 0; i < groups.Count; i++)
        {
            var group = groups[i];
            if (group.Retired || group.Phase == "captured") continue;
            var capability = PirateGroupCapabilities(group, world);
            if (group.MotionPlan is null || group.MotionPlan.EndMs <= world.LastMotionTimeMs)
            {
                var planned = PlanPirateGroup(group, world, previousPose, safe, capability.MaxSpeedKmS, capability.SensorRadius,
                    PirateControlStepMs / 1000.0, out var origin);
                group = planned with
                {
                    X = group.X,
                    Y = group.Y,
                    Direction = group.Direction,
                    SpeedKmS = group.SpeedKmS,
                    MotionPlan = new(origin, world.LastMotionTimeMs, checked(world.LastMotionTimeMs + PirateControlStepMs))
                };
            }
            var plan = group.MotionPlan!;
            var moved = HoldMotion.Sample(plan.Origin, motionTimeMs - plan.StartMs);
            var next = group with { X = moved.X, Y = moved.Y, Direction = moved.Direction, SpeedKmS = moved.SpeedKmS };
            groups[i] = next;
            PiratePoint RelativeAt(double offset)
            {
                var pirate = HoldMotion.Sample(plan.Origin, world.LastMotionTimeMs + offset - plan.StartMs);
                var target = PlayerAtFractionalTime(player, world.LastMotionTimeMs + offset);
                return new(pirate.X - target.X, pirate.Y - target.Y);
            }
            double speedBound = (Math.Max(plan.Origin.SpeedKmS, plan.Origin.MaxSpeedKmS ?? 0) +
                Math.Max(Math.Max(fromPlayer.SpeedKmS, pose.SpeedKmS), fromPlayer.MaxSpeedKmS ?? 0)) * .01;
            if (!group.ReportConsumed && playerRadius > 0 &&
                TrajectoryCircleEntry(RelativeAt, dt * 1000, speedBound, playerRadius) is { } discovered)
                discoveries.Add((group.GroupId, world.LastMotionTimeMs + discovered));
            // Swept capture is only possible with a working pirate sensor. Safe-zone entry wins at the boundary.
            if (!alreadyCaptured && capability.SensorRadius >= 200)
            {
                double SafeClearance(double offset)
                {
                    var target = PlayerAtFractionalTime(player, world.LastMotionTimeMs + offset);
                    return Distance(target.X, target.Y, safe.X, safe.Y) - 8000;
                }
                double? crossing = TrajectoryCircleEntry(RelativeAt, dt * 1000, speedBound, 200, SafeClearance);
                if (crossing is { } fraction)
                {
                    var at = PlayerAtFractionalTime(player, world.LastMotionTimeMs + fraction);
                    if (Distance(at.X, at.Y, safe.X, safe.Y) > 8000)
                    {
                        double time = world.LastMotionTimeMs + fraction;
                        if (capture is null || time < capture.Value.Time || time == capture.Value.Time &&
                            StringComparer.Ordinal.Compare(group.GroupId, capture.Value.Id) < 0)
                            capture = (i, time, group.GroupId);
                    }
                }
            }
        }
        double firstEvent = Math.Min(capture?.Time ?? double.PositiveInfinity,
            discoveries.Select(d => d.Time).DefaultIfEmpty(double.PositiveInfinity).Min());
        bool stopped = double.IsFinite(firstEvent);
        if (stopped)
        {
            motionTimeMs = Math.Clamp((long)Math.Ceiling(firstEvent), world.LastMotionTimeMs, motionTimeMs);
            for (int i = 0; i < groups.Count; i++)
                if (!groups[i].Retired && groups[i].Phase != "captured" && groups[i].MotionPlan is { } plan)
                {
                    var at = HoldMotion.Sample(plan.Origin, motionTimeMs - plan.StartMs);
                    groups[i] = groups[i] with { X = at.X, Y = at.Y, Direction = at.Direction, SpeedKmS = at.SpeedKmS };
                }
            pose = PredictMotion(player, motionTimeMs - player.StartGameTimeMs);
            position = new(pose.X, pose.Y);
        }
        if (capture is { } hit && hit.Time <= motionTimeMs)
            groups[hit.Index] = groups[hit.Index] with { Phase = "captured", CapturedMotionTimeMs = hit.Time, MotionPlan = null };
        _demoCampaign = _demoCampaign with
        {
            PirateWorld = world with { Groups = groups.ToImmutable(), LastMotionTimeMs = motionTimeMs, LastPlayerPosition = position }
        };
        foreach (var discovered in discoveries.Where(d => d.Time <= motionTimeMs).OrderBy(d => d.Id, StringComparer.Ordinal))
            _demoCampaign = AddSensorReport(EnsurePirateComposition(_demoCampaign, discovered.Id), discovered.Id, pose, motionTimeMs);
        if (discoveries.Any(d => d.Time <= motionTimeMs))
            _demoCampaign = _demoCampaign with { Revision = checked(_demoCampaign.Revision + 1), CommitSequence = checked(_demoCampaign.CommitSequence + 1) };
        return (motionTimeMs, stopped);
    }

    internal static PirateGroupData StepPirateGroup(PirateGroupData group, PirateWorldData world,
        PiratePoint player, PiratePoint start, double maximumSpeed, double sensorRadius, double dt)
        => PlanPirateGroup(group, world, player, start, maximumSpeed, sensorRadius, dt, out _);

    private static PirateGroupData PlanPirateGroup(PirateGroupData group, PirateWorldData world,
        PiratePoint player, PiratePoint start, double maximumSpeed, double sensorRadius, double dt, out ObjectMotionSnapshot maneuver)
    {
        bool playerSafe = Distance(player.X, player.Y, start.X, start.Y) <= 8000;
        bool contact = !playerSafe && SensorCovers(sensorRadius, group.X, group.Y, player.X, player.Y);
        if (contact)
            group = group with
            {
                Phase = "pursuit",
                LastObservedPlayer = player,
                ContactEpoch = group.Phase == "pursuit" ? group.ContactEpoch : checked(group.ContactEpoch + 1),
                LastPursuitEndReason = null
            };
        else if (playerSafe && group.Phase is "pursuit" or "last_known")
            group = group with { Phase = "return", LastPursuitEndReason = "start_safe" };
        else if (group.Phase == "pursuit") group = group with { Phase = "last_known" };

        var target = group.Phase is "pursuit" or "last_known" && group.LastObservedPlayer is { } last
            ? last : group.PatrolPoints[group.NextPatrolPoint];
        double distance = Distance(group.X, group.Y, target.X, target.Y);
        if (distance <= 5 && group.Phase != "pursuit")
        {
            if (group.Phase == "last_known") group = group with { Phase = "return", LastPursuitEndReason = "contact_lost" };
            else group = group with { Phase = "patrol", NextPatrolPoint = (group.NextPatrolPoint + 1) % group.PatrolPoints.Length };
            target = group.PatrolPoints[group.NextPatrolPoint];
            distance = Distance(group.X, group.Y, target.X, target.Y);
        }
        if (SegmentDistance(new(group.X, group.Y), target, start) <= 8005)
        {
            // Deterministic local waypoint around the exclusion circle; the saved patrol index is unchanged.
            double angle = Math.Atan2(group.Y - start.Y, group.X - start.X);
            double targetAngle = Math.Atan2(target.Y - start.Y, target.X - start.X);
            double delta = Math.Atan2(Math.Sin(targetAngle - angle), Math.Cos(targetAngle - angle));
            double radius = Math.Max(8500 + maximumSpeed * maximumSpeed / (2 * world.AccelerationKmS2) * 10,
                Distance(group.X, group.Y, start.X, start.Y));
            angle += (delta >= 0 ? 1 : -1) * Math.Min(Math.Abs(delta), .15);
            target = new(start.X + Math.Cos(angle) * radius, start.Y + Math.Sin(angle) * radius);
            distance = Distance(group.X, group.Y, target.X, target.Y);
        }
        double bearing = Math.Atan2(target.X - group.X, group.Y - target.Y) * 180 / Math.PI;
        double error = ((bearing - group.Direction + 540) % 360) - 180;
        double turn = Math.Clamp(error / dt, -world.TurnRateDegreesPerSecond, world.TurnRateDegreesPerSecond);
        double desired = Math.Abs(error) > 10 ? 0 : Math.Min(maximumSpeed,
            Math.Sqrt(2 * world.AccelerationKmS2 * Math.Max(0, distance - 1) / 10));
        double acceleration = Math.Clamp((desired - group.SpeedKmS) / dt, -world.AccelerationKmS2, world.AccelerationKmS2);
        var motion = new ObjectMotionSnapshot(group.GroupId, group.X, group.Y, group.SpeedKmS, group.Direction,
            MaxSpeedKmS: Math.Max(maximumSpeed, group.SpeedKmS), SpeedHoldAccelerationKmS2: acceleration, TurnHoldRateDegPerSec: turn);
        var next = HoldMotion.Sample(motion, dt * 1000);
        if (!SafePirateManeuver(group, next, start, world.AccelerationKmS2, dt, turn))
        {
            motion = motion with { SpeedHoldAccelerationKmS2 = -world.AccelerationKmS2, TurnHoldRateDegPerSec = 0 };
            next = HoldMotion.Sample(motion, dt * 1000);
            if (!SafePirateManeuver(group, next, start, world.AccelerationKmS2, dt, 0))
                throw new ScenarioException("Pirate momentum has no feasible START-safe braking corridor.");
        }
        maneuver = motion;
        return group with { X = next.X, Y = next.Y, Direction = next.Direction, SpeedKmS = next.SpeedKmS };
    }

    private static ObjectMotionSnapshot PlayerAtFractionalTime(SpaceObjectRuntime player, double time)
    {
        if (player.InitialMotion.SpeedHoldAccelerationKmS2 is not null || player.InitialMotion.TurnHoldRateDegPerSec is not null)
            return CurrentManeuverForecast.Sample(player.InitialMotion, Math.Max(0, time - player.StartGameTimeMs));
        long whole = (long)Math.Floor(time);
        var a = RuntimeMotion.At(player, whole);
        if (whole == time) return a;
        var b = RuntimeMotion.At(player, whole + 1);
        double fraction = time - whole;
        return a with { X = a.X + (b.X - a.X) * fraction, Y = a.Y + (b.Y - a.Y) * fraction };
    }

    // Adaptive continuous path search. The speed bound proves exclusion; left-first traversal gives earliest entry.
    // Terminal uncertainty is at most 0.000001 world units (0.1 mm), including tangencies.
    internal static double? TrajectoryCircleEntry(Func<double, PiratePoint> relativeAt, double durationMs, double speedPerMs, double radius,
        Func<double, double>? safeClearance = null)
    {
        double DistanceAt(double t)
        {
            var p = relativeAt(t);
            return Math.Max(Math.Sqrt(p.X * p.X + p.Y * p.Y),
                safeClearance is null ? 0 : radius - safeClearance(t) + 1e-7);
        }
        if (DistanceAt(0) <= radius) return 0;
        if (speedPerMs <= 0) return null;
        double? Search(double left, double right, int depth)
        {
            double mid = (left + right) / 2, reach = speedPerMs * (right - left) / 2;
            double distance = DistanceAt(mid);
            if (distance - reach > radius) return null;
            if (reach <= 1e-6 || depth >= 48)
            {
                // A safety boundary is open: choose the first sampled point strictly outside, never its inside edge.
                if (safeClearance is null || safeClearance(left) > 0) return left;
                if (safeClearance(mid) > 0) return mid;
                return safeClearance(right) > 0 ? right : null;
            }
            return Search(left, mid, depth + 1) ?? Search(mid, right, depth + 1);
        }
        return Search(0, durationMs, 0);
    }

    private static bool SafePirateManeuver(PirateGroupData previous, ObjectMotionSnapshot next,
        PiratePoint start, double acceleration, double dt, double turn)
    {
        // Conservative arc/chord deviation bound plus the complete straight stopping path.
        double deviation = Math.Max(previous.SpeedKmS, next.SpeedKmS) * 10 * dt * Math.Abs(turn * dt * Math.PI / 180);
        if (SegmentDistance(new(previous.X, previous.Y), new(next.X, next.Y), start) <= 8000 + deviation) return false;
        double stopping = next.SpeedKmS * next.SpeedKmS / (2 * acceleration) * 10;
        double radians = next.Direction * Math.PI / 180;
        return SegmentDistance(new(next.X, next.Y), new(next.X + Math.Sin(radians) * stopping,
            next.Y - Math.Cos(radians) * stopping), start) > 8000;
    }

    internal static double? RelativeCircleEntry(double x0, double y0, double x1, double y1, double radius)
    {
        double c = x0 * x0 + y0 * y0 - radius * radius;
        if (c <= 0) return 0;
        double dx = x1 - x0, dy = y1 - y0, a = dx * dx + dy * dy;
        if (a == 0) return null;
        double b = x0 * dx + y0 * dy, discriminant = b * b - a * c;
        if (b >= 0 || discriminant < 0) return null;
        double t = c / (-b + Math.Sqrt(discriminant));
        return t is >= 0 and <= 1 ? t : null;
    }

    private static PursuitSnapshot ObservePursuit(PirateGroupData group, ObjectMotionSnapshot player, long time)
    {
        double x = group.X - player.X, y = group.Y - player.Y, range = Distance(group.X, group.Y, player.X, player.Y);
        double angle = group.Direction * Math.PI / 180, playerAngle = player.Direction * Math.PI / 180;
        double closing = range == 0 ? 0 : -(x * (Math.Sin(angle) * group.SpeedKmS - Math.Sin(playerAngle) * player.SpeedKmS) +
            y * (-Math.Cos(angle) * group.SpeedKmS + Math.Cos(playerAngle) * player.SpeedKmS)) * 10 / range;
        return new(group.GroupId, group.GroupId + ":" + group.ContactEpoch,
            group.Phase == "captured" ? PursuitStatuses.Captured : group.LastPursuitEndReason is not null ? PursuitStatuses.Escaped :
            closing > 0 ? PursuitStatuses.Approaching : PursuitStatuses.Receding, range, closing, time, group.LastPursuitEndReason);
    }
}
