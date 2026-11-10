using System.Text.Json;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class MapDetailPolicyTests
{
    private static MapDetailLevel AtDays(double days, MapDetailLevel previous) =>
        MapDetailPolicy.Resolve(1920, 1080, 1080 / (days * 4 * 86400 / 300 * 10), 4, previous);

    [Fact]
    public void DetailDependsOnWorldSpanWithHysteresis()
    {
        Assert.Equal(MapDetailLevel.Local, AtDays(4d / 24 * 1.09, MapDetailLevel.Local));
        Assert.Equal(MapDetailLevel.Region, AtDays(4d / 24 * 1.11, MapDetailLevel.Local));
        Assert.Equal(MapDetailLevel.Region, AtDays(4d / 24 * .91, MapDetailLevel.Region));
        Assert.Equal(MapDetailLevel.Local, AtDays(4d / 24 * .89, MapDetailLevel.Region));
        Assert.Equal(MapDetailLevel.Region, AtDays(4.39, MapDetailLevel.Region));
        Assert.Equal(MapDetailLevel.Cluster, AtDays(4.41, MapDetailLevel.Region));
        Assert.Equal(MapDetailLevel.Cluster, AtDays(32.99, MapDetailLevel.Cluster));
        Assert.Equal(MapDetailLevel.System, AtDays(33.01, MapDetailLevel.Cluster));
        Assert.Equal(MapDetailLevel.System, AtDays(27.01, MapDetailLevel.System));
        Assert.Equal(MapDetailLevel.Cluster, AtDays(26.99, MapDetailLevel.System));
        Assert.Equal(MapDetailLevel.System, AtDays(100, MapDetailLevel.Local));
        Assert.Equal(MapDetailLevel.Local, AtDays(.01, MapDetailLevel.System));
        Assert.Equal(MapDetailLevel.Region, MapDetailPolicy.Resolve(0, 1, double.NaN, 4, MapDetailLevel.Region));
    }

    private static AuthoritativeSnapshot World() => new(1, 0, SimulationSpeed.Speed0,
        [new("PLAYER", 0, 0, 0, 0, ObjectType: SpaceObjectType.PlayerShip, RenderObjectType: SpaceObjectType.PlayerShip),
            new("s1", 100000, 0, 0, 0, ObjectType: SpaceObjectType.Station, RenderObjectType: SpaceObjectType.Station),
            new("s2", -100000, 0, 0, 0, ObjectType: SpaceObjectType.Station, RenderObjectType: SpaceObjectType.Station),
            new("s3", 100001, 0, 0, 0, ObjectType: SpaceObjectType.Station, RenderObjectType: SpaceObjectType.Station),
            new("r1", 100010, 0, 0, 0, ObjectType: SpaceObjectType.Asteroid, RenderObjectType: SpaceObjectType.Asteroid)], "PLAYER",
        ClusterMap: new(2, "c1", [new("c1", "First", "belt", ["s1", "s2"], "mixed"), new("c2", "Second", "belt", ["s3"], "mixed")],
            [new("s1", "c1", "mining"), new("s2", "c1", "industrial"), new("s3", "c2", "mining")], [],
            [new("r1", "c1", "s1", 10, 0)], new(4, 1, 1, 3)));

    [Fact]
    public void SystemMarkersUseDomainMembership()
    {
        var world = World();
        var markers = ClusterMapPresentation.DomainMarkers(world.ClusterMap!, world.Objects);
        Assert.Equal(2, markers.Count);
        var first = markers.Single(m => m.Id == "c1");
        Assert.Equal(new[] { "s1", "s2" }, first.MemberIds);
        Assert.Equal(0, first.X);
        Assert.Equal(-100000, first.Bounds.MinX); Assert.Equal(100000, first.Bounds.MaxX);
        // Nearby stations in different clusters remain distinct domain markers.
        Assert.Equal(new[] { "s3" }, markers.Single(m => m.Id == "c2").MemberIds);
        Assert.Empty(ClusterMapPresentation.ResourceMarkers(world.ClusterMap!, world.Objects));
    }

    [Fact]
    public void StationFocusFramesFiveFactoriesAtMaximumZoom()
    {
        const int width = 1280, height = 720;
        var world = World();
        var station = world.Objects[1];
        var orbit = new OrbitalElements(100000, 100000, 9000000, 90, 0, 0, "clockwise");
        var factories = Enumerable.Range(0, 5).Select(i =>
        {
            double angle = Math.Tau * i / 5;
            double x = Math.Cos(angle) * 35, y = Math.Sin(angle) * 35;
            return new ObjectMotionSnapshot($"factory-{i}", station.X + x, station.Y + y, 0, 0,
                RenderObjectType: SpaceObjectType.Factory, Orbit: orbit, WorldOffsetX: x, WorldOffsetY: y);
        }).ToArray();
        var objects = world.Objects.SetItem(1, station with { Orbit = orbit })
            .SetItem(4, world.Objects[4] with { X = station.X + 1141, Orbit = orbit, WorldOffsetX = 1141 })
            .AddRange(factories);
        var buffer = new SnapshotBuffer();
        buffer.Update(world with { Objects = objects, SelectedObjectId = station.ObjectId });
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, width, height);
        Assert.True(screen.FocusStation(station.ObjectId));
        canvas.Clear(SKColors.Black);
        screen.Render(canvas, width, height);

        Assert.Equal(MapDetailLevel.Local, screen.MapDetail);
        Assert.Equal(screen.LocalMaximumPpu, screen.CameraPixelsPerWorldUnit);
        var asteroid = objects[4];
        Assert.True(Math.Abs(asteroid.X - screen.CameraFocusX) * screen.CameraPixelsPerWorldUnit > width / 2d);
        Assert.Equal(0, screen.MapClusterCount);
        Assert.DoesNotContain("r1", screen.AggregatedMapObjectIds);
        foreach (var factory in factories)
        {
            Assert.DoesNotContain(factory.ObjectId, screen.AggregatedMapObjectIds);
            int x = (int)Math.Round(width / 2d + (factory.X - screen.CameraFocusX) * screen.CameraPixelsPerWorldUnit);
            int y = (int)Math.Round(height / 2d + (factory.Y - screen.CameraFocusY) * screen.CameraPixelsPerWorldUnit);
            Assert.InRange(x, 4, width - 5);
            Assert.InRange(y, 4, height - 5);
            Assert.Contains(Enumerable.Range(-3, 7).SelectMany(dy => Enumerable.Range(-3, 7)
                .Select(dx => bitmap.GetPixel(x + dx, y + dy))), pixel => pixel.Green > pixel.Red + 12);
        }
    }

    [Fact]
    public void FactoryLabelRequiresHoverEvenWhenSelected()
    {
        var factory = new ObjectMotionSnapshot("factory", 0, 0, 0, 0,
            RenderObjectType: SpaceObjectType.Factory, DisplayName: "Factory 1");
        ObjectRenderState[] states = [new(factory, factory, false)];
        var renderer = new ObjectLabelRenderer();
        var camera = new CameraState(0, 0, 1);
        renderer.ComputeGeometries(states, .02, 800, 600, camera, selectedObjectId: factory.ObjectId);
        Assert.Empty(renderer.Geometries);
        renderer.ComputeGeometries(states, .02, 800, 600, camera, hoveredObjectId: factory.ObjectId);
        Assert.Contains(factory.ObjectId, renderer.Geometries.Keys);
        renderer.ComputeGeometries(states, .02, 800, 600, camera, selectedObjectId: factory.ObjectId);
        Assert.Empty(renderer.Geometries);
    }

    [Fact]
    public void FactoryLinksDrawGrayOnlyForVisibleFactoriesAndTheirStation()
    {
        var orbit = new OrbitalElements(1000, 1000, 9000000, 90, 0, 0, "clockwise");
        var station = new ObjectMotionSnapshot("station", 0, 0, 0, 0, RenderObjectType: SpaceObjectType.Station, Orbit: orbit);
        var visible = new ObjectMotionSnapshot("visible", 35, 0, 0, 0, RenderObjectType: SpaceObjectType.Factory, Orbit: orbit);
        var hidden = visible with { ObjectId = "hidden", X = -35 };
        var other = visible with { ObjectId = "other", X = 0, Y = -35, Orbit = orbit with { InitialPhase = 91 } };
        ObjectRenderState[] states = [new(station, station, false), new(visible, visible, false),
            new(hidden, hidden, false), new(other, other, false)];
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black);
        ClusterMapPresentation.DrawFactoryLinks(canvas, states, new HashSet<string> { "hidden" }, new CameraState(0, 0, 1), 100, 100);
        var pixel = bitmap.GetPixel(65, 50);
        Assert.InRange(pixel.Red, (byte)15, (byte)100);
        Assert.Equal(pixel.Red, pixel.Green);
        Assert.Equal(pixel.Green, pixel.Blue);
        Assert.Equal(SKColors.Black, bitmap.GetPixel(35, 50));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(50, 35));
    }

    [Fact]
    public void AggregationNeverChangesWorldIdentity()
    {
        var source = World();
        var world = source with { SelectedObjectId = "r1", Objects = source.Objects.SetItem(0,
            source.Objects[0] with { NavigationTargetObjectId = "s3" }) };
        string before = JsonSerializer.Serialize(world);
        var buffer = new SnapshotBuffer(); buffer.Update(world);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        Assert.Equal(MapDetailLevel.Local, screen.MapDetail);
        var overview = screen.ScaleButtonRects[4]; screen.OnMouseDown(overview.MidX, overview.MidY);
        screen.Render(canvas, 1920, 1080);
        Assert.Equal(MapDetailLevel.System, screen.MapDetail);
        Assert.Equal(2, screen.MapClusterCount);
        Assert.Contains("s1", screen.AggregatedMapObjectIds);
        Assert.Contains("s2", screen.AggregatedMapObjectIds);
        Assert.DoesNotContain("s3", screen.AggregatedMapObjectIds);
        Assert.DoesNotContain("r1", screen.AggregatedMapObjectIds);
        Assert.DoesNotContain("PLAYER", screen.AggregatedMapObjectIds);
        Assert.Equal(before, JsonSerializer.Serialize(world));
        Assert.Equal(new[] { "PLAYER", "s1", "s2", "s3", "r1" }, world.Objects.Select(o => o.ObjectId));
    }
}
