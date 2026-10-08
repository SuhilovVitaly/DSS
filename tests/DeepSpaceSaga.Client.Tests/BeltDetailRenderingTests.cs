using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class BeltDetailRenderingTests
{
    [Theory]
    [InlineData(100256d)]
    [InlineData(50000d)]
    public void HugeOrbitsSubmitOnlyVisibleAccurateArcs(double minorAxis)
    {
        using var path = new SKPath();
        var viewport = SKRect.Create(512, 128);
        SolarSystemLayerRenderer.BuildVisibleEllipse(-100000, 64, 100256, minorAxis, viewport, path);
        Assert.False(path.IsEmpty);
        Assert.InRange(path.PointCount, 2, 64);
        Assert.All(path.Points, point =>
        {
            Assert.InRange(point.X, -.02f, 512.02f); Assert.InRange(point.Y, -.02f, 128.02f);
        });
        using var bitmap = new SKBitmap(512, 128); using var canvas = new SKCanvas(bitmap);
        using var paint = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
        canvas.Clear(SKColors.Transparent); canvas.DrawPath(path, paint);
        foreach (int y in new[] { 10, 64, 118 })
        {
            int x = (int)(-100000 + 100256 * Math.Sqrt(1 - Math.Pow((y - 64) / minorAxis, 2)));
            Assert.Contains(Enumerable.Range(x - 2, 5), px => bitmap.GetPixel(px, y).Alpha > 0);
        }
        SolarSystemLayerRenderer.BuildVisibleEllipse(0, 0, 100000, 100000, viewport, path);
        Assert.True(path.IsEmpty);
        SolarSystemLayerRenderer.BuildVisibleEllipse(double.NaN, 0, 100000, 100000, viewport, path);
        Assert.True(path.IsEmpty);
    }

    [Fact]
    public void BeltDetailLodAndSelection()
    {
        var belt = new BeltMapData("belt", 650000, 750000, 42);
        var layer = new SolarSystemLayerRenderer();
        Assert.Equal(512, layer.Decoration(belt, 0).Length);
        Assert.Equal(2048, layer.Decoration(belt, 1).Length);
        Assert.All(layer.Decoration(belt, 1), p => Assert.InRange(Math.Sqrt(p.X * p.X + p.Y * p.Y), 650000, 750000));
        var map = new SolarSystemMapSnapshot(1, 1, 825000, [belt], [], []);
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0,
            [new("ship", 700000, 0, 0, 0, RenderObjectType: "PlayerShip"),
             new("asteroid", 700150, 0, 0, 0, RenderObjectType: "Asteroid")], "ship", SolarSystemMap: map));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        screen.OnMouseDown(1110, 540);
        Assert.Equal("asteroid", screen.SelectedObjectId);
        Assert.Equal(2, buffer.Latest!.Snapshot.Objects.Length);
    }

    [Fact]
    public void DensityKeepsEntityCount()
    {
        var layer = new SolarSystemLayerRenderer();
        var belt = new BeltMapData("belt", 650000, 750000, 42);
        var a = layer.Decoration(belt, 1);
        Assert.Same(a, layer.Decoration(belt, 1));
        Assert.Equal(a, new SolarSystemLayerRenderer().Decoration(belt, 1));
        Assert.Equal(8192, layer.Decoration(belt with { DecorationSamples = 8192 }, 1).Length);
        var map = new SolarSystemMapSnapshot(1, 1, 825000, [belt], [], []);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        layer.Draw(canvas, map, new(0, 0, .0001), SKRect.Create(1280, 720));
        Assert.Equal(2, layer.CachedPatterns);
        layer.Draw(canvas, map with { Belts = [belt with { DecorationSeed = 99 }] }, new(0, 0, .0001), SKRect.Create(1280, 720));
        Assert.Equal(1, layer.CachedPatterns);
        Assert.Single(map.Belts); Assert.Empty(map.Orbits);
    }
}
