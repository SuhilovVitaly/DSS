using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion;

/// <summary>Constant-speed arc/straight interception of a target centre. No terminal heading constraint.</summary>
public static class TorpedoGuidanceMath
{
    public const int PlannerVersion = 1;
    private const double Radians = Math.PI / 180;

    public static TorpedoRoute Plan(ObjectMotionSnapshot projectilePose, ObjectMotionSnapshot targetPose,
        double speedKmS, double turnRateDegPerSec, long startMotionTimeMs = 0)
    {
        if (!double.IsFinite(speedKmS) || speedKmS <= 0)
            throw new ArgumentOutOfRangeException(nameof(speedKmS));
        if (!double.IsFinite(turnRateDegPerSec) || turnRateDegPerSec <= 0)
            throw new ArgumentOutOfRangeException(nameof(turnRateDegPerSec));

        double bestTime = double.PositiveInfinity;
        ImmutableArray<TorpedoRouteSegment> best = default;
        // Search the two single-turn families in canonical order. The residual is
        // collinearity of the relative position and relative velocity at turn exit.
        // Its domain is one revolution, not a flight-time/range limit.
        foreach (int sign in new[] { -1, 1 })
        {
            double previousAngle = 0;
            double previous = Evaluate(0, sign, false);
            Evaluate(0, sign, true);
            for (int i = 1; i <= 720; i++)
            {
                double angle = i * .5;
                double value = Evaluate(angle, sign, false);
                if (value == 0) Evaluate(angle, sign, true);
                if (Math.Sign(value) != Math.Sign(previous))
                {
                    double low = previousAngle, high = angle, lowValue = previous;
                    for (int iteration = 0; iteration < 60; iteration++)
                    {
                        double mid = (low + high) / 2;
                        double residual = Evaluate(mid, sign, false);
                        if (Math.Sign(residual) == Math.Sign(lowValue))
                        {
                            low = mid;
                            lowValue = residual;
                        }
                        else high = mid;
                    }
                    Evaluate((low + high) / 2, sign, true);
                }
                previousAngle = angle;
                previous = value;
            }
        }

        if (!best.IsDefault)
            return new(startMotionTimeMs, PlannerVersion,
                best[0].DurationMs > 0 && best[0].AngularVelocityDegPerSec != 0
                    ? TorpedoRoutePhase.Turning : TorpedoRoutePhase.Straight, true, best);

        // Receding-horizon pursuit. Engine replans at fixed physical boundaries;
        // extrapolation continues along the final tangent without stopping.
        var aim = ApproachPursuitMath.ExtrapolatePosition(targetPose.X, targetPose.Y,
            targetPose.Direction, targetPose.SpeedKmS, 1000);
        double bearing = Math.Atan2(aim.X - projectilePose.X, -(aim.Y - projectilePose.Y)) / Radians;
        double delta = (bearing - projectilePose.Direction + 540) % 360 - 180;
        double turn = Math.CopySign(turnRateDegPerSec, delta);
        var pursuit = new TorpedoRouteSegment(projectilePose.X, projectilePose.Y,
            projectilePose.Direction, speedKmS, turn, Math.Min(100, Math.Abs(delta) / turnRateDegPerSec * 1000));
        return new(startMotionTimeMs, PlannerVersion, TorpedoRoutePhase.Turning, false, [pursuit]);

        double Evaluate(double angle, int sign, bool accept)
        {
            double turnMs = angle / turnRateDegPerSec * 1000;
            var arc = new TorpedoRouteSegment(projectilePose.X, projectilePose.Y,
                projectilePose.Direction, speedKmS, sign * turnRateDegPerSec, turnMs);
            var exit = PredictSegment(arc, turnMs);
            var target = PredictSegment(new(targetPose.X, targetPose.Y,
                targetPose.Direction, targetPose.SpeedKmS, 0, turnMs), turnMs);
            double dx = target.X - exit.X, dy = target.Y - exit.Y;
            double wx = 10 * (targetPose.SpeedKmS * Math.Sin(targetPose.Direction * Radians) - speedKmS * Math.Sin(exit.Direction * Radians));
            double wy = 10 * (-targetPose.SpeedKmS * Math.Cos(targetPose.Direction * Radians) + speedKmS * Math.Cos(exit.Direction * Radians));
            double residual = dx * wy - dy * wx;
            if (!accept) return residual;
            double squared = wx * wx + wy * wy;
            double straightSeconds = squared == 0 ? 0 : -(dx * wx + dy * wy) / squared;
            double error = Math.Sqrt(Math.Pow(dx + wx * straightSeconds, 2) + Math.Pow(dy + wy * straightSeconds, 2));
            double total = turnMs + straightSeconds * 1000;
            if (straightSeconds >= 0 && double.IsFinite(total) && total < bestTime &&
                error <= 1e-6 + 1e-12 * Math.Max(Math.Abs(dx), Math.Abs(dy)))
            {
                bestTime = total;
                best = [arc, new(exit.X, exit.Y, exit.Direction, speedKmS, 0, straightSeconds * 1000)];
            }
            return residual;
        }
    }

    /// <summary>Milliseconds from plan origin; after its endpoint, continue at constant speed.</summary>
    public static (double X, double Y, double Direction) PredictPose(TorpedoRoute route, double elapsedMs)
    {
        if (route.Segments.IsDefaultOrEmpty) throw new ArgumentException("Route requires geometry.", nameof(route));
        double remaining = Math.Max(0, elapsedMs);
        foreach (var segment in route.Segments)
        {
            if (remaining <= segment.DurationMs) return PredictSegment(segment, remaining);
            remaining -= segment.DurationMs;
        }
        var last = route.Segments[^1];
        var end = PredictSegment(last, last.DurationMs);
        return PredictSegment(new(end.X, end.Y, end.Direction, last.SpeedKmS, 0, remaining), remaining);
    }

    public static (double X, double Y, double Direction) PredictSegment(TorpedoRouteSegment segment, double elapsedMs)
    {
        double seconds = elapsedMs / 1000, heading = segment.Direction * Radians;
        double omega = segment.AngularVelocityDegPerSec * Radians, speed = segment.SpeedKmS * 10;
        double end = heading + omega * seconds;
        double x = segment.X + (omega == 0 ? speed * seconds * Math.Sin(heading) : speed / omega * (Math.Cos(heading) - Math.Cos(end)));
        double y = segment.Y + (omega == 0 ? -speed * seconds * Math.Cos(heading) : speed / omega * (Math.Sin(heading) - Math.Sin(end)));
        return (x, y, ((segment.Direction + segment.AngularVelocityDegPerSec * seconds) % 360 + 360) % 360);
    }

    public static ObjectMotionSnapshot Predict(ObjectMotionSnapshot state, long elapsedMs)
    {
        var torpedo = state.Torpedo!;
        double elapsed = Math.Max(0, torpedo.Route.ElapsedMs + elapsedMs);
        var pose = PredictPose(torpedo.Route, elapsed);
        double remaining = elapsed;
        var phase = TorpedoRoutePhase.Straight;
        foreach (var segment in torpedo.Route.Segments)
        {
            if (remaining < segment.DurationMs)
            {
                phase = segment.AngularVelocityDegPerSec == 0 ? TorpedoRoutePhase.Straight : TorpedoRoutePhase.Turning;
                break;
            }
            remaining -= segment.DurationMs;
        }
        return state with
        {
            X = pose.X,
            Y = pose.Y,
            Direction = pose.Direction,
            SpeedKmS = torpedo.SpeedKmS,
            Torpedo = torpedo with { Route = torpedo.Route with { ElapsedMs = elapsed, Phase = phase } }
        };
    }
}
