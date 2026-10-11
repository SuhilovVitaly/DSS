using DeepSpaceSaga.Contracts;
namespace DeepSpaceSaga.Motion;

public sealed record CountermeasurePlan(TorpedoRoute Route, double? PredictedEncounterMotionTimeMs);

/// <summary>Predictive interception of the confirmed curved projectile trajectory, from the carrier centre.</summary>
public static class CountermeasureGuidanceMath
{
    public static TorpedoRoute? Plan(ObjectMotionSnapshot defender, TorpedoRoute target, long now,
        double firstHullContactMotionTimeMs, double speedKmS = 12, double turnRateDegPerSec = 90,
        double? expiresAtMotionTimeMs = null)
    {
        double deadline = Math.Min(firstHullContactMotionTimeMs, expiresAtMotionTimeMs ?? firstHullContactMotionTimeMs);
        double horizon = deadline - now;
        if (!double.IsFinite(horizon) || horizon <= 0) return null;
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
                    double mid = (low + high) / 2;
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
                var contact = TorpedoCollisionMath.FirstContact(Path(boundaryRoute), Path(target), now, deadline);
                return contact is { } hit && hit.MotionTimeMs < firstHullContactMotionTimeMs ? boundaryRoute : null;
            }
            low = high;
            previous = value;
        }

        double Residual(double ms, out TorpedoRoute route)
        {
            var point = TorpedoGuidanceMath.PredictPose(target, now - target.StartMotionTimeMs + ms);
            route = TorpedoGuidanceMath.Plan(defender,
                new("intercept-point", point.X, point.Y, 0, 0), speedKmS, turnRateDegPerSec, now);
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
        double? firstTargetContactMotionTimeMs = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(remainingLifetimeMs);
        if (!double.IsFinite(speedKmS) || speedKmS <= 0) throw new ArgumentOutOfRangeException(nameof(speedKmS));
        if (!double.IsFinite(turnRateDegPerSec) || turnRateDegPerSec <= 0) throw new ArgumentOutOfRangeException(nameof(turnRateDegPerSec));
        double expires = now + (double)remainingLifetimeMs;
        var route = Plan(defender, target, now, Math.BitIncrement(expires), speedKmS, turnRateDegPerSec, expires);
        if (route is not null)
        {
            CollisionPath Path(TorpedoRoute r) => new(t =>
            {
                var p = TorpedoGuidanceMath.PredictPose(r, t - r.StartMotionTimeMs);
                return (p.X, p.Y);
            }, r.Segments.Max(s => s.SpeedKmS) / 100);
            var contact = TorpedoCollisionMath.FirstContact(Path(route), Path(target), now, expires);
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
