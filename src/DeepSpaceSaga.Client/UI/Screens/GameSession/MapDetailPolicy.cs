namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

internal enum MapDetailLevel { Local, Region, Cluster, System }

/// <summary>Calendar-day spans at the frozen reference speed, with ten-percent hysteresis.</summary>
internal static class MapDetailPolicy
{
    private static readonly double[] Boundaries = [4d / 24, 4, 30];

    internal static MapDetailLevel Resolve(int width, int height, double ppu, double referenceSpeedKmS, MapDetailLevel previous)
    {
        double units = referenceSpeedKmS * 86400 / 300 * 10;
        if (width <= 0 || height <= 0 || !double.IsFinite(ppu) || ppu <= 0 || !double.IsFinite(units) || units <= 0)
            return previous;
        double days = Math.Min(width, height) / ppu / units;
        int level = (int)previous;
        while (level < 3 && days > Boundaries[level] * 1.1) level++;
        while (level > 0 && days < Boundaries[level - 1] * .9) level--;
        return (MapDetailLevel)level;
    }
}
