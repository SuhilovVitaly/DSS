using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// <see cref="NinePatch"/> stretches a small source image into an arbitrary-size panel:
/// the four corners are cut from the source's own corners and drawn unscaled, each edge
/// is a fixed <see cref="Corner"/>×<see cref="Corner"/> sample taken from the *middle* of
/// that edge (not the leftover strip next to the corners) and stretched along the long
/// axis, and the interior is stretched both ways. These tests use a synthetic 40×40
/// source with a distinct color painted into each sample region, so a wrong source rect
/// shows up as the wrong color in the output — they don't depend on the real
/// micro-panel.png asset's pixel layout.
/// </summary>
public class NinePatchTests
{
    private const int SourceSize = 40;
    private const float Corner = 10f;

    private static readonly SKColor CenterColor = new(0, 0, 255, 255);      // interior fill
    private static readonly SKColor TopSampleColor = new(0, 255, 0, 255);   // middle of top edge
    private static readonly SKColor LeftSampleColor = new(255, 255, 0, 255); // middle of left edge

    private static SKBitmap MakeSource()
    {
        var bitmap = new SKBitmap(SourceSize, SourceSize);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(CenterColor);

        using var src = new SKPaint { BlendMode = SKBlendMode.Src };

        // Transparent top-left corner, standing in for a rounded/cut panel corner.
        src.Color = SKColors.Transparent;
        canvas.DrawRect(new SKRect(0, 0, Corner, Corner), src);

        // Distinct markers at the exact source rects NinePatch is expected to sample for
        // the top and left edges: the middle of that edge, Corner×Corner in size.
        float mid = SourceSize / 2f;
        float half = Corner / 2f;
        src.Color = TopSampleColor;
        canvas.DrawRect(new SKRect(mid - half, 0, mid + half, Corner), src);
        src.Color = LeftSampleColor;
        canvas.DrawRect(new SKRect(0, mid - half, Corner, mid + half), src);

        return bitmap;
    }

    private static SKBitmap Render(SKRect dest)
    {
        using var source = MakeSource();
        var target = new SKBitmap((int)dest.Right, (int)dest.Bottom);
        using var canvas = new SKCanvas(target);
        canvas.Clear(SKColors.Transparent);
        NinePatch.Draw(canvas, source, dest, Corner);
        return target;
    }

    [Fact]
    public void Corner_transparency_is_preserved_unscaled()
    {
        using var result = Render(new SKRect(0, 0, 200, 120));

        Assert.Equal(0, result.GetPixel(1, 1).Alpha);
        Assert.Equal(0, result.GetPixel(8, 8).Alpha);
    }

    [Fact]
    public void Opposite_corner_is_unaffected_by_the_transparent_corner()
    {
        using var result = Render(new SKRect(0, 0, 200, 120));

        Assert.Equal(255, result.GetPixel(199, 119).Alpha);
    }

    [Fact]
    public void Top_edge_is_sampled_from_the_middle_of_the_source_edge_not_the_leftover_strip()
    {
        using var result = Render(new SKRect(0, 0, 200, 120));

        // Anywhere along the stretched top band (between the two corners) must show the
        // marker color that was painted at the middle of the source's top edge.
        Assert.Equal(TopSampleColor, result.GetPixel(100, 5));
        Assert.Equal(TopSampleColor, result.GetPixel(15, 5));
        Assert.Equal(TopSampleColor, result.GetPixel(185, 5));
    }

    [Fact]
    public void Left_edge_is_sampled_from_the_middle_of_the_source_edge_not_the_leftover_strip()
    {
        using var result = Render(new SKRect(0, 0, 200, 120));

        Assert.Equal(LeftSampleColor, result.GetPixel(5, 60));
        Assert.Equal(LeftSampleColor, result.GetPixel(5, 15));
        Assert.Equal(LeftSampleColor, result.GetPixel(5, 105));
    }

    [Fact]
    public void Center_is_stretched_to_fill_the_destination()
    {
        using var result = Render(new SKRect(0, 0, 200, 120));

        Assert.Equal(CenterColor, result.GetPixel(100, 60));
    }

    [Fact]
    public void Destination_smaller_than_twice_the_corner_does_not_throw()
    {
        using var dest = Render(new SKRect(0, 0, 6, 6));
    }

    [Fact]
    public void Offset_destination_rect_is_respected()
    {
        using var result = Render(new SKRect(10, 10, 210, 130));

        Assert.Equal(0, result.GetPixel(5, 5).Alpha);
        Assert.Equal(0, result.GetPixel(11, 11).Alpha);
    }
}
