using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion;

public readonly record struct HostileMotionSample(double X, double Y, double SpeedKmS, double Direction);

/// <summary>
/// Pure bounded steering and analytical segment execution. Only Engine calls Plan at fixed
/// physical boundaries; prediction never plans another segment or assumes a target binding.
/// </summary>
public static class HostileOrbitMath
{
    public const long DecisionIntervalMs = 100;

    public static bool ChooseClockwise(ObjectMotionSnapshot ship, ObjectMotionSnapshot target)
    {
        double radial = RadialBearing(ship, target);
        return Math.Abs(Delta(ship.Direction, radial + 90)) <= Math.Abs(Delta(ship.Direction, radial - 90));
    }

    public static HostileCombatSnapshot Plan(ObjectMotionSnapshot ship, ObjectMotionSnapshot target,
        long epochMs, string engineModuleId, string weaponModuleId, double radiusKm,
        double maxSpeedKmS, double accelerationKmS2, double turnRateDegPerSec,
        HostileCombatSnapshot? previous = null)
    {
        RequirePositive(radiusKm, nameof(radiusKm));
        RequirePositive(maxSpeedKmS, nameof(maxSpeedKmS));
        RequireNonnegative(accelerationKmS2, nameof(accelerationKmS2));
        RequireNonnegative(turnRateDegPerSec, nameof(turnRateDegPerSec));
        bool clockwise = previous?.Clockwise ?? ChooseClockwise(ship, target);
        double dx = (ship.X - target.X) / 10, dy = (ship.Y - target.Y) / 10;
        double distance = Math.Sqrt(dx * dx + dy * dy);
        double radial = RadialBearing(ship, target) * Math.PI / 180;
        double rx = Math.Sin(radial), ry = -Math.Cos(radial);
        double desiredHeading, desiredSpeed;
        HostileCombatPhase phase;

        if (target.SpeedKmS >= maxSpeedKmS || accelerationKmS2 == 0 || turnRateDegPerSec == 0)
        {
            // No positive relative speed reserve: retain the encounter direction for a later retry.
            desiredHeading = Normalize(radial + Math.PI) * 180 / Math.PI;
            desiredSpeed = maxSpeedKmS;
            phase = HostileCombatPhase.Pursuit;
        }
        else
        {
            // Reserve enough speed, acceleration and angular authority for a moving centre.
            double relativeSpeed = Math.Min((maxSpeedKmS - target.SpeedKmS) * 0.5,
                Math.Min(Math.Sqrt(accelerationKmS2 * radiusKm * 0.25),
                    turnRateDegPerSec * Math.PI / 180 * radiusKm * 0.25));
            double radialCorrection = Math.Clamp(2 * (distance - radiusKm) / radiusKm, -1, 1);
            double sign = clockwise ? 1 : -1;
            double vx = sign * -ry - radialCorrection * rx;
            double vy = sign * rx - radialCorrection * ry;
            double scale = relativeSpeed / Math.Sqrt(vx * vx + vy * vy);
            double targetHeading = target.Direction * Math.PI / 180;
            vx = vx * scale + target.SpeedKmS * Math.Sin(targetHeading);
            vy = vy * scale - target.SpeedKmS * Math.Cos(targetHeading);
            desiredSpeed = Math.Sqrt(vx * vx + vy * vy);
            desiredHeading = desiredSpeed > 1e-12 ? Math.Atan2(vx, -vy) * 180 / Math.PI : ship.Direction;
            double error = Math.Abs(distance - radiusKm) / radiusKm;
            phase = error <= (previous?.Phase == HostileCombatPhase.OrbitHold ? 0.10 : 0.05)
                ? HostileCombatPhase.OrbitHold
                : distance <= radiusKm * 1.5 ? HostileCombatPhase.OrbitEntry : HostileCombatPhase.Pursuit;
        }

        double dt = DecisionIntervalMs / 1000.0;
        double acceleration = Math.Clamp((desiredSpeed - ship.SpeedKmS) / dt, -accelerationKmS2, accelerationKmS2);
        double turn = Math.Clamp(Delta(ship.Direction, desiredHeading) / dt, -turnRateDegPerSec, turnRateDegPerSec);
        return new(target.ObjectId, engineModuleId, weaponModuleId, radiusKm, clockwise, phase,
            new(epochMs, DecisionIntervalMs, 0, ship.X, ship.Y, ship.SpeedKmS, ship.Direction, acceleration, turn));
    }

    /// <summary>Configured D33 attack steering. An absent band preserves the EP-0008 orbit exactly.</summary>
    public static HostileCombatSnapshot PlanAttack(ObjectMotionSnapshot ship, ObjectMotionSnapshot target,
        long epochMs, string engineModuleId, string weaponModuleId, double standoffKm,
        double maxSpeedKmS, double accelerationKmS2, double turnRateDegPerSec,
        double? bandKm = null, double flankOffsetDeg = 0, HostileCombatSnapshot? previous = null)
    {
        if (bandKm is null)
            return Plan(ship, target, epochMs, engineModuleId, weaponModuleId, standoffKm,
                maxSpeedKmS, accelerationKmS2, turnRateDegPerSec, previous);
        RequirePositive(standoffKm, nameof(standoffKm));
        RequirePositive(maxSpeedKmS, nameof(maxSpeedKmS));
        RequireNonnegative(accelerationKmS2, nameof(accelerationKmS2));
        RequireNonnegative(turnRateDegPerSec, nameof(turnRateDegPerSec));
        if (!double.IsFinite(bandKm.Value) || bandKm.Value < 0 || bandKm.Value >= standoffKm)
            throw new ArgumentOutOfRangeException(nameof(bandKm));
        if (!double.IsFinite(flankOffsetDeg) || Math.Abs(flankOffsetDeg) > 180)
            throw new ArgumentOutOfRangeException(nameof(flankOffsetDeg));

        double dx = (ship.X - target.X) / 10, dy = (ship.Y - target.Y) / 10;
        double distanceKm = Math.Sqrt(dx * dx + dy * dy);
        if (distanceKm >= standoffKm - bandKm.Value && distanceKm <= standoffKm + bandKm.Value)
        {
            var orbit = Plan(ship, target, epochMs, engineModuleId, weaponModuleId, standoffKm,
                maxSpeedKmS, accelerationKmS2, turnRateDegPerSec, previous);
            return SmoothAttack(orbit.Phase == HostileCombatPhase.Pursuit ? orbit : orbit with { Phase = HostileCombatPhase.OrbitHold }, previous, accelerationKmS2, turnRateDegPerSec, maxSpeedKmS);
        }

        double heading;
        HostileCombatPhase phase;
        if (distanceKm < standoffKm - bandKm.Value)
        {
            heading = RadialBearing(ship, target);
            phase = HostileCombatPhase.Retreat;
        }
        else
        {
            double targetRadians = target.Direction * Math.PI / 180;
            double leadSeconds = ReachableOrbitLeadSeconds((target.X - ship.X) / 10, (target.Y - ship.Y) / 10,
                target.SpeedKmS * Math.Sin(targetRadians), -target.SpeedKmS * Math.Cos(targetRadians),
                maxSpeedKmS, standoffKm);
            double leadX = target.X + 10 * target.SpeedKmS * leadSeconds * Math.Sin(targetRadians);
            double leadY = target.Y - 10 * target.SpeedKmS * leadSeconds * Math.Cos(targetRadians);
            double leadBearing = Math.Atan2(leadX - ship.X, ship.Y - leadY) * 180 / Math.PI;
            heading = leadBearing + flankOffsetDeg * Math.Min(1, (distanceKm - standoffKm) / 100);
            phase = HostileCombatPhase.FlankApproach;
        }

        double dt = DecisionIntervalMs / 1000.0;
        double acceleration = Math.Clamp((maxSpeedKmS - ship.SpeedKmS) / dt, -accelerationKmS2, accelerationKmS2);
        double turn = Math.Clamp(Delta(ship.Direction, heading) / dt, -turnRateDegPerSec, turnRateDegPerSec);
        var attack = new HostileCombatSnapshot(target.ObjectId, engineModuleId, weaponModuleId, standoffKm,
            previous?.Clockwise ?? ChooseClockwise(ship, target), phase,
            new(epochMs, DecisionIntervalMs, 0, ship.X, ship.Y, ship.SpeedKmS, ship.Direction, acceleration, turn));
        // Blend into the orbit controller before crossing the band boundary. Both
        // acceleration and curvature then approach the same limit on either side.
        var orbitPlan = Plan(ship, target, epochMs, engineModuleId, weaponModuleId, standoffKm,
            maxSpeedKmS, accelerationKmS2, turnRateDegPerSec, previous);
        double edgeDistance = Math.Max(0, Math.Abs(distanceKm - standoffKm) - bandKm.Value);
        double blend = Math.Clamp(edgeDistance / Math.Max(bandKm.Value, standoffKm * .5), 0, 1);
        blend = blend * blend * (3 - 2 * blend);
        attack = attack with
        {
            Segment = attack.Segment with
            {
                AccelerationKmS2 = orbitPlan.Segment.AccelerationKmS2 * (1 - blend) + acceleration * blend,
                TurnRateDegPerSec = orbitPlan.Segment.TurnRateDegPerSec * (1 - blend) + turn * blend
            }
        };
        return SmoothAttack(attack, previous, accelerationKmS2, turnRateDegPerSec, maxSpeedKmS);
    }

    // Earliest |relativePosition + targetVelocity*t| = orbitRadius + shipSpeed*t.
    // Equal/faster targets need a real reachable root, not an arbitrary long lead.
    private static double ReachableOrbitLeadSeconds(double x, double y, double vx, double vy, double speed, double radius)
    {
        double c = x * x + y * y - radius * radius;
        if (c <= 0) return 0;
        double a = vx * vx + vy * vy - speed * speed;
        double b = 2 * (x * vx + y * vy - radius * speed);
        if (Math.Abs(a) <= 1e-12 * Math.Max(1, speed * speed + vx * vx + vy * vy))
            return b < 0 ? -c / b : 0;
        double discriminant = b * b - 4 * a * c;
        if (discriminant < 0 || !double.IsFinite(discriminant)) return 0;
        double q = -.5 * (b + Math.CopySign(Math.Sqrt(discriminant), b));
        double first = q / a, second = q == 0 ? double.PositiveInfinity : c / q;
        double result = Math.Min(first >= 0 ? first : double.PositiveInfinity, second >= 0 ? second : double.PositiveInfinity);
        return double.IsFinite(result) ? result : 0;
    }

    private static HostileCombatSnapshot SmoothAttack(HostileCombatSnapshot plan, HostileCombatSnapshot? previous,
        double accelerationLimit, double turnLimit, double maxSpeedKmS)
    {
        if (previous is null) return plan;
        // Ramp control changes over one physical second instead of reversing
        // maximum thrust/turn every 100 ms. The previous segment is persisted.
        double dt = DecisionIntervalMs / 1000.0;
        return plan with
        {
            Segment = plan.Segment with
            {
                AccelerationKmS2 = Math.Clamp(Math.Clamp(plan.Segment.AccelerationKmS2,
                previous.Segment.AccelerationKmS2 - accelerationLimit * dt,
                previous.Segment.AccelerationKmS2 + accelerationLimit * dt),
                Math.Max(-accelerationLimit, -plan.Segment.StartSpeedKmS / dt),
                Math.Min(accelerationLimit, Math.Max(0, maxSpeedKmS - plan.Segment.StartSpeedKmS) / dt)),
                TurnRateDegPerSec = Math.Clamp(Math.Clamp(plan.Segment.TurnRateDegPerSec,
                previous.Segment.TurnRateDegPerSec - turnLimit * dt,
                previous.Segment.TurnRateDegPerSec + turnLimit * dt), -turnLimit, turnLimit)
            }
        };
    }

    public static ObjectMotionSnapshot Predict(ObjectMotionSnapshot state, long elapsedMs)
    {
        if (state.HostileCombat is not { } hostile) return state;
        var segment = hostile.Segment;
        long elapsed = elapsedMs >= 0
            ? segment.ElapsedMs + Math.Min(elapsedMs, segment.DurationMs - segment.ElapsedMs)
            : segment.ElapsedMs + Math.Max(elapsedMs, -segment.ElapsedMs);
        var sample = Sample(segment, elapsed);
        return state with
        {
            X = sample.X,
            Y = sample.Y,
            SpeedKmS = sample.SpeedKmS,
            Direction = sample.Direction,
            HostileCombat = hostile with { Segment = segment with { ElapsedMs = elapsed } }
        };
    }

    /// <summary>Fractional physical milliseconds are supported for continuous collision queries.</summary>
    public static HostileMotionSample Sample(HostileMotionSegment segment, double elapsedMs)
    {
        double t = Math.Clamp(elapsedMs, 0, segment.DurationMs) / 1000;
        double h = segment.TurnRateDegPerSec * Math.PI / 180 * t / 2;
        double sinc = Math.Abs(h) < 1e-4 ? 1 - h * h / 6 + Math.Pow(h, 4) / 120 : Math.Sin(h) / h;
        double slope = Math.Abs(h) < 1e-4 ? h / 3 - h * h * h / 30 : (Math.Sin(h) - h * Math.Cos(h)) / (h * h);
        double mid = segment.StartDirectionDegrees * Math.PI / 180 + h;
        double forward = t * (segment.StartSpeedKmS + segment.AccelerationKmS2 * t / 2) * sinc;
        double lateral = segment.AccelerationKmS2 * t * t / 2 * slope;
        return new(segment.StartX + 10 * (forward * Math.Sin(mid) + lateral * Math.Cos(mid)),
            segment.StartY + 10 * (-forward * Math.Cos(mid) + lateral * Math.Sin(mid)),
            segment.StartSpeedKmS + segment.AccelerationKmS2 * t,
            NormalizeDegrees(segment.StartDirectionDegrees + segment.TurnRateDegPerSec * t));
    }

    private static double RadialBearing(ObjectMotionSnapshot ship, ObjectMotionSnapshot target) =>
        ship.X == target.X && ship.Y == target.Y ? ship.Direction :
        Math.Atan2(ship.X - target.X, target.Y - ship.Y) * 180 / Math.PI;

    private static double Delta(double from, double to) => (NormalizeDegrees(to - from) + 180) % 360 - 180;
    private static double NormalizeDegrees(double angle) => (angle % 360 + 360) % 360;
    private static double Normalize(double angle) => (angle % (2 * Math.PI) + 2 * Math.PI) % (2 * Math.PI);

    private static void RequirePositive(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(name);
    }

    private static void RequireNonnegative(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(name);
    }
}
