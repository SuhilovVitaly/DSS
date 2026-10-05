using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class BeltDetailRenderingTests
{
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
