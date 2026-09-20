namespace DeepSpaceSaga.Motion;

/// <summary>
/// Result of one `navigation.approach` pursuit step: the freshly recomputed aim point
/// (trailing behind the target along its current heading), whether the ship has reached
/// it, the ship's new direction after this step's clamped turn, and the course lock (if
/// any) to pass back into the next call.
/// </summary>
/// <param name="AimPointX">Recomputed aim point X, world units.</param>
/// <param name="AimPointY">Recomputed aim point Y, world units.</param>
/// <param name="IsArrived">Ship reached (or swept through) the aim point this step.</param>
/// <param name="NewDirectionDegrees">Ship direction after this step's clamped turn.</param>
/// <param name="LockedCourseDegrees">
/// When non-null the ship has aligned onto a straight-line course toward the aim point
/// and this course should be held (not re-derived from possibly-noisy geometry) on the
/// next call — pass it back as <c>lockedCourseDegrees</c>. Unlike
/// <see cref="NavigationWaypointMath"/>'s Orbit lock, this is NOT permanent: the caller
/// re-passes the value each call, and <see cref="Step"/> itself drops (re-derives) the
/// lock as soon as the freshly recomputed bearing to the (possibly-moved) aim point
/// drifts meaningfully away from it — because for Approach the aim point can genuinely
/// keep moving as the target moves. Null once arrived or while not yet aligned.
/// </param>
public readonly record struct ApproachStepResult(
    double AimPointX,
    double AimPointY,
    bool IsArrived,
    double NewDirectionDegrees,
    double? LockedCourseDegrees = null);

public readonly record struct ApproachFlyThroughPlan(
    string Type,
    double FirstRemainingUnits,
    double SecondRemainingUnits,
    double ThirdRemainingUnits)
{
    public double RemainingUnits =>
        FirstRemainingUnits + SecondRemainingUnits + ThirdRemainingUnits;
}

public readonly record struct ApproachFlyThroughPlanStep(
    bool IsArrived,
    double NewDirectionDegrees,
    ApproachFlyThroughPlan RemainingPlan);

/// <summary>
/// Result of <see cref="ApproachPursuitMath.SolveInterceptFlyThroughPlan"/>: the earliest
/// physically achievable rendezvous with a target assumed to hold its current
/// heading/speed for the whole search horizon (story-20260829-210641.md §4). When
/// <see cref="HasIntercept"/> is false, none of the 6 Dubins curve types have a valid
/// root (t* &gt; 0) within the search horizon — the same "no achievable rendezvous"
/// outcome for which callers fall back to a shortest route to the captured pose.
/// </summary>
/// <param name="HasIntercept">Whether a valid rendezvous was found.</param>
/// <param name="Type">3-letter Dubins curve type ("LSL", "RSR", "LSR", "RSL", "RLR", "LRL") of the winning curve, or empty when <see cref="HasIntercept"/> is false.</param>
/// <param name="InterceptTimeSeconds">Time (seconds) at which the ship's curve arrives exactly where the target will be, assuming the target holds its current course/speed.</param>
/// <param name="TargetXAtIntercept">Target X at <see cref="InterceptTimeSeconds"/>, world units.</param>
/// <param name="TargetYAtIntercept">Target Y at <see cref="InterceptTimeSeconds"/>, world units.</param>
/// <param name="TargetDirectionAtIntercept">Target heading at <see cref="InterceptTimeSeconds"/> — equal to the input heading, since the target's course is assumed constant over the search horizon.</param>
/// <param name="Plan">The fly-through plan (same shape <see cref="ApproachPursuitMath.CreateFlyThroughPlan"/> produces) built directly to the rendezvous pose.</param>
public readonly record struct ApproachInterceptSolution(
    bool HasIntercept,
    string Type,
    double InterceptTimeSeconds,
    double TargetXAtIntercept,
    double TargetYAtIntercept,
    double TargetDirectionAtIntercept,
    ApproachFlyThroughPlan Plan)
{
    /// <summary>The canonical "no achievable rendezvous" result.</summary>
    public static readonly ApproachInterceptSolution None = new(false, string.Empty, 0, 0, 0, 0, default);
}

/// <summary>
/// Pure, deterministic trailing-pursuit steering math for `navigation.approach`
/// (shared by Engine and Client — no state, no Engine/Contracts references, only
/// numbers). Unlike <see cref="NavigationWaypointMath.StagedStep"/> (Orbit), this
/// never locks a PERMANENT course: the aim point itself moves as the target moves,
/// so the target's freshly-passed-in current position/direction is always re-read.
///
/// Model: aimPoint = targetPosition − trailDistanceWorldUnits × unitVector(targetDirection).
/// The ship steers toward the aim point using the same turn-clamp convention as
/// <see cref="NavigationWaypointMath"/> (shortest signed angle, clamped to
/// turnStepDegrees per step). Arrival uses a closest-point-on-the-travelled-segment
/// test (mirroring <see cref="NavigationWaypointMath.CheckSegmentArrival"/>) rather
/// than a single end-of-step point sample, so a fast ship cannot tunnel through a
/// small tolerance ring within one step.
///
/// Anti-circling stabilization (Post-implementation bug fix #2, story-20260827-083137.md):
/// once the ship's heading is within tolerance of the bearing to the aim point, <see
/// cref="Step"/> holds that heading (via the caller-threaded, cycle-scoped
/// <c>lockedCourseDegrees</c> parameter) instead of re-deriving a slightly different
/// bearing from tiny geometric noise every call — the same stabilization
/// <see cref="NavigationWaypointMath.HoldLockedCourse"/> already uses for Orbit,
/// including its dot-product "aim point fallen behind the ship" arrival safeguard.
/// The crucial difference from Orbit: this lock is NOT permanent — <see cref="Step"/>
/// itself drops and re-derives it as soon as the live aim point drifts meaningfully
/// away from the held course, since (unlike Orbit's fixed point) the aim point can
/// genuinely keep moving as the target moves.
///
/// Direction convention: degrees, 0° = up, 90° = right, clockwise.
/// Speed convention: km/s (1 km/s = 10 world units/s, since 1 world unit = 100 m).
/// </summary>
public static class ApproachPursuitMath
{
    public const string TrailPhase = "Trail";
    public const string FinalPhase = "Final";
    public const string FlyThroughPendingPhase = "FlyThroughPending";
    public const string FlyThroughPhasePrefix = "FlyThrough:";

    /// <summary>
    /// Phase prefix used instead of <see cref="FlyThroughPhasePrefix"/> when the
    /// fly-through plan was built by a CONFIRMED <see cref="SolveInterceptFlyThroughPlan"/>
    /// rendezvous solve rather than a fallback curve to the target's captured pose
    /// (story-20260829-210641.md §10, Checkpoint 2). Both Engine and Client branch points
    /// must read this constant rather than duplicating the string literal, to avoid a
    /// client/server desync on which phase means what.
    /// </summary>
    public const string FlyThroughInterceptPhasePrefix = "FlyThroughIntercept:";

    /// <summary>
    /// Default distance (world units) at or below which the ship is considered to have
    /// arrived at the aim point. This is a tuning default (500 m), not a hard
    /// requirement — callers may pass a different value if content ever needs to.
    /// </summary>
    public const double ArrivalToleranceUnits = 5.0;

    private const double UnitsPerKmS = 10.0; // 1 km/s → 10 world units/s.

    public static bool IsFlyThroughPhase(string? phase) =>
        phase == FlyThroughPendingPhase ||
        (phase?.StartsWith(FlyThroughPhasePrefix, StringComparison.Ordinal) ?? false) ||
        (phase?.StartsWith(FlyThroughInterceptPhasePrefix, StringComparison.Ordinal) ?? false);

    public static ApproachFlyThroughPlan CreateFlyThroughPlan(
        double shipX,
        double shipY,
        double shipDirectionDegrees,
        double shipSpeedKmS,
        double targetX,
        double targetY,
        double targetDirectionDegrees,
        int angularInertiaDegPerSec)
    {
        double directDistance = Math.Sqrt(
            (targetX - shipX) * (targetX - shipX) +
            (targetY - shipY) * (targetY - shipY));
        if (shipSpeedKmS <= 0 || angularInertiaDegPerSec <= 0)
            return new ApproachFlyThroughPlan("SSS", 0, directDistance, 0);

        double angularVelocityRadPerSec = angularInertiaDegPerSec * Math.PI / 180.0;
        double turnRadius = shipSpeedKmS * UnitsPerKmS / angularVelocityRadPerSec;

        // Convert screen coordinates/headings to Cartesian coordinates/yaw.
        double dx = targetX - shipX;
        double dy = -(targetY - shipY);
        double normalizedDistance = directDistance / turnRadius;
        double theta = Mod2Pi(Math.Atan2(dy, dx));
        double alpha = Mod2Pi((90.0 - shipDirectionDegrees) * Math.PI / 180.0 - theta);
        double beta = Mod2Pi((90.0 - targetDirectionDegrees) * Math.PI / 180.0 - theta);

        var shortest = (Type: "SSS", First: 0.0, Second: normalizedDistance, Third: 0.0);
        double bestLength = double.PositiveInfinity;
        foreach (string type in AllCurveTypes)
        {
            if (!TryEvaluateCurveType(type, alpha, beta, normalizedDistance, out double first, out double second, out double third))
                continue;
            double length = first + second + third;
            // Keep canonical family order for numerical ties, in world units.
            if (length < bestLength - 1e-6 / turnRadius)
            {
                bestLength = length;
                shortest = (type, first, second, third);
            }
        }
        return new ApproachFlyThroughPlan(
            shortest.Type,
            shortest.First * turnRadius,
            shortest.Second * turnRadius,
            shortest.Third * turnRadius);
    }

    /// <summary>
    /// All 6 Dubins curve types considered by <see cref="CreateFlyThroughPlan"/> and
    /// <see cref="SolveInterceptFlyThroughPlan"/>, in the same order both already use.
    /// </summary>
    private static readonly string[] AllCurveTypes = { "LSL", "RSR", "LSR", "RSL", "RLR", "LRL" };

    internal static ReadOnlySpan<string> CurveTypes => AllCurveTypes;

    /// <summary>
    /// Builds one named Dubins family without applying the arg-min selection used by
    /// <see cref="CreateFlyThroughPlan"/>. LineCapture uses this to compare every family
    /// against the same finite aft-line objective.
    /// </summary>
    internal static bool TryCreateLineCurvePlan(
        double along,
        double cross,
        double headingRadians,
        double turnRadius,
        double endpoint,
        string curveType,
        out ApproachFlyThroughPlan plan)
    {
        plan = default;
        // Stay in the target frame during the search. Roundoff from repeatedly
        // rotating almost-coincident world positions can otherwise create a loop.
        double dx = (endpoint - along) / turnRadius, dy = cross / turnRadius;
        if (Math.Abs(dy) < 1e-12)
            dy = 0;
        double normalizedDistance = Math.Sqrt(dx * dx + dy * dy);
        double theta = Mod2Pi(Math.Atan2(dy, dx));
        double alpha = Mod2Pi(headingRadians - theta);
        double beta = Mod2Pi(-theta);
        if (!TryEvaluateCurveType(curveType, alpha, beta, normalizedDistance,
                out double first, out double second, out double third))
            return false;

        plan = new ApproachFlyThroughPlan(
            curveType,
            first * turnRadius,
            second * turnRadius,
            third * turnRadius);
        return true;
    }

    // Geometry is normalized by the turning radius. In the target frame +X is
    // forward, +Y is Cartesian left; cross is the screen-coordinate right offset.
    // A fixed family can change branch only when a tangent/arc vanishes, the two
    // turning circles coincide, or a tangent/three-arc construction becomes invalid.
    // Between these points the length formulas are smooth.
    internal static int GetCurveBreakpoints(
        double along, double cross, double headingRadians, double radius,
        string type, double lower, double upper, double weight, Span<double> points)
    {
        int count = 0;
        AddPoint(points, ref count, lower, lower, upper);
        AddPoint(points, ref count, upper, lower, upper);
        double a = along / radius, y = -cross / radius;
        double s = type[0] == 'L' ? 1 : -1;
        double e = type[2] == 'L' ? 1 : -1;
        double cx = a - s * Math.Sin(headingRadians);
        double cy = y + s * Math.Cos(headingRadians);
        double v = e - cy;
        AddPoint(points, ref count, cx * radius, lower, upper);

        if (type[1] == 'S')
        {
            double shift = e - s;
            AddCircleCrossings(points, ref count, cx, v, Math.Abs(shift), radius, lower, upper);
            // First/last turn wraps when the tangent heading equals the start/end heading.
            AddTangent(points, ref count, headingRadians, cx, v, shift, radius, lower, upper);
            AddTangent(points, ref count, 0, cx, v, shift, radius, lower, upper);
            // L'(x) = cos(tangent heading). Stationary points of weight*L(x)-x.
            if (weight > 1)
            {
                double phi = Math.Acos(1 / weight);
                AddTangent(points, ref count, phi, cx, v, shift, radius, lower, upper);
                AddTangent(points, ref count, -phi, cx, v, shift, radius, lower, upper);
            }
        }
        else
        {
            AddCircleCrossings(points, ref count, cx, v, 4, radius, lower, upper);
            // A zero first turn places the middle circle opposite the start circle.
            double mx = a + s * Math.Sin(headingRadians);
            double my = y - s * Math.Cos(headingRadians);
            AddCircleCrossings(points, ref count, mx, e - my, 2, radius, lower, upper);
            // A zero last turn places it opposite the end circle.
            AddCircleCrossings(points, ref count, cx, -e - cy, 2, radius, lower, upper);

            // With u=x-cx, d²=u²+v², L'=-4u/(d*sqrt(16-d²)).
            // weight*L'=1 reduces to a quadratic in z=u², with u<0.
            double v2 = v * v;
            if (v2 <= 16)
            {
                double b = 2 * v2 + 16 * (weight * weight - 1);
                double c = v2 * (v2 - 16);
                double discriminant = b * b - 4 * c;
                double sqrt = Math.Sqrt(Math.Max(0, discriminant));
                double z = b >= 0 && b + sqrt > 0 ? -2 * c / (b + sqrt) : (-b + sqrt) / 2;
                if (z >= 0)
                    AddPoint(points, ref count, (cx - Math.Sqrt(z)) * radius, lower, upper);
            }
        }

        points[..count].Sort();
        int unique = 0;
        for (int i = 0; i < count; i++)
            if (unique == 0 || points[i] != points[unique - 1])
                points[unique++] = points[i];
        return unique;
    }

    private static void AddPoint(Span<double> points, ref int count, double x, double lower, double upper)
    {
        if (double.IsFinite(x) && x >= lower && x <= upper)
            points[count++] = x;
    }

    private static void AddCircleCrossings(Span<double> points, ref int count,
        double center, double vertical, double distance, double radius, double lower, double upper)
    {
        double squared = distance * distance - vertical * vertical;
        if (squared < 0)
            return;
        double offset = Math.Sqrt(squared);
        AddPoint(points, ref count, (center - offset) * radius, lower, upper);
        AddPoint(points, ref count, (center + offset) * radius, lower, upper);
    }

    private static void AddTangent(Span<double> points, ref int count,
        double phi, double cx, double v, double shift, double radius, double lower, double upper)
    {
        double sin = Math.Sin(phi), cos = Math.Cos(phi);
        if (Math.Abs(sin) <= 1e-14)
            return; // Collinear tangents have constant heading; cx already splits their domain.
        double straight = (v - shift * cos) / sin;
        if (straight >= 0)
            AddPoint(points, ref count, (cx + straight * cos - shift * sin) * radius, lower, upper);
    }

    // One-sided probes exclude angle wraps from root brackets. Their displacement
    // accounts for both turning radius and floating-point spacing at the boundary.
    internal static double InsideBoundary(double boundary, double toward, double radius)
    {
        double step = Math.Min(Math.Abs(toward - boundary) / 4,
            Math.Max(radius * 1e-10, Math.Abs(boundary) * 2e-15));
        double candidate = boundary + Math.CopySign(step, toward - boundary);
        if (candidate == boundary)
            candidate = toward > boundary ? Math.BitIncrement(boundary) : Math.BitDecrement(boundary);
        return candidate;
    }

    /// <summary>
    /// Select the shortest positive rendezvous among all six Dubins families.
    /// Constant ship speed makes this the earliest intercept as well. Numerical
    /// length ties use time, then LSL/RSR/LSR/RSL/RLR/LRL order.
    /// Each family is split at its domain boundaries, angle wraps and stationary
    /// residuals before solving, so close roots and tangent roots need no sample grid.
    /// Faster incoming targets are searched too; speed alone does not decide feasibility.
    /// </summary>
    public static ApproachInterceptSolution SolveInterceptFlyThroughPlan(
        double shipX, double shipY, double shipDirectionDegrees, double shipSpeedKmS,
        double targetX, double targetY, double targetDirectionDegrees,
        double targetSpeedKmS, int angularInertiaDegPerSec)
    {
        if (shipSpeedKmS <= 0 || angularInertiaDegPerSec <= 0 || targetSpeedKmS < 0)
            return ApproachInterceptSolution.None;

        double speed = shipSpeedKmS * UnitsPerKmS;
        double targetSpeed = targetSpeedKmS * UnitsPerKmS;
        double radius = speed / (angularInertiaDegPerSec * Math.PI / 180);
        double angle = targetDirectionDegrees * Math.PI / 180;
        double fx = Math.Sin(angle), fy = -Math.Cos(angle);
        double along = (shipX - targetX) * fx + (shipY - targetY) * fy;
        double cross = (shipX - targetX) * -fy + (shipY - targetY) * fx;
        double heading = (targetDirectionDegrees - shipDirectionDegrees) * Math.PI / 180;
        double ratio = targetSpeed / speed;
        // Straight-line reachability bounds every bounded-curvature rendezvous.
        double a = (speed - targetSpeed) * (speed + targetSpeed);
        double dot = -along * targetSpeed;
        double distanceSquared = along * along + cross * cross;
        double horizon;
        if (a > 0)
        {
            double radical = Math.Sqrt(dot * dot + a * distanceSquared);
            double lead = dot < 0 ? distanceSquared / (radical - dot) : (dot + radical) / a;
            horizon = Math.Max(5, 2 * lead + 6 * Math.PI * radius / (speed - targetSpeed));
        }
        else
        {
            if (along <= 0)
                return ApproachInterceptSolution.None;
            // Avoid subtracting two large along² terms near equal speeds.
            double discriminant = speed * speed * along * along + a * cross * cross;
            if (discriminant < 0)
                return ApproachInterceptSolution.None;
            horizon = a < 0 ? (-dot + Math.Sqrt(discriminant)) / -a : double.PositiveInfinity;
        }
        double upper = targetSpeed * horizon;
        var best = ApproachInterceptSolution.None;
        const double tolerance = 1e-6;
        Span<double> points = stackalloc double[32];

        foreach (string type in AllCurveTypes)
        {
            if (targetSpeed == 0)
            {
                if (Curve(type, 0, out var stationary))
                    Consider(type, 0, stationary, stationary.RemainingUnits / speed);
                continue;
            }
            if (a == 0)
                upper = EqualSpeedUpper(type);
            int count = GetCurveBreakpoints(along, cross, heading, radius, type, 0, upper, ratio, points);
            for (int i = 0; i < count; i++)
            {
                Check(type, points[i]);
                if (i == 0)
                    continue;
                double lo = InsideBoundary(points[i - 1], points[i], radius);
                double hi = InsideBoundary(points[i], points[i - 1], radius);
                Check(type, lo);
                Check(type, hi);
                if (!Curve(type, lo, out var low) || !Curve(type, hi, out var high))
                    continue;
                double lowResidual = Residual(low, lo);
                double highResidual = Residual(high, hi);
                if (Math.Sign(lowResidual) == Math.Sign(highResidual))
                    continue;
                for (int iteration = 0; iteration < 100; iteration++)
                {
                    double mid = lo + (hi - lo) / 2;
                    if (mid == lo || mid == hi || !Curve(type, mid, out var candidate))
                        break;
                    double residual = Residual(candidate, mid);
                    // Near equal speeds amplify a small residual into a large
                    // time/length error. Refine the bracket to machine precision;
                    // the residual tolerance is only an acceptance check.
                    if (residual == 0)
                    {
                        Check(type, mid);
                        break;
                    }
                    if (Math.Sign(residual) == Math.Sign(lowResidual))
                    {
                        lo = mid;
                        lowResidual = residual;
                    }
                    else
                        hi = mid;
                }
                Check(type, lo + (hi - lo) / 2);
            }
        }
        return best;

        bool Curve(string type, double x, out ApproachFlyThroughPlan plan) =>
            TryCreateLineCurvePlan(along, cross, heading, radius, x, type, out plan);

        double EqualSpeedUpper(string type)
        {
            // All finite domain/wrap boundaries are analytic. Beyond the last one,
            // a CSC residual L(x)-x decreases monotonically to its asymptote; CCC
            // curves have a bounded domain. No arbitrary flight-duration cap.
            Span<double> boundaries = stackalloc double[32];
            int count = GetCurveBreakpoints(along, cross, heading, radius, type,
                0, double.MaxValue, 1, boundaries);
            double end = Math.Max(Math.Abs(along) + Math.Abs(cross) + 16 * radius,
                boundaries[count - 2] + radius);
            if (type[1] != 'S')
                return end;
            double s = type[0] == 'L' ? 1 : -1;
            double e = type[2] == 'L' ? 1 : -1;
            double cx = along - s * radius * Math.Sin(heading);
            double tangentSign = Math.Sign(cross / radius + s * (1 - Math.Cos(heading)));
            double first = Mod2Pi(-s * heading);
            if (first == 0 && s * tangentSign < 0)
                first = 2 * Math.PI;
            double last = e * tangentSign > 0 ? 2 * Math.PI : 0;
            double limit = radius * (first + last) - cx;
            if (limit >= 0)
                return end;
            while (Curve(type, end, out var plan) && Residual(plan, end) > 0)
            {
                double next = end * 2;
                if (!double.IsFinite(next))
                    break;
                end = next;
            }
            return end;
        }

        // Bracketing, stopping and acceptance must use the same floating-point
        // expression. ratio*L-x can round to zero while L-speed*(x/targetSpeed)
        // differs by several ULPs on long, near-equal-speed rendezvous routes.
        double Residual(ApproachFlyThroughPlan plan, double x) =>
            plan.RemainingUnits - speed * (x / targetSpeed);

        void Check(string type, double x)
        {
            if (!Curve(type, x, out var plan))
                return;
            double time = x / targetSpeed;
            double travelled = speed * time;
            double length = plan.RemainingUnits;
            // At ~3e10 units a single double ULP exceeds 1e-6. Allow only the
            // rounding floor of these distances, not a broad relative tolerance.
            double roundingTolerance = 2 * Math.Max(
                Math.BitIncrement(length) - length, Math.BitIncrement(travelled) - travelled);
            if (Math.Abs(Residual(plan, x)) <= Math.Max(tolerance, roundingTolerance))
                Consider(type, x, plan, time);
        }

        void Consider(string type, double x, ApproachFlyThroughPlan plan, double time)
        {
            if (time <= 0 || time > horizon || !double.IsFinite(plan.RemainingUnits))
                return;
            if (best.HasIntercept &&
                !(plan.RemainingUnits < best.Plan.RemainingUnits - tolerance ||
                  Math.Abs(plan.RemainingUnits - best.Plan.RemainingUnits) <= tolerance &&
                  time < best.InterceptTimeSeconds - 1e-8))
                return;
            best = new(true, type, time, targetX + x * fx, targetY + x * fy,
                NormalizeDegrees(targetDirectionDegrees), plan);
        }
    }

    /// <summary>
    /// Evaluate one specific Dubins curve type's 3 segment lengths (normalized by turn
    /// radius) for the given geometry, reusing the same EvaluateLsl/EvaluateRsr/.../EvaluateLrl formulas
    /// <see cref="CreateFlyThroughPlan"/> uses — without duplicating them. Returns false
    /// when this type is not admissible for this geometry (its formula's domain
    /// condition — p^2 &gt;= 0 for the CSC types, |x| &lt;= 1 for the CCC types — is not
    /// met), exactly mirroring the per-type Add* methods' own "not added" behavior.
    /// </summary>
    private static bool TryEvaluateCurveType(
        string curveType, double alpha, double beta, double normalizedDistance,
        out double first, out double second, out double third)
    {
        var candidate = curveType switch
        {
            "LSL" => EvaluateLsl(alpha, beta, normalizedDistance),
            "RSR" => EvaluateRsr(alpha, beta, normalizedDistance),
            "LSR" => EvaluateLsr(alpha, beta, normalizedDistance),
            "RSL" => EvaluateRsl(alpha, beta, normalizedDistance),
            "RLR" => EvaluateRlr(alpha, beta, normalizedDistance),
            "LRL" => EvaluateLrl(alpha, beta, normalizedDistance),
            _ => throw new ArgumentOutOfRangeException(nameof(curveType), curveType, "Unknown Dubins curve type.")
        };
        if (candidate is null)
        {
            first = second = third = 0;
            return false;
        }

        (first, second, third) = candidate.Value;
        return true;
    }

    public static ApproachFlyThroughPlanStep AdvanceFlyThroughPlan(
        ApproachFlyThroughPlan plan,
        double currentDirectionDegrees,
        double targetDirectionDegrees,
        double travelledUnits,
        int turnStepDegrees)
    {
        double first = Math.Max(0, plan.FirstRemainingUnits);
        double second = Math.Max(0, plan.SecondRemainingUnits);
        double third = Math.Max(0, plan.ThirdRemainingUnits);
        double remainingTravel = Math.Max(0, travelledUnits);

        Consume(ref first, ref remainingTravel);
        Consume(ref second, ref remainingTravel);
        Consume(ref third, ref remainingTravel);

        var remaining = new ApproachFlyThroughPlan(plan.Type, first, second, third);
        if (remaining.RemainingUnits <= ArrivalToleranceUnits)
        {
            return new ApproachFlyThroughPlanStep(
                true,
                NormalizeDegrees(targetDirectionDegrees),
                remaining);
        }

        int segmentIndex = first > 0 ? 0 : second > 0 ? 1 : 2;
        char segmentType = plan.Type.Length > segmentIndex ? plan.Type[segmentIndex] : 'S';
        double newDirection = currentDirectionDegrees;
        if (segmentType == 'L')
            newDirection = NormalizeDegrees(currentDirectionDegrees - Math.Abs(turnStepDegrees));
        else if (segmentType == 'R')
            newDirection = NormalizeDegrees(currentDirectionDegrees + Math.Abs(turnStepDegrees));

        return new ApproachFlyThroughPlanStep(false, newDirection, remaining);
    }

    private static void Consume(ref double segment, ref double travel)
    {
        if (travel <= 0 || segment <= 0)
            return;
        double consumed = Math.Min(segment, travel);
        segment -= consumed;
        travel -= consumed;
    }

    private static (double First, double Second, double Third)? EvaluateLsl(double a, double b, double d)
    {
        // Squared circle-center distance avoids cancellation to a negative number
        // when the centers nearly coincide; same-turn tangents always exist.
        double dx = d + Math.Sin(a) - Math.Sin(b), dy = Math.Cos(b) - Math.Cos(a);
        double p2 = dx * dx + dy * dy;
        double x = Math.Atan2(dy, dx);
        return (Mod2Pi(-a + x), Math.Sqrt(p2), Mod2Pi(b - x));
    }

    private static (double First, double Second, double Third)? EvaluateRsr(double a, double b, double d)
    {
        double dx = d - Math.Sin(a) + Math.Sin(b), dy = Math.Cos(a) - Math.Cos(b);
        double p2 = dx * dx + dy * dy;
        double x = Math.Atan2(dy, dx);
        return (Mod2Pi(a - x), Math.Sqrt(p2), Mod2Pi(-b + x));
    }

    private static (double First, double Second, double Third)? EvaluateLsr(double a, double b, double d)
    {
        double p2 = -2 + d * d + 2 * Math.Cos(a - b) + 2 * d * (Math.Sin(a) + Math.Sin(b));
        if (p2 < 0) return null;
        double p = Math.Sqrt(p2);
        double x = Math.Atan2(-Math.Cos(a) - Math.Cos(b), d + Math.Sin(a) + Math.Sin(b)) - Math.Atan2(-2, p);
        return (Mod2Pi(-a + x), p, Mod2Pi(-Mod2Pi(b) + x));
    }

    private static (double First, double Second, double Third)? EvaluateRsl(double a, double b, double d)
    {
        double p2 = d * d - 2 + 2 * Math.Cos(a - b) - 2 * d * (Math.Sin(a) + Math.Sin(b));
        if (p2 < 0) return null;
        double p = Math.Sqrt(p2);
        double x = Math.Atan2(Math.Cos(a) + Math.Cos(b), d - Math.Sin(a) - Math.Sin(b)) - Math.Atan2(2, p);
        return (Mod2Pi(a - x), p, Mod2Pi(b - x));
    }

    private static (double First, double Second, double Third)? EvaluateRlr(double a, double b, double d)
    {
        double dx = d - Math.Sin(a) + Math.Sin(b), dy = Math.Cos(a) - Math.Cos(b);
        double distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance > 4 + 1e-12) return null;
        // acos(1-distance²/8) loses the small angle near coincident circles.
        // The equivalent 2*asin(distance/4) retains it for one-sided boundaries.
        double p = Mod2Pi(2 * Math.PI - 2 * Math.Asin(Math.Min(1, distance / 4)));
        double t = Mod2Pi(a - Math.Atan2(Math.Cos(a) - Math.Cos(b), d - Math.Sin(a) + Math.Sin(b)) + p / 2);
        return (t, p, Mod2Pi(a - b - t + p));
    }

    private static (double First, double Second, double Third)? EvaluateLrl(double a, double b, double d)
    {
        double dx = d + Math.Sin(a) - Math.Sin(b), dy = Math.Cos(b) - Math.Cos(a);
        double distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance > 4 + 1e-12) return null;
        double p = Mod2Pi(2 * Math.PI - 2 * Math.Asin(Math.Min(1, distance / 4)));
        double t = Mod2Pi(-a - Math.Atan2(Math.Cos(a) - Math.Cos(b), d + Math.Sin(a) - Math.Sin(b)) + p / 2);
        return (t, p, Mod2Pi(Mod2Pi(b) - a - t + Mod2Pi(p)));
    }

    private static double Mod2Pi(double value)
    {
        double result = value % (2 * Math.PI);
        if (result < 0)
            result += 2 * Math.PI;
        // At a zero-turn boundary, trig roundoff must not manufacture a full loop
        // and defeat the canonical family tie-break (e.g. an exactly straight LSL).
        return result < 1e-12 || 2 * Math.PI - result < 1e-12 ? 0 : result;
    }

    /// <summary>
    /// Speed (km/s) at or below which a target is treated as genuinely stationary
    /// rather than "moving however slowly" (Post-implementation bug fix #3,
    /// story-20260827-083137.md). Deliberately tight — only exact (or
    /// numerically-indistinguishable-from-exact) zero speed should be treated as
    /// stationary; a slow-but-genuinely-moving object (e.g. a drifting asteroid) must
    /// still get the full directional trailing offset.
    /// </summary>
    private const double StationaryTargetSpeedEpsilonKmS = 1e-9;

    /// <summary>
    /// Compute the point trailing behind a target along its current heading.
    /// <paramref name="targetSpeedKmS"/> is used only to decide WHETHER the trailing
    /// offset applies, not to compute its magnitude/direction: for a genuinely
    /// stationary target (speed ≈ 0, e.g. a Station) the object's Direction field is
    /// an arbitrary placeholder with no physical meaning — "trail behind it along its
    /// direction of travel" is meaningless for something that isn't moving. Applying
    /// the offset anyway previously sent the ship's aim point far from the real
    /// object (the reported "flies past the station, never arrives" bug —
    /// Post-implementation bug fix #3, story-20260827-083137.md; supersedes the prior
    /// "no speed parameter needed" design). So for a genuinely stationary target the
    /// effective trail distance is 0 and this simply returns the target's own
    /// position; a target moving however slowly (speed above
    /// <see cref="StationaryTargetSpeedEpsilonKmS"/>) still gets the full offset using
    /// its Direction field, unchanged from before.
    /// </summary>
    /// <param name="targetX">Target current X, world units.</param>
    /// <param name="targetY">Target current Y, world units.</param>
    /// <param name="targetDirectionDegrees">Target current heading, degrees.</param>
    /// <param name="targetSpeedKmS">Target current speed, km/s (fresh, live read).</param>
    /// <param name="trailDistanceWorldUnits">Distance to trail behind the target, world units.</param>
    public static (double X, double Y) ComputeAimPoint(
        double targetX,
        double targetY,
        double targetDirectionDegrees,
        double targetSpeedKmS,
        double trailDistanceWorldUnits)
    {
        double effectiveTrailDistanceWorldUnits =
            Math.Abs(targetSpeedKmS) < StationaryTargetSpeedEpsilonKmS
                ? 0.0
                : trailDistanceWorldUnits;

        double angleRad = targetDirectionDegrees * Math.PI / 180.0;
        double forwardX = Math.Sin(angleRad);
        double forwardY = -Math.Cos(angleRad);

        return (
            targetX - effectiveTrailDistanceWorldUnits * forwardX,
            targetY - effectiveTrailDistanceWorldUnits * forwardY);
    }

    /// <summary>
    /// Constant-velocity position advance. Pure function shared by server and client so
    /// both can extrapolate a target's position identically between live re-reads.
    /// </summary>
    /// <param name="x">Current X, world units.</param>
    /// <param name="y">Current Y, world units.</param>
    /// <param name="directionDegrees">Heading, degrees.</param>
    /// <param name="speedKmS">Speed, km/s.</param>
    /// <param name="elapsedMs">Elapsed time, milliseconds.</param>
    public static (double X, double Y) ExtrapolatePosition(
        double x,
        double y,
        double directionDegrees,
        double speedKmS,
        long elapsedMs)
    {
        double distance = speedKmS * (elapsedMs / 1000.0) * UnitsPerKmS;
        double angleRad = directionDegrees * Math.PI / 180.0;

        return (
            x + distance * Math.Sin(angleRad),
            y - distance * Math.Cos(angleRad));
    }

    /// <summary>
    /// Compute one `navigation.approach` pursuit step. The target's state must always be
    /// passed in fresh (never cached by the caller across calls) — this function holds no
    /// internal state and never locks a permanent course, since the aim point itself
    /// moves as the target moves.
    /// </summary>
    /// <param name="shipX">Ship current X, world units.</param>
    /// <param name="shipY">Ship current Y, world units.</param>
    /// <param name="shipDirectionDegrees">Ship current heading, degrees.</param>
    /// <param name="shipSpeedKmS">Ship current speed, km/s — used to project this step's travelled segment for arrival detection.</param>
    /// <param name="targetX">Target current X, world units (fresh, live read).</param>
    /// <param name="targetY">Target current Y, world units (fresh, live read).</param>
    /// <param name="targetDirectionDegrees">Target current heading, degrees (fresh, live read).</param>
    /// <param name="targetSpeedKmS">
    /// Target current speed, km/s (fresh, live read). Passed through to
    /// <see cref="ComputeAimPoint"/>, which uses it only to decide WHETHER the
    /// trailing offset applies (genuinely stationary targets aim directly at their own
    /// position — see <see cref="ComputeAimPoint"/>'s doc-comment, Post-implementation
    /// bug fix #3); also kept for parity with the ship's kinematic state and for
    /// callers that need it independently (e.g. baking/extrapolation).
    /// </param>
    /// <param name="trailDistanceWorldUnits">Distance to trail behind the target, world units.</param>
    /// <param name="turnStepDegrees">Maximum turn per step, degrees (module turn-step limit).</param>
    /// <param name="angularInertiaDegPerSec">Angular inertia, degrees per second (0 = cannot turn).</param>
    /// <param name="stepTimeMs">This step's elapsed time, milliseconds — used to project the travelled segment.</param>
    /// <param name="lockedCourseDegrees">
    /// The course locked on a previous call (see <see cref="ApproachStepResult.LockedCourseDegrees"/>),
    /// or null if not yet aligned/locked. Cycle-scoped, NOT permanent — pass back
    /// exactly what the previous call returned; this method itself decides whether to
    /// keep holding it, drop it (aim point moved meaningfully), or newly acquire it.
    /// </param>
    public static ApproachStepResult Step(
        double shipX,
        double shipY,
        double shipDirectionDegrees,
        double shipSpeedKmS,
        double targetX,
        double targetY,
        double targetDirectionDegrees,
        double targetSpeedKmS,
        double trailDistanceWorldUnits,
        int turnStepDegrees,
        int angularInertiaDegPerSec,
        long stepTimeMs,
        double? lockedCourseDegrees = null)
    {
        var (aimX, aimY) = ComputeAimPoint(targetX, targetY, targetDirectionDegrees, targetSpeedKmS, trailDistanceWorldUnits);

        double dx = aimX - shipX;
        double dy = aimY - shipY;
        double distanceToAim = Math.Sqrt(dx * dx + dy * dy);

        double newDirection = shipDirectionDegrees;
        double? newLockedCourse = lockedCourseDegrees;
        bool arrivedBehindShip = false;

        if (distanceToAim > ArrivalToleranceUnits && angularInertiaDegPerSec > 0 && turnStepDegrees > 0)
        {
            if (newLockedCourse is { } lockedCourse)
            {
                // Holding an existing lock: steer toward the locked heading rather than
                // a freshly recomputed bearing — mirrors
                // NavigationWaypointMath.HoldLockedCourse and is what prevents the
                // pure-pursuit circling this fix addresses.
                double lockDelta = ShortestSignedAngleDegrees(shipDirectionDegrees, lockedCourse);
                double lockTurnDelta = Math.Abs(lockDelta) <= turnStepDegrees
                    ? lockDelta
                    : Math.Sign(lockDelta) * turnStepDegrees;
                newDirection = NormalizeDegrees(shipDirectionDegrees + lockTurnDelta);

                if (Math.Abs(lockDelta) <= turnStepDegrees / 2.0)
                {
                    // Behind-the-ship arrival safeguard FIRST (mirrors
                    // NavigationWaypointMath's dot ≤ 0 check): once aligned with the
                    // locked course, if the aim point has fallen behind the ship's new
                    // heading, treat this as arrived — otherwise the ship endlessly
                    // re-chases a point it has already flown past. This must be checked
                    // BEFORE any bearing-drift staleness comparison below, because flying
                    // past a point naturally swings the raw bearing to it by a huge
                    // amount (it is now behind, not just "moved slightly") — that swing
                    // must resolve as arrival, not as a false "target moved, drop lock".
                    double dirRad = newDirection * Math.PI / 180.0;
                    double dot = dx * Math.Sin(dirRad) - dy * Math.Cos(dirRad);
                    if (dot <= 0)
                    {
                        arrivedBehindShip = true;
                    }
                    else
                    {
                        // Still ahead: the lock is only kept while the freshly
                        // recomputed bearing is still close to it (within one turn
                        // step) — beyond that, the aim point has moved enough (target
                        // genuinely moving) that the lock is stale and must be dropped
                        // so the bearing is re-derived fresh, in this SAME call. This is
                        // what keeps the lock cycle-scoped rather than permanent.
                        double bearingNow = BearingDegrees(dx, dy);
                        if (Math.Abs(ShortestSignedAngleDegrees(lockedCourse, bearingNow)) > turnStepDegrees)
                        {
                            newLockedCourse = null;
                            double delta = ShortestSignedAngleDegrees(shipDirectionDegrees, bearingNow);
                            double turnDelta = Math.Abs(delta) <= turnStepDegrees
                                ? delta
                                : Math.Sign(delta) * turnStepDegrees;
                            newDirection = NormalizeDegrees(shipDirectionDegrees + turnDelta);
                            if (Math.Abs(delta) <= turnStepDegrees / 2.0)
                                newLockedCourse = bearingNow;
                        }
                    }
                }
            }
            else
            {
                double bearing = BearingDegrees(dx, dy);
                double delta = ShortestSignedAngleDegrees(shipDirectionDegrees, bearing);
                double turnDelta = Math.Abs(delta) <= turnStepDegrees
                    ? delta
                    : Math.Sign(delta) * turnStepDegrees;
                newDirection = NormalizeDegrees(shipDirectionDegrees + turnDelta);

                // Newly aligned this step — lock the bearing as the course to hold,
                // exactly the anti-circling stabilization NavigationWaypointMath
                // already uses for Orbit (see this class's doc-comment).
                if (Math.Abs(delta) <= turnStepDegrees / 2.0)
                    newLockedCourse = bearing;
            }
        }

        double stepDistance = shipSpeedKmS * (stepTimeMs / 1000.0) * UnitsPerKmS;
        double angleRad = newDirection * Math.PI / 180.0;
        double endX = shipX + stepDistance * Math.Sin(angleRad);
        double endY = shipY - stepDistance * Math.Cos(angleRad);

        bool arrived = arrivedBehindShip
            || distanceToAim <= ArrivalToleranceUnits
            || ClosestDistanceOnSegment(shipX, shipY, endX, endY, aimX, aimY) <= ArrivalToleranceUnits;

        return new ApproachStepResult(aimX, aimY, arrived, newDirection, arrived ? null : newLockedCourse);
    }

    /// <summary>
    /// Whether a ship travelling in a straight line from (startX, startY) to (endX, endY)
    /// passed within <see cref="ArrivalToleranceUnits"/> of the aim point at
    /// (aimX, aimY) — catching a fast ship sweeping through the arrival zone mid-segment
    /// rather than only sampling the segment's end position. Same closest-point-on-segment
    /// technique as <see cref="NavigationWaypointMath.CheckSegmentArrival"/>; unlike
    /// <see cref="Step"/>'s own arrival check (which only covers a single steered interval),
    /// this is for callers that fly a straight, non-steering segment of their own (e.g. the
    /// "wait until the next cycle boundary" phase both <see cref="LinearMotionPredictor"/>
    /// and the client's trajectory preview run before their first <see cref="Step"/> call).
    /// </summary>
    public static (bool IsArrived, double ClosestX, double ClosestY) CheckSegmentArrival(
        double startX, double startY,
        double endX, double endY,
        double aimX, double aimY)
    {
        double segDx = endX - startX;
        double segDy = endY - startY;
        double lenSq = segDx * segDx + segDy * segDy;

        double closestX, closestY;
        if (lenSq <= 0)
        {
            closestX = startX;
            closestY = startY;
        }
        else
        {
            double tDx = aimX - startX;
            double tDy = aimY - startY;
            double t = Math.Clamp((tDx * segDx + tDy * segDy) / lenSq, 0.0, 1.0);
            closestX = startX + t * segDx;
            closestY = startY + t * segDy;
        }

        double dist = Math.Sqrt(
            (aimX - closestX) * (aimX - closestX) + (aimY - closestY) * (aimY - closestY));

        return dist <= ArrivalToleranceUnits ? (true, closestX, closestY) : (false, 0, 0);
    }

    /// <summary>
    /// Closest distance from <paramref name="pointX"/>/<paramref name="pointY"/> to the
    /// line segment from (startX, startY) to (endX, endY). Same closest-point-on-segment
    /// technique as <see cref="NavigationWaypointMath.CheckSegmentArrival"/>, used here to
    /// detect a fast ship sweeping through the arrival zone mid-step rather than only
    /// sampling the step's end position.
    /// </summary>
    private static double ClosestDistanceOnSegment(
        double startX, double startY,
        double endX, double endY,
        double pointX, double pointY)
    {
        double segDx = endX - startX;
        double segDy = endY - startY;
        double lenSq = segDx * segDx + segDy * segDy;

        if (lenSq <= 0)
        {
            double dx0 = pointX - startX;
            double dy0 = pointY - startY;
            return Math.Sqrt(dx0 * dx0 + dy0 * dy0);
        }

        double tDx = pointX - startX;
        double tDy = pointY - startY;
        double t = Math.Clamp((tDx * segDx + tDy * segDy) / lenSq, 0.0, 1.0);

        double closestX = startX + t * segDx;
        double closestY = startY + t * segDy;

        double dx1 = pointX - closestX;
        double dy1 = pointY - closestY;
        return Math.Sqrt(dx1 * dx1 + dy1 * dy1);
    }

    private static double BearingDegrees(double dx, double dy)
    {
        double degrees = Math.Atan2(dx, -dy) * 180.0 / Math.PI;
        return degrees < 0 ? degrees + 360 : degrees;
    }

    private static double ShortestSignedAngleDegrees(double fromDegrees, double toDegrees)
    {
        double raw = (toDegrees - fromDegrees) % 360;
        if (raw > 180)
            raw -= 360;
        else if (raw <= -180)
            raw += 360;
        return raw;
    }

    private static double NormalizeDegrees(double degrees)
    {
        double normalized = degrees % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }
}
