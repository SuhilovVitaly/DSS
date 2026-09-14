using System.Globalization;
using System.Text.Json;

namespace DeepSpaceSaga.Client.UI;

/// <summary>Client presentation only. World units and simulation distances never change.</summary>
public sealed record TacticalMapSettings
{
    public const double MetersPerWorldUnit = 100;
    public const double AstronomicalUnitKm = 149_597_870.7;
    public double[] MetersPerPixel { get; init; } = [100, 1000, 100_000, 1_000_000, 10_000_000];
    public double MinimumPpu { get; init; } = 1e-12;
    public double WheelFactor { get; init; } = 1.25;
    public double WheelUnitsPerStep { get; init; } = 1;
    // Zero is useful for reduced motion and deterministic callers; shipped Settings.json uses 120 ms.
    public double ZoomAnimationMs { get; init; }
    public double LabelDetailPpu { get; init; } = .1;
    public double CompactMarkerPpu { get; init; } = .1;
    public double TrailDetailPpu { get; init; } = .5;
    public int MaximumLabels { get; init; } = 48;
    public double ClusterPpu { get; init; } = .001;
    public double ClusterCellPixels { get; init; } = 40;
    public double GridBaseCellPixels { get; init; } = 200;
    public double GridMinimumPixels { get; init; } = 20;
    public double GridFadePixels { get; init; } = 20;
    public double FitPaddingPixels { get; init; } = 32;
    public double MaximumPpu => MetersPerWorldUnit / MetersPerPixel[0];
    public double[] ScaleTargets => MetersPerPixel.Select(m => MetersPerWorldUnit / m).ToArray();

    public TacticalMapSettings Validate()
    {
        static bool InRange(double v, double min, double max) => double.IsFinite(v) && v >= min && v <= max;
        if (MetersPerPixel is not { Length: 5 } || MetersPerPixel.Any(m => !InRange(m, 10, 1e12)) ||
            MetersPerPixel.Zip(MetersPerPixel.Skip(1)).Any(p => p.First >= p.Second) ||
            !InRange(MinimumPpu, 1e-15, MetersPerWorldUnit / MetersPerPixel[^1]) ||
            !InRange(WheelFactor, 1.01, 4) || !InRange(WheelUnitsPerStep, .01, 120) ||
            !InRange(ZoomAnimationMs, 0, 500) || !InRange(LabelDetailPpu, MinimumPpu, MaximumPpu) ||
            !InRange(CompactMarkerPpu, MinimumPpu, LabelDetailPpu) || !InRange(ClusterPpu, MinimumPpu, CompactMarkerPpu) ||
            !InRange(TrailDetailPpu, MinimumPpu, MaximumPpu) || MaximumLabels is < 4 or > 200 ||
            !InRange(ClusterCellPixels, 16, 128) || !InRange(GridMinimumPixels, 10, 100) ||
            !InRange(GridFadePixels, 1, 100) || !InRange(FitPaddingPixels, 8, 100) ||
            !InRange(GridBaseCellPixels, GridMinimumPixels + GridFadePixels, 1000))
            throw new ArgumentException("Invalid tactical map settings.");
        return this with { MetersPerPixel = (double[])MetersPerPixel.Clone() };
    }

    public static TacticalMapSettings Load(string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.TryGetProperty("gameSettings", out var game) && game.TryGetProperty("tacticalMap", out var map))
                return (map.Deserialize<TacticalMapSettings>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new()).Validate();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            InterfaceLog.Write("Invalid tacticalMap settings; using defaults.");
        }
        return new TacticalMapSettings();
    }

    public static string FormatDistance(double meters)
    {
        double km = meters / 1000;
        return km >= AstronomicalUnitKm * .1
            ? $"{(km / AstronomicalUnitKm).ToString("0.###", CultureInfo.InvariantCulture)} AU"
            : meters >= 1000 ? $"{km.ToString("#,0.###", CultureInfo.InvariantCulture)} km"
            : $"{meters.ToString("0.##", CultureInfo.InvariantCulture)} m";
    }
}
