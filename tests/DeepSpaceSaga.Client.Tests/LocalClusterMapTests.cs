using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class LocalClusterMapTests
{
    internal static AuthoritativeSnapshot Snapshot(int count = 10)
    {
        var stations = Enumerable.Range(1, count).Select(i => new ObjectMotionSnapshot($"s{i}", 700000 + i * 7000, 0, 0, 0, RenderObjectType: "Station", ObjectType: "Station")).ToImmutableArray();
        var map = new StationClusterMapSnapshot(1, "c", [new("c", "Home", "b", stations.Select(s => s.ObjectId).ToImmutableArray(), "balanced")],
            stations.Select(s => new ClusterStationData(s.ObjectId, "c", "market.scientific-military")).ToImmutableArray(), [new("l", "s1", "s2", ["item.electronics"])]);
        return new(1, 0, SimulationSpeed.Speed0, [new("ship", 700000, 0, 0, 0, RenderObjectType: "PlayerShip"), .. stations], "ship", ClusterMap: map);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(12)]
    public void StartClusterShowsEveryStation(int count)
    {
        var snapshot = Snapshot(count);
        var buffer = new SnapshotBuffer(); buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        Assert.True(screen.FitCluster("c"));
        screen.Render(canvas, 1920, 1080);
        foreach (var member in snapshot.ClusterMap!.Stations)
        {
            Assert.NotNull(ClusterMapPresentation.Station(snapshot, member.ObjectId));
            var station = snapshot.Objects.Single(o => o.ObjectId == member.ObjectId);
            float x = (float)(960 + (station.X - screen.CameraFocusX) * screen.CameraPixelsPerWorldUnit);
            float y = (float)(540 + (station.Y - screen.CameraFocusY) * screen.CameraPixelsPerWorldUnit);
            screen.OnMouseDown(x, y); screen.OnMouseUp(x, y);
            Assert.Equal(member.ObjectId, screen.SelectedObjectId);
            Assert.Equal("Home", screen.SelectedOrActiveObjectInfo!.Value.ClusterName);
        }
        Assert.False(screen.FitCluster("missing"));
    }

    [Fact]
    public void HumanScientificMilitaryIsOrdinaryMarket()
    {
        var snapshot = Snapshot();
        var station = ClusterMapPresentation.Station(snapshot, "s1")!;
        Assert.Equal("market.scientific-military", station.Profile);
        var lines = ObjectInfoPanel.BuildLines(new("s1", "Station", 0, 0, "Station", ClusterName: station.ClusterName, ClusterProfile: station.Profile, ClusterDirections: station.Directions));
        Assert.Contains(lines, l => l.Label == "Cluster" && l.Value == "Home");
        Assert.Contains(lines, l => l.Label == "Potential cargo" && l.Value.Contains("item.electronics"));
        Assert.Null(ClusterMapPresentation.Station(snapshot, "ship"));
    }

    [Fact]
    public void RemoteSelectionDoesNotOpenTrade()
    {
        var snapshot = Snapshot() with { SelectedObjectId = "s1" };
        var buffer = new SnapshotBuffer(); buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        Assert.Equal("s1", screen.SelectedObjectId);
        Assert.Null(snapshot.DockedStationTrade);
        Assert.Equal("Home", screen.SelectedOrActiveObjectInfo!.Value.ClusterName);
    }
}
