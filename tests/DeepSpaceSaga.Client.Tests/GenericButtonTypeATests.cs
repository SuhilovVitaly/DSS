using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class GenericButtonTypeATests
{
    [Fact]
    public void Production_assets_are_loaded()
    {
        Assert.True(GenericButtonTypeA.HasAssets);
    }

    [Theory]
    [InlineData(140, 44)]
    [InlineData(384, 56)]
    [InlineData(640, 84)]
    public void Draw_supports_different_button_sizes(int width, int height)
    {
        using var bitmap = new SKBitmap(width + 16, height + 16);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);
        var bounds = new SKRect(8, 8, 8 + width, 8 + height);

        GenericButtonTypeA.Draw(canvas, bounds, "ACTION", ButtonState.Normal);
        canvas.Flush();

        Assert.Equal(0, bitmap.GetPixel(0, 0).Alpha);
        Assert.True(bitmap.GetPixel((int)bounds.MidX, (int)bounds.MidY).Alpha > 0);
        Assert.True(bitmap.GetPixel((int)bounds.Left + 2, (int)bounds.MidY).Alpha > 0);
    }
}
