using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private const long PirateControlStepMs = 250;

    private long NextPirateControlTime() => _demoCampaign.PirateWorld is { } world
        ? checked(world.LastMotionTimeMs + PirateControlStepMs) : long.MaxValue;

    private void AdvancePirateWorld(long motionTimeMs)
    {
        if (_demoCampaign.PirateWorld is not { } world || motionTimeMs < NextPirateControlTime()) return;
        var player = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (player is null || player.IsDestroyed)
        {
            _demoCampaign = _demoCampaign with { PirateWorld = world with { LastMotionTimeMs = motionTimeMs } };
            return;
        }
        var start = _objects.Single(o => o.InitialMotion.ObjectId == _demoDefinitions.Cluster!.StartStationId).InitialMotion;
        var safe = new PiratePoint(start.X, start.Y);
        var pose = PredictMotion(player, motionTimeMs - player.StartGameTimeMs);
        var previousPose = world.LastPlayerPosition ?? new PiratePoint(
            PredictMotion(player, world.LastMotionTimeMs - player.StartGameTimeMs).X,
            PredictMotion(player, world.LastMotionTimeMs - player.StartGameTimeMs).Y);
        var position = new PiratePoint(pose.X, pose.Y);
        double dt = (motionTimeMs - world.LastMotionTimeMs) / 1000.0;
        var groups = world.Groups.ToBuilder();
        (int Index, double Time, string Id)? capture = null;
        bool alreadyCaptured = world.Groups.Any(g => !g.Retired && g.Phase == "captured");
        for (int i = 0; i < groups.Count; i++)
        {
            var group = groups[i];
            if (group.Retired || group.Phase == "captured") continue;
            var capability = PirateGroupCapabilities(group, world);
            var next = StepPirateGroup(group, world, position, safe, capability.MaxSpeedKmS, capability.SensorRadius, dt);
            groups[i] = next;
            // Swept capture is only possible with a working pirate sensor. Safe-zone entry wins at the boundary.
            if (!alreadyCaptured && capability.SensorRadius >= 200)
            {
                double? crossing = RelativeCircleEntry(group.X - previousPose.X, group.Y - previousPose.Y,
                    next.X - position.X, next.Y - position.Y, 200);
                if (crossing is { } fraction)
                {
                    var at = new PiratePoint(previousPose.X + (position.X - previousPose.X) * fraction,
                        previousPose.Y + (position.Y - previousPose.Y) * fraction);
                    if (Distance(at.X, at.Y, safe.X, safe.Y) > 8000)
                    {
                        double time = world.LastMotionTimeMs + fraction * dt * 1000;
                        if (capture is null || time < capture.Value.Time || time == capture.Value.Time &&
                            StringComparer.Ordinal.Compare(group.GroupId, capture.Value.Id) < 0)
                            capture = (i, time, group.GroupId);
                    }
                }
            }
        }
        if (capture is { } hit)
            groups[hit.Index] = groups[hit.Index] with { Phase = "captured", CapturedMotionTimeMs = hit.Time };
        _demoCampaign = _demoCampaign with
        {
            PirateWorld = world with { Groups = groups.ToImmutable(), LastMotionTimeMs = motionTimeMs, LastPlayerPosition = position }
        };
    }

    internal static PirateGroupData StepPirateGroup(PirateGroupData group, PirateWorldData world,
        PiratePoint player, PiratePoint start, double maximumSpeed, double sensorRadius, double dt)
    {
        bool playerSafe = Distance(player.X, player.Y, start.X, start.Y) <= 8000;
        bool contact = !playerSafe && SensorCovers(sensorRadius, group.X, group.Y, player.X, player.Y);
        if (contact)
            group = group with { Phase = "pursuit", LastObservedPlayer = player,
                ContactEpoch = group.Phase == "pursuit" ? group.ContactEpoch : checked(group.ContactEpoch + 1), LastPursuitEndReason = null };
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
            next = HoldMotion.Sample(motion with { SpeedHoldAccelerationKmS2 = -world.AccelerationKmS2, TurnHoldRateDegPerSec = 0 }, dt * 1000);
            if (!SafePirateManeuver(group, next, start, world.AccelerationKmS2, dt, 0))
                throw new ScenarioException("Pirate momentum has no feasible START-safe braking corridor.");
        }
        return group with { X = next.X, Y = next.Y, Direction = next.Direction, SpeedKmS = next.SpeedKmS };
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
