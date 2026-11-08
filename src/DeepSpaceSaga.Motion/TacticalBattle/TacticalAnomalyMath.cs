using System.Collections.Immutable;

namespace DeepSpaceSaga.Motion.TacticalBattle;

public enum TacticalAnomalyKind { Heat = 0, Dust = 1 }
/// <summary>Gaussian center, amplitude, positive world-unit sigmas and rotation in radians.</summary>
public readonly record struct TacticalClump(double X, double Y, double Amplitude, double SigmaX, double SigmaY, double Rotation);
public sealed record TacticalAnomaly(string Id, TacticalAnomalyKind Kind, ImmutableArray<TacticalClump> Clumps);
public readonly record struct TacticalAnomalySegment(double T0, double T1);

/// <summary>Pure Gaussian field and shared midpoint integration; all tuning is captured input.</summary>
public static class TacticalAnomalyMath
{
    public static double Field(ImmutableArray<TacticalAnomaly> anomalies, TacticalAnomalyKind kind,
        double x, double y, double background) => Evaluate(anomalies, kind, x, y, background, false, out _, out _);

    public static double FieldWithGradient(ImmutableArray<TacticalAnomaly> anomalies, TacticalAnomalyKind kind,
        double x, double y, double background, out double gx, out double gy) =>
        Evaluate(anomalies, kind, x, y, background, true, out gx, out gy);

    private static double Evaluate(ImmutableArray<TacticalAnomaly> anomalies, TacticalAnomalyKind kind,
        double x, double y, double background, bool gradient, out double gx, out double gy)
    {
        double total = background; gx = gy = 0;
        if (anomalies.IsDefaultOrEmpty) return total;
        foreach (var anomaly in anomalies)
        {
            if (anomaly.Kind != kind) continue;
            foreach (var clump in anomaly.Clumps)
            {
                if (!(clump.SigmaX > 0 && clump.SigmaY > 0))
                    throw new ArgumentOutOfRangeException(nameof(anomalies), "Gaussian sigma must be positive.");
                var (sin, cos) = Math.SinCos(clump.Rotation);
                double dx = x - clump.X, dy = y - clump.Y;
                double u = cos * dx + sin * dy, v = -sin * dx + cos * dy;
                double nx = u / clump.SigmaX, ny = v / clump.SigmaY;
                double value = clump.Amplitude * Math.Exp(-.5 * (nx * nx + ny * ny));
                total += value;
                if (!gradient || value == 0) continue;
                gx -= value * (nx * cos / clump.SigmaX - ny * sin / clump.SigmaY);
                gy -= value * (nx * sin / clump.SigmaX + ny * cos / clump.SigmaY);
            }
        }
        return total;
    }

    public static bool Inside(ImmutableArray<TacticalAnomaly> anomalies, TacticalAnomalyKind kind,
        double x, double y, double background, double zoneEdge) => Field(anomalies, kind, x, y, background) >= zoneEdge;

    public static double Absorption(ImmutableArray<TacticalAnomaly> anomalies, TacticalPose a, TacticalPose b,
        double background, double zoneEdge, int samples)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(samples, 1);
        if (anomalies.IsDefaultOrEmpty) return 0;
        int inside = 0;
        for (int i = 0; i < samples; i++)
            if (InsideSample(anomalies, a, b, background, zoneEdge, samples, i)) inside++;
        return inside / (double)samples;
    }

    public static ImmutableArray<TacticalAnomalySegment> Segments(ImmutableArray<TacticalAnomaly> anomalies,
        TacticalPose a, TacticalPose b, double background, double zoneEdge, int samples)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(samples, 1);
        if (anomalies.IsDefaultOrEmpty) return [];
        var segments = ImmutableArray.CreateBuilder<TacticalAnomalySegment>();
        int start = -1;
        for (int i = 0; i < samples; i++)
        {
            bool inside = InsideSample(anomalies, a, b, background, zoneEdge, samples, i);
            if (inside && start < 0) start = i;
            if (!inside && start >= 0)
            {
                segments.Add(new(start / (double)samples, i / (double)samples)); start = -1;
            }
        }
        if (start >= 0) segments.Add(new(start / (double)samples, 1));
        return segments.ToImmutable();
    }

    private static bool InsideSample(ImmutableArray<TacticalAnomaly> anomalies, TacticalPose a, TacticalPose b,
        double background, double zoneEdge, int samples, int index)
    {
        double t = (index + .5) / samples;
        return Inside(anomalies, TacticalAnomalyKind.Heat, a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t, background, zoneEdge);
    }
}
