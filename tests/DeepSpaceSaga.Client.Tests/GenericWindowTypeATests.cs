using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class GenericWindowTypeATests
{
    [Fact]
    public void Production_shell_asset_is_loaded()
    {
        Assert.True(GenericWindowTypeA.HasAssets);
    }

    [Theory]
    [InlineData(80, 60)]
    [InlineData(240, 720)]
    [InlineData(900, 180)]
    [InlineData(1200, 900)]
    public void Draw_supports_arbitrary_window_sizes(int width, int height)
    {
        using var bitmap = new SKBitmap(width + 20, height + 20);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);
        var bounds = new SKRect(10, 10, 10 + width, 10 + height);

        GenericWindowTypeA.Draw(canvas, bounds);
        canvas.Flush();

        Assert.Equal(0, bitmap.GetPixel(0, 0).Alpha);
        Assert.True(bitmap.GetPixel((int)bounds.MidX, (int)bounds.MidY).Alpha > 0);
        Assert.True(bitmap.GetPixel((int)bounds.MidX, (int)bounds.Top + 2).Alpha > 0);
        Assert.True(bitmap.GetPixel((int)bounds.Left + 2, (int)bounds.MidY).Alpha > 0);
    }
}
