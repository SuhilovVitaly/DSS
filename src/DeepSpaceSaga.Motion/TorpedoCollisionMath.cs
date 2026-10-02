namespace DeepSpaceSaga.Motion;

/// <summary>A continuous world-space path at absolute physical milliseconds.
/// Speed bounds use world units/ms; acceleration uses world units/ms².
/// Null acceleration permits corners; zero promises a straight constant-velocity path.
/// Callers must split discontinuities in position before searching.</summary>
public sealed record CollisionPath(
    Func<double, (double X, double Y)> Position,
    double MaxSpeed,
    double? MaxAcceleration = null);

public readonly record struct TorpedoContact(double MotionTimeMs, double X, double Y);

public static class TorpedoCollisionMath
{
    // One hundred-thousandth of a metre. This is numerical geometry tolerance,
    // never a pixel radius. Time is refined until the remaining travel fits it.
    public const double DistanceTolerance = 1e-7;

    public static TorpedoContact? FirstContact(CollisionPath projectilePath, CollisionPath targetPath,
        double fromMotionMs, double toMotionMs, double radiusWorldUnits = 5)
    {
        Validate(projectilePath);
        Validate(targetPath);
        if (!double.IsFinite(fromMotionMs) || !double.IsFinite(toMotionMs) || toMotionMs < fromMotionMs)
            throw new ArgumentOutOfRangeException(nameof(toMotionMs));
        if (!double.IsFinite(radiusWorldUnits) || radiusWorldUnits < 0)
            throw new ArgumentOutOfRangeException(nameof(radiusWorldUnits));
        double speed = projectilePath.MaxSpeed + targetPath.MaxSpeed;
        double? acceleration = projectilePath.MaxAcceleration + targetPath.MaxAcceleration;
        var time = Search(fromMotionMs, toMotionMs, Relative(fromMotionMs), Relative(toMotionMs));
        if (time is not { } contactTime) return null;
        var point = projectilePath.Position(contactTime);
        return new(contactTime, point.X, point.Y);

        (double X, double Y) Relative(double t)
        {
            var p = projectilePath.Position(t);
            var q = targetPath.Position(t);
            return (p.X - q.X, p.Y - q.Y);
        }

        double? Search(double a, double b, (double X, double Y) start, (double X, double Y) end)
        {
            double radius = radiusWorldUnits + DistanceTolerance;
            if (Length(start.X, start.Y) <= radius) return a;
            double dt = b - a;
            if (dt == 0) return null;
            double dx = end.X - start.X, dy = end.Y - start.Y;
            double length = Length(dx, dy);
            double ux = length == 0 ? 0 : dx / length, uy = length == 0 ? 0 : dy / length;
            double along = -(start.X * ux + start.Y * uy);
            double closest = Math.Clamp(along, 0, length);
            double chordDistance = Length(start.X + closest * ux, start.Y + closest * uy);
            // A twice-differentiable path deviates from its chord by <= a*dt²/8.
            // The speed-only bound also covers continuous paths with velocity corners.
            double error = acceleration is { } acc ? Math.Min(speed * dt / 2, acc * dt * dt / 8) : speed * dt / 2;
            if (chordDistance > radius + error) return null;
            if (acceleration == 0)
            {
                if (length == 0 || chordDistance > radius) return null;
                double perpendicular = Math.Abs(start.X * uy - start.Y * ux);
                if (perpendicular > radius) return null;
                double entry = along - Math.Sqrt(Math.Max(0, radius * radius - perpendicular * perpendicular));
                return a + Math.Clamp(entry / length, 0, 1) * dt;
            }
            double mid = a + dt / 2;
            if (speed * dt <= DistanceTolerance || mid == a || mid == b)
            {
                // The entire surviving interval is within the declared spatial
                // tolerance of the contact. Never discard it due to a search budget.
                return a + (length == 0 ? 0 : closest / length) * dt;
            }
            var middle = Relative(mid);
            return Search(a, mid, start, middle) ?? Search(mid, b, middle, end);
        }
    }

    private static double Length(double x, double y)
    {
        double scale = Math.Max(Math.Abs(x), Math.Abs(y));
        return scale == 0 ? 0 : scale * Math.Sqrt((x / scale) * (x / scale) + (y / scale) * (y / scale));
    }

    private static void Validate(CollisionPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(path.Position);
        if (!double.IsFinite(path.MaxSpeed) || path.MaxSpeed < 0 ||
            (path.MaxAcceleration is { } a && (!double.IsFinite(a) || a < 0)))
            throw new ArgumentOutOfRangeException(nameof(path));
    }
}
