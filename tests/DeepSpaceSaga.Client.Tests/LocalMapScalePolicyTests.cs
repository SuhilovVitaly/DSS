using DeepSpaceSaga.Client.UI;

namespace DeepSpaceSaga.Client.Tests;

public sealed class LocalMapScalePolicyTests
{
    public static IEnumerable<object[]> Viewports()
    {
        foreach (var (w, h) in new[] { (1280, 960), (1920, 1080), (3440, 1440), (3840, 1080), (7680, 2160), (12288, 3456), (int.MaxValue, 1), (int.MaxValue, int.MaxValue) })
            foreach (double speed in new[] { 2d, 4d }) yield return [w, h, speed];
    }

    [Theory]
    [MemberData(nameof(Viewports))]
    public void MaximumZoomBoundsHoursAndDiagonal(int width, int height, double speed)
    {
        var settings = new TacticalMapSettings().Validate();
        double ppu = MapScalePolicy.MaximumPpu(settings, width, height, speed);
        double unitsPerDay = speed * 86400 / 300 * 10;
        Assert.True(double.IsFinite(ppu));
        Assert.True(ppu >= settings.MaximumPpu);
        Assert.True(Math.Min(width, height) / ppu / unitsPerDay * 24 <= 4 + 1e-12);
        Assert.True(double.Hypot(width, height) / ppu / unitsPerDay < 1);
        // Device pixels enter exactly once; UI scale has no role in physical extent.
        Assert.Equal(ppu, MapScalePolicy.MaximumPpu(settings, width, height, speed));
    }

    [Fact]
    public void LegacyAndInvalidViewportHaveDeterministicFallback()
    {
        var settings = new TacticalMapSettings().Validate();
        Assert.Equal(4, settings.ReferenceSpeedKmS);
        Assert.Equal(4, settings.MaxLocalHours);
        foreach (double invalid in new[] { 0, -1, double.NaN, double.PositiveInfinity, double.MaxValue, double.Epsilon })
        {
            Assert.Equal(settings.ReferenceSpeedKmS, MapScalePolicy.ReferenceSpeed(settings, invalid));
            Assert.Equal(MapScalePolicy.MaximumPpu(settings, 12288, 3456, 4), MapScalePolicy.MaximumPpu(settings, 12288, 3456, invalid));
        }
        Assert.Equal(settings.MaximumPpu, MapScalePolicy.MaximumPpu(settings, 0, 1080, 2));
        Assert.Equal(settings.MaximumPpu, MapScalePolicy.MaximumPpu(settings, 1920, -1, 2));
        Assert.Throws<ArgumentException>(() => (settings with { ReferenceSpeedKmS = double.NaN }).Validate());
        Assert.Throws<ArgumentException>(() => (settings with { MaxLocalHours = 5 }).Validate());
    }
}
