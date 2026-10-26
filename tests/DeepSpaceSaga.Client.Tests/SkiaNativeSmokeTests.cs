using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class SkiaNativeSmokeTests
{
    [Fact]
    public void Native_surface_draws_and_reads_exact_pixels_without_a_window()
    {
        using var surface = SKSurface.Create(new SKImageInfo(8, 8, SKColorType.Rgba8888, SKAlphaType.Premul));
        Assert.NotNull(surface);
        surface.Canvas.Clear(SKColors.Black);
        using var paint = new SKPaint { Color = SKColors.Cyan, IsAntialias = false };
        surface.Canvas.DrawRect(new SKRect(2, 2, 6, 6), paint);
        using var image = surface.Snapshot();
        using var bitmap = SKBitmap.FromImage(image);
        Assert.Equal(SKColors.Black, bitmap.GetPixel(0, 0));
        Assert.Equal(SKColors.Cyan, bitmap.GetPixel(3, 3));
    }
}
