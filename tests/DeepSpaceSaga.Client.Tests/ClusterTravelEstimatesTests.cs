using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class ClusterTravelEstimatesTests
{
    private static ObjectInfoPanelData Render(AuthoritativeSnapshot snapshot, int width = 1920, int height = 1080)
    {
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(width, height); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, width, height);
        return screen.SelectedOrActiveObjectInfo!.Value;
    }

    [Fact]
    public void EstimatesUseEpochAndMaxSpeed()
    {
        Assert.Equal(1d, ClusterMapPresentation.EstimateStraightDays(11520, 4));
        Assert.Equal(2d, ClusterMapPresentation.EstimateStraightDays(11520, 2));
        foreach (double speed in new[] { 0d, -1d, double.NaN, double.PositiveInfinity })
            Assert.Null(ClusterMapPresentation.EstimateStraightDays(115200, speed));
        Assert.Null(ClusterMapPresentation.EstimateStraightDays(double.MaxValue, double.Epsilon));
        using var engine = SimulationEngine.CreateFromSettingsFile(DefaultSystemContentTests.Settings);
        var first = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0);
        string remote = first.ClusterMap!.Clusters[1].StationIds[0];
        var before = Render(first with { SelectedObjectId = remote });
        var later = engine.CaptureSnapshotForTests(100 * 86400000L, SimulationSpeed.Speed0, 100 * 288000L);
        var after = Render(later with { SelectedObjectId = remote });
        Assert.NotNull(before.StraightFlightDays); Assert.NotEqual(before.StraightFlightDays, after.StraightFlightDays);
        Assert.Equal(100 * 288000L, after.EstimateMotionTimeMs);
        Assert.Equal(after, Render(later with { SelectedObjectId = remote }));
        var unknown = first with { SelectedObjectId = remote, Objects = first.Objects.Select(o => o.ObjectId == first.PlayerShipObjectId ? o with { MaxSpeedKmS = null } : o).ToImmutableArray() };
        Assert.Null(Render(unknown).StraightFlightDays);
    }

    [Fact]
    public void ResizeDoesNotChangeEstimate()
    {
        var snapshot = LocalClusterMapTests.Snapshot() with { SelectedObjectId = "s1" };
        snapshot = snapshot with { Objects = snapshot.Objects.Select(o => o.ObjectId == "ship" ? o with { MaxSpeedKmS = 4 } : o).ToImmutableArray() };
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080); var first = screen.SelectedOrActiveObjectInfo!.Value;
        Assert.True(screen.FitCluster("c")); screen.Render(canvas, 1280, 720);
        var after = screen.SelectedOrActiveObjectInfo!.Value;
        Assert.Equal(first.DistanceKm, after.DistanceKm); Assert.Equal(first.StraightFlightDays, after.StraightFlightDays);
        Assert.Equal(700d, after.DistanceKm);
        Assert.Equal(700d / 4 * 300 / 86400, after.StraightFlightDays);
    }

    [Fact]
    public void PlannedLineDiffersFromApproachAndQuote()
    {
        var snapshot = LocalClusterMapTests.Snapshot() with { SelectedObjectId = "s1" };
        var poses = new[] { snapshot.Objects.Single(o => o.ObjectId == "s1") with { X = -200, Y = 0 }, snapshot.Objects.Single(o => o.ObjectId == "s2") with { X = 200, Y = 0 } };
        using var bitmap = new SKBitmap(512, 128); using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        ClusterMapPresentation.DrawDirections(canvas, snapshot.ClusterMap!, "s1", poses, new CameraState(0, 0, 1), 512, 128);
        Assert.Contains(Enumerable.Range(80, 350), x => bitmap.GetPixel(x, 64).Alpha > 0);
        Assert.Contains(Enumerable.Range(80, 350), x => bitmap.GetPixel(x, 64).Alpha == 0);
        Assert.All(Enumerable.Range(52, 410), x => Assert.Equal(0, bitmap.GetPixel(x, 60).Alpha));
        var market = new StationMarketKnowledgeSnapshot("s1", "mining", false, 0, 1, true);
        var data = Render(snapshot with { StationMarketKnowledge = [market] });
        Assert.Same(market, data.MarketKnowledge);
        Assert.Contains(ObjectInfoPanel.BuildLines(data), l => l.Label == "Market" && l.Value == "Unavailable / STALE");
        Assert.Null(snapshot.DockedStationTrade);
        Assert.All(snapshot.Objects, o => Assert.Null(o.ApproachRoute));
    }
}
