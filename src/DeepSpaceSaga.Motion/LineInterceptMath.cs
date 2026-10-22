namespace DeepSpaceSaga.Motion;

/// <summary>Constant-velocity interception. Position/velocity units must agree; result is seconds.</summary>
public static class LineInterceptMath
{
    public static double? Seconds(double x, double y, double vx, double vy, double speed)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(vx) || !double.IsFinite(vy) || !double.IsFinite(speed) || speed <= 0)
            throw new ArgumentOutOfRangeException(nameof(speed));
        double c = x * x + y * y;
        if (c == 0) return 0;
        double a = vx * vx + vy * vy - speed * speed, b = 2 * (x * vx + y * vy);
        if (Math.Abs(a) <= 1e-12 * Math.Max(1, speed * speed + vx * vx + vy * vy)) return b < 0 ? -c / b : null;
        double discriminant = b * b - 4 * a * c;
        if (discriminant < 0 || !double.IsFinite(discriminant)) return null;
        double q = -.5 * (b + Math.CopySign(Math.Sqrt(discriminant), b));
        double first = q / a, second = q == 0 ? double.PositiveInfinity : c / q;
        double result = Math.Min(first >= 0 ? first : double.PositiveInfinity, second >= 0 ? second : double.PositiveInfinity);
        return double.IsFinite(result) ? result : null;
    }
}
