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
/// outcome for which callers must fall back to today's captured-pose behavior
/// (protects SPC-0003/Default: ship not strictly faster than the target).
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

        var candidates = new List<(string Type, double First, double Second, double Third)>(6);
        AddLsl(candidates, alpha, beta, normalizedDistance);
        AddRsr(candidates, alpha, beta, normalizedDistance);
        AddLsr(candidates, alpha, beta, normalizedDistance);
        AddRsl(candidates, alpha, beta, normalizedDistance);
        AddRlr(candidates, alpha, beta, normalizedDistance);
        AddLrl(candidates, alpha, beta, normalizedDistance);

        var shortest = candidates.Count > 0
            ? candidates.MinBy(candidate => candidate.First + candidate.Second + candidate.Third)
            : (Type: "SSS", First: 0.0, Second: normalizedDistance, Third: 0.0);
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

    /// <summary>
    /// Relative extra time (as a multiple of the straight-line lead-pursuit horizon
    /// <c>t_lead</c>, Checkpoint 1 of story-20260829-210641.md §10) added when building
    /// the per-type search horizon. A Dubins curve is never shorter than the
    /// straight-line distance, so the winning type's actual rendezvous time is always
    /// &gt;= t_lead. Combined additively with <see cref="InterceptCurvatureLoopsMargin"/>
    /// (see that constant's doc-comment for why BOTH terms are needed):
    /// <c>horizon = t_lead * (1 + InterceptHorizonMarginFactor) + InterceptCurvatureLoopsMargin * 2*pi / angularVelocityRadPerSec</c>.
    /// </summary>
    private const double InterceptHorizonMarginFactor = 1.0;

    /// <summary>
    /// Curvature-correction budget, expressed as a number of FULL turning loops
    /// (<c>2*pi / angularVelocityRadPerSec</c> seconds each) added on top of the
    /// relative <see cref="InterceptHorizonMarginFactor"/> term. This is the ADDITIVE
    /// part Checkpoint 1 anticipates ("k · turnRadius / closingSpeed") and it matters
    /// independently of <c>t_lead</c>: when the ship's initial heading is badly
    /// misaligned with the rendezvous bearing, the shortest Dubins curve of the winning
    /// type can require the better part of a full extra loop of turning even though the
    /// straight-line lead time itself is tiny (a purely MULTIPLICATIVE margin on
    /// <c>t_lead</c> would then badly under-shoot the real horizon). Empirically verified
    /// against <c>ApproachPursuitMathTests</c>' type-switch regression fixture, whose
    /// genuine (non-wrap-artifact) winning root sits at roughly 1.1 loops of turning time
    /// even though <c>t_lead</c> there is only ~6.3s — 3.0 loops leaves a comfortable
    /// safety margin above that observed worst case.
    /// </summary>
    private const double InterceptCurvatureLoopsMargin = 3.0;

    /// <summary>
    /// Absolute floor (seconds) for the per-type search horizon, guarding the numerically
    /// degenerate case where the target already sits (almost) exactly at the ship's
    /// position — the lead-pursuit horizon <c>t_lead</c> collapses to ~0 there, which
    /// would otherwise leave no search interval at all.
    /// </summary>
    private const double InterceptMinHorizonSeconds = 5.0;

    /// <summary>Number of coarse samples used to bracket a sign change per curve type before bisecting.</summary>
    private const int InterceptHorizonSampleCount = 500;

    /// <summary>Bisection stops once the bracket is narrower than this, in seconds.</summary>
    private const double InterceptBisectionToleranceSeconds = 1e-6;

    private const int InterceptMaxBisectionIterations = 100;

    /// <summary>
    /// When a coarse sample lands (numerically) exactly on a root, or the bisection
    /// bracket is this tight, treat it as the root without further refinement.
    /// </summary>
    private const double InterceptResidualToleranceUnits = 1e-6;

    /// <summary>
    /// Small time nudge used only when a bisection midpoint happens to fall just outside
    /// a curve type's admissible domain (the domain boundary lies inside the current
    /// bracket) — nudges toward the still-admissible side rather than crossing into a
    /// different curve type's formula, per story-20260829-210641.md §4 ("не пересекая
    /// границу переключения типов").
    /// </summary>
    private const double InterceptInvalidNudgeSeconds = 1e-4;

    /// <summary>Two candidate intercept times within this tolerance are treated as tied (then broken by curve length).</summary>
    private const double InterceptTimeTieToleranceSeconds = 1e-6;

    /// <summary>
    /// Guard factor (multiplied by <c>turnRadius</c>) used to detect a SPURIOUS length
    /// jump within a single curve type's own formula, caused not by a genuine geometric
    /// discontinuity but by the <see cref="Mod2Pi"/> wrap each Add* method applies to
    /// keep its reported segment angles in [0, 2*pi) (the "shortest representative" for
    /// that type at that exact pose — the same convention <see cref="CreateFlyThroughPlan"/>
    /// already relies on, so it is NOT changed here). As the target pose varies
    /// continuously, one of a type's three segment angles can cross a 2*pi boundary,
    /// making the type's OWN reported length jump by close to <c>2*pi*turnRadius</c>
    /// between two arbitrarily close instants — a real, reproducible artifact (see the
    /// commit's regression test), not a hypothetical. A genuine smooth change in length
    /// between two closely-spaced samples is always far smaller than
    /// <c>pi*turnRadius</c> (half of the wrap's jump size) for any reasonable sampling
    /// resolution, so a jump at or above that threshold is treated exactly like an
    /// admissibility-domain boundary — story-20260829-210641.md §4's "не пересекая
    /// границу переключения типов" applies here too: bisection must never straddle it.
    /// </summary>
    private const double InterceptWrapJumpGuardFactor = Math.PI;

    /// <summary>
    /// Solve the exact-intercept fly-through problem: find the earliest time <c>t*</c> and
    /// Dubins curve type such that flying that curve from the ship's current pose arrives
    /// exactly where the target will be at <c>t*</c>, assuming the target holds its
    /// current heading and speed for the whole search (story-20260829-210641.md §2, §4).
    ///
    /// Unlike a single bisection over the argmin-selected curve length (the previous,
    /// reverted attempt — see the story's "why bisection on t failed" section), this
    /// solves the rendezvous equation <c>L_X(t) - shipSpeed * t = 0</c> SEPARATELY for
    /// each of the 6 Dubins curve types X, each within its own domain of validity (where
    /// the type's own formula is defined), and only picks the overall winner (earliest
    /// t*, tie-broken by shorter length) AFTER solving. This avoids ever bisecting across
    /// a point where the globally-shortest curve type switches — the argmin-selected
    /// length is not continuous there even though the underlying geometry is (see
    /// <c>InterceptFlyThroughSolution_avoids_the_type_switch_discontinuity</c> in
    /// ApproachPursuitMathTests.cs for a concrete reproduction).
    ///
    /// The search horizon per type is derived from the classical straight-line
    /// lead-pursuit quadratic (Checkpoint 1 of story-20260829-210641.md §10):
    /// <c>(shipSpeed^2 - |vTarget|^2) * t^2 - 2*(D . vTarget) * t - |D|^2 = 0</c>, where
    /// <c>D</c> is the vector from the ship to the target's CURRENT position and
    /// <c>vTarget</c> is the target's (constant) velocity vector. When
    /// <c>a = shipSpeed^2 - |vTarget|^2 &lt;= 0</c> (ship not strictly faster than the
    /// target) this quadratic has no meaningful positive horizon, and this method
    /// returns <see cref="ApproachInterceptSolution.None"/> as a direct consequence —
    /// not via a separate explicit "shipSpeed &lt;= targetSpeed" check — which is exactly
    /// the SPC-0003/Default "ship not faster than target -> no achievable rendezvous"
    /// scenario this method must protect (see
    /// <c>SolveInterceptFlyThroughPlan_ship_not_faster_than_target_returns_no_intercept</c>).
    /// </summary>
    /// <param name="shipX">Ship current X, world units.</param>
    /// <param name="shipY">Ship current Y, world units.</param>
    /// <param name="shipDirectionDegrees">Ship current heading, degrees.</param>
    /// <param name="shipSpeedKmS">Ship current speed, km/s.</param>
    /// <param name="targetX">Target current X, world units.</param>
    /// <param name="targetY">Target current Y, world units.</param>
    /// <param name="targetDirectionDegrees">Target current heading, degrees — assumed constant over the search horizon.</param>
    /// <param name="targetSpeedKmS">Target current speed, km/s — assumed constant over the search horizon.</param>
    /// <param name="angularInertiaDegPerSec">Ship angular inertia, degrees per second (0 = cannot turn -> no intercept).</param>
    public static ApproachInterceptSolution SolveInterceptFlyThroughPlan(
        double shipX,
        double shipY,
        double shipDirectionDegrees,
        double shipSpeedKmS,
        double targetX,
        double targetY,
        double targetDirectionDegrees,
        double targetSpeedKmS,
        int angularInertiaDegPerSec)
    {
        if (shipSpeedKmS <= 0 || angularInertiaDegPerSec <= 0)
            return ApproachInterceptSolution.None;

        double shipSpeedUnits = shipSpeedKmS * UnitsPerKmS;
        double targetSpeedUnits = targetSpeedKmS * UnitsPerKmS;

        double targetDirectionRad = targetDirectionDegrees * Math.PI / 180.0;
        double targetVelocityX = targetSpeedUnits * Math.Sin(targetDirectionRad);
        double targetVelocityY = -targetSpeedUnits * Math.Cos(targetDirectionRad);

        double offsetX = targetX - shipX;
        double offsetY = targetY - shipY;

        // Classical lead-pursuit quadratic (Checkpoint 1): a*t^2 + b*t + c = 0.
        double a = shipSpeedUnits * shipSpeedUnits - (targetVelocityX * targetVelocityX + targetVelocityY * targetVelocityY);
        double b = -2.0 * (offsetX * targetVelocityX + offsetY * targetVelocityY);
        double c = -(offsetX * offsetX + offsetY * offsetY);

        // a <= 0 means the ship is not strictly faster than the target: no positive lead
        // horizon exists, so no straight-line (and therefore no curved) intercept is
        // achievable — this degeneration IS the SPC-0003/Default "no intercept" outcome.
        if (a <= 0)
            return ApproachInterceptSolution.None;

        double discriminant = b * b - 4 * a * c;
        if (discriminant < 0)
            return ApproachInterceptSolution.None;

        double sqrtDiscriminant = Math.Sqrt(discriminant);
        double root1 = (-b - sqrtDiscriminant) / (2 * a);
        double root2 = (-b + sqrtDiscriminant) / (2 * a);

        double? leadTime = null;
        foreach (double root in new[] { root1, root2 })
        {
            if (root > 0 && (leadTime is null || root < leadTime))
                leadTime = root;
        }
        if (leadTime is null)
            return ApproachInterceptSolution.None;

        double angularVelocityRadPerSec = angularInertiaDegPerSec * Math.PI / 180.0;
        double turnRadius = shipSpeedUnits / angularVelocityRadPerSec;

        double horizon = Math.Max(
            leadTime.Value * (1.0 + InterceptHorizonMarginFactor)
                + InterceptCurvatureLoopsMargin * (2.0 * Math.PI / angularVelocityRadPerSec),
            InterceptMinHorizonSeconds);

        // Evaluate a single curve type's (length, self-consistency residual) at time t,
        // reusing the SAME per-type segment-length formulas CreateFlyThroughPlan already
        // uses (AddLsl/AddRsr/.../AddLrl) — never re-deriving or duplicating them.
        (bool Valid, double LengthUnits) EvaluateType(string curveType, double t)
        {
            double tx = targetX + t * targetVelocityX;
            double ty = targetY + t * targetVelocityY;
            double dx = tx - shipX;
            double dy = -(ty - shipY);
            double directDistance = Math.Sqrt(dx * dx + dy * dy);
            double normalizedDistance = directDistance / turnRadius;
            double theta = Mod2Pi(Math.Atan2(dy, dx));
            double alpha = Mod2Pi((90.0 - shipDirectionDegrees) * Math.PI / 180.0 - theta);
            double beta = Mod2Pi((90.0 - targetDirectionDegrees) * Math.PI / 180.0 - theta);

            if (!TryEvaluateCurveType(curveType, alpha, beta, normalizedDistance, out double first, out double second, out double third))
                return (false, 0);

            return (true, (first + second + third) * turnRadius);
        }

        double wrapJumpGuardUnits = InterceptWrapJumpGuardFactor * turnRadius;

        double? BisectRoot(
            string curveType, double lowT, double highT,
            double lowLength, double highLength, double lowResidual, double highResidual)
        {
            for (int iteration = 0; iteration < InterceptMaxBisectionIterations; iteration++)
            {
                if (highT - lowT < InterceptBisectionToleranceSeconds)
                    return 0.5 * (lowT + highT);

                double midT = 0.5 * (lowT + highT);
                var (valid, length) = EvaluateType(curveType, midT);
                if (!valid)
                {
                    // The type's admissible domain boundary lies inside this bracket —
                    // nudge toward the side we know is admissible rather than ever
                    // treating the OTHER side of the boundary as this same type's curve.
                    double nudgedT = lowResidual >= 0 ? midT - InterceptInvalidNudgeSeconds : midT + InterceptInvalidNudgeSeconds;
                    if (nudgedT <= lowT || nudgedT >= highT)
                        return null; // Bracket too narrow to safely resolve — give up on this bracket.

                    (valid, length) = EvaluateType(curveType, nudgedT);
                    if (!valid)
                        return null;
                    midT = nudgedT;
                }

                // Guard against the Mod2Pi wrap artifact (see InterceptWrapJumpGuardFactor):
                // if the midpoint's length is not a smooth interpolation between the two
                // bracket ends, the wrap boundary lies inside this bracket — never bisect
                // across it as if it were this type's own smooth formula.
                if (Math.Abs(length - lowLength) > wrapJumpGuardUnits && Math.Abs(length - highLength) > wrapJumpGuardUnits)
                    return null;

                double midResidual = length - shipSpeedUnits * midT;
                if (Math.Abs(midResidual) < InterceptResidualToleranceUnits)
                    return midT;

                if (Math.Sign(midResidual) == Math.Sign(lowResidual))
                {
                    lowT = midT;
                    lowLength = length;
                    lowResidual = midResidual;
                }
                else
                {
                    highT = midT;
                    highLength = length;
                    highResidual = midResidual;
                }
            }

            return 0.5 * (lowT + highT);
        }

        double? FindEarliestRootForType(string curveType)
        {
            double prevT = 0;
            double prevLength = 0;
            double prevResidual = 0;
            bool prevValid = false;

            for (int sample = 1; sample <= InterceptHorizonSampleCount; sample++)
            {
                double t = horizon * sample / InterceptHorizonSampleCount;
                var (valid, length) = EvaluateType(curveType, t);
                if (!valid)
                {
                    prevValid = false;
                    continue;
                }

                double residual = length - shipSpeedUnits * t;

                if (prevValid)
                {
                    // Skip pairs straddling a Mod2Pi wrap artifact (see
                    // InterceptWrapJumpGuardFactor) — never treat that as a genuine root
                    // bracket, even if the residual happens to change sign there.
                    bool suspiciousWrapJump = Math.Abs(length - prevLength) > wrapJumpGuardUnits;

                    if (!suspiciousWrapJump)
                    {
                        if (Math.Abs(residual) < InterceptResidualToleranceUnits)
                            return t;

                        if (Math.Sign(residual) != Math.Sign(prevResidual))
                        {
                            double? root = BisectRoot(curveType, prevT, t, prevLength, length, prevResidual, residual);
                            if (root is not null)
                                return root;
                            // Could not safely resolve this bracket (domain boundary or
                            // wrap artifact in the way) — keep scanning forward for a
                            // later, cleaner bracket.
                        }
                    }
                }

                prevT = t;
                prevLength = length;
                prevResidual = residual;
                prevValid = true;
            }

            return null;
        }

        (string Type, double TimeSeconds, double LengthUnits)? best = null;
        foreach (string curveType in AllCurveTypes)
        {
            double? rootTime = FindEarliestRootForType(curveType);
            if (rootTime is null)
                continue;

            var (valid, length) = EvaluateType(curveType, rootTime.Value);
            if (!valid)
                continue; // Defensive: should always be valid at its own found root.

            bool isEarlier = best is null || rootTime.Value < best.Value.TimeSeconds - InterceptTimeTieToleranceSeconds;
            bool isTiedButShorter =
                best is not null &&
                Math.Abs(rootTime.Value - best.Value.TimeSeconds) <= InterceptTimeTieToleranceSeconds &&
                length < best.Value.LengthUnits;

            if (isEarlier || isTiedButShorter)
                best = (curveType, rootTime.Value, length);
        }

        if (best is null)
            return ApproachInterceptSolution.None;

        double interceptTime = best.Value.TimeSeconds;
        double targetXAtIntercept = targetX + interceptTime * targetVelocityX;
        double targetYAtIntercept = targetY + interceptTime * targetVelocityY;

        double finalDx = targetXAtIntercept - shipX;
        double finalDy = -(targetYAtIntercept - shipY);
        double finalDirectDistance = Math.Sqrt(finalDx * finalDx + finalDy * finalDy);
        double finalNormalizedDistance = finalDirectDistance / turnRadius;
        double finalTheta = Mod2Pi(Math.Atan2(finalDy, finalDx));
        double finalAlpha = Mod2Pi((90.0 - shipDirectionDegrees) * Math.PI / 180.0 - finalTheta);
        double finalBeta = Mod2Pi((90.0 - targetDirectionDegrees) * Math.PI / 180.0 - finalTheta);

        if (!TryEvaluateCurveType(best.Value.Type, finalAlpha, finalBeta, finalNormalizedDistance, out double finalFirst, out double finalSecond, out double finalThird))
            return ApproachInterceptSolution.None; // Should not happen: the type was valid at this exact t by construction.

        var plan = new ApproachFlyThroughPlan(
            best.Value.Type,
            finalFirst * turnRadius,
            finalSecond * turnRadius,
            finalThird * turnRadius);

        return new ApproachInterceptSolution(
            true,
            best.Value.Type,
            interceptTime,
            targetXAtIntercept,
            targetYAtIntercept,
            NormalizeDegrees(targetDirectionDegrees),
            plan);
    }

    /// <summary>
    /// Evaluate one specific Dubins curve type's 3 segment lengths (normalized by turn
    /// radius) for the given geometry, reusing the same AddLsl/AddRsr/.../AddLrl formulas
    /// <see cref="CreateFlyThroughPlan"/> uses — without duplicating them. Returns false
    /// when this type is not admissible for this geometry (its formula's domain
    /// condition — p^2 &gt;= 0 for the CSC types, |x| &lt;= 1 for the CCC types — is not
    /// met), exactly mirroring the per-type Add* methods' own "not added" behavior.
    /// </summary>
    private static bool TryEvaluateCurveType(
        string curveType, double alpha, double beta, double normalizedDistance,
        out double first, out double second, out double third)
    {
        var candidates = new List<(string Type, double First, double Second, double Third)>(1);
        switch (curveType)
        {
            case "LSL": AddLsl(candidates, alpha, beta, normalizedDistance); break;
            case "RSR": AddRsr(candidates, alpha, beta, normalizedDistance); break;
            case "LSR": AddLsr(candidates, alpha, beta, normalizedDistance); break;
            case "RSL": AddRsl(candidates, alpha, beta, normalizedDistance); break;
            case "RLR": AddRlr(candidates, alpha, beta, normalizedDistance); break;
            case "LRL": AddLrl(candidates, alpha, beta, normalizedDistance); break;
            default: throw new ArgumentOutOfRangeException(nameof(curveType), curveType, "Unknown Dubins curve type.");
        }

        if (candidates.Count == 0)
        {
            first = second = third = 0;
            return false;
        }

        (first, second, third) = (candidates[0].First, candidates[0].Second, candidates[0].Third);
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

    private static void AddLsl(List<(string Type, double First, double Second, double Third)> paths, double a, double b, double d)
    {
        double p2 = 2 + d * d - 2 * Math.Cos(a - b) + 2 * d * (Math.Sin(a) - Math.Sin(b));
        if (p2 < 0) return;
        double x = Math.Atan2(Math.Cos(b) - Math.Cos(a), d + Math.Sin(a) - Math.Sin(b));
        paths.Add(("LSL", Mod2Pi(-a + x), Math.Sqrt(p2), Mod2Pi(b - x)));
    }

    private static void AddRsr(List<(string Type, double First, double Second, double Third)> paths, double a, double b, double d)
    {
        double p2 = 2 + d * d - 2 * Math.Cos(a - b) + 2 * d * (-Math.Sin(a) + Math.Sin(b));
        if (p2 < 0) return;
        double x = Math.Atan2(Math.Cos(a) - Math.Cos(b), d - Math.Sin(a) + Math.Sin(b));
        paths.Add(("RSR", Mod2Pi(a - x), Math.Sqrt(p2), Mod2Pi(-b + x)));
    }

    private static void AddLsr(List<(string Type, double First, double Second, double Third)> paths, double a, double b, double d)
    {
        double p2 = -2 + d * d + 2 * Math.Cos(a - b) + 2 * d * (Math.Sin(a) + Math.Sin(b));
        if (p2 < 0) return;
        double p = Math.Sqrt(p2);
        double x = Math.Atan2(-Math.Cos(a) - Math.Cos(b), d + Math.Sin(a) + Math.Sin(b)) - Math.Atan2(-2, p);
        paths.Add(("LSR", Mod2Pi(-a + x), p, Mod2Pi(-Mod2Pi(b) + x)));
    }

    private static void AddRsl(List<(string Type, double First, double Second, double Third)> paths, double a, double b, double d)
    {
        double p2 = d * d - 2 + 2 * Math.Cos(a - b) - 2 * d * (Math.Sin(a) + Math.Sin(b));
        if (p2 < 0) return;
        double p = Math.Sqrt(p2);
        double x = Math.Atan2(Math.Cos(a) + Math.Cos(b), d - Math.Sin(a) - Math.Sin(b)) - Math.Atan2(2, p);
        paths.Add(("RSL", Mod2Pi(a - x), p, Mod2Pi(b - x)));
    }

    private static void AddRlr(List<(string Type, double First, double Second, double Third)> paths, double a, double b, double d)
    {
        double x = (6 - d * d + 2 * Math.Cos(a - b) + 2 * d * (Math.Sin(a) - Math.Sin(b))) / 8;
        if (Math.Abs(x) > 1) return;
        double p = Mod2Pi(2 * Math.PI - Math.Acos(x));
        double t = Mod2Pi(a - Math.Atan2(Math.Cos(a) - Math.Cos(b), d - Math.Sin(a) + Math.Sin(b)) + p / 2);
        paths.Add(("RLR", t, p, Mod2Pi(a - b - t + p)));
    }

    private static void AddLrl(List<(string Type, double First, double Second, double Third)> paths, double a, double b, double d)
    {
        double x = (6 - d * d + 2 * Math.Cos(a - b) + 2 * d * (-Math.Sin(a) + Math.Sin(b))) / 8;
        if (Math.Abs(x) > 1) return;
        double p = Mod2Pi(2 * Math.PI - Math.Acos(x));
        double t = Mod2Pi(-a - Math.Atan2(Math.Cos(a) - Math.Cos(b), d + Math.Sin(a) - Math.Sin(b)) + p / 2);
        paths.Add(("LRL", t, p, Mod2Pi(Mod2Pi(b) - a - t + Mod2Pi(p))));
    }

    private static double Mod2Pi(double value)
    {
        double result = value % (2 * Math.PI);
        return result < 0 ? result + 2 * Math.PI : result;
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
