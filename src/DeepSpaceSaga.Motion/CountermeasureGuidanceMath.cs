using DeepSpaceSaga.Contracts;
namespace DeepSpaceSaga.Motion;

public sealed record CountermeasurePlan(TorpedoRoute Route, double? PredictedEncounterMotionTimeMs);

/// <summary>Predictive interception of the confirmed curved projectile trajectory, from the carrier centre.</summary>
public static class CountermeasureGuidanceMath
{
    /// <summary>The launcher aims before release; airborne guidance still obeys the captured turn rate.</summary>
    public static TorpedoRoute? PlanLaunch(ObjectMotionSnapshot defender, TorpedoRoute target, long now,
        double firstHullContactMotionTimeMs, double speedKmS = 12, double turnRateDegPerSec = 90,
        double? expiresAtMotionTimeMs = null) =>
        Plan(AimLaunch(defender, target, now, Math.Min(firstHullContactMotionTimeMs,
                expiresAtMotionTimeMs ?? firstHullContactMotionTimeMs) - now, speedKmS),
            target, now, firstHullContactMotionTimeMs, speedKmS, turnRateDegPerSec, expiresAtMotionTimeMs);

    public static CountermeasurePlan PlanManualLaunch(ObjectMotionSnapshot defender, TorpedoRoute target, long now,
        long remainingLifetimeMs, double speedKmS = 12, double turnRateDegPerSec = 90,
        double? firstTargetContactMotionTimeMs = null) =>
        PlanManual(AimLaunch(defender, target, now, remainingLifetimeMs, speedKmS), target, now,
            remainingLifetimeMs, speedKmS, turnRateDegPerSec, firstTargetContactMotionTimeMs);

    private static ObjectMotionSnapshot AimLaunch(ObjectMotionSnapshot defender, TorpedoRoute target, long now,
        double horizon, double speedKmS)
    {
        // Find the first straight-flight rendezvous on the confirmed curved target route.
        // Distances use world units (100 m), while speeds are supplied in km/s.
        double low = 0;
        double aimAt = Math.Min(100, Math.Max(0, horizon));
        if (double.IsFinite(horizon) && horizon > 0 && double.IsFinite(speedKmS) && speedKmS > 0)
        {
            for (double high = Math.Min(20, horizon); ; high = Math.Min(horizon, high + 20))
            {
                if (Residual(high) <= 0)
                {
                    for (int i = 0; i < 60; i++)
                    {
                        double mid = (low + high) / 2;
                        if (mid == low || mid == high) break;
                        if (Residual(mid) > 0) low = mid; else high = mid;
                    }
                    aimAt = (low + high) / 2;
                    break;
                }
                if (high >= horizon) break;
                low = high;
            }
        }
        var point = TorpedoGuidanceMath.PredictPose(target, now - target.StartMotionTimeMs + aimAt);
        double direction = Math.Atan2(point.X - defender.X, -(point.Y - defender.Y)) * 180 / Math.PI;
        return defender with { Direction = (direction + 360) % 360 };

        double Residual(double ms)
        {
            var point = TorpedoGuidanceMath.PredictPose(target, now - target.StartMotionTimeMs + ms);
            double dx = point.X - defender.X, dy = point.Y - defender.Y;
            return Math.Sqrt(dx * dx + dy * dy) - speedKmS * ms / 100;
        }
    }

    public static TorpedoRoute? Plan(ObjectMotionSnapshot defender, TorpedoRoute target, long now,
        double firstHullContactMotionTimeMs, double speedKmS = 12, double turnRateDegPerSec = 90,
        double? expiresAtMotionTimeMs = null) =>
        PlanCore(defender, target, now, firstHullContactMotionTimeMs, speedKmS, turnRateDegPerSec, expiresAtMotionTimeMs, forecast: false);

    private static TorpedoRoute? PlanCore(ObjectMotionSnapshot defender, TorpedoRoute target, long now,
        double firstHullContactMotionTimeMs, double speedKmS, double turnRateDegPerSec,
        double? expiresAtMotionTimeMs, bool forecast)
    {
        double deadline = Math.Min(firstHullContactMotionTimeMs, expiresAtMotionTimeMs ?? firstHullContactMotionTimeMs);
        double horizon = deadline - now;
        if (!double.IsFinite(horizon) || horizon <= 0) return null;
        double lastX = double.NaN, lastY = double.NaN;
        TorpedoRoute? lastRoute = null;
        double low = 0;
        double previous = Residual(0, out _);
        // Bracket the earliest feasible arrival. Every evaluation uses the entire analytic target route.
        for (double high = Math.Min(100, horizon); ; high = Math.Min(horizon, high * 1.5 + 100))
        {
            double value = Residual(high, out _);
            if (previous >= 0 && value <= 0)
            {
                for (int i = 0; i < 60; i++)
                {
                    if (forecast && MonotoneStraightBracket(defender, target, now, low, high, speedKmS, turnRateDegPerSec))
                    {
                        double left = Residual(low, out _), right = Residual(high, out _);
                        int side = 0;
                        for (int refinement = 0; refinement < 20; refinement++)
                        {
                            double candidateTime = (low * right - high * left) / (right - left);
                            if (!double.IsFinite(candidateTime) || candidateTime <= low || candidateTime >= high) candidateTime = (low + high) / 2;
                            double candidateError = Residual(candidateTime, out var candidate);
                            if (Math.Abs(candidateError) < 1e-8 && now + candidateTime < firstHullContactMotionTimeMs) return candidate;
                            if (candidateError > 0)
                            {
                                low = candidateTime; left = candidateError;
                                if (side == 1) right /= 2;
                                side = 1;
                            }
                            else
                            {
                                high = candidateTime; right = candidateError;
                                if (side == -1) left /= 2;
                                side = -1;
                            }
                        }
                    }
                    double mid = (low + high) / 2;
                    if (mid == low || mid == high) break;
                    if (Residual(mid, out _) > 0) low = mid; else high = mid;
                }
                double time = (low + high) / 2;
                double error = Residual(time, out var route);
                if (Math.Abs(error) < 1e-5 && now + time < firstHullContactMotionTimeMs)
                    return route;
            }
            if (high >= horizon)
            {
                // The 500 m contact can precede hull impact even when centre rendezvous does not.
                Residual(horizon, out var boundaryRoute);
                if (!boundaryRoute.HasIntercept) return null;
                CollisionPath Path(TorpedoRoute r) => new(t =>
                {
                    var p = TorpedoGuidanceMath.PredictPose(r, t - r.StartMotionTimeMs);
                    return (p.X, p.Y);
                }, r.Segments.Max(s => s.SpeedKmS) / 100);
                var contact = forecast ? FirstForecastContact(boundaryRoute, target, now, deadline) :
                    TorpedoCollisionMath.FirstContact(Path(boundaryRoute), Path(target), now, deadline);
                return contact is { } hit && hit.MotionTimeMs < firstHullContactMotionTimeMs ? boundaryRoute : null;
            }
            low = high;
            previous = value;
        }

        double Residual(double ms, out TorpedoRoute route)
        {
            var point = TorpedoGuidanceMath.PredictPose(target, now - target.StartMotionTimeMs + ms);
            // Near convergence several bisection times round to the same world
            // point. Reuse that exact immutable plan, without changing the root
            // iterations, comparisons or duration arithmetic.
            if (lastRoute is not null && point.X == lastX && point.Y == lastY) route = lastRoute;
            else
            {
                route = forecast ? TorpedoGuidanceMath.PlanStationaryPointForForecast(defender,
                    point.X, point.Y, speedKmS, turnRateDegPerSec, now) : TorpedoGuidanceMath.PlanStationaryPoint(defender,
                    point.X, point.Y, speedKmS, turnRateDegPerSec, now);
                lastX = point.X;
                lastY = point.Y;
                lastRoute = route;
            }
            return route.HasIntercept ? route.Segments.Sum(s => s.DurationMs) - ms : double.PositiveInfinity;
        }
    }

    /// <summary>
    /// Manual fire always has a bounded flight, even when geometry has no intercept within
    /// captured remaining lifetime. A known earlier target collision suppresses the estimate.
    /// The estimate concerns geometry only; it never promises a successful interception roll.
    /// </summary>
    public static CountermeasurePlan PlanManual(ObjectMotionSnapshot defender, TorpedoRoute target, long now,
        long remainingLifetimeMs, double speedKmS = 12, double turnRateDegPerSec = 90,
        double? firstTargetContactMotionTimeMs = null) =>
        PlanManualCore(defender, target, now, remainingLifetimeMs, speedKmS, turnRateDegPerSec, firstTargetContactMotionTimeMs, forecast: false);

    internal static CountermeasurePlan PlanManualForForecast(ObjectMotionSnapshot defender, TorpedoRoute target, long now,
        long remainingLifetimeMs, double speedKmS, double turnRateDegPerSec, double? firstTargetContactMotionTimeMs) =>
        PlanManualCore(defender, target, now, remainingLifetimeMs, speedKmS, turnRateDegPerSec, firstTargetContactMotionTimeMs, forecast: true);

    private static CountermeasurePlan PlanManualCore(ObjectMotionSnapshot defender, TorpedoRoute target, long now,
        long remainingLifetimeMs, double speedKmS, double turnRateDegPerSec, double? firstTargetContactMotionTimeMs, bool forecast)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(remainingLifetimeMs);
        if (!double.IsFinite(speedKmS) || speedKmS <= 0) throw new ArgumentOutOfRangeException(nameof(speedKmS));
        if (!double.IsFinite(turnRateDegPerSec) || turnRateDegPerSec <= 0) throw new ArgumentOutOfRangeException(nameof(turnRateDegPerSec));
        double expires = now + (double)remainingLifetimeMs;
        var route = forecast && firstTargetContactMotionTimeMs is { } deadline &&
            CannotReachBeforeDeadline(defender, target, now, Math.Min(expires, deadline), speedKmS, turnRateDegPerSec)
            ? null : PlanCore(defender, target, now, Math.BitIncrement(expires), speedKmS, turnRateDegPerSec, expires, forecast);
        if (route is not null)
        {
            // The forecast consumes only the route. A centre rendezvous before
            // the target's deadline already guarantees an earlier radius contact;
            // avoid searching the whole lifetime for an unused encounter estimate.
            double centreArrival = route.StartMotionTimeMs + route.Segments.Sum(s => s.DurationMs);
            if (forecast && centreArrival <= expires &&
                (firstTargetContactMotionTimeMs is null || centreArrival < firstTargetContactMotionTimeMs))
                return new(route, null);
            CollisionPath Path(TorpedoRoute r) => new(t =>
            {
                var p = TorpedoGuidanceMath.PredictPose(r, t - r.StartMotionTimeMs);
                return (p.X, p.Y);
            }, r.Segments.Max(s => s.SpeedKmS) / 100);
            double searchEnd = forecast ? Math.Min(expires, firstTargetContactMotionTimeMs ?? expires) : expires;
            var contact = forecast ? FirstForecastContact(route, target, now, Math.Max(now, searchEnd)) :
                TorpedoCollisionMath.FirstContact(Path(route), Path(target), now, expires);
            if (contact is { } hit && (firstTargetContactMotionTimeMs is null || hit.MotionTimeMs < firstTargetContactMotionTimeMs))
                return new(route, hit.MotionTimeMs);
        }

        double segmentMs = Math.Min(100, remainingLifetimeMs);
        var aim = TorpedoGuidanceMath.PredictPose(target, now - target.StartMotionTimeMs + segmentMs);
        double bearing = Math.Atan2(aim.X - defender.X, -(aim.Y - defender.Y)) * 180 / Math.PI;
        double delta = (bearing - defender.Direction + 540) % 360 - 180;
        double turn = Math.Clamp(delta / (segmentMs / 1000), -turnRateDegPerSec, turnRateDegPerSec);
        var segment = new TorpedoRouteSegment(defender.X, defender.Y, defender.Direction, speedKmS, turn, segmentMs);
        return new(new(now, TorpedoGuidanceMath.PlannerVersion,
            turn == 0 ? TorpedoRoutePhase.Straight : TorpedoRoutePhase.Turning, false, [segment]), null);
    }

    // On a straight target leg, a tangent family's path length has unit gradient.
    // A slower target therefore gives a decreasing arrival-time residual. Require
    // each turn family to stay feasible (or unavailable) throughout the bracket,
    // so interpolation cannot jump over a branch discontinuity.
    private static bool MonotoneStraightBracket(ObjectMotionSnapshot origin, TorpedoRoute target, long now,
        double low, double high, double speed, double turnRate, double margin = 0)
    {
        double curveEnd = target.StartMotionTimeMs, time = curveEnd;
        double targetSpeed = target.Segments[^1].SpeedKmS;
        if (targetSpeed > speed * 0.9) return false;
        foreach (var segment in target.Segments)
        {
            if (segment.SpeedKmS != targetSpeed) return false;
            time += segment.DurationMs;
            if (segment.AngularVelocityDegPerSec != 0) curveEnd = time;
        }
        if (now + low < curveEnd) return false;
        var a = TorpedoGuidanceMath.PredictPose(target, now + low - target.StartMotionTimeMs);
        var b = TorpedoGuidanceMath.PredictPose(target, now + high - target.StartMotionTimeMs);
        double radius = speed * 10 / (turnRate * Math.PI / 180), heading = origin.Direction * Math.PI / 180;
        double dx = b.X - a.X, dy = b.Y - a.Y, squared = dx * dx + dy * dy;
        for (int sign = -1; sign <= 1; sign += 2)
        {
            double cx = origin.X + sign * radius * Math.Cos(heading), cy = origin.Y + sign * radius * Math.Sin(heading);
            double t = squared == 0 ? 0 : Math.Clamp(((cx - a.X) * dx + (cy - a.Y) * dy) / squared, 0, 1);
            double ex = a.X + t * dx - cx, ey = a.Y + t * dy - cy;
            if (ex * ex + ey * ey <= (radius + margin) * (radius + margin) * (1 + 1e-8))
            {
                double ax = a.X - cx, ay = a.Y - cy, bx = b.X - cx, by = b.Y - cy;
                double inner = Math.Max(0, radius - margin);
                if (Math.Max(ax * ax + ay * ay, bx * bx + by * by) >= inner * inner * (1 - 1e-8)) return false;
            }
        }
        return true;
    }

    private static bool CannotReachBeforeDeadline(ObjectMotionSnapshot origin, TorpedoRoute target, long now,
        double deadline, double speed, double turnRate)
    {
        double horizon = deadline - now;
        if (horizon <= 0) return false;
        double curveEnd = target.StartMotionTimeMs, time = curveEnd;
        foreach (var segment in target.Segments)
        {
            time += segment.DurationMs;
            if (segment.AngularVelocityDegPerSec != 0) curveEnd = time;
        }
        double straightFrom = Math.Max(0, curveEnd - now), radius = 5 + TorpedoCollisionMath.DistanceTolerance;
        if (straightFrom >= horizon) return false;
        if (straightFrom > 0)
        {
            var initial = TorpedoGuidanceMath.PredictPose(target, now - target.StartMotionTimeMs);
            double dx = initial.X - origin.X, dy = initial.Y - origin.Y;
            double relativeSpeed = (speed + target.Segments.Max(s => s.SpeedKmS)) / 100;
            if (Math.Sqrt(dx * dx + dy * dy) <= radius + relativeSpeed * straightFrom) return false;
        }
        if (!MonotoneStraightBracket(origin, target, now, straightFrom, horizon, speed, turnRate, radius)) return false;
        var destination = TorpedoGuidanceMath.PredictPose(target, deadline - target.StartMotionTimeMs);
        var arrival = TorpedoGuidanceMath.PlanStationaryPointForForecast(origin, destination.X, destination.Y, speed, turnRate, now);
        // With fixed turn-family feasibility on the whole target disc, shortest
        // path length is 1-Lipschitz. Even the radius allowance cannot make this
        // decreasing arrival residual reach zero before the deadline. Engine's
        // full rendezvous search would reject its contact and use the same coast.
        return arrival.HasIntercept && arrival.Segments.Sum(s => s.DurationMs) - horizon > radius * 100 / speed + 1e-5;
    }

    // Split at analytic arc/straight boundaries. Each interval has a valid
    // acceleration bound, and straight relative motion has a closed-form contact.
    // The authoritative planner keeps its original collision search unchanged.
    private static TorpedoContact? FirstForecastContact(TorpedoRoute projectile, TorpedoRoute target, double from, double to)
    {
        var cuts = new List<double> { from, to };
        AddCuts(projectile);
        AddCuts(target);
        cuts.Sort();
        for (int i = 1; i < cuts.Count; i++)
        {
            double a = cuts[i - 1], b = cuts[i];
            var hit = TorpedoCollisionMath.FirstForecastContact(Path(projectile, (a + b) / 2), Path(target, (a + b) / 2), a, b);
            if (hit is not null) return hit;
        }
        return null;

        void AddCuts(TorpedoRoute route)
        {
            double time = route.StartMotionTimeMs;
            foreach (var segment in route.Segments)
            {
                time += segment.DurationMs;
                if (time > from && time < to) cuts.Add(time);
            }
        }
        CollisionPath Path(TorpedoRoute route, double time)
        {
            double end = route.StartMotionTimeMs, acceleration = 0;
            foreach (var segment in route.Segments)
            {
                end += segment.DurationMs;
                if (end < time) continue;
                acceleration = Math.Abs(segment.SpeedKmS * segment.AngularVelocityDegPerSec) * Math.PI / 18_000_000;
                break;
            }
            return new(t =>
            {
                var p = TorpedoGuidanceMath.PredictPose(route, t - route.StartMotionTimeMs);
                return (p.X, p.Y);
            }, route.Segments.Max(s => s.SpeedKmS) / 100, acceleration);
        }
    }

    public static ObjectMotionSnapshot Predict(ObjectMotionSnapshot state, long elapsedMs)
    {
        var flight = state.Countermeasure!;
        double elapsed = Math.Max(0, flight.Route.ElapsedMs + elapsedMs);
        var pose = TorpedoGuidanceMath.PredictPose(flight.Route, elapsed);
        return state with
        {
            X = pose.X,
            Y = pose.Y,
            Direction = pose.Direction,
            SpeedKmS = flight.Route.Segments[^1].SpeedKmS,
            Countermeasure = flight with { Route = flight.Route with { ElapsedMs = elapsed } }
        };
    }
}
