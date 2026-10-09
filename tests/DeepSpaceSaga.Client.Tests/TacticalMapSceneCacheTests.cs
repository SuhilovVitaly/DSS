using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class TacticalMapSceneCacheTests
{
    private static TacticalMapFrameState Frame(params ObjectMotionSnapshot[] objects) =>
        new TacticalMapStateUpdater(new LinearMotionPredictor()).Update(
            new(new(new(1, 0, SimulationSpeed.Speed0, objects.ToImmutableArray()), 0), 0, SimulationSpeed.Speed0, 0), 0, .02);
    private static TacticalMapViewInput View() => new(new(0, 0, 1), 1000, 800, 1, SKRect.Create(1000, 800), [], 0, .1,
        null, null, null, ImmutableHashSet<string>.Empty, ImmutableHashSet<string>.Empty, [], [], [], []);

    [Fact]
    public async Task Unchanged_frame_reuses_geometry()
    {
        long clock = 0;
        var buffer = new SnapshotBuffer(() => clock);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [new("player", 0, 0, 1, 90)], PlayerShipObjectId: "player"));
        using var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => clock);
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        for (int i = 0; i < 5; i++)
        {
            screen.Render(surface.Canvas, 1280, 720);
            await screen.ObjectInfoPanel.PendingImageWork;
        }
        var before = (screen.PaintPreparations, screen.SceneGeometryBuilds, screen.ClusterGeometryBuilds,
            screen.TrailGeometryBuilds, screen.FutureGeometryBuilds, screen.LabelGeometryBuilds, screen.SpatialBoundsUpdates);
        var commands = screen.PreparedScene!.PaintCommands;
        for (int i = 0; i < 40; i++)
        {
            clock += System.Diagnostics.Stopwatch.Frequency / 80;
            screen.Render(surface.Canvas, 1280, 720);
        }
        Assert.Equal(before, (screen.PaintPreparations, screen.SceneGeometryBuilds, screen.ClusterGeometryBuilds,
            screen.TrailGeometryBuilds, screen.FutureGeometryBuilds, screen.LabelGeometryBuilds, screen.SpatialBoundsUpdates));
        Assert.Same(commands, screen.PreparedScene!.PaintCommands);
        Assert.True(screen.PaintReuses >= 40);
        screen.SetUiScale(1.5f);
        screen.Render(surface.Canvas, 1280, 720);
        Assert.True(screen.PaintPreparations > before.PaintPreparations);
        Assert.True(commands!.IsDisposed);
    }

    [Theory]
    [InlineData("world", true, true, 1)]
    [InlineData("pose", true, true, 1)]
    [InlineData("route", false, false, 0)]
    [InlineData("trail", false, false, 0)]
    [InlineData("camera", true, true, 0)]
    [InlineData("layout", false, true, 0)]
    [InlineData("settings", true, true, 0)]
    [InlineData("locale", false, true, 0)]
    [InlineData("selection", true, true, 0)]
    [InlineData("ui_time", false, false, 0)]
    public void Each_revision_invalidates_only_dependents(string revision, bool markers, bool hits, int spatial)
    {
        var obj = new ObjectMotionSnapshot("a", 0, 0, 0, 0);
        var frame = Frame(obj);
        var view = View();
        var builder = new TacticalMapSceneBuilder();
        builder.Prepare(frame, view);
        var baseline = (builder.Cache.MarkerBuilds, builder.Cache.HitBuilds, builder.Index.BoundsUpdates);
        switch (revision)
        {
            case "world": frame = Frame(obj, obj with { ObjectId = "b", X = 2 }); break;
            case "pose": frame = Frame(obj with { X = 2 }); break;
            case "route": frame = Frame(obj with { NavigationTargetX = 100, NavigationTargetY = 0 }); break;
            case "trail": view = view with { Trails = [new(new(0, 0), new(1, 1), SKColors.Gray)] }; break;
            case "camera": view = view with { Camera = new(1, 0, 1) }; break;
            case "layout": view = view with { FreeViewport = new(0, 0, 500, 500) }; break;
            case "settings": view = view with { CompactMarkerPpu = .2 }; break;
            case "locale": view = view with { LocaleRevision = 1 }; break;
            case "selection": view = view with { Selected = "a", ImportantIds = ImmutableHashSet.Create("a") }; break;
            default: frame = frame with { FrameId = 2, Timestamp = 100 }; break;
        }
        var scene = builder.Prepare(frame, view);
        Assert.Equal(baseline.MarkerBuilds + (markers ? 1 : 0), builder.Cache.MarkerBuilds);
        Assert.Equal(baseline.HitBuilds + (hits ? 1 : 0), builder.Cache.HitBuilds);
        Assert.Equal(baseline.BoundsUpdates + spatial, builder.Index.BoundsUpdates);
        if (revision == "route") Assert.Equal(100, scene.Markers[0].State.Source.NavigationTargetX);
    }

    [Fact]
    public void Cache_is_bounded_and_spatial_query_is_complete()
    {
        var objects = Enumerable.Range(0, 5000).Select(i => new ObjectMotionSnapshot(i.ToString(), i, 0, 0, 0)).ToArray();
        var builder = new TacticalMapSceneBuilder(32);
        var scene = builder.Prepare(Frame(objects), View());
        Assert.Equal(32, builder.Index.Count);
        Assert.True(builder.Index.Overflow);
        Assert.Contains(scene.HitCandidates, hit => hit.ObjectId == "499"); // overflow uses full frame, not truncated membership
        builder.Prepare(Frame(objects[4999]), View());
        Assert.InRange(builder.Index.Count, 0, 1);
        builder.Prepare(Frame(objects[4999]), View() with { Selected = "4999" });
        Assert.Equal(1, builder.Index.Count);

        var index = new TacticalMapSpatialIndex(4);
        index.BeginUpdate();
        index.Include("crossing", new(-100000, -1, 100000, 1)); // both endpoints outside
        index.Include("partial", new(500, 0, 502, 0));
        index.EndUpdate();
        var found = new HashSet<string>();
        Assert.True(index.Query(new(-10, -10, 10, 10), found));
        Assert.Contains("crossing", found);
        Assert.True(index.Query(new(499, -2, 501, 2), found));
        Assert.Contains("partial", found);
        index.Clear();
        Assert.Equal(0, index.Count);
    }
}
