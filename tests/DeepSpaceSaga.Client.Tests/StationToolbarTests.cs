using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class StationToolbarTests
{
    [Fact]
    public void Size_matches_the_1400x60_toolbar_spec()
    {
        Assert.Equal(1400f, StationToolbar.Width);
        Assert.Equal(60f, StationToolbar.Height);
    }

    [Fact]
    public void Draw_fills_the_interior_with_the_spec_background_color()
    {
        using var bitmap = new SKBitmap(1420, 80);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);

        StationToolbar.Draw(canvas, 10, 10);
        canvas.Flush();

        var interior = bitmap.GetPixel(700, 40);
        Assert.Equal(new SKColor(0x5e, 0x5e, 0x5e), interior);
    }

    [Fact]
    public void Draw_strokes_the_top_edge_with_the_spec_border_color()
    {
        using var bitmap = new SKBitmap(1420, 80);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);

        StationToolbar.Draw(canvas, 10, 10);
        canvas.Flush();

        var borderPixel = bitmap.GetPixel(700, 10);
        Assert.Equal(new SKColor(0x99, 0x99, 0x99), borderPixel);
    }

    [Fact]
    public void Draw_does_not_paint_outside_the_toolbar_bounds()
    {
        using var bitmap = new SKBitmap(1420, 80);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);

        StationToolbar.Draw(canvas, 10, 10);
        canvas.Flush();

        Assert.Equal(0, bitmap.GetPixel(0, 0).Alpha);
        Assert.Equal(0, bitmap.GetPixel(700, 75).Alpha);
    }
}
