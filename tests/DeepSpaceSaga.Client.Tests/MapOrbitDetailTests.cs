using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class MapOrbitDetailTests
{
    private static ObjectRenderState Contact(string id, string type, double radius, double offsetX = 0, double offsetY = 0)
    {
        var orbit = new OrbitalElements(radius, radius, 100000, 90, 0, 0, "clockwise");
        var motion = new ObjectMotionSnapshot(id, radius + offsetX, offsetY, 0, 0, RenderObjectType: type,
            Orbit: orbit, WorldOffsetX: offsetX, WorldOffsetY: offsetY);
        return new(motion, motion, false);
    }

    [Fact]
    public void VisibleContactsDrawOrbitsWithoutSelection()
    {
        ObjectRenderState[] contacts =
        [
            Contact("planet", SpaceObjectType.Planet, 60),
            Contact("station", SpaceObjectType.Station, 100),
            Contact("asteroid", SpaceObjectType.Asteroid, 120, offsetY: 10),
            Contact("factory", SpaceObjectType.Factory, 80),
            Contact("hidden", SpaceObjectType.Asteroid, 70),
            Contact("offscreen", SpaceObjectType.Asteroid, 90, offsetX: 100),
            Contact("unknown", SpaceObjectType.UnknownSpaceObject, 50)
        ];
        using var bitmap = new SKBitmap(256, 256);
        using var canvas = new SKCanvas(bitmap);
        var renderer = new SolarSystemLayerRenderer();
        canvas.Clear(SKColors.Transparent);
        renderer.Draw(canvas, null, new(0, 0, 1), SKRect.Create(256, 256), true, contacts, new HashSet<string> { "hidden" });

        Assert.True(bitmap.GetPixel(188, 128).Alpha > 0);
        Assert.True(bitmap.GetPixel(228, 128).Alpha > 0);
        Assert.True(bitmap.GetPixel(128, 18).Alpha > 0);
        Assert.Equal(0, bitmap.GetPixel(208, 128).Alpha);
        Assert.Equal(0, bitmap.GetPixel(198, 128).Alpha);
        Assert.Equal(0, bitmap.GetPixel(138, 128).Alpha);
        Assert.True(bitmap.GetPixel(178, 128).Alpha > 0);

        canvas.Clear(SKColors.Transparent);
        renderer.Draw(canvas, null, new(0, 0, 1), SKRect.Create(256, 256), false, contacts);
        Assert.Equal(0, bitmap.GetPixel(228, 128).Alpha);
    }

    [Fact]
    public void DistrictSummaryDrawsAverageAndDetailedViewDrawsMemberOrbits()
    {
        var inner = Contact("s1", SpaceObjectType.Station, 60);
        var outer = Contact("s2", SpaceObjectType.Station, 100);
        var map = new StationClusterMapSnapshot(2, "c", [new("c", "District", "belt", ["s1", "s2"], "mixed")], [], []);
        var summary = Assert.Single(ClusterMapPresentation.DomainMarkers(map, [inner.Source, outer.Source]));
        Assert.Equal(40, summary.Diameter);
        Assert.Equal(80, summary.X);
        var orbit = Assert.IsType<MapOrbitCurve>(summary.Orbit);
        Assert.Equal(80, orbit.Major);
        using var bitmap = new SKBitmap(256, 256);
        using var canvas = new SKCanvas(bitmap);
        var renderer = new SolarSystemLayerRenderer();
        var viewport = SKRect.Create(256, 256);
        var camera = new CameraState(0, 0, 1);

        canvas.Clear(SKColors.Transparent);
        renderer.Draw(canvas, null, camera, viewport, true, [inner, outer], districtOrbits: [orbit],
            districtMembers: new HashSet<string> { "s1", "s2" });
        Assert.True(bitmap.GetPixel(208, 128).Alpha > 0);
        Assert.Equal(0, bitmap.GetPixel(188, 128).Alpha);
        Assert.Equal(0, bitmap.GetPixel(228, 128).Alpha);

        canvas.Clear(SKColors.Transparent);
        renderer.Draw(canvas, null, camera, viewport, true, [inner, outer]);
        Assert.Equal(0, bitmap.GetPixel(208, 128).Alpha);
        Assert.True(bitmap.GetPixel(188, 128).Alpha > 0);
        Assert.True(bitmap.GetPixel(228, 128).Alpha > 0);
    }

    [Fact]
    public void IdenticalOrbitsDrawOnlyOnce()
    {
        var station = Contact("station", SpaceObjectType.Station, 80);
        var asteroid = Contact("asteroid", SpaceObjectType.Asteroid, 80);
        var shifted = Contact("shifted", SpaceObjectType.Asteroid, 80, offsetX: 12);
        using var bitmap = new SKBitmap(256, 256);
        using var canvas = new SKCanvas(bitmap);
        var renderer = new SolarSystemLayerRenderer();
        var viewport = SKRect.Create(256, 256);
        var camera = new CameraState(0, 0, 1);

        canvas.Clear(SKColors.Transparent);
        renderer.Draw(canvas, null, camera, viewport, true, [station]);
        byte single = bitmap.GetPixel(208, 128).Alpha;
        Assert.True(single > 0);
        canvas.Clear(SKColors.Transparent);
        renderer.Draw(canvas, null, camera, viewport, true, [station, asteroid, shifted]);
        Assert.Equal(single, bitmap.GetPixel(208, 128).Alpha);
        Assert.True(bitmap.GetPixel(220, 128).Alpha > 0);
    }

    [Fact]
    public void StationsShareOneOrbitAndKeepItOverANearbyAsteroid()
    {
        var station = Contact("station", SpaceObjectType.Station, 80);
        var second = station.Source with { ObjectId = "second-station", X = -80, Orbit = station.Source.Orbit! with { InitialPhase = 270 } };
        var asteroid = Contact("asteroid", SpaceObjectType.Asteroid, 81);
        using var bitmap = new SKBitmap(256, 256);
        using var canvas = new SKCanvas(bitmap);
        var renderer = new SolarSystemLayerRenderer();
        var viewport = SKRect.Create(256, 256);
        var camera = new CameraState(0, 0, 1);

        canvas.Clear(SKColors.Transparent);
        renderer.Draw(canvas, null, camera, viewport, true, [station]);
        var sharedOrbit = Enumerable.Range(205, 8).Select(x => bitmap.GetPixel(x, 128)).ToArray();
        Assert.Contains(sharedOrbit, pixel => pixel.Alpha > 0);

        canvas.Clear(SKColors.Transparent);
        renderer.Draw(canvas, null, camera, viewport, true, [asteroid, station, new(second, second, false)]);
        Assert.Equal(sharedOrbit, Enumerable.Range(205, 8).Select(x => bitmap.GetPixel(x, 128)));
    }

    [Fact]
    public void CloseOrbitsMergeAtCurrentZoomAndSeparateWhenZoomingIn()
    {
        var first = Contact("station", SpaceObjectType.Station, 80);
        var nearby = Contact("asteroid", SpaceObjectType.Asteroid, 83);
        using var bitmap = new SKBitmap(256, 256);
        using var canvas = new SKCanvas(bitmap);
        var renderer = new SolarSystemLayerRenderer();
        var viewport = SKRect.Create(256, 256);
        var camera = new CameraState(0, 0, .5);

        canvas.Clear(SKColors.Transparent);
        renderer.Draw(canvas, null, camera, viewport, true, [first]);
        var oneOrbit = Enumerable.Range(165, 9).Select(x => bitmap.GetPixel(x, 128)).ToArray();
        canvas.Clear(SKColors.Transparent);
        renderer.Draw(canvas, null, camera, viewport, true, [first, nearby]);
        Assert.Equal(oneOrbit, Enumerable.Range(165, 9).Select(x => bitmap.GetPixel(x, 128)));

        camera.SetZoom(1, .5, 1);
        canvas.Clear(SKColors.Transparent);
        renderer.Draw(canvas, null, camera, viewport, true, [first, nearby]);
        Assert.True(bitmap.GetPixel(208, 128).Alpha > 0);
        Assert.True(bitmap.GetPixel(211, 128).Alpha > 0);
    }

    [Fact]
    public void CloseVisibleArcsOfOffsetCirclesMerge()
    {
        const double radius = 100000, shift = 400;
        var first = Contact("station", SpaceObjectType.Station, radius);
        var shifted = Contact("asteroid", SpaceObjectType.Asteroid, radius, offsetY: shift).Source with
        {
            X = Math.Sqrt(radius * radius - shift * shift),
            Y = 0
        };
        using var bitmap = new SKBitmap(512, 128);
        using var canvas = new SKCanvas(bitmap);
        var renderer = new SolarSystemLayerRenderer();
        var viewport = SKRect.Create(512, 128);
        var camera = new CameraState(radius, 0, 1);
        canvas.Clear(SKColors.Transparent);
        renderer.Draw(canvas, null, camera, viewport, true, [first]);
        var oneOrbit = Enumerable.Range(252, 9).Select(x => bitmap.GetPixel(x, 64)).ToArray();
        Assert.Contains(oneOrbit, pixel => pixel.Alpha > 0);
        canvas.Clear(SKColors.Transparent);
        renderer.Draw(canvas, null, camera, viewport, true, [first, new(shifted, shifted, false)]);
        Assert.Equal(oneOrbit, Enumerable.Range(252, 9).Select(x => bitmap.GetPixel(x, 64)));
    }
}
