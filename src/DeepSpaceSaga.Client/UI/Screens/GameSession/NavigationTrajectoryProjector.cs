using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Projects the future trajectory of an active navigation cycle
/// (engine.orbit) using EXACTLY the same deterministic math as the
/// engine: <see cref="NavigationWaypointMath.Step"/> for the turn decision and the
/// same straight-line advance formula as <see cref="LinearMotionPredictor"/>.
/// The cycle phase comes from the authoritative snapshot (TurnStepRemainingMs,
/// TurnStepIntervalMs, NavigationAngularInertiaDegPerSec), so the client-side line
/// matches the authoritative discrete motion (AC10).
/// Pure client-side — never touches the Engine.
/// </summary>
internal sealed class NavigationTrajectoryProjector
{
    /// <summary>Same horizon as the future trajectory — never longer than the engine can fly.</summary>
    public const int FutureTrajectoryHorizonMs = FutureTrajectoryProjector.FutureTrajectoryHorizonMs;

    /// <summary>
    /// Safety bound for <see cref="ProjectApproach"/>'s loop, which — unlike the generic
    /// Orbit-oriented branch above (still capped at <see cref="FutureTrajectoryHorizonMs"/>,
    /// 200s) — deliberately runs until actual arrival rather than truncating at a fixed
    /// horizon (story-20260827-083137.md, Post-implementation bug fix #2: the user
    /// explicitly asked for the preview not to be capped short of the target). With the
    /// companion convergence fixes (Approach's faster cadence + <see cref="ApproachPursuitMath.Step"/>'s
    /// course locking) the loop should always terminate via IsArrived long before this is
    /// ever reached — this is a generous order-of-magnitude-plus margin (10x
    /// FutureTrajectoryHorizonMs) purely as a backstop against a residual edge case, not a
    /// value expected to be hit in normal play.
    /// </summary>
    public const int ApproachTrajectoryMaxHorizonMs = 10 * FutureTrajectoryHorizonMs;

    /// <summary>
    /// Hard backstop on <see cref="ProjectApproach"/>'s loop iteration count, independent of
    /// <see cref="ApproachTrajectoryMaxHorizonMs"/> — protects against a pathologically small
    /// TurnStepIntervalMs (e.g. malformed/legacy snapshot data) driving an unbounded number
    /// of iterations within the time bound above.
    /// </summary>
    private const int ApproachTrajectoryMaxIterations = 20_000;

    /// <summary>
    /// If <see cref="ProjectApproach"/>'s distance to the (possibly moving) aim point hasn't
    /// improved on its best-so-far value in this many milliseconds of simulated flight, the
    /// chase is treated as stalled and the preview is truncated at the closest point actually
    /// reached — rather than continuing all the way to <see cref="ApproachTrajectoryMaxHorizonMs"/>.
    /// Without this, a target the ship can only almost (but never quite, within
    /// <see cref="ApproachPursuitMath.ArrivalToleranceUnits"/>) catch — e.g. marginally faster,
    /// or on a course the ship can only asymptotically approach — draws a preview line that
    /// passes right by the target early on and then keeps extending far past it in a long,
    /// visually wrong "escaping" straight line for the full 2000s backstop. Comfortably above
    /// the slowest legitimate warm-up (turning up to ~180° to face the target before any
    /// distance closes at all) for the smallest turn-rate content uses — a full 180° turn at
    /// module.engine.basic's 1°/250ms takes ~45s — while still cutting the line off within
    /// roughly a couple of minutes of simulated flight instead of the full 2000s cap.
    /// </summary>
    private const long ApproachStagnationTimeoutMs = 180_000;

    /// <summary>
    /// Compute future world-coordinate trajectory points for an active navigation
    /// cycle, starting from the current predicted state. Empty when the snapshot does
    /// not carry an authoritative navigation target (no active navigate cycle).
    /// </summary>
    public List<FutureTrajectoryPoint> Project(ObjectMotionSnapshot predicted) =>
        Project(predicted, out _, out _);

    /// <summary>Same as <see cref="Project(ObjectMotionSnapshot)"/>, plus <see cref="Project(ObjectMotionSnapshot, out bool, out FutureTrajectoryPoint)"/>'s isConfirmedIntercept.</summary>
    public List<FutureTrajectoryPoint> Project(ObjectMotionSnapshot predicted, out bool isConfirmedIntercept) =>
        Project(predicted, out isConfirmedIntercept, out _);

    /// <summary>
    /// Same as <see cref="Project(ObjectMotionSnapshot)"/>, plus whether the returned
    /// path is a CONFIRMED intercept-solve rendezvous curve (story-20260829-210641.md
    /// §10, Checkpoint 2) — true as soon as <see cref="ApproachPursuitMath.SolveInterceptFlyThroughPlan"/>
    /// finds one, even during the single transient <see cref="ApproachPursuitMath.FlyThroughPendingPhase"/>
    /// frame before the engine has baked that confirmation into the authoritative
    /// snapshot's NavigationPhase. Callers that gate a "confirmed intercept" marker on
    /// NavigationPhase alone miss that first frame — the preview curve already shows the
    /// resolved rendezvous shape one frame before the phase string catches up. When true,
    /// <paramref name="interceptPoint"/> is the exact rendezvous pose — computed
    /// analytically from the target's live pose and constant velocity, NOT read off the
    /// discretized preview curve's own tracked endpoint, which accumulates enough
    /// per-cycle turn quantization over a long curve to visibly miss the target's own
    /// drawn straight-line trajectory (see <see cref="ProjectFlyThrough"/>).
    /// </summary>
    public List<FutureTrajectoryPoint> Project(
        ObjectMotionSnapshot predicted, out bool isConfirmedIntercept, out FutureTrajectoryPoint interceptPoint)
    {
        isConfirmedIntercept = false;
        interceptPoint = default;
        var points = new List<FutureTrajectoryPoint>(FutureTrajectoryProjector.MaxSamplePoints);

        // navigation.approach: trailing-pursuit preview against a moving aim point —
        // checked before the generic Orbit-oriented branch below since both populate
        // NavigationTargetX/Y (different meaning — see the doc-comment on
        // ObjectMotionSnapshot.NavigationTargetX).
        if (predicted.ActiveEngineCommandType == NavigationComputerCommandTypes.Approach &&
            predicted.NavigationTargetX is { } bakedAimX &&
            predicted.NavigationTargetY is { } bakedAimY &&
            predicted.NavigationTargetSpeedKmS is { } targetSpeedKmS &&
            predicted.NavigationTargetDirectionDegrees is { } targetDirectionDegrees)
        {
            return ProjectApproach(
                predicted, bakedAimX, bakedAimY, targetDirectionDegrees, targetSpeedKmS,
                out isConfirmedIntercept, out interceptPoint);
        }

        if (predicted.NavigationTargetX is not { } targetX ||
            predicted.NavigationTargetY is not { } targetY)
        {
            return points;
        }

        double x = predicted.X;
        double y = predicted.Y;
        double direction = predicted.Direction;
        double speedKmS = predicted.SpeedKmS;

        // Defensive clamps mirror the engine's invariants: TurnStepDegrees is |module|
        // for navigation cycles, the interval is MinTurnIntervalMs (> 0).
        int turnStepDegrees = Math.Max(1, predicted.TurnStepDegrees);
        long intervalMs = Math.Max(1, predicted.TurnStepIntervalMs);
        long phaseMs = Math.Max(1, predicted.TurnStepRemainingMs);

        points.Add(new FutureTrajectoryPoint(x, y));

        // Phase until the first cycle step: straight flight with the CURRENT course.
        double phaseStartX = x, phaseStartY = y;
        (x, y) = AdvanceStraight(x, y, direction, speedKmS, phaseMs);

        // Check segment arrival after phase flight.
        var phaseArrival = NavigationWaypointMath.CheckSegmentArrival(
            phaseStartX, phaseStartY, x, y, targetX, targetY);
        if (phaseArrival.IsArrived)
        {
            points.Add(new FutureTrajectoryPoint(phaseArrival.ClosestX, phaseArrival.ClosestY));
            return points;
        }

        points.Add(new FutureTrajectoryPoint(x, y));

        long elapsedMs = phaseMs;
        double? lockedCourse = predicted.NavigationLockedCourseDegrees;
        string? navigationPhase = predicted.NavigationPhase;
        double? escapeCourse = predicted.NavigationEscapeCourseDegrees;
        double? requiredDepartureDistance = predicted.NavigationRequiredDepartureDistance;
        while (elapsedMs < FutureTrajectoryHorizonMs)
        {
            double stepStartX = x, stepStartY = y;

            var step = NavigationWaypointMath.StagedStep(
                x,
                y,
                direction,
                speedKmS,
                targetX,
                targetY,
                turnStepDegrees,
                predicted.NavigationAngularInertiaDegPerSec,
                stepTimeMs: intervalMs,
                phase: navigationPhase,
                lockedCourseDegrees: lockedCourse,
                escapeCourseDegrees: escapeCourse,
                requiredDepartureDistance: requiredDepartureDistance);

            lockedCourse = step.LockedCourseDegrees ?? lockedCourse;
            navigationPhase = step.NextNavigationPhase ?? navigationPhase;
            escapeCourse = step.EscapeCourseDegrees ?? escapeCourse;
            requiredDepartureDistance = step.RequiredDepartureDistance ?? requiredDepartureDistance;
            if (step.NextNavigationPhase == "Approach")
            {
                lockedCourse = null;
            }

            direction = NormalizeDirection(direction + step.TurnDeltaDegrees);

            if (step.IsArrived)
            {
                // Snap final point at the closest approach to the target on this segment.
                (double cx, double cy) = ClosestApproach(x, y, direction, speedKmS, intervalMs, targetX, targetY);
                points.Add(new FutureTrajectoryPoint(cx, cy));
                break;
            }

            (x, y) = AdvanceStraight(x, y, direction, speedKmS, intervalMs);
            points.Add(new FutureTrajectoryPoint(x, y));

            // Check segment arrival after this advance.
            var segArrival = NavigationWaypointMath.CheckSegmentArrival(
                stepStartX, stepStartY, x, y, targetX, targetY);
            if (segArrival.IsArrived)
            {
                points[^1] = new FutureTrajectoryPoint(segArrival.ClosestX, segArrival.ClosestY);
                break;
            }

            elapsedMs += intervalMs;
        }

        return points;
    }

    /// <summary>
    /// Preview trajectory for an active <see cref="NavigationComputerCommandTypes.Approach"/>
    /// cycle. Structurally mirrors the Orbit branch above (initial straight "wait" phase
    /// until the first cycle boundary, then one steering decision per interval), but unlike
    /// it (a) never locks a PERMANENT course — it re-steers toward the current snapshot's
    /// fixed aim point via <see cref="ApproachPursuitMath.Step"/> without assuming that the
    /// target will continue linearly, threading Step's cycle-scoped course lock the
    /// same way, and (b) runs until actual arrival rather than truncating at the fixed
    /// <see cref="FutureTrajectoryHorizonMs"/> (Post-implementation bug fix #2 — see
    /// <see cref="ApproachTrajectoryMaxHorizonMs"/>'s doc-comment for the safety-bound
    /// rationale).
    /// </summary>
    private List<FutureTrajectoryPoint> ProjectApproach(
        ObjectMotionSnapshot predicted,
        double bakedAimX,
        double bakedAimY,
        double targetDirectionDegrees,
        double targetSpeedKmS,
        out bool isConfirmedIntercept,
        out FutureTrajectoryPoint interceptPoint)
    {
        if (ApproachPursuitMath.IsFlyThroughPhase(predicted.NavigationPhase))
        {
            return ProjectFlyThrough(
                predicted, bakedAimX, bakedAimY, targetDirectionDegrees, targetSpeedKmS,
                out isConfirmedIntercept, out interceptPoint);
        }

        isConfirmedIntercept = false;
        interceptPoint = default;

        var points = new List<FutureTrajectoryPoint>(FutureTrajectoryProjector.MaxSamplePoints);

        double x = predicted.X;
        double y = predicted.Y;
        double direction = predicted.Direction;
        double speedKmS = predicted.SpeedKmS;
        double? lockedCourse = predicted.NavigationLockedCourseDegrees;
        double trailDistance = Math.Max(0, predicted.NavigationApproachTrailDistanceWorldUnits ?? 0);
        string approachPhase = predicted.NavigationPhase == ApproachPursuitMath.TrailPhase && trailDistance > 0
            ? ApproachPursuitMath.TrailPhase
            : ApproachPursuitMath.FinalPhase;

        int turnStepDegrees = Math.Max(1, Math.Abs(predicted.TurnStepDegrees));
        long intervalMs = Math.Max(1, predicted.TurnStepIntervalMs);
        long phaseMs = Math.Max(1, predicted.TurnStepRemainingMs);

        return RunApproachPursuitPreview(
            points, x, y, direction, speedKmS, lockedCourse, approachPhase,
            bakedAimX, bakedAimY, trailDistance, targetDirectionDegrees, targetSpeedKmS,
            turnStepDegrees, intervalMs, predicted.NavigationAngularInertiaDegPerSec,
            phaseMs, includeInitialPhaseSegment: true);
    }

    /// <summary>
    /// Runs the per-turn trailing-pursuit preview (Trail then Final phase) shared by
    /// <see cref="ProjectApproach"/>'s direct entry and <see cref="ProjectFlyThrough"/>'s
    /// hand-off once its Dubins re-orientation curve completes. Appends sample points to
    /// <paramref name="points"/> (already seeded with the ship's current position by the
    /// caller) and returns it.
    /// </summary>
    private static List<FutureTrajectoryPoint> RunApproachPursuitPreview(
        List<FutureTrajectoryPoint> points,
        double x,
        double y,
        double direction,
        double speedKmS,
        double? lockedCourse,
        string approachPhase,
        double aimBaseX,
        double aimBaseY,
        double trailDistance,
        double targetDirectionDegrees,
        double targetSpeedKmS,
        int turnStepDegrees,
        long intervalMs,
        int angularInertiaDegPerSec,
        long phaseMs,
        bool includeInitialPhaseSegment)
    {
        // The baked point is phase-relative: the trailing staging point during Trail,
        // and the target itself during Final. When Trail completes, this base is moved
        // forward by the configured offset so projection continues to the real target.

        // Preview deliberately treats the target state from the current snapshot as
        // fixed. The authoritative engine now re-bakes that snapshot state from the
        // live target every completed cycle (including mid fly-through), so "current
        // snapshot" is never more than one cycle stale — the preview does not itself
        // guess any further into the future than this.
        (double X, double Y) AimAt(long _) => (aimBaseX, aimBaseY);

        bool ContinueFromTrailToTarget(long elapsedMs, double trailAimX, double trailAimY)
        {
            if (approachPhase != ApproachPursuitMath.TrailPhase)
                return false;

            double angleRad = targetDirectionDegrees * Math.PI / 180.0;
            aimBaseX = trailAimX + trailDistance * Math.Sin(angleRad);
            aimBaseY = trailAimY - trailDistance * Math.Cos(angleRad);
            approachPhase = ApproachPursuitMath.FinalPhase;
            lockedCourse = null;
            return true;
        }

        long elapsedMs;
        if (includeInitialPhaseSegment)
        {
            points.Add(new FutureTrajectoryPoint(x, y));

            // Phase until the first cycle boundary: straight flight with the CURRENT
            // course. Unlike every subsequent interval (each covered by
            // ApproachPursuitMath.Step's own segment-sweep arrival check), this phase
            // segment runs with no steering decision at all, so it needs its own arrival
            // check here — otherwise a ship already close to (or aimed straight at) the
            // target flies straight through it during this very first segment and the
            // preview keeps extending past the target instead of stopping there.
            double phaseStartX = x, phaseStartY = y;
            (x, y) = AdvanceStraight(x, y, direction, speedKmS, phaseMs);

            var (phaseAimX, phaseAimY) = AimAt(phaseMs);
            var phaseArrival = ApproachPursuitMath.CheckSegmentArrival(
                phaseStartX, phaseStartY, x, y, phaseAimX, phaseAimY);
            if (phaseArrival.IsArrived)
            {
                // Arrival is tolerance-based, but the rendered route is a promise to the
                // player: it must visually terminate on the navigation aim point itself.
                // Keeping ClosestX/Y here leaves a visible gap at high zoom whenever the
                // straight phase passes near (rather than exactly through) the aim point.
                points.Add(new FutureTrajectoryPoint(phaseAimX, phaseAimY));
                if (!ContinueFromTrailToTarget(phaseMs, phaseAimX, phaseAimY))
                    return points;

                x = phaseAimX;
                y = phaseAimY;
            }
            else
            {
                points.Add(new FutureTrajectoryPoint(x, y));
            }

            elapsedMs = phaseMs;
        }
        else
        {
            // Hand-off from a just-completed Dubins re-orientation curve (see
            // ProjectFlyThrough): the ship is already exactly at a fresh cycle boundary
            // and `points` already ends with its current position, so there is no
            // separate "wait until the first boundary" segment to draw here.
            elapsedMs = 0;
        }

        // Runs until actual arrival (IsArrived) or a stagnation timeout (see
        // ApproachStagnationTimeoutMs), bounded by ApproachTrajectoryMaxHorizonMs/
        // ApproachTrajectoryMaxIterations as an outer safety backstop only — see their
        // doc-comments. If the loop ever exhausts a bound WITHOUT arriving, that is not
        // automatically a bug: a stalled ship (SpeedKmS≈0) or one simply slower than a
        // fleeing target can never geometrically catch it, and legitimately exhausts the
        // bound every time. A hard assertion was considered here (the fix's design notes
        // suggested "surface it, don't silently swallow it") but rejected — a pure
        // client-side projector has no business running a reachability check to tell that
        // ordinary case apart from an actual residual convergence bug, so a hard assert
        // would false-fire on the ordinary case. In that situation the caller simply gets
        // the points computed up to the closest approach reached — an honest "this is as
        // close as it gets" preview, not a guaranteed-complete trajectory.
        int iterations = 0;
        double bestDistanceToAim = double.MaxValue;
        long bestDistanceElapsedMs = elapsedMs;
        int bestDistancePointIndex = points.Count - 1;
        while (elapsedMs < ApproachTrajectoryMaxHorizonMs && iterations < ApproachTrajectoryMaxIterations)
        {
            iterations++;

            var (aimX, aimY) = AimAt(elapsedMs);

            double distanceToAim = Math.Sqrt((aimX - x) * (aimX - x) + (aimY - y) * (aimY - y));
            if (distanceToAim < bestDistanceToAim)
            {
                bestDistanceToAim = distanceToAim;
                bestDistanceElapsedMs = elapsedMs;
                bestDistancePointIndex = points.Count - 1;
            }
            else if (elapsedMs - bestDistanceElapsedMs >= ApproachStagnationTimeoutMs)
            {
                points.RemoveRange(bestDistancePointIndex + 1, points.Count - bestDistancePointIndex - 1);
                var (fixedAimX, fixedAimY) = AimAt(bestDistanceElapsedMs);
                points.Add(new FutureTrajectoryPoint(fixedAimX, fixedAimY));
                return points;
            }

            var step = ApproachPursuitMath.Step(
                x, y, direction, speedKmS,
                aimX, aimY, targetDirectionDegrees, targetSpeedKmS,
                trailDistanceWorldUnits: 0,
                turnStepDegrees: turnStepDegrees,
                angularInertiaDegPerSec: angularInertiaDegPerSec,
                stepTimeMs: intervalMs,
                lockedCourseDegrees: lockedCourse);

            direction = step.NewDirectionDegrees;
            lockedCourse = step.LockedCourseDegrees;

            if (step.IsArrived)
            {
                points.Add(new FutureTrajectoryPoint(aimX, aimY));
                if (!ContinueFromTrailToTarget(elapsedMs, aimX, aimY))
                    return points;

                x = aimX;
                y = aimY;
                bestDistanceToAim = double.MaxValue;
                bestDistanceElapsedMs = elapsedMs;
                bestDistancePointIndex = points.Count - 1;
                continue;
            }

            (x, y) = AdvanceStraight(x, y, direction, speedKmS, intervalMs);
            points.Add(new FutureTrajectoryPoint(x, y));

            // A ship that cannot out-pace the target (equal or slower speed) can never
            // close the remaining distance — mirrors SimulationEngine.ApplyApproachStep's
            // own check. Once locked onto the bearing to the (receding) aim point, that
            // bearing sits directly on the target's own line of motion, so the preview
            // already shows the ship moving in the target's own direction here — the
            // achievable goal when true arrival isn't — so stop rather than keep drawing
            // an endless chase line toward a gap that can never close. Only meaningful
            // for a hand-off from fly-through, where the aim point was just extrapolated
            // to the target's LIVE position — the direct-entry case deliberately treats
            // its baked aim point as fixed for this whole call (never extrapolated), so
            // "target speed" doesn't describe anything actually receding here; that case
            // still falls back to the stagnation-timeout check below instead.
            if (!includeInitialPhaseSegment && lockedCourse is not null && speedKmS <= targetSpeedKmS)
                return points;

            elapsedMs += intervalMs;
        }

        var (finalAimX, finalAimY) = AimAt(elapsedMs);
        points.Add(new FutureTrajectoryPoint(finalAimX, finalAimY));

        return points;
    }

    private const double UnitsPerKmS = 10.0; // 1 km/s -> 10 world units/s (matches ApproachPursuitMath).

    private static List<FutureTrajectoryPoint> ProjectFlyThrough(
        ObjectMotionSnapshot predicted,
        double targetX,
        double targetY,
        double targetDirectionDegrees,
        double targetSpeedKmS,
        out bool isConfirmedIntercept,
        out FutureTrajectoryPoint interceptPoint)
    {
        isConfirmedIntercept = false;
        interceptPoint = default;
        var points = new List<FutureTrajectoryPoint>(FutureTrajectoryProjector.MaxSamplePoints);
        double x = predicted.X;
        double y = predicted.Y;
        double direction = predicted.Direction;
        double speedKmS = predicted.SpeedKmS;
        long intervalMs = Math.Max(1, predicted.TurnStepIntervalMs);
        long untilNextTurnMs = Math.Max(1, predicted.TurnStepRemainingMs);
        string phase = predicted.NavigationPhase!;
        ApproachFlyThroughPlan? plan = null;
        long elapsedMs = untilNextTurnMs;

        // A confirmed rendezvous pose is a fixed, mathematically exact point on the
        // target's straight-line path (target(t) = targetPosition + t * targetVelocity)
        // — always recoverable from the CURRENT live target pose plus however much curve
        // is left to fly, independent of how the discrete per-cycle stepping loop below
        // (AdvanceStraight'ing one interval at a time) happens to trace toward it. Reading
        // the endpoint off that discretized trace instead (the ship's own tracked (x, y)
        // at "arrival") accumulates the same small per-cycle quantization the fallback
        // path already documents below — over a long multi-cycle curve that drift is
        // enough to visibly miss the target's own drawn trajectory line. Computing it
        // analytically here keeps the marker exactly on that line by construction.
        double targetDirectionRad = targetDirectionDegrees * Math.PI / 180.0;
        double targetVelocityX = targetSpeedKmS * UnitsPerKmS * Math.Sin(targetDirectionRad);
        double targetVelocityY = -targetSpeedKmS * UnitsPerKmS * Math.Cos(targetDirectionRad);

        // story-20260829-210641.md §10, Checkpoint 2: a confirmed intercept-solve plan
        // (FlyThroughIntercept:) resumes exactly like a fallback fly-through plan
        // (FlyThrough:) — same encoded remaining-segment fields — but on arrival it must
        // complete immediately rather than hand off to a live-tracking Final preview (see
        // below). Checked before FlyThroughPhasePrefix since "FlyThroughIntercept:" is not
        // itself prefixed by "FlyThrough:" (they diverge at the 11th character), so either
        // order would be unambiguous, but checking the more specific prefix first keeps
        // that non-overlap explicit rather than relied upon.
        bool isInterceptPlan = false;
        if (phase.StartsWith(ApproachPursuitMath.FlyThroughInterceptPhasePrefix, StringComparison.Ordinal))
        {
            plan = new ApproachFlyThroughPlan(
                phase[ApproachPursuitMath.FlyThroughInterceptPhasePrefix.Length..],
                predicted.NavigationEscapeCourseDegrees ?? 0,
                predicted.NavigationRequiredDepartureDistance ?? 0,
                predicted.NavigationLockedCourseDegrees ?? 0);
            isInterceptPlan = true;

            // Resuming an already-confirmed curve: targetX/Y here are the target's LIVE
            // (re-baked-every-cycle) position, not the original rendezvous pose — recover
            // that pose from here via the curve's own remaining arc length instead (see
            // comment above): remaining flight time = remaining units / ship speed.
            double remainingTimeSeconds = plan.Value.RemainingUnits / (speedKmS * UnitsPerKmS);
            isConfirmedIntercept = true;
            interceptPoint = new FutureTrajectoryPoint(
                targetX + remainingTimeSeconds * targetVelocityX,
                targetY + remainingTimeSeconds * targetVelocityY);
        }
        else if (phase.StartsWith(ApproachPursuitMath.FlyThroughPhasePrefix, StringComparison.Ordinal))
        {
            plan = new ApproachFlyThroughPlan(
                phase[ApproachPursuitMath.FlyThroughPhasePrefix.Length..],
                predicted.NavigationEscapeCourseDegrees ?? 0,
                predicted.NavigationRequiredDepartureDistance ?? 0,
                predicted.NavigationLockedCourseDegrees ?? 0);
        }

        points.Add(new FutureTrajectoryPoint(x, y));
        (x, y) = AdvanceStraight(x, y, direction, speedKmS, untilNextTurnMs);
        points.Add(new FutureTrajectoryPoint(x, y));

        int iterations = 0;
        while (iterations++ < ApproachTrajectoryMaxIterations)
        {
            ApproachFlyThroughPlanStep step;
            if (plan is null)
            {
                // Mirror SimulationEngine.ApplyApproachStep's FlyThroughPending handling
                // (story-20260829-210641.md §10, Checkpoint 2): try an exact-rendezvous
                // solve FIRST, from the same live ship/target pose about to be passed to
                // CreateFlyThroughPlan below, before falling back to the captured-pose
                // curve. The fallback path (HasIntercept == false) is byte-for-byte
                // unchanged from before this method learned about intercepts.
                var interceptSolution = ApproachPursuitMath.SolveInterceptFlyThroughPlan(
                    x, y, direction, speedKmS,
                    targetX, targetY, targetDirectionDegrees, targetSpeedKmS,
                    predicted.NavigationAngularInertiaDegPerSec);

                if (interceptSolution.HasIntercept)
                {
                    plan = interceptSolution.Plan;
                    isInterceptPlan = true;
                    targetX = interceptSolution.TargetXAtIntercept;
                    targetY = interceptSolution.TargetYAtIntercept;
                    targetDirectionDegrees = interceptSolution.TargetDirectionAtIntercept;
                    isConfirmedIntercept = true;
                    interceptPoint = new FutureTrajectoryPoint(targetX, targetY);
                }
                else
                {
                    plan = ApproachPursuitMath.CreateFlyThroughPlan(
                        x, y, direction, speedKmS,
                        targetX, targetY, targetDirectionDegrees,
                        predicted.NavigationAngularInertiaDegPerSec);
                }

                step = ApproachPursuitMath.AdvanceFlyThroughPlan(
                    plan.Value, direction, targetDirectionDegrees,
                    travelledUnits: 0,
                    turnStepDegrees: Math.Max(1, Math.Abs(predicted.TurnStepDegrees)));
            }
            else
            {
                double travelledUnits = speedKmS * (intervalMs / 1000.0) * 10.0;
                step = ApproachPursuitMath.AdvanceFlyThroughPlan(
                    plan.Value, direction, targetDirectionDegrees,
                    travelledUnits,
                    Math.Max(1, Math.Abs(predicted.TurnStepDegrees)));
            }

            direction = step.NewDirectionDegrees;
            plan = step.RemainingPlan;
            if (step.IsArrived)
            {
                if (isInterceptPlan)
                {
                    // A confirmed intercept-solve curve was built directly to the
                    // target's own future pose at t* — arrival here IS the rendezvous
                    // with the live target, so (unlike the fallback below, which aims at
                    // a pose captured when the leg was planned and can go stale) there is
                    // no stale captured pose to hand off to a live-tracking Final preview
                    // for. Mirrors SimulationEngine.ApplyApproachStep's immediate-
                    // completion branch for FlyThroughIntercept:. isConfirmedIntercept and
                    // interceptPoint were already set above (as soon as isInterceptPlan
                    // became true).
                    //
                    // The tracked (x, y) added along the way only approximates that pose —
                    // AdvanceFlyThroughPlan's arrival is bookkeeping-based (cumulative
                    // travelled distance against the planned segment lengths, discretized
                    // into per-cycle straight chords at the quantized turn heading), not a
                    // position match, so it can land a visible distance short of the true
                    // rendezvous pose on a long curve (the drift compounds over many
                    // cycles). The drawn LINE must actually reach the marked intercept
                    // point precisely, not stop short of it — but replacing only the very
                    // last point would turn that accumulated drift into one single,
                    // visually obvious final-segment jump instead. Blend the correction in
                    // smoothly over the tail of the curve instead, so the line eases into
                    // the exact point rather than snapping to it.
                    SmoothlyConvergeTailToPoint(points, interceptPoint);
                    return points;
                }

                // A ship that cannot out-pace the target (equal or slower speed) can
                // never close the remaining distance — mirrors SimulationEngine
                // .ApplyApproachStep's own check. The fly-through curve was built to
                // arrive with the ship's heading EXACTLY matching the target's own
                // heading (that is what a Dubins curve to a given heading guarantees),
                // so stopping right here already shows the achievable goal — trailing
                // behind the target, moving in its same direction — instead of drawing
                // an endless chase line toward a live-tracking Final phase a slower
                // ship could never actually complete.
                if (speedKmS <= targetSpeedKmS)
                    return points; // (x, y) is already the last point added below

                // AdvanceFlyThroughPlan's arrival is bookkeeping-based (cumulative
                // travelled distance against the planned segment lengths), not a
                // position/heading match — so the ship's actually-tracked (x, y, direction)
                // at this point rarely lands exactly on the target's own heading line,
                // especially after a long re-orientation curve (a real, if small, turn
                // quantization artifact of the module's discrete turnStepDegrees steering
                // — the same imprecision the authoritative engine has at this same point).
                // The engine corrects for exactly this by handing off into a live-tracking
                // Final-phase pursuit once the curve completes (see SimulationEngine
                // .ApplyApproachStep) rather than declaring victory at the stale captured
                // pose — mirror that here instead of snapping straight to (targetX,targetY),
                // which produced a visible kink/near-miss whenever the tracked point and
                // the target disagreed.
                var (liveTargetX, liveTargetY) = ApproachPursuitMath.ExtrapolatePosition(
                    targetX, targetY, targetDirectionDegrees, targetSpeedKmS, elapsedMs);

                return RunApproachPursuitPreview(
                    points, x, y, direction, speedKmS, lockedCourse: null,
                    approachPhase: ApproachPursuitMath.FinalPhase,
                    aimBaseX: liveTargetX, aimBaseY: liveTargetY,
                    trailDistance: 0, targetDirectionDegrees, targetSpeedKmS,
                    turnStepDegrees: Math.Max(1, Math.Abs(predicted.TurnStepDegrees)),
                    intervalMs, predicted.NavigationAngularInertiaDegPerSec,
                    phaseMs: 0, includeInitialPhaseSegment: false);
            }

            (x, y) = AdvanceStraight(x, y, direction, speedKmS, intervalMs);
            points.Add(new FutureTrajectoryPoint(x, y));
            elapsedMs += intervalMs;
        }

        points.Add(new FutureTrajectoryPoint(targetX, targetY));
        return points;
    }

    /// <summary>
    /// Nudges the tail of a confirmed intercept curve's discretized point list so it
    /// ends EXACTLY at <paramref name="exactEndpoint"/> (the analytically computed
    /// rendezvous pose — see the caller) instead of at the drift-accumulated tracked
    /// position the per-cycle stepping loop actually landed on. The correction is eased
    /// in over the last several samples (quadratic ramp, negligible at the start of the
    /// span, full at the last point) rather than applied only to the final point, so the
    /// line visibly bends smoothly into the marker instead of jumping there in one
    /// oversized final segment.
    /// </summary>
    private static void SmoothlyConvergeTailToPoint(
        List<FutureTrajectoryPoint> points, FutureTrajectoryPoint exactEndpoint)
    {
        if (points.Count == 0)
            return;

        var last = points[^1];
        double errorX = exactEndpoint.X - last.X;
        double errorY = exactEndpoint.Y - last.Y;

        // Spread the correction over up to a third of the curve's own samples (capped,
        // so a short curve doesn't have its ENTIRE shape visibly bent) — long enough
        // that even the largest drift seen in practice (tens of world units over a long
        // multi-cycle curve) still eases in gently rather than as a sharp final kink.
        const int MaxCorrectionSpan = 40;
        int correctionSpan = Math.Min(points.Count, Math.Max(1, Math.Min(MaxCorrectionSpan, points.Count / 3)));
        int startIndex = points.Count - correctionSpan;

        for (int i = startIndex; i < points.Count; i++)
        {
            double t = (double)(i - startIndex + 1) / correctionSpan; // (0, 1], 1 at the last point
            double eased = t * t;
            var p = points[i];
            points[i] = new FutureTrajectoryPoint(p.X + errorX * eased, p.Y + errorY * eased);
        }
    }

    private static (double X, double Y) ClosestApproach(
        double x, double y, double directionDegrees, double speedKmS, long intervalMs,
        double targetX, double targetY)
    {
        double stepDist = speedKmS * (intervalMs / 1000.0) * 10.0;
        double angleRad = directionDegrees * Math.PI / 180.0;
        double segDx = stepDist * Math.Sin(angleRad);
        double segDy = -stepDist * Math.Cos(angleRad);
        double tDx = targetX - x;
        double tDy = targetY - y;
        double dot = tDx * segDx + tDy * segDy;
        double lenSq = segDx * segDx + segDy * segDy;
        double t = lenSq > 0 ? Math.Clamp(dot / lenSq, 0.0, 1.0) : 0.0;
        return (x + t * segDx, y + t * segDy);
    }

    private static (double X, double Y) AdvanceStraight(
        double x, double y, double directionDegrees, double speedKmS, long elapsedMs)
    {
        // Mirror of LinearMotionPredictor.AdvanceStraight (1 km/s = 10 world units/s).
        double distance = speedKmS * (elapsedMs / 1000.0) * 10.0;
        double angleRad = directionDegrees * Math.PI / 180.0;
        return (x + distance * Math.Sin(angleRad), y - distance * Math.Cos(angleRad));
    }

    private static double NormalizeDirection(double degrees)
    {
        double normalized = degrees % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }
}
