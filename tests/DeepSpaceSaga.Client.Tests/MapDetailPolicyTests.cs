using System.Text.Json;
using DeepSpaceSaga.Client;
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
        var resources = Assert.Single(ClusterMapPresentation.ResourceMarkers(world.ClusterMap!, world.Objects));
        Assert.Equal("s1", resources.Id); Assert.Equal(new[] { "r1" }, resources.MemberIds);
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
