using System.Collections.Immutable;

namespace DeepSpaceSaga.Motion.TacticalBattle;

public readonly record struct TacticalPose(double X, double Y, double Course);

/// <summary>V4 Combat §§1, 1.1 and 5. Pure geometry shared by rules and presentation.</summary>
public static class TacticalGeometry
{
    public const string HypotCPythonVersion = "3.12.3";
    public const double RadiansPerDegree = 0.017453292519943295;
    public const double DegreesPerRadian = 57.29577951308232;

    public static double Norm(double angle)
    {
        double value = PyMath.Fmod(angle + 180.0, 360.0);
        if (value < 0) value += 360.0;
        return value - 180.0;
    }
    public static double Sin(double radians) => Math.Sin(radians);
    public static double Cos(double radians) => Math.Cos(radians);
    public static double Atan2(double y, double x) => Math.Atan2(y, x);
    public static double Bearing(double ax, double ay, double bx, double by) => Atan2(by - ay, bx - ax) * DegreesPerRadian;
    public static (double X, double Y) Vec(double angle, double length)
    {
        double radians = angle * RadiansPerDegree;
        return (Cos(radians) * length, Sin(radians) * length);
    }
    public static double Dist(double ax, double ay, double bx, double by) => Hypot(bx - ax, by - ay);
    public static double Clamp(double value, double lo, double hi) => PyMath.Clamp(value, lo, hi);

    // CPython v3.12.3 Modules/mathmodule.c: vector_norm for n=2, dl_mul,
    // dl_fast_sum. Copyright (c) 2001-2023 Python Software Foundation.
    // PSF License Version 2 and adaptation summary: CPython-LICENSE.txt.
    // https://github.com/python/cpython/blob/v3.12.3/Modules/mathmodule.c#L2296
    public static double Hypot(double x, double y)
    {
        x = Math.Abs(x);
        y = Math.Abs(y);
        double max = 0;
        if (x > max) max = x;
        if (y > max) max = y;
        if (double.IsInfinity(max)) return max;
        if (double.IsNaN(x) || double.IsNaN(y)) return double.NaN;
        if (max == 0) return max;
        int maxExponent = Math.ILogB(max) + 1;
        if (maxExponent < -1023)
        {
            const double minNormal = 2.2250738585072014e-308;
            return minNormal * Hypot(x / (double)minNormal, y / (double)minNormal);
        }
        double scale = Math.ScaleB(1.0, -maxExponent);
        double csum = 1.0, frac1 = 0.0, frac2 = 0.0;
        Accumulate(x * scale, ref csum, ref frac1, ref frac2);
        Accumulate(y * scale, ref csum, ref frac1, ref frac2);
        double h = Math.Sqrt(csum - 1.0 + (frac1 + frac2));
        var product = Product(-h, h);
        var sum = FastSum(csum, product.Hi);
        csum = sum.Hi;
        frac1 += product.Lo;
        frac2 += sum.Lo;
        double correction = csum - 1.0 + (frac1 + frac2);
        h += correction / (double)(2.0 * h);
        return h / (double)scale;
    }
    private static void Accumulate(double value, ref double sum, ref double frac1, ref double frac2)
    {
        var product = Product(value, value);
        var total = FastSum(sum, product.Hi);
        sum = total.Hi;
        frac1 += product.Lo;
        frac2 += total.Lo;
    }
    private static (double Hi, double Lo) Product(double a, double b)
    {
        double high = a * b;
        return (high, Math.FusedMultiplyAdd(a, b, -high));
    }
    private static (double Hi, double Lo) FastSum(double a, double b)
    {
        double high = a + b;
        return (high, (a - high) + b);
    }

    public static TacticalPose Integrate(TacticalPose start, double speed, double turn, int substeps) => IntegrateCore(start, speed, turn, substeps, null);

    /// <summary>Start plus each substep; only the final course is normalized.</summary>
    public static ImmutableArray<TacticalPose> Path(TacticalPose start, double speed, double turn, int substeps)
    {
        Validate(start, speed, turn, substeps);
        var path = ImmutableArray.CreateBuilder<TacticalPose>(substeps + 1);
        path.Add(start);
        IntegrateCore(start, speed, turn, substeps, path);
        return path.MoveToImmutable();
    }
    private static TacticalPose IntegrateCore(TacticalPose start, double speed, double turn, int substeps, ImmutableArray<TacticalPose>.Builder? path)
    {
        Validate(start, speed, turn, substeps);
        double x = start.X, y = start.Y, course = start.Course;
        for (int i = 0; i < substeps; i++)
        {
            course += turn / (double)substeps;
            var delta = Vec(course, speed / (double)substeps);
            x += delta.X;
            y += delta.Y;
            if (i == substeps - 1) course = Norm(course);
            path?.Add(new(x, y, course));
        }
        return new(x, y, course);
    }
    private static void Validate(TacticalPose start, double speed, double turn, int substeps)
    {
        if (!double.IsFinite(start.X) || !double.IsFinite(start.Y) || !double.IsFinite(start.Course)
            || !double.IsFinite(speed) || speed < 0 || !double.IsFinite(turn) || substeps <= 0)
            throw new ArgumentOutOfRangeException(nameof(speed), "Finite pose, nonnegative speed and positive substeps are required.");
    }
}
