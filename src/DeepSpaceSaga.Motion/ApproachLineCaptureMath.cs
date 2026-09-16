using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion;

/// <summary>
/// Constant-speed, bounded-curvature rendezvous behind a catchable target, or
/// closest practical approach onto a faster target's aft ray.
/// Fly the selected route once using exact arc integration. No speed changes.
/// </summary>
public static class ApproachLineCaptureMath
{
    public const string Phase = "LineCapture";
    public const int PlannerVersion = 1;
    public const double PositionTolerance = 0.01;

    // One extra unit of flight must save at least 0.01 units of separation.
    // This keeps equal/near-equal speeds finite instead of chasing an asymptote.
    private const double FlightDistancePenalty = 0.01;

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
            // Preserve a finite, safe fallback if a rendezvous solve degenerates.
            return Route(Curve(-trail));
        }

        var best = Curve(-trail);
        double radius = ship.SpeedKmS * 10 / (turnRate * Math.PI / 180);
        double ratio = targetSpeed / ship.SpeedKmS;
        double weightedRatio = ratio + FlightDistancePenalty;
        // At endpoint x, the target has moved ratio * L while the ship flies L.
        // Minimize their separation on arrival, not time to an arbitrary aft point.
        double bestCost = weightedRatio * best.RemainingUnits + trail;
        // L >= |x - along| gives finite bounds for EVERY route that can beat bestCost.
        // weightedRatio > 1, even when the two speeds are equal.
        double lower = (weightedRatio * along - bestCost) / (weightedRatio + 1);
        double upper = (bestCost + weightedRatio * along) / (weightedRatio - 1);
        double straightOptimum = along + Math.Abs(cross) / Math.Sqrt(weightedRatio * weightedRatio - 1);
        // A global grid plus turn-radius neighborhoods of the ship, trailing slot,
        // and analytic straight-flight optimum. All scratch storage is bounded.
        Span<double> samples = stackalloc double[700];
        int count = 0;
        samples[count++] = lower; samples[count++] = upper; samples[count++] = -trail;
        samples[count++] = Math.Clamp(straightOptimum, lower, upper);
        for (int i = 0; i <= 256; i++)
            samples[count++] = lower + (upper - lower) * i / 256;
        // Resolve short turn-radius features even when the target is very far away.
        for (int centerIndex = 0; centerIndex < 3; centerIndex++)
            for (int i = -64; i <= 64; i++)
            {
                double center = centerIndex == 0 ? along : centerIndex == 1 ? -trail : straightOptimum;
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
        Span<double> costs = stackalloc double[700];
        Span<bool> feasible = stackalloc bool[700];
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
            double cost = weightedRatio * length - x;
            if (valid && (cost < bestCost - 1e-8 ||
                Math.Abs(cost - bestCost) <= 1e-8 && length < best.RemainingUnits))
            {
                best = p;
                bestCost = cost;
            }
            return valid ? cost : double.PositiveInfinity;
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
