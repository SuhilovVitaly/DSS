namespace DeepSpaceSaga.Motion;

/// <summary>Relative straight-line threat, using observed motion without a hidden projectile endpoint.</summary>
public sealed record BlastThreatResult(bool IsThreat, double? EntryAfterSeconds, double? ExitAfterSeconds,
    double ClosestAfterSeconds, double ClosestOffsetXKm, double ClosestOffsetYKm,
    double? DangerStartX, double? DangerStartY, double? DangerEndX, double? DangerEndY);

public static class BlastThreatMath
{
    public static BlastThreatResult Evaluate(double shipX, double shipY, double shipVelocityXKmS, double shipVelocityYKmS,
        double projectileX, double projectileY, double directionX, double directionY,
        double projectileSpeedKmS, double blastRadiusKm)
    {
        foreach (double value in new[] { shipX, shipY, shipVelocityXKmS, shipVelocityYKmS,
                     projectileX, projectileY, directionX, directionY, projectileSpeedKmS, blastRadiusKm })
            RequireFinite(value);
        if (projectileSpeedKmS < 0) throw new ArgumentOutOfRangeException(nameof(projectileSpeedKmS));
        if (blastRadiusKm < 0) throw new ArgumentOutOfRangeException(nameof(blastRadiusKm));
        double directionLength = Length(directionX, directionY);
        if (directionLength == 0 && projectileSpeedKmS != 0)
            throw new ArgumentException("A moving projectile requires an observed direction.", nameof(directionX));
        double projectileVx = directionLength == 0 ? 0 : directionX / directionLength * projectileSpeedKmS;
        double projectileVy = directionLength == 0 ? 0 : directionY / directionLength * projectileSpeedKmS;
        // One world unit is 100 metres. Divide before subtracting to avoid unnecessary overflow.
        double rx = projectileX / 10 - shipX / 10;
        double ry = projectileY / 10 - shipY / 10;
        double vx = projectileVx - shipVelocityXKmS;
        double vy = projectileVy - shipVelocityYKmS;
        double speed = Length(vx, vy);
        double radius = blastRadiusKm + 0.5;
        if (speed == 0)
        {
            bool inside = Length(rx, ry) < radius;
            return new(inside, inside ? 0 : null, null, 0, rx, ry,
                inside ? projectileX : null, inside ? projectileY : null, null, null);
        }

        double ux = vx / speed, uy = vy / speed;
        double along = rx * ux + ry * uy;
        double perpendicular = rx * uy - ry * ux;
        double closest = Math.Max(0, -along / speed);
        double closestX = rx + vx * closest, closestY = ry + vy * closest;
        RequireFinite(closest);
        RequireFinite(closestX);
        RequireFinite(closestY);
        if (Math.Abs(perpendicular) >= radius)
            return NoThreat();

        // Stable chord formula; avoids subtracting large squared distances in a discriminant.
        double halfChord = Math.Sqrt((radius - Math.Abs(perpendicular)) * (radius + Math.Abs(perpendicular)));
        double entry = Math.Max(0, (-along - halfChord) / speed);
        double exit = (-along + halfChord) / speed;
        RequireFinite(entry);
        RequireFinite(exit);
        if (exit <= 0) return NoThreat();
        var start = PointAt(entry);
        var end = PointAt(exit);
        return new(true, entry, exit, closest, closestX, closestY, start.X, start.Y, end.X, end.Y);

        BlastThreatResult NoThreat() => new(false, null, null, closest, closestX, closestY, null, null, null, null);

        (double X, double Y) PointAt(double time)
        {
            double x = projectileX + projectileVx * time * 10;
            double y = projectileY + projectileVy * time * 10;
            RequireFinite(x);
            RequireFinite(y);
            return (x, y);
        }
    }

    private static double Length(double x, double y)
    {
        RequireFinite(x);
        RequireFinite(y);
        double scale = Math.Max(Math.Abs(x), Math.Abs(y));
        if (scale == 0) return 0;
        double result = scale * Math.Sqrt((x / scale) * (x / scale) + (y / scale) * (y / scale));
        RequireFinite(result);
        return result;
    }

    private static void RequireFinite(double value)
    {
        if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value), "Threat geometry must be finite.");
    }
}
