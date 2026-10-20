namespace DeepSpaceSaga.Motion;

/// <summary>Point projectile geometry in kilometres and physical milliseconds. Does not sample RNG.</summary>
public static class BlastMath
{
    public static double Falloff(double distanceKm, double radiusKm, double coreFraction)
    {
        NonnegativeFinite(distanceKm, nameof(distanceKm));
        PositiveFinite(radiusKm, nameof(radiusKm));
        if (!double.IsFinite(coreFraction) || coreFraction <= 0 || coreFraction >= 1)
            throw new ArgumentOutOfRangeException(nameof(coreFraction));
        double ratio = distanceKm / radiusKm;
        if (ratio <= coreFraction) return 1;
        if (ratio >= 1) return 0;
        return (1 - ratio) / (1 - coreFraction);
    }

    public static double ScatterSigma(double rangeKm, double scatterFraction, int accuracySkill) =>
        ScatterSigma(rangeKm, scatterFraction, accuracySkill, 15, 0.0333, 0.3);

    /// <summary>Configurable rule coefficients; scatterFraction is a fraction, for example 0.02 for 2%.</summary>
    public static double ScatterSigma(double rangeKm, double scatterFraction, int accuracySkill,
        int skillPivot, double skillSlope, double minMultiplier)
    {
        NonnegativeFinite(rangeKm, nameof(rangeKm));
        NonnegativeFinite(scatterFraction, nameof(scatterFraction));
        if (accuracySkill is < 0 or > 30) throw new ArgumentOutOfRangeException(nameof(accuracySkill));
        if (skillPivot is < 0 or > 30) throw new ArgumentOutOfRangeException(nameof(skillPivot));
        NonnegativeFinite(skillSlope, nameof(skillSlope));
        if (!double.IsFinite(minMultiplier) || minMultiplier <= 0 || minMultiplier > 1)
            throw new ArgumentOutOfRangeException(nameof(minMultiplier));
        double result = rangeKm * scatterFraction * Math.Max(minMultiplier, 1 - (accuracySkill - skillPivot) * skillSlope);
        NonnegativeFinite(result, nameof(rangeKm));
        return result;
    }

    /// <summary>Clamps progress to the captured segment; endpoints use the caller's common coordinate units.</summary>
    public static (double X, double Y) PositionAt(double startX, double startY, double endX, double endY, double progress)
    {
        Finite(startX, nameof(startX));
        Finite(startY, nameof(startY));
        Finite(endX, nameof(endX));
        Finite(endY, nameof(endY));
        Finite(progress, nameof(progress));
        progress = Math.Clamp(progress, 0, 1);
        // A weighted sum avoids overflowing end - start for finite opposite endpoints.
        return (startX * (1 - progress) + endX * progress, startY * (1 - progress) + endY * progress);
    }

    public static double DetonationTimeMs(double launchedAtMs, double distanceKm, double speedKmS)
    {
        NonnegativeFinite(launchedAtMs, nameof(launchedAtMs));
        NonnegativeFinite(distanceKm, nameof(distanceKm));
        PositiveFinite(speedKmS, nameof(speedKmS));
        double result = launchedAtMs + distanceKm / speedKmS * 1000;
        NonnegativeFinite(result, nameof(distanceKm));
        return result;
    }

    private static void Finite(double value, string name)
    {
        if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(name);
    }

    private static void NonnegativeFinite(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(name);
    }

    private static void PositiveFinite(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(name);
    }
}
