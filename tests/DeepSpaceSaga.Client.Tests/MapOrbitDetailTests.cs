using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class MapOrbitDetailTests
{
    private static OrbitalElements Orbit(double radius) => new(radius, radius, 100000, 0, 0, 0, "clockwise");
    private static SolarSystemMapSnapshot Map() => new(2, 42, 1000, [], [new("planet", "Rocky", 2)],
        [new("planet", Orbit(60)), new("station", Orbit(100)), new("asteroid", Orbit(120))]);
    private static ObjectMotionSnapshot[] Objects() =>
        [new("planet", 0, -60, 0, 0, RenderObjectType: SpaceObjectType.Planet),
            new("station", 0, -100, 0, 0, RenderObjectType: SpaceObjectType.Station),
            new("asteroid", 0, -120, 0, 0, RenderObjectType: SpaceObjectType.Asteroid),
            new("player", 1, -99, 0, 0, RenderObjectType: SpaceObjectType.PlayerShip, IsDocked: true, DockedStationObjectId: "station")];

    [Fact]
    public void SmallObjectOrbitsDoNotFormOverviewBands()
    {
        var map = Map();
        map = map with { Orbits = map.Orbits.AddRange(Enumerable.Range(0, 1000).Select(i => new OrbitMapData("resource" + i, Orbit(100 + i * .01)))) };
        var visible = SolarSystemLayerRenderer.VisibleOrbits(map, true, MapDetailLevel.System, Objects());
        Assert.Equal("planet", Assert.Single(visible).ObjectId);
        using var bitmap = new SKBitmap(256, 256); using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        new SolarSystemLayerRenderer().Draw(canvas, map, new(0, 0, 1), SKRect.Create(256, 256), true, MapDetailLevel.System, Objects());
        Assert.True(bitmap.GetPixel(188, 128).Alpha > 0);
        Assert.Equal(0, bitmap.GetPixel(228, 128).Alpha);
    }

    [Fact]
    public void SelectionAndShowOrbitsControlVisibleCurves()
    {
        var map = Map(); var objects = Objects();
        Assert.Empty(SolarSystemLayerRenderer.VisibleOrbits(map, false, MapDetailLevel.System, objects, "asteroid", "station", "player"));
        Assert.Empty(SolarSystemLayerRenderer.VisibleOrbits(map, true, MapDetailLevel.Local, objects));
        Assert.Equal("asteroid", Assert.Single(SolarSystemLayerRenderer.VisibleOrbits(map, true, MapDetailLevel.Local, objects, "asteroid")).ObjectId);
        Assert.Equal("station", Assert.Single(SolarSystemLayerRenderer.VisibleOrbits(map, true, MapDetailLevel.Local, objects, "player")).ObjectId);
        Assert.Equal("station", Assert.Single(SolarSystemLayerRenderer.VisibleOrbits(map, true, MapDetailLevel.Local, objects, playerId: "player")).ObjectId);
        var duplicate = map with { Orbits = map.Orbits.SetItem(1, new("station", Orbit(60) with { InitialPhase = 180 })) };
        Assert.Single(SolarSystemLayerRenderer.VisibleOrbits(duplicate, true, MapDetailLevel.System, objects, "station"));
        using var bitmap = new SKBitmap(256, 256); using var canvas = new SKCanvas(bitmap);
        var renderer = new SolarSystemLayerRenderer();
        canvas.Clear(SKColors.Transparent);
        renderer.Draw(canvas, map, new(0, 0, 1), SKRect.Create(256, 256), true, MapDetailLevel.Local, objects, "station");
        Assert.True(bitmap.GetPixel(228, 128).Alpha > 0);
        Assert.Equal(0, bitmap.GetPixel(248, 128).Alpha);
        canvas.Clear(SKColors.Transparent);
        renderer.Draw(canvas, map, new(0, 0, 1), SKRect.Create(256, 256), false, MapDetailLevel.Local, objects, "station");
        Assert.Equal(0, bitmap.GetPixel(228, 128).Alpha);
    }
}
