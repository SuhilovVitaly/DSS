using DeepSpaceSaga.Contracts;
namespace DeepSpaceSaga.Motion;

/// <summary>Predictive interception of the confirmed curved projectile trajectory, from the carrier centre.</summary>
public static class CountermeasureGuidanceMath
{
    public static TorpedoRoute? Plan(ObjectMotionSnapshot defender, TorpedoRoute target, long now,
        double firstHullContactMotionTimeMs, double speedKmS = 12, double turnRateDegPerSec = 90)
    {
        double horizon = firstHullContactMotionTimeMs - now;
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
                var contact = TorpedoCollisionMath.FirstContact(Path(boundaryRoute), Path(target), now, firstHullContactMotionTimeMs);
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
