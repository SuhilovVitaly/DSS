using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion;

/// <summary>
/// Constant-speed, bounded-curvature rendezvous behind a catchable target, or
/// capture of an uncatchable target's aft ray with a free along-track endpoint.
/// Fly the selected route once using exact arc integration. No speed changes.
/// </summary>
public static class ApproachLineCaptureMath
{
    public const string Phase = "LineCapture";
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
            p.ThirdRemainingUnits, targetX, targetY, targetDirection, targetSpeed, trail);
        bool canCatch = ship.SpeedKmS > targetSpeed;
        if (Math.Abs(cross) <= PositionTolerance &&
            (canCatch ? Math.Abs(along + trail) <= PositionTolerance : along <= -trail) &&
            Math.Abs(Delta(ship.Direction, targetDirection)) <= 1e-7)
            return Route(new("SSS", 0, 0, 0));

        if (canCatch)
        {
            if (targetSpeed == 0)
                return Route(Curve(-trail));
            // Rendezvous with the moving trailing slot, never the object's center.
            // Keep the existing tested per-family root solver; replace its former
            // quantized execution and pursuit handoff with one exact route.
            var intercept = ApproachPursuitMath.SolveInterceptFlyThroughPlan(
                ship.X, ship.Y, ship.Direction, ship.SpeedKmS,
                targetX - trail * fx, targetY - trail * fy, targetDirection, targetSpeed, turnRate);
            if (intercept.HasIntercept)
                return Route(intercept.Plan);
            // A degenerate numerical solve still has a finite feasible aft-ray
            // route. It must never restart an endless point-pursuit cycle.
        }

        var best = Curve(-trail);
        double bestLength = best.RemainingUnits;
        double radius = ship.SpeedKmS * 10 / (turnRate * Math.PI / 180);
        double ratio = targetSpeed / ship.SpeedKmS;
        // A route to the current aft point is always feasible for a forward-moving
        // target. Its length bounds the endpoint of EVERY shorter candidate.
        double lower = along - bestLength;
        double upper = Math.Min(along + bestLength, -trail + ratio * bestLength);
        // At most 3 + 257 + 2*129 samples. Scratch buffers are local and bounded,
        // so command planning creates neither tree nodes nor temporary arrays.
        Span<double> samples = stackalloc double[520];
        int count = 0;
        samples[count++] = lower; samples[count++] = upper; samples[count++] = -trail;
        for (int i = 0; i <= 256; i++)
            samples[count++] = lower + (upper - lower) * i / 256;
        // Resolve short turn-radius features even when the target is very far away.
        for (int centerIndex = 0; centerIndex < 2; centerIndex++)
            for (int i = -64; i <= 64; i++)
            {
                double center = centerIndex == 0 ? along : -trail;
                double x = center + radius * i / 8;
                if (x >= lower && x <= upper)
                    samples[count++] = x;
            }
        samples = samples[..count];
        samples.Sort();
        int unique = 0;
        for (int i = 0; i < samples.Length; i++)
            if (unique == 0 || samples[i] != samples[unique - 1]) samples[unique++] = samples[i];
        var xs = samples[..unique];
        Span<double> costs = stackalloc double[520];
        Span<bool> feasible = stackalloc bool[520];
        for (int i = 0; i < xs.Length; i++)
            costs[i] = Evaluate(xs[i], out feasible[i]);
        for (int i = 1; i < xs.Length; i++)
        {
            if (feasible[i - 1] != feasible[i])
            {
                double lo = xs[i - 1], hi = xs[i];
                for (int j = 0; j < 45; j++)
                {
                    double mid = (lo + hi) / 2;
                    Evaluate(mid, out bool valid);
                    if (valid == feasible[i - 1]) lo = mid; else hi = mid;
                }
            }
            // Refine sampled local minima without assuming that the different
            // Dubins families form one globally smooth/convex cost function.
            if (i + 1 < xs.Length && costs[i] <= costs[i - 1] && costs[i] <= costs[i + 1] && feasible[i])
            {
                double lo = xs[i - 1], hi = xs[i + 1];
                for (int j = 0; j < 35; j++)
                {
                    double a = lo + (hi - lo) / 3, b = hi - (hi - lo) / 3;
                    if (Evaluate(a, out _) < Evaluate(b, out _)) hi = b; else lo = a;
                }
            }
        }
        return Route(best);

        ApproachFlyThroughPlan Curve(double x) => ApproachPursuitMath.CreateFlyThroughPlan(
            ship.X, ship.Y, ship.Direction, ship.SpeedKmS,
            targetX + x * fx, targetY + x * fy, targetDirection, turnRate);
        double Evaluate(double x, out bool valid)
        {
            var p = Curve(x);
            double length = p.RemainingUnits;
            valid = x - ratio * length <= -trail + 1e-8;
            if (valid && length < bestLength)
            {
                best = p;
                bestLength = length;
            }
            return valid ? length : double.PositiveInfinity;
        }
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
            X = pose.X, Y = pose.Y, Direction = pose.Direction,
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
