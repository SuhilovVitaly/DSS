using DeepSpaceSaga.Client.UI.Screens.GameSession;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class TacticalMapSelectionReticleTests
{
    [Fact]
    public void Selection_reticle_draws_circle_with_cross_arms_only_outside_it()
    {
        using var bitmap = new SKBitmap(64, 64);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        var renderer = new TacticalMapDepthRenderer();
        renderer.DrawSelectionReticle(canvas, 32f, 32f, markerRadius: 5f);

        // Four short arms run from outside to the 8.5 px ring, while the center
        // remains substantially fainter because no line crosses the object.
        byte centerAlpha = bitmap.GetPixel(32, 32).Alpha;
        byte ringAlpha = bitmap.GetPixel(38, 38).Alpha;
        Assert.True(bitmap.GetPixel(32, 20).Alpha > 0);
        Assert.True(ringAlpha > 0);
        Assert.True(centerAlpha < ringAlpha / 4f);

        // Nothing is painted beyond the compact sight geometry.
        Assert.Equal(0, bitmap.GetPixel(47, 47).Alpha);
    }

    [Fact]
    public void Selection_reticle_scales_with_large_object_marker()
    {
        using var bitmap = new SKBitmap(96, 96);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        var renderer = new TacticalMapDepthRenderer();
        renderer.DrawSelectionReticle(canvas, 48f, 48f, markerRadius: 12.5f);

        // Planet-sized marker: ring radius is 16 px, safely outside its edge.
        Assert.True(bitmap.GetPixel(64, 48).Alpha > 0);
        Assert.True(bitmap.GetPixel(48, 68).Alpha > 0);
        Assert.Equal(0, bitmap.GetPixel(72, 72).Alpha);
    }
}
