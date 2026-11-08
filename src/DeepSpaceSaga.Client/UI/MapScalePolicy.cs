namespace DeepSpaceSaga.Client.UI;

/// <summary>Physical presentation limits; never changes world coordinates or a ship's speed.</summary>
internal static class MapScalePolicy
{
    internal static double ReferenceSpeed(TacticalMapSettings settings, double referenceSpeedKmS)
    {
        double day = referenceSpeedKmS * 86400 / 300 * 10;
        return double.IsFinite(day) && day > 0 &&
            double.IsFinite(int.MaxValue / (day * settings.MaxLocalHours / 24))
            ? referenceSpeedKmS : settings.ReferenceSpeedKmS;
    }

    internal static double MaximumPpu(TacticalMapSettings settings, int width, int height, double referenceSpeedKmS)
    {
        if (width <= 0 || height <= 0) return settings.MaximumPpu;
        double day = ReferenceSpeed(settings, referenceSpeedKmS) * 86400 / 300 * 10;
        double shortSide = Math.Min(width, height) / (day * settings.MaxLocalHours / 24);
        double diagonal = double.Hypot(width, height) / (day * .95);
        return Math.Max(settings.MaximumPpu, Math.Max(shortSide, diagonal));
    }
}
