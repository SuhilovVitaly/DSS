namespace DeepSpaceSaga.Motion;

/// <summary>
/// Result of one navigation waypoint step: the signed turn delta to apply (degrees,
/// 0 = fly straight) and whether the ship has arrived at (or flown past) the target.
/// </summary>
public readonly record struct NavigationStepResult(double TurnDeltaDegrees, bool IsArrived);

/// <summary>
/// Pure, deterministic step math for navigation to a world point (shared by Engine and
/// Client). No state, no Engine/Contracts references — only numbers.
///
/// Model (approved): a turn now would overshoot iff the distance to the target is
/// r &lt; R, where R = v/ω is the turn radius (v = speed m/s, ω = angular inertia
/// rad/s; world units R/100). While r &lt; R the ship waits — it flies straight.
/// When r ≥ R a turn is possible and applied in steps of at most TurnStepDegrees.
///
/// Direction convention: degrees, 0° = up, 90° = right, clockwise (same as the
/// rest of the motion system).
/// </summary>
public static class NavigationWaypointMath
{
    /// <summary>Distance (world units) at or below which the ship has arrived.</summary>
    public const double ArrivalEpsilon = 1.0;

    /// <summary>
    /// Compute one navigation step from the ship's current state toward the target.
    /// </summary>
    /// <param name="x">Ship position X, world units.</param>
    /// <param name="y">Ship position Y, world units.</param>
    /// <param name="directionDegrees">Ship course, degrees (0° = up, clockwise).</param>
    /// <param name="speedKmS">Ship speed, km/s.</param>
    /// <param name="targetX">Target X, world units.</param>
    /// <param name="targetY">Target Y, world units.</param>
    /// <param name="turnStepDegrees">Maximum turn per step (module TurnStepDegrees).</param>
    /// <param name="angularInertiaDegPerSec">Angular inertia, degrees per second.</param>
    public static NavigationStepResult Step(
        double x,
        double y,
        double directionDegrees,
        double speedKmS,
        double targetX,
        double targetY,
        int turnStepDegrees,
        int angularInertiaDegPerSec)
    {
        double dx = targetX - x;
        double dy = targetY - y;
        double r = Math.Sqrt(dx * dx + dy * dy);

        if (r <= ArrivalEpsilon)
            return new NavigationStepResult(0, IsArrived: true);

        double bearingDegrees = BearingDegrees(dx, dy);
        double delta = ShortestSignedAngleDegrees(directionDegrees, bearingDegrees);

        if (Math.Abs(delta) <= turnStepDegrees / 2.0)
        {
            // On course: keep the current heading (AC5). If the target is at or behind
            // the ship (dot ≤ 0) the ship has flown past it — arrived.
            double directionRad = directionDegrees * Math.PI / 180.0;
            double dot = dx * Math.Sin(directionRad) - dy * Math.Cos(directionRad);
            return dot <= 0
                ? new NavigationStepResult(0, IsArrived: true)
                : new NavigationStepResult(0, IsArrived: false);
        }

        // Turn radius R = v/ω in world units (1 unit = 100 m, so /100).
        if (angularInertiaDegPerSec <= 0)
            return new NavigationStepResult(0, IsArrived: false); // cannot turn — fly straight

        double turnRadiusUnits = speedKmS <= 0
            ? 0 // stationary — a turn on the spot is always possible
            : speedKmS * 1000.0 / (angularInertiaDegPerSec * Math.PI / 180.0) / 100.0;

        if (r < turnRadiusUnits)
            return new NavigationStepResult(0, IsArrived: false); // wait — turning now would miss

        // r ≥ R: turn toward the bearing, at most one step (partial turns are fine,
        // never faster than the angular inertia allows).
        double turnDelta = Math.Sign(delta) * Math.Min(Math.Abs(delta), turnStepDegrees);
        return new NavigationStepResult(turnDelta, IsArrived: false);
    }

    /// <summary>Bearing from (0,0) toward (dx, dy) in degrees, 0° = up, clockwise, in [0, 360).</summary>
    private static double BearingDegrees(double dx, double dy)
    {
        double degrees = Math.Atan2(dx, -dy) * 180.0 / Math.PI;
        return degrees < 0 ? degrees + 360 : degrees;
    }

    /// <summary>Shortest signed angle from course to bearing, in (-180, 180].</summary>
    private static double ShortestSignedAngleDegrees(double fromDegrees, double toDegrees)
    {
        double raw = (toDegrees - fromDegrees) % 360;
        if (raw > 180)
            raw -= 360;
        else if (raw <= -180)
            raw += 360;
        return raw;
    }
}
