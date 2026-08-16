using DeepSpaceSaga.Client.UI.Screens.GameSession;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class TacticalMapSelectionReticleTests
{
    [Fact]
    public void Selection_reticle_draws_circle_with_cross_arms_only_outside_it()
    {
        using var bitmap = new SKBitmap(80, 80);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        var renderer = new TacticalMapDepthRenderer();
        renderer.DrawSelectionReticle(canvas, 40f, 40f, markerRadius: 5f);

        // Four short arms run from outside to the doubled 17 px ring, while the center
        // remains substantially fainter because no line crosses the object.
        byte centerAlpha = bitmap.GetPixel(40, 40).Alpha;
        byte ringAlpha = bitmap.GetPixel(52, 52).Alpha;
        Assert.True(bitmap.GetPixel(40, 20).Alpha > 0);
        Assert.True(ringAlpha > 0);
        Assert.True(centerAlpha < ringAlpha / 4f);

        // Nothing is painted beyond the compact sight geometry.
        Assert.Equal(0, bitmap.GetPixel(64, 64).Alpha);
    }

    [Fact]
    public void Active_object_reticle_uses_orange_tint()
    {
        using var bitmap = new SKBitmap(80, 80);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        var renderer = new TacticalMapDepthRenderer();
        renderer.DrawActiveObjectReticle(canvas, 40f, 40f, markerRadius: 5f);

        SKColor ringPixel = bitmap.GetPixel(52, 52);
        Assert.True(ringPixel.Red > ringPixel.Green);
        Assert.True(ringPixel.Green > ringPixel.Blue);
        Assert.True(ringPixel.Alpha > 0);
    }


    [Fact]
    public void Active_rotates_clockwise_faster_than_selected_rotates_counter_clockwise()
    {
        const long elapsedUiTimeMs = 1_000;
        float activeAngle = TacticalMapDepthRenderer.GetActiveReticleRotationDegrees(elapsedUiTimeMs);
        float selectedAngle = TacticalMapDepthRenderer.GetSelectedReticleRotationDegrees(elapsedUiTimeMs);

        Assert.True(activeAngle > 0f);
        Assert.True(selectedAngle < 0f);
        Assert.True(Math.Abs(activeAngle) > Math.Abs(selectedAngle));
    }

    [Fact]
    public void Selection_reticle_scales_with_large_object_marker()
    {
        using var bitmap = new SKBitmap(128, 128);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        var renderer = new TacticalMapDepthRenderer();
        renderer.DrawSelectionReticle(canvas, 64f, 64f, markerRadius: 12.5f);

        // Planet-sized marker: doubled ring radius is 32 px, safely outside its edge.
        Assert.True(bitmap.GetPixel(96, 64).Alpha > 0);
        Assert.True(bitmap.GetPixel(64, 100).Alpha > 0);
        Assert.Equal(0, bitmap.GetPixel(112, 112).Alpha);
    }
}
