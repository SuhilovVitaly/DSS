using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class MultiClusterOverviewTests
{
    [Fact]
    public void AllClustersVisibleAndFittable()
    {
        using var engine = SimulationEngine.CreateFromSettingsFile(DefaultSystemContentTests.Settings);
        var snapshot = engine.CaptureSnapshot();
        var buffer = new SnapshotBuffer(); buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        Assert.True(screen.FitMapView(MapFitMode.System));
        foreach (var cluster in snapshot.ClusterMap!.Clusters)
        {
            Assert.True(screen.FitCluster(cluster.Id));
            screen.Render(canvas, 1920, 1080);
            foreach (var stationId in cluster.StationIds)
            {
                Assert.Contains(screen.RenderStates, s => s.Pose.ObjectId == stationId);
                Assert.NotNull(ClusterMapPresentation.Station(snapshot, stationId));
            }
        }
        Assert.Equal(snapshot.ClusterMap.Clusters.Length, snapshot.ClusterMap.Clusters.Select(c => c.Id).Distinct().Count());
    }

    [Fact]
    public void OrbitEpochMovesClusterBounds()
    {
        using var engine = SimulationEngine.CreateFromSettingsFile(DefaultSystemContentTests.Settings);
        var first = engine.CaptureSnapshot();
        var later = engine.CaptureSnapshotForTests(86400000, DeepSpaceSaga.Contracts.SimulationSpeed.Speed0, 288000);
        Assert.Equal(JsonSerializer.Serialize(first.ClusterMap), JsonSerializer.Serialize(later.ClusterMap));
        var cluster = first.ClusterMap!.Clusters[0];
        var before = ClusterMapPresentation.Bounds(cluster, first.Objects);
        var after = ClusterMapPresentation.Bounds(cluster, later.Objects);
        Assert.NotEqual(before, after);
        var buffer = new SnapshotBuffer(); buffer.Update(later);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1280, 720); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        Assert.True(screen.FitCluster(cluster.Id));
        Assert.Equal(JsonSerializer.Serialize(first.ClusterMap), JsonSerializer.Serialize(buffer.Latest!.Snapshot.ClusterMap));
    }
}
