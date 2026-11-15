using DeepSpaceSaga.Client.UI.Screens.GameSession;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class TacticalMapStationTokenTests
{
    private static readonly SKColor StationColor = new(90, 200, 255);

    [Fact]
    public void Station_token_keeps_readable_area_of_station_colour()
    {
        using var bitmap = DrawToken(radius: 5f);

        int stationPixels = 0;
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
                if (bitmap.GetPixel(x, y) == StationColor)
                    stationPixels++;

        // A 10 px plate keeps a 6 x 6 colour face inside its edge and bevel;
        // the old 6.8 px plate left only about 2 x 2 px of station colour.
        Assert.True(stationPixels >= 25, $"station colour pixels: {stationPixels}");
    }

    [Fact]
    public void Station_token_spans_full_marker_diameter_like_hover_area()
    {
        using var bitmap = DrawToken(radius: 5f);

        // Centre 40, r = 5: the visible plate reaches x/y = 35 and 44, as a 10 px marker would.
        Assert.True(bitmap.GetPixel(35, 40).Alpha > 0);
        Assert.True(bitmap.GetPixel(44, 40).Alpha > 0);
        Assert.True(bitmap.GetPixel(40, 35).Alpha > 0);
        Assert.True(bitmap.GetPixel(40, 44).Alpha > 0);
        // Nothing beyond the plate and its 1-2 px drop shadow.
        Assert.Equal(0, bitmap.GetPixel(32, 40).Alpha);
        Assert.Equal(0, bitmap.GetPixel(40, 32).Alpha);
    }

    private static SKBitmap DrawToken(float radius)
    {
        var bitmap = new SKBitmap(80, 80);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        new TacticalMapDepthRenderer().DrawStationToken(canvas, 40f, 40f, radius, StationColor);
        return bitmap;
    }
}
