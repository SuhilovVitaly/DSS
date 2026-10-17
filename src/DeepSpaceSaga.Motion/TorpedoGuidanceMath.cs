using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion;

/// <summary>Constant-speed arc/straight interception of a target centre. No terminal heading constraint.</summary>
public static class TorpedoGuidanceMath
{
    public const int PlannerVersion = 1;
    private const double Radians = Math.PI / 180;
    private static readonly int[] TurnSigns = [-1, 1];
    private readonly record struct GuidancePose(double X, double Y, double SpeedKmS, double Direction);

    public static TorpedoRoute Plan(ObjectMotionSnapshot projectilePose, ObjectMotionSnapshot targetPose,
        double speedKmS, double turnRateDegPerSec, long startMotionTimeMs = 0)
        => Plan(new GuidancePose(projectilePose.X, projectilePose.Y, projectilePose.SpeedKmS, projectilePose.Direction),
            new GuidancePose(targetPose.X, targetPose.Y, targetPose.SpeedKmS, targetPose.Direction), speedKmS, turnRateDegPerSec, startMotionTimeMs);

    internal static TorpedoRoute PlanStationaryPoint(ObjectMotionSnapshot projectilePose, double x, double y,
        double speedKmS, double turnRateDegPerSec, long startMotionTimeMs)
        => Plan(new GuidancePose(projectilePose.X, projectilePose.Y, projectilePose.SpeedKmS, projectilePose.Direction),
            new GuidancePose(x, y, 0, 0), speedKmS, turnRateDegPerSec, startMotionTimeMs);

    private static TorpedoRoute Plan(GuidancePose projectilePose, GuidancePose targetPose,
        double speedKmS, double turnRateDegPerSec, long startMotionTimeMs)
    {
        if (!double.IsFinite(speedKmS) || speedKmS <= 0)
            throw new ArgumentOutOfRangeException(nameof(speedKmS));
        if (!double.IsFinite(turnRateDegPerSec) || turnRateDegPerSec <= 0)
            throw new ArgumentOutOfRangeException(nameof(turnRateDegPerSec));

        if (targetPose.SpeedKmS == 0 && TryStationaryTarget(projectilePose, targetPose,
            speedKmS, turnRateDegPerSec, startMotionTimeMs) is { } stationary) return stationary;

        double bestTime = double.PositiveInfinity;
        ImmutableArray<TorpedoRouteSegment> best = default;
        // Search the two single-turn families in canonical order. The residual is
        // collinearity of the relative position and relative velocity at turn exit.
        // Its domain is one revolution, not a flight-time/range limit.
        foreach (int sign in TurnSigns)
        {
            Evaluate(0, sign, true);
            bool largeCoordinates = Math.Max(Math.Max(Math.Abs(projectilePose.X), Math.Abs(projectilePose.Y)),
                Math.Max(Math.Abs(targetPose.X), Math.Abs(targetPose.Y))) > 1e8;
            if (largeCoordinates)
            {
                double previous = Evaluate(0, sign, false);
                for (int i = 1; i <= 720; i++)
                {
                    double value = Evaluate(i * .5, sign, false);
                    Bracket(i - 1, i, previous, value);
                    previous = value;
                }
            }
            else Scan(0, 720, Evaluate(0, sign, false), Evaluate(360, sign, false));

            void Bracket(int from, int to, double previous, double value)
            {
                double angle = to * .5;
                if (value == 0) Evaluate(angle, sign, true);
                if (Math.Sign(value) != Math.Sign(previous))
                {
                    double low = from * .5, high = angle, lowValue = previous;
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
            }

            void Scan(int from, int to, double left, double right)
            {
                // No nonnegative straight leg can improve a candidate whose
                // entire arrival precedes the beginning of this turn bracket.
                if (from * .5 / turnRateDegPerSec * 1000 > bestTime + 1e-6) return;
                if (to - from == 1) { Bracket(from, to, left, right); return; }
                int middle = (from + to) / 2;
                double angle = middle * .5;
                double value = Evaluate(angle, sign, false);
                if (left != 0 && right != 0 && Math.Sign(left) == Math.Sign(right) && Math.Sign(value) == Math.Sign(left))
                {
                    double ms = angle / turnRateDegPerSec * 1000;
                    var p = PredictSegment(projectilePose.X, projectilePose.Y, projectilePose.Direction, speedKmS, sign * turnRateDegPerSec, ms);
                    var q = PredictSegment(targetPose.X, targetPose.Y, targetPose.Direction, targetPose.SpeedKmS, 0, ms);
                    double halfWidth = Math.Max(angle - from * .5, to * .5 - angle);
                    double relativeDistance = Math.Sqrt(Math.Pow(q.X - p.X, 2) + Math.Pow(q.Y - p.Y, 2));
                    double distanceBound = relativeDistance + (speedKmS + targetPose.SpeedKmS) * 10 * halfWidth / turnRateDegPerSec;
                    // d cross(relative position, relative velocity)/d angle =
                    // cross(relative position, -d projectile velocity/d angle).
                    // Excluding a zero by this Lipschitz bound safely skips whole
                    // half-degree brackets; retained brackets use the old operations.
                    double bound = distanceBound * speedKmS * 10 * Radians * halfWidth;
                    // The chord error is bounded by max |F''| * width² / 8.
                    // Here r'=w/turnRate, w'=-sign*Radians*J(v),
                    // w''=Radians²*v, so this also excludes shallow roots safely.
                    double secondDerivative = speedKmS * 10 *
                        ((speedKmS + targetPose.SpeedKmS) * 10 / turnRateDegPerSec * Radians +
                         distanceBound * Radians * Radians);
                    double width = (to - from) * .5;
                    double chordError = secondDerivative * width * width / 8;
                    if (Math.Min(Math.Abs(left), Math.Abs(right)) > chordError * (1 + 1e-10) + 1e-6) return;
                    if (Math.Abs(value) > bound * (1 + 1e-10) + 1e-7) return;
                }
                Scan(from, middle, left, value);
                Scan(middle, to, value, right);
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
            var exit = PredictSegment(projectilePose.X, projectilePose.Y,
                projectilePose.Direction, speedKmS, sign * turnRateDegPerSec, turnMs);
            var target = PredictSegment(targetPose.X, targetPose.Y,
                targetPose.Direction, targetPose.SpeedKmS, 0, turnMs);
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
                best = [new(projectilePose.X, projectilePose.Y, projectilePose.Direction, speedKmS,
                    sign * turnRateDegPerSec, turnMs), new(exit.X, exit.Y, exit.Direction, speedKmS, 0, straightSeconds * 1000)];
            }
            return residual;
        }
    }

    /// <summary>Equivalent tangent construction for a fixed aim point. Countermeasure
    /// bracketing asks this question repeatedly; no angular search is necessary.</summary>
    private static TorpedoRoute? TryStationaryTarget(GuidancePose origin, GuidancePose target,
        double speed, double turnRate, long start)
    {
        double radius = speed * 10 / (turnRate * Radians);
        double heading = origin.Direction * Radians;
        double bestTime = double.PositiveInfinity;
        ImmutableArray<TorpedoRouteSegment> best = default;
        foreach (int sign in TurnSigns)
        {
            double cx = origin.X + sign * radius * Math.Cos(heading);
            double cy = origin.Y + sign * radius * Math.Sin(heading);
            double dx = target.X - cx, dy = target.Y - cy;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            // Degenerate/tangent roots retain the original numerical search exactly.
            if (distance <= radius * (1 + 1e-10)) return null;
            double bearing = Math.Atan2(dy, dx);
            double offset = Math.Acos(-sign * radius / distance);
            double first = Angle(bearing - offset), second = Angle(bearing + offset);
            if (first > second) (first, second) = (second, first);
            for (int candidate = 0; candidate < 2; candidate++)
            {
                double angle = candidate == 0 ? first : second;
                if (angle < 1e-8 || 360 - angle < 1e-8 || Math.Abs(angle * 2 - Math.Round(angle * 2)) < 1e-8) return null;
                double roughMs = angle / turnRate * 1000;
                var rough = PredictSegment(origin.X, origin.Y, origin.Direction, speed, sign * turnRate, roughMs);
                if ((target.X - rough.X) * Math.Sin(rough.Direction * Radians) -
                    (target.Y - rough.Y) * Math.Cos(rough.Direction * Radians) < -1e-6) continue;
                // Use precisely the old half-degree bracket and bisection operations.
                // This preserves the route doubles (and event ordering), not merely
                // an approximately equal geometric tangent.
                double low = Math.Floor(angle * 2) / 2, high = low + .5;
                double lowValue = Residual(low);
                if (Math.Sign(lowValue) == Math.Sign(Residual(high))) return null;
                for (int iteration = 0; iteration < 60; iteration++)
                {
                    double mid = (low + high) / 2;
                    double residual = Residual(mid);
                    if (Math.Sign(residual) == Math.Sign(lowValue)) { low = mid; lowValue = residual; }
                    else high = mid;
                }
                double turnMs = ((low + high) / 2) / turnRate * 1000;
                var arc = new TorpedoRouteSegment(origin.X, origin.Y, origin.Direction, speed, sign * turnRate, turnMs);
                var exit = PredictSegment(arc, turnMs);
                double wx = 10 * (0 - speed * Math.Sin(exit.Direction * Radians));
                double wy = 10 * (0 + speed * Math.Cos(exit.Direction * Radians));
                double ex = target.X - exit.X, ey = target.Y - exit.Y;
                double squared = wx * wx + wy * wy;
                double straightSeconds = squared == 0 ? 0 : -(ex * wx + ey * wy) / squared;
                double error = Math.Sqrt(Math.Pow(ex + wx * straightSeconds, 2) + Math.Pow(ey + wy * straightSeconds, 2));
                double total = turnMs + straightSeconds * 1000;
                if (straightSeconds < 0 || !double.IsFinite(total) || total >= bestTime ||
                    error > 1e-6 + 1e-12 * Math.Max(Math.Abs(ex), Math.Abs(ey))) continue;
                bestTime = total;
                best = [arc, new(exit.X, exit.Y, exit.Direction, speed, 0, straightSeconds * 1000)];
            }

            double Residual(double angle)
            {
                double ms = angle / turnRate * 1000;
                var exit = PredictSegment(origin.X, origin.Y, origin.Direction, speed, sign * turnRate, ms);
                double ex = target.X - exit.X, ey = target.Y - exit.Y;
                double wx = 10 * (0 - speed * Math.Sin(exit.Direction * Radians));
                double wy = 10 * (0 + speed * Math.Cos(exit.Direction * Radians));
                return ex * wy - ey * wx;
            }
            double Angle(double exitHeading) =>
                (((sign * (exitHeading - heading) % (2 * Math.PI)) + 2 * Math.PI) % (2 * Math.PI)) / Radians;
        }
        return best.IsDefault ? null : new(start, PlannerVersion, TorpedoRoutePhase.Turning, true, best);
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
        return PredictSegment(end.X, end.Y, end.Direction, last.SpeedKmS, 0, remaining);
    }

    public static (double X, double Y, double Direction) PredictSegment(TorpedoRouteSegment segment, double elapsedMs)
        => PredictSegment(segment.X, segment.Y, segment.Direction, segment.SpeedKmS, segment.AngularVelocityDegPerSec, elapsedMs);

    private static (double X, double Y, double Direction) PredictSegment(double startX, double startY,
        double direction, double speedKmS, double angularVelocity, double elapsedMs)
    {
        double seconds = elapsedMs / 1000, heading = direction * Radians;
        double omega = angularVelocity * Radians, speed = speedKmS * 10;
        double end = heading + omega * seconds;
        double x = startX + (omega == 0 ? speed * seconds * Math.Sin(heading) : speed / omega * (Math.Cos(heading) - Math.Cos(end)));
        double y = startY + (omega == 0 ? -speed * seconds * Math.Cos(heading) : speed / omega * (Math.Sin(heading) - Math.Sin(end)));
        return (x, y, ((direction + angularVelocity * seconds) % 360 + 360) % 360);
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
