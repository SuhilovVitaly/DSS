using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class TacticalMapSceneGeometryTests
{
    private static TacticalMapFrameState Frame(params ObjectMotionSnapshot[] objects) =>
        new TacticalMapStateUpdater(new LinearMotionPredictor()).Update(
            new(new(new(1, 0, SimulationSpeed.Speed0, objects.ToImmutableArray(), PlayerShipObjectId: "player"), 0),
                0, SimulationSpeed.Speed0, 0), 100, .02);

    private static TacticalMapViewInput View(double focus = 0) =>
        new(new(focus, 0, 1), 1000, 800, 1, SKRect.Create(1000, 800), [], 0, .1,
            null, null, null, ImmutableHashSet.Create("player"), ImmutableHashSet<string>.Empty, [], [], [], []);

    [Fact]
    public void Scene_preparation_is_deterministic()
    {
        var builder = new TacticalMapSceneBuilder();
        var frame = Frame(new("player", 0, 0, 0, 0), new("station", 20, 0, 0, 0, RenderObjectType: SpaceObjectType.Station));
        var first = builder.Prepare(frame, View());
        var next = builder.Prepare(frame, View());
        Assert.Equal(first.Markers.ToArray(), next.Markers.ToArray());
        Assert.Equal(first.HitCandidates.ToArray(), next.HitCandidates.ToArray());
        Assert.Equal("player", first.Markers[^1].State.Pose.ObjectId);
        builder.Prepare(Frame(new ObjectMotionSnapshot("other", 100, 0, 0, 0)), View());
        Assert.Equal(2, first.Markers.Length);
        Assert.Equal(new SKPoint(500, 400), first.Markers[^1].Center);
    }

    [Fact]
    public void Scene_geometry_matches_current_render_contract()
    {
        long clock = 0;
        var buffer = new SnapshotBuffer(() => clock);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0,
            [new("player", 0, 0, 1, 90), new("station", 200, 0, 0, 0, RenderObjectType: SpaceObjectType.Station),
             new("far", 10000000, 0, 0, 0)], PlayerShipObjectId: "player"));
        using var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => clock);
        using var surface = SKSurface.Create(new SKImageInfo(1920, 1080));
        screen.Render(surface.Canvas, 1920, 1080);
        var scene = Assert.IsType<TacticalMapSceneGeometry>(screen.PreparedScene);
        Assert.DoesNotContain(scene.Markers, marker => marker.State.Source.ObjectId == "far");
        Assert.Contains(scene.Markers, marker => marker.State.IsPlayerShip);
        Assert.All(scene.View.Labels, label => Assert.Equal(screen.MapLabels[label.State.Source.ObjectId], label.Geometry));
        Assert.Contains(scene.View.Paths, path => path.ObjectId == "player" && path.WorldPoints.Length >= 2);
        var originalPaths = scene.View.Paths.Select(p => p.WorldPoints.ToArray()).ToArray();
        buffer.Update(new(2, 1000, SimulationSpeed.Speed0, [new("player", 100, 0, 1, 90)], PlayerShipObjectId: "player"));
        screen.Render(surface.Canvas, 1920, 1080);
        for (int i = 0; i < originalPaths.Length; i++) Assert.Equal(originalPaths[i], scene.View.Paths[i].WorldPoints);
        Assert.Equal(0, scene.Frame.Objects[0].Pose.X);
    }

    [Fact]
    public void Large_coordinates_keep_camera_relative_precision()
    {
        const double origin = 1e12;
        var scene = new TacticalMapSceneBuilder().Prepare(
            Frame(new("a", origin, 0, 0, 0), new("b", origin + .25, 0, 0, 0)), View(origin));
        Assert.Equal(.25f, scene.Markers[1].Center.X - scene.Markers[0].Center.X);
        Assert.Equal(2, scene.HitCandidates.Length);
        var empty = new TacticalMapSceneBuilder().Prepare(scene.Frame, View(origin) with { Width = 0 });
        Assert.Empty(empty.Markers);
        Assert.Empty(empty.HitCandidates);
    }
}
