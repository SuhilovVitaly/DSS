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

    [Theory]
    [InlineData(10, 20, 500, 550, 23)]
    [InlineData(40, 60, 900, 620, 32)]
    public void Title_position_is_centered_and_has_the_shared_top_inset(
        float left, float top, float width, float height, float textSize)
    {
        using var paint = new SKPaint { TextSize = textSize, TextAlign = SKTextAlign.Center };
        var bounds = new SKRect(left, top, left + width, top + height);

        var position = GenericWindowTypeA.TitlePosition(bounds, paint);

        Assert.Equal(bounds.MidX, position.X);
        Assert.Equal(bounds.Top + GenericWindowTypeA.TitleTopInset,
            position.Y + paint.FontMetrics.Ascent, precision: 3);
    }
}
