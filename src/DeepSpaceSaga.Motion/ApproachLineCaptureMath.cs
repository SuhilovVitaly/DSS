using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion;

/// <summary>
/// Constant-speed, bounded-curvature rendezvous behind a catchable target, or
/// shortest route to the captured trailing point when rendezvous is impossible.
/// Fly the selected route once using exact arc integration. No speed changes.
/// </summary>
public static class ApproachLineCaptureMath
{
    public const string Phase = "LineCapture";
    public const int PlannerVersion = 3;
    public const double PositionTolerance = 0.01;

    public static ApproachRoute? Plan(ObjectMotionSnapshot ship, double targetX, double targetY,
        double targetDirection, double targetSpeed, double trailDistance, int turnRate)
    {
        if (ship.SpeedKmS <= 0 || turnRate <= 0)
            return null;
        double angle = targetDirection * Math.PI / 180;
        double fx = Math.Sin(angle), fy = -Math.Cos(angle);
        double along = (ship.X - targetX) * fx + (ship.Y - targetY) * fy;
        double cross = (ship.X - targetX) * -fy + (ship.Y - targetY) * fx;
        double trail = Math.Max(1 + ship.SpeedKmS * .01, trailDistance);
        ApproachRoute Route(ApproachFlyThroughPlan p) => new(ship.X, ship.Y, ship.Direction,
            ship.SpeedKmS, turnRate, p.Type, p.FirstRemainingUnits, p.SecondRemainingUnits,
            p.ThirdRemainingUnits, targetX, targetY, targetDirection, targetSpeed, trail, PlannerVersion: PlannerVersion);
        if (Math.Abs(cross) <= PositionTolerance &&
            Math.Abs(along + trail) <= PositionTolerance &&
            Math.Abs(Delta(ship.Direction, targetDirection)) <= 1e-7)
            return Route(new("SSS", 0, 0, 0));

        // Shortest length and earliest arrival are the same objective at fixed speed.
        // A faster target can still meet us if it is coming from behind.
        var intercept = ApproachPursuitMath.SolveInterceptFlyThroughPlan(
            ship.X, ship.Y, ship.Direction, ship.SpeedKmS,
            targetX - trail * fx, targetY - trail * fy, targetDirection, targetSpeed, turnRate);
        if (intercept.HasIntercept)
            return Route(intercept.Plan);

        // An unreachable moving target has no minimum-time rendezvous. Fly once to
        // its captured trailing pose instead of optimizing where to merge onto its
        // aft ray. Do not continually move this fallback waypoint as the target runs
        // away; TargetChanged only invalidates an actual change of motion.
        return Route(ApproachPursuitMath.CreateFlyThroughPlan(
            ship.X, ship.Y, ship.Direction, ship.SpeedKmS,
            targetX - trail * fx, targetY - trail * fy, targetDirection, turnRate));
    }

    /// <summary>Evaluate from the immutable origin, including partial arcs and crossings of segment boundaries.</summary>
    public static ObjectMotionSnapshot Predict(ObjectMotionSnapshot state, double elapsedMs)
    {
        if (state.ApproachRoute is not { } route || elapsedMs < 0)
            return state;
        double elapsed = route.ElapsedMs + elapsedMs;
        var pose = PredictPose(route, elapsed);
        bool complete = elapsed >= route.DurationMs - 1e-7;
        var target = ApproachPursuitMath.ExtrapolatePosition(route.TargetX, route.TargetY,
            route.TargetDirection, route.TargetSpeedKmS, (long)elapsed);
        return state with
        {
            X = pose.X,
            Y = pose.Y,
            Direction = pose.Direction,
            SpeedKmS = route.SpeedKmS,
            ApproachRoute = complete ? null : route with { ElapsedMs = elapsed },
            ActiveEngineCommandType = complete ? null : NavigationComputerCommandTypes.Approach,
            NavigationPhase = complete ? null : Phase,
            TurnStepRemainingMs = complete ? 0 : Math.Max(0, state.TurnStepRemainingMs - (long)elapsedMs),
            TurnStepIntervalMs = complete ? 0 : state.TurnStepIntervalMs,
            TurnStepDegrees = complete ? 0 : state.TurnStepDegrees,
            NavigationTargetX = complete ? null : target.X,
            NavigationTargetY = complete ? null : target.Y,
            NavigationTargetDirectionDegrees = complete ? null : route.TargetDirection,
            NavigationTargetSpeedKmS = complete ? null : route.TargetSpeedKmS
        };
    }

    /// <summary>Allocation-free pose at absolute route time, shared by simulation and map previews.</summary>
    public static (double X, double Y, double Direction) PredictPose(ApproachRoute route, double elapsed)
    {
        double travel = elapsed / 1000 * route.SpeedKmS * 10;
        double x = route.X, y = route.Y, heading = route.Direction * Math.PI / 180;
        double radius = route.SpeedKmS * 10 / (route.TurnRate * Math.PI / 180);
        ReadOnlySpan<double> lengths = stackalloc double[] { route.First, route.Second, route.Third };
        for (int i = 0; i < 3; i++)
        {
            double distance = Math.Min(travel, lengths[i]);
            travel -= distance;
            int sign = route.Type[i] == 'L' ? -1 : route.Type[i] == 'R' ? 1 : 0;
            if (sign == 0)
            {
                x += distance * Math.Sin(heading);
                y -= distance * Math.Cos(heading);
            }
            else
            {
                double next = heading + sign * distance / radius;
                x += radius / sign * (Math.Cos(heading) - Math.Cos(next));
                y += radius / sign * (Math.Sin(heading) - Math.Sin(next));
                heading = next;
            }
        }
        x += travel * Math.Sin(heading);
        y -= travel * Math.Cos(heading);
        bool complete = elapsed >= route.DurationMs - 1e-7;
        return (x, y, complete ? route.TargetDirection : (heading * 180 / Math.PI % 360 + 360) % 360);
    }

    /// <summary>True only if this route actually reaches the moving trailing slot.</summary>
    public static bool IsRendezvous(ApproachRoute route)
    {
        var end = PredictPose(route, route.DurationMs);
        double angle = route.TargetDirection * Math.PI / 180;
        double fx = Math.Sin(angle), fy = -Math.Cos(angle);
        double dx = end.X - route.TargetX, dy = end.Y - route.TargetY;
        double gap = route.TargetSpeedKmS * 10 * route.DurationMs / 1000 - (dx * fx + dy * fy);
        return Math.Abs(dx * -fy + dy * fx) <= PositionTolerance &&
            Math.Abs(gap - route.TrailDistance) <= .1;
    }

    public static bool TargetChanged(ApproachRoute route, ObjectMotionSnapshot target, double elapsedMs)
    {
        var expected = ApproachPursuitMath.ExtrapolatePosition(route.TargetX, route.TargetY,
            route.TargetDirection, route.TargetSpeedKmS, (long)(route.ElapsedMs + elapsedMs));
        return Math.Abs(Delta(route.TargetDirection, target.Direction)) > 0.01 ||
            Math.Abs(route.TargetSpeedKmS - target.SpeedKmS) > 1e-6 ||
            Math.Abs(expected.X - target.X) > PositionTolerance ||
            Math.Abs(expected.Y - target.Y) > PositionTolerance;
    }

    public static bool IsAlignedBehind(ObjectMotionSnapshot ship, ObjectMotionSnapshot target)
    {
        double angle = target.Direction * Math.PI / 180;
        double fx = Math.Sin(angle), fy = -Math.Cos(angle);
        double dx = ship.X - target.X, dy = ship.Y - target.Y;
        return dx * fx + dy * fy < 0 && Math.Abs(dx * -fy + dy * fx) <= PositionTolerance &&
            Math.Abs(Delta(ship.Direction, target.Direction)) <= 1e-6;
    }

    public static double Delta(double from, double to) => (to - from + 540) % 360 - 180;
}
