using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class SystemMapNavigationTests
{
    [Fact]
    public void OrbitToggleKeepsWorldAndSelection()
    {
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0,
            [new("ship", 700000, 0, 0, 0, RenderObjectType: "PlayerShip"),
             new("station", 700150, 0, 0, 0, RenderObjectType: "Station")], "ship",
            SolarSystemMap: new(1, 1, 1100000, [new("belt", 650000, 750000, 1)], [], []));
        var buffer = new SnapshotBuffer(); buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        screen.OnMouseDown(1110, 540); screen.OnMouseUp(1110, 540);
        Assert.Equal("station", screen.SelectedObjectId);
        string before = JsonSerializer.Serialize(buffer.Latest!.Snapshot);
        var button = screen.MapViewButtonRects[5];
        screen.OnMouseDown(button.MidX, button.MidY);
        Assert.False(screen.ShowOrbits);
        screen.Render(canvas, 1920, 1080);
        Assert.Equal("station", screen.SelectedObjectId);
        Assert.Equal(before, JsonSerializer.Serialize(buffer.Latest!.Snapshot));
        screen.OnMouseDown(button.MidX, button.MidY);
        Assert.True(screen.ShowOrbits);
        button = screen.MapViewButtonRects[6];
        screen.OnMouseDown(button.MidX, button.MidY);
        Assert.False(screen.IsFocusAttachedToPlayer);
        Assert.False(screen.FitBelt("missing"));
        Assert.Equal("station", screen.SelectedObjectId);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.2f)]
    [InlineData(1.5f)]
    public void SelectedLabelWinsAtEveryScale(float scale)
    {
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0,
            [new("ship", 700000, 0, 0, 0, RenderObjectType: "PlayerShip"),
             new("station", 700150, 0, 0, 0, RenderObjectType: "Station"),
             new("asteroid", 700151, 0, 0, 0, RenderObjectType: "Asteroid")], "ship",
            SolarSystemMap: new(1, 1, 1100000, [new("belt", 650000, 750000, 1)], [], []));
        var buffer = new SnapshotBuffer(); buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), uiScale: scale);
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        screen.OnMouseDown(1110, 540); screen.OnMouseUp(1110, 540);
        Assert.Equal("station", screen.SelectedObjectId);
        Assert.True(screen.FitMapView(MapFitMode.System));
        screen.Render(canvas, 1920, 1080);
        Assert.Contains("station", screen.MapLabels.Keys);
        Assert.Contains("ship", screen.MapLabels.Keys);
        var button = screen.MapViewButtonRects[5];
        screen.OnMouseDown(button.MidX * scale, button.MidY * scale);
        Assert.False(screen.ShowOrbits);
        Assert.Equal("station", screen.SelectedObjectId);
    }

    [Fact]
    public void FitAndResizePreserveWorldDistances()
    {
        foreach (var scenario in ScenarioRepository.ListScenarios(Path.Combine(AppContext.BaseDirectory, "Scenarios")))
        {
            using var engine = SimulationEngine.CreateFromScenarioFile(DefaultSystemContentTests.Settings, scenario.ScenarioPath);
            engine.SetSpeed(SimulationSpeed.Speed0);
            var snapshot = engine.CaptureSnapshot();
            var buffer = new SnapshotBuffer(); buffer.Update(snapshot);
            string before = JsonSerializer.Serialize(snapshot);
            var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
            foreach (var (width, height) in new[] { (1280, 720), (1920, 1080) })
                foreach (float scale in new[] { 1f, 1.2f, 1.5f })
                {
                    screen.SetUiScale(scale);
                    using var bitmap = new SKBitmap(width, height);
                    using var canvas = new SKCanvas(bitmap);
                    screen.Render(canvas, width, height);
                    Assert.True(screen.FitMapView(MapFitMode.System));
                    CheckBounds(snapshot.SolarSystemMap!.SystemRadius);
                    foreach (var belt in snapshot.SolarSystemMap.Belts)
                    {
                        Assert.True(screen.FitBelt(belt.Id));
                        CheckBounds(belt.OuterRadius);
                    }
                    Assert.Equal(before, JsonSerializer.Serialize(buffer.Latest!.Snapshot));
                    void CheckBounds(double radius)
                    {
                        var free = screen.AvailableMapRect();
                        foreach (double sign in new[] { -1d, 1d })
                        {
                            double x = width / 2d + (sign * radius - screen.CameraFocusX) * screen.CameraPixelsPerWorldUnit;
                            double y = height / 2d + (sign * radius - screen.CameraFocusY) * screen.CameraPixelsPerWorldUnit;
                            Assert.InRange(x, free.Left - .01, free.Right + .01);
                            Assert.InRange(y, free.Top - .01, free.Bottom + .01);
                        }
                    }
                }
        }
    }
}
